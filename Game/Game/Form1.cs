using Game.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

// Thêm cái này để dùng Resources
using Game.Properties;

namespace Game
{
    /// <summary>
    /// Class chính của Form, chứa logic điều khiển game loop và input
    /// </summary>
    public partial class Form1 : Form
    {
        private List<Image> IdleImages = new List<Image>();
        private List<Image> WalkImagesRight = new List<Image>();
        private List<Image> WalkImagesLeft = new List<Image>();
        private List<Image> JumpImagesRight = new List<Image>(); // NHẢY PHẢI
        private List<Image> JumpImagesLeft = new List<Image>();  // NHẢY TRÁI

        private int IdleIndexMaxImages = 6;
        private int WalkIndexMaxImages = 6;
        private int JumpIndexMaxImages = 10;
        private int CurrentImageIndex = 0;

        private enum CharacterState { Idle, WalkRight, WalkLeft, JumpRight, JumpLeft } // THÊM 2 STATE
        private CharacterState currentState = CharacterState.Idle;

        private int posX = 100;
        private int posY = 200;
        private int moveSpeed = 10;

        private bool isRightPressed = false;
        private bool isLeftPressed = false;
        private bool isJumping = false;
        private bool lastFacingRight = true; // Nhớ hướng cuối cùng

        private int jumpSpeed = 12;
        private int gravity = 1;
        private int velocityY = 0;
        private int groundY = 200;

        // BACKGROUND
        private Image backgroundImage;

        public Form1()
        {
            InitializeComponent();
            LoadIdleImages();
            LoadJumpImages();
            LoadBackground();
            this.DoubleBuffered = true;
            this.KeyPreview = true;
            animationTimer.Start();
        }

        private void LoadIdleImages()
        {
            for (int i = 1; i <= IdleIndexMaxImages; i++)
                IdleImages.Add(Image.FromFile($@"Resources\CharacterStand\idle{i}.png"));

            for (int i = 1; i <= WalkIndexMaxImages; i++)
            {
                WalkImagesRight.Add(Image.FromFile($@"Resources\CharacterRunning\Right\idle{i}.png"));
                WalkImagesLeft.Add(Image.FromFile($@"Resources\CharacterRunning\Left\idle{i}.png"));
            }
        }

        // CẮT SPRITE SHEET NHẢY (CẢ TRÁI VÀ PHẢI)
        private void LoadJumpImages()
        {
            // Load sprite sheet nhảy phải
            Image jumpSpriteSheetRight = Image.FromFile(@"Resources\CharacterJump\Right\jump_spritesheet.png");
            // Load sprite sheet nhảy trái
            Image jumpSpriteSheetLeft = Image.FromFile(@"Resources\CharacterJump\Left\jump_spritesheett.png");

            int frameCount = JumpIndexMaxImages;
            int frameWidth = jumpSpriteSheetRight.Width / frameCount;
            int frameHeight = jumpSpriteSheetRight.Height;

            // CẮT NHẢY PHẢI
            for (int i = 0; i < frameCount; i++)
            {
                Bitmap frame = new Bitmap(frameWidth, frameHeight);
                using (Graphics g = Graphics.FromImage(frame))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

                    g.DrawImage(
                        jumpSpriteSheetRight,
                        new Rectangle(0, 0, frameWidth, frameHeight),
                        new Rectangle(i * frameWidth, 0, frameWidth, frameHeight),
                        GraphicsUnit.Pixel
                    );
                }
                JumpImagesRight.Add(frame);
            }

            // CẮT NHẢY TRÁI
            for (int i = 0; i < frameCount; i++)
            {
                Bitmap frame = new Bitmap(frameWidth, frameHeight);
                using (Graphics g = Graphics.FromImage(frame))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

                    g.DrawImage(
                        jumpSpriteSheetLeft,
                        new Rectangle(0, 0, frameWidth, frameHeight),
                        new Rectangle(i * frameWidth, 0, frameWidth, frameHeight),
                        GraphicsUnit.Pixel
                    );
                }
                JumpImagesLeft.Add(frame);
            }

            jumpSpriteSheetRight.Dispose();
            jumpSpriteSheetLeft.Dispose();
        }

        private void LoadBackground()
        {
            backgroundImage = Image.FromFile(@"Resources\Background\background.jpg");
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            // VẼ BACKGROUND
            if (backgroundImage != null)
            {
                e.Graphics.DrawImage(backgroundImage, 0, 0, this.ClientSize.Width, this.ClientSize.Height);
            }

            // VẼ NHÂN VẬT
            int safeIndex = CurrentImageIndex;

            switch (currentState)
            {
                case CharacterState.Idle:
                    if (safeIndex < IdleImages.Count)
                        e.Graphics.DrawImage(IdleImages[safeIndex], new Point(posX, posY));
                    break;
                case CharacterState.WalkRight:
                    if (safeIndex < WalkImagesRight.Count)
                        e.Graphics.DrawImage(WalkImagesRight[safeIndex], new Point(posX, posY));
                    break;
                case CharacterState.WalkLeft:
                    if (safeIndex < WalkImagesLeft.Count)
                        e.Graphics.DrawImage(WalkImagesLeft[safeIndex], new Point(posX, posY));
                    break;
                case CharacterState.JumpRight:
                    if (safeIndex < JumpImagesRight.Count)
                        e.Graphics.DrawImage(JumpImagesRight[safeIndex], new Point(posX, posY));
                    break;
                case CharacterState.JumpLeft:
                    if (safeIndex < JumpImagesLeft.Count)
                        e.Graphics.DrawImage(JumpImagesLeft[safeIndex], new Point(posX, posY));
                    break;
            }
        }

        private void animationTimer_Tick(object sender, EventArgs e)
        {
            // XỬ LÝ NHẢY
            if (isJumping)
            {
                // XÁC ĐỊNH HƯỚNG NHẢY
                if (isRightPressed)
                {
                    currentState = CharacterState.JumpRight;
                    lastFacingRight = true;
                }
                else if (isLeftPressed)
                {
                    currentState = CharacterState.JumpLeft;
                    lastFacingRight = false;
                }
                else
                {
                    // Nhảy tại chỗ - giữ hướng cuối cùng
                    currentState = lastFacingRight ? CharacterState.JumpRight : CharacterState.JumpLeft;
                }

                // Vật lý nhảy
                posY += velocityY;
                velocityY += gravity;

                // Chạm đất
                if (posY >= groundY)
                {
                    posY = groundY;
                    velocityY = 0;
                    isJumping = false;
                    CurrentImageIndex = 0;
                }
            }
            else
            {
                // XỬ LÝ ĐI BỘ KHI KHÔNG NHẢY
                if (isRightPressed)
                {
                    currentState = CharacterState.WalkRight;
                    lastFacingRight = true;
                }
                else if (isLeftPressed)
                {
                    currentState = CharacterState.WalkLeft;
                    lastFacingRight = false;
                }
                else
                {
                    currentState = CharacterState.Idle;
                }
            }

            // XÁC ĐỊNH SỐ FRAME
            int maxFrames = IdleIndexMaxImages;
            switch (currentState)
            {
                case CharacterState.Idle:
                    maxFrames = IdleIndexMaxImages;
                    break;
                case CharacterState.WalkRight:
                case CharacterState.WalkLeft:
                    maxFrames = WalkIndexMaxImages;
                    break;
                case CharacterState.JumpRight:
                case CharacterState.JumpLeft:
                    maxFrames = JumpIndexMaxImages;
                    break;
            }

            // TĂNG FRAME ANIMATION
            CurrentImageIndex++;
            if (CurrentImageIndex >= maxFrames)
            {
                // Nếu đang nhảy, giữ frame cuối
                if (isJumping)
                    CurrentImageIndex = maxFrames - 1;
                else
                    CurrentImageIndex = 0;
            }

            // DI CHUYỂN TRÁI PHẢI
            if (isRightPressed)
            {
                posX += moveSpeed;
                if (posX > this.ClientSize.Width - 100)
                    posX = this.ClientSize.Width - 100;
            }
            else if (isLeftPressed)
            {
                posX -= moveSpeed;
                if (posX < 0)
                    posX = 0;
            }

            this.Invalidate();
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Right )
            {
                if (!isRightPressed)
                {
                    isRightPressed = true;
                    if (!isJumping) CurrentImageIndex = 0;
                }
            }
            else if (e.KeyCode == Keys.Left )
            {
                if (!isLeftPressed)
                {
                    isLeftPressed = true;
                    if (!isJumping) CurrentImageIndex = 0;
                }
            }
            else if (e.KeyCode == Keys.Space || e.KeyCode == Keys.W || e.KeyCode == Keys.Up)
            {
                // NHẢY
                if (!isJumping)
                {
                    isJumping = true;
                    velocityY = -jumpSpeed;
                    CurrentImageIndex = 0;
                }
            }
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Right || e.KeyCode == Keys.D)
            {
                isRightPressed = false;
            }
            else if (e.KeyCode == Keys.Left || e.KeyCode == Keys.A)
            {
                isLeftPressed = false;
            }

            if (!isRightPressed && !isLeftPressed && !isJumping)
            {
                CurrentImageIndex = 0;
            }
        }
    }
}