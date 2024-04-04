using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ProjectEarthServerAPI.Util;
using Microsoft.AspNetCore.Authorization;
using Asp.Versioning;

namespace ProjectEarthServerAPI.Controllers
{
	[Authorize]
	[ApiVersion("1.1")]
	[Route("1/api/v{version:apiVersion}/locations/{latitude}/{longitude}")]
	public class LocationController : Controller
	{
		public ContentResult Get(double latitude, double longitude)
		{
			//Create our response
			var resp = TappableUpdates.GetActiveLocations(latitude, longitude);

			//Send
			return Content(JsonConvert.SerializeObject(resp), "application/json");
		}
	}
}
