using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Rewards;

namespace ItemLegacy;

public sealed class LegacyLinkedRewardSet : LinkedRewardSet
{
    private static readonly MethodInfo SuccessfullySelectedSetter =
        AccessTools.PropertySetter(typeof(Reward), nameof(Reward.SuccessfullySelected));

    private readonly List<Reward> _originals;

    public LegacyLinkedRewardSet(List<Reward> rewards, Player player)
        : base(rewards, player)
    {
        _originals = new List<Reward>(rewards);
    }

    protected override Task<bool> OnSelect()
    {
        if (_originals.Any(static r => r.SuccessfullySelected))
        {
            SuccessfullySelectedSetter.Invoke(this, new object[] { true });
        }

        return Task.FromResult(true);
    }
}
