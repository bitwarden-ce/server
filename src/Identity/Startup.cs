using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Bit.Core;
using Bit.Core.Utilities;
using AspNetCoreRateLimit;
using System.Globalization;

namespace Bit.Identity
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
            // Options
            services.AddOptions();

            // Settings
            var globalSettings = services.AddGlobalSettingsServices(Configuration);

            // Data Protection
            services.AddCustomDataProtectionServices(Environment, globalSettings);

            // Repositories
            services.AddSqlServerRepositories(globalSettings);

            // Context
            services.AddScoped<CurrentContext>();

            // Caching
            services.AddMemoryCache();

            // IdentityServer
            services.AddCustomIdentityServerServices(Environment, globalSettings);

            // Identity
            services.AddCustomIdentityServices(globalSettings);

            // Services
            services.AddBaseServices();
            services.AddDefaultServices(globalSettings);

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
            app.UseSerilog(env, appLifetime, globalSettings);

            // Default Middleware
            app.UseDefaultMiddleware(env, globalSettings);

            app.UseForwardedHeaders(globalSettings);

            // Add current context
            app.UseMiddleware<CurrentContextMiddleware>();

            // Add IdentityServer to the request pipeline.
            app.UseIdentityServer();
        }
    }
}
