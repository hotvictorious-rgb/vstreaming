using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace VictoriousStreamingStudio
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
            Directory.SetCurrentDirectory(appDir);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string nodePath = FindPortableOrSystemNode(appDir);
            if (string.IsNullOrEmpty(nodePath))
            {
                MessageBox.Show(
                    "The runtime engine (node.exe) was not found in 'runtime\\node' or on your system.\n\nPlease ensure the runtime folder is intact.",
                    "Victorious Streaming Studio",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return;
            }

            try
            {
                Application.Run(new StudioContext(appDir, nodePath));
            }
            catch (Exception ex)
            {
                File.WriteAllText(Path.Combine(appDir, "crash.log"), "Studio Crash:\n" + ex.ToString());
            }
        }

        public static string FindPortableOrSystemNode(string appDir)
        {
            // 1. Check bundled runtime folder (100% portable)
            string localRuntime = Path.Combine(appDir, @"runtime\node\node.exe");
            if (File.Exists(localRuntime)) return localRuntime;

            // 2. Check local bin folder
            string localBin = Path.Combine(appDir, @"bin\node.exe");
            if (File.Exists(localBin)) return localBin;

            // 3. Fallback to system installations
            string[] searchPaths = new string[]
            {
                @"C:\Program Files\nodejs\node.exe",
                @"C:\Program Files (x86)\nodejs\node.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\node\node.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"npm\node.exe")
            };

            foreach (string p in searchPaths)
            {
                if (File.Exists(p)) return p;
            }

            // 4. Try 'where node'
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "where.exe",
                    Arguments = "node",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };
                using (Process p = Process.Start(psi))
                {
                    string outStr = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();
                    if (!string.IsNullOrEmpty(outStr))
                    {
                        string[] lines = outStr.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        if (lines.Length > 0 && File.Exists(lines[0].Trim()))
                        {
                            return lines[0].Trim();
                        }
                    }
                }
            }
            catch { }

            return null;
        }

        public static string FindBrowser()
        {
            string[] browsers = new string[]
            {
                @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
                @"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
                @"C:\Program Files\Google\Chrome\Application\chrome.exe",
                @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe"
            };

            foreach (string b in browsers)
            {
                if (File.Exists(b)) return b;
            }

            return null;
        }
    }

    public class StudioContext : ApplicationContext
    {
        private NotifyIcon trayIcon;
        private Process nodeProcess;
        private string appDir;
        private string nodePath;

        private int httpPort = 3000;
        private int httpsPort = 3443;
        private string localIp = "127.0.0.1";
        private string sessionPin = "";
        private bool isServerReady = false;

        public StudioContext(string appDir, string nodePath)
        {
            this.appDir = appDir;
            this.nodePath = nodePath;

            SetupTray();
            StartServerProcess();

            Thread monitorThread = new Thread(() =>
            {
                int waited = 0;
                while (!isServerReady && waited < 15000)
                {
                    Thread.Sleep(250);
                    waited += 250;
                }

                LaunchDirectorWindow();
            });
            monitorThread.IsBackground = true;
            monitorThread.Start();
        }

        private void SetupTray()
        {
            trayIcon = new NotifyIcon();
            trayIcon.Text = "Victorious Streaming Studio (Starting...)";
            trayIcon.Icon = SystemIcons.Application;

            ContextMenu menu = new ContextMenu();
            menu.MenuItems.Add(new MenuItem("👑 Victorious Streaming Studio") { Enabled = false });
            menu.MenuItems.Add(new MenuItem("-"));
            menu.MenuItems.Add("🎛️ Open Director Command Center", (s, e) => LaunchDirectorWindow());
            menu.MenuItems.Add("📱 Copy Camera Connect Link", (s, e) => CopyCameraLink());
            menu.MenuItems.Add("🎬 Copy OBS Program URL", (s, e) => CopyObsUrl());
            menu.MenuItems.Add(new MenuItem("-"));
            menu.MenuItems.Add("🛑 Stop Studio & Exit Safely", (s, e) => Shutdown());

            trayIcon.ContextMenu = menu;
            trayIcon.Visible = true;
            trayIcon.DoubleClick += (s, e) => LaunchDirectorWindow();
        }

        private void StartServerProcess()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = nodePath,
                    Arguments = "server.js",
                    WorkingDirectory = appDir,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                nodeProcess = new Process();
                nodeProcess.StartInfo = psi;
                nodeProcess.EnableRaisingEvents = true;

                nodeProcess.OutputDataReceived += (s, e) =>
                {
                    if (string.IsNullOrEmpty(e.Data)) return;

                    // Parse server ready signal:
                    // [STUDIO_SERVER_READY] HTTP=3000 HTTPS=3443 IP=192.168.1.100 PIN=1234
                    if (e.Data.Contains("[STUDIO_SERVER_READY]"))
                    {
                        Match mHttp = Regex.Match(e.Data, @"HTTP=(\d+)");
                        Match mHttps = Regex.Match(e.Data, @"HTTPS=(\d+)");
                        Match mIp = Regex.Match(e.Data, @"IP=([0-9.]+)");
                        Match mPin = Regex.Match(e.Data, @"PIN=(\d+)");

                        if (mHttp.Success) int.TryParse(mHttp.Groups[1].Value, out httpPort);
                        if (mHttps.Success) int.TryParse(mHttps.Groups[1].Value, out httpsPort);
                        if (mIp.Success) localIp = mIp.Groups[1].Value;
                        if (mPin.Success) sessionPin = mPin.Groups[1].Value;

                        isServerReady = true;

                        if (trayIcon != null)
                        {
                            trayIcon.Text = string.Format("Victorious Studio (Port {0} • PIN {1})", httpPort, sessionPin);
                            trayIcon.ShowBalloonTip(
                                3000,
                                "Victorious Streaming Studio Ready",
                                string.Format("Studio online on Port {0}\nPIN: {1}\nIP: {2}", httpPort, sessionPin, localIp),
                                ToolTipIcon.Info
                            );
                        }
                    }
                };

                nodeProcess.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        File.AppendAllText(Path.Combine(appDir, "server_error.log"), e.Data + "\n");
                    }
                };

                nodeProcess.Exited += (s, e) =>
                {
                    if (!isShuttingDown)
                    {
                        trayIcon.ShowBalloonTip(4000, "Studio Stopped", "The background broadcast engine stopped unexpectedly.", ToolTipIcon.Warning);
                    }
                };

                nodeProcess.Start();
                nodeProcess.BeginOutputReadLine();
                nodeProcess.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to launch broadcast engine: " + ex.Message, "Victorious Studio Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LaunchDirectorWindow()
        {
            try
            {
                string browserPath = Program.FindBrowser();
                string targetUrl = string.Format("http://localhost:{0}?pin={1}", httpPort, sessionPin);
                string profileDir = Path.Combine(appDir, ".profile");

                if (!string.IsNullOrEmpty(browserPath))
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = browserPath,
                        Arguments = string.Format("--app=\"{0}\" --user-data-dir=\"{1}\" --app-id=VictoriousStreamingStudio --window-size=1366,850 --autoplay-policy=no-user-gesture-required --allow-insecure-localhost", targetUrl, profileDir),
                        WorkingDirectory = appDir
                    };
                    Process.Start(psi);
                }
                else
                {
                    Process.Start(targetUrl);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open Director Window: " + ex.Message, "Victorious Studio", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void CopyCameraLink()
        {
            try
            {
                string camUrl = string.Format("https://{0}:{1}/cam.html?id=1&pin={2}", localIp, httpsPort, sessionPin);
                Clipboard.SetText(camUrl);
                trayIcon.ShowBalloonTip(2000, "Copied to Clipboard", "Camera 1 link copied:\n" + camUrl, ToolTipIcon.Info);
            }
            catch { }
        }

        private void CopyObsUrl()
        {
            try
            {
                string obsUrl = string.Format("http://localhost:{0}/obs.html?id=program", httpPort);
                Clipboard.SetText(obsUrl);
                trayIcon.ShowBalloonTip(2000, "Copied to Clipboard", "OBS Program source link copied:\n" + obsUrl, ToolTipIcon.Info);
            }
            catch { }
        }

        private bool isShuttingDown = false;

        private void Shutdown()
        {
            isShuttingDown = true;
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }

            // Cleanly kill ONLY our child process
            try
            {
                if (nodeProcess != null && !nodeProcess.HasExited)
                {
                    nodeProcess.Kill();
                    nodeProcess.WaitForExit(1000);
                }
            }
            catch { }

            ExitThread();
        }
    }
}