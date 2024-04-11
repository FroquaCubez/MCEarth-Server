using System;
using System.Collections.Generic;
using System.Linq;
using ProjectEarthServerAPI.Models;
using ProjectEarthServerAPI.Models.Features;
using ProjectEarthServerAPI.Models.Player;
using Serilog;

namespace ProjectEarthServerAPI.Util
{
    public class TokenUtils
    {
        public static Dictionary<Guid, Token> GetSigninTokens(string playerId)
        {
            var origTokens = ReadTokens(playerId);
            Dictionary<Guid, Token> returnTokens = new Dictionary<Guid, Token>();
            returnTokens = origTokens.Result.tokens
                .Where(pred => pred.Value.clientProperties.Count == 0)
                .ToDictionary(pred => pred.Key, pred => pred.Value);



            return returnTokens;
        }

        public static void AddItemToken(string playerId, Guid itemId)
        {
            var itemtoken = new Token
            {
                clientProperties = new Dictionary<string, string>(),
                clientType = "item.unlocked",
                lifetime = "Persistent",
                rewards = new Rewards()
            };

            itemtoken.clientProperties.Add("itemid", itemId.ToString());

            AddToken(playerId, itemtoken);

            Log.Information($"[{playerId}]: Added item token {itemId}!");
			EventUtils.HandleEvents(playerId, new ItemEvent
			{
				action = ItemEventAction.ItemJournalEntryUnlocked,
				eventId = itemId
			});
		}

        public static bool AddToken(string playerId, Token tokenToAdd)
        {
            var tokens = ReadTokens(playerId);
            if (!tokens.Result.tokens.ContainsValue(tokenToAdd))
            {
                tokens.Result.tokens.Add(Guid.NewGuid(), tokenToAdd);
                WriteTokens(playerId, tokens);
                Log.Information($"[{playerId}] Added token!");
                return true;
            }

            Log.Error($"[{playerId}] Tried to add token, but it already exists!");
            return false;
        }

        public static Token RedeemToken(string playerId, Guid tokenId)
        {
            var parsedTokens = ReadTokens(playerId);
            if (parsedTokens.Result.tokens.ContainsKey(tokenId))
            {
                var tokenToRedeem = parsedTokens.Result.tokens[tokenId];
                RewardUtils.RedeemRewards(playerId, tokenToRedeem.rewards, EventLocation.Token);

				parsedTokens.Result.tokens.Remove(tokenId);

                WriteTokens(playerId, parsedTokens);

                Log.Information($"[{playerId}]: Redeemed token {tokenId}.");

				if (tokenToRedeem.clientProperties != null)
				{
					string itemId;
					if (tokenToRedeem.clientProperties.TryGetValue("itemid", out itemId))
						JournalUtils.AddActivityLogEntry(playerId, DateTime.UtcNow, Scenario.JournalContentCollected,
							new Rewards { Inventory = new RewardComponent[] { new RewardComponent { Id = Guid.Parse(itemId), Amount = 0 } } },
							ChallengeDuration.Career, null, null, null, null, null);
					/*EventUtils.HandleEvents(playerId, new ItemEvent
					{
						action = ItemEventAction.ItemJournalEntryUnlocked,
						eventId = Guid.Parse(itemId)
					});*/
				}

				return tokenToRedeem;
            }
            else
            {
                Log.Information($"[{playerId}] tried to redeem token {tokenId}, but it was not in the token list!");
                return null;
            }
        }

        public static TokenResponse ReadTokens(string playerId)
            => GenericUtils.ParseJsonFile<TokenResponse>(playerId, "tokens");
        private static void WriteTokens(string playerId, TokenResponse tokenList)
            => GenericUtils.WriteJsonFile(playerId, tokenList, "tokens");
    }
}
