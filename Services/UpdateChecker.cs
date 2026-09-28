using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NomisKitchenHDT.Utils;

namespace NomisKitchenHDT.Services
{
    public class UpdateChecker
    {
        const string LatestApi = "https://api.github.com/repos/RainWritesCode/NomisKitchenHDT/releases/latest";
        const string DownloadPrefix = "https://github.com/RainWritesCode/NomisKitchenHDT/releases/download/";
        const long MaxInstallerBytes = 100L * 1024 * 1024;

        public bool UpdateAvailable { get; private set; }
        public string LatestVersion { get; private set; }
        public string DownloadUrl { get; private set; }
        public string ReleaseUrl { get; private set; }
        public string LastError { get; private set; }

        readonly Version _current;

        public UpdateChecker(Version current)
        {
            _current = current;
        }

        WebClient MakeClient()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            var wc = new WebClient();
            wc.Headers.Add("User-Agent", "NomisKitchenHDT-UpdateCheck");
            return wc;
        }

        public async Task<bool> CheckAsync()
        {
            try
            {
                string json;
                using (var wc = MakeClient())
                    json = await wc.DownloadStringTaskAsync(LatestApi);

                var release = JObject.Parse(json);
                var tag = (string)release["tag_name"];
                ReleaseUrl = (string)release["html_url"];
                if (string.IsNullOrEmpty(tag)) return false;
                LatestVersion = tag.TrimStart('v', 'V');
                if (!Version.TryParse(LatestVersion, out var latest)) return false;

                DownloadUrl = AssetUrl(release, "NomisKitchenSetup-v" + LatestVersion + ".exe");
                UpdateAvailable = latest > _current;
                return UpdateAvailable;
            }
            catch { return false; }
        }

        static string AssetUrl(JObject release, string name)
        {
            var asset = (release["assets"] as JArray)?.OfType<JObject>().FirstOrDefault(a => (string)a["name"] == name);
            var url = (string)asset?["browser_download_url"];
            return url != null && url.StartsWith(DownloadPrefix, StringComparison.Ordinal) ? url : null;
        }

        public async Task<bool> DownloadAndRunAsync()
        {
            LastError = null;
            if (string.IsNullOrEmpty(DownloadUrl))
            {
                LastError = "This release has no installer named NomisKitchenSetup-v" + LatestVersion + ".exe.";
                return false;
            }
            try
            {
                byte[] installer;
                using (var wc = MakeClient())
                    installer = await wc.DownloadDataTaskAsync(DownloadUrl);
                if (installer.Length == 0 || installer.Length > MaxInstallerBytes)
                {
                    LastError = "The downloaded installer looks broken.";
                    return false;
                }
                var path = Path.Combine(Path.GetTempPath(), "NomisKitchenSetup-v" + LatestVersion + ".exe");
                File.WriteAllBytes(path, installer);
                Log.Info("Update v" + LatestVersion + " downloaded; starting the installer");
                Process.Start(path);
                return true;
            }
            catch (Exception ex)
            {
                LastError = "Download failed.";
                Log.Error("Update download failed", ex);
                return false;
            }
        }
    }
}
