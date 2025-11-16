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
using System.IO;

namespace Game
{
    public partial class Form1 : Form
    {
        // --- Biến của Player ---
        private List<Image> IdleImages = new List<Image>();
        private List<Image> WalkImagesRight = new List<Image>();
        private List<Image> WalkImagesLeft = new List<Image>();
        private List<Image> JumpImagesRight = new List<Image>();
        private List<Image> JumpImagesLeft = new List<Image>();

        private int IdleIndexMaxImages = 6;
        private int WalkIndexMaxImages = 8;
        private int JumpIndexMaxImages = 10;
        private int CurrentImageIndex = 0;

        private enum CharacterState { Idle, WalkRight, WalkLeft, JumpRight, JumpLeft }
        private CharacterState currentState = CharacterState.Idle;

        private int posX = 100;
        private int posY = 150;
        private int moveSpeed = 10;

        private bool isRightPressed = false;
        private bool isLeftPressed = false;
        private bool isJumping = false;
        private bool lastFacingRight = true;

        private int jumpSpeed = 15;
        private int gravity = 2;    // Trọng lực
        private int velocityY = 0;

        // <<< QUAN TRỌNG: ĐỘ CAO MẶT ĐẤT (Số càng lớn càng thấp) >>>
        private int groundY = 270;

        private Image backgroundImage;
        private SpellEffect currentEffect = null;

        public Form1()
        {
            InitializeComponent();

            this.DoubleBuffered = true;
            this.KeyPreview = true;

            // Kích hoạt Timer và Sự kiện bàn phím
            this.animationTimer.Tick += animationTimer_Tick;
            this.KeyUp += Form1_KeyUp;

            this.animationTimer.Interval = 40;
            this.animationTimer.Start();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadIdleImages();
            LoadJumpImages();
            LoadBackground();
            SpellEffect.LoadContent();
        }

        // ... (CÁC HÀM LOAD ẢNH GIỮ NGUYÊN) ...
        private void LoadIdleImages()
        {
            try
            {
                IdleImages.Add(Resources.Standing1); IdleImages.Add(Resources.Standing2); IdleImages.Add(Resources.Standing3);
                IdleImages.Add(Resources.Standing4); IdleImages.Add(Resources.Standing5); IdleImages.Add(Resources.Standing6);

                WalkImagesRight.Add(Resources.idle1Right); WalkImagesRight.Add(Resources.idle2Right); WalkImagesRight.Add(Resources.idle3Right);
                WalkImagesRight.Add(Resources.idle4Right); WalkImagesRight.Add(Resources.idle5Right); WalkImagesRight.Add(Resources.idle6Right);
                WalkImagesRight.Add(Resources.idle7Right); WalkImagesRight.Add(Resources.idle8Right);

                WalkImagesLeft.Add(Resources.idle1); WalkImagesLeft.Add(Resources.idle2); WalkImagesLeft.Add(Resources.idle3);
                WalkImagesLeft.Add(Resources.idle4); WalkImagesLeft.Add(Resources.idle5); WalkImagesLeft.Add(Resources.idle6);
                WalkImagesLeft.Add(Resources.idle7); WalkImagesLeft.Add(Resources.idle8);
            }
            catch (Exception ex) { MessageBox.Show("Lỗi load ảnh Idle: " + ex.Message); }
        }

        private void LoadJumpImages()
        {
            try
            {
                Image jumpSpriteSheetRight = Resources.jumpRight;
                Image jumpSpriteSheetLeft = Resources.jumpLeft;
                int frameCount = JumpIndexMaxImages;
                int frameWidth = jumpSpriteSheetRight.Width / frameCount;
                int frameHeight = jumpSpriteSheetRight.Height;

                for (int i = 0; i < frameCount; i++)
                {
                    Bitmap frame = new Bitmap(frameWidth, frameHeight);
                    using (Graphics g = Graphics.FromImage(frame))
                    {
                        g.DrawImage(jumpSpriteSheetRight, new Rectangle(0, 0, frameWidth, frameHeight), new Rectangle(i * frameWidth, 0, frameWidth, frameHeight), GraphicsUnit.Pixel);
                    }
                    JumpImagesRight.Add(frame);
                }
                for (int i = 0; i < frameCount; i++)
                {
                    Bitmap frame = new Bitmap(frameWidth, frameHeight);
                    using (Graphics g = Graphics.FromImage(frame))
                    {
                        g.DrawImage(jumpSpriteSheetLeft, new Rectangle(0, 0, frameWidth, frameHeight), new Rectangle(i * frameWidth, 0, frameWidth, frameHeight), GraphicsUnit.Pixel);
                    }
                    JumpImagesLeft.Add(frame);
                }
            }
            catch (Exception ex) { MessageBox.Show("Lỗi load ảnh Jump: " + ex.Message); }
        }

        private void LoadBackground()
        {
            try { backgroundImage = Resources.background; } catch (Exception ex) { }
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            if (backgroundImage != null)
                e.Graphics.DrawImage(backgroundImage, 0, 0, this.ClientSize.Width, this.ClientSize.Height);

            int safeIndex = CurrentImageIndex;
            Image playerImage = null;

            switch (currentState)
            {
                case CharacterState.Idle:
                    if (safeIndex < IdleImages.Count) playerImage = IdleImages[safeIndex];
                    break;
                case CharacterState.WalkRight:
                    if (safeIndex < WalkImagesRight.Count) playerImage = WalkImagesRight[safeIndex];
                    break;
                case CharacterState.WalkLeft:
                    if (safeIndex < WalkImagesLeft.Count) playerImage = WalkImagesLeft[safeIndex];
                    break;
                case CharacterState.JumpRight:
                    if (safeIndex < JumpImagesRight.Count) playerImage = JumpImagesRight[safeIndex];
                    break;
                case CharacterState.JumpLeft:
                    if (safeIndex < JumpImagesLeft.Count) playerImage = JumpImagesLeft[safeIndex];
                    break;
            }

            if (playerImage != null)
                e.Graphics.DrawImage(playerImage, new Point(posX, posY));

            if (currentEffect != null && currentEffect.IsActive)
                currentEffect.Draw(e.Graphics);
        }

        // <<< SỬA: LOGIC GAME LOOP CÓ TRỌNG LỰC >>>
        private void animationTimer_Tick(object sender, EventArgs e)
        {
            // 1. XỬ LÝ TRỌNG LỰC (TỰ RƠI)
            // Nếu chưa chạm đất (330) và không đang nhảy thì tự rơi xuống
            if (!isJumping && posY < groundY)
            {
                posY += 10; // Rơi tự do
                if (posY > groundY) posY = groundY; // Chạm đất
            }

            // 2. XỬ LÝ NHẢY
            if (isJumping)
            {
                if (isRightPressed) { currentState = CharacterState.JumpRight; lastFacingRight = true; }
                else if (isLeftPressed) { currentState = CharacterState.JumpLeft; lastFacingRight = false; }
                else { currentState = lastFacingRight ? CharacterState.JumpRight : CharacterState.JumpLeft; }

                // Công thức nhảy
                posY += velocityY;
                velocityY += gravity;

                // Kiểm tra chạm đất
                if (posY >= groundY)
                {
                    posY = groundY;
                    velocityY = 0;
                    isJumping = false; // Đáp đất thành công

                    if (isRightPressed) currentState = CharacterState.WalkRight;
                    else if (isLeftPressed) currentState = CharacterState.WalkLeft;
                    else currentState = CharacterState.Idle;

                    CurrentImageIndex = 0;
                }
            }
            else
            {
                // 3. XỬ LÝ ĐI BỘ (KHI KHÔNG NHẢY)
                if (isRightPressed)
                {
                    posX += moveSpeed;
                    currentState = CharacterState.WalkRight;
                    lastFacingRight = true;
                }
                else if (isLeftPressed)
                {
                    posX -= moveSpeed;
                    currentState = CharacterState.WalkLeft;
                    lastFacingRight = false;
                }
                else
                {
                    currentState = CharacterState.Idle;
                }
            }

            // 4. CẬP NHẬT ANIMATION
            int maxFrames = IdleIndexMaxImages;
            if (currentState == CharacterState.WalkRight || currentState == CharacterState.WalkLeft) maxFrames = WalkIndexMaxImages;
            if (currentState == CharacterState.JumpRight || currentState == CharacterState.JumpLeft) maxFrames = JumpIndexMaxImages;

            CurrentImageIndex++;
            if (CurrentImageIndex >= maxFrames)
            {
                if (isJumping) CurrentImageIndex = maxFrames - 1;
                else CurrentImageIndex = 0;
            }

            // 5. CẬP NHẬT CHIÊU THỨC
            if (currentEffect != null && currentEffect.IsActive)
            {
                currentEffect.Update();
                if (!currentEffect.IsActive) currentEffect = null;
            }

            // Giới hạn màn hình
            if (posX < 0) posX = 0;
            if (posX > this.ClientSize.Width - 50) posX = this.ClientSize.Width - 50;

            this.Invalidate();
        }

        private void FireAttack()
        {
            if (currentEffect == null || !currentEffect.IsActive)
            {
                int pW = 50; int pH = 50;
                if (IdleImages.Count > 0) { pW = IdleImages[0].Width; pH = IdleImages[0].Height; }
                Point spawnPos = new Point(posX + (pW / 2), posY + (pH / 2));
                currentEffect = new SpellEffect(spawnPos, this.lastFacingRight);
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Right || e.KeyCode == Keys.D) { isRightPressed = true; if (!isJumping) lastFacingRight = true; }
            else if (e.KeyCode == Keys.Left || e.KeyCode == Keys.A) { isLeftPressed = true; if (!isJumping) lastFacingRight = false; }
            else if (e.KeyCode == Keys.Space || e.KeyCode == Keys.W || e.KeyCode == Keys.Up)
            {
                if (!isJumping) // Chỉ nhảy được khi đang ở dưới đất (hoặc gần đất)
                {
                    isJumping = true;
                    velocityY = -22;
                    CurrentImageIndex = 0;
                }
            }
            else if (e.KeyCode == Keys.E) FireAttack();
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Right || e.KeyCode == Keys.D) isRightPressed = false;
            else if (e.KeyCode == Keys.Left || e.KeyCode == Keys.A) isLeftPressed = false;

            if (!isJumping && !isRightPressed && !isLeftPressed) CurrentImageIndex = 0;
        }
    }

    // CLASS SPELLEFFECT (GIỮ NGUYÊN)
    public class SpellEffect
    {
        private static List<Image> frames = new List<Image>();
        private static bool isLoaded = false;
        private static float scale = 0.5f;
        private Point position;
        private int currentFrameIndex;
        private bool isActive;
        private bool facingRight;
        private int speedX;
        private const int MOVEMENT_SPEED = 20;

        public bool IsActive { get { return isActive; } }

        public static void LoadContent()
        {
            if (isLoaded) return;
            try
            {
                frames.Add(Resources.Fire_Arrow_Frame_01); frames.Add(Resources.Fire_Arrow_Frame_02);
                frames.Add(Resources.Fire_Arrow_Frame_03); frames.Add(Resources.Fire_Arrow_Frame_04);
                frames.Add(Resources.Fire_Arrow_Frame_05); frames.Add(Resources.Fire_Arrow_Frame_06);
                frames.Add(Resources.Fire_Arrow_Frame_07); frames.Add(Resources.Fire_Arrow_Frame_08);
                isLoaded = true;
            }
            catch (Exception ex) { MessageBox.Show("Lỗi load ảnh lửa: " + ex.Message); }
        }

        public SpellEffect(Point startPosition, bool facingRight)
        {
            if (!isLoaded || frames.Count == 0) { this.isActive = false; return; }
            this.position = startPosition;
            this.currentFrameIndex = 0;
            this.isActive = true;
            this.facingRight = facingRight;
            this.speedX = this.facingRight ? MOVEMENT_SPEED : -MOVEMENT_SPEED;
        }

        public void Update()
        {
            if (!isActive) return;
            this.position.X += this.speedX;
            currentFrameIndex++;
            if (currentFrameIndex >= frames.Count) isActive = false;
        }

        public void Draw(Graphics g)
        {
            if (!isActive) return;
            Image currentFrame = frames[currentFrameIndex];
            int newWidth = (int)(currentFrame.Width * scale);
            int newHeight = (int)(currentFrame.Height * scale);
            int drawX = position.X - newWidth / 2;
            int drawY = position.Y - newHeight / 2;
            Rectangle destRect = new Rectangle(drawX, drawY, newWidth, newHeight);
            using (Image frameToDraw = (Image)currentFrame.Clone())
            {
                if (this.facingRight) frameToDraw.RotateFlip(RotateFlipType.RotateNoneFlipX);
                g.DrawImage(frameToDraw, destRect);
            }
        }
    }
}