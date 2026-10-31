using System;
using System.IO;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace HexDvergrAllies.Assets
{
    internal static class AssetHelper
    {
        internal static Sprite DvergrPortrait { get; private set; }

        internal static void Initialize()
        {
            DvergrPortrait = LoadSprite("HexDvergrAllies.Assets.DvergrPortrait.png");
        }

        private static Sprite LoadSprite(string resourceName)
        {
            Assembly assembly = typeof(AssetHelper).Assembly;

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    Plugin.Log.LogError($"[AssetHelper] Embedded resource not found: {resourceName}");

                    return null;
                }

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    stream.CopyTo(memoryStream);

                    Texture2D texture = new Texture2D(2, 2);

                    Type imageConversionType = Type.GetType("UnityEngine.ImageConversion, UnityEngine.ImageConversionModule");

                    if (imageConversionType == null)
                    {
                        Plugin.Log.LogError("[AssetHelper] Unity ImageConversion type not found.");

                        UnityEngine.Object.Destroy(texture);
                        return null;
                    }

                    MethodInfo loadImage = AccessTools.Method(imageConversionType, "LoadImage", new[] { typeof(Texture2D), typeof(byte[]) });

                    if (loadImage == null)
                    {
                        Plugin.Log.LogError("[AssetHelper] ImageConversion.LoadImage method not found.");

                        UnityEngine.Object.Destroy(texture);
                        return null;
                    }

                    try
                    {
                        bool success = (bool)loadImage.Invoke(null, new object[] { texture, memoryStream.ToArray() });

                        if (!success)
                        {
                            Plugin.Log.LogError($"[AssetHelper] Failed to decode image: {resourceName}");

                            UnityEngine.Object.Destroy(texture);
                            return null;
                        }
                    }
                    catch (Exception exception)
                    {
                        Plugin.Log.LogError($"[AssetHelper] Error loading image: {exception}");

                        UnityEngine.Object.Destroy(texture);
                        return null;
                    }

                    texture.name = "HexDvergr_DvergrPortrait";

                    Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

                    Plugin.Log.LogInfo($"[AssetHelper] Loaded sprite: {resourceName}");

                    return sprite;
                }
            }
        }
    }
}
