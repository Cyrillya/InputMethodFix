using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using StardewValley;
using StardewValley.BellsAndWhistles;

namespace InputMethodFix;

internal static class Utils
{
    /// <summary>
    /// 直接复制自 Game1.DrawBox，唯一的区别是自定义SpriteBatch，而不是使用Game1.spriteBatch。因为部分模组可能有自己的SpriteBatch，不能一股脑用Game1的（已知直接用Game1.spriteBatch不支持GMCM的输入法框）
    /// </summary>
    public static void DrawBox(SpriteBatch spriteBatch, int x, int y, int width, int height, Color? color = null)
    {
        Rectangle sourceRect = new Rectangle(64, 128, 64, 64);
        Texture2D menu_texture = Game1.menuTexture;
        Color draw_color = Color.White;
        Color inner_color = Color.White;
        if (color.HasValue)
        {
            draw_color = color.Value;
            menu_texture = Game1.uncoloredMenuTexture;
            inner_color = new Color((int) Utility.Lerp(draw_color.R, Math.Min(255, draw_color.R + 150), 0.65f), (int) Utility.Lerp(draw_color.G, Math.Min(255, draw_color.G + 150), 0.65f), (int) Utility.Lerp(draw_color.B, Math.Min(255, draw_color.B + 150), 0.65f));
        }
        spriteBatch.Draw(menu_texture, new Rectangle(x, y, width, height), sourceRect, inner_color);
        sourceRect.Y = 0;
        Vector2 offset = new Vector2(-sourceRect.Width * 0.5f, -sourceRect.Height * 0.5f);
        sourceRect.X = 0;
        spriteBatch.Draw(menu_texture, new Vector2(x + offset.X, y + offset.Y), sourceRect, draw_color);
        sourceRect.X = 192;
        spriteBatch.Draw(menu_texture, new Vector2(x + offset.X + width, y + offset.Y), sourceRect, draw_color);
        sourceRect.Y = 192;
        spriteBatch.Draw(menu_texture, new Vector2(x + width + offset.X, y + height + offset.Y), sourceRect, draw_color);
        sourceRect.X = 0;
        spriteBatch.Draw(menu_texture, new Vector2(x + offset.X, y + height + offset.Y), sourceRect, draw_color);
        sourceRect.X = 128;
        sourceRect.Y = 0;
        spriteBatch.Draw(menu_texture, new Rectangle(64 + x + (int) offset.X, y + (int) offset.Y, width - 64, 64), sourceRect, draw_color);
        sourceRect.Y = 192;
        spriteBatch.Draw(menu_texture, new Rectangle(64 + x + (int) offset.X, y + (int) offset.Y + height, width - 64, 64), sourceRect, draw_color);
        sourceRect.Y = 128;
        sourceRect.X = 0;
        spriteBatch.Draw(menu_texture, new Rectangle(x + (int) offset.X, y + (int) offset.Y + 64, 64, height - 64), sourceRect, draw_color);
        sourceRect.X = 192;
        spriteBatch.Draw(menu_texture, new Rectangle(x + width + (int) offset.X, y + (int) offset.Y + 64, 64, height - 64), sourceRect, draw_color);
    }

    /// <summary>
    /// 获取灰度值
    /// </summary>
    /// <param name="r"></param>
    /// <param name="g"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static byte RgbToGray(Color color)
    {
        return (byte) (0.299 * color.R + 0.587 * color.G + 0.114 * color.B);
    }

    /// <summary>
    /// 与 Utility.drawTextWithColoredShadow 相同，但是根据传入的文本颜色的灰度值自动决定阴影颜色（黑or白）
    /// </summary>
    public static void DrawTextWithAutoShadow(SpriteBatch b, string text, SpriteFont font, Vector2 position, Color color, float scale = 1f, float layerDepth = -1f, int horizontalShadowOffset = -1, int verticalShadowOffset = -1, int numShadows = 3)
    {
        byte gray = RgbToGray(color);
        Color shadowColor = SpriteText.color_Black;
        shadowColor *= 0.3f;

        Utility.drawTextWithColoredShadow(b, text, font, position, color, shadowColor, scale, layerDepth, horizontalShadowOffset, verticalShadowOffset, numShadows);
    }
}
