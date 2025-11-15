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
        // Vị trí "fake" của player (giữa màn hình)
        private Point playerPosition;

        // Biến để giữ đối tượng hiệu ứng đang chạy
        private SpellEffect currentEffect = null;

        // <<< MỚI: Thêm biến lưu hướng của Player
        // Mặc định là 'false' = quay trái (vì ảnh gốc của sếp quay trái)
        private bool isPlayerFacingRight = false;


        public Form1()
        {
            InitializeComponent();

            // Giảm giật/lag hình khi vẽ
            this.DoubleBuffered = true;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Set vị trí player là giữa màn hình
            playerPosition = new Point(this.ClientSize.Width / 2, this.ClientSize.Height / 2);

            // Tải tài nguyên (ảnh) cho class SpellEffect
            // Hàm này sẽ tự động load ảnh từ Properties.Resources
            SpellEffect.LoadContent();

            // Cấu hình Timer
            this.animationTimer.Interval = 100; // 100ms = 10 frame/giây. Chỉnh tốc độ ở đây
            this.animationTimer.Tick += AnimationTimer_Tick;
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            // Vẽ một dấu + ở vị trí "player" để dễ hình dung
            Pen playerPen = new Pen(Color.White, 2);
            e.Graphics.DrawLine(playerPen, playerPosition.X - 10, playerPosition.Y, playerPosition.X + 10, playerPosition.Y);
            e.Graphics.DrawLine(playerPen, playerPosition.X, playerPosition.Y - 10, playerPosition.X, playerPosition.Y + 10);

            // <<< MỚI: Vẽ mũi tên nhỏ chỉ hướng player
            if (isPlayerFacingRight)
            {
                e.Graphics.DrawLine(playerPen, playerPosition.X + 10, playerPosition.Y, playerPosition.X + 5, playerPosition.Y - 5);
                e.Graphics.DrawLine(playerPen, playerPosition.X + 10, playerPosition.Y, playerPosition.X + 5, playerPosition.Y + 5);
            }
            else
            {
                e.Graphics.DrawLine(playerPen, playerPosition.X - 10, playerPosition.Y, playerPosition.X - 5, playerPosition.Y - 5);
                e.Graphics.DrawLine(playerPen, playerPosition.X - 10, playerPosition.Y, playerPosition.X - 5, playerPosition.Y + 5);
            }

            playerPen.Dispose();

            // Nếu hiệu ứng đang active thì BẢO NÓ TỰ VẼ
            if (currentEffect != null && currentEffect.IsActive)
            {
                currentEffect.Draw(e.Graphics);
            }
        }

        /// <summary>
        /// Sửa lại hàm này: Thêm tham số "hướng"
        /// </summary>
        private void FireAttack(Point position, bool facingRight)
        {
            // Chỉ tạo hiệu ứng mới nếu hiệu ứng cũ đã chạy xong
            if (currentEffect == null || !currentEffect.IsActive)
            {
                // Tạo một đối tượng hiệu ứng mới tại vị trí
                // <<< MỚI: Báo cho effect biết hướng quay
                currentEffect = new SpellEffect(position, facingRight);

                // Bắt đầu chạy Timer
                this.animationTimer.Start();
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            // <<< MỚI: Thêm phím A và D để đổi hướng
            if (e.KeyCode == Keys.A)
            {
                isPlayerFacingRight = false;
                this.Invalidate(); // Vẽ lại form để thấy mũi tên đổi hướng
            }

            if (e.KeyCode == Keys.D)
            {
                isPlayerFacingRight = true;
                this.Invalidate(); // Vẽ lại form
            }

            // Khi nhấn phím cách
            if (e.KeyCode == Keys.Space)
            {
                // <<< MỚI: Bắn chiêu theo hướng của player
                FireAttack(playerPosition, isPlayerFacingRight);
            }
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            // Nếu có hiệu ứng đang chạy
            if (currentEffect != null && currentEffect.IsActive)
            {
                // Bảo nó tự cập nhật
                currentEffect.Update();

                // Nếu sau khi cập nhật mà nó hết active
                if (!currentEffect.IsActive)
                {
                    // Dừng timer
                    this.animationTimer.Stop();
                }

                // Yêu cầu vẽ lại Form (để cập nhật frame mới)
                this.Invalidate();
            }
            else
            {
                // Nếu không có gì để update thì dừng timer
                this.animationTimer.Stop();
            }
        }
    }

    // ===================================================================
    // Class SpellEffect được gộp vào đây
    // ===================================================================
    public class SpellEffect
    {
        // --- Dữ liệu tĩnh (Static) ---
        private static List<Image> frames = new List<Image>();
        private static bool isLoaded = false;

        // <<< MỚI: CHỖ CHỈNH TỈ LỆ
        // 1.0f = 100% (như cũ), 0.5f = 50% (nhỏ bằng 1 nửa)
        // Sếp sửa số này nhé!
        private static float scale = 0.5f;


        // --- Dữ liệu của đối tượng (Instance) ---
        private Point position;
        private int currentFrameIndex;
        private bool isActive;

        // <<< MỚI: Thêm biến lưu hướng
        private bool facingRight;

        // <<< MỚI: Thêm biến tốc độ di chuyển
        private int speedX;

        // <<< MỚI: Hằng số tốc độ
        // Sếp chỉnh tốc độ lướt ở đây (pixel mỗi tick timer)
        private const int MOVEMENT_SPEED = 15;

        // --- Thuộc tính (Properties) ---
        public bool IsActive
        {
            get { return isActive; }
        }

        /// <summary>
        /// Sửa lại hàm này để load ảnh từ Properties.Resources
        /// </summary>
        public static void LoadContent()
        {
            if (isLoaded) return; // Chỉ load 1 lần

            try
            {
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
                MessageBox.Show("Lỗi khi load ảnh từ Resources: " + ex.Message + "\nKiểm tra lại tên ảnh trong Resources.resx nhé!");
            }
        }

        /// <summary>
        /// Constructor: <<< Sửa lại, thêm tham số 'facingRight'
        /// </summary>
        public SpellEffect(Point startPosition, bool facingRight)
        {
            if (!isLoaded || frames.Count == 0)
            {
                this.isActive = false;
                return;
            }

            this.position = startPosition;
            this.currentFrameIndex = 0;
            this.isActive = true;
            this.facingRight = facingRight; // <<< MỚI: Lưu lại hướng

            // <<< MỚI: Set tốc độ dựa vào hướng
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
        /// Hàm Update: Được gọi mỗi tick của Timer
        /// </summary>
        public void Update()
        {
            if (!isActive) return;

            // <<< MỚI: Cập nhật vị trí
            // Mỗi lần update (mỗi tick của timer)
            // Vị trí X sẽ được cộng thêm tốc độ
            this.position.X += this.speedX;

            // Chuyển frame tiếp theo
            currentFrameIndex++;

            // Nếu đã qua frame cuối
            if (currentFrameIndex >= frames.Count)
            {
                isActive = false; // Hủy kích hoạt
            }
        }

        /// <summary>
        /// Hàm Draw: 
        /// </summary>
        public void Draw(Graphics g)
        {
            if (!isActive) return;

            // Lấy ảnh gốc
            Image currentFrame = frames[currentFrameIndex];

            // --- TÍNH TOÁN TỈ LỆ ---
            // Lấy kích thước mới dựa trên tỉ lệ (scale)
            int newWidth = (int)(currentFrame.Width * scale);
            int newHeight = (int)(currentFrame.Height * scale);

            // Tính toán vị trí X, Y để căn giữa cục lửa
            int drawX = position.X - newWidth / 2;
            int drawY = position.Y - newHeight / 2;

            // Tạo một hình chữ nhật là khu vực sẽ vẽ
            Rectangle destRect = new Rectangle(drawX, drawY, newWidth, newHeight);

            // --- XỬ LÝ QUAY HƯỚNG ---
            // Phải "Clone" (nhân bản) ảnh, VÌ NẾU FLIP ẢNH GỐC (STATIC) NÓ SẼ BỊ HƯ LUÔN
            using (Image frameToDraw = (Image)currentFrame.Clone())
            {
                // Nếu đang quay phải
                if (this.facingRight)
                {
                    // Lật ảnh theo chiều ngang
                    frameToDraw.RotateFlip(RotateFlipType.RotateNoneFlipX);
                }

                // Vẽ ảnh đã (có thể) lật, và co dãn vào hình chữ nhật đích
                g.DrawImage(frameToDraw, destRect);
            }
        }
    }
}