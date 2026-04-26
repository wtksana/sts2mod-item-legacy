using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ItemLegacy;

public static class LegacyHistoryService
{
    private const string RemoteMultiplayerDescription = "多人局中仅由该玩家本地客户端根据其自己的上一局历史生成遗产，获得结果继续复用原版多人奖励同步。";

    public sealed class OfferPlan
    {
        public required IReadOnlyList<SerializableCard> Cards { get; init; }

        public required IReadOnlyList<SerializablePotion> Potions { get; init; }

        public required IReadOnlyList<SerializableRelic> Relics { get; init; }

        public required string DescriptionText { get; init; }
    }

    public static bool TryGetOptionState(Player player, out bool isEnabled, out string descriptionText)
    {
        if (LegacyRunClaimTracker.HasClaimedCurrentRun(player))
        {
            isEnabled = false;
            descriptionText = "本局已经领取过遗产。";
            return false;
        }

        if (!ShouldResolvePlanLocally(player))
        {
            isEnabled = true;
            descriptionText = RemoteMultiplayerDescription;
            return true;
        }

        if (TryCreateOfferPlanInternal(player, out OfferPlan? plan, out string disabledReason) && plan != null)
        {
            isEnabled = true;
            descriptionText = plan.DescriptionText;
            return true;
        }

        isEnabled = false;
        descriptionText = disabledReason;
        return false;
    }

    public static async Task<bool> OfferForSelectionAsync(Player player)
    {
        if (LegacyRunClaimTracker.HasClaimedCurrentRun(player))
        {
            return false;
        }

        if (!ShouldResolvePlanLocally(player))
        {
            return true;
        }

        if (!TryCreateOfferPlanInternal(player, out OfferPlan? plan, out _) || plan == null)
        {
            return false;
        }

        return await OfferAsync(player, plan);
    }

    private static bool TryCreateOfferPlanInternal(Player player, out OfferPlan? plan, out string disabledReason)
    {
        plan = null;
        disabledReason = "没有可用的上一局历史记录。";

        if (!TryLoadLatestHistory(player, out RunHistoryPlayer? historyPlayer) || historyPlayer == null)
        {
            return false;
        }

        List<SerializableCard> cards = DistinctCards(historyPlayer.Deck);
        List<SerializablePotion> potions = DistinctPotions(historyPlayer.Potions)
            .Where(potion => IsAvailablePotion(player, potion))
            .ToList();
        List<SerializableRelic> relics = DistinctRelics(historyPlayer.Relics)
            .Where(relic => IsAvailableLegacyRelic(player, relic))
            .ToList();

        if (relics.Count <= 0 && potions.Count <= 0 && cards.Count <= 0)
        {
            disabledReason = "上一局没有可领取的遗产。";
            return false;
        }

        plan = new OfferPlan
        {
            Relics = relics,
            Potions = potions,
            Cards = cards,
            DescriptionText = $"依次从上一局结束时保留的卡牌、药水、遗物中各选一个获得。当前可选：卡牌 {cards.Count}，药水 {potions.Count}，遗物 {relics.Count}。"
        };
        disabledReason = string.Empty;
        return true;
    }

    private static bool ShouldResolvePlanLocally(Player player)
    {
        if (RunManager.Instance.IsSinglePlayerOrFakeMultiplayer)
        {
            return true;
        }

        return LocalContext.IsMe(player);
    }

    public static async Task<bool> OfferAsync(Player player, OfferPlan plan)
    {
        if (plan.Cards.Count > 0)
        {
            List<Reward> cardRewards = plan.Cards
                .Select(save => (Reward)new LegacyCardReward(save, player))
                .ToList();
            await OfferRewardsAsync(player, cardRewards);
        }

        List<Reward> potionRewards = CreatePotionRewards(player, plan.Potions);
        if (potionRewards.Count > 0)
        {
            await OfferRewardsAsync(player, potionRewards);
        }

        List<Reward> relicRewards = CreateRelicRewards(player, plan.Relics);
        if (relicRewards.Count > 0)
        {
            await OfferRewardsAsync(player, relicRewards);
        }
        return true;
    }

    private static async Task OfferRewardsAsync(Player player, List<Reward> rewards)
    {
        if (rewards.Count == 0)
        {
            return;
        }

        if (rewards.Count == 1)
        {
            await new RewardsSet(player)
                .WithCustomRewards(rewards)
                .Offer();
            return;
        }

        LinkedRewardSet linkedRewardSet = new LinkedRewardSet(rewards, player);
        await new RewardsSet(player)
            .WithCustomRewards(new List<Reward> { linkedRewardSet })
            .Offer();
    }

    private static List<Reward> CreatePotionRewards(Player player, IEnumerable<SerializablePotion> potions)
    {
        return potions
            .Select(PotionModel.FromSerializable)
            .Select(model => (Reward)new PotionReward(model, player))
            .ToList();
    }

    private static List<Reward> CreateRelicRewards(Player player, IEnumerable<SerializableRelic> relics)
    {
        return relics
            .Select(RelicModel.FromSerializable)
            .Select(model => (Reward)new RelicReward(model, player))
            .ToList();
    }

    private static bool IsAvailablePotion(Player player, SerializablePotion potion)
    {
        PotionModel model = PotionModel.FromSerializable(potion);
        return Hook.ShouldProcurePotion(player.RunState, player.Creature.CombatState, model, player);
    }

    private static bool IsAvailableLegacyRelic(Player player, SerializableRelic relic)
    {
        RelicModel model = RelicModel.FromSerializable(relic);
        return IsLegacyRelicRarityAllowed(model) && model.IsAllowed(player.RunState);
    }

    private static List<SerializableCard> DistinctCards(IEnumerable<SerializableCard> cards)
    {
        return cards
            .GroupBy(static card => card.Id)
            .Select(static group => group.First())
            .ToList();
    }

    private static List<SerializablePotion> DistinctPotions(IEnumerable<SerializablePotion> potions)
    {
        return potions
            .GroupBy(static potion => potion.Id)
            .Select(static group => group.First())
            .ToList();
    }

    private static List<SerializableRelic> DistinctRelics(IEnumerable<SerializableRelic> relics)
    {
        return relics
            .GroupBy(static relic => relic.Id)
            .Select(static group => group.First())
            .ToList();
    }

    private static bool IsLegacyRelicRarityAllowed(RelicModel relic)
    {
        return LegacyConfig.Current.InheritableRelicRarities.Contains(relic.Rarity);
    }

    private static bool TryLoadLatestHistory(Player currentPlayer, out RunHistoryPlayer? historyPlayer)
    {
        historyPlayer = null;
        List<string> historyNames = SaveManager.Instance.GetAllRunHistoryNames()
            .OrderByDescending(ParseHistoryStartTime)
            .ToList();

        foreach (string historyName in historyNames)
        {
            var result = SaveManager.Instance.LoadRunHistory(historyName);
            if (!result.Success)
            {
                Log.Warn($"ItemLegacy skipped unreadable run history file: {historyName}");
                continue;
            }

            RunHistory? history = result.SaveData;
            if (history == null)
            {
                Log.Warn($"ItemLegacy loaded empty run history file: {historyName}");
                continue;
            }

            historyPlayer = SelectHistoryPlayer(history, currentPlayer);
            if (historyPlayer != null)
            {
                return true;
            }
        }

        return false;
    }

    private static RunHistoryPlayer? SelectHistoryPlayer(RunHistory history, Player currentPlayer)
    {
        ulong localPlayerId = PlatformUtil.GetLocalPlayerId(history.PlatformType);
        return history.Players.FirstOrDefault(player => player.Id == localPlayerId)
            ?? history.Players.FirstOrDefault(player => player.Character == currentPlayer.Character.Id)
            ?? history.Players.FirstOrDefault();
    }

    private static long ParseHistoryStartTime(string historyName)
    {
        string numeric = historyName.EndsWith(".run", StringComparison.OrdinalIgnoreCase)
            ? historyName[..^4]
            : historyName;

        return long.TryParse(numeric, NumberStyles.Integer, CultureInfo.InvariantCulture, out long value)
            ? value
            : long.MinValue;
    }
}
