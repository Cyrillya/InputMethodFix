using System.Runtime.InteropServices;

using HarmonyLib;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewModdingAPI;
using StardewModdingAPI.Events;

using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Extensions;
using StardewValley.Menus;

using xTile.Tiles;

namespace InputMethodFix;

public class ModEntry : Mod
{
    public static IntPtr hImc;
    public static IntPtr hWnd;

    public static IKeyboardSubscriber KeyboardSubscriber => Game1.keyboardDispatcher.Subscriber;
    private static bool _oldIsTextInputSubscribed;
    public static bool IsTextInputSubscribed => KeyboardSubscriber is not null;

    public override void Entry(IModHelper helper)
    {
        // 初始化
        hWnd = SDL.GetWin32Handle(Game1.game1.Window.Handle);
        hImc = Imm.ImmGetContext(hWnd);

        // 游戏启动时，关闭输入法
        helper.Events.GameLoop.GameLaunched += (_, _) =>
        {
            CandidateHandler.SetState(false);
            SDL.SDL_StopTextInput();
        };

        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;

        // 为了确保是绘制到鼠标下层，必须用Harmony了
        var harmony = new Harmony(ModManifest.UniqueID);
        harmony.Patch(
           original: AccessTools.Method(typeof(Game1), nameof(Game1.drawMouseCursor)),
           prefix: new HarmonyMethod(typeof(ModEntry), nameof(RenderInputMethodNotInFullscreenMenu))
        );
        harmony.Patch(
           original: AccessTools.Method(typeof(IClickableMenu), nameof(IClickableMenu.drawMouse)),
           prefix: new HarmonyMethod(typeof(ModEntry), nameof(RenderInputMethodInFullscreenMenu))
        );
    }

    public static void RenderInputMethodInFullscreenMenu(SpriteBatch b, bool ignore_transparency, int cursor)
    {
        RenderInputMethod(b);
    }

    public static void RenderInputMethodNotInFullscreenMenu()
    {
        RenderInputMethod(Game1.spriteBatch);
    }

    // 由于全屏模式下不显示输入法候选，所以要自行绘制输入法，类似泰拉瑞亚
    // 暂不支持除Windows以外的系统
    public static void RenderInputMethod(SpriteBatch spriteBatch)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        if (!SDL.IsTextInputActive() || !IsTextInputSubscribed) return;

        CandidateHandler.UpdateCandidateList();

        List<string> candidates = new List<string>();
        for (uint num = 0u; num < CandidateHandler.CandidateCount; num++)
        {
            string candidate = CandidateHandler.GetCandidate(num);
            candidates.Add(candidate);
        }

        if (candidates.Count == 0)
            return;

        // 计算候选框宽高
        var font = Game1.dialogueFont;
        float scale = 0.77f;
        int innerPadding = 10;
        int outerPadding = 16;
        float width = 0f;
        int height = 64;
        width += innerPadding;
        string format = "{0,2}: {1}";
        string split = "  ";
        for (int i = 0; i < candidates.Count; i++)
        {
            int number = i + 1;
            string candidateText = format;
            if (i < candidates.Count - 1)
                candidateText += split;

            string displayText = string.Format(candidateText, number, candidates[i]);
            width += font.MeasureString(displayText).X * scale;
            width += innerPadding;
        }

        // 计算绘制位置
        Rectangle titleSafeArea = Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea();
        int screenHeight = titleSafeArea.Height;
        int screenWidth = titleSafeArea.Width;
        float x, y;
        if (KeyboardSubscriber is TextBox textBox)
        {
            var textForCalc = textBox.Text;
            if (string.IsNullOrEmpty(textForCalc) || textForCalc.EndsWith('\n'))
            {
                // 不准以无字符作为某一行开始，不然位置计算乱套了
                textForCalc += "H";
            }

            var textSize = textBox.Font.MeasureString(textForCalc);
            var cursorPosition = new Vector2(textBox.X + 16 + textSize.X, textBox.Y + 10 + textSize.Y);
            x = cursorPosition.X;
            if (cursorPosition.Y + height > screenHeight)
                y = cursorPosition.Y - height - outerPadding - textBox.Font.LineSpacing;
            else
                y = cursorPosition.Y + textBox.Font.LineSpacing;

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
        Game1.DrawBox((int) x, (int) y, (int) width, height);

        // 绘制缓冲区文字
        Color shadowColor = SpriteText.color_Black * 0.3f;
        var pos = new Vector2(x + innerPadding, y + 6);
        string compositionString = CandidateHandler.GetCompositionString();
        Utility.drawTextWithColoredShadow(spriteBatch, $" {compositionString}", font, pos, SpriteText.color_White, shadowColor, scale);

        // 绘制候选
        uint selectedCandidate = CandidateHandler.SelectedCandidate;
        pos = new Vector2(x + innerPadding, y + 36);
        for (int i = 0; i < candidates.Count; i++)
        {
            Color color = SpriteText.color_White;
            if (i == selectedCandidate)
                color = SpriteText.color_Cyan;

            int number = i + 1;
            string candidateText = format;
            if (i < candidates.Count - 1)
                candidateText += split;

            string displayText = string.Format(candidateText, number, candidates[i]);
            Vector2 textSize = font.MeasureString(displayText) * scale;
            Utility.drawTextWithColoredShadow(spriteBatch, displayText, font, pos, color, shadowColor, scale);
            pos.X += textSize.X + innerPadding;
        }
    }

    // 每帧检测输入状态是否更改，并切换SDL输入模式
    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (_oldIsTextInputSubscribed != IsTextInputSubscribed)
        {
            _oldIsTextInputSubscribed = IsTextInputSubscribed;

            this.Monitor.Log($"检测到输入状态更改为 {IsTextInputSubscribed}", LogLevel.Debug);

            if (IsTextInputSubscribed)
            {
                CandidateHandler.SetState(true);
                // 开启输入模式
                SDL.SDL_StartTextInput();
                // 尝试让系统输入法框绘制在输入框的下面，但是好像没用，不管了，反正我自己绘制输入法框（
                // StartTextInputAtSubscriber();
            }
            else
            {
                CandidateHandler.SetState(false);
                SDL.SDL_StopTextInput();
            }
        }
    }

    private void StartTextInputAtSubscriber()
    {
        if (KeyboardSubscriber is not TextBox textBox)
        {
            SDL.SDL_StartTextInput();
            return;
        }

        SDL.SetTextInputRect(new Rectangle(textBox.X, textBox.Y, textBox.Width, textBox.Height));
        SDL.SDL_StartTextInput();
    }
}