using Microsoft.AspNetCore.Http;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace GanjooRazor.Utils
{
    /// <summary>
    /// DelegatingHandler that makes every request sent through an HttpClient constructed with it
    /// resilient to token expiration: whenever the API answers with 401 (Unauthorized), this handler
    /// transparently calls /api/users/relogin/{sessionId}, updates the session cookies with the fresh
    /// token, and retries the original request once with that new token - all without the calling code
    /// (any Razor Page handler making a secureClient.GetAsync/PostAsync/PutAsync/DeleteAsync/... call)
    /// having to know anything happened.
    ///
    /// This closes the gap left by <see cref="GanjoorSessionChecker.PrepareClient(HttpClient, HttpRequest, HttpResponse)"/>
    /// alone: PrepareClient only renews the token if the *pre-flight* checkmysession call fails with 401.
    /// If the token expires (or the session is otherwise invalidated) between that pre-flight check and
    /// one of the actual API calls that follow it - which is exactly the "every authorized method call
    /// might encounter a 401" bug - nothing used to retry that call. Attaching this handler to every
    /// secureClient makes the fix apply to all of them at once, since it intercepts SendAsync
    /// (the method GetAsync/PostAsync/PutAsync/DeleteAsync all funnel into) regardless of verb.
    /// </summary>
    public class GanjoorReloginHandler : DelegatingHandler
    {
        private readonly HttpRequest _request;
        private readonly HttpResponse _response;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="request">current HTTP request (used to read the SessionId/Token cookies)</param>
        /// <param name="response">current HTTP response (used to write refreshed cookies back)</param>
        public GanjoorReloginHandler(HttpRequest request, HttpResponse response)
            : base(new HttpClientHandler())
        {
            _request = request;
            _response = response;
        }

        /// <summary>
        /// sends the request; on a 401 response, relogins and retries the same request once
        /// </summary>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // keep a resend-able copy before the original message gets consumed/disposed by the first attempt
            HttpRequestMessage retryRequest = string.IsNullOrEmpty(_request.Cookies["SessionId"]) ? null : await CloneAsync(request);

            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized && retryRequest != null)
            {
                var newToken = await GanjoorSessionChecker.TryReloginAsync(_request, _response);
                if (!string.IsNullOrEmpty(newToken))
                {
                    retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
                    response.Dispose();
                    response = await base.SendAsync(retryRequest, cancellationToken);
                }
            }

            return response;
        }

        /// <summary>
        /// HttpRequestMessage (and its content stream) can only be sent once, so a full clone is needed
        /// up front in case a retry after relogin turns out to be necessary
        /// </summary>
        private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage request)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri)
            {
                Version = request.Version
            };

            if (request.Content != null)
            {
                // buffer the body once so both the original (first attempt) and the clone (retry, if
                // needed) each get their own independent, fully-rewound stream to send from
                var bodyBytes = await request.Content.ReadAsByteArrayAsync();
                var contentHeaders = request.Content.Headers;

                var originalContent = new ByteArrayContent(bodyBytes);
                foreach (var header in contentHeaders)
                {
                    originalContent.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
                request.Content = originalContent;

                var cloneContent = new ByteArrayContent(bodyBytes);
                foreach (var header in contentHeaders)
                {
                    cloneContent.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
                clone.Content = cloneContent;
            }

            foreach (var header in request.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            foreach (var option in request.Options)
            {
                clone.Options.Set(new HttpRequestOptionsKey<object>(option.Key), option.Value);
            }

            return clone;
        }
    }
}
