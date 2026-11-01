using System.IO;
using System.Reflection;
using HarmonyLib;

namespace HexDvergrAllies.Core.HexLocalization
{
    internal static class HexLocalizationManager
    {
        private const string EnglishResourceName = "HexDvergrAllies.Core.HexLocalization.English.csv";

        internal static void Initialize()
        {
            Localization.OnLanguageChange += OnLanguageChanged;
            LoadTranslations();
        }

        internal static void Shutdown()
        {
            Localization.OnLanguageChange -= OnLanguageChanged;
        }

        private static void OnLanguageChanged()
        {
            LoadTranslations();
        }

        private static void LoadTranslations()
        {
            var assembly = Assembly.GetExecutingAssembly();

            using (var stream = assembly.GetManifestResourceStream(EnglishResourceName))
            {
                if (stream == null)
                {
                    Plugin.Log.LogError($"Localization resource not found: {EnglishResourceName}");
                    return;
                }

                using (var reader = new StreamReader(stream))
                {
                    var csv = reader.ReadToEnd();
                    var textAssetType = AccessTools.TypeByName("UnityEngine.TextAsset");
                    var constructor = AccessTools.Constructor(textAssetType, new[] { typeof(string) });
                    var textAsset = constructor.Invoke(new object[] { csv });

                    try
                    {
                        var language = Localization.instance.GetSelectedLanguage();
                        var loadCsv = AccessTools.Method(typeof(Localization), "LoadCSV");
                        var loaded = (bool)loadCsv.Invoke(Localization.instance, new[] { textAsset, language });

                        Plugin.Log.LogInfo($"Dvergr localization loaded: {language}, Success={loaded}");
                    }
                    finally
                    {
                        UnityEngine.Object.Destroy((UnityEngine.Object)textAsset);
                    }
                }
            }
        }
    }
}