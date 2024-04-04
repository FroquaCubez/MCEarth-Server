using System;
using System.IO;
using Newtonsoft.Json;

namespace ProjectEarthServerAPI.Util
{
	public class TappableGenerationConfig
	{
		public string[] TappableTypes { get; set; }
		public string[] TappableGrass { get; set; }
		public string[] TappableForest { get; set; }
		public string[] TappablePlain { get; set; }
		public string[] TappableBuilding { get; set; }
		public string[] TappableBeach { get; set; }
		public string[] TappableWater { get; set; }

		public static TappableGenerationConfig getFromFile()
		{
			string jsonFilePath = "./data/config/tappables.json";

			try
			{
				if (File.Exists(jsonFilePath))
				{
					string json = File.ReadAllText(jsonFilePath);
					return JsonConvert.DeserializeObject<TappableGenerationConfig>(json);
				}
				else
				{
					Console.WriteLine($"Error: JSON file not found at path: {jsonFilePath}");
					return null;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error loading configuration from JSON file: {ex.Message}");
				return null;
			}
		}
	}
}
