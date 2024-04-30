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

			Log.Debug($"Removed {tappablesToRemove.Count} expired tappables.");
		}

		public static LocationResponse.Root GetActiveLocations(double lat, double lon, int radius = 1)
		{
			RemoveExpiredTappables();

			if (lat == 0 && lon == 0) // Verifica si latitud y longitud son 0 en lugar de null
			{
				var globaltappables = StateSingleton.Instance.activeTappables
					.ToDictionary(pred => pred.Key, pred => pred.Value.location)
					.Values.ToList();

				var globalencounters = AdventureUtils.GetEncountersForLocation(lat, lon);
				globaltappables.AddRange(globalencounters.ToList());

				return new LocationResponse.Root
				{
					result = new LocationResponse.Result
					{
						killSwitchedTileIds = new List<object>(), // No he visto esta lista utilizada, ¿es para depuración?
						activeLocations = globaltappables,
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
				double minTileLat = 0;
				double minTileLon = 0;
				double maxTileLat = 0;
				double maxTileLon = 0;

				// Parse the first and second parts to integers
				int TileIdLat = 0;
				int TileIdLon = 0;
				if (int.TryParse(parts[0], out TileIdLat) && int.TryParse(parts[1], out TileIdLon))
				{
					// Perform the subtraction with radius to get minTileId
					int minTileIdLat = TileIdLat - radius;
					int minTileIdLon = TileIdLon - radius;
					string minTileId = $"{minTileIdLat}_{minTileIdLon}";

					// Perform the addition with radius to get maxTileId
					int maxTileIdLat = TileIdLat + radius;
					int maxTileIdLon = TileIdLon + radius;
					string maxTileId = $"{maxTileIdLat}_{maxTileIdLon}";

					// Get coordinates for minTileId
					double[][] minTileCoordinates = Tile.GetCoordinatesForTile(minTileId);

					maxTileLat = minTileCoordinates[0][0];
					minTileLon = minTileCoordinates[0][1];

					// Get coordinates for maxTileId
					double[][] maxTileCoordinates = Tile.GetCoordinatesForTile(maxTileId);

					minTileLat = maxTileCoordinates[1][0];
					maxTileLon = maxTileCoordinates[1][1];
				}

				var maxCoordinates = new Coordinate { latitude = maxTileLat, longitude = maxTileLon };
				var minCoordinates = new Coordinate { latitude = minTileLat, longitude = minTileLon };

				var tappables = StateSingleton.Instance.activeTappables
				.Where(pred =>
				pred.Value.location.coordinate.latitude != null)
				.Select(pred => pred.Value.location)
				.ToList();

				if (tappables.Count < StateSingleton.Instance.config.maxTappableSpawnAmount)
				{
					for (int latLoop = TileIdLat - radius; latLoop <= TileIdLat + radius; latLoop++)
					{
						for (int lonLoop = TileIdLon - radius; lonLoop <= TileIdLon + radius; lonLoop++)
						{
							string currentTileId = $"{latLoop}_{lonLoop}";
							// Aquí debes obtener la lista de tappables en el tile actual
							// Supongamos que tienes una función tappablesInTileId que devuelve la cantidad de tappables en un tile dado
							var tappableListInTile = StateSingleton.Instance.activeTappables
								.Where(pred => pred.Value.location.tileId == currentTileId)
								.ToList();
							int tappablesInTileId = tappableListInTile.Count;
							int maxTappablesPerTile = StateSingleton.Instance.config.maxTappablesPerTile;
							int spawneableTappablesInTile = maxTappablesPerTile - tappablesInTileId;
							int perRequestMaxTappableSpawnsInTile = StateSingleton.Instance.config.perRequestMaxTappableSpawnsInTile;
							spawneableTappablesInTile = Math.Min(spawneableTappablesInTile, perRequestMaxTappableSpawnsInTile);
							if (spawneableTappablesInTile > 0)
							{
								// Generar nuevos tappables en este tile
								for (int i = 0; i < spawneableTappablesInTile; i++)
								{
									double tappableRandomLatitude = minCoordinates.latitude + (random.NextDouble() * (maxCoordinates.latitude - minCoordinates.latitude));
									double tappableRandomLongitude = minCoordinates.longitude + (random.NextDouble() * (maxCoordinates.longitude - minCoordinates.longitude));

									// Crear el nuevo tappable con las coordenadas aleatorias generadas
									var newTappable = TappableGeneration.CreateTappableInRadiusOfCoordinates(tappableRandomLatitude, tappableRandomLongitude);
									// Agregar el nuevo tappable a la lista de tappables
									tappables.Add(newTappable);
								}
							}
						}
					}
				}

				double randomLatitude = minCoordinates.latitude + (random.NextDouble() * (maxCoordinates.latitude - minCoordinates.latitude));
				double randomLongitude = minCoordinates.longitude + (random.NextDouble() * (maxCoordinates.longitude - minCoordinates.longitude));


				int randomnumber = new Random().Next(1, 101);
				if (randomnumber <= StateSingleton.Instance.config.publicAdventureSpawnPercentage && StateSingleton.Instance.config.publicAdventuresLimit > AdventureUtils.ReadEncounterLocations().Count)
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
						killSwitchedTileIds = [], // No he visto esta lista utilizada, ¿es para depuración?
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
