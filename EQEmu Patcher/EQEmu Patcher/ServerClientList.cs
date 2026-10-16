using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace EQEmu_Patcher
{
    // clients.yml, hosted next to the client folders at FILELIST_URL. It lets the server add clients
    // (e.g. the monthly live build) without rebuilding the patcher. Built-in clients are matched first.
    public class ServerClientListFile
    {
        public List<ServerClientEntry> clients { get; set; }
    }

    public class ServerClientEntry
    {
        public string name { get; set; } // shown in the title bar, e.g. "Live"
        public string folder { get; set; } // client folder, e.g. "live" -> live/filelist_live.yml
        public List<string> hashes { get; set; } // eqgame.exe MD5s that use this folder
        public string supported { get; set; } // optional: the build the server speaks now; older listed builds are updated through LaunchPad

        public bool Matches(string hash)
        {
            if (hashes == null || hash == null) return false;
            foreach (var entryHash in hashes)
            {
                if (entryHash != null && entryHash.Trim().Equals(hash, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        public bool IsSupported(string hash)
        {
            return string.IsNullOrWhiteSpace(supported) || supported.Trim().Equals(hash, StringComparison.OrdinalIgnoreCase);
        }
    }

    class ServerClientList
    {
        static readonly Regex folderPattern = new Regex("^[a-z0-9_-]+$");

        // Returns the entry whose hashes contain eqgame.exe's hash, or null if none does or the list can't be read
        public static async Task<ServerClientEntry> Find(CancellationTokenSource cts, string filelistUrl, string hash)
        {
            string url = $"{filelistUrl}clients.yml";
            ServerClientListFile list;
            try
            {
                var data = await UtilityLibrary.Download(cts, url);
                var deserializer = new DeserializerBuilder().WithNamingConvention(new CamelCaseNamingConvention()).IgnoreUnmatchedProperties().Build();
                list = deserializer.Deserialize<ServerClientListFile>(Encoding.UTF8.GetString(data));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"No server client list at {url}: {ex.Message}");
                return null;
            }
            if (list == null || list.clients == null) return null;

            foreach (var entry in list.clients)
            {
                if (entry == null || entry.hashes == null || entry.folder == null) continue;
                // The folder becomes part of a URL, so only plain names are allowed
                if (!folderPattern.IsMatch(entry.folder)) continue;
                if (entry.Matches(hash))
                {
                    if (string.IsNullOrWhiteSpace(entry.name)) entry.name = entry.folder;
                    return entry;
                }
            }
            return null;
        }
    }
}
