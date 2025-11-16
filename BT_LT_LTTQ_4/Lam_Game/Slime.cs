using System;
using System.Drawing;
using System.Windows.Forms;

namespace SlimeAnimation
{
    public class Slime : IDisposable
    {
        public Image[] Frames { get; private set; }
        public Point Position { get; private set; }
        public int CurrentFrameIndex { get; private set; }
        public int Direction { get; private set; }
        public int Speed { get; private set; }
        public int TargetX { get; private set; }
        public bool HasReachedTarget { get; private set; }

        private Timer animationTimer;

        public Slime(Image[] frames, Point startPosition, int direction, int targetX)
        {
            Frames = frames;
            Position = startPosition;
            Direction = direction;
            CurrentFrameIndex = 0;
            Speed = 2;
            TargetX = targetX;
            HasReachedTarget = false;

            // Timer cho animation - vẫn chạy NGAY CẢ KHI ĐÃ DỪNG
            animationTimer = new Timer();
            animationTimer.Interval = 100;
            animationTimer.Tick += (s, e) =>
            {
                CurrentFrameIndex = (CurrentFrameIndex + 1) % Frames.Length;
            };
            animationTimer.Start();
        }

        public void Update()
        {
            // 🆕 CHỈ di chuyển nếu CHƯA đến đích
            if (!HasReachedTarget)
            {
                int newX = Position.X + (Speed * Direction);

                if ((Direction == 1 && newX >= TargetX) ||
                    (Direction == -1 && newX <= TargetX))
                {
                    newX = TargetX;
                    HasReachedTarget = true; // 🆕 Đánh dấu đã đến đích
                    Console.WriteLine("🎯 Slime đã đến vị trí đích!");
                }

                Position = new Point(newX, Position.Y);
            }
            // 🆕 Nếu đã đến đích thì KHÔNG làm gì cả, chỉ giữ nguyên vị trí
        }

        public void Draw(Graphics g)
        {
            if (Frames != null && Frames.Length > 0 && CurrentFrameIndex < Frames.Length)
            {
                if (Direction == -1)
                {
                    g.DrawImage(Frames[CurrentFrameIndex],
                               Position.X + Frames[CurrentFrameIndex].Width,
                               Position.Y,
                               -Frames[CurrentFrameIndex].Width,
                               Frames[CurrentFrameIndex].Height);
                }
                else
                {
                    g.DrawImage(Frames[CurrentFrameIndex], Position);
                }
            }
        }

        public Image GetCurrentFrame()
        {
            if (Frames != null && CurrentFrameIndex < Frames.Length)
            {
                return Frames[CurrentFrameIndex];
            }
            return null;
        }

        public void Dispose()
        {
            animationTimer?.Stop();
            animationTimer?.Dispose();

            if (Frames != null)
            {
                foreach (var frame in Frames)
                {
                    frame?.Dispose();
                }
            }
        }
    }
}