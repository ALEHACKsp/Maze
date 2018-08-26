﻿using System;
using System.Buffers;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Orcus.Sockets
{
    public class WebSocketWrapper : IDataSocket
    {
        private readonly int _packageBufferSize;

        public WebSocketWrapper(WebSocket webSocket, int packageBufferSize)
        {
            WebSocket = webSocket;
            _packageBufferSize = packageBufferSize + 1;
            BufferPool = ArrayPool<byte>.Create(_packageBufferSize * 5, 10);
        }

        public void Dispose()
        {
            WebSocket.Dispose();
        }

        public WebSocket WebSocket { get; }

        public ArrayPool<byte> BufferPool { get; }
        public event EventHandler<DataReceivedEventArgs> DataReceivedEventArgs;

        public async Task ReceiveAsync()
        {
            while (WebSocket.State == WebSocketState.Open)
            {
                var buffer = BufferPool.Rent(_packageBufferSize);

                var i = 0;
                while (true)
                {
                    var result = await WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer, i, _packageBufferSize - i),
                        CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                        return;

                    if (!result.EndOfMessage)
                    {
                        i = result.Count;
                        continue;
                    }

                    var opCode = (OrcusSocket.MessageOpcode) buffer[0];
                    var payload = new ArraySegment<byte>(buffer, 1, result.Count + i - 1);

                    DataReceivedEventArgs?.Invoke(this, new DataReceivedEventArgs(payload, opCode));
                    break;
                }
            }
        }

        public Task SendFrameAsync(OrcusSocket.MessageOpcode opcode, ArraySegment<byte> payloadBuffer,
            CancellationToken cancellationToken)
        {
            var sendBuffer = WriteFrameToSendBuffer(opcode, payloadBuffer);
            return WebSocket.SendAsync(sendBuffer, WebSocketMessageType.Binary, true, cancellationToken)
                .ContinueWith(task => ReleaseSendBuffer(sendBuffer.Array));
        }

        /// <summary>Writes a frame into the send buffer, which can then be sent over the network.</summary>
        private ArraySegment<byte> WriteFrameToSendBuffer(OrcusSocket.MessageOpcode opcode, ArraySegment<byte> payloadBuffer)
        {
            var sendBuffer = AllocateSendBuffer(payloadBuffer.Count + 1);
            sendBuffer[0] = (byte) opcode;

            if (payloadBuffer.Count > 0)
                Buffer.BlockCopy(payloadBuffer.Array, payloadBuffer.Offset, sendBuffer, 1,
                    payloadBuffer.Count);

            return new ArraySegment<byte>(sendBuffer, 0, 1 + payloadBuffer.Count);
        }

        private byte[] AllocateSendBuffer(int minLength) => ArrayPool<byte>.Shared.Rent(minLength);

        private void ReleaseSendBuffer(byte[] buffer)
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}