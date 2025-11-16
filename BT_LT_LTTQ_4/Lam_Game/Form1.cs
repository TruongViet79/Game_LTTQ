using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace SlimeAnimation
{
    public partial class MainForm : Form
    {
        private Slime slime;
        private Image backgroundImage;
        private Timer gameTimer;
        private Timer spawnTimer;
        private Random random;
        private int slimeCount = 0; // 🆕 Đếm số slime đã tạo

        private string backgroundPath = "background.jpg";
        private string[] slimeFramePaths = new string[]
        {
            "tile01.png",
            "tile02.png",
            "tile03.png",
            "tile04.png",
            "tile05.png",
            "tile06.png",
            "tile07.png"
        };

        public MainForm()
        {
            InitializeComponent();
            InitializeAnimation();
        }

        private void InitializeAnimation()
        {
            random = new Random();

            if (File.Exists(backgroundPath))
            {
                backgroundImage = Image.FromFile(backgroundPath);
            }
            else
            {
                backgroundImage = CreateFallbackBackground();
            }

            this.DoubleBuffered = true;
            this.Size = new Size(1000, 600);
            this.Text = "Slime Animation - Slime Count: 0"; // 🆕 Hiển thị số slime

            // Timer cho game loop
            gameTimer = new Timer();
            gameTimer.Interval = 16; // ~60 FPS
            gameTimer.Tick += GameLoop;
            gameTimer.Start();

            // 🆕 Timer spawn slime mỗi 5 giây
            spawnTimer = new Timer();
            spawnTimer.Interval = 10000; 
            spawnTimer.Tick += SpawnTimer_Tick;
            spawnTimer.Start();

            // Tạo slime đầu tiên ngay lập tức
            CreateSlime();
        }

        private void SpawnTimer_Tick(object sender, EventArgs e)
        {
            Console.WriteLine($"⏰ Timer tick! Tạo slime mới...");
            CreateSlime();
        }

        private void CreateSlime()
        {
            try
            {
                slimeCount++;
                this.Text = $"Slime Animation - Slime Count: {slimeCount}";

                Console.WriteLine($"🆕 Tạo slime thứ {slimeCount}...");

                // Dispose slime cũ trước
                if (slime != null)
                {
                    Console.WriteLine("🗑️ Đang dispose slime cũ...");
                    slime.Dispose();
                    slime = null;
                }

                Image[] frames = LoadIndividualFrames();

                if (frames.Length == 0)
                {
                    MessageBox.Show("Không tìm thấy file slime nào!");
                    return;
                }

                float zoomScale = 1.8f;
                frames = ScaleFrames(frames, zoomScale);

                // Random spawn từ trái hoặc phải
                int direction = random.Next(2) == 0 ? 1 : -1;
                int startX, targetX;
                int quarterWidth = this.ClientSize.Width / 4;

                if (direction == 1) // Spawn từ trái
                {
                    startX = -frames[0].Width;
                    targetX = random.Next(quarterWidth, this.ClientSize.Width / 2);
                }
                else // Spawn từ phải
                {
                    startX = this.ClientSize.Width;
                    targetX = random.Next(this.ClientSize.Width / 2, this.ClientSize.Width - quarterWidth);
                }

                // 🆕 ĐỘ CAO CỐ ĐỊNH - chỉ định pixel cụ thể (ví dụ: 300px từ trên xuống)
                int fixedHeight = 470;
                int startY = fixedHeight;

                // Tạo slime mới
                slime = new Slime(frames, new Point(startX, startY), direction, targetX);

                Console.WriteLine($"🎯 Slime {slimeCount} created: Direction={direction}, StartX={startX}, TargetX={targetX}, FixedHeight={fixedHeight}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tạo slime: {ex.Message}");
                Console.WriteLine($"❌ Lỗi: {ex.Message}");
            }
        }

        private Image[] LoadIndividualFrames()
        {
            System.Collections.Generic.List<Image> frames = new System.Collections.Generic.List<Image>();

            foreach (string framePath in slimeFramePaths)
            {
                if (File.Exists(framePath))
                {
                    try
                    {
                        Image frame = Image.FromFile(framePath);
                        frames.Add(frame);
                        Console.WriteLine($"✅ Đã tải: {framePath} ({frame.Width}x{frame.Height})");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Lỗi tải {framePath}: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"⚠️ Không tìm thấy: {framePath}");
                }
            }

            if (frames.Count == 0)
            {
                frames.Add(CreateFallbackSlime());
                Console.WriteLine("⚠️ Sử dụng slime mặc định");
            }
            else
            {
                Console.WriteLine($"✅ Đã tải thành công {frames.Count}/{slimeFramePaths.Length} frames");
            }

            return frames.ToArray();
        }

        private Image[] ScaleFrames(Image[] frames, float scale)
        {
            Image[] scaledFrames = new Image[frames.Length];

            for (int i = 0; i < frames.Length; i++)
            {
                int newWidth = (int)(frames[i].Width * scale);
                int newHeight = (int)(frames[i].Height * scale);

                Bitmap scaledFrame = new Bitmap(newWidth, newHeight);
                using (Graphics g = Graphics.FromImage(scaledFrame))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                    g.DrawImage(frames[i], 0, 0, newWidth, newHeight);
                }

                scaledFrames[i] = scaledFrame;
                frames[i].Dispose();
            }

            Console.WriteLine($"🔍 Đã phóng to frames lên {scale}x");
            return scaledFrames;
        }

        private void GameLoop(object sender, EventArgs e)
        {
            if (slime != null)
            {
                slime.Update();
            }

            this.Invalidate(); // 🆕 Quan trọng: bắt vẽ lại form
        }

        private void MainForm_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            // Vẽ background
            if (backgroundImage != null)
            {
                g.DrawImage(backgroundImage, 0, 0, this.ClientSize.Width, this.ClientSize.Height);
            }

            // Vẽ slime
            if (slime != null)
            {
                slime.Draw(g);
                Console.WriteLine($"🎨 Đang vẽ slime tại Position: {slime.Position}");
            }
            else
            {
                Console.WriteLine("🎨 Không có slime để vẽ");
            }
        }

        private Bitmap CreateFallbackBackground()
        {
            Bitmap bmp = new Bitmap(800, 600);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                using (Brush skyBrush = new SolidBrush(Color.LightBlue))
                using (Brush grassBrush = new SolidBrush(Color.Green))
                {
                    g.FillRectangle(skyBrush, 0, 0, 800, 400);
                    g.FillRectangle(grassBrush, 0, 400, 800, 200);
                }
            }
            return bmp;
        }

        private Bitmap CreateFallbackSlime()
        {
            Bitmap slime = new Bitmap(50, 50);
            using (Graphics g = Graphics.FromImage(slime))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                using (Brush slimeBrush = new SolidBrush(Color.LimeGreen))
                {
                    g.FillEllipse(slimeBrush, 5, 5, 40, 40);
                }
            }
            return slime;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            slime?.Dispose();
            gameTimer?.Stop();
            spawnTimer?.Stop();
            backgroundImage?.Dispose();
            base.OnFormClosing(e);
        }
    }
}