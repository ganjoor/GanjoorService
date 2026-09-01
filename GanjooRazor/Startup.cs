using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace GanjooRazor
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpClient();

            services.AddMemoryCache();

            services.AddScoped<GanjooRazor.Utils.PoetCacheService>();

            services.AddSingleton(
                   HtmlEncoder.Create(allowedRanges: new[] { UnicodeRanges.BasicLatin,
                    UnicodeRanges.Arabic }));

            services.AddRazorPages(options =>
            {
                options.Conventions.AddPageRoute("/GanjoorPage", "{*url}");

                // Pages/ reorganization: these pages moved into subfolders (Auth/, SongRecommendation/,
                // ImageRecommendation/, Recitations/, CommentReports/, Misc/) for readability, but each
                // one used a bare `@page` (no explicit route), so without these overrides their public
                // URL would change from the flat form (e.g. "/Login") to the new nested form
                // (e.g. "/Auth/Login"). None of these pages are referenced via asp-page/RedirectToPage
                // anywhere in the app (checked), only via plain hrefs and JS-embedded URLs, so
                // preserving the URL here is sufficient - no other code needed to change.
                options.Conventions.AddPageRoute("/Auth/Login", "/Login");
                options.Conventions.AddPageRoute("/Auth/SignUp", "/SignUp");
                options.Conventions.AddPageRoute("/Auth/ResetPassword", "/ResetPassword");

                options.Conventions.AddPageRoute("/SongRecommendation/Bp", "/Bp");
                options.Conventions.AddPageRoute("/SongRecommendation/Golha", "/Golha");
                options.Conventions.AddPageRoute("/SongRecommendation/Spotify", "/Spotify");

                options.Conventions.AddPageRoute("/ImageRecommendation/Pin", "/Pin");

                options.Conventions.AddPageRoute("/Recitations/AudioClip", "/AudioClip");
                options.Conventions.AddPageRoute("/Recitations/RecitationsOrder", "/RecitationsOrder");
                options.Conventions.AddPageRoute("/Recitations/ReportRecitation", "/ReportRecitation");

                options.Conventions.AddPageRoute("/CommentReports/ReportComment", "/ReportComment");

                options.Conventions.AddPageRoute("/Misc/Photos", "/Photos");
                options.Conventions.AddPageRoute("/Misc/t6e", "/t6e");
            });

            services.AddCors(options =>
            {
                options.AddPolicy(name: "GanjoorCorsPolicy",
                                  policy =>
                                  {
                                      policy.WithOrigins("https://museum.ganjoor.net",
                                                          "https://naskban.ir",
                                                          "http://localhost:5173"
                                                          );
                                  });
            });

            services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(Configuration["DataProtectionPersistPath"]))
                .SetApplicationName("GanjooRazor");

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        { 

            app.UseCors("GanjoorCorsPolicy");

            app.UseExceptionHandler("/Error");

            app.UseStatusCodePagesWithReExecute("/errors/{0}");

            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = ctx =>
                {
                    var path = ctx.Context.Request.Path.Value ?? "";
                    var isExplicitlyVersioned = ctx.Context.Request.Query.ContainsKey("version") ||
                                                 ctx.Context.Request.Query.ContainsKey("v");
                    // /lib and /dist are third-party vendor code (jQuery, Bootstrap, TinyMCE, diff.js,
                    // the jPlayer skin) that isn't hand-edited, so it's safe to treat the same as
                    // explicitly-versioned assets even without a query string.
                    var isVendorPath = path.StartsWith("/lib/", StringComparison.OrdinalIgnoreCase) ||
                                        path.StartsWith("/dist/", StringComparison.OrdinalIgnoreCase);

                    var headers = ctx.Context.Response.GetTypedHeaders();
                    headers.CacheControl = new CacheControlHeaderValue
                    {
                        Public = true,
                        // Versioned (?version=N / ?v=N) and vendor assets: the URL itself changes
                        // whenever the content does, so a year-long cache is safe and is what should
                        // eliminate most of the repeated p8.css/bk.js/user-panel.css/js downloads seen
                        // in the IIS logs.
                        // Everything else: no version query to rely on, so a conservative 6-hour cache
                        // still cuts a lot of redundant requests without risking long-lived staleness
                        // if someone edits chart.js/r2.js/etc. without remembering to bump a version.
                        MaxAge = (isExplicitlyVersioned || isVendorPath)
                            ? TimeSpan.FromDays(365)
                            : TimeSpan.FromHours(6)
                    };
                }
            });

            app.UseRouting();

            app.UseAuthorization();



            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }
    }
}
