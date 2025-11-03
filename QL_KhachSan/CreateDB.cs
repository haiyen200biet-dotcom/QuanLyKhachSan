using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using DTO;

namespace QL_KhachSan
{
    public class CreateDB : CreateDatabaseIfNotExists<KhachSanContext>
    {
        // Hàm mã hóa mật khẩu MD5
        private string MaHoa(string pass)
        {
            MD5 mh = MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(pass);
            byte[] hash = mh.ComputeHash(inputBytes);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }

        protected override void Seed(KhachSanContext context)
        {
            // Seed data mẫu và tài khoản đăng nhập
            // Chỉ thêm dữ liệu nếu chưa tồn tại để tránh trùng lặp
            
            // Kiểm tra và thêm User mẫu cho Admin nếu chưa có
            if (!context.Users.Any(u => u.CMND == 123456789))
            {
                context.Users.Add(new User
                {
                    CMND = 123456789,
                    HoTen = "Nguyễn Văn Admin",
                    DiaChi = "10 Đường Nguyễn Trãi, Quận 5, TP.HCM",
                    GioiTinh = true,
                    SDT = "0934567890",
                    NgaySinh = new DateTime(1990, 1, 15)
                });
            }

            // Kiểm tra và thêm User mẫu cho Lễ Tân nếu chưa có
            if (!context.Users.Any(u => u.CMND == 987654321))
            {
                context.Users.Add(new User
                {
                    CMND = 987654321,
                    HoTen = "Phạm Thị Lễ Tân",
                    DiaChi = "20 Đường Trần Hưng Đạo, Quận 1, TP.HCM",
                    GioiTinh = false,
                    SDT = "0945678901",
                    NgaySinh = new DateTime(1995, 6, 25)
                });
            }

            // Lưu Users trước để có thể reference
            context.SaveChanges();

            // Kiểm tra và thêm Account Admin: admin|123456
            if (!context.Accounts.Any(a => a.TenDangNhap == "admin"))
            {
                context.Accounts.Add(new Account
                {
                    STT = 1,
                    CMND = 123456789,
                    TenDangNhap = "admin",
                    MatKhau = MaHoa("123456"),
                    ChucVu = "Admin"
                });
            }

            // Kiểm tra và thêm Account Lễ Tân: letan1|123456
            if (!context.Accounts.Any(a => a.TenDangNhap == "letan1"))
            {
                context.Accounts.Add(new Account
                {
                    STT = 2,
                    CMND = 987654321,
                    TenDangNhap = "letan1",
                    MatKhau = MaHoa("123456"),
                    ChucVu = "Lễ Tân"
                });
            }

            // Kiểm tra và thêm Dịch vụ mẫu nếu chưa có
            if (!context.DichVus.Any(dv => dv.MaDichVu == 1))
            {
                context.DichVus.Add(new DichVu { MaDichVu = 1, TenDichVu = "Giặt ủi", DonGia = 50000 });
            }
            if (!context.DichVus.Any(dv => dv.MaDichVu == 2))
            {
                context.DichVus.Add(new DichVu { MaDichVu = 2, TenDichVu = "Massage", DonGia = 300000 });
            }
            if (!context.DichVus.Any(dv => dv.MaDichVu == 3))
            {
                context.DichVus.Add(new DichVu { MaDichVu = 3, TenDichVu = "Internet", DonGia = 20000 });
            }
            if (!context.DichVus.Any(dv => dv.MaDichVu == 4))
            {
                context.DichVus.Add(new DichVu { MaDichVu = 4, TenDichVu = "Bữa sáng", DonGia = 100000 });
            }
            if (!context.DichVus.Any(dv => dv.MaDichVu == 5))
            {
                context.DichVus.Add(new DichVu { MaDichVu = 5, TenDichVu = "Thức ăn", DonGia = 150000 });
            }
            if (!context.DichVus.Any(dv => dv.MaDichVu == 6))
            {
                context.DichVus.Add(new DichVu { MaDichVu = 6, TenDichVu = "Xe đưa đón", DonGia = 200000 });
            }

            // Kiểm tra và thêm Phòng mẫu nếu chưa có
            if (!context.Phongs.Any(p => p.MaPhong == 101))
            {
                context.Phongs.Add(new Phong { MaPhong = 101, LoaiPhong = "Standard", DonGia = 500000, SoNguoiToiDa = 2 });
            }
            if (!context.Phongs.Any(p => p.MaPhong == 102))
            {
                context.Phongs.Add(new Phong { MaPhong = 102, LoaiPhong = "Standard", DonGia = 500000, SoNguoiToiDa = 2 });
            }
            if (!context.Phongs.Any(p => p.MaPhong == 201))
            {
                context.Phongs.Add(new Phong { MaPhong = 201, LoaiPhong = "Deluxe", DonGia = 800000, SoNguoiToiDa = 3 });
            }
            if (!context.Phongs.Any(p => p.MaPhong == 202))
            {
                context.Phongs.Add(new Phong { MaPhong = 202, LoaiPhong = "Deluxe", DonGia = 800000, SoNguoiToiDa = 3 });
            }
            if (!context.Phongs.Any(p => p.MaPhong == 301))
            {
                context.Phongs.Add(new Phong { MaPhong = 301, LoaiPhong = "VIP", DonGia = 1500000, SoNguoiToiDa = 4 });
            }
            if (!context.Phongs.Any(p => p.MaPhong == 302))
            {
                context.Phongs.Add(new Phong { MaPhong = 302, LoaiPhong = "VIP", DonGia = 1500000, SoNguoiToiDa = 4 });
            }

            // Lưu tất cả thay đổi
            context.SaveChanges();
        }
    }
}
