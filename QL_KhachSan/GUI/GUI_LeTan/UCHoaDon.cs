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
    public partial class UCHoaDon : UserControl
    {
        private static UCHoaDon _Instance;
        public static UCHoaDon Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new UCHoaDon();
                }
                return _Instance;
            }
        }
        
        private int currentCMND;
        private Dictionary<string, PhieuThuePhong> phieuThuePhongDict; // Lưu mapping giữa display text và PhieuThuePhong
        private ComboBox comboBoxPhieuThuePhong; // Dropdown để chọn phiếu thuê phòng
        
        private UCHoaDon()
        {
            InitializeComponent();
            phieuThuePhongDict = new Dictionary<string, PhieuThuePhong>();
            this.Load += UCHoaDon_Load;
        }

        private void InitializeComboBoxPhieuThuePhong()
        {
            // Tạo ComboBox để chọn phiếu thuê phòng
            comboBoxPhieuThuePhong = new ComboBox();
            comboBoxPhieuThuePhong.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxPhieuThuePhong.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F);
            comboBoxPhieuThuePhong.Width = 294;
            comboBoxPhieuThuePhong.Height = 35;
            
            // Đặt vị trí lên trên, tránh đè lên phần Nhân Viên Lập
            if (bunifuCards2 != null)
            {
                // Đặt ComboBox ở trên cùng, phía trên cả textbox Mã Hóa Đơn
                // Sử dụng vị trí Y nhỏ hơn để ở trên cùng
                int comboBoxY = 100; // Đặt ở trên, tránh đè lên phần Nhân Viên Lập (Y=163)
                
                comboBoxPhieuThuePhong.Location = new System.Drawing.Point(
                    bunifuCustomTextbox5.Location.X,  // Cùng X với các textbox khác
                    comboBoxY
                );
                comboBoxPhieuThuePhong.Anchor = bunifuCustomTextbox5.Anchor;
                bunifuCards2.Controls.Add(comboBoxPhieuThuePhong);
                comboBoxPhieuThuePhong.BringToFront();
                comboBoxPhieuThuePhong.TabIndex = 0; // Tab đầu tiên
                
                // Thêm label cho ComboBox
                Label lblPhieuThuePhong = new Label();
                lblPhieuThuePhong.Text = "Chọn Phiếu Thuê Phòng";
                lblPhieuThuePhong.Font = new System.Drawing.Font("Comic Sans MS", 11F, System.Drawing.FontStyle.Bold);
                lblPhieuThuePhong.ForeColor = System.Drawing.SystemColors.GrayText;
                lblPhieuThuePhong.Location = new System.Drawing.Point(
                    bunifuCustomLabel17.Location.X,  // Cùng vị trí X với label CMND
                    comboBoxY - 25  // Label phía trên ComboBox
                );
                lblPhieuThuePhong.AutoSize = true;
                bunifuCards2.Controls.Add(lblPhieuThuePhong);
                lblPhieuThuePhong.BringToFront();
            }
            
            comboBoxPhieuThuePhong.SelectedIndexChanged += ComboBoxPhieuThuePhong_SelectedIndexChanged;
        }

        private void ComboBoxPhieuThuePhong_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxPhieuThuePhong.SelectedItem != null && comboBoxPhieuThuePhong.SelectedIndex > 0)
            {
                string selectedText = comboBoxPhieuThuePhong.SelectedItem.ToString();
                if (phieuThuePhongDict.ContainsKey(selectedText))
                {
                    var phieuThue = phieuThuePhongDict[selectedText];
                    
                    // Tự động điền CMND
                    bunifuCustomTextbox5.Text = phieuThue.CMNDKhachHang.ToString();
                    
                    // Tự động điền mã phiếu thuê phòng (readonly, không được nhập)
                    bunifuCustomTextbox7.Text = phieuThue.MaPhieuThuePhong;
                    
                    // Tự động sinh mã hóa đơn (KHÔNG được nhập tay)
                    try
                    {
                        KhachSanContext contextTemp = new KhachSanContext();
                        bunifuCustomTextbox1.Text = GenerateMaHoaDon(contextTemp);
                        contextTemp.Dispose();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Lỗi khi sinh mã hóa đơn: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    
                    // Tự động tính và cập nhật tổng tiền (bao gồm cả dịch vụ)
                    ReloadTongTienVaChiTiet(phieuThue.MaPhieuThuePhong);
                }
            }
        }

        // Hàm reload tổng tiền và chi tiết (khi có thay đổi dịch vụ)
        private void ReloadTongTienVaChiTiet(string maPhieuThuePhong)
        {
            try
            {
                // Load lại từ database để lấy dịch vụ mới nhất
                KhachSanContext context = new KhachSanContext();
                var phieuThue = context.PhieuThuePhongs
                    .Include("Phong")
                    .Include("PhieuDichVus")
                    .Include("PhieuDichVus.DichVu")
                    .FirstOrDefault(pt => pt.MaPhieuThuePhong == maPhieuThuePhong);

                if (phieuThue != null)
                {
                    // Cập nhật lại vào dictionary
                    string displayText = $"Mã: {phieuThue.MaPhieuThuePhong} - KH: {phieuThue.KhachHang.HoTen} (CMND: {phieuThue.CMNDKhachHang}) - Phòng: {phieuThue.MaPhong} - {phieuThue.NgayThue:dd/MM/yyyy}";
                    if (phieuThuePhongDict.ContainsKey(displayText))
                    {
                        phieuThuePhongDict[displayText] = phieuThue;
                    }

                    // Tính lại tổng tiền (bao gồm cả dịch vụ mới)
                    float tongTien = 0;
                    
                    // Tính tiền phòng
                    int soNgay = (phieuThue.NgayTra.Date - phieuThue.NgayThue.Date).Days + 1;
                    if (soNgay < 1) soNgay = 1;
                    tongTien += phieuThue.Phong.DonGia * soNgay;
                    
                    // Tính tiền dịch vụ (từ tất cả dịch vụ khách đã chọn)
                    if (phieuThue.PhieuDichVus != null && phieuThue.PhieuDichVus.Count > 0)
                    {
                        foreach (var pdv in phieuThue.PhieuDichVus)
                        {
                            if (pdv.DichVu != null)
                            {
                                tongTien += pdv.DichVu.DonGia * pdv.SoLuong;
                            }
                            else
                            {
                                // Load lại nếu null
                                var dichVu = context.DichVus.FirstOrDefault(dv => dv.MaDichVu == pdv.MaDichVu);
                                if (dichVu != null)
                                {
                                    tongTien += dichVu.DonGia * pdv.SoLuong;
                                }
                            }
                        }
                    }
                    
                    // Cập nhật tổng tiền
                    bunifuCustomTextbox8.Text = tongTien.ToString("N0");
                    
                    // Hiển thị chi tiết hóa đơn (bao gồm dịch vụ mới)
                    LoadChiTietHoaDonFromPhieuThue(phieuThue);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi reload tổng tiền: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Hàm load chi tiết từ PhieuThuePhong object (đã có data)
        private void LoadChiTietHoaDonFromPhieuThue(PhieuThuePhong phieuThue)
        {
            try
            {
                DataGridView dgvChiTiet = GetOrCreateDataGridViewChiTiet();
                
                if (dgvChiTiet != null && phieuThue != null)
                {
                    SetupDataGridViewColumns(dgvChiTiet);
                    dgvChiTiet.Rows.Clear();
                    
                    // Thêm chi tiết phòng - lấy giá phòng đã đặt
                    int soNgay = (phieuThue.NgayTra.Date - phieuThue.NgayThue.Date).Days + 1;
                    if (soNgay < 1) soNgay = 1;
                    float thanhTienPhong = phieuThue.Phong.DonGia * soNgay;
                    
                    dgvChiTiet.Rows.Add(1, "Phòng", $"Phòng {phieuThue.MaPhong}", soNgay, phieuThue.Phong.DonGia, thanhTienPhong);
                    
                    // Thêm chi tiết dịch vụ - lấy giá từ dịch vụ khách đã chọn
                    int stt = 2;
                    if (phieuThue.PhieuDichVus != null && phieuThue.PhieuDichVus.Count > 0)
                    {
                        foreach (var pdv in phieuThue.PhieuDichVus)
                        {
                            if (pdv.DichVu != null)
                            {
                                float thanhTienDV = pdv.DichVu.DonGia * pdv.SoLuong;
                                dgvChiTiet.Rows.Add(stt++, "Dịch Vụ", pdv.DichVu.TenDichVu, pdv.SoLuong, pdv.DichVu.DonGia, thanhTienDV);
                            }
                        }
                    }
                    
                    bunifuCards1.Visible = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi load chi tiết hóa đơn: {ex.Message}", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void LoadChiTietHoaDon(string maPhieuThuePhong)
        {
            try
            {
                KhachSanContext context = new KhachSanContext();
                var phieuThue = context.PhieuThuePhongs
                    .Include("Phong")
                    .Include("PhieuDichVus")
                    .Include("PhieuDichVus.DichVu")
                    .FirstOrDefault(pt => pt.MaPhieuThuePhong == maPhieuThuePhong);
                
                if (phieuThue == null) return;
                
                // Dùng hàm chung để load chi tiết
                LoadChiTietHoaDonFromPhieuThue(phieuThue);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi load chi tiết hóa đơn: {ex.Message}", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadChiTietHoaDonByMaHoaDon(string maHoaDon)
        {
            try
            {
                KhachSanContext context = new KhachSanContext();
                
                // Kiểm tra xem có ChiTietHoaDon trong database không
                var chiTietHoaDons = context.ChiTietHoaDons
                    .Where(ct => ct.MaHoaDon == maHoaDon)
                    .OrderBy(ct => ct.STT)
                    .ToList();
                
                if (chiTietHoaDons == null || chiTietHoaDons.Count == 0)
                {
                    // Nếu chưa có chi tiết trong database, thử load từ phiếu thuê phòng để hiển thị tạm
                    var hoaDon = context.HoaDons.FirstOrDefault(hd => hd.MaHoaDon == maHoaDon);
                    if (hoaDon != null)
                    {
                        // Tìm phiếu thuê phòng liên quan đến hóa đơn này
                        var phieuThue = context.PhieuThuePhongs
                            .Include("Phong")
                            .Include("PhieuDichVus")
                            .Include("PhieuDichVus.DichVu")
                            .Where(pt => pt.CMNDKhachHang == hoaDon.CMNDKhachHang && 
                                         pt.NgayThue <= hoaDon.NgayXuat)
                            .OrderByDescending(pt => pt.NgayThue)
                            .FirstOrDefault();
                        
                        if (phieuThue != null)
                        {
                            // Hiển thị từ phiếu thuê phòng (dữ liệu tạm)
                            LoadChiTietHoaDon(phieuThue.MaPhieuThuePhong);
                            MessageBox.Show($"Chưa có chi tiết hóa đơn trong database. Đang hiển thị từ phiếu thuê phòng.\nMã phiếu: {phieuThue.MaPhieuThuePhong}", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                    MessageBox.Show("Không tìm thấy chi tiết hóa đơn trong database!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                // Tạo hoặc lấy DataGridView
                DataGridView dgvChiTiet = GetOrCreateDataGridViewChiTiet();
                
                if (dgvChiTiet != null)
                {
                    // Setup columns
                    SetupDataGridViewColumns(dgvChiTiet);
                    
                    // Xóa dữ liệu cũ
                    dgvChiTiet.Rows.Clear();
                    
                    // Tính tổng tiền phòng và tiền dịch vụ
                    float tienPhong = 0;
                    float tienDichVu = 0;
                    int soNgay = 0;
                    string maPhong = "";
                    
                    // Load từ ChiTietHoaDon trong database
                    foreach (var ct in chiTietHoaDons)
                    {
                        dgvChiTiet.Rows.Add(
                            ct.STT,
                            ct.LoaiChiTiet,
                            ct.TenChiTiet,
                            ct.SoLuong,
                            ct.DonGia,
                            ct.ThanhTien
                        );
                        
                        // Tính tổng theo loại
                        if (ct.LoaiChiTiet == "Phong")
                        {
                            tienPhong += ct.ThanhTien;
                            soNgay = ct.SoLuong;
                            // Lấy mã phòng từ tên (ví dụ: "Phòng 301" -> "301")
                            if (ct.TenChiTiet.Contains("Phòng"))
                            {
                                string[] parts = ct.TenChiTiet.Split(' ');
                                if (parts.Length > 1)
                                {
                                    maPhong = parts[1];
                                }
                            }
                        }
                        else if (ct.LoaiChiTiet == "DichVu")
                        {
                            tienDichVu += ct.ThanhTien;
                        }
                    }
                    
                    // Hiển thị vào các textbox trong cardNhanPhong (Chi tiết hóa đơn)
                    if (cardNhanPhong != null)
                    {
                        // bunifuCustomTextbox6 - Mã Phòng
                        if (bunifuCustomTextbox6 != null && !string.IsNullOrEmpty(maPhong))
                        {
                            bunifuCustomTextbox6.Text = maPhong;
                        }
                        
                        // bunifuCustomTextbox9 - Số Ngày Thuê
                        if (bunifuCustomTextbox9 != null && soNgay > 0)
                        {
                            bunifuCustomTextbox9.Text = soNgay.ToString();
                        }
                        
                        // bunifuCustomTextbox10 - Tiền Phòng
                        if (bunifuCustomTextbox10 != null)
                        {
                            bunifuCustomTextbox10.Text = tienPhong.ToString("N0");
                        }
                        
                        // bunifuCustomTextbox3 - Tiền Dịch Vụ
                        if (bunifuCustomTextbox3 != null)
                        {
                            bunifuCustomTextbox3.Text = tienDichVu.ToString("N0");
                        }
                        
                        cardNhanPhong.Visible = true;
                    }
                    
                    bunifuCards1.Visible = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi load chi tiết hóa đơn: {ex.Message}", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private DataGridView GetOrCreateDataGridViewChiTiet()
        {
            DataGridView dgvChiTiet = null;
            if (bunifuCards1 != null)
            {
                // Tìm DataGridView trong bunifuCards1
                foreach (Control ctrl in bunifuCards1.Controls)
                {
                    if (ctrl is DataGridView && ctrl.Name == "dgvChiTietHoaDon")
                    {
                        dgvChiTiet = (DataGridView)ctrl;
                        break;
                    }
                }
                
                // Nếu chưa có, tạo mới
                if (dgvChiTiet == null)
                {
                    dgvChiTiet = new DataGridView();
                    dgvChiTiet.Name = "dgvChiTietHoaDon";
                    dgvChiTiet.ReadOnly = true;
                    dgvChiTiet.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                    dgvChiTiet.Location = new System.Drawing.Point(30, 100);
                    dgvChiTiet.Size = new System.Drawing.Size(650, 500);
                    dgvChiTiet.AllowUserToAddRows = false;
                    dgvChiTiet.AllowUserToDeleteRows = false;
                    dgvChiTiet.AllowUserToOrderColumns = false;
                    dgvChiTiet.BackgroundColor = System.Drawing.Color.White;
                    dgvChiTiet.Enabled = false; // Chỉ hiển thị, không cho chỉnh sửa
                    bunifuCards1.Controls.Add(dgvChiTiet);
                    dgvChiTiet.BringToFront();
                }
            }
            return dgvChiTiet;
        }

        private void SetupDataGridViewColumns(DataGridView dgv)
        {
            dgv.Columns.Clear();
            dgv.Columns.Add("STT", "STT");
            dgv.Columns.Add("Loai", "Loại");
            dgv.Columns.Add("Ten", "Tên");
            dgv.Columns.Add("SoLuong", "Số Lượng");
            dgv.Columns.Add("DonGia", "Đơn Giá");
            dgv.Columns.Add("ThanhTien", "Thành Tiền");
            
            dgv.Columns["DonGia"].DefaultCellStyle.Format = "N0";
            dgv.Columns["ThanhTien"].DefaultCellStyle.Format = "N0";
            dgv.Columns["STT"].Width = 50;
            dgv.Columns["Loai"].Width = 80;
        }

        // Hàm tự động sinh mã hóa đơn (không được nhập tay)
        // Mã chỉ được tối đa 10 ký tự theo database schema
        private string GenerateMaHoaDon(KhachSanContext context)
        {
            // Format: yyMMddHHmm (10 ký tự) - bỏ giây để giữ 10 ký tự
            string maHoaDon = DateTime.Now.ToString("yyMMddHHmm");
            
            // Đảm bảo không trùng
            int attempt = 0;
            while (context.HoaDons.Any(hd => hd.MaHoaDon == maHoaDon) && attempt < 100)
            {
                System.Threading.Thread.Sleep(100); // Đợi 100ms để đảm bảo phút khác
                maHoaDon = DateTime.Now.ToString("yyMMddHHmm");
                attempt++;
            }
            
            // Nếu vẫn trùng sau 100 lần thử, thêm số ngẫu nhiên (vẫn giữ 10 ký tự)
            if (attempt >= 100)
            {
                Random rnd = new Random();
                // Format: yyMMddHH + 2 số ngẫu nhiên = 10 ký tự
                maHoaDon = DateTime.Now.ToString("yyMMddHH") + rnd.Next(10, 99).ToString();
                
                // Đảm bảo không trùng
                while (context.HoaDons.Any(hd => hd.MaHoaDon == maHoaDon))
                {
                    maHoaDon = DateTime.Now.ToString("yyMMddHH") + rnd.Next(10, 99).ToString();
                }
            }
            
            return maHoaDon;
        }
        
        public void SetUserCMND(int cmnd)
        {
            currentCMND = cmnd;
        }

        private void UCHoaDon_Load(object sender, EventArgs e)
        {
            LoadPhongStandard();
            InitializeComboBoxPhieuThuePhong();
            LoadPhieuThuePhong();
            bunifuDatepicker4.Value = new DateTime(2025, 10, 2);
            bunifuDatepicker5.Value = new DateTime(2025, 10, 2);
            bunifuDatepicker6.Value = new DateTime(2025, 10, 2);
            
            // Disable các textbox mã (tự động sinh)
            if (bunifuCustomTextbox1 != null)
            {
                bunifuCustomTextbox1.ReadOnly = true;
                bunifuCustomTextbox1.BackColor = System.Drawing.SystemColors.Control;
            }
            if (bunifuCustomTextbox7 != null)
            {
                bunifuCustomTextbox7.ReadOnly = true;
                bunifuCustomTextbox7.BackColor = System.Drawing.SystemColors.Control;
            }
            
            // Thêm event handler để tự động load thông tin khi nhập CMND
            bunifuCustomTextbox5.TextChanged += BunifuCustomTextbox5_TextChanged;
            
            // Thêm event để reload khi form được focus lại (để cập nhật dịch vụ mới)
            this.Enter += UCHoaDon_Enter;
        }

        private void UCHoaDon_Enter(object sender, EventArgs e)
        {
            // Khi form được focus lại, reload tổng tiền nếu đã có mã phiếu thuê phòng
            if (!string.IsNullOrWhiteSpace(bunifuCustomTextbox7.Text))
            {
                try
                {
                    ReloadTongTienVaChiTiet(bunifuCustomTextbox7.Text);
                }
                catch
                {
                    // Ignore lỗi
                }
            }
        }

        private void BunifuCustomTextbox5_TextChanged(object sender, EventArgs e)
        {
            // Tự động load thông tin khi CMND thay đổi
            if (!string.IsNullOrWhiteSpace(bunifuCustomTextbox5.Text))
            {
                int cmnd;
                if (int.TryParse(bunifuCustomTextbox5.Text, out cmnd))
                {
                    // Tự động tính và điền tổng tiền
                    try
                    {
                        KhachSanContext context = new KhachSanContext();
                        var phieuThuePhong = context.PhieuThuePhongs
                            .Where(pt => pt.CMNDKhachHang == cmnd)
                            .OrderByDescending(pt => pt.NgayThue)
                            .FirstOrDefault();
                        
                        if (phieuThuePhong != null)
                        {
                            // Điền mã phiếu thuê phòng
                            bunifuCustomTextbox7.Text = phieuThuePhong.MaPhieuThuePhong;
                            
                            // Reload tổng tiền và chi tiết (bao gồm dịch vụ mới nhất)
                            // Load lại từ database để lấy dịch vụ mới nhất
                            var phieuThueUpdated = context.PhieuThuePhongs
                                .Include("Phong")
                                .Include("PhieuDichVus")
                                .Include("PhieuDichVus.DichVu")
                                .FirstOrDefault(pt => pt.MaPhieuThuePhong == phieuThuePhong.MaPhieuThuePhong);
                            
                            if (phieuThueUpdated != null)
                            {
                                ReloadTongTienVaChiTiet(phieuThueUpdated.MaPhieuThuePhong);
                            }
                        }
                    }
                    catch
                    {
                        // Bỏ qua lỗi khi load
                    }
                }
            }
        }

        private void LoadPhieuThuePhong()
        {
            try
            {
                KhachSanContext context = new KhachSanContext();
                var phieuThuePhongs = context.PhieuThuePhongs
                    .Include("KhachHang")
                    .Include("Phong")
                    .OrderByDescending(pt => pt.NgayThue)
                    .ToList();
                
                phieuThuePhongDict.Clear();
                List<string> items = new List<string>();
                items.Add("-- Chọn phiếu thuê phòng --");
                
                foreach (var ptp in phieuThuePhongs)
                {
                    string displayText = $"Mã: {ptp.MaPhieuThuePhong} - KH: {ptp.KhachHang.HoTen} (CMND: {ptp.CMNDKhachHang}) - Phòng: {ptp.MaPhong} - {ptp.NgayThue:dd/MM/yyyy}";
                    items.Add(displayText);
                    phieuThuePhongDict[displayText] = ptp;
                }
                
                // Load vào ComboBox
                if (comboBoxPhieuThuePhong != null)
                {
                    comboBoxPhieuThuePhong.Items.Clear();
                    comboBoxPhieuThuePhong.Items.AddRange(items.ToArray());
                    comboBoxPhieuThuePhong.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi load phiếu thuê phòng: {ex.Message}", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private float TinhTongTien(string maPhieuThuePhong)
        {
            try
            {
                KhachSanContext context = new KhachSanContext();
                var phieuThue = context.PhieuThuePhongs
                    .Include("Phong")
                    .Include("PhieuDichVus")
                    .Include("PhieuDichVus.DichVu")
                    .FirstOrDefault(pt => pt.MaPhieuThuePhong == maPhieuThuePhong);
                
                if (phieuThue == null) return 0;
                
                float tongTien = 0;
                
                // Tính tiền phòng (số ngày * đơn giá phòng)
                int soNgay = (phieuThue.NgayTra.Date - phieuThue.NgayThue.Date).Days + 1;
                if (soNgay < 1) soNgay = 1;
                tongTien += phieuThue.Phong.DonGia * soNgay;
                
                // Tính tiền dịch vụ
                if (phieuThue.PhieuDichVus != null)
                {
                    foreach (var pdv in phieuThue.PhieuDichVus)
                    {
                        tongTien += pdv.DichVu.DonGia * pdv.SoLuong;
                    }
                }
                
                return tongTien;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tính tổng tiền: {ex.Message}", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 0;
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
                bunifuDropdown3.Items = items.ToArray();
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
                bunifuDropdown3.Items = items.ToArray();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi load phòng: {ex.Message}", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
                LoadPhongVIP();
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked)
                LoadPhongStandard();
        }

        private void bunifuCustomLabel17_Click(object sender, EventArgs e)
        {

        }

        private void btnAccept_Click(object sender, EventArgs e)
        {
            try
            {
                // Validation - Kiểm tra đã chọn phiếu thuê phòng hoặc nhập CMND
                string maPhieuThuePhong = null;
                int cmnd = 0;
                
                // Ưu tiên lấy từ ComboBox nếu đã chọn
                PhieuThuePhong selectedPhieuThue = null;
                if (comboBoxPhieuThuePhong != null && comboBoxPhieuThuePhong.SelectedIndex > 0)
                {
                    string selectedText = comboBoxPhieuThuePhong.SelectedItem.ToString();
                    if (phieuThuePhongDict.ContainsKey(selectedText))
                    {
                        selectedPhieuThue = phieuThuePhongDict[selectedText];
                        maPhieuThuePhong = selectedPhieuThue.MaPhieuThuePhong;
                        cmnd = selectedPhieuThue.CMNDKhachHang;
                    }
                }
                
                // Nếu chưa chọn từ ComboBox, kiểm tra CMND từ textbox
                KhachSanContext context = null;
                if (selectedPhieuThue == null)
                {
                    if (string.IsNullOrWhiteSpace(bunifuCustomTextbox5.Text))
                    {
                        MessageBox.Show("Vui lòng chọn phiếu thuê phòng từ danh sách hoặc nhập CMND khách hàng!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Parse CMND
                    if (!int.TryParse(bunifuCustomTextbox5.Text, out cmnd))
                    {
                        MessageBox.Show("CMND phải là số!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Kiểm tra khách hàng có tồn tại
                    context = new KhachSanContext();
                    var khachHang = context.KhachHangs.FirstOrDefault(kh => kh.CMND == cmnd);
                    
                    if (khachHang == null)
                    {
                        MessageBox.Show($"Không tìm thấy khách hàng với CMND: {cmnd}!\nVui lòng tạo phiếu thuê phòng trước.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Tự động tìm phiếu thuê phòng mới nhất của khách hàng này
                    var phieuThuePhong = context.PhieuThuePhongs
                        .Where(pt => pt.CMNDKhachHang == cmnd)
                        .OrderByDescending(pt => pt.NgayThue)
                        .FirstOrDefault();
                    
                    if (phieuThuePhong == null)
                    {
                        MessageBox.Show($"Không tìm thấy phiếu thuê phòng cho khách hàng này!\nVui lòng tạo phiếu thuê phòng trước.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    selectedPhieuThue = phieuThuePhong;
                    maPhieuThuePhong = phieuThuePhong.MaPhieuThuePhong;
                }

                // LUÔN tự động tính lại tổng tiền từ phiếu thuê phòng (bao gồm dịch vụ mới nhất)
                // Đảm bảo tổng tiền luôn cập nhật khi có dịch vụ mới
                float tongTien = TinhTongTien(maPhieuThuePhong);
                bunifuCustomTextbox8.Text = tongTien.ToString("N0");

                // Đảm bảo context luôn được khởi tạo
                if (context == null)
                {
                    context = new KhachSanContext();
                }

                // Tạo mã hóa đơn - Tự động sinh (KHÔNG được nhập tay)
                // Mã hóa đơn luôn tự sinh, không lấy từ textbox
                string maHoaDon = GenerateMaHoaDon(context);

                // Tạo HoaDon
                HoaDon hoaDon = new HoaDon
                {
                    MaHoaDon = maHoaDon,
                    CMNDKhachHang = cmnd,
                    Tien = tongTien,
                    NgayXuat = bunifuDatepicker6.Value.Date,
                    HinhThucThanhToan = true // Mặc định true (Tiền mặt), có thể thêm checkbox sau
                };

                context.HoaDons.Add(hoaDon);

                // Lưu chi tiết hóa đơn
                var phieuThue = context.PhieuThuePhongs
                    .Include("Phong")
                    .Include("PhieuDichVus")
                    .Include("PhieuDichVus.DichVu")
                    .FirstOrDefault(pt => pt.MaPhieuThuePhong == maPhieuThuePhong);

                if (phieuThue == null)
                {
                    MessageBox.Show($"Không tìm thấy phiếu thuê phòng với mã: {maPhieuThuePhong}!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                // Kiểm tra xem đã có chi tiết hóa đơn cho mã này chưa (tránh trùng lặp)
                var existingChiTiet = context.ChiTietHoaDons
                    .Where(ct => ct.MaHoaDon == maHoaDon)
                    .ToList();
                
                if (existingChiTiet.Count > 0)
                {
                    // Xóa chi tiết cũ nếu có (cho phép cập nhật)
                    context.ChiTietHoaDons.RemoveRange(existingChiTiet);
                    context.SaveChanges();
                }
                
                int stt = 1;
                
                // Lưu chi tiết phòng
                if (phieuThue.Phong == null)
                {
                    MessageBox.Show("Phiếu thuê phòng không có thông tin phòng!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                int soNgay = (phieuThue.NgayTra.Date - phieuThue.NgayThue.Date).Days + 1;
                if (soNgay < 1) soNgay = 1;
                float thanhTienPhong = phieuThue.Phong.DonGia * soNgay;
                
                ChiTietHoaDon ctdPhong = new ChiTietHoaDon
                {
                    MaHoaDon = maHoaDon,
                    STT = stt++,
                    LoaiChiTiet = "Phong",
                    TenChiTiet = $"Phòng {phieuThue.MaPhong}",
                    SoLuong = soNgay,
                    DonGia = phieuThue.Phong.DonGia,
                    ThanhTien = thanhTienPhong
                };
                context.ChiTietHoaDons.Add(ctdPhong);
                
                // Lưu chi tiết dịch vụ
                if (phieuThue.PhieuDichVus != null && phieuThue.PhieuDichVus.Count > 0)
                {
                    foreach (var pdv in phieuThue.PhieuDichVus)
                    {
                        if (pdv.DichVu == null)
                        {
                            // Load lại DichVu nếu null
                            var dichVu = context.DichVus.FirstOrDefault(dv => dv.MaDichVu == pdv.MaDichVu);
                            if (dichVu == null)
                            {
                                continue; // Bỏ qua nếu không tìm thấy dịch vụ
                            }
                            
                            ChiTietHoaDon ctdDV = new ChiTietHoaDon
                            {
                                MaHoaDon = maHoaDon,
                                STT = stt++,
                                LoaiChiTiet = "DichVu",
                                TenChiTiet = dichVu.TenDichVu,
                                SoLuong = pdv.SoLuong,
                                DonGia = dichVu.DonGia,
                                ThanhTien = dichVu.DonGia * pdv.SoLuong
                            };
                            context.ChiTietHoaDons.Add(ctdDV);
                        }
                        else
                        {
                            float thanhTienDV = pdv.DichVu.DonGia * pdv.SoLuong;
                            ChiTietHoaDon ctdDV = new ChiTietHoaDon
                            {
                                MaHoaDon = maHoaDon,
                                STT = stt++,
                                LoaiChiTiet = "DichVu",
                                TenChiTiet = pdv.DichVu.TenDichVu,
                                SoLuong = pdv.SoLuong,
                                DonGia = pdv.DichVu.DonGia,
                                ThanhTien = thanhTienDV
                            };
                            context.ChiTietHoaDons.Add(ctdDV);
                        }
                    }
                }

                // Lưu tất cả thay đổi
                try
                {
                    context.SaveChanges();
                }
                catch (Exception saveEx)
                {
                    MessageBox.Show($"Lỗi khi lưu chi tiết hóa đơn: {saveEx.Message}\n\nChi tiết: {saveEx.InnerException?.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Cập nhật lại mã hóa đơn vào textbox
                bunifuCustomTextbox1.Text = maHoaDon;

                // Hiển thị chi tiết hóa đơn sau khi tạo thành công
                // Đợi một chút để đảm bảo dữ liệu đã được lưu vào database
                System.Threading.Thread.Sleep(100);
                LoadChiTietHoaDonByMaHoaDon(maHoaDon);

                MessageBox.Show($"Tạo hóa đơn thành công!\nMã HĐ: {maHoaDon}\nMã phiếu thuê: {maPhieuThuePhong}\nTổng tiền: {tongTien:N0} VNĐ", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                // KHÔNG clear form ngay, để người dùng xem chi tiết hóa đơn
                // ClearForm();
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
            bunifuCustomTextbox1.Text = "";
            bunifuCustomTextbox5.Text = "";
            bunifuCustomTextbox7.Text = "";
            bunifuCustomTextbox8.Text = "";
            bunifuDatepicker6.Value = new DateTime(2025, 10, 2);
            if (comboBoxPhieuThuePhong != null)
            {
                comboBoxPhieuThuePhong.SelectedIndex = 0;
            }
            
            // Xóa chi tiết hóa đơn
            if (bunifuCards1 != null)
            {
                foreach (Control ctrl in bunifuCards1.Controls)
                {
                    if (ctrl is DataGridView)
                    {
                        ((DataGridView)ctrl).Rows.Clear();
                        break;
                    }
                }
                bunifuCards1.Visible = false;
            }
        }
    }
}
