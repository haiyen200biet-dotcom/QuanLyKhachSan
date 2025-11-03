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
    
    public partial class UCPhieuDV : UserControl
    {
        private static UCPhieuDV _Instance;
        public static UCPhieuDV Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new UCPhieuDV();
                }
                return _Instance;
            }
        }
        
        private Dictionary<string, PhieuThuePhong> phieuThuePhongDict; // Lưu mapping giữa display text và PhieuThuePhong
        private ComboBox comboBoxPhieuThuePhong; // Dropdown để chọn phiếu thuê phòng
        private DataGridView dgvDanhSachDichVu; // Hiển thị danh sách dịch vụ đã chọn
        private string currentMaPhieuThuePhong; // Lưu mã phiếu thuê phòng hiện tại
        
        public UCPhieuDV()
        {
            InitializeComponent();
            phieuThuePhongDict = new Dictionary<string, PhieuThuePhong>();
            this.Load += UCPhieuDV_Load;
            LoadDichVu();
        }

        private void InitializeComboBoxPhieuThuePhong()
        {
            // Tạo ComboBox để chọn phiếu thuê phòng
            comboBoxPhieuThuePhong = new ComboBox();
            comboBoxPhieuThuePhong.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxPhieuThuePhong.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F);
            comboBoxPhieuThuePhong.Width = 294;
            comboBoxPhieuThuePhong.Height = 35;
            
            // Đặt vị trí gần textbox mã phiếu thuê (thấp hơn)
            if (cardNhanPhong != null)
            {
                comboBoxPhieuThuePhong.Location = new System.Drawing.Point(
                    bunifuCustomTextbox6.Location.X,
                    bunifuCustomTextbox6.Location.Y - 40  // Thấp hơn, cách 40px
                );
                comboBoxPhieuThuePhong.Anchor = bunifuCustomTextbox6.Anchor;
                cardNhanPhong.Controls.Add(comboBoxPhieuThuePhong);
                comboBoxPhieuThuePhong.BringToFront();
                comboBoxPhieuThuePhong.TabIndex = bunifuCustomTextbox6.TabIndex - 1;
                
                // Thêm label
                Label lblPhieuThue = new Label();
                lblPhieuThue.Text = "Chọn Phiếu Thuê Phòng";
                lblPhieuThue.Font = new System.Drawing.Font("Comic Sans MS", 11F, System.Drawing.FontStyle.Bold);
                lblPhieuThue.ForeColor = System.Drawing.SystemColors.GrayText;
                lblPhieuThue.Location = new System.Drawing.Point(
                    bunifuCustomLabel8.Location.X,
                    comboBoxPhieuThuePhong.Location.Y + 5
                );
                lblPhieuThue.AutoSize = true;
                cardNhanPhong.Controls.Add(lblPhieuThue);
                lblPhieuThue.BringToFront();
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
                    
                    // Load lại từ database để đảm bảo có đầy đủ thông tin (Phong, KhachHang)
                    try
                    {
                        KhachSanContext context = new KhachSanContext();
                        var phieuThueFull = context.PhieuThuePhongs
                            .Include("Phong")
                            .Include("KhachHang")
                            .FirstOrDefault(pt => pt.MaPhieuThuePhong == phieuThue.MaPhieuThuePhong);
                        
                        if (phieuThueFull != null)
                        {
                            phieuThue = phieuThueFull;
                            // Cập nhật lại dictionary
                            phieuThuePhongDict[selectedText] = phieuThue;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Lỗi khi load thông tin phiếu thuê phòng: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    
                    // Lưu mã phiếu thuê phòng hiện tại
                    currentMaPhieuThuePhong = phieuThue.MaPhieuThuePhong;
                    
                    // Tự động điền mã phiếu thuê phòng (readonly)
                    bunifuCustomTextbox6.Text = phieuThue.MaPhieuThuePhong;
                    
                    // Hiển thị thông tin phòng và các thông tin liên quan
                    LoadThongTinPhongVaThongTinLienQuan(phieuThue);
                    
                    // Load và hiển thị danh sách dịch vụ đã chọn cho phiếu thuê phòng này
                    LoadDanhSachDichVu(phieuThue.MaPhieuThuePhong);
                }
            }
        }

        // Hàm load và hiển thị thông tin phòng và các thông tin liên quan
        private void LoadThongTinPhongVaThongTinLienQuan(PhieuThuePhong phieuThue)
        {
            try
            {
                if (phieuThue == null || phieuThue.Phong == null)
                {
                    return;
                }
                
                // Lấy thông tin phòng
                var phong = phieuThue.Phong;
                
                // Xác định loại phòng và load phòng tương ứng
                if (phong.LoaiPhong == "VIP")
                {
                    radioButton3.Checked = true;
                    LoadPhongVIP();
                }
                else if (phong.LoaiPhong == "Standard" || phong.LoaiPhong == "Deluxe")
                {
                    radioButton4.Checked = true;
                    LoadPhongStandard();
                }
                
                // Tự động chọn phòng đã đặt trong dropdown
                if (bunifuDropdown1 != null && bunifuDropdown1.Items != null)
                {
                    // Tìm phòng trong danh sách và chọn
                    // BunifuDropdown.Items trả về string[] (array), nên dùng Length thay vì Count
                    string[] items = bunifuDropdown1.Items as string[];
                    if (items != null && items.Length > 0)
                    {
                        for (int i = 0; i < items.Length; i++)
                        {
                            string itemText = items[i];
                            if (itemText != null && itemText.Contains($"Phòng {phong.MaPhong}"))
                            {
                                bunifuDropdown1.selectedIndex = i;
                                break;
                            }
                        }
                    }
                }
                
                // Hiển thị ngày (nếu có bunifuDatepicker1)
                if (bunifuDatepicker1 != null && phieuThue.NgayThue != null)
                {
                    bunifuDatepicker1.Value = phieuThue.NgayThue;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi load thông tin phòng: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void UCPhieuDV_Load(object sender, EventArgs e)
        {
            bunifuDatepicker1.Value = new DateTime(2025, 10, 2);
            InitializeComboBoxPhieuThuePhong();
            InitializeDataGridViewDichVu();
            LoadPhieuThuePhong();
        }

        private void InitializeDataGridViewDichVu()
        {
            // Tạo DataGridView để hiển thị danh sách dịch vụ đã chọn
            dgvDanhSachDichVu = new DataGridView();
            dgvDanhSachDichVu.Name = "dgvDanhSachDichVu";
            dgvDanhSachDichVu.ReadOnly = true;
            dgvDanhSachDichVu.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvDanhSachDichVu.AllowUserToAddRows = false;
            dgvDanhSachDichVu.AllowUserToDeleteRows = false;
            dgvDanhSachDichVu.BackgroundColor = System.Drawing.Color.White;
            
            // Setup columns
            dgvDanhSachDichVu.Columns.Add("STT", "STT");
            dgvDanhSachDichVu.Columns.Add("TenDichVu", "Tên Dịch Vụ");
            dgvDanhSachDichVu.Columns.Add("DonGia", "Đơn Giá");
            dgvDanhSachDichVu.Columns.Add("SoLuong", "Số Lượng");
            dgvDanhSachDichVu.Columns.Add("ThanhTien", "Thành Tiền");
            
            dgvDanhSachDichVu.Columns["DonGia"].DefaultCellStyle.Format = "N0";
            dgvDanhSachDichVu.Columns["ThanhTien"].DefaultCellStyle.Format = "N0";
            dgvDanhSachDichVu.Columns["STT"].Width = 50;
            
            // Đặt vị trí trong groupBox1
            if (groupBox1 != null)
            {
                dgvDanhSachDichVu.Location = new System.Drawing.Point(30, 180);
                dgvDanhSachDichVu.Size = new System.Drawing.Size(850, 400);
                groupBox1.Controls.Add(dgvDanhSachDichVu);
                dgvDanhSachDichVu.BringToFront();
            }
        }
        
        private void LoadDanhSachDichVu(string maPhieuThuePhong)
        {
            try
            {
                if (dgvDanhSachDichVu == null) return;
                
                if (string.IsNullOrEmpty(maPhieuThuePhong))
                {
                    dgvDanhSachDichVu.Rows.Clear();
                    return;
                }
                
                KhachSanContext context = new KhachSanContext();
                
                // Load PhieuDichVu với Include DichVu để tránh lỗi null
                var phieuDichVus = context.PhieuDichVus
                    .Include("DichVu")
                    .Where(pdv => pdv.MaPhieuThuePhong == maPhieuThuePhong)
                    .ToList();
                
                // Xóa dữ liệu cũ
                dgvDanhSachDichVu.Rows.Clear();
                
                // Kiểm tra nếu không có dịch vụ nào
                if (phieuDichVus == null || phieuDichVus.Count == 0)
                {
                    return;
                }
                
                // Load danh sách dịch vụ đã chọn
                int stt = 1;
                foreach (var pdv in phieuDichVus)
                {
                    // Kiểm tra null cho DichVu
                    if (pdv.DichVu == null)
                    {
                        // Load lại DichVu nếu bị null
                        var dichVu = context.DichVus.FirstOrDefault(dv => dv.MaDichVu == pdv.MaDichVu);
                        if (dichVu == null)
                        {
                            continue; // Bỏ qua nếu không tìm thấy dịch vụ
                        }
                        
                        dgvDanhSachDichVu.Rows.Add(
                            stt++,
                            dichVu.TenDichVu ?? "N/A",
                            dichVu.DonGia,
                            pdv.SoLuong,
                            dichVu.DonGia * pdv.SoLuong
                        );
                    }
                    else
                    {
                        float thanhTien = pdv.DichVu.DonGia * pdv.SoLuong;
                        dgvDanhSachDichVu.Rows.Add(
                            stt++,
                            pdv.DichVu.TenDichVu ?? "N/A",
                            pdv.DichVu.DonGia,
                            pdv.SoLuong,
                            thanhTien
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi load danh sách dịch vụ: {ex.Message}\n\nChi tiết: {ex.InnerException?.Message}", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    string displayText = $"Mã: {ptp.MaPhieuThuePhong} - KH: {ptp.KhachHang.HoTen} - Phòng: {ptp.MaPhong} - {ptp.NgayThue:dd/MM/yyyy}";
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

        private void LoadDichVu()
        {
            try
            {
                KhachSanContext context = new KhachSanContext();
                var dichVus = context.DichVus.ToList();
                
                // Tạo danh sách string từ dịch vụ
                List<string> items = new List<string>();
                foreach (var dv in dichVus)
                {
                    items.Add($"{dv.TenDichVu} - {dv.DonGia:N0} VNĐ");
                }
                
                // Gán array vào dropdown
                bunifuDropdown3.Items = items.ToArray();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi load dịch vụ: {ex.Message}", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi load phòng: {ex.Message}", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

        private void btnAccept_Click(object sender, EventArgs e)
        {
            try
            {
                // Validation - Kiểm tra đã chọn phiếu thuê phòng
                string maPhieuThue = null;
                
                // Ưu tiên lấy từ ComboBox nếu đã chọn
                PhieuThuePhong selectedPhieuThue = null;
                if (comboBoxPhieuThuePhong != null && comboBoxPhieuThuePhong.SelectedIndex > 0)
                {
                    string selectedText = comboBoxPhieuThuePhong.SelectedItem.ToString();
                    if (phieuThuePhongDict.ContainsKey(selectedText))
                    {
                        selectedPhieuThue = phieuThuePhongDict[selectedText];
                        maPhieuThue = selectedPhieuThue.MaPhieuThuePhong;
                    }
                }
                
                // Nếu chưa chọn từ ComboBox, kiểm tra từ textbox
                if (string.IsNullOrEmpty(maPhieuThue))
                {
                    if (string.IsNullOrWhiteSpace(bunifuCustomTextbox6.Text))
                    {
                        MessageBox.Show("Vui lòng chọn phiếu thuê phòng từ danh sách hoặc nhập Mã Phiếu Thuê!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    
                    maPhieuThue = bunifuCustomTextbox6.Text.Trim();
                }

                if (bunifuDropdown3.selectedIndex < 0)
                {
                    MessageBox.Show("Vui lòng chọn dịch vụ!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Kiểm tra độ dài mã phiếu thuê
                if (maPhieuThue.Length > 10)
                {
                    MessageBox.Show("Mã phiếu thuê tối đa 10 ký tự!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Kiểm tra mã phiếu thuê có tồn tại (nếu chưa lấy từ ComboBox)
                KhachSanContext context = new KhachSanContext();
                if (selectedPhieuThue == null)
                {
                    selectedPhieuThue = context.PhieuThuePhongs
                        .Include("Phong")
                        .FirstOrDefault(pt => pt.MaPhieuThuePhong == maPhieuThue);
                    
                    if (selectedPhieuThue == null)
                    {
                        MessageBox.Show($"Không tìm thấy phiếu thuê với mã: {maPhieuThue}!\nVui lòng kiểm tra lại mã phiếu hoặc tạo phiếu thuê phòng trước.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
                
                var phieuThue = selectedPhieuThue;

                // Không cần chọn phòng nữa vì đã có trong phiếu thuê phòng
                // Chỉ cần kiểm tra phiếu thuê phòng có tồn tại và có phòng
                if (phieuThue.Phong == null)
                {
                    MessageBox.Show("Phiếu thuê phòng không có thông tin phòng!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string selectedDichVu = bunifuDropdown3.selectedValue;

                // Parse Mã Dịch Vụ từ dropdown (format: "Tên - Giá VNĐ")
                var allDichVus = context.DichVus.ToList();
                int maDichVu = allDichVus.Where(dv => selectedDichVu.Contains(dv.TenDichVu)).First().MaDichVu;

                // Tạo mã phiếu dịch vụ
                string maPhieuDV = DateTime.Now.ToString("yyMMddHHmm");

                // Tạo PhieuDichVu
                PhieuDichVu phieuDV = new PhieuDichVu
                {
                    MaPhieuDichVu = maPhieuDV,
                    MaPhieuThuePhong = maPhieuThue,
                    MaDichVu = maDichVu,
                    SoLuong = 1 // Mặc định 1, có thể thêm control sau
                };

                context.PhieuDichVus.Add(phieuDV);
                context.SaveChanges();

                // Reload danh sách dịch vụ sau khi thêm mới
                if (!string.IsNullOrEmpty(currentMaPhieuThuePhong))
                {
                    LoadDanhSachDichVu(currentMaPhieuThuePhong);
                }

                MessageBox.Show($"Thêm dịch vụ thành công!\nMã phiếu DV: {maPhieuDV}\nDịch vụ: {selectedDichVu}", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                // Chỉ clear phần chọn dịch vụ, giữ lại phiếu thuê phòng và danh sách
                bunifuDropdown3.selectedIndex = -1;
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
            bunifuCustomTextbox6.Text = "";
            bunifuDropdown3.selectedIndex = -1;
            bunifuDatepicker1.Value = new DateTime(2025, 10, 2);
            if (comboBoxPhieuThuePhong != null)
            {
                comboBoxPhieuThuePhong.SelectedIndex = 0;
            }
            
            // Xóa danh sách dịch vụ
            if (dgvDanhSachDichVu != null)
            {
                dgvDanhSachDichVu.Rows.Clear();
            }
            
            currentMaPhieuThuePhong = null;
        }
    }
}
