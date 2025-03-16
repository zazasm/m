using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace Project_Terror_v2.Role
{
    public enum CompressBitmapColor : byte
    {
        Black = 0, // 0x0
        Blue = 1, // 0xff
        Green = 2, // 0xff00
        Red = 3, // 0xff0000
        White = 4 // 0xffffff
    }

    /// <summary>
    /// A compressed bitmap only supports the colors:
    /// white, black, red, green, and blue. In doing so, colors can be stored
    /// as a byte (8-bit integer), rather than a uint (32-bit integer).
    /// </summary>
    public class CompressedBitmap
    {
        private CompressBitmapColor[,] Coordinates;
        public readonly int MaxX;
        public readonly int MaxY;
        public static CompressBitmapColor Translate(uint dwColor)
        {
            switch (dwColor)
            {
                case 0xFF000000: return CompressBitmapColor.Black;
                case 0xFFFFFFFF: return CompressBitmapColor.White;
                case 0xFFFF0000: return CompressBitmapColor.Red;
                case 0xFF00FF00: return CompressBitmapColor.Green;
                case 0xFF0000FF: return CompressBitmapColor.Blue;
            }
            throw new ArgumentException("dwColor");
        }
        public CompressedBitmap(string FileName)
        {
            using (Bitmap bmp = new Bitmap(FileName))
            {
                MaxX = bmp.Width;
                MaxY = bmp.Height;
                Coordinates = new CompressBitmapColor[MaxX, MaxY];
                for (int x = 0; x < bmp.Width; x++)
                {
                    for (int y = 0; y < bmp.Height; y++)
                    {
                        Coordinates[x, y] = Translate((uint)bmp.GetPixel(x, y).ToArgb());
                    }
                }
            }
        }
        public CompressBitmapColor this[int x, int y]
        {
            get
            {
                if (x < MaxX && y < MaxY)
                    return Coordinates[x, y];
                else
                    return 0;
            }
        }
    }
}
