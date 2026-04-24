using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ItemLegacy;

public sealed class LegacyCardReward : Reward
{
    private readonly CardModel _card;

    private bool _wasTaken;

    private static string RewardIcon => ImageHelper.GetImagePath("ui/reward_screen/reward_icon_special_card.png");

    protected override RewardType RewardType => RewardType.SpecialCard;

    public override int RewardsSetIndex => 4;

    protected override string IconPath => RewardIcon;

    public override LocString Description
    {
        get
        {
            var loc = new LocString("gameplay_ui", "COMBAT_REWARD_ADD_SPECIAL_CARD");
            loc.Add("Card", _card.Title);
            return loc;
        }
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { HoverTipFactory.FromCard(_card) };

    public override bool IsPopulated => true;

    public LegacyCardReward(SerializableCard save, Player player)
        : base(player)
    {
        if (save.Id == null)
        {
            throw new System.InvalidOperationException("Legacy card reward received a card without Id.");
        }

        _card = SaveUtil.CardOrDeprecated(save.Id).ToMutable();
    }

    public override Task Populate()
    {
        return Task.CompletedTask;
    }

    protected override async Task<bool> OnSelect()
    {
        if (!Player.RunState.ContainsCard(_card))
        {
            Player.RunState.AddCard(_card, Player);
        }

        Log.Info($"Obtained {_card.Id} from legacy card reward");
        CardPileAddResult result = await CardPileCmd.Add(_card, PileType.Deck);
        if (!result.success)
        {
            return false;
        }

        RunManager.Instance.RewardSynchronizer.SyncLocalObtainedCard(result.cardAdded);
        CardCmd.PreviewCardPileAdd(result, 2f);
        _wasTaken = true;
        return true;
    }

    public override void OnSkipped()
    {
        if (_wasTaken)
        {
            return;
        }

        ulong? netId = LocalContext.NetId;
        if (!netId.HasValue)
        {
            return;
        }

        Player.RunState.CurrentMapPointHistoryEntry?.GetEntry(netId.Value).CardChoices.Add(new CardChoiceHistoryEntry(_card, wasPicked: false));
        RunManager.Instance.RewardSynchronizer.SyncLocalSkippedCard(_card);
    }

    public override void MarkContentAsSeen()
    {
    }
}
