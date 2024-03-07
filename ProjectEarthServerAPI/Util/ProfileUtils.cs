using System.Collections.Generic;
using Newtonsoft.Json;
using ProjectEarthServerAPI.Models;
using System.IO;
using ProjectEarthServerAPI.Models.Features;
using ProjectEarthServerAPI.Models.Player;
using System;

namespace ProjectEarthServerAPI.Util
{
	public class ProfileUtils
	{
		public static ProfileData ReadProfile(string playerId)
		{
			return GenericUtils.ParseJsonFile<ProfileData>(playerId, "profile");
		}

		public static void AddExperienceToPlayer(string playerId, int experiencePoints)
		{
			var playerProfile = ReadProfile(playerId);
			var currentLvl = playerProfile.level;
			playerProfile.totalExperience += experiencePoints;
			while (currentLvl < 25 && playerProfile.experienceRemaining <= 0)
			{
				playerProfile.level++;
				RewardLevelupRewards(playerId, playerProfile.level);
			}

			WriteProfile(playerId, playerProfile);
		}

		public static void AddHealthToPlayer(string playerId, int healthPoints)
		{
			var playerProfile = ReadProfile(playerId);
			playerProfile.healthPercentage += healthPoints;

			WriteProfile(playerId, playerProfile);
		}

		private static void RewardLevelupRewards(string playerId, int level)
		{
			var rewards = ProfileResponse.levels[level.ToString()].rewards;
			RewardUtils.RedeemRewards(playerId, rewards, EventLocation.LevelUp);
			rewards.Levels = level;
			JournalUtils.AddActivityLogEntry(playerId, DateTime.UtcNow, Scenario.LevelUp, rewards, ChallengeDuration.SignIn, null, null, null, null, null);
		}

		private static bool WriteProfile(string playerId, ProfileData playerProfile)
		{
			return GenericUtils.WriteJsonFile(playerId, playerProfile, "profile");
		}

		public static Dictionary<string, ProfileLevel> readLevelDictionary()
		{
			var filepath = StateSingleton.Instance.config.LevelDictionaryFileLocation;
			var levelsJson = File.ReadAllText(filepath);
			return JsonConvert.DeserializeObject<Dictionary<string, ProfileLevel>>(levelsJson);
		}
	}
}
