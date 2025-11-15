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

// Thêm cái này để dùng Resources
using Game.Properties;

namespace Game
{
    /// <summary>
    /// Class chính của Form, chứa logic điều khiển game loop và input
    /// </summary>
    public partial class Form1 : Form
    {
        // --- Biến của Player (từ code của sếp) ---
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

        private Image backgroundImage;

        // --- Biến của Attack Effect (gộp vào) ---
        // <<< MỚI: Thêm biến cho Attack Effect
        private SpellEffect currentEffect = null;

        public Form1()
        {
            InitializeComponent();

            // <<< MỚI: Dời 4 HÀM LOAD xuống Form1_Load cho đúng bài
            // LoadIdleImages();
            // LoadJumpImages();
            // LoadBackground();
            // SpellEffect.LoadContent();

            this.DoubleBuffered = true;
            this.KeyPreview = true;
            animationTimer.Start();
        }

        // <<< MỚI: TẠO LẠI HÀM FORM1_LOAD ĐỂ SỬA LỖI
        private void Form1_Load(object sender, EventArgs e)
        {
            // Dời code load của sếp vào đây
            LoadIdleImages();
            LoadJumpImages();
            LoadBackground();

            // Sếp phải chắc chắn 8 ảnh Fire_Arrow... có trong Resources nhé!
            SpellEffect.LoadContent();
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
            Image jumpSpriteSheetRight = Image.FromFile(@"Resources\CharacterJump\Right\jump_spritesheet.png");
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
            try
            {
                // Sếp PHẢI thêm ảnh background vào Resources.resx và đặt tên là "background"
                backgroundImage = Resources.background;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi load ảnh Background: " + ex.Message + "\nSếp đã thêm ảnh (background) vào Resources.resx chưa?");
            }
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            if (backgroundImage != null)
            {
                e.Graphics.DrawImage(backgroundImage, 0, 0, this.ClientSize.Width, this.ClientSize.Height);
            }

            int safeIndex = CurrentImageIndex;
            Image playerImage = null; // <<< MỚI: Lưu lại ảnh player để lấy width/height

            switch (currentState)
            {
                case CharacterState.Idle:
                    if (safeIndex < IdleImages.Count)
                        playerImage = IdleImages[safeIndex];
                    break;
                case CharacterState.WalkRight:
                    if (safeIndex < WalkImagesRight.Count)
                        playerImage = WalkImagesRight[safeIndex];
                    break;
                case CharacterState.WalkLeft:
                    if (safeIndex < WalkImagesLeft.Count)
                        playerImage = WalkImagesLeft[safeIndex];
                    break;
                case CharacterState.JumpRight:
                    if (safeIndex < JumpImagesRight.Count)
                        playerImage = JumpImagesRight[safeIndex];
                    break;
                case CharacterState.JumpLeft:
                    if (safeIndex < JumpImagesLeft.Count)
                        playerImage = JumpImagesLeft[safeIndex];
                    break;
            }

            // Thực hiện vẽ player
            if (playerImage != null)
            {
                e.Graphics.DrawImage(playerImage, new Point(posX, posY));
            }

            // <<< MỚI: Vẽ cục lửa (vẽ sau player để nó đè lên trên)
            if (currentEffect != null && currentEffect.IsActive)
            {
                currentEffect.Draw(e.Graphics);
            }
        }

        private void animationTimer_Tick(object sender, EventArgs e)
        {
            // XỬ LÝ NHẢY
            if (isJumping)
            {
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
                if (isJumping)
                    CurrentImageIndex = maxFrames - 1; // Giữ frame cuối nếu đang nhảy
                else
                    CurrentImageIndex = 0; // Quay về 0 nếu là idle/walk
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

            // <<< MỚI: Cập nhật cục lửa
            if (currentEffect != null && currentEffect.IsActive)
            {
                currentEffect.Update();
                // Nếu effect chạy xong, xóa nó đi (null)
                if (!currentEffect.IsActive)
                    currentEffect = null;
            }

            this.Invalidate();
        }

        // <<< MỚI: Thêm hàm Tấn Công (Attack)
        private void FireAttack()
        {
            // Chỉ tấn công nếu cục lửa cũ đã nổ xong
            if (currentEffect == null || !currentEffect.IsActive)
            {
                // Ước lượng kích thước player
                int playerWidth = 0;
                int playerHeight = 0;

                // Lấy kích thước từ ảnh idle đầu tiên (để biết tâm)
                if (IdleImages.Count > 0)
                {
                    playerWidth = IdleImages[0].Width;
                    playerHeight = IdleImages[0].Height;
                }

                // <<< MỚI: Tính vị trí TRUNG TÂM của player
                Point spawnPosition = new Point(posX + (playerWidth / 2), posY + (playerHeight / 2));

                // Tạo cục lửa tại tâm player, theo hướng player
                currentEffect = new SpellEffect(spawnPosition, this.lastFacingRight);
            }
        }


        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Right)
            {
                if (!isRightPressed)
                {
                    isRightPressed = true;
                    if (!isJumping) CurrentImageIndex = 0;
                }
            }
            else if (e.KeyCode == Keys.Left)
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
            // <<< MỚI: Thêm phím E để Tấn Công
            else if (e.KeyCode == Keys.E)
            {
                FireAttack();
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

            // Nếu thả hết phím (và không nhảy) thì reset animation
            if (!isRightPressed && !isLeftPressed && !isJumping)
            {
                CurrentImageIndex = 0;
            }
        }
    }


    //===================================================================
    // CLASS SPELLEFFECT (GỘP CHUNG FILE)
    //===================================================================
    public class SpellEffect
    {
        // --- Dữ liệu tĩnh (Static) ---
        private static List<Image> frames = new List<Image>();
        private static bool isLoaded = false;

        // CHỖ CHỈNH TỈ LỆ (0.5f = 50%)
        private static float scale = 0.5f;

        // --- Dữ liệu của đối tượng (Instance) ---
        private Point position;
        private int currentFrameIndex;
        private bool isActive;
        private bool facingRight;
        private int speedX;

        // CHỖ CHỈNH TỐC ĐỘ LƯỚT
        private const int MOVEMENT_SPEED = 15;

        public bool IsActive
        {
            get { return isActive; }
        }

        /// <summary>
        /// Load 8 frame ảnh từ Resources (chỉ chạy 1 lần)
        /// Sếp PHẢI THÊM 8 ảnh Fire_Arrow_Frame... vào Resources của Project
        /// </summary>
        public static void LoadContent()
        {
            if (isLoaded) return; // Chỉ load 1 lần

            try
            {
                // Lấy ảnh trực tiếp từ Resources
                frames.Add(Resources.Fire_Arrow_Frame_01);
                frames.Add(Resources.Fire_Arrow_Frame_02);
                frames.Add(Resources.Fire_Arrow_Frame_03);
                frames.Add(Resources.Fire_Arrow_Frame_04);
                frames.Add(Resources.Fire_Arrow_Frame_05);
                frames.Add(Resources.Fire_Arrow_Frame_06);
                frames.Add(Resources.Fire_Arrow_Frame_07);
                frames.Add(Resources.Fire_Arrow_Frame_08);

                isLoaded = true; // Đánh dấu là đã load thành công
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi load ảnh từ Resources: " + ex.Message + "\nSếp đã thêm 8 ảnh Fire_Arrow... vào Properties.Resources chưa?");
            }
        }

        /// <summary>
        /// Constructor: Tạo ra 1 cục lửa mới
        /// </summary>
        public SpellEffect(Point startPosition, bool facingRight)
        {
            // Báo lỗi nếu chưa load ảnh
            if (!isLoaded || frames.Count == 0)
            {
                this.isActive = false;
                return;
            }

            this.position = startPosition;
            this.currentFrameIndex = 0;
            this.isActive = true;
            this.facingRight = facingRight;

            // Set tốc độ dựa vào hướng
            if (this.facingRight)
            {
                this.speedX = MOVEMENT_SPEED;
            }
            else
            {
                this.speedX = -MOVEMENT_SPEED;
            }
        }

        /// <summary>
        /// Hàm Update: Được gọi mỗi tick của Timer, dùng để di chuyển và lật frame
        /// </summary>
        public void Update()
        {
            if (!isActive) return;

            // Cập nhật vị trí lướt
            this.position.X += this.speedX;

            // Chuyển frame tiếp theo
            currentFrameIndex++;

            // Nếu là frame cuối thì hủy
            if (currentFrameIndex >= frames.Count)
            {
                isActive = false;
            }
        }

        /// <summary>
        /// Hàm Draw: Vẽ cục lửa lên màn hình (có xử lý tỉ lệ và lật ảnh)
        /// </summary>
        public void Draw(Graphics g)
        {
            if (!isActive) return;

            Image currentFrame = frames[currentFrameIndex];

            // --- TÍNH TOÁN TỈ LỆ ---
            int newWidth = (int)(currentFrame.Width * scale);
            int newHeight = (int)(currentFrame.Height * scale);
            // Căn giữa cục lửa tại "position"
            int drawX = position.X - newWidth / 2;
            int drawY = position.Y - newHeight / 2;
            Rectangle destRect = new Rectangle(drawX, drawY, newWidth, newHeight);

            // --- XỬ LÝ QUAY HƯỚNG ---
            // Clone ảnh để tránh lật (Flip) cái ảnh gốc trong list
            using (Image frameToDraw = (Image)currentFrame.Clone())
            {
                // Nếu quay phải, lật ảnh theo chiều X
                if (this.facingRight)
                {
                    frameToDraw.RotateFlip(RotateFlipType.RotateNoneFlipX);
                }

                // Vẽ cái ảnh đã (hoặc chưa) lật
                g.DrawImage(frameToDraw, destRect);
            }
        }
    }
}