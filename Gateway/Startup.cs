using Gateway.DelegatingHandlers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Gateway
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAccessTokenManagement();

            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            var AuthenticationScheme = Configuration.GetValue<string>("AuthenticationScheme");
            var Authority = Configuration.GetValue<string>("Authority");
            var Audience = Configuration.GetValue<string>("Audience");

            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    builder => builder.WithOrigins(Configuration.GetValue<string>("DriversAppUrl"))
                        .AllowCredentials()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
            });

            // Disable certificates checking because we dont support them 
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                   .AddJwtBearer(AuthenticationScheme, options =>
                   {
                       options.Authority = Authority;
                       options.Audience = Audience;
                       options.BackchannelHttpHandler = new HttpClientHandler
                       { ServerCertificateCustomValidationCallback = delegate { return true; } };
                   });

            // Disable certificates checking because we dont support them 
            services.AddHttpClient("client")
                .ConfigurePrimaryHttpMessageHandler((context) => new HttpClientHandler
                { ServerCertificateCustomValidationCallback = delegate { return true; } });

            services.AddScoped<TokenExchangeDelegatingHandler>();

            services.AddOcelot()
                .AddDelegatingHandler<TokenExchangeDelegatingHandler>();
        }
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public async Task ConfigureAsync(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseCors("CorsPolicy");
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            await app.UseOcelot();
        }
    }
}
