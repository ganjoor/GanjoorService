using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using RMuseum.DbContext;
using RMuseum.Models.Auth.Memory;
using RMuseum.Services;
using RMuseum.Services.Implementation;
using RMuseum.Services.Implementationa;
using RSecurityBackend.Authorization;
using RSecurityBackend.DbContext;
using RSecurityBackend.Models.Auth.Db;
using RSecurityBackend.Models.Auth.Memory;
using RSecurityBackend.Models.Mail;
using RSecurityBackend.Services;
using RSecurityBackend.Services.Implementation;
using RSecurityBackend.Utilities;
using System;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Threading.Tasks;

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


            services.AddSingleton(
                    HtmlEncoder.Create(allowedRanges: new[] { UnicodeRanges.BasicLatin,
                    UnicodeRanges.Arabic }));

            services.AddRazorPages(options =>
            {
                options.Conventions.AddPageRoute("/index", "{*url}");
            });

            services.AddDbContext<RMuseumDbContext>();

            Audit.Core.Configuration.JsonSettings.ContractResolver = AuditNetEnvironmentSkippingContractResolver.Instance;
            Audit.Core.Configuration.DataProvider = new RAuditDataProvider(Configuration.GetConnectionString("DefaultConnection"));

            services.AddIdentityCore<RAppUser>(
                options =>
                {
                    // Password settings.
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequiredLength = 6;
                    options.Password.RequiredUniqueChars = 1;

                    // Lockout settings.
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.AllowedForNewUsers = true;

                    // User settings.
                    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
                    options.User.RequireUniqueEmail = false;
                }
                ).AddErrorDescriber<PersianIdentityErrorDescriber>();


            new IdentityBuilder(typeof(RAppUser), typeof(RAppRole), services)
                .AddRoleManager<RoleManager<RAppRole>>()
                .AddSignInManager<SignInManager<RAppUser>>()
                .AddEntityFrameworkStores<RMuseumDbContext>()
                .AddErrorDescriber<PersianIdentityErrorDescriber>();

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = "bearer";
            }).AddJwtBearer("bearer", options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidAudience = "Everyone",
                    ValidateIssuer = true,
                    ValidIssuer = "Ganjoor",

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes($"{Configuration.GetSection("Security")["Secret"]}")),

                    ValidateLifetime = true, //validate the expiration and not before values in the token

                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            context.Response.Headers.Add("Token-Expired", "true");
                        }
                        return Task.CompletedTask;
                    }
                };

            });

            services.AddAuthorization(options =>
            {
                //this is the default policy to make sure the use session has not yet been deleted by him/her from another client
                //or by an admin (Authorize with no policy should fail on deleted sessions)
                var defPolicy = new AuthorizationPolicyBuilder();
                defPolicy.Requirements.Add(new UserGroupPermissionRequirement("null", "null"));
                options.DefaultPolicy = defPolicy.Build();


                foreach (SecurableItem Item in RMuseumSecurableItem.Items)
                {
                    foreach (SecurableItemOperation Operation in Item.Operations)
                    {
                        options.AddPolicy($"{Item.ShortName}:{Operation.ShortName}", policy => policy.Requirements.Add(new UserGroupPermissionRequirement(Item.ShortName, Operation.ShortName)));
                    }
                }
            });

            services.AddMemoryCache();

           

            //security context maps to main db context
            services.AddTransient<RSecurityDbContext<RAppUser, RAppRole, Guid>, RMuseumDbContext>();

            //captcha service
            services.AddTransient<ICaptchaService, CaptchaServiceEF>();


            //generic image file service
            services.AddTransient<IImageFileService, ImageFileServiceEF>();

            //app user services
            services.AddTransient<IAppUserService, GanjoorAppUserService>();

            //user groups services
            services.AddTransient<IUserRoleService, RoleService>();

            //audit service
            services.AddTransient<IAuditLogService, AuditLogServiceEF>();

            //user permission checker
            services.AddTransient<IUserPermissionChecker, UserPermissionChecker>();

            //secret generator
            services.AddTransient<ISecretGenerator, SecretGenerator>();

            // email service
            services.AddTransient<IEmailSender, MailKitEmailSender>();
            services.Configure<SmptConfig>(Configuration);

            //picture file service
            services.AddTransient<IPictureFileService, PictureFileService>();

            //messaging service
            services.AddTransient<IRNotificationService, RNotificationService>();

            //artifact service
            services.AddTransient<IArtifactService, ArtifactService>();

            //audio service
            services.AddTransient<IRecitationService, RecitationService>();

            //ganjoor service
            services.AddTransient<IGanjoorService, GanjoorService>();

            //music catalogue service
            services.AddTransient<IMusicCatalogueService, MusicCatalogueService>();

            //long running job service
            services.AddTransient<ILongRunningJobProgressService, LongRunningJobProgressServiceEF>();

            //upload limit for IIS
            services.Configure<IISServerOptions>(options =>
            {
                options.MaxRequestBodySize = int.Parse(Configuration.GetSection("IIS")["UploadLimit"]);
            });


            services.AddHostedService<QueuedHostedService>();
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

            

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseExceptionHandler("/Error");

            app.UseStatusCodePagesWithReExecute("/errors/{0}");

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }
    }
}
