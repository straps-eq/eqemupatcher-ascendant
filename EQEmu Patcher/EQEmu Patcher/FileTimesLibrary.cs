using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace EQEmu_Patcher
{
    // filetimes.yml, hosted next to the client's filelist (e.g. tob/filetimes.yml). Some files must carry an exact
    // "last modified" time: the client reports it and the server compares it (e.g. the achievement files and the
    // Character:AchievementFileTime rule). The patcher only compares contents, so it resets those times itself.
    public class FileTimesFile
    {
        public List<FileTimeGroup> times { get; set; }
    }

    public class FileTimeGroup
    {
        public long time { get; set; } // unix time, whole seconds, UTC
        public List<string> files { get; set; } // paths relative to the EverQuest folder
    }

    class FileTimesLibrary
    {
        static FileTimesFile fileTimes;

        // Downloads {suffix}/filetimes.yml; a missing or unreadable file just means there are no times to set
        public static async Task Load(CancellationTokenSource cts, string filelistUrl, string suffix)
        {
            string url = $"{filelistUrl}{suffix}/filetimes.yml";
            try
            {
                var data = await UtilityLibrary.Download(cts, url);
                var deserializer = new DeserializerBuilder().WithNamingConvention(new CamelCaseNamingConvention()).IgnoreUnmatchedProperties().Build();
                fileTimes = deserializer.Deserialize<FileTimesFile>(Encoding.UTF8.GetString(data));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"No file times at {url}: {ex.Message}");
                fileTimes = null;
            }
        }

        // Sets each listed file that exists to its time, leaving the contents alone
        public static void Apply()
        {
            if (fileTimes == null || fileTimes.times == null) return;
            var basePath = Path.GetDirectoryName(Application.ExecutablePath);
            foreach (var group in fileTimes.times)
            {
                if (group == null || group.files == null || group.time <= 0) continue;
                var time = DateTimeOffset.FromUnixTimeSeconds(group.time).UtcDateTime;
                foreach (var name in group.files)
                {
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    var path = basePath + "\\" + name.Trim().Replace("/", "\\");
                    if (!UtilityLibrary.IsPathChild(path) || !File.Exists(path)) continue;
                    try
                    {
                        if (File.GetLastWriteTimeUtc(path) == time) continue;
                        File.SetLastWriteTimeUtc(path, time);
                        Console.WriteLine($"Set {name} modified time to {group.time}");
                    }
                    catch (Exception ex)
                    {
                        StatusLibrary.Log($"Couldn't set the modified time of {name}: {ex.Message}");
                    }
                }
            }
        }
    }
}
