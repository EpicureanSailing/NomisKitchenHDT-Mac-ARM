using System;
using System.Windows.Controls;
using Hearthstone_Deck_Tracker.API;
using Hearthstone_Deck_Tracker.Plugins;
using NomisKitchen.Reports;
using NomisKitchenHDT.Services;
using NomisKitchenHDT.UI;
using NomisKitchenHDT.Utils;

namespace NomisKitchenHDT
{
    public class Plugin : IPlugin
    {
        public string Name => "Nomi's Kitchen";
        public string Description => "Battlegrounds QoL: disable board number abbreviation, live APM overlay, experimental minion dance fix.";
        public string Author => "RainWritesCode";
        public string ButtonText => "Settings";
        public Version Version => new Version(1, 0, 8);
        public MenuItem MenuItem => _menuItem;

        MenuItem _menuItem;
        internal PluginConfig Config;
        internal ApmTracker Tracker;
        internal ApmOverlay Overlay;
        internal AbbreviationDisabler Disabler;
        internal DanceFixToggle DanceFix;
        internal ApmProviderInstaller ApmInstaller;
        internal BepInExDeployer Deployer;
        internal UpdateChecker Updater;
        internal Reporter Reporter;

        internal static Plugin Instance;
        static int _hookGeneration;

        public void OnLoad()
        {
            Instance = this;
            Config = PluginConfig.Load();
            Log.Info("=== Nomi's Kitchen v" + Version + " loading ===");
            Log.Info("Config: HearthstoneDir='" + Config.HearthstoneDir + "' ShowApmOverlay=" + Config.ShowApmOverlay + " DisableAbbreviation=" + Config.DisableAbbreviation + " FixMinionDance=" + Config.FixMinionDance + " LockOverlay=" + Config.LockOverlay + " AutoCheckUpdates=" + Config.AutoCheckUpdates + " ShareGameLogs=" + Config.ShareGameLogs + " Log=" + Log.FilePath);

            Deployer = new BepInExDeployer(Config);

            Disabler = new AbbreviationDisabler(Config, Deployer);
            Disabler.SyncWithSetting();

            DanceFix = new DanceFixToggle(Config, Deployer);
            DanceFix.SyncWithSetting();

            ApmInstaller = new ApmProviderInstaller(Deployer);
            ApmInstaller.EnsureInstalled();

            Tracker = new ApmTracker();
            Tracker.Start();

            Overlay = new ApmOverlay(Config, Tracker);
            Log.Info("Overlay created; ShowApmOverlay=" + Config.ShowApmOverlay);
            if (Config.ShowApmOverlay)
                Overlay.Show();

            _menuItem = new MenuItem { Header = "Nomi's Kitchen" };
            _menuItem.Click += (_, _1) => OpenSettings();

            Reporter = new Reporter(new ReportSettings
            {
                Version = Version,
                ShareGameLogs = () => Config.ShareGameLogs,
                HearthstoneDir = () => Config.HearthstoneDir,
                FixMinionDance = () => Config.FixMinionDance,
                DisableAbbreviation = () => Config.DisableAbbreviation,
                ShowApmOverlay = () => Config.ShowApmOverlay,
                PluginLogPath = Log.FilePath,
                Info = Log.Info,
                Warn = Log.Warn,
                Error = (message, ex) => Log.Error(message, ex),
            });
            Reporter.Start();
            HookGameEvents();

            Updater = new UpdateChecker(Version);
            if (Config.AutoCheckUpdates)
                _ = CheckAndPromptAsync();
        }

        async System.Threading.Tasks.Task CheckAndPromptAsync()
        {
            var available = await Updater.CheckAsync();
            Log.Info(available ? ("Update available: v" + Updater.LatestVersion) : "Update check: up to date, or check failed");
            if (!available) return;
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher == null) return;
            dispatcher.BeginInvoke(new Action(async () =>
            {
                var result = System.Windows.MessageBox.Show(
                    $"Nomi's Kitchen v{Updater.LatestVersion} is available (you have v{Version}).\n\nDownload and install now? Hearthstone Deck Tracker will close so the update can apply.",
                    "Nomi's Kitchen update",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Information);
                if (result != System.Windows.MessageBoxResult.Yes) return;
                var ok = await Updater.DownloadAndRunAsync();
                if (ok) System.Windows.Application.Current.Shutdown();
                else System.Windows.MessageBox.Show((Updater.LastError ?? "Update download failed.") + " Get it from the Releases page instead.",
                    "Nomi's Kitchen", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }));
        }

        void HookGameEvents()
        {
            int generation = ++_hookGeneration;
            GameEvents.OnGameStart.Add(() => { if (generation == _hookGeneration) Reporter?.OnGameStart(); });
            GameEvents.OnGameEnd.Add(() => { if (generation == _hookGeneration) Reporter?.OnGameEnd(); });
        }

        public void OnUnload()
        {
            _hookGeneration++;
            Reporter?.Stop();
            Reporter = null;
            Overlay?.Hide();
            Log.Info("Nomi's Kitchen unloading"); Tracker?.Stop();
            Disabler?.Dispose();
            DanceFix?.Dispose();
            Deployer?.Dispose();
            Config?.Save();
        }

        public void OnUpdate() { }
        public void OnButtonPress() => OpenSettings();

        void OpenSettings()
        {
            Log.Info("Settings opened");
            var win = new SettingsWindow(Config, Updater);
            win.StyleApplied += () => Overlay?.ApplyStyle();
            win.Closed += (_, _1) =>
            {
                Config.Save();
                Disabler.SyncWithSetting();
                DanceFix.SyncWithSetting();
                if (Config.ShowApmOverlay) Overlay.Show(); else Overlay.Hide();
                Overlay?.ApplyStyle();
            };
            win.Show();
        }
    }
}
