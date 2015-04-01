﻿#region License
//   Copyright 2010 John Sheehan
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License. 
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace RestSharp.Portable.Authenticators
{
    /// <summary>
    /// Base class for OAuth 2 Authenticators.
    /// </summary>
    /// <remarks>
    /// Since there are many ways to authenticate in OAuth2,
    /// this is used as a base class to differentiate between 
    /// other authenticators.
    /// 
    /// Any other OAuth2 authenticators must derive from this
    /// abstract class.
    /// </remarks>
    public abstract class OAuth2Authenticator : AsyncAuthenticator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OAuth2Authenticator"/> class.
        /// </summary>
        /// <param name="client">The OAuth2 client</param>
        protected OAuth2Authenticator(OAuth2.OAuth2Client client)
        {
            Client = client;
        }

        /// <summary>
        /// Gets a value indicating whether the authentication module supports pre-authentication.
        /// </summary>
        public override bool CanPreAuthenticate
        {
            get { return true; }
        }

        /// <summary>
        /// The OAuth client that is used by this authenticator
        /// </summary>
        protected OAuth2.OAuth2Client Client { get; private set; }

        /// <summary>
        /// Will be called when the authentication failed
        /// </summary>
        /// <param name="client">Client executing this request</param>
        /// <param name="request">Request to authenticate</param>
        /// <param name="response">Response of the failed request</param>
        /// <returns>Task where the handler for a failed authentication gets executed</returns>
        public override async Task HandleChallenge(IRestClient client, IRestRequest request, HttpResponseMessage response)
        {
            if (string.IsNullOrEmpty(Client.RefreshToken))
                return;
            await Client.GetCurrentToken(forceUpdate: true);
        }

        /// <summary>
        /// Determines if the authentication module can handle the challenge sent with the response.
        /// </summary>
        /// <param name="response">The response that returned the authentication challenge</param>
        /// <returns>true when the authenticator can handle the sent challenge</returns>
        public override bool CanHandleChallenge(HttpResponseMessage response)
        {
            return !string.IsNullOrEmpty(Client.RefreshToken);
        }
    }

    /// <summary>
    /// The OAuth 2 authenticator using URI query parameter.
    /// </summary>
    /// <remarks>
    /// Based on http://tools.ietf.org/html/draft-ietf-oauth-v2-10#section-5.1.2
    /// </remarks>
    public class OAuth2UriQueryParameterAuthenticator : OAuth2Authenticator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OAuth2UriQueryParameterAuthenticator"/> class.
        /// </summary>
        /// <param name="client">The OAuth2 client</param>
        public OAuth2UriQueryParameterAuthenticator(OAuth2.OAuth2Client client)
            : base(client) { }

        /// <summary>
        /// Modifies the request to ensure that the authentication requirements are met.
        /// </summary>
        /// <param name="client">Client executing this request</param>
        /// <param name="request">Request to authenticate</param>
        /// <returns></returns>
        public override async Task PreAuthenticate(IRestClient client, IRestRequest request)
        {
            request.AddParameter("oauth_token", await Client.GetCurrentToken(), ParameterType.GetOrPost);
        }
    }

    /// <summary>
    /// The OAuth 2 authenticator using the authorization request header field.
    /// </summary>
    /// <remarks>
    /// Based on http://tools.ietf.org/html/draft-ietf-oauth-v2-10#section-5.1.1
    /// </remarks>
    public class OAuth2AuthorizationRequestHeaderAuthenticator : OAuth2Authenticator
    {
        private readonly string _tokenType;
        private bool _authFailed;

        /// <summary>
        /// Initializes a new instance of the <see cref="OAuth2AuthorizationRequestHeaderAuthenticator"/> class.
        /// </summary>
        /// <param name="client">The OAuth2 client</param>
        public OAuth2AuthorizationRequestHeaderAuthenticator(OAuth2.OAuth2Client client)
            : this(client, (string.IsNullOrEmpty(client.TokenType) ? "OAuth" : client.TokenType)) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="OAuth2AuthorizationRequestHeaderAuthenticator"/> class.
        /// </summary>
        /// <param name="client">The OAuth2 client</param>
        /// <param name="tokenType">
        /// The token type.
        /// </param>
        public OAuth2AuthorizationRequestHeaderAuthenticator(OAuth2.OAuth2Client client, string tokenType)
            : base(client)
        {
            _tokenType = tokenType;
        }

        /// <summary>
        /// Will be called when the authentication failed
        /// </summary>
        /// <param name="client">Client executing this request</param>
        /// <param name="request">Request to authenticate</param>
        /// <param name="response">Response of the failed request</param>
        /// <returns>Task where the handler for a failed authentication gets executed</returns>
        public override async Task HandleChallenge(IRestClient client, IRestRequest request, HttpResponseMessage response)
        {
            if (string.IsNullOrEmpty(Client.RefreshToken))
                return;

            // Set this variable only if we have a refresh token
            _authFailed = true;
            await Client.GetCurrentToken(forceUpdate: true);
        }

        /// <summary>
        /// Modifies the request to ensure that the authentication requirements are met.
        /// </summary>
        /// <param name="client">Client executing this request</param>
        /// <param name="request">Request to authenticate</param>
        /// <returns></returns>
        public override async Task PreAuthenticate(IRestClient client, IRestRequest request)
        {
            // Only add the Authorization parameter if it hasn't been added and the authorization didn't fail previously
            var authParam = request.Parameters.LastOrDefault(p => p.Type == ParameterType.HttpHeader && p.Name.Equals("Authorization", StringComparison.OrdinalIgnoreCase));
            if (!_authFailed && authParam != null)
                return;

            // When the authorization failed or when the Authorization header is missing, we're just adding it (again) with the
            // new AccessToken.
            _authFailed = false;
            var authValue = string.Format("{0} {1}", _tokenType, await Client.GetCurrentToken());
            if (authParam == null)
            {
                request.AddParameter("Authorization", authValue, ParameterType.HttpHeader);
            }
            else
            {
                authParam.Value = authValue;
            }
        }
    }
}
