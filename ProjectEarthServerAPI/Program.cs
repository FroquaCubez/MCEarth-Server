using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.ComponentModel;
using ProjectEarthServerAPI.Models;
using ProjectEarthServerAPI.Util;
using ProjectEarthServerAPI.Models.Features;
using ProjectEarthServerAPI.Models.Player;
using Serilog;
using Uma.Uuid;
using Serilog.Events;
using System;
using Microsoft.AspNetCore.Mvc.Routing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace ProjectEarthServerAPI
{

	public class Program
    {

		public static void Main(string[] args)
		{
			TypeDescriptor.AddAttributes(typeof(Uuid), new TypeConverterAttribute(typeof(StringToUuidConv)));

			// Init Logging
			Log.Logger = new LoggerConfiguration()
				.WriteTo.Console()
				.WriteTo.File("logs/debug.txt", rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true, fileSizeLimitBytes: 8338607, outputTemplate: "{Timestamp:HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
				.MinimumLevel.Debug()
				.MinimumLevel.Override("Microsoft", LogEventLevel.Information)
				.MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
				.MinimumLevel.Override("ProjectEarthServerAPI.Authentication", LogEventLevel.Warning)
				.CreateLogger();

			// Config Gen
			string ConfigFilePath = "./data/config/apiconfig.json";
			ConfigGenerator configGenerator = new ConfigGenerator();

			try
			{
				configGenerator.GenerateConfigFile(ConfigFilePath);
			}
			catch (Exception ex)
			{
				Console.WriteLine("An error has ocurred: " + ex.Message);
			}

			//Initialize state singleton from config
			StateSingleton.Instance.config = ServerConfig.getFromFile();
			StateSingleton.Instance.TappableGenerationConfig = TappableGenerationConfig.getFromFile();
			StateSingleton.Instance.catalog = CatalogResponse.FromFiles(StateSingleton.Instance.config.itemsFolderLocation, StateSingleton.Instance.config.efficiencyCategoriesFolderLocation);
			StateSingleton.Instance.recipes = Recipes.FromFile(StateSingleton.Instance.config.recipesFileLocation);
			StateSingleton.Instance.settings = SettingsResponse.FromFile(StateSingleton.Instance.config.settingsFileLocation);
			StateSingleton.Instance.challengeStorage = ChallengeStorage.FromFiles(StateSingleton.Instance.config.challengeStorageFolderLocation);
			StateSingleton.Instance.productCatalog = ProductCatalogResponse.FromFile(StateSingleton.Instance.config.productCatalogFileLocation);
			StateSingleton.Instance.tappableData = TappableGeneration.loadAllTappableSets();
			StateSingleton.Instance.activeTappables = [];
			StateSingleton.Instance.levels = ProfileUtils.readLevelDictionary();
			StateSingleton.Instance.shopItems = ShopUtils.readShopItemDictionary();

			string resourcepacksFolderPath = "./data/resourcepacks";
			string vanillaZipPath = "./data/resourcepacks/vanilla.zip";

			// Check if the resourcepacks folder exists, if not, create it
			if (!Directory.Exists(resourcepacksFolderPath))
			{
				Directory.CreateDirectory(resourcepacksFolderPath);
				Log.Debug("Created './data/resourcepacks' folder.");
			}

			// Check if vanilla.zip exists, if not, download it
			if (!File.Exists(vanillaZipPath))
			{
				string vanillaUrl = StateSingleton.Instance.config.resourcepack;
				DownloadFile(vanillaUrl, vanillaZipPath);
				Log.Debug("Downloaded 'vanilla.zip' from {0}.", vanillaUrl);
			}

			static void DownloadFile(string url, string outputPath)
			{
				using (var httpClient = new HttpClient())
				{
					var response = httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).Result;
					response.EnsureSuccessStatusCode();

					using (var contentStream = response.Content.ReadAsStreamAsync().Result)
					{
						using (var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
						{
							byte[] buffer = new byte[8192]; // 8KB buffer
							long totalBytesRead = 0;
							int bytesRead;
							long? contentLength = response.Content.Headers.ContentLength;

							while ((bytesRead = contentStream.Read(buffer, 0, buffer.Length)) > 0)
							{
								fileStream.Write(buffer, 0, bytesRead);
								totalBytesRead += bytesRead;

								if (contentLength.HasValue)
								{
									int progressPercentage = (int)((totalBytesRead * 100) / contentLength.Value);
									Console.Write($"\rDownloading... {progressPercentage}%");
								}
							}

							Console.WriteLine(); // Move to next line after download completes
						}
					}
				}

			}

			CreateHostBuilder(args).Build().Run();

			Log.Information("Server started!");
		}

		public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }

}
