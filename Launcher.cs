using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace VictoriousStreamingHub
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            Directory.SetCurrentDirectory(appDir);

            // 1. If server is not already active on port 3000, start it
            if (!IsPortOpen("127.0.0.1", 3000, 300))
            {
                string nodePath = Path.Combine(appDir, "runtime", "node", "node.exe");
                if (!File.Exists(nodePath)) nodePath = "node.exe";

                string serverScript = Path.Combine(appDir, "server.js");

                // Start node completely detached in background with WindowStyle Hidden
                ProcessStartInfo serverPsi = new ProcessStartInfo
                {
                    FileName = nodePath,
                    Arguments = "\"" + serverScript + "\"",
                    WorkingDirectory = appDir,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                try { Process.Start(serverPsi); } catch { }

                // Wait up to 10 seconds for port 3000 to bind
                for (int i = 0; i < 20; i++)
                {
                    if (IsPortOpen("127.0.0.1", 3000, 500)) break;
                    Thread.Sleep(300);
                }
            }

            // 2. Launch frameless desktop app window via Edge or Chrome
            string browserPath = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe";
            if (!File.Exists(browserPath))
                browserPath = @"C:\Program Files\Microsoft\Edge\Application\msedge.exe";
            if (!File.Exists(browserPath))
                browserPath = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
            if (!File.Exists(browserPath))
                browserPath = @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe";

            string args = "--app=http://localhost:3000/ --app-id=VictoriousStreamingHub --window-size=1366,850 --autoplay-policy=no-user-gesture-required --disable-features=WebRtcHideLocalIpsWithMdns --allow-insecure-localhost --no-first-run --no-default-browser-check";

            if (File.Exists(browserPath))
            {
                Process.Start(new ProcessStartInfo(browserPath, args) { UseShellExecute = true });
            }
            else
            {
                Process.Start("http://localhost:3000/");
            }
        }

        static bool IsPortOpen(string host, int port, int timeoutMs)
        {
            try
            {
                using (TcpClient tcp = new TcpClient())
                {
                    IAsyncResult res = tcp.BeginConnect(host, port, null, null);
                    bool success = res.AsyncWaitHandle.WaitOne(timeoutMs);
                    return success && tcp.Connected;
                }
            }
            catch { return false; }
        }
    }
}