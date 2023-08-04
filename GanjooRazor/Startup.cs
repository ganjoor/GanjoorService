using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

            services.AddSingleton(
                   HtmlEncoder.Create(allowedRanges: new[] { UnicodeRanges.BasicLatin,
                    UnicodeRanges.Arabic }));

            services.AddRazorPages(options =>
            {
                options.Conventions.AddPageRoute("/index", "{*url}");
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


        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        { 

            app.UseCors("GanjoorCorsPolicy");

            app.UseExceptionHandler("/Error");

            app.UseStatusCodePagesWithReExecute("/errors/{0}");

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();



            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }
    }
}
