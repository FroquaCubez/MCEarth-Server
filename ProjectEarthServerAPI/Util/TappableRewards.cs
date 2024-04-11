using System;
using System.Collections.Generic;
using System.Linq;
using ProjectEarthServerAPI.Models;
using ProjectEarthServerAPI.Models.Features;
using ProjectEarthServerAPI.Models.Player;
using Serilog;

namespace ProjectEarthServerAPI.Util
{
	public class TappableRewards
	{
		private static Random random = new Random();

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

			EventUtils.HandleEvents(playerId, new TappableEvent { eventId = tappable.location.id });
			StateSingleton.Instance.activeTappables.Remove(tappable.location.id);

			return response;
		}

		private static Guid GetRandomItemForTappable(string type)
		{
			Dictionary<Guid, TappableItemDrop> DropTable = StateSingleton.Instance.tappableData[type].dropTable;
			float totalPercentage = (int)DropTable.Sum(item => item.Value.chance);
			float diceRoll = random.Next(0, (int)(totalPercentage * 10)) / 10;
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
			Dictionary<Guid, TappableItemDrop> DropTable;
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
				var dirtReward = new Rewards
				{
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
				itemRewards[i] = new RewardComponent()
				{
					Amount = targetDropSet[targetDropSet.Keys.ToList()[i]],
					Id = targetDropSet.Keys.ToList()[i]
				};
			}

			var rewards = new Rewards
			{
				Inventory = itemRewards,
				ExperiencePoints = experiencePoints,
				Rubies = (random.Next(0, 4) >= 3) ? 1 : 0
			};

			return rewards;
		}
	}
}
