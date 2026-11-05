using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using HexDvergrAllies.Assets;
using HexDvergrAllies.Core.HexLocalization;
using HexDvergrAllies.Core.Trading;
using System;
using System.IO;
using System.Reflection;

namespace HexDvergrAllies
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        internal const string PluginGuid = "com.hex.dvergr.allies";
        private const string PluginName = "HexDvergrAllies";
        private const string PluginVersion = "0.0.2";

        private Harmony _harmonyInstance;
        private FileSystemWatcher _configFileWatcher;

        internal static ManualLogSource Log;
        internal static Plugin Instance;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            AssetHelper.Initialize();

            Config.SaveOnConfigSet = false;

            HexDvergrAlliesConfiguration.Init(Config);
            HexLocalizationManager.Initialize();

            Config.Save();
            Config.SaveOnConfigSet = true;

            SetupConfigWatcher();

            _harmonyInstance = new Harmony(PluginGuid);
            _harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());

            DvergrTradeTerminalCommands.Register();

            Log.LogInfo($"{PluginName} v{PluginVersion} loaded.");
        }

        private void SetupConfigWatcher()
        {
            _configFileWatcher = new FileSystemWatcher(Path.GetDirectoryName(Config.ConfigFilePath), Path.GetFileName(Config.ConfigFilePath));
            _configFileWatcher.Changed += OnConfigFileChanged;
            _configFileWatcher.Created += OnConfigFileChanged;
            _configFileWatcher.Renamed += OnConfigFileChanged;
            _configFileWatcher.IncludeSubdirectories = true;
            _configFileWatcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            _configFileWatcher.EnableRaisingEvents = true;
        }

        private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(Config.ConfigFilePath))
            {
                return;
            }

            try
            {
                Log.LogDebug("Attempting to reload configuration...");
                Config.Reload();
            }
            catch (Exception exception)
            {
                Log.LogError($"There was an issue loading {Path.GetFileName(Config.ConfigFilePath)}: {exception}");
            }
        }

        private void OnDestroy()
        {
            if (_configFileWatcher != null)
            {
                _configFileWatcher.EnableRaisingEvents = false;
                _configFileWatcher.Changed -= OnConfigFileChanged;
                _configFileWatcher.Created -= OnConfigFileChanged;
                _configFileWatcher.Renamed -= OnConfigFileChanged;
                _configFileWatcher.Dispose();
                _configFileWatcher = null;
            }

            Config.Save();

            _harmonyInstance?.UnpatchSelf();
            _harmonyInstance = null;

            Log.LogInfo($"{PluginName} v{PluginVersion} unloaded.");

            HexLocalizationManager.Shutdown();

            Instance = null;
            Log = null;
        }
    }
}