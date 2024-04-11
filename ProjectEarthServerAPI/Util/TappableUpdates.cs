using System;
using System.Collections.Generic;
using System.Linq;
using ProjectEarthServerAPI.Models;
using Serilog;

namespace ProjectEarthServerAPI.Util
{
	public class TappableUpdates
	{
		
		private static Random random = new Random();

		public static void RemoveExpiredTappables()
		{
			var currentTime = DateTime.UtcNow;
			var tappablesToRemove = new List<Guid>();

			foreach (var kvp in StateSingleton.Instance.activeTappables)
			{
				var tappable = kvp.Value.location;
				//Log.Debug($"Current time: {currentTime} - Expiration time: {tappable.expirationTime}");
				if (tappable.expirationTime <= currentTime)
				{
					tappablesToRemove.Add(kvp.Key);
				}
			}

			// Remove expired tappables from the dictionary
			foreach (var tappableId in tappablesToRemove)
			{
				StateSingleton.Instance.activeTappables.Remove(tappableId);
			}

			Log.Information($"Removed {tappablesToRemove.Count} expired tappables.");
		}

		public static LocationResponse.Root GetActiveLocations(double lat, double lon, double radius = -1.0)
		{

			RemoveExpiredTappables();
			if (radius == -1.0) radius = StateSingleton.Instance.config.tappableSpawnRadius;
			var maxCoordinates = new Coordinate { latitude = lat + radius, longitude = lon + radius };
			var minCoordinates = new Coordinate { latitude = lat - radius, longitude = lon - radius };

			var tappables = StateSingleton.Instance.activeTappables
				.Where(pred =>
					(pred.Value.location.coordinate.latitude >= minCoordinates.latitude && pred.Value.location.coordinate.latitude <= maxCoordinates.latitude)
					&& (pred.Value.location.coordinate.longitude >= minCoordinates.longitude && pred.Value.location.coordinate.longitude <= maxCoordinates.longitude))
				.ToDictionary(pred => pred.Key, pred => pred.Value.location).Values.ToList();

			if (tappables.Count <= StateSingleton.Instance.config.maxTappableSpawnAmount)
			{
				var count = StateSingleton.Instance.config.maxTappableSpawnAmount - tappables.Count;

				var newTappables = Enumerable.Range(0, count)
											  .Select(_ => TappableGeneration.CreateTappableInRadiusOfCoordinates(lat, lon, radius))
											  .ToList();

				tappables.AddRange(newTappables);
			}

			var encounters = AdventureUtils.GetEncountersForLocation(lat, lon);
			tappables.AddRange(encounters.Where(pred =>
					(pred.coordinate.latitude >= minCoordinates.latitude && pred.coordinate.latitude <= maxCoordinates.latitude)
					&& (pred.coordinate.longitude >= minCoordinates.longitude && pred.coordinate.longitude <= maxCoordinates.longitude)).ToList());

			return new LocationResponse.Root
			{
				result = new LocationResponse.Result
				{
					killSwitchedTileIds = new List<object> { }, //havent seen this used. Debugging thing maybe?
					activeLocations = tappables,
				},
				expiration = null,
				continuationToken = null,
				updates = new Updates()
			};
		}
	}
}
