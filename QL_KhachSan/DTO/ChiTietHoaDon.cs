using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTO
{
    [Table("ChiTietHoaDon")]
    public class ChiTietHoaDon
    {
        [Key]
        [Column(Order = 0)]
        [StringLength(10)]
        public string MaHoaDon { get; set; }

        [Key]
        [Column(Order = 1)]
        public int STT { get; set; }

        [StringLength(20)]
        public string LoaiChiTiet { get; set; } // "Phong" hoặc "DichVu"

        [StringLength(50)]
        public string TenChiTiet { get; set; } // Tên phòng hoặc tên dịch vụ

        public int SoLuong { get; set; }

        public float DonGia { get; set; }

        public float ThanhTien { get; set; }

        [ForeignKey("MaHoaDon")]
        public virtual HoaDon HoaDon { get; set; }
    }
}

