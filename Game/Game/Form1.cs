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
        // <<< MỚI: Danh sách ảnh cho hành động Tấn công >>>
        private List<Image> AttackImages = new List<Image>();

        private int IdleIndexMaxImages = 6;
        private int WalkIndexMaxImages = 8;
        private int JumpIndexMaxImages = 10;
        // <<< MỚI: Số lượng frame cho Attack >>>
        private int AttackIndexMaxImages = 4; // Vì có 4 ảnh trong sprite chưởng
        private int CurrentPlayerImageIndex = 0;

        // <<< MỚI: Thêm trạng thái Attack vào enum CharacterState >>>
        private enum CharacterState { Idle, WalkRight, WalkLeft, JumpRight, JumpLeft, Attack }
        private CharacterState currentCharacterState = CharacterState.Idle;
        // <<< MỚI: Lưu trạng thái trước đó để quay lại sau khi Attack xong >>>
        private CharacterState previousCharacterState = CharacterState.Idle;

        private int playerPosX = 100;
        private int playerPosY = 270;
        private int playerMoveSpeed = 6;
        private float playerScale = 1f;

        private bool isRightPressed = false;
        private bool isLeftPressed = false;
        private bool isJumping = false;
        private bool lastFacingRight = true;
        // <<< MỚI: Biến kiểm soát Attack >>>
        private bool isAttacking = false;

        private int jumpSpeed = 18;
        private int gravity = 2;
        private int velocityY = 0;

        private int groundY = 270;

        private Image backgroundImage;
        private SpellEffect currentEffect = null;

        // --- Biến của Slime ---
        private List<Image> SlimeWalkImages = new List<Image>();
        private List<Image> SlimeDeathImages = new List<Image>();
        // <<< SỬA: CHỈ CÓ 7 ẢNH SLIME THÔI >>>
        private int SlimeWalkIndexMaxImages = 7;
        private int SlimeDeathIndexMaxImages = 12;
        private float slimeScale = 1f;

        private Random random = new Random();
        private int maxSlimes = 3;

        private class Slime
        {
            public int PosX;
            public int PosY;
            public int Speed;
            public bool FacingRight;
            public SlimeState State;
            public int AnimationIndex;

            public enum SlimeState { Walk, Death }

            public Slime(int x, int y, int speed, bool facingRight)
            {
                PosX = x;
                PosY = y;
                Speed = speed;
                FacingRight = facingRight;
                State = SlimeState.Walk;
                AnimationIndex = 0;
            }

            public Rectangle GetBounds(int width, int height, float scale)
            {
                int w = (int)(width * scale);
                int h = (int)(height * scale);
                return new Rectangle(PosX + 10, PosY + 10, w - 20, h - 20);
            }
        }
        private List<Slime> slimes = new List<Slime>();


        public Form1()
        {
            InitializeComponent();

            this.DoubleBuffered = true;
            this.KeyPreview = true;

            this.animationTimer.Tick += animationTimer_Tick;
            this.KeyUp += Form1_KeyUp;

            this.animationTimer.Interval = 50;
            this.animationTimer.Start();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadIdleImages();
            LoadWalkImages();
            LoadJumpImages();
            // <<< MỚI: Load ảnh Attack >>>
            LoadAttackImages();
            LoadBackground();
            LoadSlimeImages();
            SpellEffect.LoadContent(0.4f);

            SpawnSlimes();
        }

        private void LoadIdleImages() { try { IdleImages.Add(Resources.Standing1); IdleImages.Add(Resources.Standing2); IdleImages.Add(Resources.Standing3); IdleImages.Add(Resources.Standing4); IdleImages.Add(Resources.Standing5); IdleImages.Add(Resources.Standing6); } catch (Exception ex) { MessageBox.Show("Lỗi load ảnh Idle: " + ex.Message); } }
        private void LoadWalkImages() { try { WalkImagesRight.Add(Resources.idle1Right); WalkImagesRight.Add(Resources.idle2Right); WalkImagesRight.Add(Resources.idle3Right); WalkImagesRight.Add(Resources.idle4Right); WalkImagesRight.Add(Resources.idle5Right); WalkImagesRight.Add(Resources.idle6Right); WalkImagesRight.Add(Resources.idle7Right); WalkImagesRight.Add(Resources.idle8Right); WalkImagesLeft.Add(Resources.idle1); WalkImagesLeft.Add(Resources.idle2); WalkImagesLeft.Add(Resources.idle3); WalkImagesLeft.Add(Resources.idle4); WalkImagesLeft.Add(Resources.idle5); WalkImagesLeft.Add(Resources.idle6); WalkImagesLeft.Add(Resources.idle7); WalkImagesLeft.Add(Resources.idle8); } catch (Exception ex) { MessageBox.Show("Lỗi load ảnh Walk: " + ex.Message); } }
        private void LoadJumpImages() { try { Image jumpSpriteSheetRight = Resources.jumpRight; Image jumpSpriteSheetLeft = Resources.jumpLeft; int frameCount = JumpIndexMaxImages; int frameWidth = jumpSpriteSheetRight.Width / frameCount; int frameHeight = jumpSpriteSheetRight.Height; for (int i = 0; i < frameCount; i++) { Bitmap frame = new Bitmap(frameWidth, frameHeight); using (Graphics g = Graphics.FromImage(frame)) { g.DrawImage(jumpSpriteSheetRight, new Rectangle(0, 0, frameWidth, frameHeight), new Rectangle(i * frameWidth, 0, frameWidth, frameHeight), GraphicsUnit.Pixel); } JumpImagesRight.Add(frame); } for (int i = 0; i < frameCount; i++) { Bitmap frame = new Bitmap(frameWidth, frameHeight); using (Graphics g = Graphics.FromImage(frame)) { g.DrawImage(jumpSpriteSheetLeft, new Rectangle(0, 0, frameWidth, frameHeight), new Rectangle(i * frameWidth, 0, frameWidth, frameHeight), GraphicsUnit.Pixel); } JumpImagesLeft.Add(frame); } } catch (Exception ex) { MessageBox.Show("Lỗi load ảnh Jump: " + ex.Message); } }
        private void LoadBackground() { try { backgroundImage = Resources.background; } catch (Exception ex) { } }

        // <<< MỚI: HÀM LOAD ẢNH CHO ATTACK >>>
        private void LoadAttackImages()
        {
            try
            {
               
                Image attackSpriteSheet = Resources.Attack_1;
                int frameCount = AttackIndexMaxImages;
                int frameWidth = attackSpriteSheet.Width / frameCount;
                int frameHeight = attackSpriteSheet.Height;
                for (int i = 0; i < frameCount; i++) {
                    Bitmap frame = new Bitmap(frameWidth, frameHeight);
                     using (Graphics g = Graphics.FromImage(frame)) {
                         g.DrawImage(attackSpriteSheet, new Rectangle(0, 0, frameWidth, frameHeight), new Rectangle(i * frameWidth, 0, frameWidth, frameHeight), GraphicsUnit.Pixel);
                     }
                     AttackImages.Add(frame);
                }

            }
            catch (Exception ex) { MessageBox.Show("Lỗi load ảnh Attack: " + ex.Message); }
        }

        private void LoadSlimeImages()
        {
            try
            {
                SlimeWalkImages.Add(Resources.slime01); SlimeWalkImages.Add(Resources.slime02); SlimeWalkImages.Add(Resources.slime03);
                SlimeWalkImages.Add(Resources.slime04); SlimeWalkImages.Add(Resources.slime05); SlimeWalkImages.Add(Resources.slime06);
                SlimeWalkImages.Add(Resources.slime07);
                // <<< SỬA: KHÔNG LOAD SLIME 08 NỮA, CHỈ CÓ 7 SLIME THÔI >>>
                // SlimeWalkImages.Add(Resources.slime08); 

                SlimeDeathImages.AddRange(SlimeWalkImages);
            }
            catch (Exception ex) { MessageBox.Show("Lỗi load ảnh Slime: " + ex.Message); }
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            if (backgroundImage != null)
                e.Graphics.DrawImage(backgroundImage, 0, 0, this.ClientSize.Width, this.ClientSize.Height);

            int safePlayerIndex = CurrentPlayerImageIndex;
            Image playerImage = null;

            switch (currentCharacterState)
            {
                case CharacterState.Idle: if (safePlayerIndex < IdleImages.Count) playerImage = IdleImages[safePlayerIndex]; break;
                case CharacterState.WalkRight: if (safePlayerIndex < WalkImagesRight.Count) playerImage = WalkImagesRight[safePlayerIndex]; break;
                case CharacterState.WalkLeft: if (safePlayerIndex < WalkImagesLeft.Count) playerImage = WalkImagesLeft[safePlayerIndex]; break;
                case CharacterState.JumpRight: if (safePlayerIndex < JumpImagesRight.Count) playerImage = JumpImagesRight[safePlayerIndex]; break;
                case CharacterState.JumpLeft: if (safePlayerIndex < JumpImagesLeft.Count) playerImage = JumpImagesLeft[safePlayerIndex]; break;
                // <<< MỚI: Case Attack >>>
                case CharacterState.Attack: if (safePlayerIndex < AttackImages.Count) playerImage = AttackImages[safePlayerIndex]; break;
            }

            if (playerImage != null)
            {
                int drawWidth = (int)(playerImage.Width * playerScale);
                int drawHeight = (int)(playerImage.Height * playerScale);

                // <<< MỚI: Lật ảnh chưởng nếu đang quay trái >>>
                if (!lastFacingRight && (currentCharacterState == CharacterState.WalkLeft || currentCharacterState == CharacterState.Attack || currentCharacterState == CharacterState.JumpLeft || currentCharacterState == CharacterState.Idle))
                {
                    using (Image flippedImage = (Image)playerImage.Clone())
                    {
                        flippedImage.RotateFlip(RotateFlipType.RotateNoneFlipX);
                        e.Graphics.DrawImage(flippedImage, playerPosX, playerPosY, drawWidth, drawHeight);
                    }
                }
                else
                {
                    e.Graphics.DrawImage(playerImage, playerPosX, playerPosY, drawWidth, drawHeight);
                }
            }

            DrawSlimes(e.Graphics);

            if (currentEffect != null && currentEffect.IsActive)
                currentEffect.Draw(e.Graphics);
        }

        private void animationTimer_Tick(object sender, EventArgs e)
        {
            // --- Cập nhật PLAYER ---
            if (isAttacking) // Nếu đang tấn công, chỉ cập nhật animation attack
            {
                CurrentPlayerImageIndex++;
                if (CurrentPlayerImageIndex >= AttackIndexMaxImages)
                {
                    CurrentPlayerImageIndex = 0; // Reset animation
                    isAttacking = false; // Kết thúc tấn công
                    currentCharacterState = previousCharacterState; // Quay về trạng thái trước đó
                }
            }
            else // Nếu không tấn công, xử lý di chuyển và nhảy bình thường
            {
                // 1. XỬ LÝ TRỌNG LỰC (TỰ RƠI) CỦA PLAYER
                if (!isJumping && playerPosY < groundY) { playerPosY += 5; if (playerPosY > groundY) playerPosY = groundY; }

                // 2. XỬ LÝ NHẢY CỦA PLAYER
                if (isJumping)
                {
                    if (isRightPressed) { currentCharacterState = CharacterState.JumpRight; lastFacingRight = true; }
                    else if (isLeftPressed) { currentCharacterState = CharacterState.JumpLeft; lastFacingRight = false; }
                    else { currentCharacterState = lastFacingRight ? CharacterState.JumpRight : CharacterState.JumpLeft; }

                    playerPosY += velocityY;
                    velocityY += gravity;

                    if (playerPosY >= groundY)
                    {
                        playerPosY = groundY;
                        velocityY = 0;
                        isJumping = false;

                        if (isRightPressed) currentCharacterState = CharacterState.WalkRight;
                        else if (isLeftPressed) currentCharacterState = CharacterState.WalkLeft;
                        else currentCharacterState = CharacterState.Idle;

                        CurrentPlayerImageIndex = 0;
                    }
                }
                else
                {
                    // 3. XỬ LÝ ĐI BỘ CỦA PLAYER
                    if (isRightPressed)
                    {
                        playerPosX += playerMoveSpeed;
                        currentCharacterState = CharacterState.WalkRight;
                        lastFacingRight = true;
                    }
                    else if (isLeftPressed)
                    {
                        playerPosX -= playerMoveSpeed;
                        currentCharacterState = CharacterState.WalkLeft;
                        lastFacingRight = false;
                    }
                    else
                    {
                        currentCharacterState = CharacterState.Idle;
                    }
                }

                // 4. CẬP NHẬT ANIMATION CỦA PLAYER (cho Idle/Walk/Jump)
                int maxPlayerFrames = IdleIndexMaxImages;
                if (currentCharacterState == CharacterState.WalkRight || currentCharacterState == CharacterState.WalkLeft) maxPlayerFrames = WalkIndexMaxImages;
                if (currentCharacterState == CharacterState.JumpRight || currentCharacterState == CharacterState.JumpLeft) maxPlayerFrames = JumpIndexMaxImages;

                CurrentPlayerImageIndex++;
                if (CurrentPlayerImageIndex >= maxPlayerFrames)
                {
                    if (isJumping) CurrentPlayerImageIndex = maxPlayerFrames - 1;
                    else CurrentPlayerImageIndex = 0;
                }
            }

            // --- Cập nhật Chiêu thức ---
            if (currentEffect != null && currentEffect.IsActive)
            {
                currentEffect.Update();
                if (!currentEffect.IsActive) currentEffect = null;
            }

            // --- Cập nhật Slime và Respawn ---
            UpdateSlimes();

            if (slimes.Count < maxSlimes)
            {
                if (random.Next(0, 100) < 5)
                {
                    SpawnOneSlime();
                }
            }

            // --- Kiểm tra Va chạm ---
            CheckCollisions();

            // Giới hạn màn hình của player
            if (playerPosX < 0) playerPosX = 0;
            if (playerPosX > this.ClientSize.Width - (int)(IdleImages[0].Width * playerScale)) playerPosX = this.ClientSize.Width - (int)(IdleImages[0].Width * playerScale);

            this.Invalidate();
        }

        private void CheckCollisions()
        {
            if (currentEffect == null || !currentEffect.IsActive) return;

            Rectangle fireRect = currentEffect.GetBounds();

            foreach (var slime in slimes)
            {
                if (slime.State == Slime.SlimeState.Walk)
                {
                    if (SlimeWalkImages.Count > 0)
                    {
                        Rectangle slimeRect = slime.GetBounds(SlimeWalkImages[0].Width, SlimeWalkImages[0].Height, slimeScale);

                        if (fireRect.IntersectsWith(slimeRect))
                        {
                            slime.State = Slime.SlimeState.Death;
                            slime.AnimationIndex = 0;
                            currentEffect.IsActive = false;
                        }
                    }
                }
            }
        }

        private void SpawnOneSlime()
        {
            int side = random.Next(0, 2);
            int spawnX = 0;
            bool facingRight = true;

            if (side == 0)
            {
                spawnX = -50;
                facingRight = true;
            }
            else
            {
                spawnX = this.ClientSize.Width + 50;
                facingRight = false;
            }

            int speed = random.Next(1, 3);

            slimes.Add(new Slime(spawnX, groundY, speed, facingRight));
        }

        private void SpawnSlimes()
        {
            slimes.Add(new Slime(500, groundY, 1, false));
            slimes.Add(new Slime(800, groundY, 2, true));
        }

        private void FireAttack()
        {
            // Chỉ cho phép tấn công nếu không đang tấn công và không đang nhảy
            if (!isAttacking && !isJumping)
            {
                // Bắt đầu animation tấn công
                isAttacking = true;
                previousCharacterState = currentCharacterState; // Lưu trạng thái hiện tại
                currentCharacterState = CharacterState.Attack;
                CurrentPlayerImageIndex = 0; // Bắt đầu animation chưởng từ frame đầu

                // Tạo hiệu ứng lửa như bình thường
                if (currentEffect == null || !currentEffect.IsActive)
                {
                    int pW = 50; int pH = 50;
                    if (IdleImages.Count > 0) { pW = (int)(IdleImages[0].Width * playerScale); pH = (int)(IdleImages[0].Height * playerScale); }
                    Point spawnPos = new Point(playerPosX + (pW / 2), playerPosY + (pH / 2) + 15);
                    currentEffect = new SpellEffect(spawnPos, this.lastFacingRight);
                }
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            // Không cho di chuyển khi đang tấn công
            if (isAttacking) return;

            if (e.KeyCode == Keys.Right || e.KeyCode == Keys.D) { isRightPressed = true; if (!isJumping) lastFacingRight = true; }
            else if (e.KeyCode == Keys.Left || e.KeyCode == Keys.A) { isLeftPressed = true; if (!isJumping) lastFacingRight = false; }
            else if (e.KeyCode == Keys.Space || e.KeyCode == Keys.W || e.KeyCode == Keys.Up)
            {
                if (!isJumping)
                {
                    isJumping = true;
                    velocityY = -22;
                    CurrentPlayerImageIndex = 0;
                }
            }
            else if (e.KeyCode == Keys.E) FireAttack();
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Right || e.KeyCode == Keys.D) isRightPressed = false;
            else if (e.KeyCode == Keys.Left || e.KeyCode == Keys.A) isLeftPressed = false;

            // Chỉ reset animation về Idle nếu không đang nhảy và không đang tấn công
            if (!isJumping && !isRightPressed && !isLeftPressed && !isAttacking) CurrentPlayerImageIndex = 0;
        }

        private void UpdateSlimes()
        {
            for (int i = slimes.Count - 1; i >= 0; i--)
            {
                Slime slime = slimes[i];
                if (slime.State == Slime.SlimeState.Walk)
                {
                    if (slime.FacingRight)
                    {
                        slime.PosX += slime.Speed;
                        if (slime.PosX > this.ClientSize.Width + 100) slimes.RemoveAt(i);
                    }
                    else
                    {
                        slime.PosX -= slime.Speed;
                        if (slime.PosX < -100) slimes.RemoveAt(i);
                    }

                    if (i < slimes.Count)
                    {
                        slime.AnimationIndex++;
                        if (slime.AnimationIndex >= SlimeWalkIndexMaxImages) slime.AnimationIndex = 0;
                    }
                }
                else if (slime.State == Slime.SlimeState.Death)
                {
                    slime.AnimationIndex++;
                    if (slime.AnimationIndex >= SlimeDeathIndexMaxImages) slimes.RemoveAt(i);
                }
            }
        }

        private void DrawSlimes(Graphics g)
        {
            foreach (var slime in slimes)
            {
                Image slimeImage = null;
                int safeSlimeIndex = slime.AnimationIndex;

                if (slime.State == Slime.SlimeState.Walk && safeSlimeIndex < SlimeWalkImages.Count)
                    slimeImage = SlimeWalkImages[safeSlimeIndex];
                else if (slime.State == Slime.SlimeState.Death && safeSlimeIndex < SlimeDeathImages.Count)
                    slimeImage = SlimeDeathImages[safeSlimeIndex];

                if (slimeImage != null)
                {
                    int drawWidth = (int)(slimeImage.Width * slimeScale);
                    int drawHeight = (int)(slimeImage.Height * slimeScale);

                    if (!slime.FacingRight)
                    {
                        using (Image flippedImage = (Image)slimeImage.Clone())
                        {
                            flippedImage.RotateFlip(RotateFlipType.RotateNoneFlipX);
                            g.DrawImage(flippedImage, slime.PosX, slime.PosY, drawWidth, drawHeight);
                        }
                    }
                    else
                    {
                        g.DrawImage(slimeImage, slime.PosX, slime.PosY, drawWidth, drawHeight);
                    }
                }
            }
        }
    }

    // CLASS SPELLEFFECT (Không thay đổi trong lần này)
    public class SpellEffect
    {
        private static List<Image> frames = new List<Image>();
        private static bool isLoaded = false;
        private static float currentScale = 1.0f;

        public Point Position { get; private set; }
        public bool IsActive { get; set; }

        private int currentFrameIndex;
        private bool facingRight;
        private int speedX;
        private const int MOVEMENT_SPEED = 20;

        public static void LoadContent(float scale = 0.4f)
        {
            if (isLoaded) return;
            try
            {
                frames.Add(Resources.Fire_Arrow_Frame_01); frames.Add(Resources.Fire_Arrow_Frame_02);
                frames.Add(Resources.Fire_Arrow_Frame_03); frames.Add(Resources.Fire_Arrow_Frame_04);
                frames.Add(Resources.Fire_Arrow_Frame_05); frames.Add(Resources.Fire_Arrow_Frame_06);
                frames.Add(Resources.Fire_Arrow_Frame_07); frames.Add(Resources.Fire_Arrow_Frame_08);
                isLoaded = true;
                currentScale = scale;
            }
            catch (Exception ex) { MessageBox.Show("Lỗi load ảnh lửa: " + ex.Message); }
        }

        public SpellEffect(Point startPosition, bool facingRight)
        {
            if (!isLoaded || frames.Count == 0) { this.IsActive = false; return; }
            this.Position = startPosition;
            this.currentFrameIndex = 0;
            this.IsActive = true;
            this.facingRight = facingRight;
            this.speedX = this.facingRight ? MOVEMENT_SPEED : -MOVEMENT_SPEED;
        }

        public void Update()
        {
            if (!IsActive) return;
            this.Position = new Point(this.Position.X + this.speedX, this.Position.Y);
            currentFrameIndex++;
            if (currentFrameIndex >= frames.Count) IsActive = false;
        }

        public void Draw(Graphics g)
        {
            if (!IsActive) return;
            Image currentFrame = frames[currentFrameIndex];
            int newWidth = (int)(currentFrame.Width * currentScale);
            int newHeight = (int)(currentFrame.Height * currentScale);
            int drawX = Position.X - newWidth / 2;
            int drawY = Position.Y - newHeight / 2;
            Rectangle destRect = new Rectangle(drawX, drawY, newWidth, newHeight);
            using (Image frameToDraw = (Image)currentFrame.Clone())
            {
                if (this.facingRight) frameToDraw.RotateFlip(RotateFlipType.RotateNoneFlipX);
                g.DrawImage(frameToDraw, destRect);
            }
        }

        public Rectangle GetBounds()
        {
            if (!isLoaded || frames.Count == 0) return Rectangle.Empty;
            Image currentFrame = frames[currentFrameIndex];
            int w = (int)(currentFrame.Width * currentScale);
            int h = (int)(currentFrame.Height * currentScale);
            return new Rectangle(Position.X - w / 2 + 10, Position.Y - h / 2 + 10, w - 20, h - 20);
        }
    }
}