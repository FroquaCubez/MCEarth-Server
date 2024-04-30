using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using ProjectEarthServerAPI.Models;
using ProjectEarthServerAPI.Models.Features;
using ProjectEarthServerAPI.Models.Multiplayer.Adventure;

namespace ProjectEarthServerAPI.Util
{
	public class AdventureUtils
	{
		public static List<LocationResponse.ActiveLocation> Encounters = new List<LocationResponse.ActiveLocation>();

		public static string[] AdventureIcons = new[] 
		{
			"genoa:adventure_generic_map", "genoa:adventure_generic_map_b", "genoa:adventure_generic_map_c"
		};

		private static Random random = new Random();

		public Dictionary<Guid, Item.Rarity> crystalRarityList = StateSingleton.Instance.catalog.result.items
			.FindAll(select => select.item.type == "AdventureScroll")
			.ToDictionary(pred => pred.id, pred => pred.rarity);

		public static AdventureRequestResult RedeemCrystal(string playerId, PlayerAdventureRequest adventureRequest, Guid crystalId)
		{
			InventoryUtils.RemoveItemFromInv(playerId, crystalId);

			string selectedAdventureIcon = AdventureIcons[random.Next(0, AdventureIcons.Length)];
			Guid selectedAdventureId = Guid.Parse("b7335819-c123-49b9-83fb-8a0ec5032779");

			LocationResponse.ActiveLocation adventureLocation = new LocationResponse.ActiveLocation
			{
				coordinate = adventureRequest.coordinate,
				encounterMetadata = new EncounterMetadata
				{
					anchorId = "",
					anchorState = "Off",
					augmentedImageSetId = "",
					encounterType = EncounterType.None,
					locationId = Guid.Empty,
					worldId = Guid.Parse("4f16a053-4929-263a-c91a-29663e29df76") // TODO: Replace this with actual adventure id
				},
				expirationTime = DateTime.UtcNow.Add(TimeSpan.FromMinutes(10.00)),
				spawnTime = DateTime.UtcNow,
				icon = selectedAdventureIcon,
				id = selectedAdventureId,
				metadata = new LocationResponse.Metadata
				{
					rarity = Item.Rarity.Common,
					rewardId = "genoa:adventure_rewards" 
				},
				tileId = Tile.GetTileForCoordinates(adventureRequest.coordinate.latitude, adventureRequest.coordinate.longitude),
				type = "PlayerAdventure"
			};

			return new AdventureRequestResult {result = adventureLocation, updates = new Updates()};
		}

		public static List<LocationResponse.ActiveLocation> ReadEncounterLocations() {
			string filepath = StateSingleton.Instance.config.EncounterLocationsFileLocation;
			string encouterLocationsJson = File.ReadAllText(filepath);
            return JsonConvert.DeserializeObject<List<LocationResponse.ActiveLocation>>(encouterLocationsJson);
        }

		public static List<LocationResponse.ActiveLocation> GetEncountersForLocation(double lat, double lon) {
			List<LocationResponse.ActiveLocation> encounterLocations = ReadEncounterLocations();

			Encounters.RemoveAll(match => match.expirationTime < DateTime.UtcNow);

			foreach (LocationResponse.ActiveLocation encounter in encounterLocations)
			{
				if (Encounters.FirstOrDefault(match => match.coordinate.latitude == encounter.coordinate.latitude && match.coordinate.longitude == encounter.coordinate.longitude) == null) {
					string selectedAdventureIcon = AdventureIcons[random.Next(0, AdventureIcons.Length)];
					Guid selectedAdventureId = Guid.Parse("b7335819-c123-49b9-83fb-8a0ec5032779");
					DateTime currentTime = DateTime.UtcNow;
					DateTime expirationTime = encounter.expirationTime;
					Encounters.Add(new LocationResponse.ActiveLocation
					{
						coordinate = encounter.coordinate,
						encounterMetadata = new EncounterMetadata
						{
							anchorId = "",
							anchorState = "Off",
							augmentedImageSetId = "",
							encounterType = EncounterType.Short16X16Hostile,
							locationId = selectedAdventureId,
							worldId = selectedAdventureId // TODO: Replace this with actual adventure id
						},
						expirationTime = expirationTime,
						spawnTime = currentTime,
						icon = selectedAdventureIcon,
						id = selectedAdventureId,
						metadata = new LocationResponse.Metadata
						{
							rarity = Item.Rarity.Common,
							rewardId = "genoa:adventure_rewards"//version4Generator.NewUuid().ToString() // Seems to always be uuidv4 from official responses so generate one
						},
						tileId = Tile.GetTileForCoordinates(encounter.coordinate.latitude, encounter.coordinate.longitude),
						type = "Encounter"
					});
				}
			}
			return Encounters;
		}

		public static LocationResponse.ActiveLocation CreateEncounterLocation(double randomLatitude, double randomLongitude, DateTime expirationTime)
		{
			Encounters.RemoveAll(match => match.expirationTime < DateTime.UtcNow);

			string selectedAdventureIcon = AdventureIcons[random.Next(0, AdventureIcons.Length)];
			Guid selectedAdventureId = Guid.NewGuid(); // Generate a new unique ID for the encounter

			var newEncounterLocation = new LocationResponse.ActiveLocation
			{
				coordinate = new Coordinate { latitude = randomLatitude, longitude = randomLongitude },
				encounterMetadata = new EncounterMetadata
				{
					anchorId = "",
					anchorState = "Off",
					augmentedImageSetId = "",
					encounterType = EncounterType.Short16X16Hostile, // You may adjust this based on your encounter type
					locationId = selectedAdventureId,
					worldId = selectedAdventureId // Set to the same ID as locationId for simplicity
				},
				expirationTime = expirationTime,
				spawnTime = DateTime.UtcNow,
				icon = selectedAdventureIcon,
				id = selectedAdventureId,
				metadata = new LocationResponse.Metadata
				{
					rarity = Item.Rarity.Common, // You may adjust the rarity as needed
					rewardId = "genoa:adventure_rewards" // You may adjust the reward ID as needed
				},
				tileId = Tile.GetTileForCoordinates(randomLatitude, randomLongitude),
				type = "Encounter"
			};

			Encounters.Add(newEncounterLocation);

			return newEncounterLocation;
		}

	}
}

