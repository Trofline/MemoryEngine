//using MemoryEngine;
//using System;
//using System.Diagnostics;
//using System.Numerics;
//using System.Runtime.InteropServices;
//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Interop;
//using System.Windows.Media;
//using System.Windows.Shapes;
//using System.Windows.Threading;
//using MemoryEngine.External;
//using MemoryEngine.Game;

//namespace CheatMenu
//{
//    public partial class MainWindow : Window
//    {
//        [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);
//        [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
//        [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr hWnd, int nIndex);
//        [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

//        private DispatcherTimer _timer;
//        private Engine _engine;
//        private MatrixSettings _currentSettings;

//        public MainWindow()
//        {
//            InitializeComponent();         
//        }

//private void Window_Loaded(object sender, RoutedEventArgs e)
//{
//    IntPtr hwnd = new WindowInteropHelper(this).Handle;
//    int extendedStyle = GetWindowLong(hwnd, -20);
//    SetWindowLong(hwnd, -20, extendedStyle | 0x80000 | 0x20);

//    try
//    {
//        _engine = new Engine("ac_client", force32Bit: true);
//        _currentSettings = new MatrixSettings { IsColumnMajor = false, IsZeroToOneRange = false, InvertY = true };

//        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(4) };
//        _timer.Tick += UpdateLoop;
//        _timer.Start();


//    }
//    catch (Exception ex) { Debug.WriteLine("[INIT FEHLER] " + ex.Message); }
//}


//        private void UpdateLoop(object sender, EventArgs e)
//        {
//            try
//            {
//                PositionOverlay();
//                ESP_Canvas.Children.Clear();

//                byte[] mBytes = _engine.ReadMemory((IntPtr)0x501AE8, 64);
//                if (mBytes == null) return;

//                ViewMatrix matrix = ViewMatrix.FromBytes(mBytes, _currentSettings);
//                IntPtr eList = _engine.ReadPointer(_engine.ModuleBase + 0x10F4F8);
//                int numEntities = _engine.ReadInt(_engine.ModuleBase + 0x10F500);

//                for (int i = 0; i < numEntities; i++)
//                {
//                    IntPtr entity = _engine.ReadPointer(eList + (i * 4));
//                    if (entity == IntPtr.Zero) continue;

//                    float x = _engine.ReadFloat(entity + 0x34);
//                    float y = _engine.ReadFloat(entity + 0x38);
//                    float z = _engine.ReadFloat(entity + 0x3C);
//                    int hp = _engine.ReadInt(entity + 0xF8);

//                    if (hp > 0 && hp <= 100)
//                    {
//                        bool onScreen = matrix.WorldToScreen(new Vector3(x, y, z), out Vector2 feetPos, (int)ActualWidth, (int)ActualHeight);

//                        // 1. Box nur zeichnen, wenn sichtbar
//                        if (onScreen)
//                        {
//                            matrix.WorldToScreen(new Vector3(x, y, z + 4.0f), out Vector2 headPos, (int)ActualWidth, (int)ActualHeight);
//                            float boxHeight = Math.Abs(feetPos.Y - headPos.Y);
//                            float boxWidth = boxHeight / 2.0f;

//                            var box = new Rectangle { Width = boxWidth, Height = boxHeight, Stroke = Brushes.Red, StrokeThickness = 2 };
//                            Canvas.SetLeft(box, feetPos.X - (boxWidth / 2f));
//                            Canvas.SetTop(box, headPos.Y);
//                            ESP_Canvas.Children.Add(box);
//                        }

//                        // 2. Linie: Wenn onScreen, Ziel = Gegner-Füße, wenn NEIN, Ziel = Bildschirm-Mitte unten (neutral)
//                        float targetX = onScreen ? feetPos.X : (float)ActualWidth / 2;
//                        float targetY = onScreen ? feetPos.Y : (float)ActualHeight;

//                        var line = new Line
//                        {
//                            X1 = ActualWidth / 2,
//                            Y1 = ActualHeight - 100,
//                            X2 = targetX,
//                            Y2 = targetY,
//                            Stroke = Brushes.Yellow,
//                            StrokeThickness = 1.5,
//                            SnapsToDevicePixels = true,
//                            UseLayoutRounding = true
//                        };
//                        ESP_Canvas.Children.Add(line);
//                    }
//                }
//            }
//            catch { }
//        }

//        private void PositionOverlay()
//        {
//            var procs = Process.GetProcessesByName("ac_client");
//            if (procs.Length > 0)
//            {
//                GetWindowRect(procs[0].MainWindowHandle, out RECT r);
//                this.Left = r.Left; this.Top = r.Top;
//                this.Width = r.Right - r.Left; this.Height = r.Bottom - r.Top;
//            }
//        }
//    }
//}