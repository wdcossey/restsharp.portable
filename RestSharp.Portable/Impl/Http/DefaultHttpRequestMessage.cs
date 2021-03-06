﻿using System;
using System.Net.Http;

using JetBrains.Annotations;

namespace RestSharp.Portable.Impl.Http
{
    /// <summary>
    /// Wraps an instance of the <see cref="HttpRequestMessage"/> as <see cref="IHttpRequestMessage"/>.
    /// </summary>
    public class DefaultHttpRequestMessage : IHttpRequestMessage
    {
        private readonly DefaultHttpHeaders _requestHttpHeaders;

        private IHttpContent _content;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultHttpRequestMessage"/> class.
        /// </summary>
        /// <param name="requestMessage">The request message to wrap</param>
        public DefaultHttpRequestMessage([NotNull] HttpRequestMessage requestMessage)
        {
            RequestMessage = requestMessage;
            _requestHttpHeaders = new DefaultHttpHeaders(requestMessage.Headers);
            _content = requestMessage.Content.AsRestHttpContent();
        }

        /// <summary>
        /// Gets the HTTP request message to wrap
        /// </summary>
        public HttpRequestMessage RequestMessage { get; private set; }

        /// <summary>
        /// Gets the HTTP headers for the request message
        /// </summary>
        public IHttpHeaders Headers
        {
            get { return _requestHttpHeaders; }
        }

        /// <summary>
        /// Gets or sets the HTTP request method
        /// </summary>
        public Method Method
        {
            get { return RequestMessage.Method.ToMethod(); }
            set { RequestMessage.Method = value.ToHttpMethod(); }
        }

        /// <summary>
        /// Gets or sets the request URI
        /// </summary>
        public Uri RequestUri
        {
            get { return RequestMessage.RequestUri; }
            set { RequestMessage.RequestUri = value; }
        }

        /// <summary>
        /// Gets or sets the HTTP protocol version
        /// </summary>
        public Version Version
        {
            get { return RequestMessage.Version; }
            set { RequestMessage.Version = value; }
        }

        /// <summary>
        /// Gets or sets the content of the request message
        /// </summary>
        public IHttpContent Content
        {
            get
            {
                return _content;
            }
            set
            {
                _content = value;
                RequestMessage.Content = value.AsHttpContent();
            }
        }

        /// <summary>
        /// Disposes the underlying HTTP request message
        /// </summary>
        public void Dispose()
        {
            RequestMessage.Dispose();
        }
    }
}
