﻿using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace RestSharp.Portable.Impl.Http
{
    /// <summary>
    /// Wraps a <see cref="HttpClient"/> instance as <see cref="IHttpClient"/>.
    /// </summary>
    public class DefaultHttpClient : IHttpClient
    {
        private readonly DefaultHttpHeaders _defaultHeaders;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultHttpClient"/> class.
        /// </summary>
        /// <param name="client">The <see cref="HttpClient"/> to wrap.</param>
        public DefaultHttpClient([NotNull] HttpClient client)
        {
            Client = client;
            _defaultHeaders = new DefaultHttpHeaders(client.DefaultRequestHeaders);
        }

        /// <summary>
        /// Gets the HTTP client to wrap.
        /// </summary>
        public HttpClient Client { get; private set; }

        /// <summary>
        /// Gets or sets the base address of the HTTP client
        /// </summary>
        public Uri BaseAddress
        {
            get { return Client.BaseAddress; }
            set { Client.BaseAddress = value; }
        }

        /// <summary>
        /// Gets the default request headers
        /// </summary>
        public IHttpHeaders DefaultRequestHeaders
        {
            get { return _defaultHeaders; }
        }

        /// <summary>
        /// Gets or sets the timeout
        /// </summary>
        public TimeSpan Timeout
        {
            get { return Client.Timeout; }
            set { Client.Timeout = value; }
        }

        /// <summary>
        /// Asynchronously send a request
        /// </summary>
        /// <param name="request">The request do send</param>
        /// <param name="cancellationToken">The cancellation token used to signal an abortion</param>
        /// <returns>The task to query the response</returns>
        public async Task<IHttpResponseMessage> SendAsync(IHttpRequestMessage request, CancellationToken cancellationToken)
        {
            var requestMessage = request.AsHttpRequestMessage();
            var response = await Client.SendAsync(requestMessage, cancellationToken);
            return new DefaultHttpResponseMessage(response);
        }

        /// <summary>
        /// Disposes the underlying HTTP client
        /// </summary>
        public void Dispose()
        {
            Client.Dispose();
        }
    }
}
