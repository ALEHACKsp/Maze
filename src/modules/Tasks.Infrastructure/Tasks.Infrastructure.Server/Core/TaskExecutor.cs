using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Maze.Server.Connection.Extensions;
using Maze.Server.Library.Hubs;
using Maze.Utilities;
using Tasks.Infrastructure.Core;
using Tasks.Infrastructure.Core.Commands;
using Tasks.Infrastructure.Core.Dtos;
using Tasks.Infrastructure.Management;
using Tasks.Infrastructure.Management.Data;
using Tasks.Infrastructure.Server.Core.Storage;
using Tasks.Infrastructure.Server.Library;

namespace Tasks.Infrastructure.Server.Core
{
    public class TaskExecutor
    {
        private readonly MazeTask _mazeTask;
        private readonly TaskSession _taskSession;
        private readonly ITaskResultStorage _taskResultStorage;
        private readonly IServiceProvider _services;
        private readonly ILogger<TaskExecutor> _logger;
        private readonly ActiveTasksManager _activeTasksManager;
        private readonly IHubContext<AdministrationHub> _hubContext;

        private readonly IReadOnlyDictionary<CommandInfo, Type> _executorTypes;
        private readonly IReadOnlyDictionary<Type, MethodInfo> _executionMethods;

        public TaskExecutor(MazeTask mazeTask, TaskSession taskSession, ITaskResultStorage taskResultStorage, IServiceProvider services)
        {
            _mazeTask = mazeTask;
            _taskSession = taskSession;
            _taskResultStorage = taskResultStorage;
            _services = services;

            _logger = services.GetRequiredService<ILogger<TaskExecutor>>();
            _activeTasksManager = services.GetRequiredService<ActiveTasksManager>();
            _hubContext = services.GetRequiredService<IHubContext<AdministrationHub>>();

            _executorTypes = _mazeTask.Commands.ToDictionary(x => x, commandInfo => typeof(ITaskExecutor<>).MakeGenericType(commandInfo.GetType()));
            _executionMethods = _executorTypes.Values.ToDictionary(x => x, executorType => executorType.GetMethod("InvokeAsync"));
        }

        public Task Execute(IEnumerable<TargetId> attenders, ConcurrentDictionary<TargetId, IServiceScope> attenderScopes, CancellationToken cancellationToken)
        {
            return TaskCombinators.ThrottledAsync(attenders, async (id, token) =>
            {
                if (!attenderScopes.TryGetValue(id, out var scope))
                    scope = _services.CreateScope();

                using (scope)
                {
                    await ExecuteAttender(id, _services, token);
                }
            }, cancellationToken);
        }

        private async Task ExecuteAttender(TargetId id, IServiceProvider services, CancellationToken cancellationToken)
        {
            var execution = new TaskExecutionDto
            {
                TaskExecutionId = Guid.NewGuid(),
                TaskSessionId = _taskSession.TaskSessionId,
                TargetId = id.IsServer ? (int?) null : id.ClientId,
                CreatedOn = DateTimeOffset.UtcNow,
                TaskReferenceId = _mazeTask.Id
            };

            if (!await _taskResultStorage.CreateTaskExecution(execution))
                return;

            await _hubContext.Clients.All.SendAsync(HubEventNames.TaskExecutionCreated, execution, cancellationToken);

            var machineStatus = _activeTasksManager.ActiveCommands.GetOrAdd(id, _ => new TasksMachineStatus());
            foreach (var commandInfo in _mazeTask.Commands)
            {
                await ExecuteCommand(id, services, commandInfo, machineStatus, execution.TaskExecutionId, cancellationToken);
            }
        }

        private async Task ExecuteCommand(TargetId id, IServiceProvider services, CommandInfo commandInfo, TasksMachineStatus status, Guid executionId, CancellationToken cancellationToken)
        {
            var executorType = _executorTypes[commandInfo];
            if (executorType == null)
                return;

            var service = services.GetService(executorType);
            if (service == null)
                return;

            var commandName = commandInfo.GetType().Name.TrimEnd("CommandInfo", StringComparison.Ordinal);
            var commandResult = new CommandResultDto
            {
                CommandResultId = Guid.NewGuid(),
                TaskExecutionId = executionId,
                CommandName = commandName
            };

            var commandProcessDto = new CommandProcessDto
            {
                CommandResultId = commandResult.CommandResultId, TaskExecutionId = executionId, CommandName = commandName
            };

            Task UpdateStatus(CommandProcessDto arg)
            {
                commandProcessDto.StatusMessage = arg.StatusMessage;
                commandProcessDto.Progress = arg.Progress;

                return _hubContext.Clients.All.SendAsync(HubEventNames.TaskCommandProcess, commandProcessDto, cancellationToken);
            }

            var executionMethod = _executionMethods[executorType];

            status.Processes.TryAdd(commandProcessDto.CommandResultId, commandProcessDto);
            try
            {
                using (var context = new DefaultTaskExecutionContext(_taskSession, _mazeTask, services, UpdateStatus))
                {
                    context.ReportProgress(null); //important to notify about the start of the operation

                    var task = (Task<HttpResponseMessage>) executionMethod.Invoke(service,
                        new object[] {commandInfo, id, context, cancellationToken});
                    var response = await task;

                    using (var memoryStream = new MemoryStream())
                    {
                        await HttpResponseSerializer.Format(response, memoryStream);

                        commandResult.Result = Convert.ToBase64String(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);
                        commandResult.Status = (int) response.StatusCode;
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "An error occurred when executing {method}", executorType.FullName);
                commandResult.Result = e.ToString();
            }
            finally
            {
                status.Processes.TryRemove(commandProcessDto.CommandResultId, out _);
            }

            commandResult.FinishedAt = DateTimeOffset.UtcNow;
            
            try
            {
                await _taskResultStorage.CreateCommandResult(commandResult);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Executing CreateCommandResult failed.");
            }

            await _hubContext.Clients.All.SendAsync(HubEventNames.TaskCommandResultCreated, commandResult);
        }
    }
}