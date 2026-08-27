using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using Forms = System.Windows.Forms;
using DrawingColor = System.Drawing.Color;
using MediaColor = System.Windows.Media.Color;
using Pen = System.Windows.Media.Pen;
using Brushes = System.Drawing.Brushes;
using Point = System.Windows.Point;
using Rectangle = System.Drawing.Rectangle;

namespace ReptileDesktopPet
{
    internal static class Program
    {
        private static Mutex _singleInstance;

        [STAThread]
        private static void Main()
        {
            bool created;
            _singleInstance = new Mutex(true, "ReptileDesktopPet.SingleInstance.73A92F4B", out created);
            if (!created)
            {
                return;
            }

            try
            {
                NativeMethods.TryEnablePerMonitorDpi();
                PetApplication app = new PetApplication();
                app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                app.Run();
            }
            finally
            {
                if (_singleInstance != null)
                {
                    _singleInstance.ReleaseMutex();
                    _singleInstance.Dispose();
                }
            }
        }
    }

    internal sealed class PetApplication : Application
    {
        private const string StartupValueName = "ReptileDesktopPet";
        private CreatureController _controller;
        private Forms.NotifyIcon _notifyIcon;
        private Forms.ToolStripMenuItem _pauseItem;
        private Forms.ToolStripMenuItem _startupItem;
        private Icon _trayIcon;
        private IntPtr _trayIconHandle;
        private DesktopBlankClickMonitor _desktopClickMonitor;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _controller = new CreatureController(Dispatcher);
            CreateTrayIcon();
            _controller.Start();
            _desktopClickMonitor = new DesktopBlankClickMonitor(Dispatcher, TogglePaused);
        }

        private void CreateTrayIcon()
        {
            _notifyIcon = new Forms.NotifyIcon();
            _trayIcon = CreateCreatureIcon(out _trayIconHandle);
            _notifyIcon.Icon = _trayIcon;
            _notifyIcon.Text = "Reptile Desktop Pet";
            _notifyIcon.Visible = true;

            Forms.ContextMenuStrip menu = new Forms.ContextMenuStrip();
            _pauseItem = new Forms.ToolStripMenuItem("\u6682\u505c");
            _pauseItem.Click += delegate { TogglePaused(); };

            _startupItem = new Forms.ToolStripMenuItem("\u5f00\u673a\u81ea\u542f");
            _startupItem.Checked = IsStartupEnabled();
            _startupItem.CheckOnClick = false;
            _startupItem.Click += delegate
            {
                bool enabled = !_startupItem.Checked;
                SetStartupEnabled(enabled);
                _startupItem.Checked = IsStartupEnabled();
            };

            Forms.ToolStripMenuItem exitItem = new Forms.ToolStripMenuItem("\u9000\u51fa");
            exitItem.Click += delegate { Shutdown(); };

            menu.Items.Add(_pauseItem);
            menu.Items.Add(_startupItem);
            menu.Items.Add(new Forms.ToolStripSeparator());
            menu.Items.Add(exitItem);
            _notifyIcon.ContextMenuStrip = menu;
            _notifyIcon.DoubleClick += delegate { TogglePaused(); };
        }

        private void TogglePaused()
        {
            _controller.IsPaused = !_controller.IsPaused;
            _pauseItem.Text = _controller.IsPaused ? "\u7ee7\u7eed" : "\u6682\u505c";
        }

        private static Icon CreateCreatureIcon(out IntPtr iconHandle)
        {
            Bitmap bitmap = new Bitmap(32, 32, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(DrawingColor.Transparent);
                using (System.Drawing.Pen glow = new System.Drawing.Pen(DrawingColor.FromArgb(75, 205, 225, 226), 4.0f))
                using (System.Drawing.Pen bone = new System.Drawing.Pen(DrawingColor.FromArgb(235, 224, 233, 230), 1.4f))
                {
                    PointF[] spine = new PointF[]
                    {
                        new PointF(27, 8), new PointF(21, 7), new PointF(15, 10),
                        new PointF(11, 15), new PointF(13, 21), new PointF(19, 24),
                        new PointF(25, 22), new PointF(28, 18)
                    };
                    g.DrawCurve(glow, spine);
                    g.DrawCurve(bone, spine);
                    for (int i = 1; i < spine.Length - 1; i++)
                    {
                        float w = 2.0f + i * 0.45f;
                        g.DrawLine(bone, spine[i].X - w, spine[i].Y - 2, spine[i].X + w, spine[i].Y + 2);
                    }
                    g.FillEllipse(Brushes.White, 25, 6, 4, 4);
                }
            }
            iconHandle = bitmap.GetHicon();
            Icon icon = Icon.FromHandle(iconHandle);
            bitmap.Dispose();
            return icon;
        }

        private static bool IsStartupEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", false))
                {
                    return key != null && key.GetValue(StartupValueName) != null;
                }
            }
            catch
            {
                return false;
            }
        }

        private static void SetStartupEnabled(bool enabled)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run"))
            {
                if (enabled)
                {
                    string path = Process.GetCurrentProcess().MainModule.FileName;
                    key.SetValue(StartupValueName, "\"" + path + "\"");
                }
                else
                {
                    key.DeleteValue(StartupValueName, false);
                }
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_desktopClickMonitor != null)
            {
                _desktopClickMonitor.Dispose();
            }
            if (_controller != null)
            {
                _controller.Dispose();
            }
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
            if (_trayIcon != null)
            {
                _trayIcon.Dispose();
            }
            if (_trayIconHandle != IntPtr.Zero)
            {
                NativeMethods.DestroyIcon(_trayIconHandle);
            }
            base.OnExit(e);
        }
    }

    internal sealed class DesktopBlankClickMonitor : IDisposable
    {
        private readonly Dispatcher _dispatcher;
        private readonly Action _onBlankClick;
        private readonly NativeMethods.LowLevelMouseProc _hookProc;
        private IntPtr _hook;
        private NativeMethods.POINT _mouseDownPoint;
        private bool _leftButtonDown;

        public DesktopBlankClickMonitor(Dispatcher dispatcher, Action onBlankClick)
        {
            _dispatcher = dispatcher;
            _onBlankClick = onBlankClick;
            _hookProc = OnMouseEvent;
            IntPtr module = NativeMethods.GetModuleHandle(null);
            _hook = NativeMethods.SetWindowsHookEx(
                NativeMethods.WH_MOUSE_LL, _hookProc, module, 0);
            if (_hook == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to monitor desktop clicks.");
            }
        }

        private IntPtr OnMouseEvent(int code, IntPtr wParam, IntPtr lParam)
        {
            if (code >= 0)
            {
                int message = wParam.ToInt32();
                NativeMethods.MSLLHOOKSTRUCT data =
                    (NativeMethods.MSLLHOOKSTRUCT)Marshal.PtrToStructure(
                        lParam, typeof(NativeMethods.MSLLHOOKSTRUCT));

                if (message == NativeMethods.WM_LBUTTONDOWN)
                {
                    _mouseDownPoint = data.Point;
                    _leftButtonDown = true;
                }
                else if (message == NativeMethods.WM_LBUTTONUP && _leftButtonDown)
                {
                    _leftButtonDown = false;
                    NativeMethods.POINT down = _mouseDownPoint;
                    NativeMethods.POINT up = data.Point;
                    int dragWidth = Forms.SystemInformation.DragSize.Width;
                    int dragHeight = Forms.SystemInformation.DragSize.Height;
                    if (Math.Abs(up.X - down.X) < dragWidth &&
                        Math.Abs(up.Y - down.Y) < dragHeight)
                    {
                        _dispatcher.BeginInvoke(new Action(delegate
                        {
                            if (DesktopShell.IsBlankAt(down) && DesktopShell.IsBlankAt(up))
                            {
                                _onBlankClick();
                            }
                        }));
                    }
                }
            }

            return NativeMethods.CallNextHookEx(_hook, code, wParam, lParam);
        }

        public void Dispose()
        {
            if (_hook == IntPtr.Zero) return;
            NativeMethods.UnhookWindowsHookEx(_hook);
            _hook = IntPtr.Zero;
        }
    }

    internal sealed class CreatureController : IDisposable
    {
        private readonly Dispatcher _dispatcher;
        private readonly CreatureModel _model;
        private readonly List<DesktopWindow> _windows;
        private readonly DispatcherTimer _timer;
        private readonly Stopwatch _clock;
        private long _lastTicks;
        private int _attachmentCountdown;
        private bool _disposed;
        private bool _isPaused;

        public bool IsPaused
        {
            get { return _isPaused; }
            set
            {
                if (_isPaused == value) return;
                _isPaused = value;
                _lastTicks = _clock.ElapsedTicks;
                for (int i = 0; i < _windows.Count; i++)
                {
                    _windows[i].RequestRender();
                }
            }
        }

        public CreatureController(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            _model = new CreatureModel(78);
            _windows = new List<DesktopWindow>();
            _clock = Stopwatch.StartNew();
            _timer = new DispatcherTimer(DispatcherPriority.Render, dispatcher);
            _timer.Interval = TimeSpan.FromMilliseconds(33);
            _timer.Tick += OnTick;
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        }

        public void Start()
        {
            RebuildWindows();
            NativeMethods.POINT cursor;
            NativeMethods.GetCursorPos(out cursor);
            _model.Reset(new Vec(cursor.X, cursor.Y), GetDpiScaleAt(cursor.X, cursor.Y));
            _lastTicks = _clock.ElapsedTicks;
            _timer.Start();
        }

        private void OnTick(object sender, EventArgs e)
        {
            long ticks = _clock.ElapsedTicks;
            double dt = (ticks - _lastTicks) / (double)Stopwatch.Frequency;
            _lastTicks = ticks;
            if (dt < 0.001) dt = 0.001;
            if (dt > 0.05) dt = 0.05;

            if (!IsPaused)
            {
                NativeMethods.POINT cursor;
                if (NativeMethods.GetCursorPos(out cursor))
                {
                    _model.Update(new Vec(cursor.X, cursor.Y), dt, GetDpiScaleAt(cursor.X, cursor.Y));
                }
            }

            if (!IsPaused && !_model.IsSleeping)
            {
                for (int i = 0; i < _windows.Count; i++)
                {
                    _windows[i].RequestRender();
                }
            }

            _attachmentCountdown--;
            if (_attachmentCountdown <= 0)
            {
                _attachmentCountdown = 120;
                for (int i = 0; i < _windows.Count; i++)
                {
                    _windows[i].EnsureDesktopAttachment();
                }
            }
        }

        private double GetDpiScaleAt(int x, int y)
        {
            for (int i = 0; i < _windows.Count; i++)
            {
                if (_windows[i].ScreenBounds.Contains(x, y))
                {
                    return _windows[i].DpiScale;
                }
            }
            return 1.0;
        }

        private void OnDisplaySettingsChanged(object sender, EventArgs e)
        {
            _dispatcher.BeginInvoke(new Action(RebuildWindows));
        }

        private void RebuildWindows()
        {
            for (int i = 0; i < _windows.Count; i++)
            {
                _windows[i].Close();
            }
            _windows.Clear();

            Forms.Screen[] screens = Forms.Screen.AllScreens;
            for (int i = 0; i < screens.Length; i++)
            {
                DesktopWindow window = new DesktopWindow(screens[i].Bounds, _model);
                _windows.Add(window);
                window.Show();
            }
            _attachmentCountdown = 1;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _timer.Stop();
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            for (int i = 0; i < _windows.Count; i++)
            {
                _windows[i].Close();
            }
            _windows.Clear();
        }
    }

    internal sealed class DesktopWindow : Window
    {
        private readonly CreatureView _view;
        private IntPtr _handle;
        private IntPtr _desktopHost;

        public Rectangle ScreenBounds { get; private set; }
        public double DpiScale { get; private set; }

        public DesktopWindow(Rectangle bounds, CreatureModel model)
        {
            ScreenBounds = bounds;
            DpiScale = 1.0;
            AllowsTransparency = true;
            Background = System.Windows.Media.Brushes.Transparent;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            Topmost = false;
            Focusable = false;
            IsHitTestVisible = false;
            Left = bounds.Left;
            Top = bounds.Top;
            Width = Math.Max(1, bounds.Width);
            Height = Math.Max(1, bounds.Height);

            _view = new CreatureView(model, this);
            Content = _view;
            SourceInitialized += OnSourceInitialized;
        }

        private void OnSourceInitialized(object sender, EventArgs e)
        {
            _handle = new WindowInteropHelper(this).Handle;
            long exStyle = NativeMethods.GetWindowLongPtr(_handle, NativeMethods.GWL_EXSTYLE).ToInt64();
            exStyle |= NativeMethods.WS_EX_TRANSPARENT | NativeMethods.WS_EX_NOACTIVATE |
                       NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_LAYERED;
            NativeMethods.SetWindowLongPtr(_handle, NativeMethods.GWL_EXSTYLE, new IntPtr(exStyle));

            uint dpi = NativeMethods.GetDpiForWindowSafe(_handle);
            DpiScale = Math.Max(1.0, dpi / 96.0);
            AttachToDesktop();
        }

        public void RequestRender()
        {
            _view.InvalidateVisual();
        }

        public void EnsureDesktopAttachment()
        {
            if (_handle == IntPtr.Zero || !NativeMethods.IsWindow(_handle)) return;
            if (_desktopHost == IntPtr.Zero || !NativeMethods.IsWindow(_desktopHost))
            {
                AttachToDesktop();
            }
            else
            {
                PlaceAboveWallpaper();
            }
        }

        private void AttachToDesktop()
        {
            IntPtr host = DesktopHost.FindForScreen(ScreenBounds);
            if (host == IntPtr.Zero) return;

            _desktopHost = host;
            long style = NativeMethods.GetWindowLongPtr(_handle, NativeMethods.GWL_STYLE).ToInt64();
            style = (style & ~NativeMethods.WS_CHILD) | NativeMethods.WS_POPUP;
            NativeMethods.SetWindowLongPtr(_handle, NativeMethods.GWL_STYLE, new IntPtr(style));
            if (NativeMethods.GetParent(_handle) != IntPtr.Zero)
            {
                NativeMethods.SetParent(_handle, IntPtr.Zero);
            }

            PlaceAboveWallpaper();
        }

        private void PlaceAboveWallpaper()
        {
            if (_desktopHost == IntPtr.Zero || !NativeMethods.IsWindow(_desktopHost)) return;

            IntPtr currentProgman = NativeMethods.FindWindow("Progman", null);
            if (currentProgman == IntPtr.Zero) return;
            _desktopHost = currentProgman;

            IntPtr predecessor = NativeMethods.GetWindow(_desktopHost, NativeMethods.GW_HWNDPREV);
            uint flags = NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW |
                         NativeMethods.SWP_FRAMECHANGED;
            IntPtr insertAfter = predecessor;
            if (predecessor == IntPtr.Zero)
            {
                insertAfter = NativeMethods.HWND_TOP;
            }
            else if (predecessor == _handle)
            {
                flags |= NativeMethods.SWP_NOZORDER;
                insertAfter = NativeMethods.HWND_TOP;
            }
            NativeMethods.SetWindowPos(
                _handle,
                insertAfter,
                ScreenBounds.Left,
                ScreenBounds.Top,
                ScreenBounds.Width,
                ScreenBounds.Height,
                flags);
        }
    }

    internal sealed class CreatureView : FrameworkElement
    {
        private readonly CreatureModel _model;
        private readonly DesktopWindow _owner;
        private readonly SolidColorBrush _headBrush;
        private readonly Pen _bonePen;

        public CreatureView(CreatureModel model, DesktopWindow owner)
        {
            _model = model;
            _owner = owner;
            IsHitTestVisible = false;
            _headBrush = FrozenBrush(MediaColor.FromArgb(225, 226, 236, 233));
            _bonePen = CreatePen(MediaColor.FromArgb(205, 204, 219, 215), 1.25);
        }

        private static SolidColorBrush FrozenBrush(MediaColor color)
        {
            SolidColorBrush brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        private Point Local(double x, double y)
        {
            double dpi = _owner.DpiScale;
            return new Point((x - _owner.ScreenBounds.Left) / dpi, (y - _owner.ScreenBounds.Top) / dpi);
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            if (!_model.IsInitialized || _model.Segments.Count == 0) return;

            StreamGeometry skeleton = BuildSkeletonGeometry();
            dc.DrawGeometry(null, _bonePen, skeleton);
            DrawHead(dc, _bonePen);
        }

        private static Pen CreatePen(MediaColor color, double thickness)
        {
            Pen pen = new Pen(FrozenBrush(color), thickness);
            pen.StartLineCap = PenLineCap.Round;
            pen.EndLineCap = PenLineCap.Round;
            pen.LineJoin = PenLineJoin.Round;
            pen.Freeze();
            return pen;
        }

        private StreamGeometry BuildSkeletonGeometry()
        {
            StreamGeometry geometry = new StreamGeometry();
            using (StreamGeometryContext context = geometry.Open())
            {
                IList<Segment> segments = _model.Segments;
                for (int i = 0; i < segments.Count; i++)
                {
                    Segment segment = segments[i];
                    context.BeginFigure(Local(segment.Parent.X, segment.Parent.Y), false, false);
                    context.LineTo(Local(segment.X, segment.Y), true, false);
                }
            }
            geometry.Freeze();
            return geometry;
        }

        private void DrawHead(DrawingContext dc, Pen outline)
        {
            double r = 4.0 * _model.Scale;
            double angle = _model.AbsAngle;
            Vec forward = new Vec(Math.Cos(angle), Math.Sin(angle));
            Vec normal = forward.Perpendicular();
            Vec center = new Vec(_model.X, _model.Y);
            Vec nose = center + forward * (r * 1.65);
            Vec left = center - forward * (r * 0.65) + normal * (r * 0.85);
            Vec back = center - forward * (r * 1.05);
            Vec right = center - forward * (r * 0.65) - normal * (r * 0.85);
            StreamGeometry skull = new StreamGeometry();
            using (StreamGeometryContext context = skull.Open())
            {
                context.BeginFigure(Local(nose.X, nose.Y), true, true);
                context.LineTo(Local(left.X, left.Y), true, false);
                context.LineTo(Local(back.X, back.Y), true, false);
                context.LineTo(Local(right.X, right.Y), true, false);
            }
            skull.Freeze();
            dc.DrawGeometry(_headBrush, outline, skull);
        }
    }

    // Motion model adapted from MacSearlas' "Creepy Crawly Kinematics".
    internal abstract class SkeletonNode
    {
        public double X;
        public double Y;
        public double AbsAngle;
        public readonly List<Segment> Children = new List<Segment>();
        public virtual bool IsSegment { get { return false; } }
    }

    internal sealed class Segment : SkeletonNode
    {
        public readonly SkeletonNode Parent;
        public readonly double Size;
        public double RelAngle;
        public readonly double DefAngle;
        public readonly double Range;
        public readonly double Stiffness;
        public override bool IsSegment { get { return true; } }

        public Segment(SkeletonNode parent, double size, double angle, double range, double stiffness)
        {
            Parent = parent;
            parent.Children.Add(this);
            Size = size;
            RelAngle = angle;
            DefAngle = angle;
            AbsAngle = parent.AbsAngle + angle;
            Range = range;
            Stiffness = stiffness;
            UpdateRelative(false, true);
        }

        public void UpdateRelative(bool iter, bool flex)
        {
            RelAngle -= 2.0 * Math.PI * Math.Floor((RelAngle - DefAngle) / (2.0 * Math.PI) + 0.5);
            if (flex)
            {
                RelAngle = Math.Min(
                    DefAngle + Range / 2.0,
                    Math.Max(DefAngle - Range / 2.0, (RelAngle - DefAngle) / Stiffness + DefAngle));
            }
            AbsAngle = Parent.AbsAngle + RelAngle;
            X = Parent.X + Math.Cos(AbsAngle) * Size;
            Y = Parent.Y + Math.Sin(AbsAngle) * Size;
            if (iter)
            {
                for (int i = 0; i < Children.Count; i++)
                {
                    Children[i].UpdateRelative(true, flex);
                }
            }
        }

        public void Follow(bool iter)
        {
            double dx = X - Parent.X;
            double dy = Y - Parent.Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);
            if (distance < 0.000001)
            {
                dx = Math.Cos(AbsAngle);
                dy = Math.Sin(AbsAngle);
                distance = 1.0;
            }
            X = Parent.X + Size * dx / distance;
            Y = Parent.Y + Size * dy / distance;
            AbsAngle = Math.Atan2(Y - Parent.Y, X - Parent.X);
            RelAngle = AbsAngle - Parent.AbsAngle;
            UpdateRelative(false, true);
            if (iter)
            {
                for (int i = 0; i < Children.Count; i++) Children[i].Follow(true);
            }
        }
    }

    internal class LimbSystem
    {
        protected readonly Segment End;
        protected readonly CreatureModel Creature;
        protected readonly List<Segment> Nodes = new List<Segment>();
        protected readonly SkeletonNode Hip;
        protected readonly double Speed;
        public virtual int StepState { get { return 0; } }

        public LimbSystem(Segment end, int length, double speed, CreatureModel creature)
        {
            End = end;
            Creature = creature;
            Speed = speed;
            SkeletonNode node = end;
            for (int i = 0; i < Math.Max(1, length); i++)
            {
                Segment segment = node as Segment;
                if (segment == null) break;
                Nodes.Insert(0, segment);
                node = segment.Parent;
                if (!node.IsSegment) break;
            }
            Hip = Nodes[0].Parent;
        }

        protected void MoveTo(double x, double y)
        {
            Nodes[0].UpdateRelative(true, true);
            double dx = x - End.X;
            double dy = y - End.Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);
            double length = Math.Max(0, distance - Speed);
            for (int i = Nodes.Count - 1; i >= 0; i--)
            {
                Segment node = Nodes[i];
                double angle = Math.Atan2(node.Y - y, node.X - x);
                node.X = x + length * Math.Cos(angle);
                node.Y = y + length * Math.Sin(angle);
                x = node.X;
                y = node.Y;
                length = node.Size;
            }
            for (int i = 0; i < Nodes.Count; i++)
            {
                Segment node = Nodes[i];
                node.AbsAngle = Math.Atan2(node.Y - node.Parent.Y, node.X - node.Parent.X);
                node.RelAngle = node.AbsAngle - node.Parent.AbsAngle;
                for (int childIndex = 0; childIndex < node.Children.Count; childIndex++)
                {
                    Segment child = node.Children[childIndex];
                    if (!Nodes.Contains(child))
                    {
                        child.UpdateRelative(true, false);
                    }
                }
            }
        }

        public virtual void Update(double x, double y)
        {
            MoveTo(x, y);
        }
    }

    internal sealed class LegSystem : LimbSystem
    {
        private double _goalX;
        private double _goalY;
        private int _step;
        private double _forwardness;
        private readonly double _reach;
        private readonly double _swing;
        private readonly double _swingOffset;

        public override int StepState { get { return _step; } }

        public LegSystem(Segment end, int length, double speed, CreatureModel creature)
            : base(end, length, speed, creature)
        {
            _goalX = end.X;
            _goalY = end.Y;
            _reach = 0.9 * Distance(end.X, end.Y, Hip.X, Hip.Y);
            double relativeAngle = creature.AbsAngle - Math.Atan2(end.Y - Hip.Y, end.X - Hip.X);
            relativeAngle -= 2.0 * Math.PI * Math.Floor(relativeAngle / (2.0 * Math.PI) + 0.5);
            _swing = -relativeAngle + (relativeAngle < 0 ? 1 : -1) * Math.PI / 2.0;
            _swingOffset = creature.AbsAngle - Hip.AbsAngle;
        }

        public override void Update(double x, double y)
        {
            MoveTo(_goalX, _goalY);
            if (_step == 0)
            {
                double distance = Distance(End.X, End.Y, _goalX, _goalY);
                if (distance > Creature.Scale && Creature.AllowNewSteps)
                {
                    _step = 1;
                    _goalX = Hip.X + _reach * Math.Cos(_swing + Hip.AbsAngle + _swingOffset)
                             + (2.0 * Creature.NextRandom() - 1.0) * _reach / 2.0;
                    _goalY = Hip.Y + _reach * Math.Sin(_swing + Hip.AbsAngle + _swingOffset)
                             + (2.0 * Creature.NextRandom() - 1.0) * _reach / 2.0;
                }
            }
            else
            {
                double theta = Math.Atan2(End.Y - Hip.Y, End.X - Hip.X) - Hip.AbsAngle;
                double distance = Distance(End.X, End.Y, Hip.X, Hip.Y);
                double forwardness = distance * Math.Cos(theta);
                double delta = _forwardness - forwardness;
                _forwardness = forwardness;
                if (delta * delta < Creature.Scale * Creature.Scale)
                {
                    _step = 0;
                    _goalX = End.X;
                    _goalY = End.Y;
                }
            }
        }

        private static double Distance(double x1, double y1, double x2, double y2)
        {
            double dx = x1 - x2;
            double dy = y1 - y2;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }

    internal sealed class CreatureModel : SkeletonNode
    {
        private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());
        private readonly List<Segment> _segments = new List<Segment>();
        private readonly List<LimbSystem> _systems = new List<LimbSystem>();
        private double _fSpeed;
        private double _fAccel;
        private double _fFric;
        private double _fRes;
        private double _fThresh;
        private double _rSpeed;
        private double _rAccel;
        private double _rFric;
        private double _rRes;
        private double _rThresh;
        private double _physicsAccumulator;
        private Vec _lastTarget;
        private bool _hasLastTarget;
        private double _idleSeconds;
        private bool _isSettling;

        public IList<Segment> Segments { get { return _segments; } }
        public bool IsInitialized { get; private set; }
        public bool IsSleeping { get; private set; }
        public bool AllowNewSteps { get; private set; }
        public double Scale { get; private set; }
        public int LegPairCount { get; private set; }
        public int TailSegmentCount { get; private set; }

        public CreatureModel(int ignoredNodeCount)
        {
            Scale = 1.0;
        }

        public void Reset(Vec head, double scale)
        {
            Scale = Math.Max(1.0, scale);
            X = head.X;
            Y = head.Y;
            AbsAngle = 0;
            Children.Clear();
            _segments.Clear();
            _systems.Clear();
            _fSpeed = 0;
            _rSpeed = 0;
            _physicsAccumulator = 0;
            _lastTarget = head;
            _hasLastTarget = true;
            _idleSeconds = 0;
            _isSettling = false;
            IsSleeping = false;
            AllowNewSteps = true;

            LegPairCount = _random.Next(1, 13);
            TailSegmentCount = (int)Math.Floor(4.0 + _random.NextDouble() * LegPairCount * 8.0);
            double s = 8.0 / Math.Sqrt(LegPairCount) * Scale;
            _fAccel = s * 10.0;
            _fFric = s * 2.0;
            _fRes = 0.5;
            _fThresh = 16.0 * Scale;
            _rAccel = 0.5;
            _rFric = 0.085;
            _rRes = 0.5;
            _rThresh = 0.3;

            SetupLizard(s, LegPairCount, TailSegmentCount);
            IsInitialized = true;
        }

        public void Update(Vec target, double dt, double scale)
        {
            if (!IsInitialized)
            {
                Reset(target, scale);
                return;
            }

            double targetDx = target.X - _lastTarget.X;
            double targetDy = target.Y - _lastTarget.Y;
            bool targetMoved = !_hasLastTarget ||
                               targetDx * targetDx + targetDy * targetDy > 0.0625 * Scale * Scale;
            _lastTarget = target;
            _hasLastTarget = true;

            if (targetMoved)
            {
                _idleSeconds = 0;
                _isSettling = false;
                IsSleeping = false;
                AllowNewSteps = true;
            }
            else
            {
                _idleSeconds += dt;
            }

            if (IsSleeping) return;

            double rootDx = target.X - X;
            double rootDy = target.Y - Y;
            double rootDistance = Math.Sqrt(rootDx * rootDx + rootDy * rootDy);
            if (_idleSeconds >= 0.08 && rootDistance <= 18.0 * Scale)
            {
                _isSettling = true;
                AllowNewSteps = false;
            }

            _physicsAccumulator += dt * 60.0;
            int steps = Math.Min(4, (int)Math.Floor(_physicsAccumulator));
            for (int i = 0; i < steps; i++)
            {
                FollowFrame(target.X, target.Y);
                _physicsAccumulator -= 1.0;
            }
            if (_physicsAccumulator > 4.0) _physicsAccumulator = 1.0;

            if (_isSettling && _idleSeconds >= 0.65 && AllLegsGrounded() &&
                Math.Abs(_fSpeed) < 0.15 * Scale && Math.Abs(_rSpeed) < 0.002)
            {
                IsSleeping = true;
                _physicsAccumulator = 0;
            }
        }

        private void FollowFrame(double targetX, double targetY)
        {
            double dx = X - targetX;
            double dy = Y - targetY;
            double distance = Math.Sqrt(dx * dx + dy * dy);
            double targetAngle = Math.Atan2(targetY - Y, targetX - X);

            double accel = _fAccel;
            if (_systems.Count > 0)
            {
                int grounded = 0;
                for (int i = 0; i < _systems.Count; i++) grounded += _systems[i].StepState == 0 ? 1 : 0;
                accel *= grounded / (double)_systems.Count;
            }
            if (!_isSettling && distance > _fThresh) _fSpeed += accel;
            _fSpeed *= _isSettling ? 0.22 : 1.0 - _fRes;
            double speed = Math.Max(0, _fSpeed - _fFric);

            double difference = AbsAngle - targetAngle;
            difference -= 2.0 * Math.PI * Math.Floor(difference / (2.0 * Math.PI) + 0.5);
            if (!_isSettling && Math.Abs(difference) > _rThresh && distance > _fThresh)
            {
                _rSpeed -= _rAccel * (difference > 0 ? 1 : -1);
            }
            _rSpeed *= _isSettling ? 0.20 : 1.0 - _rRes;
            if (Math.Abs(_rSpeed) > _rFric)
            {
                _rSpeed -= _rFric * (_rSpeed > 0 ? 1 : -1);
            }
            else
            {
                _rSpeed = 0;
            }

            AbsAngle += _rSpeed;
            AbsAngle -= 2.0 * Math.PI * Math.Floor(AbsAngle / (2.0 * Math.PI) + 0.5);
            X += speed * Math.Cos(AbsAngle);
            Y += speed * Math.Sin(AbsAngle);

            AbsAngle += Math.PI;
            for (int i = 0; i < Children.Count; i++) Children[i].Follow(true);
            for (int i = 0; i < _systems.Count; i++) _systems[i].Update(targetX, targetY);
            AbsAngle -= Math.PI;
        }

        private bool AllLegsGrounded()
        {
            for (int i = 0; i < _systems.Count; i++)
            {
                if (_systems[i].StepState != 0) return false;
            }
            return true;
        }

        private void SetupLizard(double s, int legs, int tail)
        {
            SkeletonNode spinal = this;
            for (int i = 0; i < 6; i++)
            {
                spinal = AddSegment(spinal, s * 4, 0, Math.PI * 2.0 / 3.0, 1.1);
                for (int side = -1; side <= 1; side += 2)
                {
                    SkeletonNode node = AddSegment(spinal, s * 3, side, 0.1, 2);
                    for (int branch = 0; branch < 3; branch++)
                        node = AddSegment(node, s * 0.1, -side * 0.1, 0.1, 2);
                }
            }

            for (int leg = 0; leg < legs; leg++)
            {
                if (leg > 0)
                {
                    for (int section = 0; section < 6; section++)
                    {
                        spinal = AddSegment(spinal, s * 4, 0, 1.571, 1.5);
                        for (int side = -1; side <= 1; side += 2)
                        {
                            SkeletonNode rib = AddSegment(spinal, s * 3, side * 1.571, 0.1, 1.5);
                            for (int part = 0; part < 3; part++)
                                rib = AddSegment(rib, s * 3, -side * 0.3, 0.1, 2);
                        }
                    }
                }

                for (int side = -1; side <= 1; side += 2)
                {
                    Segment upper = AddSegment(spinal, s * 12, side * 0.785, 0, 8);
                    Segment lower = AddSegment(upper, s * 16, -side * 0.785, 2.0 * Math.PI, 1);
                    Segment foot = AddSegment(lower, s * 16, side * 1.571, Math.PI, 2);
                    for (int toe = 0; toe < 4; toe++)
                        AddSegment(foot, s * 4, (toe / 3.0 - 0.5) * 1.571, 0.1, 4);
                    _systems.Add(new LegSystem(foot, 3, s * 12, this));
                }
            }

            for (int i = 0; i < tail; i++)
            {
                spinal = AddSegment(spinal, s * 4, 0, Math.PI * 2.0 / 3.0, 1.1);
                for (int side = -1; side <= 1; side += 2)
                {
                    SkeletonNode rib = AddSegment(spinal, s * 3, side, 0.1, 2);
                    for (int part = 0; part < 3; part++)
                        rib = AddSegment(rib, s * 3 * (tail - i) / tail, -side * 0.1, 0.1, 2);
                }
            }
        }

        private Segment AddSegment(SkeletonNode parent, double size, double angle, double range, double stiffness)
        {
            Segment segment = new Segment(parent, size, angle, range, stiffness);
            _segments.Add(segment);
            return segment;
        }

        public double NextRandom()
        {
            return _random.NextDouble();
        }
    }

    internal struct Vec
    {
        public double X;
        public double Y;

        public Vec(double x, double y)
        {
            X = x;
            Y = y;
        }

        public Vec Perpendicular() { return new Vec(-Y, X); }
        public static Vec operator +(Vec a, Vec b) { return new Vec(a.X + b.X, a.Y + b.Y); }
        public static Vec operator -(Vec a, Vec b) { return new Vec(a.X - b.X, a.Y - b.Y); }
        public static Vec operator *(Vec a, double b) { return new Vec(a.X * b, a.Y * b); }
    }

    internal static class DesktopHost
    {
        public static IntPtr FindForScreen(Rectangle screen)
        {
            IntPtr progman = NativeMethods.FindWindow("Progman", null);
            return progman;
        }
    }

    internal static class DesktopShell
    {
        private const int ObjIdClient = unchecked((int)0xFFFFFFFC);
        private static readonly Guid IidAccessible =
            new Guid("618736E0-3C3D-11CF-810C-00AA00389B71");

        public static bool IsBlankAt(NativeMethods.POINT point)
        {
            IntPtr listView = FindListViewAt(point);
            if (listView == IntPtr.Zero) return false;
            if (!IsDesktopSurfaceWindow(NativeMethods.WindowFromPoint(point), listView))
            {
                return false;
            }

            object accessibleObject = null;
            try
            {
                Guid iid = IidAccessible;
                int result = NativeMethods.AccessibleObjectFromWindow(
                    listView, ObjIdClient, ref iid, out accessibleObject);
                if (result < 0 || accessibleObject == null) return false;

                Accessibility.IAccessible accessible =
                    accessibleObject as Accessibility.IAccessible;
                if (accessible == null) return false;

                object hit = accessible.accHitTest(point.X, point.Y);
                return hit is int && (int)hit == 0;
            }
            catch (COMException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
            finally
            {
                if (accessibleObject != null && Marshal.IsComObject(accessibleObject))
                {
                    Marshal.ReleaseComObject(accessibleObject);
                }
            }
        }

        private static bool IsDesktopSurfaceWindow(IntPtr window, IntPtr listView)
        {
            while (window != IntPtr.Zero)
            {
                if (window == listView) return true;
                window = NativeMethods.GetParent(window);
            }
            return false;
        }

        private static IntPtr FindListViewAt(NativeMethods.POINT point)
        {
            IntPtr found = IntPtr.Zero;
            NativeMethods.EnumWindows(delegate(IntPtr topLevel, IntPtr parameter)
            {
                IntPtr shellView = NativeMethods.FindWindowEx(
                    topLevel, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (shellView == IntPtr.Zero) return true;

                IntPtr listView = NativeMethods.FindWindowEx(
                    shellView, IntPtr.Zero, "SysListView32", "FolderView");
                if (listView == IntPtr.Zero)
                {
                    listView = NativeMethods.FindWindowEx(
                        shellView, IntPtr.Zero, "SysListView32", null);
                }
                if (listView == IntPtr.Zero) return true;

                NativeMethods.RECT bounds;
                if (!NativeMethods.GetWindowRect(listView, out bounds)) return true;
                if (point.X < bounds.Left || point.X >= bounds.Right ||
                    point.Y < bounds.Top || point.Y >= bounds.Bottom) return true;

                found = listView;
                return false;
            }, IntPtr.Zero);
            return found;
        }
    }

    internal static class NativeMethods
    {
        public const int GWL_STYLE = -16;
        public const int GWL_EXSTYLE = -20;
        public const long WS_CHILD = 0x40000000L;
        public const long WS_POPUP = unchecked((long)0x80000000L);
        public const long WS_EX_TRANSPARENT = 0x00000020L;
        public const long WS_EX_TOOLWINDOW = 0x00000080L;
        public const long WS_EX_LAYERED = 0x00080000L;
        public const long WS_EX_NOACTIVATE = 0x08000000L;
        public const uint SWP_FRAMECHANGED = 0x0020;
        public const uint SWP_NOZORDER = 0x0004;
        public const uint SWP_NOACTIVATE = 0x0010;
        public const uint SWP_SHOWWINDOW = 0x0040;
        public const uint SMTO_NORMAL = 0x0000;
        public const int WH_MOUSE_LL = 14;
        public const int WM_LBUTTONDOWN = 0x0201;
        public const int WM_LBUTTONUP = 0x0202;
        public static readonly IntPtr HWND_TOP = IntPtr.Zero;
        public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        public const uint GW_HWNDPREV = 3;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MSLLHOOKSTRUCT
        {
            public POINT Point;
            public uint MouseData;
            public uint Flags;
            public uint Time;
            public UIntPtr ExtraInfo;
        }

        public delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);
        public delegate IntPtr LowLevelMouseProc(int code, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetCursorPos(out POINT point);

        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(POINT point);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindow(string className, string windowName);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindowEx(IntPtr parent, IntPtr childAfter, string className, string windowName);

        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(
            int hookId, LowLevelMouseProc callback, IntPtr module, uint threadId);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnhookWindowsHookEx(IntPtr hook);

        [DllImport("user32.dll")]
        public static extern IntPtr CallNextHookEx(
            IntPtr hook, int code, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr GetModuleHandle(string moduleName);

        [DllImport("oleacc.dll")]
        public static extern int AccessibleObjectFromWindow(
            IntPtr hwnd,
            int objectId,
            ref Guid iid,
            [MarshalAs(UnmanagedType.Interface)] out object accessibleObject);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);

        [DllImport("user32.dll")]
        public static extern IntPtr SetParent(IntPtr child, IntPtr newParent);

        [DllImport("user32.dll")]
        public static extern IntPtr GetParent(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindow(IntPtr hwnd, uint command);

        [DllImport("user32.dll")]
        public static extern bool IsWindow(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int width, int height, uint flags);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hwnd, int index);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
        private static extern IntPtr GetWindowLong32(IntPtr hwnd, int index);

        public static IntPtr GetWindowLongPtr(IntPtr hwnd, int index)
        {
            return IntPtr.Size == 8 ? GetWindowLongPtr64(hwnd, index) : GetWindowLong32(hwnd, index);
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hwnd, int index, IntPtr value);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern IntPtr SetWindowLong32(IntPtr hwnd, int index, IntPtr value);

        public static IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr value)
        {
            return IntPtr.Size == 8 ? SetWindowLongPtr64(hwnd, index, value) : SetWindowLong32(hwnd, index, value);
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr hwnd, System.Text.StringBuilder className, int maxCount);

        public static string GetClassName(IntPtr hwnd)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder(256);
            GetClassName(hwnd, builder, builder.Capacity);
            return builder.ToString();
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SendMessageTimeout(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam, uint flags, uint timeout, out IntPtr result);

        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hwnd);

        public static uint GetDpiForWindowSafe(IntPtr hwnd)
        {
            try { return GetDpiForWindow(hwnd); }
            catch (EntryPointNotFoundException) { return 96; }
        }

        [DllImport("user32.dll")]
        private static extern bool SetProcessDpiAwarenessContext(IntPtr value);

        public static void TryEnablePerMonitorDpi()
        {
            try { SetProcessDpiAwarenessContext(new IntPtr(-4)); }
            catch (EntryPointNotFoundException) { }
        }

        [DllImport("user32.dll")]
        public static extern bool DestroyIcon(IntPtr handle);
    }
}
