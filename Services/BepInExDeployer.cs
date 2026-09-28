using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using NomisKitchenHDT.Utils;

namespace NomisKitchenHDT.Services
{
    public sealed class BepInExDeployer : IDisposable
    {
        static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(15);

        readonly PluginConfig _config;
        readonly object _lock = new object();
        readonly Dictionary<string, Job> _pending = new Dictionary<string, Job>(StringComparer.OrdinalIgnoreCase);
        System.Threading.Timer _retry;

        public BepInExDeployer(PluginConfig config)
        {
            _config = config;
        }

        public string PluginsFolder()
        {
            var candidates = new List<string>();
            if (!string.IsNullOrEmpty(_config.HearthstoneDir)) candidates.Add(_config.HearthstoneDir);
            var install = HearthstonePaths.InstallDir(_config);
            if (install != null) candidates.Add(install);
            candidates.Add(@"C:\Program Files (x86)\Hearthstone");
            candidates.Add(@"C:\Program Files\Hearthstone");
            foreach (var dir in candidates)
            {
                var plugins = Path.Combine(dir, "BepInEx", "plugins");
                if (Directory.Exists(plugins)) return plugins;
            }
            return null;
        }

        public string Sync(string resourceName, string fileName, bool wanted)
        {
            var folder = PluginsFolder();
            if (folder == null)
            {
                Log.Warn(fileName + ": no Hearthstone BepInEx\\plugins folder found (config HearthstoneDir='" + _config.HearthstoneDir + "').");
                return null;
            }
            var job = new Job { Resource = resourceName, Target = Path.Combine(folder, fileName), Wanted = wanted };
            lock (_lock)
            {
                _pending.Remove(fileName);
                var outcome = Apply(job);
                if (outcome == Outcome.Locked)
                {
                    _pending[fileName] = job;
                    StartRetry();
                    return wanted
                        ? "Hearthstone is using the old " + fileName + ". It updates by itself once Hearthstone is closed."
                        : "Hearthstone is using " + fileName + ". It is removed by itself once Hearthstone is closed.";
                }
                return outcome == Outcome.Failed ? "Could not change " + fileName + ". Close Hearthstone and try again." : null;
            }
        }

        public bool IsPending(string fileName)
        {
            lock (_lock) return _pending.ContainsKey(fileName);
        }

        Outcome Apply(Job job)
        {
            try
            {
                bool present = File.Exists(job.Target);
                if (!job.Wanted)
                {
                    if (!present) return Outcome.Done;
                    File.Delete(job.Target);
                    Log.Info("Removed " + job.Target);
                    ClearChainloaderCache(job.Target);
                    return Outcome.Done;
                }
                var embedded = ReadEmbedded(job.Resource);
                if (embedded == null)
                {
                    Log.Error("Embedded resource missing: " + job.Resource);
                    return Outcome.Failed;
                }
                if (present && SameBytes(job.Target, embedded))
                {
                    Log.Info("Up to date: " + job.Target);
                    return Outcome.Done;
                }
                File.WriteAllBytes(job.Target, embedded);
                Log.Info((present ? "Updated " : "Installed ") + job.Target);
                ClearChainloaderCache(job.Target);
                return Outcome.Done;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                if (HearthstoneRunning())
                {
                    Log.Warn("Hearthstone has " + Path.GetFileName(job.Target) + " open; retrying once it closes.");
                    return Outcome.Locked;
                }
                Log.Error("Could not write " + job.Target, ex);
                return Outcome.Failed;
            }
        }

        void StartRetry()
        {
            if (_retry != null) return;
            _retry = new System.Threading.Timer(_ => RetryPending(), null, RetryInterval, RetryInterval);
        }

        void RetryPending()
        {
            lock (_lock)
            {
                if (_pending.Count == 0 || HearthstoneRunning()) return;
                foreach (var name in new List<string>(_pending.Keys))
                {
                    var outcome = Apply(_pending[name]);
                    if (outcome != Outcome.Locked) _pending.Remove(name);
                }
                if (_pending.Count == 0)
                {
                    _retry?.Dispose();
                    _retry = null;
                }
            }
        }

        static void ClearChainloaderCache(string target)
        {
            try
            {
                var cache = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(target) ?? "", "..", "cache", "chainloader_typeloader.dat"));
                if (File.Exists(cache)) File.Delete(cache);
            }
            catch (Exception ex) { Log.Warn("Could not clear the BepInEx type cache: " + ex.Message); }
        }

        static bool HearthstoneRunning()
        {
            var processes = Process.GetProcessesByName("Hearthstone");
            foreach (var p in processes) p.Dispose();
            return processes.Length > 0;
        }

        static byte[] ReadEmbedded(string resourceName)
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            if (stream == null) return null;
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            return buffer.ToArray();
        }

        static bool SameBytes(string path, byte[] expected)
        {
            if (new FileInfo(path).Length != expected.Length) return false;
            var actual = File.ReadAllBytes(path);
            for (int i = 0; i < actual.Length; i++)
                if (actual[i] != expected[i]) return false;
            return true;
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _retry?.Dispose();
                _retry = null;
            }
        }

        enum Outcome { Done, Locked, Failed }

        sealed class Job
        {
            public string Resource;
            public string Target;
            public bool Wanted;
        }
    }
}
