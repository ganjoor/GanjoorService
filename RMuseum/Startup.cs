using Audit.WebApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
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
using Swashbuckle.AspNetCore.Filters;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RMuseum
{

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add service and create Policy with options
            services.AddCors(options =>
            {
                options.AddPolicy("GanjoorCorsPolicy",
                    builder => builder.SetIsOriginAllowed(_ => true)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .WithExposedHeaders("paging-headers", "audio-upload-enabled")
                    .AllowCredentials()
                    );
            });

            services.AddDbContextPool<RMuseumDbContext>(
                        options => options.UseSqlServer(
                            Configuration.GetConnectionString("DefaultConnection"),
                            providerOptions => {
                                providerOptions.EnableRetryOnFailure();
                                providerOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                                }
                            )
                        );

            //Audit.Core.Configuration.JsonSettings.ContractResolver = AuditNetEnvironmentSkippingContractResolver.Instance;
            Audit.Core.Configuration.DataProvider = new RAuditDataProvider(Configuration.GetConnectionString("DefaultConnection"));
            Audit.Core.Configuration.AuditDisabled = bool.Parse(Configuration["AuditNetEnabled"]) == false;

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


            services.AddMvc(mvc =>
                    mvc.AddAuditFilter(config => config
                    .LogRequestIf(r => r.Method != "GET")
                    .WithEventType("{controller}/{action} ({verb})")
                    .IncludeHeaders(ctx => !ctx.ModelState.IsValid)
                    .IncludeRequestBody()
                    .IncludeModelState()
                ));

            services.AddMemoryCache();

            services.AddHttpClient();

            services.Configure<FormOptions>(x =>
            {
                x.ValueLengthLimit = int.MaxValue;
                x.MultipartBodyLengthLimit = int.MaxValue; // In case of multipart
            });

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


            // Register the Swagger generator, defining 1 or more Swagger documents
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "RMuseum API",
                    Version = "v1",
                    Description = "RMuseum API",
                    TermsOfService = new Uri("https://ganjoor.net/contact"),
                    Contact = new OpenApiContact
                    {
                        Name = "Ganjoor",
                        Email = "ganjoor@ganjoor.net",
                        Url = new Uri("https://ganjoor.net")
                    }
                }
                                );
                c.EnableAnnotations();

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);

                c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "RSecurityBackend.xml"));

                c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme()
                {
                    Description = "format: \"bearer {token}\"",
                    In = ParameterLocation.Header,
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey

                });


                c.OperationFilter<SecurityRequirementsOperationFilter>();
                c.OperationFilter<AppendAuthorizeToSummaryOperationFilter>(); // Adds "(Auth)" to the summary so that you can see which endpoints have Authorization
                                                                              // or use the generic method, e.g. c.OperationFilter<AppendAuthorizeToSummaryOperationFilter<MyCustomAttribute>>();

            });

            //IHttpContextAccessor
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();


            //authorization handler
            services.AddScoped<IAuthorizationHandler, UserGroupPermissionHandler>();


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

            //generic options service
            services.AddTransient<IRGenericOptionsService, RGenericOptionsServiceEF>();

            //site banner service
            services.AddTransient<ISiteBannersService, SiteBannersService>();

            //donation service
            services.AddTransient<IDonationService, DonationService>();

            //translation service
            services.AddTransient<IGanjoorTranslationService, GanjoorTranslationService>();

            //numbering service
            services.AddTransient<IGanjoorNumberingService, GanjoorNumberingService>();

            //geo location service
            services.AddTransient<IGeoLocationService, GeoLocationService>();

            //tracking service
            services.AddTransient<IUserVisitsTrackingService, UserVisitsTrackingService>();

            //poet photo suggestion service
            services.AddTransient<IPoetPhotoSuggestionService, PoetPhotoSuggestionService>();

            //faq service
            services.AddTransient<IFAQService, FAQService>();

            //PDF library service
            services.AddTransient<IPDFLibraryService, PDFLibraryService>();

            //Queued FTP Upload Service
            services.AddTransient<IQueuedFTPUploadService, QueuedFTPUploadService>();

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
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }


            app.UseStaticFiles();

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), 
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "RMuseum API V1");
                c.RoutePrefix = string.Empty;
            });


            app.UseAuthentication();

            // global policy - assign here or on each controller
            app.UseCors("GanjoorCorsPolicy");

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });


        }
    }
}
