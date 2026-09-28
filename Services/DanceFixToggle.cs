using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using NomisKitchenHDT.Utils;

namespace NomisKitchenHDT.Services
{
    public class DanceFixToggle : IDisposable
    {
        private const string EmbeddedResourceName =
            "NomisKitchenHDT.Resources.com.community.hs.NomiCantDance.dll";
        private const string DeployedFileName =
            "com.community.hs.NomiCantDance.dll";
        private const string ConfigFileName =
            "com.community.hs.NomiCantDance.cfg";

        private static readonly Regex LogFixesOff =
            new Regex(@"(?im)^([ \t]*LogFixes[ \t]*=[ \t]*)(?![ \t]*true[ \t]*\r?$)[^\r\n]*");

        private readonly PluginConfig _config;
        private readonly BepInExDeployer _deployer;
        public string LastError { get; private set; }

        public DanceFixToggle(PluginConfig config, BepInExDeployer deployer)
        {
            _config = config;
            _deployer = deployer;
        }

        public void SyncWithSetting()
        {
            LastError = _deployer.Sync(EmbeddedResourceName, DeployedFileName, _config.FixMinionDance);
            if (_config.FixMinionDance) EnsureLogFixes();
        }

        private void EnsureLogFixes()
        {
            try
            {
                var plugins = _deployer.PluginsFolder();
                if (plugins == null) return;
                var path = Path.GetFullPath(Path.Combine(plugins, "..", "config", ConfigFileName));
                if (!File.Exists(path)) return;
                var text = File.ReadAllText(path);
                if (!LogFixesOff.IsMatch(text)) return;
                File.WriteAllText(path, LogFixesOff.Replace(text, "${1}true"), new UTF8Encoding(false));
                Log.Info("Turned LogFixes back on in " + path);
            }
            catch (Exception ex)
            {
                Log.Warn("Could not turn LogFixes on: " + ex.Message);
            }
        }

        public void Dispose() { }
    }
}
