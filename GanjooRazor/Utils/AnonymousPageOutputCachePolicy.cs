using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GanjooRazor.Utils
{
    /// <summary>
    /// Output-cache policy for the public content pages (GanjoorPage, Index, Search, Simi,
    /// Contribs, Hashieha, Quotes, FAQ).
    ///
    /// These pages bake per-visitor state into the rendered HTML: login status, the CanEdit /
    /// KeepHistory / CanTranslate cookies (which change the editor UI, admin links, and the
    /// tracking script), and comment/bookmark "mine" flags. None of that is safe to share between
    /// visitors, so this policy only allows a response to be served from (or written to) the
    /// output cache when the incoming request carries none of the cookies that drive that
    /// personalization - i.e. anonymous visitors and, importantly, search-engine crawlers and
    /// other bots, which make up a large share of the repeat hits on the same poem/poet/category
    /// URL and never send cookies at all.
    ///
    /// Any visitor with one of these cookies - logged-in readers, and especially editors - always
    /// gets a fully fresh, uncached render, so this never makes the site show stale content to a
    /// logged-on user.
    /// </summary>
    public sealed class AnonymousPageOutputCachePolicy : IOutputCachePolicy
    {
        public static readonly AnonymousPageOutputCachePolicy Instance = new();

        private static readonly string[] PersonalizationCookies =
        {
            "Token",        // logged in
            "UserId",       // logged in
            "CanEdit",      // editor UI / admin links
            "KeepHistory",  // "you last visited this on..." text
            "CanTranslate", // translation UI
        };

        private AnonymousPageOutputCachePolicy() { }

        ValueTask IOutputCachePolicy.CacheRequestAsync(OutputCacheContext context, CancellationToken cancellationToken)
        {
            var request = context.HttpContext.Request;

            bool isPersonalized =
                !HttpMethods.IsGet(request.Method) ||
                PersonalizationCookies.Any(request.Cookies.ContainsKey);

            context.EnableOutputCaching = true;
            context.AllowCacheLookup = !isPersonalized;
            context.AllowCacheStorage = !isPersonalized;
            context.Tags.Add("ganjoor-public-page");

            // Short TTL: a poem/poet/category edit needs to reach anonymous visitors within
            // minutes, not be pinned for the site's whole lifetime. Tune per page type later if
            // needed (e.g. shorter for recently-active pages, longer for old archived poets).
            context.ResponseExpirationTimeSpan = TimeSpan.FromMinutes(5);

            return ValueTask.CompletedTask;
        }

        ValueTask IOutputCachePolicy.ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;

        /// <summary>
        /// Runs after the response has actually been generated, right before the middleware would
        /// store it. Registering this policy directly (via
        /// <c>options.AddPolicy(name, IOutputCachePolicy)</c> in Startup.cs) means it's used
        /// standalone instead of chained onto ASP.NET Core's built-in DefaultPolicy - so none of
        /// DefaultPolicy's own storage safety checks run unless reproduced here. Without this
        /// method, EVERY response was eligible for storage, including 302 redirects (GanjoorPage
        /// and Index both have legitimate redirect paths - legacy URL redirects, the "?p=" lookup,
        /// 404-to-redirecturl fallback) - one of those got cached and was then replayed to every
        /// anonymous visitor of that URL until the TTL expired. This restores the same three
        /// checks DefaultPolicy applies: only store 200 responses, never store a response that set
        /// a cookie, never store for an authenticated request.
        /// </summary>
        ValueTask IOutputCachePolicy.ServeResponseAsync(OutputCacheContext context, CancellationToken cancellationToken)
        {
            var response = context.HttpContext.Response;

            if (!StringValues.IsNullOrEmpty(response.Headers[HeaderNames.SetCookie]) ||
                context.HttpContext.User?.Identity?.IsAuthenticated == true ||
                response.StatusCode != StatusCodes.Status200OK)
            {
                context.AllowCacheStorage = false;
            }

            return ValueTask.CompletedTask;
        }
    }
}
