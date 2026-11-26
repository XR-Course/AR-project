using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CampusNavigation.ARGuidance.Editor
{
    /// <summary>
    /// Draws the white UI icons (turn arrow, destination target) into PNG sprites,
    /// so the project needs no external art and the icons can be tinted in the UI.
    /// </summary>
    public static class IconSpriteGenerator
    {
        private const int Size = 256;
        private const int Supersample = 4;

        public static Sprite CreateTurnArrow(string assetPath) =>
            Create(assetPath, (x, y) =>
            {
                // Up-pointing arrow: shaft + triangular head.
                bool shaft = x >= 100 && x <= 156 && y >= 24 && y <= 150;

                float headBase = 136f, apex = 236f, halfWidth = 92f;
                bool head = y >= headBase && y <= apex &&
                            Mathf.Abs(x - 128f) <= halfWidth * (apex - y) / (apex - headBase);

                return shaft || head;
            });

        public static Sprite CreateDestinationTarget(string assetPath) =>
            Create(assetPath, (x, y) =>
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(128f, 128f));
                bool ring = distance >= 88f && distance <= 112f;
                bool dot = distance <= 46f;
                return ring || dot;
            });

        private static Sprite Create(string assetPath, Func<float, float, bool> inside)
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            var pixels = new Color32[Size * Size];
            const float step = 1f / Supersample;

            for (int py = 0; py < Size; py++)
            {
                for (int px = 0; px < Size; px++)
                {
                    int hits = 0;

                    for (int sy = 0; sy < Supersample; sy++)
                    for (int sx = 0; sx < Supersample; sx++)
                    {
                        if (inside(px + (sx + 0.5f) * step, py + (sy + 0.5f) * step))
                            hits++;
                    }

                    byte alpha = (byte)(255 * hits / (Supersample * Supersample));
                    pixels[py * Size + px] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            Directory.CreateDirectory(Path.GetDirectoryName(assetPath)!);
            File.WriteAllBytes(assetPath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }
    }
}
