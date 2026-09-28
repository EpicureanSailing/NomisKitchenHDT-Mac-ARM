namespace NomisKitchenHDT.Services
{
    public class ApmProviderInstaller
    {
        const string EmbeddedResourceName =
            "NomisKitchenHDT.Resources.com.community.hs.NomisKitchenApm.dll";
        const string DeployedFileName =
            "com.community.hs.NomisKitchenApm.dll";

        readonly BepInExDeployer _deployer;
        public string LastError { get; private set; }

        public ApmProviderInstaller(BepInExDeployer deployer)
        {
            _deployer = deployer;
        }

        public void EnsureInstalled()
        {
            LastError = _deployer.Sync(EmbeddedResourceName, DeployedFileName, true);
        }
    }
}
