using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

namespace VictoriousStreamingHub
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            Directory.SetCurrentDirectory(appDir);

            // 1. Locate Node runtime (embedded or system fallback)
            string nodePath = Path.Combine(appDir, "runtime", "node", "node.exe");
            if (!File.Exists(nodePath))
            {
                nodePath = "node.exe";
            }

            string serverScript = Path.Combine(appDir, "server.js");
            if (!File.Exists(serverScript))
            {
                MessageBox.Show("Could not find server.js in:\n" + appDir, "Victorious Streaming Hub Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 2. Kill any stale process using port 3000
            try
            {
                ProcessStartInfo killPsi = new ProcessStartInfo("cmd.exe", "/c for /f \"tokens=5\" %a in ('netstat -aon ^| findstr \":3000\" ^| findstr \"LISTENING\"') do taskkill /F /PID %a")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                Process killProc = Process.Start(killPsi);
                killProc.WaitForExit(2000);
            }
            catch { }

            // 3. Start Node.js server silently in background
            ProcessStartInfo serverPsi = new ProcessStartInfo(nodePath, "\"" + serverScript + "\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = appDir
            };

            Process serverProc = null;
            try
            {
                serverProc = Process.Start(serverPsi);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to launch streaming engine:\n" + ex.Message, "Victorious Streaming Hub", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 4. Wait for server to bind port 3000 (up to 15 seconds)
            bool serverReady = false;
            for (int i = 0; i < 30; i++)
            {
                try
                {
                    using (TcpClient tcp = new TcpClient())
                    {
                        IAsyncResult res = tcp.BeginConnect("127.0.0.1", 3000, null, null);
                        bool success = res.AsyncWaitHandle.WaitOne(500);
                        if (success && tcp.Connected)
                        {
                            serverReady = true;
                            break;
                        }
                    }
                }
                catch { }
                Thread.Sleep(300);
            }

            // 5. Find Microsoft Edge or Google Chrome for Standalone App Window
            string browserPath = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe";
            if (!File.Exists(browserPath))
                browserPath = @"C:\Program Files\Microsoft\Edge\Application\msedge.exe";
            if (!File.Exists(browserPath))
                browserPath = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
            if (!File.Exists(browserPath))
                browserPath = @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe";

            string profileDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VictoriousStreamingHub", "profile");
            try { Directory.CreateDirectory(profileDir); } catch { }

            string args = string.Format("--app=http://localhost:3000/ --user-data-dir=\"{0}\" --window-size=1366,850 --app-id=VictoriousStreamingHub --autoplay-policy=no-user-gesture-required --disable-features=WebRtcHideLocalIpsWithMdns --allow-insecure-localhost --no-first-run --no-default-browser-check", profileDir);

            Process browserProc = null;
            if (File.Exists(browserPath))
            {
                ProcessStartInfo browserPsi = new ProcessStartInfo(browserPath, args)
                {
                    UseShellExecute = true
                };
                browserProc = Process.Start(browserPsi);
            }
            else
            {
                // Fallback to default browser
                Process.Start("http://localhost:3000/");
            }

            // If we launched Edge/Chrome in app mode, wait for it to exit, then cleanly terminate Node
            if (browserProc != null)
            {
                browserProc.WaitForExit();
                try
                {
                    if (serverProc != null && !serverProc.HasExited)
                    {
                        serverProc.Kill();
                    }
                }
                catch { }
            }
        }
    }
}