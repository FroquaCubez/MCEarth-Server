using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using ProjectEarthServerAPI.Util;
using Microsoft.AspNetCore.Authentication;
using ProjectEarthServerAPI.Authentication;
using Serilog;
using Serilog.Events;

namespace ProjectEarthServerAPI
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
			services.AddControllers();

			services.AddResponseCompression(options =>
			{
				options.Providers.Add<GzipCompressionProvider>();
			});

			services.AddResponseCaching();

			services.AddApiVersioning(config =>
			{
				config.DefaultApiVersion = new ApiVersion(1, 1);
				config.AssumeDefaultVersionWhenUnspecified = true;
				config.ReportApiVersions = true;
			});

			services.AddAuthentication("GenoaAuth")
				.AddScheme<AuthenticationSchemeOptions, GenoaAuthenticationHandler>("GenoaAuth", null);
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				//app.UseDeveloperExceptionPage();
			}

			app.UseSerilogRequestLogging(options =>
			{
				// Customize the message template
				options.MessageTemplate = "{RemoteIpAddress} {RequestMethod} {RequestScheme}://{RequestHost}{RequestPath}{RequestQuery} responded {StatusCode} in {Elapsed:0.0000} ms";

				// Emit debug-level events instead of the defaults
				options.GetLevel = (httpContext, elapsed, ex) => LogEventLevel.Debug;

				// Attach additional properties to the request completion event
				options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
				{
					diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
					diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
					diagnosticContext.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress);
					diagnosticContext.Set("RequestQuery", httpContext.Request.QueryString);
				};
			});

			app.UseETagger();
			//app.UseHttpsRedirection();

			app.UseRouting();

			app.UseAuthentication();
			app.UseAuthorization();

            app.UseWebSockets(new WebSocketOptions{KeepAliveInterval = TransactionManager.MaximumTimeout});

			app.UseResponseCaching();

			app.UseResponseCompression();

			//app.UseSession();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllers();
			});
		}
	}
}
