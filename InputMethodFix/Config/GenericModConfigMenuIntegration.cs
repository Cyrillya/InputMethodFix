namespace InputMethodFix.Config;

internal class GenericModConfigMenuIntegration
{

    public static void Init()
    {
        // 兼容Generic Mod Config Menu
        var helper = ModEntry.Instance.Helper;
        var modManifest = ModEntry.Instance.ModManifest;
        var config = ModEntry.Config;
        var configMenu = helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (configMenu is null)
            return;

        configMenu.Register(
            mod: modManifest,
            reset: () => ModEntry.Config = new ModConfig(),
            save: () => helper.WriteConfig(ModEntry.Config)
        );

        configMenu.AddBoolOption(
            mod: modManifest,
            name: () => Localization.Config_UseSystemIme_Name(),
            tooltip: () => Localization.Config_UseSystemIme_Tooltip(),
            getValue: () => config.UseSystemIME,
            setValue: value => config.UseSystemIME = value
        );
        configMenu.AddBoolOption(
            mod: modManifest,
            name: () => Localization.Config_ShowLogText_Name(),
            tooltip: () => Localization.Config_ShowLogText_Name(),
            getValue: () => config.ShowLogText,
            setValue: value => config.ShowLogText = value
        );
        configMenu.AddParagraph(
            mod: modManifest,
            text: () => Localization.Config_ColorChangeNote()
        );
    }
}
