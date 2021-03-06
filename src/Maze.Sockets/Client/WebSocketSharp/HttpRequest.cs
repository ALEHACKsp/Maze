#region License

/*
 * HttpRequest.cs
 *
 * The MIT License
 *
 * Copyright (c) 2012-2015 sta.blockhead
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

#endregion

#region Contributors

/*
 * Contributors:
 * - David Burhans
 */

#endregion

using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Maze.Sockets.Internal;

namespace Maze.Sockets.Client.WebSocketSharp
{
    internal class HttpRequest
    {
        public static HttpRequestMessage CreateHandshakeRequest(Uri uri)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri.PathAndQuery);

            request.Headers.Upgrade.Add(new ProductHeaderValue(MazeSocketHeaders.UpgradeSocket));
            request.Headers.Connection.Add(MazeSocketHeaders.Upgrade);

            if (uri.Port == 80 && uri.Scheme == "ws" || uri.Port == 443 && uri.Scheme == "wss")
                request.Headers.Host = uri.DnsSafeHost;
            else request.Headers.Host = uri.Authority;

            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("Maze.Socket", "1.0"));

            return request;
        }

        //#region Private Constructors

        //private HttpRequest(HttpMethod method, string uri, Version version, HttpRequestHeaders headers) : base(version,
        //    headers)
        //{
        //    HttpMethod = method;
        //    RequestUri = uri;

        //    RequestHeaders = headers;
        //}

        //#endregion

        //#region Internal Constructors

        //internal HttpRequest(HttpMethod method, string uri) : this(method, uri, HttpVersion.Version11, new HttpRequestHeaders())
        //{
        //    RequestHeaders.Set("User-Agent", "websocket-sharp/1.0");

        //    var message = new HttpRequestMessage();
        //    message.Headers
        //}

        //public HttpRequestHeaders RequestHeaders { get; }

        //#endregion

        //#region Private Fields

        //private CookieCollection _cookies;

        //#endregion

        //#region Public Properties

        //public HttpMethod HttpMethod { get; }

        //public bool IsWebSocketRequest =>
        //    HttpMethod.Equals(HttpMethod.Get) && ProtocolVersion > HttpVersion.Version10 && RequestHeaders.con Headers.Upgrades("websocket");

        //public string RequestUri { get; }

        //#endregion

        //#region Internal Methods

        //internal static HttpRequest CreateConnectRequest(Uri uri)
        //{
        //    var host = uri.DnsSafeHost;
        //    var port = uri.Port;
        //    var authority = String.Format("{0}:{1}", host, port);
        //    var req = new HttpRequest("CONNECT", authority);
        //    req.Headers["Host"] = port == 80 ? host : authority;

        //    return req;
        //}

        //internal static HttpRequest CreateWebSocketRequest(Uri uri)
        //{
        //    var req = new HttpRequest("GET", uri.PathAndQuery);
        //    var headers = req.Headers;

        //    // Only includes a port number in the Host header value if it's non-default.
        //    // See: https://tools.ietf.org/html/rfc6455#page-17
        //    var port = uri.Port;
        //    var schm = uri.Scheme;
        //    headers["Host"] = port == 80 && schm == "ws" || port == 443 && schm == "wss"
        //        ? uri.DnsSafeHost
        //        : uri.Authority;

        //    headers["Upgrade"] = "websocket";
        //    headers["Connection"] = "Upgrade";

        //    return req;
        //}

        //internal HttpResponse GetResponse(Stream stream, int millisecondsTimeout)
        //{
        //    var buff = ToByteArray();
        //    stream.Write(buff, 0, buff.Length);

        //    return Read(stream, HttpResponse.Parse, millisecondsTimeout);
        //}

        //internal static HttpRequest Parse(string[] headerParts)
        //{
        //    var requestLine = headerParts[0].Split(new[] { ' ' }, 3);
        //    if (requestLine.Length != 3)
        //        throw new ArgumentException("Invalid request line: " + headerParts[0]);

        //    var headers = new WebHeaderCollection();
        //    for (int i = 1; i < headerParts.Length; i++)
        //        headers.InternalSet(headerParts[i], false);

        //    return new HttpRequest(requestLine[0], requestLine[1], new Version(requestLine[2].Substring(5)), headers);
        //}

        //internal static HttpRequest Read(Stream stream, int millisecondsTimeout) =>
        //    Read(stream, Parse, millisecondsTimeout);

        //#endregion

        //#region Public Methods

        //public void SetCookies(CookieCollection cookies)
        //{
        //    if (cookies == null || cookies.Count == 0)
        //        return;

        //    var buff = new StringBuilder(64);
        //    foreach (var cookie in cookies.Sorted)
        //        if (!cookie.Expired)
        //            buff.AppendFormat("{0}; ", cookie.ToString());

        //    var len = buff.Length;
        //    if (len > 2)
        //    {
        //        buff.Length = len - 2;
        //        Headers["Cookie"] = buff.ToString();
        //    }
        //}

        //public override string ToString()
        //{
        //    var output = new StringBuilder(64);
        //    output.AppendFormat("{0} {1} HTTP/{2}{3}", HttpMethod, RequestUri, ProtocolVersion, CrLf);

        //    var headers = Headers;
        //    foreach (var key in headers.AllKeys)
        //        output.AppendFormat("{0}: {1}{2}", key, headers[key], CrLf);

        //    output.Append(CrLf);

        //    var entity = EntityBody;
        //    if (entity.Length > 0)
        //        output.Append(entity);

        //    return output.ToString();
        //}

        //#endregion
    }
}