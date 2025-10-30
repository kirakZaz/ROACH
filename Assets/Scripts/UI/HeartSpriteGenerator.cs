using UnityEngine;

public static class HeartSpriteGenerator
{
    public static Sprite CreatePixelHeartSprite(int pixelSize = 8, int pixelsPerUnit = 64)
    {
        string[] rows = new[]
        {
            "..XX..XX..",
            ".XXXXXXXX.",
            "XXXXXXXXXX",
            "XXXXXXXXXX",
            ".XXXXXXXX.",
            "..XXXXXX..",
            "...XXXX...",
            "....XX....",
        };

        int w = rows[0].Length;
        int h = rows.Length;

        int texW = w * pixelSize;
        int texH = h * pixelSize;

        var tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Point; // << crisp pixel edges

        Color32 on = new Color32(255, 255, 255, 255);
        Color32 off = new Color32(0, 0, 0, 0);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool filled = rows[y][x] == 'X';
                // paint block of pixelSize x pixelSize
                for (int dy = 0; dy < pixelSize; dy++)
                for (int dx = 0; dx < pixelSize; dx++)
                {
                    int px = x * pixelSize + dx;
                    int py = (h - 1 - y) * pixelSize + dy; // flip Y into texture space
                    tex.SetPixel(px, py, filled ? on : off);
                }
            }
        }

        tex.Apply();
        var rect = new Rect(0, 0, texW, texH);
        var sprite = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), pixelsPerUnit);
        return sprite;
    }
}
