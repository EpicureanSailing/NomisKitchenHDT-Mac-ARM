using System;

namespace NomisKitchenHDT.Services
{
    public class AbbreviationDisabler : IDisposable
    {
        private const string EmbeddedResourceName =
            "NomisKitchenHDT.Resources.com.community.hs.NomiHatesAbbreviation.dll";
        private const string DeployedFileName =
            "com.community.hs.NomiHatesAbbreviation.dll";

        private readonly PluginConfig _config;
        private readonly BepInExDeployer _deployer;
        public string LastError { get; private set; }

        public AbbreviationDisabler(PluginConfig config, BepInExDeployer deployer)
        {
            _config = config;
            _deployer = deployer;
        }

        public void SyncWithSetting()
        {
            LastError = _deployer.Sync(EmbeddedResourceName, DeployedFileName, _config.DisableAbbreviation);
        }

        public void Dispose() { }
    }
}
