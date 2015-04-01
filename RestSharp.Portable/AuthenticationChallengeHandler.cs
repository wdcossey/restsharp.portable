﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using RestSharp.Portable.Authenticators;

namespace RestSharp.Portable
{
    /// <summary>
    /// The authentication manager that bundles authentication mechanisms that may be requested by the Www-Authenticate or Proxy-Authenticate headers.
    /// </summary>
    public class AuthenticationChallengeHandler : AsyncAuthenticator
    {
        private readonly Dictionary<string, AuthenticatorInfo> _authenticators = new Dictionary<string, AuthenticatorInfo>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationChallengeHandler" /> class.
        /// </summary>
        /// <param name="authHeader">The HTTP header to look for the challenge.</param>
        public AuthenticationChallengeHandler(AuthHeader authHeader)
        {
            Header = authHeader;
        }

        /// <summary>
        /// Gets the current list auf registered authenticators.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "There's no better (easier) solution")]
        public IEnumerable<Tuple<string, IAuthenticator>> Authenticators
        {
            get { return _authenticators.Select(x => Tuple.Create(x.Key, x.Value.Authenticator)); }
        }

        /// <summary>
        /// Gets a value that specifies the HTTP header to look for when trying to find the authentication mechanism.
        /// </summary>
        public AuthHeader Header { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the authentication module supports pre-authentication.
        /// </summary>
        public override bool CanPreAuthenticate
        {
            get { return _authenticators.Values.Any(x => x.Authenticator.CanPreAuthenticate); }
        }

        /// <summary>
        /// Registers a new authentication module
        /// </summary>
        /// <param name="method">The method ID used in the Www-Authenticate header.</param>
        /// <param name="authenticator">The authenticator to assign with the method ID.</param>
        /// <param name="securityLevel">Authenticators with higher security levels are preferred.</param>
        [SuppressMessage(
            "Microsoft.Design",
            "CA1011:ConsiderPassingBaseTypesAsParameters",
            Justification = "We must not use the base type, because we only support ISyncAuthenticator and IAsyncAuthenticator")]
        public void Register(string method, ISyncAuthenticator authenticator, int securityLevel)
        {
            _authenticators[method] = new AuthenticatorInfo(securityLevel, authenticator);
        }

        /// <summary>
        /// Registers a new authentication module
        /// </summary>
        /// <param name="method">The method ID used in the Www-Authenticate header.</param>
        /// <param name="authenticator">The authenticator to assign with the method ID.</param>
        /// <param name="securityLevel">Authenticators with higher security levels are preferred.</param>
        [SuppressMessage(
            "Microsoft.Design",
            "CA1011:ConsiderPassingBaseTypesAsParameters",
            Justification = "We must not use the base type, because we only support ISyncAuthenticator and IAsyncAuthenticator")]
        public void Register(string method, IAsyncAuthenticator authenticator, int securityLevel)
        {
            _authenticators[method] = new AuthenticatorInfo(securityLevel, authenticator);
        }

        /// <summary>
        /// Removes an authentication module with the given method ID.
        /// </summary>
        /// <param name="method">The method ID used in the Www-Authenticate header.</param>
        public void Unregister(string method)
        {
            _authenticators.Remove(method);
        }

        /// <summary>
        /// Determines if the authentication module can handle the challenge sent with the response.
        /// </summary>
        /// <param name="client">The REST client the response is assigned to</param>
        /// <param name="request">The REST request the response is assigned to</param>
        /// <param name="response">The response that returned the authentication challenge</param>
        /// <returns>true when the authenticator can handle the sent challenge</returns>
        public override bool CanHandleChallenge(IRestClient client, IRestRequest request, HttpResponseMessage response)
        {
            return response
                .GetAuthenticationHeaderInfo(Header)
                .Where(x => _authenticators.ContainsKey(x.Name))
                .Select(x => _authenticators[x.Name])
                .Where(x => x.Authenticator.CanHandleChallenge(client, request, response))
                .OrderByDescending(x => x.Security)
                .Select(x => x.Authenticator)
                .Any();
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
            var authenticator = response
                .GetAuthenticationHeaderInfo(Header)
                .Where(x => _authenticators.ContainsKey(x.Name))
                .Select(x => _authenticators[x.Name])
                .Where(x => x.Authenticator.CanHandleChallenge(client, request, response))
                .OrderByDescending(x => x.Security)
                .Select(x => x.Authenticator)
                .First();
            var asyncAuth = authenticator as IAsyncAuthenticator;
            if (asyncAuth != null)
            {
                await asyncAuth.HandleChallenge(client, request, response);
            }
            else
            {
                var syncAuth = (ISyncAuthenticator)authenticator;
                syncAuth.HandleChallenge(client, request, response);
            }
        }

        /// <summary>
        /// Modifies the request to ensure that the authentication requirements are met.
        /// </summary>
        /// <param name="client">Client executing this request</param>
        /// <param name="request">Request to authenticate</param>
        /// <returns>The task the authentication is performed on</returns>
        public override async Task PreAuthenticate(IRestClient client, IRestRequest request)
        {
            foreach (var authenticator in _authenticators.Values.Select(x => x.Authenticator).Where(x => x.CanPreAuthenticate))
            {
                var asyncAuth = authenticator as IAsyncAuthenticator;
                if (asyncAuth != null)
                {
                    await asyncAuth.PreAuthenticate(client, request);
                }
                else
                {
                    var syncAuth = (ISyncAuthenticator)authenticator;
                    syncAuth.PreAuthenticate(client, request);
                }
            }
        }

        private class AuthenticatorInfo
        {
            public AuthenticatorInfo(int security, IAuthenticator authenticator)
            {
                Security = security;
                Authenticator = authenticator;
            }

            public int Security { get; private set; }

            public IAuthenticator Authenticator { get; private set; }
        }
    }
}