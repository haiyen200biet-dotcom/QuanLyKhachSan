using Microsoft.VisualBasic.ApplicationServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.Entity;

namespace QL_KhachSan
{
    static class Program
    {

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // Đảm bảo database được tạo và khởi tạo khi ứng dụng khởi động
            try
            {
                Database.SetInitializer<KhachSanContext>(new CreateDB());
                
                // Tạo context và force initialization
                using (var context = new KhachSanContext())
                {
                    // Kiểm tra xem database có tồn tại không
                    bool exists = context.Database.Exists();
                    
                    if (!exists)
                    {
                        // Tạo database và tất cả các bảng nếu chưa tồn tại
                        context.Database.Create();
                    }
                    else
                    {
                        // Nếu database đã tồn tại, kiểm tra xem có bảng ChiTietHoaDon chưa
                        // Nếu chưa có, tạo lại database (hoặc có thể dùng migration)
                        try
                        {
                            // Thử truy vấn một bảng để kiểm tra schema
                            var test = context.ChiTietHoaDons.Take(1).ToList();
                        }
                        catch
                        {
                            // Nếu bảng không tồn tại, tạo lại database
                            // LƯU Ý: Điều này sẽ XÓA tất cả dữ liệu cũ
                            context.Database.Delete();
                            context.Database.Create();
                        }
                        
                        // Đảm bảo database được initialize
                        context.Database.Initialize(true);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi khởi tạo database: {ex.Message}\n\nChi tiết: {ex.InnerException?.Message}\n\nVui lòng kiểm tra lại:\n1. SQL Server đã được cài đặt và đang chạy\n2. Server name trong App.config đúng (LAPTOP-CJENKQO3)\n3. Connection string trong App.config\n4. Quyền truy cập database\n5. Database KhachSanPro có thể chưa tồn tại", 
                    "Lỗi Database", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // Vẫn cho phép chạy ứng dụng, nhưng database có thể không hoạt động
            }
            
            Application.Run(new frmLogin());
        }


    }
}