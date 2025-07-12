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

        configMenu.AddTextOption(
            mod: modManifest,
            name: () => Localization.Config_DummyInputBox_Name(),
            tooltip: () => Localization.Config_DummyInputBox_Tooltip(),
            getValue: () => config.DummyInputBox,
            setValue: value => config.DummyInputBox = value
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
            tooltip: () => null,
            getValue: () => config.ShowLogText,
            setValue: value => config.ShowLogText = value
        );

        // GMCM Options 模组提供颜色支持

        var configMenuExt = helper.ModRegistry.GetApi<IGMCMOptionsAPI>("jltaylor-us.GMCMOptions");

        if (configMenuExt is null)
        {
            configMenu.AddParagraph(
                mod: modManifest,
                text: () => Localization.Config_ColorChangeNote()
            );
            return;
        }

        configMenuExt.AddColorOption(
            mod: modManifest,
            getValue: () => config.SelectedTextColor,
            setValue: (c) => config.SelectedTextColor = c,
            name: () => Localization.Config_SelectedTextColor_Name(),
            tooltip: () => Localization.Config_SelectedTextColor_Tooltip(),
            showAlpha: false,
            colorPickerStyle: (uint) (IGMCMOptionsAPI.ColorPickerStyle.AllStyles | IGMCMOptionsAPI.ColorPickerStyle.RadioChooser));
        configMenuExt.AddColorOption(
            mod: modManifest,
            getValue: () => config.UnselectedTextColor,
            setValue: (c) => config.UnselectedTextColor = c,
            name: () => Localization.Config_UnselectedTextColor_Name(),
            tooltip: () => Localization.Config_UnselectedTextColor_Tooltip(),
            showAlpha: false,
            colorPickerStyle: (uint) (IGMCMOptionsAPI.ColorPickerStyle.AllStyles | IGMCMOptionsAPI.ColorPickerStyle.RadioChooser));
        configMenuExt.AddColorOption(
            mod: modManifest,
            getValue: () => config.CompositionTextColor,
            setValue: (c) => config.CompositionTextColor = c,
            name: () => Localization.Config_CompositionTextColor_Name(),
            tooltip: () => Localization.Config_CompositionTextColor_Tooltip(),
            showAlpha: false,
            colorPickerStyle: (uint) (IGMCMOptionsAPI.ColorPickerStyle.AllStyles | IGMCMOptionsAPI.ColorPickerStyle.RadioChooser));
    }
}
