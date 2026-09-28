using System;
using System.IO;

namespace NomisKitchenHDT.Utils
{
    internal static class HearthstonePaths
    {
        internal static string InstallDir(PluginConfig config)
        {
            if (IsInstall(config?.HearthstoneDir)) return config.HearthstoneDir;
            try
            {
                var hdt = Hearthstone_Deck_Tracker.Config.Instance.HearthstoneDirectory;
                if (IsInstall(hdt)) return hdt;
            }
            catch { }
            foreach (var candidate in new[] { @"C:\Program Files (x86)\Hearthstone", @"C:\Program Files\Hearthstone" })
                if (IsInstall(candidate)) return candidate;
            return null;
        }

        static bool IsInstall(string dir) =>
            !string.IsNullOrEmpty(dir) && Directory.Exists(dir) && File.Exists(Path.Combine(dir, "Hearthstone.exe"));
    }
}
