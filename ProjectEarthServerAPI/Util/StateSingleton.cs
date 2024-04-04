using System;
using System.Collections.Generic;
using ProjectEarthServerAPI.Models;
using ProjectEarthServerAPI.Models.Features;
using ProjectEarthServerAPI.Models.Player;

namespace ProjectEarthServerAPI.Util
{
	/// <summary>
	/// Global state information
	/// </summary>
	public sealed class StateSingleton
	{
		private StateSingleton() { }

		private static StateSingleton instance = null;

		public static StateSingleton Instance
		{
			get
			{
				if (instance == null)
				{
					instance = new StateSingleton();
				}

				return instance;
			}
		}

		public CatalogResponse catalog { get; set; }
		public ServerConfig config { get; set; }
		public TappableGenerationConfig TappableGenerationConfig { get; set; }
		public Recipes recipes { get; set; }
		public SettingsResponse settings { get; set; }
		public ChallengeStorage challengeStorage { get; set; }
		public ProductCatalogResponse productCatalog { get; set; }

		public Dictionary<string, TappableLootTable> tappableData { get; set; }

		/// <summary>
		/// A reference of guid <-> id, so that we can keep track of a tappable from spawn to redeem
		/// </summary>
		public Dictionary<Guid, LocationResponse.ActiveLocationStorage> activeTappables { get; set; }
		public Dictionary<string, ProfileLevel> levels { get; set; }
		public Dictionary<Guid, StoreItemInfo> shopItems { get; set; }
	}
}
