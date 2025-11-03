using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DTO;
using System.Data.Entity;

namespace QL_KhachSan
{
    public partial class UCDatPhong : UserControl
    {
        private static UCDatPhong _Instance;

        public static UCDatPhong Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new UCDatPhong();
                }
                return _Instance;
            }
        }
        private UCDatPhong()
        {
            InitializeComponent();
        }

        // Hàm tự động sinh mã phiếu thuê phòng (không được nhập tay)
        // Mã chỉ được tối đa 10 ký tự theo database schema
        private string GenerateMaPhieuThuePhong(KhachSanContext context)
        {
            // Format: yyMMddHHmm (10 ký tự) - bỏ giây để giữ 10 ký tự
            string maPhieu = DateTime.Now.ToString("yyMMddHHmm");
            
            // Đảm bảo không trùng - nếu trùng thì thêm số ngẫu nhiên
            int attempt = 0;
            while (context.PhieuThuePhongs.Any(pt => pt.MaPhieuThuePhong == maPhieu) && attempt < 100)
            {
                System.Threading.Thread.Sleep(100); // Đợi 100ms để đảm bảo phút khác
                maPhieu = DateTime.Now.ToString("yyMMddHHmm");
                attempt++;
            }
            
            // Nếu vẫn trùng sau 100 lần thử, thêm số ngẫu nhiên (vẫn giữ 10 ký tự)
            if (attempt >= 100)
            {
                Random rnd = new Random();
                // Format: yyMMddHH + 2 số ngẫu nhiên = 10 ký tự
                maPhieu = DateTime.Now.ToString("yyMMddHH") + rnd.Next(10, 99).ToString();
                
                // Đảm bảo không trùng
                while (context.PhieuThuePhongs.Any(pt => pt.MaPhieuThuePhong == maPhieu))
                {
                    maPhieu = DateTime.Now.ToString("yyMMddHH") + rnd.Next(10, 99).ToString();
                }
            }
            
            return maPhieu;
        }

        private void UCDatPhong_Load(object sender, EventArgs e)
        {
            LoadPhongStandard();
            bunifuDatepicker1.Value = new DateTime(2025, 10, 2);
            bunifuDatepicker2.Value = new DateTime(2025, 10, 2);
            bunifuDatepicker3.Value = new DateTime(2025, 10, 2);
            
            // Thêm event handler cho nút tìm kiếm khách hàng
            if (pictureBox1 != null)
            {
                pictureBox1.Click += PictureBox1_Click;
                pictureBox1.Cursor = Cursors.Hand;
            }
        }

        private void PictureBox1_Click(object sender, EventArgs e)
        {
            // Tìm kiếm khách hàng theo CMND
            if (string.IsNullOrWhiteSpace(bunifuCustomTextbox1.Text))
            {
                MessageBox.Show("Vui lòng nhập số CMND để tìm kiếm!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int cmnd;
            if (!int.TryParse(bunifuCustomTextbox1.Text, out cmnd))
            {
                MessageBox.Show("CMND phải là số!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                KhachSanContext context = new KhachSanContext();
                var khachHang = context.KhachHangs.FirstOrDefault(kh => kh.CMND == cmnd);

                if (khachHang != null)
                {
                    // Tìm thấy - tự động điền thông tin khách hàng
                    bunifuCustomTextbox2.Text = khachHang.HoTen;
                    bunifuCustomTextbox4.Text = khachHang.SDT;
                    richTextBox1.Text = khachHang.DiaChi;
                    radioButton5.Checked = khachHang.GioiTinh; // Nam
                    radioButton6.Checked = !khachHang.GioiTinh; // Nữ
                    
                    // Nếu đang ở chế độ nhận phòng, tìm thông tin đặt phòng
                    if (cardNhanPhong.Visible)
                    {
                        LoadThongTinDatPhong(cmnd, context);
                    }
                    
                    MessageBox.Show($"Tìm thấy khách hàng: {khachHang.HoTen}", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    // Không tìm thấy - yêu cầu nhập thông tin mới
                    bunifuCustomTextbox2.Text = "";
                    bunifuCustomTextbox4.Text = "";
                    richTextBox1.Text = "";
                    radioButton5.Checked = true;
                    
                    // Nếu đang ở chế độ nhận phòng, xóa thông tin đặt phòng
                    if (cardNhanPhong.Visible)
                    {
                        bunifuCustomTextbox6.Text = "";
                        bunifuDropdown1.selectedIndex = -1;
                        bunifuDatepicker1.Value = DateTime.Now.AddDays(1);
                    }
                    
                    MessageBox.Show($"Không tìm thấy khách hàng với CMND: {cmnd}!\nVui lòng nhập thông tin khách hàng mới.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tìm kiếm khách hàng: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Hàm load thông tin đặt phòng khi nhận phòng
        private void LoadThongTinDatPhong(int cmnd, KhachSanContext context)
        {
            try
            {
                // Tìm phiếu đặt phòng của khách hàng
                // Logic: Tìm phiếu mới nhất của khách hàng này (có thể là đã đặt trước hoặc chưa check-in)
                // Ưu tiên: Phiếu có NgayThue gần nhất với hôm nay (có thể là hôm nay, tương lai, hoặc quá khứ gần)
                DateTime today = DateTime.Now.Date;
                
                // Tìm tất cả phiếu của khách hàng này
                var allPhieu = context.PhieuThuePhongs
                    .Include("Phong")
                    .Where(pt => pt.CMNDKhachHang == cmnd)
                    .ToList(); // Load về memory trước
                
                if (allPhieu.Count == 0)
                {
                    // Không có phiếu nào
                    bunifuCustomTextbox6.Text = "";
                    bunifuDropdown1.selectedIndex = -1;
                    bunifuDatepicker1.Value = DateTime.Now.AddDays(1);
                    
                    MessageBox.Show($"Khách hàng này chưa có phiếu đặt phòng nào!\nVui lòng chọn phòng và tạo phiếu thuê mới.", 
                        "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                
                // Ưu tiên 1: Tìm phiếu có NgayThue là hôm nay hoặc trong tương lai (đã đặt trước)
                var phieuDatPhong = allPhieu
                    .Where(pt => pt.NgayThue.Date >= today)
                    .OrderBy(pt => pt.NgayThue)
                    .FirstOrDefault();
                
                // Ưu tiên 2: Nếu không có, tìm phiếu có NgayThue trong quá khứ gần đây (đã đặt nhưng chưa nhận)
                // Cho phép nhận phòng muộn (trong vòng 30 ngày gần đây)
                if (phieuDatPhong == null)
                {
                    phieuDatPhong = allPhieu
                        .Where(pt => pt.NgayThue.Date < today && pt.NgayThue.Date >= today.AddDays(-30))
                        .OrderByDescending(pt => pt.NgayThue)
                        .FirstOrDefault();
                }
                
                // Ưu tiên 3: Nếu vẫn không tìm thấy, lấy phiếu mới nhất của khách hàng này
                // (để hỗ trợ trường hợp đặc biệt)
                if (phieuDatPhong == null)
                {
                    phieuDatPhong = allPhieu
                        .OrderByDescending(pt => pt.NgayThue)
                        .FirstOrDefault();
                }

                if (phieuDatPhong != null)
                {
                    // Tìm thấy phiếu đặt phòng - tự động điền thông tin
                    bunifuCustomTextbox6.Text = phieuDatPhong.MaPhieuThuePhong;
                    bunifuDatepicker1.Value = phieuDatPhong.NgayTra;

                    // Tự động chọn phòng đã đặt trong dropdown
                    var phongs = Phong.GetPhongByLoai(phieuDatPhong.Phong.LoaiPhong);
                    List<string> items = new List<string>();
                    int selectedIndex = -1;
                    
                    for (int i = 0; i < phongs.Count; i++)
                    {
                        var p = phongs[i];
                        string itemText = $"Phòng {p.MaPhong} - {p.DonGia:N0} VNĐ - {p.SoNguoiToiDa} người";
                        items.Add(itemText);
                        if (p.MaPhong == phieuDatPhong.MaPhong)
                        {
                            selectedIndex = i;
                        }
                    }
                    
                    bunifuDropdown1.Items = items.ToArray();
                    if (selectedIndex >= 0)
                    {
                        bunifuDropdown1.selectedIndex = selectedIndex;
                    }

                    // Chọn loại phòng tương ứng
                    if (phieuDatPhong.Phong.LoaiPhong == "VIP")
                    {
                        radioButton3.Checked = true;
                    }
                    else if (phieuDatPhong.Phong.LoaiPhong == "Standard" || phieuDatPhong.Phong.LoaiPhong == "Deluxe")
                    {
                        radioButton4.Checked = true;
                    }

                    MessageBox.Show($"Đã tìm thấy phiếu đặt phòng!\nMã phiếu: {phieuDatPhong.MaPhieuThuePhong}\nPhòng: {phieuDatPhong.MaPhong}\nNgày nhận: {phieuDatPhong.NgayThue:dd/MM/yyyy}", 
                        "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    // Không tìm thấy phiếu đặt phòng
                    bunifuCustomTextbox6.Text = "";
                    bunifuDropdown1.selectedIndex = -1;
                    bunifuDatepicker1.Value = DateTime.Now.AddDays(1);
                    
                    MessageBox.Show($"Khách hàng này chưa có phiếu đặt phòng nào!\nVui lòng chọn phòng và tạo phiếu thuê mới.", 
                        "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi load thông tin đặt phòng: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadPhongStandard()
        {
            try
            {
                var phongs = Phong.GetPhongByLoai("Standard");
                List<string> items = new List<string>();
                foreach (var p in phongs)
                {
                    items.Add($"Phòng {p.MaPhong} - {p.DonGia:N0} VNĐ - {p.SoNguoiToiDa} người");
                }
                bunifuDropdown1.Items = items.ToArray();
                bunifuDropdown2.Items = items.ToArray();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi load phòng: {ex.Message}", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadPhongVIP()
        {
            try
            {
                var phongs = Phong.GetPhongByLoai("VIP");
                List<string> items = new List<string>();
                foreach (var p in phongs)
                {
                    items.Add($"Phòng {p.MaPhong} - {p.DonGia:N0} VNĐ - {p.SoNguoiToiDa} người");
                }
                bunifuDropdown1.Items = items.ToArray();
                bunifuDropdown2.Items = items.ToArray();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi load phòng: {ex.Message}", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void bunifuCustomLabel4_Click(object sender, EventArgs e)
        {

        }

        private void bunifuCustomTextbox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton3.Checked)
                LoadPhongVIP();
        }

        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton4.Checked)
                LoadPhongStandard();
        }

        private void radioButton7_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton7.Checked)
                LoadPhongVIP();
        }

        private void radioButton8_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton8.Checked)
                LoadPhongStandard();
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            label1.Text = "Thông tin nhận phòng";
            cardDatPhong.Visible = false;
            cardNhanPhong.Visible = true;
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            label1.Text = "Thông tin đặt phòng";
            cardDatPhong.Visible = true;
            cardNhanPhong.Visible = false;
        }

        private void bunifuCustomLabel6_Click(object sender, EventArgs e)
        {

        }

        private void btnAccept_Click(object sender, EventArgs e)
        {
            try
            {
                // Xác định mode: Nhận phòng (cardNhanPhong) hoặc Đặt phòng (cardDatPhong)
                if (cardNhanPhong.Visible)
                {
                    // Mode: Khách nhận phòng (Check-in ngay)
                    if (string.IsNullOrWhiteSpace(bunifuCustomTextbox1.Text))
                    {
                        MessageBox.Show("Vui lòng nhập số CMND để tìm khách hàng!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    int cmnd;
                    if (!int.TryParse(bunifuCustomTextbox1.Text, out cmnd))
                    {
                        MessageBox.Show("CMND phải là số!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Tìm hoặc tạo khách hàng
                    KhachSanContext context = new KhachSanContext();
                    var khachHang = context.KhachHangs.FirstOrDefault(kh => kh.CMND == cmnd);
                    
                    if (khachHang == null)
                    {
                        // Kiểm tra thông tin khách hàng đã nhập đầy đủ chưa
                        if (string.IsNullOrWhiteSpace(bunifuCustomTextbox2.Text) ||
                            string.IsNullOrWhiteSpace(bunifuCustomTextbox4.Text) ||
                            string.IsNullOrWhiteSpace(richTextBox1.Text))
                        {
                            MessageBox.Show("Không tìm thấy khách hàng! Vui lòng nhập đầy đủ thông tin khách hàng.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        // Kiểm tra độ dài các trường
                        if (bunifuCustomTextbox2.Text.Length > 50)
                        {
                            MessageBox.Show("Tên khách hàng quá dài (tối đa 50 ký tự)!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        if (bunifuCustomTextbox4.Text.Length > 10)
                        {
                            MessageBox.Show("Số điện thoại quá dài (tối đa 10 ký tự)!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        if (richTextBox1.Text.Length > 100)
                        {
                            MessageBox.Show("Địa chỉ quá dài (tối đa 100 ký tự)!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        // Tạo khách hàng mới
                        khachHang = new KhachHang
                        {
                            CMND = cmnd,
                            HoTen = bunifuCustomTextbox2.Text,
                            SDT = bunifuCustomTextbox4.Text,
                            DiaChi = richTextBox1.Text,
                            GioiTinh = radioButton5.Checked,
                            NgaySinh = DateTime.Now.AddYears(-30)
                        };

                        context.KhachHangs.Add(khachHang);
                        context.SaveChanges();
                    }

                    // Kiểm tra phòng đã chọn
                    if (bunifuDropdown1.selectedIndex == -1)
                    {
                        MessageBox.Show("Vui lòng chọn phòng!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Lấy mã phòng từ dropdown
                    string selectedRoom = bunifuDropdown1.selectedValue;
                    int maPhong = int.Parse(selectedRoom.Split(' ')[1]);

                    // Nếu có mã phiếu thuê phòng (từ phiếu đặt phòng), cập nhật thay vì tạo mới
                    string maPhieuThuePhong = bunifuCustomTextbox6.Text.Trim();
                    
                    if (!string.IsNullOrEmpty(maPhieuThuePhong))
                    {
                        // Cập nhật phiếu thuê phòng đã đặt (chuyển từ đặt phòng sang nhận phòng)
                        var phieuThue = context.PhieuThuePhongs
                            .FirstOrDefault(pt => pt.MaPhieuThuePhong == maPhieuThuePhong);
                        
                        if (phieuThue != null)
                        {
                            // Cập nhật ngày thuê thành hôm nay (nhận phòng)
                            phieuThue.NgayThue = DateTime.Now.Date;
                            phieuThue.NgayTra = bunifuDatepicker1.Value.Date;
                            phieuThue.MaPhong = maPhong; // Cho phép đổi phòng nếu cần
                            
                            context.SaveChanges();
                            
                            MessageBox.Show($"Nhận phòng thành công!\nMã phiếu: {phieuThue.MaPhieuThuePhong}\nPhòng: {maPhong}\nNgày trả: {phieuThue.NgayTra:dd/MM/yyyy}", 
                                "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            ClearForm();
                            return;
                        }
                    }

                    // Tạo phiếu thuê phòng mới - Tự động sinh mã phiếu (không được nhập)
                    // Sinh mã duy nhất: yyMMddHHmmss (thêm giây để tránh trùng)
                    string maPhieu = GenerateMaPhieuThuePhong(context);
                    
                    // Kiểm tra mã có trùng không (nếu trùng thì thêm số ngẫu nhiên)
                    while (context.PhieuThuePhongs.Any(pt => pt.MaPhieuThuePhong == maPhieu))
                    {
                        maPhieu = GenerateMaPhieuThuePhong(context);
                    }
                    
                    PhieuThuePhong phieuThueMoi = new PhieuThuePhong
                    {
                        MaPhieuThuePhong = maPhieu,
                        CMNDKhachHang = cmnd,
                        MaPhong = maPhong,
                        NgayThue = DateTime.Now.Date, // Nhận phòng ngay hôm nay
                        NgayTra = bunifuDatepicker1.Value.Date
                    };

                    context.PhieuThuePhongs.Add(phieuThueMoi);
                    context.SaveChanges();

                    MessageBox.Show($"Nhận phòng thành công!\nMã phiếu: {maPhieu}\nPhòng: {maPhong}\nNgày trả: {phieuThueMoi.NgayTra:dd/MM/yyyy}", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ClearForm();
                }
                else if (cardDatPhong.Visible)
                {
                    // Mode: Khách đặt phòng (Booking trước)
                    if (string.IsNullOrWhiteSpace(bunifuCustomTextbox1.Text))
                    {
                        MessageBox.Show("Vui lòng nhập số CMND!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    int cmnd;
                    if (!int.TryParse(bunifuCustomTextbox1.Text, out cmnd))
                    {
                        MessageBox.Show("CMND phải là số!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Tìm hoặc tạo khách hàng
                    KhachSanContext context = new KhachSanContext();
                    var khachHang = context.KhachHangs.FirstOrDefault(kh => kh.CMND == cmnd);
                    
                    if (khachHang == null)
                    {
                        // Kiểm tra thông tin khách hàng đã nhập đầy đủ chưa
                        if (string.IsNullOrWhiteSpace(bunifuCustomTextbox2.Text) ||
                            string.IsNullOrWhiteSpace(bunifuCustomTextbox4.Text) ||
                            string.IsNullOrWhiteSpace(richTextBox1.Text))
                        {
                            MessageBox.Show("Không tìm thấy khách hàng! Vui lòng nhập đầy đủ thông tin khách hàng.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        // Kiểm tra độ dài các trường
                        if (bunifuCustomTextbox2.Text.Length > 50)
                        {
                            MessageBox.Show("Tên khách hàng quá dài (tối đa 50 ký tự)!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        if (bunifuCustomTextbox4.Text.Length > 10)
                        {
                            MessageBox.Show("Số điện thoại quá dài (tối đa 10 ký tự)!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        if (richTextBox1.Text.Length > 100)
                        {
                            MessageBox.Show("Địa chỉ quá dài (tối đa 100 ký tự)!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        // Tạo khách hàng mới
                        khachHang = new KhachHang
                        {
                            CMND = cmnd,
                            HoTen = bunifuCustomTextbox2.Text,
                            SDT = bunifuCustomTextbox4.Text,
                            DiaChi = richTextBox1.Text,
                            GioiTinh = radioButton5.Checked,
                            NgaySinh = DateTime.Now.AddYears(-30)
                        };

                        context.KhachHangs.Add(khachHang);
                        context.SaveChanges();
                    }

                    // Kiểm tra phòng đã chọn
                    if (bunifuDropdown2.selectedIndex == -1)
                    {
                        MessageBox.Show("Vui lòng chọn phòng!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Lấy mã phòng từ dropdown
                    string selectedRoom = bunifuDropdown2.selectedValue;
                    int maPhong = int.Parse(selectedRoom.Split(' ')[1]);

                    // Tạo phiếu thuê phòng cho đặt trước - Tự động sinh mã phiếu (không được nhập)
                    // Sinh mã duy nhất: yyMMddHHmmss (thêm giây để tránh trùng)
                    string maPhieu = GenerateMaPhieuThuePhong(context);
                    
                    // Kiểm tra mã có trùng không (nếu trùng thì sinh lại)
                    while (context.PhieuThuePhongs.Any(pt => pt.MaPhieuThuePhong == maPhieu))
                    {
                        maPhieu = GenerateMaPhieuThuePhong(context);
                    }
                    
                    PhieuThuePhong phieuThue = new PhieuThuePhong
                    {
                        MaPhieuThuePhong = maPhieu,
                        CMNDKhachHang = cmnd,
                        MaPhong = maPhong,
                        NgayThue = bunifuDatepicker2.Value.Date, // Ngày đặt trong tương lai
                        NgayTra = bunifuDatepicker3.Value.Date
                    };

                    context.PhieuThuePhongs.Add(phieuThue);
                    context.SaveChanges();

                    MessageBox.Show($"Đặt phòng thành công!\nMã phiếu: {maPhieu}\nPhòng: {maPhong}\nNgày nhận: {phieuThue.NgayThue:dd/MM/yyyy}\nNgày trả: {phieuThue.NgayTra:dd/MM/yyyy}", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ClearForm();
                }
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                string errorMessage = "Lỗi validation:\n";
                foreach (var eve in ex.EntityValidationErrors)
                {
                    foreach (var ve in eve.ValidationErrors)
                    {
                        errorMessage += $"{ve.PropertyName}: {ve.ErrorMessage}\n";
                    }
                }
                MessageBox.Show(errorMessage, "Lỗi Validation", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnXoa_Click(object sender, EventArgs e)
        {
            ClearForm();
        }

        private void ClearForm()
        {
            // Clear thông tin khách hàng
            bunifuCustomTextbox1.Text = "";
            bunifuCustomTextbox2.Text = "";
            bunifuCustomTextbox4.Text = "";
            richTextBox1.Text = "";
            radioButton5.Checked = true;

            // Clear dropdowns
            bunifuDropdown1.selectedIndex = -1;
            bunifuDropdown2.selectedIndex = -1;

            // Reset dates
            bunifuDatepicker1.Value = new DateTime(2025, 10, 2);
            bunifuDatepicker2.Value = new DateTime(2025, 10, 2);
            bunifuDatepicker3.Value = new DateTime(2025, 10, 2);
        }
    }
}
