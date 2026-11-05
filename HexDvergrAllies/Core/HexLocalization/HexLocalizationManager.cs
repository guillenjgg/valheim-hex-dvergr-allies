using System;
using System.IO;
using System.Reflection;
using HarmonyLib;

namespace HexDvergrAllies.Core.HexLocalization
{
    internal static class HexLocalizationManager
    {
        private const string EnglishResourceName = "HexDvergrAllies.Core.HexLocalization.English.csv";

        internal static string Localize(string key, params object[] arguments)
        {
            var text = Localization.instance.Localize(key).Replace("\\n", "\n");

            return arguments.Length == 0 ? text : string.Format(text, arguments);
        }

        internal static void Initialize()
        {
            var modDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Directory.CreateDirectory(Path.Combine(modDirectory, "Translations"));

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
            var modDirectory = Path.GetDirectoryName(assembly.Location);
            var translationsDirectory = Path.Combine(modDirectory, "Translations");
            var language = Localization.instance.GetSelectedLanguage();

            using (var stream = assembly.GetManifestResourceStream(EnglishResourceName))
            {
                if (stream == null)
                {
                    Plugin.Log.LogError($"Localization resource not found: {EnglishResourceName}");
                }
                else
                {
                    using (var reader = new StreamReader(stream))
                    {
                        LoadCSV(reader.ReadToEnd(), "English", "Embedded English");
                    }
                }
            }

            var languageDirectory = Path.Combine(translationsDirectory, language);

            if (!Directory.Exists(languageDirectory))
            {
                return;
            }

            foreach (var file in Directory.GetFiles(languageDirectory, "*.csv"))
            {
                try
                {
                    LoadCSV(File.ReadAllText(file), language, Path.GetFileName(file));
                }
                catch (Exception exception)
                {
                    Plugin.Log.LogError($"Failed to load localization file {file}: {exception}");
                }
            }
        }

        private static void LoadCSV(string csv, string language, string source)
        {
            var textAssetType = AccessTools.TypeByName("UnityEngine.TextAsset");
            var constructor = AccessTools.Constructor(textAssetType, new[] { typeof(string) });
            var textAsset = constructor.Invoke(new object[] { csv });

            try
            {
                var loadCsv = AccessTools.Method(typeof(Localization), "LoadCSV");
                var loaded = (bool)loadCsv.Invoke(Localization.instance, new[] { textAsset, language });

                if (loaded)
                {
                    Plugin.Log.LogInfo($"Dvergr localization loaded: {source}, Language={language}");
                }
            }
            finally
            {
                UnityEngine.Object.Destroy((UnityEngine.Object)textAsset);
            }
        }
    }
}
