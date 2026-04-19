using System.Linq;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;

namespace ItemLegacy;

public static class LegacyRunClaimTracker
{
    public static bool HasClaimedCurrentRun(Player player)
    {
        IRunState? runState = player.RunState;
        if (runState == null)
        {
            return false;
        }

        return runState.MapPointHistory
            .SelectMany(static actEntries => actEntries)
            .SelectMany(static mapEntry => mapEntry.PlayerStats)
            .Any(historyEntry =>
                historyEntry.PlayerId == player.NetId &&
                historyEntry.RestSiteChoices.Contains(LegacyRestSiteOption.LegacyOptionId));
    }
}
