using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace VictoriousStreamingHub
{
    static class Program
    {
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

        [STAThread]
        static void Main()
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
            Directory.SetCurrentDirectory(appDir);

            AppDomain.CurrentDomain.UnhandledException += (s, e) => {
                File.WriteAllText(Path.Combine(appDir, "crash.log"), "UnhandledException:\n" + e.ExceptionObject.ToString());
            };

            Application.ThreadException += (s, e) => {
                File.WriteAllText(Path.Combine(appDir, "crash.log"), "ThreadException:\n" + e.Exception.ToString());
            };

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string nodePath = FindNode();
            if (string.IsNullOrEmpty(nodePath))
            {
                MessageBox.Show("Node.js runtime was not detected.", "Victorious Streaming Hub", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            KillPort3000();
            Thread.Sleep(300);

            try
            {
                Application.Run(new StudioContext(appDir, nodePath));
            }
            catch (Exception ex)
            {
                File.WriteAllText(Path.Combine(appDir, "crash.log"), "RunException:\n" + ex.ToString());
            }
        }

        public static string FindNode()
        {
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
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();
                    if (!string.IsNullOrEmpty(output))
                    {
                        string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        if (lines.Length > 0 && File.Exists(lines[0].Trim()))
                        {
                            return lines[0].Trim();
                        }
                    }
                }
            }
            catch { }

            string[] searchPaths = new string[]
            {
                @"C:\Program Files\nodejs\node.exe",
                @"C:\Program Files (x86)\nodejs\node.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\node\node.exe")
            };

            foreach (string path in searchPaths)
            {
                if (File.Exists(path)) return path;
            }

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

        public static void KillPort3000()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c for /f \"tokens=5\" %a in ('netstat -aon 2^>nul ^| findstr \":3000\" ^| findstr \"LISTENING\"') do taskkill /F /PID %a >nul 2>&1",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using (Process p = Process.Start(psi))
                {
                    p.WaitForExit();
                }
            }
            catch { }
        }
    }

    public class StudioContext : ApplicationContext
    {
        private NotifyIcon trayIcon;
        private Process nodeProcess;
        private string appDir;
        private string nodePath;

        public StudioContext(string appDir, string nodePath)
        {
            this.appDir = appDir;
            this.nodePath = nodePath;

            File.AppendAllText(Path.Combine(appDir, "debug.log"), "1. StudioContext ctor\n");

            StartNodeEngine();
            SetupTray();

            Thread launchThread = new Thread(() =>
            {
                try
                {
                    File.AppendAllText(Path.Combine(appDir, "debug.log"), "2. launchThread started\n");
                    Thread.Sleep(500);
                    LaunchStudioWindow();
                }
                catch (Exception ex)
                {
                    File.AppendAllText(Path.Combine(appDir, "debug.log"), "launchThread error: " + ex + "\n");
                }
            });
            launchThread.IsBackground = true;
            launchThread.Start();
        }

        private void StartNodeEngine()
        {
            try
            {
                File.AppendAllText(Path.Combine(appDir, "debug.log"), "3. StartNodeEngine\n");
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = nodePath,
                    Arguments = "server.js",
                    WorkingDirectory = appDir,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                nodeProcess = Process.Start(psi);
                File.AppendAllText(Path.Combine(appDir, "debug.log"), "4. Node started PID=" + (nodeProcess != null ? nodeProcess.Id.ToString() : "null") + "\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText(Path.Combine(appDir, "debug.log"), "StartNodeEngine error: " + ex + "\n");
            }
        }

        private void SetupTray()
        {
            try
            {
                File.AppendAllText(Path.Combine(appDir, "debug.log"), "5. SetupTray\n");
                trayIcon = new NotifyIcon();
                trayIcon.Text = "Victorious Streaming Hub";
                trayIcon.Icon = SystemIcons.Application;

                ContextMenu menu = new ContextMenu();
                menu.MenuItems.Add("Open Studio Window", (s, e) => LaunchStudioWindow());
                menu.MenuItems.Add("Exit Victorious Hub", (s, e) => Shutdown());
                trayIcon.ContextMenu = menu;
                trayIcon.Visible = true;
                trayIcon.DoubleClick += (s, e) => LaunchStudioWindow();
                File.AppendAllText(Path.Combine(appDir, "debug.log"), "6. SetupTray complete\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText(Path.Combine(appDir, "debug.log"), "SetupTray error: " + ex + "\n");
            }
        }

        private void LaunchStudioWindow()
        {
            try
            {
                File.AppendAllText(Path.Combine(appDir, "debug.log"), "7. LaunchStudioWindow waiting port\n");
                WaitForPort(3000, 10000);
                File.AppendAllText(Path.Combine(appDir, "debug.log"), "8. Port 3000 ready\n");

                string browserPath = Program.FindBrowser();
                string profileDir = Path.Combine(appDir, ".profile");

                if (!string.IsNullOrEmpty(browserPath))
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = browserPath,
                        Arguments = "--app=http://localhost:3000 --user-data-dir=\"" + profileDir + "\" --window-size=1366,850",
                        WorkingDirectory = appDir
                    };
                    Process.Start(psi);
                    File.AppendAllText(Path.Combine(appDir, "debug.log"), "9. Browser started\n");
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(Path.Combine(appDir, "debug.log"), "LaunchStudioWindow error: " + ex + "\n");
            }
        }

        private void Shutdown()
        {
            File.AppendAllText(Path.Combine(appDir, "debug.log"), "10. Shutdown called\n");
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }

            try
            {
                if (nodeProcess != null && !nodeProcess.HasExited)
                {
                    nodeProcess.Kill();
                }
            }
            catch { }

            Program.KillPort3000();
            ExitThread();
        }

        private void WaitForPort(int port, int timeoutMs)
        {
            int elapsed = 0;
            while (elapsed < timeoutMs)
            {
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://localhost:" + port + "/");
                    request.Timeout = 600;
                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    {
                        if (response.StatusCode == HttpStatusCode.OK) return;
                    }
                }
                catch
                {
                    Thread.Sleep(250);
                    elapsed += 250;
                }
            }
        }
    }
}