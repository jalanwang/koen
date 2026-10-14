// KoEnCaret - shows a Korean flag (Hangul mode) or US flag (English mode) next to the text caret.
// Built with the .NET Framework 4 C# compiler (C# 5), no external dependencies.
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Accessibility;

namespace KoEnCaret
{
    static class Native
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT { public int left, top, right, bottom; }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT { public int x, y; }

        [StructLayout(LayoutKind.Sequential)]
        public struct GUITHREADINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hwndActive, hwndFocus, hwndCapture, hwndMenuOwner, hwndMoveSize, hwndCaret;
            public RECT rcCaret;
        }

        public const int SW_HIDE = 0;
        public const int SW_SHOWNOACTIVATE = 4;
        public const uint SWP_NOSIZE = 0x1, SWP_NOMOVE = 0x2, SWP_NOACTIVATE = 0x10, SWP_SHOWWINDOW = 0x40;
        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

        [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
        [DllImport("user32.dll")] public static extern bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO info);
        [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr hWnd, ref POINT pt);
        [DllImport("user32.dll")] public static extern IntPtr GetKeyboardLayout(uint idThread);
        [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int cmd);
        [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
        [DllImport("user32.dll")] public static extern bool DestroyIcon(IntPtr hIcon);
        [DllImport("user32.dll")] public static extern IntPtr SetProcessDpiAwarenessContext(IntPtr ctx);
        [DllImport("user32.dll")] public static extern uint GetDpiForSystem();
        [DllImport("imm32.dll")] public static extern IntPtr ImmGetDefaultIMEWnd(IntPtr hWnd);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam,
            uint flags, uint timeout, out IntPtr result);
        [DllImport("oleacc.dll")]
        public static extern int AccessibleObjectFromWindow(IntPtr hwnd, uint dwObjectID, ref Guid riid,
            [MarshalAs(UnmanagedType.Interface)] out IAccessible acc);
    }

    static class Flags
    {
        // Draws a flag filling a w x h bitmap.
        public static Bitmap Korea(int w, int h)
        {
            Bitmap bmp = new Bitmap(w, h);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.White);
                float cx = w / 2f, cy = h / 2f, r = h * 0.22f;
                RectangleF circle = new RectangleF(cx - r, cy - r, 2 * r, 2 * r);
                using (Brush red = new SolidBrush(Color.FromArgb(205, 46, 58)))
                using (Brush blue = new SolidBrush(Color.FromArgb(0, 71, 160)))
                {
                    g.FillPie(red, circle.X, circle.Y, circle.Width, circle.Height, 180, 180);
                    g.FillPie(blue, circle.X, circle.Y, circle.Width, circle.Height, 0, 180);
                    float s = r / 2f;
                    g.FillEllipse(red, cx - r, cy - s, 2 * s, 2 * s);
                    g.FillEllipse(blue, cx, cy - s, 2 * s, 2 * s);
                }
                // trigrams: three bars in each corner, rotated along the diagonals
                float bw = Math.Max(2f, w * 0.17f), bh = Math.Max(0.7f, h * 0.06f), gap = bh * 1.5f;
                float[] px = { w * 0.17f, w * 0.83f, w * 0.17f, w * 0.83f };
                float[] py = { h * 0.22f, h * 0.22f, h * 0.78f, h * 0.78f };
                float[] ang = { -35f, 35f, 35f, -35f };
                for (int i = 0; i < 4; i++)
                {
                    GraphicsState st = g.Save();
                    g.TranslateTransform(px[i], py[i]);
                    g.RotateTransform(ang[i]);
                    for (int k = -1; k <= 1; k++)
                        g.FillRectangle(Brushes.Black, -bw / 2, k * gap - bh / 2, bw, bh);
                    g.Restore(st);
                }
                using (Pen p = new Pen(Color.FromArgb(150, 150, 150)))
                    g.DrawRectangle(p, 0, 0, w - 1, h - 1);
            }
            return bmp;
        }

        public static Bitmap Usa(int w, int h)
        {
            Bitmap bmp = new Bitmap(w, h);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                float sh = h / 13f;
                using (Brush red = new SolidBrush(Color.FromArgb(178, 34, 52)))
                using (Brush blue = new SolidBrush(Color.FromArgb(60, 59, 110)))
                {
                    for (int i = 0; i < 13; i += 2)
                        g.FillRectangle(red, 0, i * sh, w, sh);
                    g.FillRectangle(blue, 0, 0, w * 0.42f, sh * 7);
                }
                // star field approximated by a grid of white dots
                for (int row = 0; row < 3; row++)
                    for (int col = 0; col < 4; col++)
                    {
                        float x = w * 0.42f * (col + 0.7f) / 4.4f;
                        float y = sh * 7 * (row + 0.6f) / 3.2f;
                        g.FillRectangle(Brushes.White, x, y, Math.Max(1f, w / 20f), Math.Max(1f, h / 15f));
                    }
                using (Pen p = new Pen(Color.FromArgb(150, 150, 150)))
                    g.DrawRectangle(p, 0, 0, w - 1, h - 1);
            }
            return bmp;
        }
    }

    class Overlay : Form
    {
        public Bitmap Image;

        public Overlay(int w, int h)
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            Size = new Size(w, h);
            Opacity = 0.95;
        }

        protected override bool ShowWithoutActivation { get { return true; } }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                // TOOLWINDOW | NOACTIVATE | TRANSPARENT (click-through) | LAYERED
                cp.ExStyle |= 0x80 | 0x08000000 | 0x20 | 0x80000;
                return cp;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Image != null) e.Graphics.DrawImage(Image, 0, 0);
        }
    }

    class App : ApplicationContext
    {
        const uint WM_IME_CONTROL = 0x283;
        const int IMC_GETCONVERSIONMODE = 1;
        const int IME_CMODE_NATIVE = 1;
        const int LANG_KOREAN = 0x0412;
        static Guid IID_IAccessible = new Guid("618736E0-3C3D-11CF-810C-00AA00389B71");

        readonly int w, h;
        readonly Overlay overlay;
        readonly Bitmap korFlag, usFlag;
        readonly NotifyIcon tray;
        readonly System.Windows.Forms.Timer timer;
        bool? lastKorean;
        bool visible;

        public App()
        {
            double scale = 1.0;
            try { scale = Native.GetDpiForSystem() / 96.0; } catch { }
            w = (int)Math.Round(22 * scale);
            h = (int)Math.Round(15 * scale);
            korFlag = Flags.Korea(w, h);
            usFlag = Flags.Usa(w, h);

            overlay = new Overlay(w, h);
            IntPtr handle = overlay.Handle; // force handle creation without showing

            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("종료", null, delegate { ExitThread(); });
            tray = new NotifyIcon();
            tray.ContextMenuStrip = menu;
            tray.Text = "KoEnCaret";
            SetTray(false);
            tray.Visible = true;

            timer = new System.Windows.Forms.Timer();
            timer.Interval = 100;
            timer.Tick += delegate { try { Tick(); } catch { } };
            timer.Start();
        }

        void SetTray(bool korean)
        {
            using (Bitmap b = new Bitmap(16, 16))
            {
                using (Graphics g = Graphics.FromImage(b))
                using (Bitmap f = korean ? Flags.Korea(16, 11) : Flags.Usa(16, 11))
                {
                    g.Clear(Color.Transparent);
                    g.DrawImage(f, 0, 2);
                }
                IntPtr hIcon = b.GetHicon();
                Icon old = tray.Icon;
                tray.Icon = (Icon)Icon.FromHandle(hIcon).Clone();
                Native.DestroyIcon(hIcon);
                if (old != null) old.Dispose();
            }
            tray.Text = korean ? "KoEnCaret - 한글" : "KoEnCaret - English";
        }

        void Hide()
        {
            if (visible)
            {
                Native.ShowWindow(overlay.Handle, Native.SW_HIDE);
                visible = false;
            }
        }

        void Tick()
        {
            IntPtr fg = Native.GetForegroundWindow();
            if (fg == IntPtr.Zero) { Hide(); return; }

            uint pid;
            uint tid = Native.GetWindowThreadProcessId(fg, out pid);
            Native.GUITHREADINFO gti = new Native.GUITHREADINFO();
            gti.cbSize = Marshal.SizeOf(typeof(Native.GUITHREADINFO));
            if (!Native.GetGUIThreadInfo(tid, ref gti)) { Hide(); return; }
            IntPtr focus = gti.hwndFocus != IntPtr.Zero ? gti.hwndFocus : fg;

            // 1) classic Win32 caret
            bool have = false;
            int x = 0, y = 0;
            if (gti.hwndCaret != IntPtr.Zero)
            {
                Native.POINT p;
                p.x = gti.rcCaret.left;
                p.y = gti.rcCaret.top;
                if (Native.ClientToScreen(gti.hwndCaret, ref p))
                {
                    int ch = gti.rcCaret.bottom - gti.rcCaret.top;
                    x = p.x;
                    y = p.y + ch;
                    have = true;
                }
            }

            // 2) MSAA caret (Chrome, Edge, Office, ...)
            if (!have)
            {
                try
                {
                    IAccessible acc;
                    Guid iid = IID_IAccessible;
                    int hr = Native.AccessibleObjectFromWindow(focus, 0xFFFFFFF8, ref iid, out acc);
                    if (hr == 0 && acc != null)
                    {
                        int l, t, cw, chh;
                        acc.accLocation(out l, out t, out cw, out chh, 0);
                        if (chh > 0 && (l != 0 || t != 0))
                        {
                            x = l;
                            y = t + chh;
                            have = true;
                        }
                    }
                }
                catch { }
            }

            if (!have) { Hide(); return; }

            // Hangul / English state
            bool korean = false;
            long layout = Native.GetKeyboardLayout(tid).ToInt64() & 0xFFFF;
            if (layout == LANG_KOREAN)
            {
                IntPtr ime = Native.ImmGetDefaultIMEWnd(focus);
                if (ime != IntPtr.Zero)
                {
                    IntPtr res;
                    IntPtr ok = Native.SendMessageTimeout(ime, WM_IME_CONTROL, new IntPtr(IMC_GETCONVERSIONMODE),
                        IntPtr.Zero, 0x2 /*SMTO_ABORTIFHUNG*/, 50, out res);
                    if (ok != IntPtr.Zero)
                        korean = (res.ToInt64() & IME_CMODE_NATIVE) != 0;
                }
            }

            if (lastKorean == null || lastKorean.Value != korean)
            {
                lastKorean = korean;
                overlay.Image = korean ? korFlag : usFlag;
                overlay.Invalidate();
                SetTray(korean);
            }

            Native.SetWindowPos(overlay.Handle, Native.HWND_TOPMOST, x + 2, y, w, h,
                Native.SWP_NOACTIVATE | Native.SWP_SHOWWINDOW);
            visible = true;
        }

        protected override void ExitThreadCore()
        {
            timer.Stop();
            tray.Visible = false;
            tray.Dispose();
            overlay.Dispose();
            base.ExitThreadCore();
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            bool created;
            using (Mutex m = new Mutex(true, @"Local\KoEnCaret_SingleInstance", out created))
            {
                if (!created) return;
                try { Native.SetProcessDpiAwarenessContext(new IntPtr(-4)); } catch { }
                Application.EnableVisualStyles();
                Application.Run(new App());
            }
        }
    }
}
