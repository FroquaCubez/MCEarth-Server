using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Asp.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Transactions;
using Microsoft.AspNetCore.ResponseCompression;
using ProjectEarthServerAPI.Util;
using Microsoft.AspNetCore.Authentication;
using ProjectEarthServerAPI.Authentication;
using Serilog;
using Serilog.Events;
using ProjectEarthServerAPI.Controllers;
using Microsoft.Extensions.FileProviders;
using System.IO;
using System;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.StaticFiles;

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

			if (StateSingleton.Instance.config.webPanel == true)
			{
				services.AddSession(options =>
				{
					options.IdleTimeout = TimeSpan.FromDays(30);
					options.Cookie.HttpOnly = true;
					options.Cookie.IsEssential = true;
				});

				services.AddAuthorization(options =>
				{
					options.AddPolicy("LoggedIn", policy =>
					{
						policy.RequireAuthenticatedUser();
						policy.RequireClaim("Session", "loggedIn");
					});
				});
			}

			services.AddControllersWithViews();

			services.AddHttpContextAccessor();

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

			services.AddHttpClient();

			services.AddAuthentication("GenoaAuth")
				.AddScheme<AuthenticationSchemeOptions, GenoaAuthenticationHandler>("GenoaAuth", null);
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			app.UseSerilogRequestLogging(options =>
			{
				// Customize the message template
				options.MessageTemplate = "{RemoteIpAddress} {RequestMethod} {RequestScheme}://{RequestHost}{RequestPath}{RequestQuery} responded {StatusCode} in {Elapsed:0.0000} ms";

				// Emit debug-level events instead of the defaults
				options.GetLevel = (httpContext, elapsed, ex) => LogEventLevel.Verbose;

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

			app.UseSession();

			app.UseRouting();

			app.UseAuthentication();
			app.UseAuthorization();

			if (StateSingleton.Instance.config.webPanel == true)
			{
				app.UseStaticFiles();

				// For Avif images
				var provider = new FileExtensionContentTypeProvider();
				provider.Mappings[".avif"] = "image/avif";
				app.UseStaticFiles(new StaticFileOptions
				{
					FileProvider = new PhysicalFileProvider(
						Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "backgrounds")),
					RequestPath = "/images/backgrounds",
					ContentTypeProvider = provider
				});

				app.UseEndpoints(endpoints =>
				{
					endpoints.MapControllerRoute(
						name: "default",
						pattern: "{controller=Home}/{action=Index}/{id?}");
				});
			}


			app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TransactionManager.MaximumTimeout });

			app.UseResponseCaching();


			if (!env.IsDevelopment())
			{
				app.UseResponseCompression();
			}

		}

	}
}
