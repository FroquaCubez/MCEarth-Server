using Microsoft.AspNetCore.Mvc;
using ProjectEarthServerAPI.Util;
using System.Linq;

namespace ProjectEarthServerAPI.Panel
{
	public class PanelController : Controller
	{
		public IActionResult Index()
		{

			ViewBag.TappableCount = StateSingleton.Instance.activeTappables.Count;

			ViewBag.BiomeGeneration = StateSingleton.Instance.config.biomeGeneration;

			ViewBag.SpawnRadius = StateSingleton.Instance.config.tappableSpawnRadius;

			return View();
		}
	}
}
