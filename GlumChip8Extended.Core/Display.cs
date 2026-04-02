using Raylib_cs;

public class Display
{
    public int Width = 64;
    public int Height = 32;
    public byte[,] pixels = new byte[128, 64];
    public const int SCALE = 10;
    public bool _drawFlag;

    public Display()
    {

    }

    public void SetResolution(bool highRes)
    {
        Width = highRes ? 128 : 64;
        Height = highRes ? 64 : 32;
        Raylib.SetWindowSize(Width * SCALE, Height * SCALE);
        Array.Clear(pixels, 0, pixels.Length);
    }

    public void Draw(bool refreshExternal = false)
    {
        int screenWidth = Raylib.GetScreenWidth();
        int screenHeight = Raylib.GetScreenHeight();

        float scaleX = (float)screenWidth / Width;
        float scaleY = (float)screenHeight / Height;

        //integer scaling
        int scale = Math.Min(screenWidth / Width, screenHeight / Height);

        //center the content
        float offsetX = (screenWidth - (Width * scale)) / 2f;
        float offsetY = (screenHeight - (Height * scale)) / 2f;

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (pixels[x, y] == 1)
                {
                    Raylib.DrawRectangle(
                        (int)(offsetX + x * scale),
                        (int)(offsetY + y * scale),
                        (int)scale,
                        (int)scale,
                        Color.White
                    );
                }
            }
        }
    }
}