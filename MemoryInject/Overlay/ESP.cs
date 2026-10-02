using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MemoryEngine.Overlay
{
    public partial class ESP : Form
    {
        private System.Windows.Forms.Timer renderTimer;

        public static void Launch()
        {
            Thread t = new Thread(() =>
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new ESP());
            });
            t.SetApartmentState(ApartmentState.STA);
            t.IsBackground = true;
            t.Start();
        }

        public ESP()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.StartPosition = FormStartPosition.Manual;
            this.BackColor = Color.Magenta;
            this.TransparencyKey = Color.Magenta;
            this.DoubleBuffered = true;
            this.Bounds = Screen.PrimaryScreen.Bounds;

            // Click-Through aktivieren (Mauseingaben gehen durch)
            int initialStyle = GetWindowLong(this.Handle, -20);
            SetWindowLong(this.Handle, -20, initialStyle | 0x80000 | 0x20);

            // Render-Timer (60 FPS)
            renderTimer = new System.Windows.Forms.Timer();
            renderTimer.Interval = 16;
            renderTimer.Tick += (s, e) => this.Invalidate();
            renderTimer.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;

            lock (EspData.LockObject)
            {
                foreach (var enemy in EspData.Enemies)
                {
                    float boxHeight = Math.Abs(enemy.FeetPos.Y - enemy.HeadPos.Y);
                    float boxWidth = boxHeight / 2f;

                    float boxX = enemy.FeetPos.X - (boxWidth / 2f);
                    float boxY = enemy.HeadPos.Y;

                    // Nutzt die individuelle Farbe des jeweiligen Spielers!
                    using (Pen pen = new Pen(enemy.BoxColor, 2))
                    {
                        g.DrawRectangle(pen, boxX, boxY, boxWidth, boxHeight);
                    }

                    // Text zusammenbauen (Distanz + optionaler Custom-Text wie Name oder HP)
                    string displayText = $"{Math.Round(enemy.Distance, 1)}m";
                    if (!string.IsNullOrEmpty(enemy.CustomText))
                    {
                        displayText = $"{enemy.CustomText} ({displayText})";
                    }

                    using (Font font = new Font("Arial", 9, FontStyle.Bold))
                    {
                        SizeF textSize = g.MeasureString(displayText, font);
                        g.DrawString(displayText, font, new SolidBrush(enemy.BoxColor), boxX + (boxWidth / 2f) - (textSize.Width / 2f), boxY + boxHeight + 2);
                    }
                }
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);
    }

    public class EnemyScreenData
    {
        public Vector2 HeadPos { get; set; }
        public Vector2 FeetPos { get; set; }
        public float Distance { get; set; }

        public Color BoxColor { get; set; } = Color.LimeGreen; 
        public string CustomText { get; set; } = string.Empty;   
    }

    public static class EspData
    {
        public static readonly object LockObject = new object();
        public static List<EnemyScreenData> Enemies = new List<EnemyScreenData>();
    }
}