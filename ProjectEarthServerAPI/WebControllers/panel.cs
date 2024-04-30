using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using ProjectEarthServerAPI.Util;

namespace ProjectEarthServerAPI.Views
{
	public class PanelController : Controller
	{
		private const string SessionKey = "LoggedIn";

		[Route("/panel/login")]
		public IActionResult Login()
		{
			return View();
		}

		[HttpPost]
		[Route("/panel/login")]
		public IActionResult Login(string password)
		{
			if (password == StateSingleton.Instance.config.webPanelPassword)
			{
				// Correct password, set session flag
				HttpContext.Session.SetString(SessionKey, "true");

				// Redirect to the panel
				return RedirectToAction("Index");
			}
			else
			{
				// Incorrect password, show error message
				ViewBag.Error = "Incorrect password. Please try again.";
				return View();
			}
		}

		public IActionResult Index()
		{
			// Check if the session flag is present
			if (HttpContext.Session.GetString(SessionKey) == "true")
			{
				// User has successfully logged in, show the panel
				// Get data for the view
				ViewBag.TappableCount = StateSingleton.Instance.activeTappables.Count;
				ViewBag.MaxTappableSpawnAmount = StateSingleton.Instance.config.maxTappableSpawnAmount;
				ViewBag.BiomeGeneration = StateSingleton.Instance.config.biomeGeneration;
				ViewBag.SpawnRadius = StateSingleton.Instance.config.tappableSpawnRadius;
				ViewBag.TappableExpirationTime = StateSingleton.Instance.config.tappableExpirationTime;
				ViewBag.AdventuresCount = AdventureUtils.ReadEncounterLocations().Count;
				ViewBag.PublicAdventuresLimit = StateSingleton.Instance.config.publicAdventuresLimit;
				ViewBag.AdventureSpawnPercentage = StateSingleton.Instance.config.publicAdventureSpawnPercentage;
				ViewBag.MaxTappablesPerTile = StateSingleton.Instance.config.maxTappablesPerTile;
				ViewBag.PerRequestMaxTappableSpawnsInTile = StateSingleton.Instance.config.perRequestMaxTappableSpawnsInTile;
				return View();
			}
			else
			{
				// User has not logged in or session is not valid, redirect to the login page
				return RedirectToAction("Login");
			}
		}
	}
}
