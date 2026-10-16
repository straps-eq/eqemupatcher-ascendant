using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EQEmu_Patcher
{
    // Keeps a live client's LaunchPad.exe renamed so it can't update past the build the server supports,
    // and runs it on request when the server moves to a newer live build
    class LaunchPadLibrary
    {
        static string Dir => Path.GetDirectoryName(Application.ExecutablePath);
        static string Enabled => Path.Combine(Dir, "LaunchPad.exe");
        static string Disabled => Path.Combine(Dir, "LaunchPad.exe.disabled");

        // Renames LaunchPad.exe to LaunchPad.exe.disabled; returns an error message or ""
        public static string Disable()
        {
            try
            {
                if (!File.Exists(Enabled)) return "";
                if (File.Exists(Disabled)) File.Delete(Disabled);
                File.Move(Enabled, Disabled);
                return "";
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public static bool Exists()
        {
            return File.Exists(Enabled) || File.Exists(Disabled);
        }

        // Restores LaunchPad.exe, runs it and waits until every LaunchPad process has exited
        // (it can restart itself while self-updating), then disables it again
        public static async Task<string> RunAndWait()
        {
            try
            {
                if (!File.Exists(Enabled) && File.Exists(Disabled)) File.Move(Disabled, Enabled);
                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = Enabled,
                    WorkingDirectory = Dir
                });
                if (process == null) return "LaunchPad did not start";
                await Task.Run(() => process.WaitForExit());
                int quietChecks = 0;
                while (quietChecks < 3)
                {
                    await Task.Delay(2000);
                    quietChecks = (Process.GetProcessesByName("LaunchPad").Length == 0) ? quietChecks + 1 : 0;
                }
            }
            catch (Exception e)
            {
                Disable();
                return e.Message;
            }
            return Disable();
        }
    }
}
