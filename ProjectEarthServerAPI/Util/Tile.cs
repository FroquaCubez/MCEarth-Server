using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using Serilog;

namespace ProjectEarthServerAPI.Util
{
    /// <summary>
    /// Contains Functions for converting long/lat -> tile pos, downloading tilees, and anything else that might come up
    /// </summary>
    public class Tile
    {
        public static bool DownloadTile(int pos1, int pos2, string basePath)
        {
            using (HttpClient httpClient = new HttpClient())

            try
            {
                Directory.CreateDirectory(Path.Combine(basePath, pos1.ToString()));
                //string downloadUrl = "https://cdn.mceserv.net/tile/16/" + pos1 + "/" + pos1 + "_" + pos2 + "_16.png";// Disabled because the server is down 
                string downloadUrl = StateSingleton.Instance.config.tileServerUrl + "/styles/mc-earth/16/" + pos1 + "/" + pos2 + ".png"; 
				//Log.Debug("[Tile Download] Tile download url:" + downloadUrl);
                HttpResponseMessage response = httpClient.GetAsync(downloadUrl).Result;
                response.EnsureSuccessStatusCode();
                byte[] imageData = response.Content.ReadAsByteArrayAsync().Result;
                File.WriteAllBytes(Path.Combine(basePath, pos1.ToString(), $"{pos1}_{pos2}_16.png"), imageData);
                return true;
            }
            catch (HttpRequestException ex)
            {
                // Handle HTTP request exception
                Console.WriteLine("HTTP Request Exception: " + ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                // Handle other exceptions
                Console.WriteLine("Error: " + ex.Message);
                return false;
            }
        }

        //From https://wiki.openstreetmap.org/wiki/Slippy_map_tilenames with slight changes

        public static string GetTileForCoordinates(double lat, double lon)
        {
            const int zoom = 16;
            
            int xtile = (int)Math.Floor((lon + 180) / 360 * (1 << zoom));
            int ytile = (int)Math.Floor((1 - Math.Log(Math.Tan(ToRadians(lat)) + 1 / Math.Cos(ToRadians(lat))) / Math.PI) / 2 * (1 << zoom));

            xtile = Math.Clamp(xtile, 0, (1 << zoom) - 1);
            ytile = Math.Clamp(ytile, 0, (1 << zoom) - 1);

            return $"{xtile}_{ytile}";
        }

        public static string GetPixelForCoordinates(double lat, double lon)
        {
            const int tileSize = 256;
            const int zoom = 16;
            
            int pixelX = (int)Math.Floor((lon + 180) / 360 * tileSize * (1 << zoom) % tileSize);
            int pixelY = (int)Math.Floor((1 - Math.Log(Math.Tan(ToRadians(lat)) + 1 / Math.Cos(ToRadians(lat))) / Math.PI) / 2 * tileSize * (1 << zoom) % tileSize);

            pixelX = Math.Clamp(pixelX, 0, tileSize - 1);
            pixelY = Math.Clamp(pixelY, 0, tileSize - 1);

            return $"{pixelX}_{pixelY}";
        }

        private static double ToRadians(double angle)
        {
            return angle * Math.PI / 180;
        }

        public static (double lat, double lon) GetCoordinatesFromPixel(int x, int y)
        {
            double normalizedX = (double)x / 256.0;
            double normalizedY = (double)y / 256.0;

            double lon = normalizedX * 360.0 - 180.0;

            double latRad = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * normalizedY)));
            double lat = latRad * (180.0 / Math.PI);

            return (lat, lon);
        }
    }

    public class Biome
    {
        public enum Type
        {
            Water,
            Grass,
            Beach,
            Street,
            Road,
            Plain,
            Building,
            ChildrenPark,
            Forest,
            Unknown
        }

        public static Type GetTappableBiomeForCoordinates(double lat, double lon)
        {
            string tilePath = ""; // Definir tilePath fuera del bloque if

            // Obtener el tile para las coordenadas dadas
            string tile = Tile.GetTileForCoordinates(lat, lon);

            // Separar el tile en partes utilizando el carácter '_'
            string[] tileParts = tile.Split('_');

            // Asegurarse de que haya dos partes (latitud y longitud)
            if (tileParts.Length == 2)
            {
                // Asignar cada parte a las variables correspondientes
                string tile_lat = tileParts[0];
                string tile_lon = tileParts[1];

                // Construir la ruta del archivo de imagen
                tilePath = StateSingleton.Instance.config.tileServerUrl + "/styles/mc-earth/16/" + tile_lat + "/" + tile_lon + ".png";

                // Ahora, tile_lat y tile_lon contienen la latitud y longitud del tile respectivamente.
                // Y tilePath contiene la URL del archivo de imagen.
            }
            else
            {
                // Manejar el caso en el que el formato del tile no sea válido
                Console.WriteLine("El formato del tile no es válido.");
            }

            // Log the tile path for debugging purposes
            //Console.WriteLine("Tile Path: " + tilePath);

            try
            {
				// Descargar la imagen desde la URL
				using HttpClient httpClient = new HttpClient();
				byte[] imageData = httpClient.GetByteArrayAsync(tilePath).Result;
				using MemoryStream memoryStream = new MemoryStream(imageData);
				using Bitmap tileImage = new Bitmap(memoryStream);
				{
					// Verificar que las coordenadas estén dentro de los límites de la imagen
					string pixel = Tile.GetPixelForCoordinates(lat, lon);
					//Log.Debug(lon.ToString() + "" + lat.ToString());
					//Log.Debug(pixel.ToString());

					// Obtener el color del píxel en las coordenadas dadas
					string[] parts = pixel.Split('_');
					int pixelX = int.Parse(parts[0]);
					int pixelY = int.Parse(parts[1]);
					Color pixelColor = tileImage.GetPixel(pixelX, pixelY);
					Log.Debug("Pixel Color: " + pixelColor.ToString() + " in pixel " + pixel);

					// Mapeo de colores hexadecimales a biomas
					Dictionary<Color, Type> colorBiomeMap = new Dictionary<Color, Type>
						{
							{ Color.FromArgb(153,153,153), Type.Water },
							{ Color.FromArgb(204,204,204), Type.Grass },
							{ Color.FromArgb(102,102,102), Type.Beach },
							{ Color.FromArgb(51,51,51), Type.Street },
							{ Color.FromArgb(34,34,34), Type.Road },
							{ Color.FromArgb(255,255,255), Type.Plain },
							{ Color.FromArgb(238,238,238), Type.Building },
							{ Color.FromArgb(170,170,170), Type.ChildrenPark },
							{ Color.FromArgb(241,241,241), Type.Forest },
                            // Agregar más mapeos de colores y biomas según sea necesario
                        };

					// Buscar el color en el mapeo y devolver el bioma correspondiente
					if (colorBiomeMap.TryGetValue(pixelColor, out Type value))
					{
						return value;
					}
					else
					{
						return Type.Unknown;
					}
				}
			}
            catch (Exception ex)
            {
                // Manejar otras excepciones
                Console.WriteLine("Error al procesar la imagen del azulejo: " + ex.Message);
                return Type.Unknown;
            }
        }
    }
}
