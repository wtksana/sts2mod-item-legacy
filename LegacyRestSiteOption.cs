using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;

namespace ItemLegacy;

public sealed class LegacyRestSiteOption : RestSiteOption
{
    public const string LegacyOptionId = "LEGACY";

    public const string TitleText = "遗产";

    public const string FallbackIconPath = "res://images/ui/rest_site/option_smith.png";

    public override string OptionId => LegacyOptionId;

    public override LocString Description => new("rest_site_ui", "PROMPT");

    public string DisplayDescription { get; }

    public LegacyRestSiteOption(Player owner)
        : base(owner)
    {
        LegacyHistoryService.TryGetOptionState(owner, out bool isEnabled, out string descriptionText);
        IsEnabled = isEnabled;
        DisplayDescription = descriptionText;
    }

    public override async Task<bool> OnSelect()
    {
        return await LegacyHistoryService.OfferForSelectionAsync(Owner);
    }
}
