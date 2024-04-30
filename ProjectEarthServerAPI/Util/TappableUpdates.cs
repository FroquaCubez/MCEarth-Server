using System;
using System.Collections.Generic;
using System.Linq;
using ProjectEarthServerAPI.Models;
using Serilog;

namespace ProjectEarthServerAPI.Util
{
    public class TappableUpdates
    {
        private static readonly Random random = new Random();

        public static void RemoveExpiredTappables()
        {
            var currentTime = DateTime.UtcNow;
            var tappablesToRemove = StateSingleton.Instance.activeTappables
                .Where(kvp => kvp.Value.location.expirationTime <= currentTime)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var tappableId in tappablesToRemove)
            {
                StateSingleton.Instance.activeTappables.Remove(tappableId);
            }

            Log.Debug($"Removed {tappablesToRemove.Count} expired tappables.");
        }

        public static LocationResponse.Root GetActiveLocations(double lat, double lon, int radius = 1)
        {
            RemoveExpiredTappables();

            if (lat == 0 && lon == 0)
            {
                var globalTappables = StateSingleton.Instance.activeTappables
                    .Select(kvp => kvp.Value.location)
                    .ToList();

                var globalEncounters = AdventureUtils.GetEncountersForLocation(lat, lon);
                globalTappables.AddRange(globalEncounters);

                return new LocationResponse.Root
                {
                    result = new LocationResponse.Result
                    {
                        killSwitchedTileIds = new List<object>(),
                        activeLocations = globalTappables,
                    },
                    expiration = null,
                    continuationToken = null,
                    updates = new Updates()
                };
            }
            else
            {
                radius = StateSingleton.Instance.config.tappableSpawnRadius;
                string tileId = Tile.GetTileForCoordinates(lat, lon);
                string[] parts = tileId.Split('_');

				if (parts.Length != 2 || !int.TryParse(parts[0], out int tileIdLat) || !int.TryParse(parts[1], out int tileIdLon))
                {
                    return null;
                }
                double[][] minTileCoordinates = Tile.GetCoordinatesForTile($"{tileIdLat - radius}_{tileIdLon - radius}");
                double[][] maxTileCoordinates = Tile.GetCoordinatesForTile($"{tileIdLat + radius}_{tileIdLon + radius}");

                var minCoordinates = new Coordinate { latitude = minTileCoordinates[0][0], longitude = minTileCoordinates[0][1] };
                var maxCoordinates = new Coordinate { latitude = maxTileCoordinates[1][0], longitude = maxTileCoordinates[1][1] };

                var tappables = StateSingleton.Instance.activeTappables
                    .Select(kvp => kvp.Value.location)
                    .ToList();


				// TAPPABLE GENERATION
				if (StateSingleton.Instance.config.maxTappableSpawnAmount > tappables.Count)
				{
					// For each tile
					for (int latLoop = tileIdLat - radius; latLoop <= tileIdLat + radius; latLoop++)
					{
						for (int lonLoop = tileIdLon - radius; lonLoop <= tileIdLon + radius; lonLoop++)
						{
							// In each tile
							string currentTileId = $"{latLoop}_{lonLoop}";

							var tappablesInCurrentTile = StateSingleton.Instance.activeTappables
							.Where(kvp => kvp.Value.location.tileId == currentTileId)
							.ToList();

							int spawneableTappablesInTile = StateSingleton.Instance.config.maxTappablesPerTile - tappablesInCurrentTile.Count;
							int perRequestMaxTappableSpawnsInTile = StateSingleton.Instance.config.perRequestMaxTappableSpawnsInTile;
							spawneableTappablesInTile = Math.Min(spawneableTappablesInTile, perRequestMaxTappableSpawnsInTile);


							if (spawneableTappablesInTile > 0)
							{
								// For each tappable we can spawn run this
								for (int i = 0; i < spawneableTappablesInTile; i++)
								{

									double[][] loopTileCoordinates = Tile.GetCoordinatesForTile(currentTileId);

									var minLoopCoordinates = new Coordinate { latitude = loopTileCoordinates[0][0], longitude = loopTileCoordinates[0][1] };
									var maxLoopCoordinates = new Coordinate { latitude = loopTileCoordinates[1][0], longitude = loopTileCoordinates[1][1] };

									double tappableRandomLatitude = minLoopCoordinates.latitude + (random.NextDouble() * (maxLoopCoordinates.latitude - minLoopCoordinates.latitude));
									double tappableRandomLongitude = minLoopCoordinates.longitude + (random.NextDouble() * (maxLoopCoordinates.longitude - minLoopCoordinates.longitude));

									var newTappable = TappableGeneration.CreateTappableInRadiusOfCoordinates(tappableRandomLatitude, tappableRandomLongitude);
									if (newTappable == null)
									{
										// This is done not to send a null tappable to client so it doesnt affect client :)
									}
									else
									{
										tappables.Add(newTappable);
									}
								}
							}
						}
					}
				}

                double randomLatitude = minCoordinates.latitude + (random.NextDouble() * (maxCoordinates.latitude - minCoordinates.latitude));
                double randomLongitude = minCoordinates.longitude + (random.NextDouble() * (maxCoordinates.longitude - minCoordinates.longitude));

                if (random.Next(1, 101) <= StateSingleton.Instance.config.publicAdventureSpawnPercentage && StateSingleton.Instance.config.publicAdventuresLimit > AdventureUtils.ReadEncounterLocations().Count)
                {
                    DateTime expirationTime = DateTime.UtcNow.AddMinutes(30);
                    AdventureUtils.CreateEncounterLocation(randomLatitude, randomLongitude, expirationTime);
                }

                var encounters = AdventureUtils.GetEncountersForLocation(lat, lon);
                tappables.AddRange(encounters.Where(pred =>
                    pred.coordinate.latitude >= minCoordinates.latitude &&
                    pred.coordinate.latitude <= maxCoordinates.latitude &&
                    pred.coordinate.longitude >= minCoordinates.longitude &&
                    pred.coordinate.longitude <= maxCoordinates.longitude));

                return new LocationResponse.Root
                {
                    result = new LocationResponse.Result
                    {
                        killSwitchedTileIds = [],
                        activeLocations = tappables,
                    },
                    expiration = null,
                    continuationToken = null,
                    updates = new Updates()
                };
            }
        }
    }
}
