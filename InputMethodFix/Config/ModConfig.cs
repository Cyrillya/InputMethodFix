using Microsoft.Xna.Framework;

using StardewValley.BellsAndWhistles;

namespace InputMethodFix.Config;

internal sealed class ModConfig
{
    public bool UseSystemIME { get; set; } = false;
    public bool ShowLogText { get; set; } = false;
    public Color SelectedTextColor { get; set; } = SpriteText.color_Cyan;
    public Color UnselectedTextColor { get; set; } = SpriteText.color_White;
    public Color CompositionTextColor { get; set; } = SpriteText.color_White;
}
