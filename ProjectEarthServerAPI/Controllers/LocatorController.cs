using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ProjectEarthServerAPI.Models;
using ProjectEarthServerAPI.Util;
using Serilog;
using System.Collections.Generic;

namespace ProjectEarthServerAPI.Controllers
{
	[ApiController]
	[ApiVersion("1.0")]
	[ApiVersion("1.1")]
	[Route("player/environment")]
	public class LocatorBaseController : ControllerBase
	{

		protected string GetBaseServerIP()
		{
			string protocol = Request.IsHttps ? "https://" : "http://";
			return StateSingleton.Instance.config.useBaseServerIP ? StateSingleton.Instance.config.baseServerIP : $"{protocol}{Request.Host.Value}";
		}
	}

	public class LocatorController : LocatorBaseController
	{

		[HttpGet]
		public ContentResult Get()
		{
			string baseServerIP = GetBaseServerIP();
			Log.Information($"{HttpContext.Connection.RemoteIpAddress} has issued locator, replying with {baseServerIP}");

			LocatorResponse.Root response = new LocatorResponse.Root()
			{
				result = new LocatorResponse.Result()
				{
					serviceEnvironments = new LocatorResponse.ServiceEnvironments()
					{
						production = new LocatorResponse.Production()
						{
							playfabTitleId = StateSingleton.Instance.config.playfabTitleId,
							serviceUri = baseServerIP,
							cdnUri = baseServerIP + "/cdn",
						}
					},
					supportedEnvironments = new Dictionary<string, List<string>>() { { "2020.1217.02", new List<string>() { "production" } }, { "2020.1210.01", new List<string>() { "production" } } }
				},
			};

			var resp = JsonConvert.SerializeObject(response);
			return Content(resp, "application/json");
		}
	}

	[ApiController]
	[ApiVersion("1.0")]
	[ApiVersion("1.1")]
	[Route("/api/v1.1/player/environment")]
	public class MojankLocatorController : LocatorBaseController
	{

		[HttpGet]
		public ContentResult Get()
		{
			string baseServerIP = GetBaseServerIP();
			Log.Information($"{HttpContext.Connection.RemoteIpAddress} has issued locator, replying with {baseServerIP}");

			LocatorResponse.Root response = new LocatorResponse.Root()
			{
				result = new LocatorResponse.Result()
				{
					serviceEnvironments = new LocatorResponse.ServiceEnvironments()
					{
						production = new LocatorResponse.Production()
						{
							playfabTitleId = StateSingleton.Instance.config.playfabTitleId,
							serviceUri = baseServerIP,
							cdnUri = baseServerIP + "/cdn",
						}
					},
					supportedEnvironments = new Dictionary<string, List<string>>() { { "2020.1217.02", new List<string>() { "production" } } }
				},
			};

			var resp = JsonConvert.SerializeObject(response);
			return Content(resp, "application/json");
		}
	}
}
