using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using ProjectEarthServerAPI.Models;
using ProjectEarthServerAPI.Models.Features;
using ProjectEarthServerAPI.Models.Player;
using Serilog;
using Uma.Uuid;

namespace ProjectEarthServerAPI.Util
{
	/// <summary>
	/// Some simple utilities to interface with generated files from Tappy
	/// </summary>
	public class TappableUtils
	{
		private static Version4Generator version4Generator = new Version4Generator();

		// TODO: Consider turning this into a dictionary (or pull it out to a separate file) and building out a spawn-weight system? 
		public static string[] TappableTypes = new[]
		{
			"genoa:stone_mound_a_tappable_map", "genoa:stone_mound_b_tappable_map",
			"genoa:stone_mound_c_tappable_map", "genoa:grass_mound_a_tappable_map",
			"genoa:grass_mound_b_tappable_map", "genoa:grass_mound_c_tappable_map", "genoa:tree_oak_a_tappable_map",
			"genoa:tree_oak_b_tappable_map", "genoa:tree_oak_c_tappable_map", "genoa:tree_birch_a_tappable_map",
			"genoa:tree_spruce_a_tappable_map", "genoa:chest_tappable_map", "genoa:sheep_tappable_map",
			"genoa:cow_tappable_map", "genoa:pig_tappable_map", "genoa:chicken_tappable_map", "genoa:squid_tappable_map"
		};

		public static string[] TappableGrass = new[]
		{
			"genoa:grass_mound_a_tappable_map", "genoa:grass_mound_b_tappable_map", "genoa:grass_mound_c_tappable_map", "genoa:tree_oak_a_tappable_map",
			"genoa:tree_oak_b_tappable_map", "genoa:tree_oak_c_tappable_map", "genoa:tree_birch_a_tappable_map",
			"genoa:tree_spruce_a_tappable_map"
		};

		public static string[] TappableForest = new[]
		{
			"genoa:tree_oak_a_tappable_map",
			"genoa:tree_oak_b_tappable_map", "genoa:tree_oak_c_tappable_map", "genoa:tree_birch_a_tappable_map",
			"genoa:tree_spruce_a_tappable_map"
		};

		public static string[] TappablePlain = new[]
		{
			"genoa:grass_mound_a_tappable_map", "genoa:grass_mound_b_tappable_map", "genoa:grass_mound_c_tappable_map", "genoa:sheep_tappable_map",
			"genoa:cow_tappable_map", "genoa:pig_tappable_map", "genoa:chicken_tappable_map"
		};

		public static string[] TappableStones = new[]
		{
			"genoa:stone_mound_a_tappable_map", "genoa:stone_mound_b_tappable_map",
			"genoa:stone_mound_c_tappable_map"
		};

		public static string[] TappableWater = new[]
		{
			"genoa:squid_tappable_map"
		};

		private static Random random = new Random();

		// For json deserialization
		public class ItemDrop {
			public float chance { get; set; }
			public int min { get; set; }
			public int max { get; set; }
		}
		public class TappableLootTable
		{
			public string tappableID { get; set; }
			[JsonConverter(typeof(StringEnumConverter))]
		    public Item.Rarity rarity { get; set; }
			public Dictionary<Guid, ItemDrop> dropTable { get; set; }
		}

		public static Dictionary<string, TappableLootTable> loadAllTappableSets()
		{
			Log.Information("[Tappables] Loading tappable data.");
			Dictionary<string, TappableLootTable> tappableData = new();
			string[] files = Directory.GetFiles("./data/tappable", "*.json");
			foreach (var file in files)
			{
				TappableLootTable table = JsonConvert.DeserializeObject<TappableLootTable>(File.ReadAllText(file));
				tappableData.Add(table.tappableID, table);
				Log.Information($"Loaded {table.dropTable.Count} drops for tappable ID {table.tappableID} | Path: {file}");
			}

			return tappableData;
		}

		/// <summary>
		/// Generate a new tappable in a given radius of a given cord set
		/// </summary>
		/// <param name="longitude"></param>
		/// <param name="latitude"></param>
		/// <param name="radius">Optional. Spawn Radius if not provided, will default to value specified in config</param>
		/// <param name="type">Optional. If not provided, a random type will be picked from TappableUtils.TappableTypes</param>
		/// <returns></returns>
		//double is default set to negative because its *extremely unlikely* someone will set a negative value intentionally, and I can't set it to null.
		public static LocationResponse.ActiveLocation createTappableInRadiusOfCoordinates(double latitude, double longitude, double radius = -1.0, string type = null)
		{
			// Debugging: Log method parameters
			// Log.Debug($"createTappableInRadiusOfCoordinates called with latitude: {latitude}, longitude: {longitude}, radius: {radius}, type: {type}");

			if (radius == -1.0)
			{
				radius = StateSingleton.Instance.config.tappableSpawnRadius;
				// Log.Information($"Using default radius from config: {radius}");
			}

			var currentTime = DateTime.UtcNow;

			// Debugging: Log current time
			Log.Debug($"Current time: {currentTime}");

			//Nab tile loc
			string tileId = Tile.GetTileForCoordinates(latitude, longitude);
			Log.Debug($"Tile ID for coordinates ({latitude}, {longitude}): {tileId}");

			// Modificar las coordenadas para LocationResponse.ActiveLocation
			double randomLatitude = Math.Round(latitude + (random.NextDouble() * 2 - 1) * radius, 6);
			double randomLongitude = Math.Round(longitude + (random.NextDouble() * 2 - 1) * radius, 6);

			//Log.Debug($"Biome: {tappableBiome}");

			TappableUtils tappableUtils = new TappableUtils();

			if (StateSingleton.Instance.config.biomeGeneration == true)
			{
				// Obtener el bioma para las nuevas coordenadas manipuladas
				string tappableBiome = Biome.GetTappableBiomeForCoordinates(randomLatitude, randomLongitude).ToString();
				Log.Debug($"Tappable Biome in ({randomLatitude}, {randomLongitude}): {tappableBiome}");

				if (random.NextDouble() < 0.05)
				{
					type = "genoa:chest_tappable_map";

					return tappableUtils.CreateTappable(type, tileId, randomLatitude, randomLongitude, currentTime);
				}
				else if (tappableBiome == "Building")
				{
					type ??= TappableStones[random.Next(0, TappableStones.Length)];

					return tappableUtils.CreateTappable(type, tileId, randomLatitude, randomLongitude, currentTime);
				}
				else if (tappableBiome == "Plain")
				{
					type ??= TappablePlain[random.Next(0, TappablePlain.Length)];

					return tappableUtils.CreateTappable(type, tileId, randomLatitude, randomLongitude, currentTime);
				}
				else if (tappableBiome == "Grass")
				{
					type ??= TappableGrass[random.Next(0, TappableGrass.Length)];

					return tappableUtils.CreateTappable(type, tileId, randomLatitude, randomLongitude, currentTime);
				}
				else if (tappableBiome == "Forest")
				{
					type ??= TappableForest[random.Next(0, TappableForest.Length)];

					return tappableUtils.CreateTappable(type, tileId, randomLatitude, randomLongitude, currentTime);
				}
				else if (tappableBiome == "Water")
				{
					type ??= TappableWater[random.Next(0, TappableWater.Length)];

					return tappableUtils.CreateTappable(type, tileId, randomLatitude, randomLongitude, currentTime);
				}
				else
				{
					return createTappableInRadiusOfCoordinates(latitude, longitude, radius, type);
				}
			}
			else
			{
				type ??= TappableTypes[random.Next(0, TappableTypes.Length)];

				return tappableUtils.CreateTappable(type, tileId, randomLatitude, randomLongitude, currentTime);
			}
		}

		public LocationResponse.ActiveLocation CreateTappable(string type, string tileId, double randomLatitude, double randomLongitude, DateTime currentTime)
		{
			// Obtain the type of tappable if not specified
			Log.Debug($"Selected tappable type: \x1b[35m{type}\x1b[0m"); // Magenta color for better visibility

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
				expirationTime = currentTime.AddMinutes(10),
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

			Log.Debug($"\x1b[32mTapable has been successfully created\x1b[0m"); // Green color for success

			// Generate rewards for the tappable
			var rewards = GenerateRewardsForTappable(tappable.icon);

			// Store the tappable and its rewards
			StoreTappable(tappable, rewards);

			return tappable;
		}

		private void StoreTappable(LocationResponse.ActiveLocation tappable, Rewards rewards)
		{
			var storage = new LocationResponse.ActiveLocationStorage { location = tappable, rewards = rewards };
			StateSingleton.Instance.activeTappables.Add(tappable.id, storage);
			Log.Information($"Active tappables count: {StateSingleton.Instance.activeTappables.Count}");
		}

		public static TappableResponse RedeemTappableForPlayer(string playerId, TappableRequest request)
		{
			var tappable = StateSingleton.Instance.activeTappables[request.id];

			var response = new TappableResponse()
			{
				result = new TappableResponse.Result()
				{
					token = new Token()
					{
						clientProperties = new Dictionary<string, string>(),
						clientType = "redeemtappable",
						lifetime = "Persistent",
						rewards = tappable.rewards
					}
				},
				updates = RewardUtils.RedeemRewards(playerId, tappable.rewards, EventLocation.Tappable)
			};

			EventUtils.HandleEvents(playerId, new TappableEvent{eventId = tappable.location.id});
			StateSingleton.Instance.activeTappables.Remove(tappable.location.id);

			return response;
		}

		private static Guid GetRandomItemForTappable(string type) 
		{
			Dictionary<Guid, ItemDrop> DropTable = StateSingleton.Instance.tappableData[type].dropTable;
			float totalPercentage = (int)DropTable.Sum(item => item.Value.chance);
			float diceRoll = random.Next(0, (int)(totalPercentage*10))/10;
			foreach (Guid item in DropTable.Keys)
			{
				if (diceRoll >= DropTable[item].chance && (random.Next(0, 4) >= 3))
					return item;
				diceRoll -= DropTable[item].chance;
			}
			return Guid.Empty;
		}

		public static Rewards GenerateRewardsForTappable(string type)
		{
			var catalog = StateSingleton.Instance.catalog;
			Dictionary<Guid, ItemDrop> DropTable;
			var targetDropSet = new Dictionary<Guid, int> { };
			int experiencePoints = 0;

			try
			{
				DropTable = StateSingleton.Instance.tappableData[type].dropTable;
			}
			catch (Exception e)
			{
				Log.Error("[Tappables] no json file for tappable type " + type + " exists in data/tappables. Using backup of dirt (f0617d6a-c35a-5177-fcf2-95f67d79196d). Error:" + e);
				Guid dirtId = Guid.Parse("f0617d6a-c35a-5177-fcf2-95f67d79196d");
				var dirtReward = new Rewards { 
					Inventory = new RewardComponent[1] { new RewardComponent { Id = dirtId, Amount = 1 } }, 
					ExperiencePoints = catalog.result.items.Find(match => match.id == dirtId).experiencePoints.tappable, 
					Rubies = (random.Next(0, 4) >= 3) ? 1 : 0,
				};

				return dirtReward;
				//dirt for you... sorry :/
			}

			for (int i = 0; i < 3; i++)
			{
				Guid item = GetRandomItemForTappable(type);
				if (!targetDropSet.Keys.Contains(item) && item != Guid.Empty)
				{
					int amount = random.Next(DropTable[item].min, DropTable[item].max);
					targetDropSet.Add(item, amount);
					experiencePoints += catalog.result.items.Find(match => match.id == item).experiencePoints.tappable * amount;
				}
			}

			if (targetDropSet.Count == 0)
			{
				Guid item = DropTable.Aggregate((x, y) => x.Value.chance > y.Value.chance ? x : y).Key;
				int amount = random.Next(DropTable[item].min, DropTable[item].max);
				targetDropSet.Add(item, amount);
				experiencePoints += catalog.result.items.Find(match => match.id == item).experiencePoints.tappable * amount;
			}

			var itemRewards = new RewardComponent[targetDropSet.Count];
			for (int i = 0; i < targetDropSet.Count; i++)
			{
				itemRewards[i] = new RewardComponent() { 
					Amount = targetDropSet[targetDropSet.Keys.ToList()[i]], 
					Id = targetDropSet.Keys.ToList()[i] 
				};
			}

			var rewards = new Rewards { 
				Inventory = itemRewards, 
				ExperiencePoints = experiencePoints, 
				Rubies = (random.Next(0, 4) >= 3) ? 1 : 0 
			}; 

			return rewards;
		}

		public static LocationResponse.Root GetActiveLocations(double lat, double lon, double radius = -1.0)
		{
			if (radius == -1.0) radius = StateSingleton.Instance.config.tappableSpawnRadius;
			var maxCoordinates = new Coordinate {latitude = lat + radius, longitude = lon + radius};
			var minCoordinates = new Coordinate {latitude = lat - radius, longitude = lon - radius};

			var tappables = StateSingleton.Instance.activeTappables
				.Where(pred =>
					(pred.Value.location.coordinate.latitude >= minCoordinates.latitude && pred.Value.location.coordinate.latitude <= maxCoordinates.latitude)
					&& (pred.Value.location.coordinate.longitude >= minCoordinates.longitude && pred.Value.location.coordinate.longitude <= maxCoordinates.longitude))
				.ToDictionary(pred => pred.Key, pred => pred.Value.location).Values.ToList();

			if (tappables.Count <= StateSingleton.Instance.config.maxTappableSpawnAmount)
			{
				var count = random.Next(StateSingleton.Instance.config.minTappableSpawnAmount,
					StateSingleton.Instance.config.maxTappableSpawnAmount);
				count -= tappables.Count;
				for (; count > 0; count--)
				{
					var tappable = createTappableInRadiusOfCoordinates(lat, lon);
					tappables.Add(tappable);
				}
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
