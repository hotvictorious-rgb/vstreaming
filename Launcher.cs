using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

[assembly: AssemblyTitle("Victorious Streaming Studio")]
[assembly: AssemblyDescription("Professional Church Multi-Camera Broadcast Studio")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("VICTORIOUS MARKET")]
[assembly: AssemblyProduct("Victorious Streaming Studio")]
[assembly: AssemblyCopyright("Copyright © 2026 VICTORIOUS MARKET • Victory Saviour Edet")]
[assembly: AssemblyTrademark("Victorious Streaming Studio™")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace VictoriousStreamingStudio
{
    static class Program
    {
        private static Mutex appMutex = null;

        [STAThread]
        static void Main()
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
            Directory.SetCurrentDirectory(appDir);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 1. Single Instance Protection: Avoid port collision & duplicate instances
            bool isNewInstance;
            appMutex = new Mutex(true, "VictoriousStreamingStudio_AppMutex_v1", out isNewInstance);
            if (!isNewInstance)
            {
                // Studio is already running! Signal existing instance and notify user
                NativeMethods.SignalRunningStudio();
                MessageBox.Show(
                    "Victorious Streaming Studio is already running in the background!\n\nCheck your Windows Taskbar System Tray near the clock, or look for the open Director Window.",
                    "Victorious Streaming Studio",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                return;
            }

            // 2. Locate portable or system Node engine
            string nodePath = FindPortableOrSystemNode(appDir);
            if (string.IsNullOrEmpty(nodePath))
            {
                MessageBox.Show(
                    "The broadcast engine (node.exe) was not found in 'runtime\\node' or on this system.\n\nPlease ensure the runtime folder is included with the application.",
                    "Victorious Streaming Studio - Missing Runtime",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return;
            }

            // 3. Launch with Studio Application Context
            try
            {
                Application.Run(new StudioContext(appDir, nodePath));
            }
            catch (Exception ex)
            {
                try { File.WriteAllText(Path.Combine(appDir, "crash.log"), ex.ToString()); } catch { }
            }
            finally
            {
                if (appMutex != null)
                {
                    appMutex.ReleaseMutex();
                    appMutex.Dispose();
                }
            }
        }

        public static string FindPortableOrSystemNode(string appDir)
        {
            // 1. Check local portable runtime folder
            string localRuntime = Path.Combine(appDir, @"runtime\node\node.exe");
            if (File.Exists(localRuntime)) return localRuntime;

            // 2. Check local bin folder
            string localBin = Path.Combine(appDir, @"bin\node.exe");
            if (File.Exists(localBin)) return localBin;

            // 3. Fallback to standard system installations
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

            // 4. Try PATH 'where node'
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

    // =========================================================================
    // NATIVE SPLASH SCREEN (LUXURY GOLD & OBSIDIAN PURPLE BRANDING)
    // =========================================================================
    public class StudioSplashScreen : Form
    {
        private Label lblStatus;
        private Label lblTitle;
        private Label lblSubtitle;
        private System.Windows.Forms.Timer pulseTimer;
        private int pulseTick = 0;

        public StudioSplashScreen(Icon appIcon)
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(540, 310);
            this.BackColor = Color.FromArgb(17, 9, 36);
            this.ShowInTaskbar = true;
            this.Text = "Victorious Streaming Studio - Starting";
            if (appIcon != null) this.Icon = appIcon;

            // Custom double buffer
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

            // Header Title
            lblTitle = new Label();
            lblTitle.Text = "VICTORIOUS STREAMING STUDIO";
            lblTitle.Font = new Font("Segoe UI", 16f, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(245, 158, 11); // Gold
            lblTitle.Location = new Point(0, 140);
            lblTitle.Size = new Size(540, 32);
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            this.Controls.Add(lblTitle);

            // Subtitle
            lblSubtitle = new Label();
            lblSubtitle.Text = "Enterprise Church Multi-Camera Broadcast Engine";
            lblSubtitle.Font = new Font("Segoe UI", 10.5f, FontStyle.Regular);
            lblSubtitle.ForeColor = Color.FromArgb(196, 181, 253); // Lavender
            lblSubtitle.Location = new Point(0, 175);
            lblSubtitle.Size = new Size(540, 24);
            lblSubtitle.TextAlign = ContentAlignment.MiddleCenter;
            this.Controls.Add(lblSubtitle);

            // Status message
            lblStatus = new Label();
            lblStatus.Text = "⚡ Initializing high-speed WebRTC broadcast engine...";
            lblStatus.Font = new Font("Segoe UI", 9.5f, FontStyle.Italic);
            lblStatus.ForeColor = Color.FromArgb(148, 163, 184); // Slate
            lblStatus.Location = new Point(0, 240);
            lblStatus.Size = new Size(540, 24);
            lblStatus.TextAlign = ContentAlignment.MiddleCenter;
            this.Controls.Add(lblStatus);

            pulseTimer = new System.Windows.Forms.Timer();
            pulseTimer.Interval = 80;
            pulseTimer.Tick += (s, e) =>
            {
                pulseTick++;
                this.Invalidate();
            };
            pulseTimer.Start();
        }

        public void SetStatus(string text)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => SetStatus(text)));
                return;
            }
            lblStatus.Text = text;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Background subtle gradient
            using (LinearGradientBrush bg = new LinearGradientBrush(
                this.ClientRectangle,
                Color.FromArgb(28, 16, 56),
                Color.FromArgb(11, 6, 24),
                45f))
            {
                g.FillRectangle(bg, this.ClientRectangle);
            }

            // Outer gold border
            using (Pen goldPen = new Pen(Color.FromArgb(245, 158, 11), 2f))
            {
                g.DrawRectangle(goldPen, 1, 1, this.Width - 2, this.Height - 2);
            }

            // Top Crown & Camera Emblem
            float cx = this.Width / 2f;
            float cy = 70f;
            float s = 1.2f;

            // Halo glow
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddEllipse(cx - 45f, cy - 45f, 90f, 90f);
                using (PathGradientBrush pgb = new PathGradientBrush(path))
                {
                    pgb.CenterColor = Color.FromArgb(90, 245, 158, 11);
                    pgb.SurroundColors = new Color[] { Color.Transparent };
                    g.FillEllipse(pgb, cx - 45f, cy - 45f, 90f, 90f);
                }
            }

            // Crown polygon
            using (LinearGradientBrush goldBrush = new LinearGradientBrush(
                new RectangleF(cx - 35f, cy - 35f, 70f, 70f),
                Color.FromArgb(253, 224, 71),
                Color.FromArgb(217, 119, 6),
                90f))
            {
                PointF[] pts = new PointF[]
                {
                    new PointF(cx - 24f * s, cy + 12f * s),
                    new PointF(cx - 30f * s, cy - 14f * s),
                    new PointF(cx - 12f * s, cy + 0f * s),
                    new PointF(cx + 0f * s,  cy - 22f * s),
                    new PointF(cx + 12f * s, cy + 0f * s),
                    new PointF(cx + 30f * s, cy - 14f * s),
                    new PointF(cx + 24f * s, cy + 12f * s)
                };
                g.FillPolygon(goldBrush, pts);
                g.FillRectangle(goldBrush, cx - 25f * s, cy + 12f * s, 50f * s, 6.5f * s);
            }

            // Crown jewels
            using (SolidBrush jBrush = new SolidBrush(Color.White))
            {
                float jr = 2.8f;
                g.FillEllipse(jBrush, cx - 30f * s - jr, cy - 14f * s - jr, jr * 2, jr * 2);
                g.FillEllipse(jBrush, cx + 0f * s - jr,  cy - 22f * s - jr, jr * 2, jr * 2);
                g.FillEllipse(jBrush, cx + 30f * s - jr, cy - 14f * s - jr, jr * 2, jr * 2);
            }

            // Broadcast wave arc
            using (Pen wavePen = new Pen(Color.FromArgb(230, 56, 189, 248), 2.2f))
            {
                g.DrawArc(wavePen, cx - 20f * s, cy + 22f * s, 40f * s, 14f * s, 0, 180);
            }

            // Animated subtle loading line
            int barWidth = 260;
            int barHeight = 4;
            int barX = (this.Width - barWidth) / 2;
            int barY = 275;

            using (SolidBrush trackBrush = new SolidBrush(Color.FromArgb(40, 255, 255, 255)))
            {
                g.FillRectangle(trackBrush, barX, barY, barWidth, barHeight);
            }

            int pulsePos = (pulseTick * 5) % (barWidth + 60) - 30;
            int clipLeft = Math.Max(barX, barX + pulsePos);
            int clipRight = Math.Min(barX + barWidth, barX + pulsePos + 50);
            if (clipRight > clipLeft)
            {
                using (LinearGradientBrush pulseBrush = new LinearGradientBrush(
                    new Rectangle(clipLeft, barY, clipRight - clipLeft, barHeight),
                    Color.FromArgb(245, 158, 11),
                    Color.FromArgb(56, 189, 248),
                    0f))
                {
                    g.FillRectangle(pulseBrush, clipLeft, barY, clipRight - clipLeft, barHeight);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && pulseTimer != null)
            {
                pulseTimer.Stop();
                pulseTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    // =========================================================================
    // STUDIO CONTEXT (TRAY CONTROLLER & BACKGROUND PROCESS MANAGER)
    // =========================================================================
    public class StudioContext : ApplicationContext
    {
        private NotifyIcon trayIcon;
        private Process nodeProcess;
        private Process browserProcess;
        private StudioSplashScreen splash;
        private Icon appIcon;

        private string appDir;
        private string nodePath;

        private int httpPort = 3000;
        private int httpsPort = 3443;
        private string localIp = "127.0.0.1";
        private string sessionPin = "";
        private bool isServerReady = false;
        private bool isShuttingDown = false;

        public StudioContext(string appDir, string nodePath)
        {
            this.appDir = appDir;
            this.nodePath = nodePath;

            LoadAppIcon();

            // Show native splash screen
            splash = new StudioSplashScreen(appIcon);
            splash.Show();

            SetupTray();
            StartServerProcess();

            // Background monitor thread
            Thread monitorThread = new Thread(() =>
            {
                int waited = 0;
                while (!isServerReady && waited < 20000)
                {
                    Thread.Sleep(200);
                    waited += 200;
                }

                // Close splash and open Director window
                if (splash != null && !splash.IsDisposed)
                {
                    try
                    {
                        splash.Invoke(new Action(() =>
                        {
                            splash.Close();
                            splash.Dispose();
                            splash = null;
                        }));
                    }
                    catch { }
                }

                LaunchDirectorWindow();
            });
            monitorThread.IsBackground = true;
            monitorThread.Start();
        }

        private void LoadAppIcon()
        {
            try
            {
                string iconPath = Path.Combine(appDir, "app.ico");
                if (File.Exists(iconPath))
                {
                    appIcon = new Icon(iconPath);
                }
                else
                {
                    appIcon = SystemIcons.Application;
                }
            }
            catch
            {
                appIcon = SystemIcons.Application;
            }
        }

        private void SetupTray()
        {
            trayIcon = new NotifyIcon();
            trayIcon.Text = "Victorious Streaming Studio (Starting...)";
            trayIcon.Icon = appIcon;

            ContextMenu menu = new ContextMenu();
            menu.MenuItems.Add(new MenuItem("👑 Victorious Streaming Studio") { Enabled = false });
            menu.MenuItems.Add(new MenuItem("-"));
            menu.MenuItems.Add("🎛️ Open Director Command Center", (s, e) => LaunchDirectorWindow());
            menu.MenuItems.Add("🌐 Open in Default Web Browser", (s, e) => OpenInSystemBrowser());
            menu.MenuItems.Add("📱 Copy Camera 1 Connect Link", (s, e) => CopyCameraLink(1));
            menu.MenuItems.Add("🎬 Copy OBS Program URL", (s, e) => CopyObsUrl());
            menu.MenuItems.Add("📁 Open Snapped Photos Folder", (s, e) => OpenPhotosFolder());
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

                        if (splash != null && !splash.IsDisposed)
                        {
                            splash.SetStatus("✨ Broadcast engine ready! Opening Studio...");
                        }

                        if (trayIcon != null)
                        {
                            trayIcon.Text = string.Format("Victorious Studio (Port {0} • PIN {1})", httpPort, sessionPin);
                            trayIcon.ShowBalloonTip(
                                3500,
                                "Victorious Streaming Studio Online",
                                string.Format("Director Hub: Port {0}\nCamera IP: {1}\nPIN: {2}", httpPort, localIp, sessionPin),
                                ToolTipIcon.Info
                            );
                        }
                    }
                };

                nodeProcess.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        try { File.AppendAllText(Path.Combine(appDir, "server_error.log"), e.Data + "\n"); } catch { }
                    }
                };

                nodeProcess.Exited += (s, e) =>
                {
                    if (!isShuttingDown)
                    {
                        if (trayIcon != null)
                        {
                            trayIcon.ShowBalloonTip(4000, "Studio Stopped", "The background broadcast engine stopped.", ToolTipIcon.Warning);
                        }
                    }
                };

                nodeProcess.Start();
                nodeProcess.BeginOutputReadLine();
                nodeProcess.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to start broadcast engine:\n" + ex.Message, "Victorious Studio Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LaunchDirectorWindow()
        {
            try
            {
                // If browser process is still alive and has a window, bring it to front
                if (browserProcess != null && !browserProcess.HasExited)
                {
                    IntPtr hWnd = browserProcess.MainWindowHandle;
                    if (hWnd != IntPtr.Zero)
                    {
                        NativeMethods.SetForegroundWindow(hWnd);
                        if (NativeMethods.IsIconic(hWnd))
                        {
                            NativeMethods.ShowWindow(hWnd, NativeMethods.SW_RESTORE);
                        }
                        return;
                    }
                }

                string browserPath = Program.FindBrowser();
                string targetUrl = string.Format("http://localhost:{0}?pin={1}", httpPort, sessionPin);
                string profileDir = Path.Combine(appDir, ".profile");

                if (!string.IsNullOrEmpty(browserPath))
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = browserPath,
                        Arguments = string.Format(
                            "--app=\"{0}\" --user-data-dir=\"{1}\" --app-id=VictoriousStreamingStudio --window-size=1366,850 --autoplay-policy=no-user-gesture-required --allow-insecure-localhost",
                            targetUrl,
                            profileDir
                        ),
                        WorkingDirectory = appDir
                    };
                    browserProcess = Process.Start(psi);
                }
                else
                {
                    browserProcess = Process.Start(targetUrl);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open Director Window: " + ex.Message, "Victorious Studio", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OpenInSystemBrowser()
        {
            try
            {
                string targetUrl = string.Format("http://localhost:{0}?pin={1}", httpPort, sessionPin);
                Process.Start(new ProcessStartInfo(targetUrl) { UseShellExecute = true });
            }
            catch { }
        }

        private void CopyCameraLink(int camId)
        {
            try
            {
                string camUrl = string.Format("https://{0}:{1}/cam.html?id={2}&pin={3}", localIp, httpsPort, camId, sessionPin);
                Clipboard.SetText(camUrl);
                if (trayIcon != null)
                {
                    trayIcon.ShowBalloonTip(2500, "Copied to Clipboard", string.Format("Camera {0} Link:\n{1}", camId, camUrl), ToolTipIcon.Info);
                }
            }
            catch { }
        }

        private void CopyObsUrl()
        {
            try
            {
                string obsUrl = string.Format("http://localhost:{0}/obs.html?id=program", httpPort);
                Clipboard.SetText(obsUrl);
                if (trayIcon != null)
                {
                    trayIcon.ShowBalloonTip(2500, "Copied to Clipboard", "OBS Master Program Source Link:\n" + obsUrl, ToolTipIcon.Info);
                }
            }
            catch { }
        }

        private void OpenPhotosFolder()
        {
            try
            {
                string photosDir = Path.Combine(appDir, "photos");
                if (!Directory.Exists(photosDir)) Directory.CreateDirectory(photosDir);
                Process.Start(new ProcessStartInfo("explorer.exe", photosDir));
            }
            catch { }
        }

        private void Shutdown()
        {
            if (isShuttingDown) return;
            isShuttingDown = true;

            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }

            // Cleanly kill child node process
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

    // =========================================================================
    // WIN32 NATIVE METHODS
    // =========================================================================
    internal static class NativeMethods
    {
        public const int SW_RESTORE = 9;

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        public static void SignalRunningStudio()
        {
            try
            {
                // Attempt to bring browser window or director window to front
                Process[] procs = Process.GetProcessesByName("msedge");
                foreach (Process p in procs)
                {
                    if (!string.IsNullOrEmpty(p.MainWindowTitle) && p.MainWindowTitle.Contains("Victorious"))
                    {
                        ShowWindow(p.MainWindowHandle, SW_RESTORE);
                        SetForegroundWindow(p.MainWindowHandle);
                        return;
                    }
                }
            }
            catch { }
        }
    }
}