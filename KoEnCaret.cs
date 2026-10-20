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
        [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr hWnd, out RECT rect);
        [DllImport("user32.dll")] public static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder sb, int max);
        [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
        public delegate bool EnumProc(IntPtr hWnd, IntPtr lParam);
        [DllImport("user32.dll")] public static extern bool EnumChildWindows(IntPtr hWnd, EnumProc cb, IntPtr lParam);
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
        int uiaSkip; // ticks to wait before trying the UI Automation fallback again

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

        static bool ClientScreenRect(IntPtr hwnd, out Rectangle rect)
        {
            rect = Rectangle.Empty;
            Native.RECT cr;
            if (!Native.GetClientRect(hwnd, out cr) || cr.right <= cr.left || cr.bottom <= cr.top) return false;
            Native.POINT tl;
            tl.x = cr.left;
            tl.y = cr.top;
            if (!Native.ClientToScreen(hwnd, ref tl)) return false;
            rect = new Rectangle(tl.x, tl.y, cr.right - cr.left, cr.bottom - cr.top);
            return true;
        }

        // Screen rectangle of the focused element's text selection (the caret), via UI Automation.
        // Also returns the bounds of the focused element itself (e.g. the address bar field).
        static bool UiaCaretRect(out Rectangle caret, out Rectangle element, bool editOnly = false)
        {
            caret = Rectangle.Empty;
            element = Rectangle.Empty;
            try
            {
                System.Windows.Automation.AutomationElement fe = System.Windows.Automation.AutomationElement.FocusedElement;
                object pattern;
                if (fe == null) return false;
                if (editOnly && fe.Current.ControlType != System.Windows.Automation.ControlType.Edit) return false;
                if (!fe.TryGetCurrentPattern(System.Windows.Automation.TextPattern.Pattern, out pattern))
                    return false;
                System.Windows.Automation.Text.TextPatternRange[] sel =
                    ((System.Windows.Automation.TextPattern)pattern).GetSelection();
                if (sel.Length == 0) return false;
                System.Windows.Rect[] rects = sel[0].GetBoundingRectangles();
                if (rects.Length == 0) return false;
                System.Windows.Rect r = rects[0];
                if (double.IsInfinity(r.X) || double.IsNaN(r.X) || double.IsInfinity(r.Y) || double.IsNaN(r.Y) ||
                    double.IsInfinity(r.Height) || r.Height <= 0) return false;
                caret = new Rectangle((int)r.X, (int)r.Y, (int)Math.Max(1, r.Width), (int)r.Height);
                System.Windows.Rect eb = fe.Current.BoundingRectangle;
                if (!double.IsInfinity(eb.X) && !double.IsNaN(eb.X) && !double.IsInfinity(eb.Width) && eb.Width > 0)
                    element = new Rectangle((int)eb.X, (int)eb.Y, (int)eb.Width, (int)eb.Height);
                return true;
            }
            catch { return false; }
        }

        // Chromium (Chrome, Edge, Electron apps): largest visible render widget = page viewport.
        static bool FindChromiumViewport(IntPtr top, out Rectangle best)
        {
            Rectangle found = Rectangle.Empty;
            System.Text.StringBuilder sb = new System.Text.StringBuilder(64);
            Native.GetClassName(top, sb, 64);
            if (!sb.ToString().StartsWith("Chrome_WidgetWin"))
            {
                best = Rectangle.Empty;
                return false;
            }
            Native.EnumChildWindows(top, delegate(IntPtr child, IntPtr lp)
            {
                System.Text.StringBuilder cls = new System.Text.StringBuilder(64);
                Native.GetClassName(child, cls, 64);
                Rectangle r;
                if (cls.ToString() == "Chrome_RenderWidgetHostHWND" && Native.IsWindowVisible(child) &&
                    ClientScreenRect(child, out r) && r.Width * r.Height > found.Width * found.Height)
                    found = r;
                return true;
            }, IntPtr.Zero);
            best = found;
            return !found.IsEmpty;
        }

        void Hide()
        {
            if (visible)
            {
                Native.ShowWindow(overlay.Handle, Native.SW_HIDE);
                visible = false;
            }
        }

        // Optional diagnostics: create %LOCALAPPDATA%\KoEnCaret\debug.flag to log every decision
        // (state changes only) to debug.log in the same folder.
        static readonly string DbgDir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KoEnCaret");
        bool dbgOn;
        int dbgTicks;
        string lastDbg;

        void Dbg(string s)
        {
            if (!dbgOn || s == lastDbg) return;
            lastDbg = s;
            try
            {
                System.IO.File.AppendAllText(System.IO.Path.Combine(DbgDir, "debug.log"),
                    DateTime.Now.ToString("HH:mm:ss.fff ") + s + "\r\n");
            }
            catch { }
        }

        static string ClassOf(IntPtr h)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder(128);
            Native.GetClassName(h, sb, 128);
            return sb.ToString();
        }

        void Tick()
        {
            if (dbgTicks-- <= 0)
            {
                dbgTicks = 20;
                dbgOn = System.IO.File.Exists(System.IO.Path.Combine(DbgDir, "debug.flag"));
            }

            IntPtr fg = Native.GetForegroundWindow();
            if (fg == IntPtr.Zero) { Dbg("hide: no foreground window"); Hide(); return; }

            uint pid;
            uint tid = Native.GetWindowThreadProcessId(fg, out pid);
            Native.GUITHREADINFO gti = new Native.GUITHREADINFO();
            gti.cbSize = Marshal.SizeOf(typeof(Native.GUITHREADINFO));
            if (!Native.GetGUIThreadInfo(tid, ref gti)) { Dbg("hide: GetGUIThreadInfo failed"); Hide(); return; }
            IntPtr focus = gti.hwndFocus != IntPtr.Zero ? gti.hwndFocus : fg;

            // 1) classic Win32 caret
            bool have = false;
            int x = 0, top = 0, height = 0;
            int msaaWidth = -1; // stays -1 unless the caret came from MSAA
            if (gti.hwndCaret != IntPtr.Zero)
            {
                Native.POINT p;
                p.x = gti.rcCaret.left;
                p.y = gti.rcCaret.top;
                if (Native.ClientToScreen(gti.hwndCaret, ref p))
                {
                    x = p.x;
                    top = p.y;
                    height = gti.rcCaret.bottom - gti.rcCaret.top;
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
                            top = t;
                            height = chh;
                            msaaWidth = cw;
                            have = true;
                        }
                    }
                }
                catch { }
            }

            // 3) UI Automation (XAML/WinUI text boxes such as the taskbar search box, which
            //    expose neither a Win32 caret nor an MSAA one). Throttled when it keeps failing.
            if (!have)
            {
                if (uiaSkip > 0) uiaSkip--;
                else
                {
                    Rectangle real, field;
                    if (UiaCaretRect(out real, out field, true))
                    {
                        x = real.X;
                        top = real.Y;
                        height = real.Height;
                        have = true;
                    }
                    else uiaSkip = 5;
                }
            }

            if (dbgOn)
                Dbg(string.Format("fg={0} have={1} caret=({2},{3}) h={4} msaaW={5}", ClassOf(fg), have, x, top, height, msaaWidth));
            if (!have) { Dbg("hide: no caret from Win32/MSAA/UIA (fg=" + ClassOf(fg) + ")"); Hide(); return; }

            // The caret line may have been scrolled out of view: hide when the caret
            // is outside the visible area of the window that owns it. For Chromium-based
            // apps the focus window is the whole frame (tabs, toolbar included), so use the
            // page viewport (Chrome_RenderWidgetHostHWND) instead.
            IntPtr owner = gti.hwndCaret != IntPtr.Zero ? gti.hwndCaret : focus;
            Rectangle view;
            bool chromium = FindChromiumViewport(fg, out view);
            if (!chromium && !ClientScreenRect(owner, out view))
                view = Rectangle.Empty;
            if (!view.IsEmpty)
            {
                int midY = top + height / 2;
                if (chromium && midY < view.Top)
                {
                    // Browser UI above the page (address bar, ...): MSAA gives a stale position
                    // here, so take the caret from UI Automation and require it to be in that UI.
                    Rectangle real, field, frame;
                    if (!UiaCaretRect(out real, out field) || real.Y + real.Height / 2 >= view.Top ||
                        !ClientScreenRect(fg, out frame) || !frame.Contains(real.X, real.Y + real.Height / 2))
                    {
                        Hide();
                        return;
                    }
                    x = real.X;
                    top = real.Y;
                    height = real.Height;
                }
                else
                {
                    // Chromium keeps reporting the last caret position with width 0 once focus is
                    // no longer in a text field (e.g. after switching tabs): there is no caret.
                    if (chromium && msaaWidth == 0)
                    {
                        Hide();
                        return;
                    }
                    if (x < view.Left || x > view.Right || midY < view.Top || midY >= view.Bottom)
                    {
                        Dbg(string.Format("hide: caret ({0},{1}) outside view {2}", x, midY, view));
                        Hide();
                        return;
                    }
                    // Chromium clamps the MSAA caret to the viewport edge when its line is scrolled
                    // away. When the caret sits on an edge, ask UI Automation for the real position.
                    if (chromium && (top <= view.Top + 2 || top + height >= view.Bottom - 2))
                    {
                        Rectangle real, field;
                        if (UiaCaretRect(out real, out field) &&
                            (real.Y + real.Height / 2 < view.Top || real.Y + real.Height / 2 >= view.Bottom))
                        {
                            Hide();
                            return;
                        }
                    }
                }
            }

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

            // Flag follows the caret: just right of it, below the line. When there is no room
            // below within the work area, go above the caret; a caret inside the taskbar (its
            // search box) gets the flag right above the taskbar, which would otherwise cover it.
            Screen scr = Screen.FromPoint(new Point(x, top));
            Rectangle screen = scr.Bounds, work = scr.WorkingArea;
            int fx = Math.Max(screen.Left, Math.Min(x + 2, screen.Right - w));
            int fy = top + height;
            if (fy + h > work.Bottom) fy = Math.Min(top - h, work.Bottom - h);
            fy = Math.Max(screen.Top, Math.Min(fy, screen.Bottom - h));
            Native.SetWindowPos(overlay.Handle, Native.HWND_TOPMOST, fx, fy, w, h,
                Native.SWP_NOACTIVATE | Native.SWP_SHOWWINDOW);
            visible = true;
            if (dbgOn)
                Dbg(string.Format("show at ({0},{1}) korean={2} screen={3} fg={4}", fx, fy, korean, screen, ClassOf(fg)));
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
