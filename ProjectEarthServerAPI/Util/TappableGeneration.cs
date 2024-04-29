using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using ProjectEarthServerAPI.Models;
using ProjectEarthServerAPI.Models.Features;
using Serilog;
using Uma.Uuid;

namespace ProjectEarthServerAPI.Util
{
	/// <summary>
	/// Some simple utilities to interface with generated files from Tappy
	/// </summary>
	public class TappableGeneration
	{
		private static Version4Generator version4Generator = new Version4Generator();

		private static Random random = new Random();

		public static Dictionary<string, TappableLootTable> loadAllTappableSets()
		{
			Log.Information("[Tappables] Loading tappable data.");
			Dictionary<string, TappableLootTable> tappableData = new();
			string[] files = Directory.GetFiles("./data/tappable", "*.json");
			foreach (var file in files)
			{
				TappableLootTable table = JsonConvert.DeserializeObject<TappableLootTable>(File.ReadAllText(file));
				tappableData.Add(table.tappableID, table);
				//Log.Information($"Loaded {table.dropTable.Count} drops for tappable ID {table.tappableID} | Path: {file}");
			}

			return tappableData;
		}

		//double is default set to negative because its *extremely unlikely* someone will set a negative value intentionally, and I can't set it to null.
		public static LocationResponse.ActiveLocation CreateTappableInRadiusOfCoordinates(double randomLatitude, double randomLongitude)
		{

			string type = null;

			// Debugging: Log method parameters
			// Log.Debug($"createTappableInRadiusOfCoordinates called with latitude: {latitude}, longitude: {longitude}, radius: {radius}, type: {type}");


			var currentTime = DateTime.UtcNow;

			// Debugging: Log current time
			//Log.Debug($"Current time: {currentTime}");

			//Nab tile loc
			string tileId = Tile.GetTileForCoordinates(randomLatitude, randomLongitude);
			//Log.Debug($"Tile ID for coordinates ({latitude}, {longitude}): {tileId}");

			// Modificar las coordenadas para LocationResponse.ActiveLocation

			//Log.Debug($"Biome: {tappableBiome}");

			TappableGeneration tappableUtils = new TappableGeneration();

			if (StateSingleton.Instance.config.biomeGeneration)
			{
				string tappableBiome = Biome.GetTappableBiomeForCoordinates(randomLatitude, randomLongitude).ToString();
				//Log.Debug($"Tappable Biome in ({randomLatitude}, {randomLongitude}): {tappableBiome}");

				if (random.NextDouble() < 0.005)
				{
					type = "genoa:chest_tappable_map";
				}
				else
				{
					string[] tappableArray = null;

					switch (tappableBiome)
					{
						case "Building":
							tappableArray = StateSingleton.Instance.TappableGenerationConfig.TappableBuilding;
							break;
						case "Plain":
							tappableArray = StateSingleton.Instance.TappableGenerationConfig.TappablePlain;
							break;
						case "Grass":
							tappableArray = StateSingleton.Instance.TappableGenerationConfig.TappableGrass;
							break;
						case "Forest":
							tappableArray = StateSingleton.Instance.TappableGenerationConfig.TappableForest;
							break;
						case "Water":
							tappableArray = StateSingleton.Instance.TappableGenerationConfig.TappableWater;
							break;
						case "Beach":
							tappableArray = StateSingleton.Instance.TappableGenerationConfig.TappableBeach;
							break;
					}

					if (tappableArray != null)
					{
						type ??= tappableArray[random.Next(0, tappableArray.Length)];
					}
					else
					{
						tappableArray = StateSingleton.Instance.TappableGenerationConfig.TappableTypes;
						type ??= tappableArray[random.Next(0, tappableArray.Length)];
						return tappableUtils.CreateTappable(type, tileId, randomLatitude, randomLongitude, currentTime);
					}
				}

				return tappableUtils.CreateTappable(type, tileId, randomLatitude, randomLongitude, currentTime);
			}
			else
			{
				string[] tappableArray = StateSingleton.Instance.TappableGenerationConfig.TappableTypes;
				type ??= tappableArray[random.Next(0, tappableArray.Length)];
				return tappableUtils.CreateTappable(type, tileId, randomLatitude, randomLongitude, currentTime);
			}

		}


		public LocationResponse.ActiveLocation CreateTappable(string type, string tileId, double randomLatitude, double randomLongitude, DateTime currentTime)
		{
			// Obtain the type of tappable if not specified
			//Log.Debug($"Selected tappable type: \x1b[35m{type}\x1b[0m"); // Magenta color for better visibility

			// Check if the tappable type is present in the tappables data
			if (!StateSingleton.Instance.tappableData.TryGetValue(type, out TappableLootTable tappableData))
			{
				Log.Error("[Tappables] Tappable rarity was not found for tappable type \x1b[31m" + type + "\x1b[0m. Using common"); // Red color for error
				return null; // Return null if the tappable type is not present
			}

			// Get the rarity of the tappable type
			Item.Rarity rarity = tappableData.rarity;

			// Create the tappable
			LocationResponse.ActiveLocation tappable = new LocationResponse.ActiveLocation
			{
				id = Guid.NewGuid(),
				tileId = tileId,
				coordinate = new Coordinate
				{
					latitude = randomLatitude,
					longitude = randomLongitude
				},
				spawnTime = currentTime,
				expirationTime = currentTime.AddMinutes(StateSingleton.Instance.config.tappableExpirationTime),
				type = "Tappable",
				icon = type,
				metadata = new LocationResponse.Metadata
				{
					rarity = rarity,
					rewardId = version4Generator.NewUuid().ToString()
				},
				encounterMetadata = null,
				tappableMetadata = new LocationResponse.TappableMetadata
				{
					rarity = rarity
				}
			};

			//Log.Debug($"\x1b[32mTapable has been successfully created\x1b[0m"); // Green color for success

			// Generate rewards for the tappable
			var rewards = TappableRewards.GenerateRewardsForTappable(tappable.icon);

			// Store the tappable and its rewards
			StoreTappable(tappable, rewards);

			return tappable;
		}

		private void StoreTappable(LocationResponse.ActiveLocation tappable, Rewards rewards)
		{
			var storage = new LocationResponse.ActiveLocationStorage { location = tappable, rewards = rewards };
			StateSingleton.Instance.activeTappables.Add(tappable.id, storage);
			Log.Debug($"Active tappables count: {StateSingleton.Instance.activeTappables.Count}");
		}

	}
}
