using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;

class FallbackDesigner
{
    static void Main()
    {
        string baseDir = Directory.GetCurrentDirectory();
        string logoPath = Path.Combine(baseDir, "public", "assets", "raw_ag_logo.jpg");
        string outJpg = Path.Combine(baseDir, "public", "assets", "church_flier.jpg");
        string outPng = Path.Combine(baseDir, "public", "assets", "church_flier.png");

        if (!File.Exists(logoPath))
        {
            Console.WriteLine("Error: logo not found at " + logoPath);
            return;
        }

        // 1. Process Logo: Isolate the golden shield from white background cleanly from outer edges
        Bitmap rawLogo = new Bitmap(logoPath);
        Bitmap cleanLogo = CleanLogoFromOuterWhite(rawLogo);

        // 2. Setup 1920x1080 High-Definition Canvas
        int W = 1920;
        int H = 1080;
        Bitmap canvas = new Bitmap(W, H, PixelFormat.Format32bppArgb);

        using (Graphics g = Graphics.FromImage(canvas))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            // Clean, Cinematic, Dark Sanctuary Background
            using (GraphicsPath bgPath = new GraphicsPath())
            {
                bgPath.AddRectangle(new Rectangle(0, 0, W, H));
                using (PathGradientBrush pgb = new PathGradientBrush(bgPath))
                {
                    pgb.CenterPoint = new PointF(W / 2f, H * 0.44f);
                    pgb.CenterColor = Color.FromArgb(255, 26, 12, 46); // subtle rich royal shadow
                    pgb.SurroundColors = new Color[] { Color.FromArgb(255, 6, 3, 12) }; // deep pure obsidian
                    g.FillRectangle(pgb, 0, 0, W, H);
                }
            }

            // Subtle depth vignette
            using (LinearGradientBrush lgb = new LinearGradientBrush(
                new Point(0, 0), new Point(0, H),
                Color.FromArgb(40, 0, 0, 0),
                Color.FromArgb(140, 0, 0, 0)))
            {
                g.FillRectangle(lgb, 0, 0, W, H);
            }

            float centerX = W / 2f;

            // Logo Dimensions & Vertical Center Placement
            // With only Logo and Caption, we position them harmoniously in the vertical center.
            int logoW = 340;
            int logoH = (int)(logoW * ((float)cleanLogo.Height / cleanLogo.Width)); // 340
            int logoX = (int)(centerX - logoW / 2);
            int logoY = 250; // tops around 250, bottoms at 590

            // Soft warm golden backlight halo behind the logo
            for (int radius = 320; radius >= 140; radius -= 20)
            {
                int haloAlpha = (int)(18 * (1.0f - (radius - 140f) / 180f));
                using (SolidBrush haloBrush = new SolidBrush(Color.FromArgb(haloAlpha, 251, 191, 36)))
                {
                    g.FillEllipse(haloBrush, centerX - radius, (logoY + logoH / 2f) - radius, radius * 2, radius * 2);
                }
            }

            // Realistic Soft Drop Shadow behind the Shield
            for (int s = 18; s >= 2; s -= 2)
            {
                int shadowAlpha = 14;
                using (SolidBrush sBrush = new SolidBrush(Color.FromArgb(shadowAlpha, 0, 0, 0)))
                {
                    g.FillEllipse(sBrush, logoX - s, logoY + 30 - s, logoW + s * 2, logoH + s * 2);
                }
            }

            // Draw the exact logo cleanly
            g.DrawImage(cleanLogo, new Rectangle(logoX, logoY, logoW, logoH));

            // ==============================================================
            // ONLY THE CAPTION: "Thanks for Worshipping with Us!"
            // ==============================================================
            StringFormat sfCenter = new StringFormat();
            sfCenter.Alignment = StringAlignment.Center;
            sfCenter.LineAlignment = StringAlignment.Center;

            Font captionFont;
            try { captionFont = new Font("Georgia", 60, FontStyle.Bold); }
            catch { captionFont = new Font("Segoe UI", 58, FontStyle.Bold); }

            using (captionFont)
            {
                string caption = "Thanks for Worshipping with Us!";
                float capY = logoY + logoH + 95; // ~685px

                // Multi-layered soft realistic drop shadows for depth
                for (int d = 6; d >= 2; d -= 2)
                {
                    using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(160 / (d / 2), 0, 0, 0)))
                    {
                        g.DrawString(caption, captionFont, shadowBrush, new PointF(centerX + d * 0.7f, capY + d), sfCenter);
                    }
                }

                // Subtle warm golden ambient rim glow
                using (SolidBrush glowBrush = new SolidBrush(Color.FromArgb(35, 245, 158, 11)))
                {
                    for (int gOffset = -2; gOffset <= 2; gOffset += 2)
                    {
                        if (gOffset != 0)
                        {
                            g.DrawString(caption, captionFont, glowBrush, new PointF(centerX + gOffset, capY), sfCenter);
                            g.DrawString(caption, captionFont, glowBrush, new PointF(centerX, capY + gOffset), sfCenter);
                        }
                    }
                }

                // Premium Gradient Fill (Lustrous White down to Soft Champagne Gold)
                using (LinearGradientBrush capBrush = new LinearGradientBrush(
                    new PointF(centerX, capY - 45),
                    new PointF(centerX, capY + 45),
                    Color.FromArgb(255, 255, 255, 255),
                    Color.FromArgb(255, 253, 230, 138)))
                {
                    g.DrawString(caption, captionFont, capBrush, new PointF(centerX, capY), sfCenter);
                }
            }
        }

        // Save at 95% High Quality JPEG & Lossless PNG
        ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);
        EncoderParameters encParams = new EncoderParameters(1);
        encParams.Param[0] = new EncoderParameter(Encoder.Quality, 95L);

        canvas.Save(outJpg, jpgEncoder, encParams);
        canvas.Save(outPng, ImageFormat.Png);

        Console.WriteLine("SUCCESS: Generated Clean Minimal Fallback Slate at " + outJpg);
    }

    // Flood/edge remove white background around the shield logo cleanly
    static Bitmap CleanLogoFromOuterWhite(Bitmap src)
    {
        int w = src.Width;
        int h = src.Height;
        Bitmap res = new Bitmap(w, h, PixelFormat.Format32bppArgb);

        // Find outer white via breadth-first search from border
        bool[,] isOuterWhite = new bool[w, h];
        var queue = new System.Collections.Generic.Queue<Point>();

        for (int x = 0; x < w; x++)
        {
            CheckAndEnqueue(src, x, 0, isOuterWhite, queue);
            CheckAndEnqueue(src, x, h - 1, isOuterWhite, queue);
        }
        for (int y = 0; y < h; y++)
        {
            CheckAndEnqueue(src, 0, y, isOuterWhite, queue);
            CheckAndEnqueue(src, w - 1, y, isOuterWhite, queue);
        }

        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };

        while (queue.Count > 0)
        {
            Point pt = queue.Dequeue();
            for (int k = 0; k < 4; k++)
            {
                int nx = pt.X + dx[k];
                int ny = pt.Y + dy[k];
                if (nx >= 0 && nx < w && ny >= 0 && ny < h && !isOuterWhite[nx, ny])
                {
                    CheckAndEnqueue(src, nx, ny, isOuterWhite, queue);
                }
            }
        }

        // Copy pixels: make outer white transparent with smooth edge antialiasing
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (isOuterWhite[x, y])
                {
                    res.SetPixel(x, y, Color.Transparent);
                }
                else
                {
                    Color c = src.GetPixel(x, y);
                    bool nearEdge = false;
                    for (int k = 0; k < 4; k++)
                    {
                        int nx = x + dx[k];
                        int ny = y + dy[k];
                        if (nx >= 0 && nx < w && ny >= 0 && ny < h && isOuterWhite[nx, ny])
                        {
                            nearEdge = true;
                            break;
                        }
                    }
                    if (nearEdge && (c.R > 210 && c.G > 210 && c.B > 210))
                    {
                        res.SetPixel(x, y, Color.FromArgb(100, c.R, c.G, c.B));
                    }
                    else
                    {
                        res.SetPixel(x, y, c);
                    }
                }
            }
        }
        return res;
    }

    static void CheckAndEnqueue(Bitmap src, int x, int y, bool[,] visited, System.Collections.Generic.Queue<Point> q)
    {
        if (visited[x, y]) return;
        Color c = src.GetPixel(x, y);
        if (c.R >= 225 && c.G >= 225 && c.B >= 225)
        {
            visited[x, y] = true;
            q.Enqueue(new Point(x, y));
        }
    }

    static ImageCodecInfo GetEncoder(ImageFormat format)
    {
        ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
        foreach (ImageCodecInfo codec in codecs)
        {
            if (codec.FormatID == format.Guid) return codec;
        }
        return null;
    }
}
