using System.Runtime.InteropServices;
using PBG.MathLibrary;
using StbImageSharp;

namespace PBG.Graphics
{
    public static class TextureData
    {
        public static List<byte[]> SplitTextureAtlasCellSize(string path, int width, int height, bool flipped = false)
        {
            ImageResult atlas = ImageResult.FromStream(File.OpenRead(path), ColorComponents.RedGreenBlueAlpha);

            int cols = atlas.Width / width;
            int rows = atlas.Height / height;

            return SplitTextureAtlas(cols, rows, width, height, atlas, flipped);
        }

        public static List<byte[]> SplitTextureAtlasAtlasSize(string path, int cols, int rows, out int newWidth, out int newHeight, bool flipped = false)
        {
            ImageResult atlas = ImageResult.FromStream(File.OpenRead(path), ColorComponents.RedGreenBlueAlpha);

            newWidth = atlas.Width / cols;
            newHeight = atlas.Height / rows;

            return SplitTextureAtlas(cols, rows, newWidth, newHeight, atlas, flipped);
        }

        public static List<byte[]> SplitTextureAtlas(int cols, int rows, int width, int height, ImageResult atlas, bool flipped = false)
        {
            List<byte[]> textures = [];

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    int effectiveRow = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? (flipped ? (rows - row - 1) : row) : (flipped ? (rows - row - 1) : row);
                    byte[] subImage = ExtractSubImage(atlas, col * width, effectiveRow * height, width, height);
                    textures.Add(subImage);
                }
            }

            return textures;
        }

        public static List<Vector3> GetAverageColors(string path, int width, int height)
        {
            ImageResult atlas = ImageResult.FromStream(File.OpenRead(path), ColorComponents.RedGreenBlueAlpha);

            int atlasWidth = atlas.Width;
            int atlasHeight = atlas.Height;

            int cols = atlasWidth / width;
            int rows = atlasHeight / height;

            List<Vector3> colors = [];

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    Vector3 avgColor = ExtractAverageColor(atlas, col * width, row * height, width, height);
                    colors.Add(avgColor);
                }
            }

            return colors;
        }

        private static byte[] ExtractSubImage(ImageResult atlas, int startX, int startY, int width, int height)
        {
            byte[] subImage = new byte[width * height * 4];
            byte[] data = atlas.Data;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int atlasIndex = ((startY + y) * atlas.Width + (startX + x)) * 4;
                    int subImageIndex = (y * width + x) * 4;

                    Array.Copy(data, atlasIndex, subImage, subImageIndex, 4);
                }
            }

            return subImage;
        }

        private static Vector3 ExtractAverageColor(ImageResult atlas, int startX, int startY, int width, int height)
        {
            Vector3 avgColor = Vector3.Zero;
            byte[] data = atlas.Data;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int atlasIndex = ((startY + y) * atlas.Width + (startX + x)) * 4;
                    byte r = data[atlasIndex];
                    byte g = data[atlasIndex + 1];
                    byte b = data[atlasIndex + 2];

                    avgColor += new Vector3(r, g, b);
                }
            }

            avgColor /= width * height;

            return avgColor / 255.0f;
        }
    }
}