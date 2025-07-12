using System.Runtime.InteropServices;

using HarmonyLib;

using InputMethodFix.Config;
using InputMethodFix.Windows;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI;
using StardewModdingAPI.Events;

using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Extensions;
using StardewValley.Menus;

using xTile.Tiles;

using static InputMethodFix.SDL;

namespace InputMethodFix;

public class ModEntry : Mod
{
    internal static ModConfig Config;
    internal static ModEntry Instance;

    public static IntPtr hImc;
    public static IntPtr hWnd;
    private static WindowsMessageHook _wndProcHook;
    private static WinImm32Ime WinImm32Ime;

    public static Game1 KeyboardFocusInstance => Game1.keyboardFocusInstance;
    public static IKeyboardSubscriber? KeyboardSubscriber => KeyboardFocusInstance?.instanceKeyboardDispatcher?.Subscriber;
    private static bool _oldIsTextInputSubscribed;
    public static bool IsTextInputSubscribed => KeyboardSubscriber != null;
    private static bool _imeDrawnThisTick;

    public override void Entry(IModHelper helper)
    {
        // 初始化
        Instance = this;
        Config = Helper.ReadConfig<ModConfig>();
        Localization.Init(helper.Translation);
        InitIME();

        // 游戏启动时，关闭输入法
        helper.Events.GameLoop.GameLaunched += OnGameLaunched;

        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.Display.Rendering += (sender, e) => { _imeDrawnThisTick = false; };

        // 为了确保是绘制到鼠标下层，必须用Harmony了
        var harmony = new Harmony(ModManifest.UniqueID);
        var drawMouseCursorMethod = AccessTools.Method(typeof(Game1), nameof(Game1.drawMouseCursor));
        var drawMouseMethod = AccessTools.Method(typeof(IClickableMenu), nameof(IClickableMenu.drawMouse));

        if (drawMouseCursorMethod != null)
        {
            harmony.Patch(
                original: drawMouseCursorMethod,
                prefix: new HarmonyMethod(typeof(ModEntry), nameof(RenderInputMethodNotInFullscreenMenu))
            );
        }

        if (drawMouseMethod != null)
        {
            harmony.Patch(
                original: drawMouseMethod,
                prefix: new HarmonyMethod(typeof(ModEntry), nameof(RenderInputMethodInFullscreenMenu))
            );
        }
    }

    public void InitIME()
    {
        if (Game1.game1?.IsMainInstance != true) return;
        if (Game1.game1.Window?.Handle == null) return;

        // 初始化IME状态
        var windowHandle = Game1.game1.Window.Handle;
        SDL_SysWMinfo info = new SDL_SysWMinfo();
        SDL_GetVersion(out info.version);
        SDL_GetWindowWMInfo(windowHandle, ref info);
        hWnd = info.info.win.window;
        hImc = NativeMethods.ImmGetContext(hWnd);

        _wndProcHook ??= new WindowsMessageHook(hWnd);
        WinImm32Ime ??= new WinImm32Ime(_wndProcHook, hWnd);
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        // 初始禁用输入法
        WinImm32Ime?.Disable();
        SDL_StopTextInput();

        GenericModConfigMenuIntegration.Init();
    }

    public static void RenderInputMethodInFullscreenMenu(SpriteBatch b, bool ignore_transparency, int cursor)
    {
        RenderInputMethod(b);
    }

    public static void RenderInputMethodNotInFullscreenMenu()
    {
        RenderInputMethod(Game1.spriteBatch);
    }

    public static void RenderInputMethod(SpriteBatch spriteBatch)
    {
    // 设置中开启
        if (Config?.UseSystemIME == true) return;
        // 暂不支持Linux，OSX
        if (Constants.TargetPlatform is not GamePlatform.Windows) return;
        // 你得先在打字
        if (!IsTextInputActive() || !IsTextInputSubscribed) return;
        // 多人同屏模式下，绘制到正在输入的屏幕
        if (Game1.game1 == null || KeyboardFocusInstance != Game1.game1) return;
        // WinImm32Ime至少得先实例化，不然哪来的输入法信息
        if (WinImm32Ime == null) return;

        RenderInputMethodInner(spriteBatch);
    }

    // 由于全屏模式下不显示输入法候选，所以要自行绘制输入法，类似泰拉瑞亚
    // 暂不支持除Windows以外的系统
    public static void RenderInputMethodInner(SpriteBatch spriteBatch)
    {
        WinImm32Ime.UpdateCandidateList();

        List<string> candidates = new List<string>();
        for (uint num = 0u; num < WinImm32Ime.CandidateCount; num++)
        {
            string candidate = WinImm32Ime.GetCandidate(num);
            candidates.Add(candidate);
        }

        string compositionString = WinImm32Ime.GetCompositionString();
        if (string.IsNullOrWhiteSpace(compositionString))
            return;

        // 计算候选框宽高
        var font = Game1.dialogueFont;
        float scale = 0.77f;
        int innerPadding = 10;
        int outerPadding = 16;
        float candidatesWidth = 0f;
        int height = candidates.Count == 0 ? font.LineSpacing : font.LineSpacing * 2 + 12;
        height = (int) (height * scale);
        candidatesWidth += innerPadding;
        string format = "{0,2}. {1}";
        for (int i = 0; i < candidates.Count; i++)
        {
            int number = i + 1;
            string candidateText = format;

            string displayText = string.Format(candidateText, number, candidates[i]);
            candidatesWidth += font.MeasureString(displayText).X * scale;
            candidatesWidth += innerPadding;
        }

        float compositionWidth = font.MeasureString(compositionString).X * scale + innerPadding * 2;
        float width = Math.Max(candidatesWidth, compositionWidth);

        // 计算绘制位置
        Rectangle titleSafeArea = Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea();
        int screenHeight = titleSafeArea.Height;
        int screenWidth = titleSafeArea.Width;
        float x, y;
        if (KeyboardSubscriber is TextBox textBox)
        {
            var boxFont = textBox.Font ?? Game1.dialogueFont;

            var textForCalc = textBox.Text;
            if (string.IsNullOrEmpty(textForCalc) || textForCalc.EndsWith('\n'))
            {
                // 不准以无字符作为某一行开始，不然位置计算乱套了
                textForCalc += "H";
            }

            var textSize = boxFont.MeasureString(textForCalc);
            var cursorPosition = new Vector2(textBox.X + 16 + textSize.X, textBox.Y + 10 + textSize.Y);
            x = cursorPosition.X;
            if (cursorPosition.Y + height > screenHeight)
                y = cursorPosition.Y - height - outerPadding - boxFont.LineSpacing;
            else
                y = cursorPosition.Y + boxFont.LineSpacing;

            // 确保输入法整框在游戏屏幕范围内
            if (x + width > screenWidth)
                x = screenWidth - width - outerPadding;
            if (y + height > screenHeight)
                y = screenHeight - height - outerPadding;
        }
        else // 默认在右下角
        {
            x = screenWidth - width - outerPadding;
            y = screenHeight - height - outerPadding;
        }

        // 绘制框
        Utils.DrawBox(spriteBatch, (int) x, (int) y, (int) width, height);

        // 绘制缓冲区文字
        var pos = new Vector2(x + innerPadding, y + 7 * scale);
        
        Utils.DrawTextWithAutoShadow(spriteBatch, $"{compositionString}", font, pos, Config.CompositionTextColor, scale);

        if (candidates.Count == 0)
            return;

        // 绘制候选
        uint selectedCandidate = WinImm32Ime.SelectedCandidate;
        pos = new Vector2(x + innerPadding, y + (font.LineSpacing + 9) * scale);
        for (int i = 0; i < candidates.Count; i++)
        {
            Color color = Config.UnselectedTextColor;
            if (i == selectedCandidate)
                color = Config.SelectedTextColor;

            int number = i + 1;
            string candidateText = format;

            string displayText = string.Format(candidateText, number, candidates[i]);
            Vector2 textSize = font.MeasureString(displayText) * scale;
            Utils.DrawTextWithAutoShadow(spriteBatch, displayText, font, pos, color, scale);
            pos.X += textSize.X + innerPadding;
        }
    }

    // 每帧检测输入状态是否更改，并切换SDL输入模式
    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (Game1.game1?.IsMainInstance != true) return;
        if (_oldIsTextInputSubscribed == IsTextInputSubscribed) return; // 检测更改

        _oldIsTextInputSubscribed = IsTextInputSubscribed;

        if (Config.ShowLogText)
        {

            Monitor.Log($"检测到输入状态更改为 {IsTextInputSubscribed}，手柄输入状态为 {Game1.options.gamepadControls}（正常情况下，该值应永远为False）", LogLevel.Debug);
            if (Context.IsSplitScreen)
            {
                Monitor.Log($"当前为分屏状态，执行输入操作的屏幕ID为 {KeyboardFocusInstance.instanceId}", LogLevel.Debug);
            }
        }

        if (IsTextInputSubscribed)
        {
            WinImm32Ime?.Enable();
            // 开启输入模式
            SDL_StartTextInput();
            // 尝试让系统输入法框绘制在输入框的下面，但是好像没用，不管了，反正我自己绘制输入法框（
            //StartTextInputAtSubscriber();
        }
        else
        {
            WinImm32Ime?.Disable();
            SDL_StopTextInput();
        }
    }

    private void StartTextInputAtSubscriber()
    {
        if (KeyboardSubscriber is not TextBox textBox)
        {
            SDL_StartTextInput();
            return;
        }

        SetTextInputRect(new Rectangle(textBox.X, textBox.Y, textBox.Width, textBox.Height));
        SDL_StartTextInput();
    }
}