using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using ProjectEarthServerAPI.Models.Features;
using System.Collections.Generic;
using System;

namespace ProjectEarthServerAPI.Models
{
	public class TappableLootTable
	{
		public string tappableID { get; set; }
		[JsonConverter(typeof(StringEnumConverter))]
		public Item.Rarity rarity { get; set; }
		public Dictionary<Guid, TappableItemDrop> dropTable { get; set; }
	}
}
