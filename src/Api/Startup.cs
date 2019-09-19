﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Bit.Api.Utilities;
using Bit.Core;
using Bit.Core.Identity;
using Newtonsoft.Json.Serialization;
using AspNetCoreRateLimit;
using Stripe;
using Bit.Core.Utilities;
using IdentityModel;
using System.Globalization;
using Microsoft.IdentityModel.Logging;

namespace Bit.Api
{
    public class Startup
    {
        public Startup(IHostingEnvironment env, IConfiguration configuration)
        {
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
            Configuration = configuration;
            Environment = env;
        }

        public IConfiguration Configuration { get; private set; }
        public IHostingEnvironment Environment { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            var provider = services.BuildServiceProvider();

            // Options
            services.AddOptions();

            // Settings
            var globalSettings = services.AddGlobalSettingsServices(Configuration);

            // Data Protection
            services.AddCustomDataProtectionServices(Environment, globalSettings);

            // Stripe Billing
            StripeConfiguration.ApiKey = globalSettings.StripeApiKey;

            // Repositories
            services.AddSqlServerRepositories(globalSettings);

            // Context
            services.AddScoped<CurrentContext>();

            // Caching
            services.AddMemoryCache();

            // Identity
            services.AddCustomIdentityServices(globalSettings);
            services.AddIdentityAuthenticationServices(globalSettings, Environment, config =>
            {
                config.AddPolicy("Application", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim(JwtClaimTypes.AuthenticationMethod, "Application");
                    policy.RequireClaim(JwtClaimTypes.Scope, "api");
                });
                config.AddPolicy("Web", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim(JwtClaimTypes.AuthenticationMethod, "Application");
                    policy.RequireClaim(JwtClaimTypes.Scope, "api");
                    policy.RequireClaim(JwtClaimTypes.ClientId, "web");
                });
                config.AddPolicy("Push", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim(JwtClaimTypes.Scope, "api.push");
                });
                config.AddPolicy("Licensing", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim(JwtClaimTypes.Scope, "api.licensing");
                });
                config.AddPolicy("Organization", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim(JwtClaimTypes.Scope, "api.organization");
                });
            });

            services.AddScoped<AuthenticatorTokenProvider>();

            // Services
            services.AddBaseServices();
            services.AddDefaultServices(globalSettings);

            // MVC
            services.AddMvc(config =>
            {
                config.Conventions.Add(new ApiExplorerGroupConvention());
                config.Conventions.Add(new PublicApiControllersModelConvention());
            }).AddJsonOptions(options =>
            {
                if(Environment.IsProduction() && Configuration["swaggerGen"] != "true")
                {
                    options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                }
            });

            services.AddSwagger(globalSettings);

            // Jobs service
            Jobs.JobsHostedService.AddJobsServices(services);
            services.AddHostedService<Jobs.JobsHostedService>();
            if(CoreHelpers.SettingHasValue(globalSettings.ServiceBus.ConnectionString) &&
                CoreHelpers.SettingHasValue(globalSettings.ServiceBus.ApplicationCacheTopicName))
            {
                services.AddHostedService<Core.HostedServices.ApplicationCacheHostedService>();
            }
        }

        public void Configure(
            IApplicationBuilder app,
            IHostingEnvironment env,
            IApplicationLifetime appLifetime,
            GlobalSettings globalSettings)
        {
            IdentityModelEventSource.ShowPII = true;
            app.UseSerilog(env, appLifetime, globalSettings);

            // Default Middleware
            app.UseDefaultMiddleware(env, globalSettings);

            app.UseForwardedHeaders(globalSettings);

            // Add static files to the request pipeline.
            app.UseStaticFiles();

            // Add Cors
            app.UseCors(policy => policy.SetIsOriginAllowed(h => true)
                .AllowAnyMethod().AllowAnyHeader().AllowCredentials());

            // Add authentication to the request pipeline.
            app.UseAuthentication();

            // Add current context
            app.UseMiddleware<CurrentContextMiddleware>();

            // Add MVC to the request pipeline.
            app.UseMvc();

            // Add Swagger
            app.UseSwagger(config =>
            {
                config.RouteTemplate = "specs/{documentName}/swagger.json";
                var host = globalSettings.BaseServiceUri.Api.Replace("https://", string.Empty)
                    .Replace("http://", string.Empty);
                config.PreSerializeFilters.Add((swaggerDoc, httpReq) => swaggerDoc.Host = host);
            });
            app.UseSwaggerUI(config =>
            {
                config.DocumentTitle = "Bitwarden API Documentation";
                config.RoutePrefix = "docs";
                config.SwaggerEndpoint($"{globalSettings.BaseServiceUri.Api}/specs/public/swagger.json",
                    "Bitwarden Public API");
                config.OAuthClientId("accountType.id");
                config.OAuthClientSecret("secretKey");
            });
        }
    }
}
