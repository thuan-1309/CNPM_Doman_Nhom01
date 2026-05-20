// ===============================
// PhieuMuonController.cs (FULL)
// ===============================

using CNPM_Doman_Nhom01.Models; // Giữ nguyên namespace Models của nhóm
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace CNPM_Doman_Nhom01.Controllers
{
    public class PhieuMuonController : Controller
    {
        private readonly string _connectionString;

        public PhieuMuonController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Không tìm thấy chuỗi kết nối.");
        }

        // ==========================================
        // 1. GIAO DIỆN LẬP PHIẾU
        // ==========================================
        public IActionResult LapPhieu()
        {
            var listDocGia = new List<dynamic>();
            var listSach = new List<dynamic>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                // ĐỘC GIẢ
                SqlCommand cmdDG = new SqlCommand("SELECT MaDocGia, TenDocGia, SoDienThoai, DiaChi FROM DocGia", conn);
                using (var reader = cmdDG.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        listDocGia.Add(new
                        {
                            MaDocGia = reader["MaDocGia"].ToString(),
                            TenDocGia = reader["TenDocGia"].ToString(),
                            SoDienThoai = reader["SoDienThoai"].ToString(),
                            DiaChi = reader["DiaChi"].ToString()
                        });
                    }
                }

                // SÁCH
                SqlCommand cmdSach = new SqlCommand("SELECT MaSach, TenSach, TacGia, SoLuongTon FROM Sach WHERE SoLuongTon > 0", conn);
                using (var reader = cmdSach.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        listSach.Add(new
                        {
                            MaSach = reader["MaSach"].ToString(),
                            TenSach = reader["TenSach"].ToString(),
                            TacGia = reader["TacGia"].ToString(),
                            SoLuongTon = Convert.ToInt32(reader["SoLuongTon"])
                        });
                    }
                }
            }

            ViewBag.DocGias = listDocGia;
            ViewBag.Sachs = listSach;
            return View();
        }

        // ==========================================
        // 2. LƯU PHIẾU MƯỢN
        // ==========================================
        [HttpPost]
        public IActionResult LuuPhieuMuon([FromBody] PhieuMuonRequest request)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                SqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    string maPhieu = "PM" + DateTime.Now.ToString("HHmmss");

                    string sqlPhieu = @"
                        INSERT INTO PhieuMuon (MaPhieuMuon, MaDocGia, NgayMuon, HanTra, ThuThu, GhiChu, TrangThai)
                        VALUES (@Ma, @DG, @Ngay, @Han, 'admin', @GC, N'Đang mượn')";

                    SqlCommand cmdPhieu = new SqlCommand(sqlPhieu, conn, transaction);
                    cmdPhieu.Parameters.AddWithValue("@Ma", maPhieu);
                    cmdPhieu.Parameters.AddWithValue("@DG", request.MaDocGia);
                    cmdPhieu.Parameters.AddWithValue("@Ngay", request.NgayMuon);
                    cmdPhieu.Parameters.AddWithValue("@Han", request.HanTra);
                    cmdPhieu.Parameters.AddWithValue("@GC", string.IsNullOrEmpty(request.GhiChu) ? DBNull.Value : request.GhiChu);
                    cmdPhieu.ExecuteNonQuery();

                    foreach (var sach in request.DanhSachSach)
                    {
                        string sqlCT = @"
                            INSERT INTO ChiTietPhieuMuon (MaPhieuMuon, MaSach, SoLuong, TinhTrangTra)
                            VALUES (@MaP, @MaS, @SL, N'Chưa trả')";
                        SqlCommand cmdCT = new SqlCommand(sqlCT, conn, transaction);
                        cmdCT.Parameters.AddWithValue("@MaP", maPhieu);
                        cmdCT.Parameters.AddWithValue("@MaS", sach.MaSach);
                        cmdCT.Parameters.AddWithValue("@SL", sach.SoLuong);
                        cmdCT.ExecuteNonQuery();

                        string sqlUpdate = "UPDATE Sach SET SoLuongTon = SoLuongTon - @SL WHERE MaSach = @MaS";
                        SqlCommand cmdUpdate = new SqlCommand(sqlUpdate, conn, transaction);
                        cmdUpdate.Parameters.AddWithValue("@SL", sach.SoLuong);
                        cmdUpdate.Parameters.AddWithValue("@MaS", sach.MaSach);
                        cmdUpdate.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    return Json(new { success = true, message = $"Lập phiếu {maPhieu} thành công!" });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = ex.Message });
                }
            }
        }

        // ==========================================
        // 3. GIAO DIỆN TRẢ SÁCH
        // ==========================================
        public IActionResult TraSach()
        {
            return View();
        }

        // ==========================================
        // 4. TÌM KIẾM PHIẾU
        // ==========================================
        [HttpGet]
        public IActionResult TimKiemPhieu(string maPhieu)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                SqlCommand cmdInfo = new SqlCommand(@"
                    SELECT p.MaPhieuMuon, d.TenDocGia, p.NgayMuon, p.HanTra
                    FROM PhieuMuon p JOIN DocGia d ON p.MaDocGia = d.MaDocGia
                    WHERE p.MaPhieuMuon = @MaP", conn);
                cmdInfo.Parameters.AddWithValue("@MaP", maPhieu);

                var info = new Dictionary<string, string>();
                using (var reader = cmdInfo.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        info.Add("TenDocGia", reader["TenDocGia"].ToString());
                        info.Add("NgayMuon", Convert.ToDateTime(reader["NgayMuon"]).ToString("dd/MM/yyyy"));
                        info.Add("HanTra", Convert.ToDateTime(reader["HanTra"]).ToString("dd/MM/yyyy"));
                    }
                    else
                    {
                        return Json(new { success = false, message = "Không tìm thấy phiếu mượn!" });
                    }
                }

                var dsSach = new List<dynamic>();
                SqlCommand cmdSach = new SqlCommand(@"
                    SELECT c.MaSach, s.TenSach, p.NgayMuon, p.HanTra, c.TinhTrangTra
                    FROM ChiTietPhieuMuon c
                    JOIN Sach s ON c.MaSach = s.MaSach
                    JOIN PhieuMuon p ON c.MaPhieuMuon = p.MaPhieuMuon
                    WHERE c.MaPhieuMuon = @MaP", conn);
                cmdSach.Parameters.AddWithValue("@MaP", maPhieu);

                using (var reader = cmdSach.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        DateTime hanTra = Convert.ToDateTime(reader["HanTra"]);
                        DateTime ngayHienTai = DateTime.Now;

                        int soNgayTre = (ngayHienTai - hanTra).Days;
                        if (soNgayTre < 0) soNgayTre = 0;
                        decimal tienPhat = soNgayTre * 5000;

                        dsSach.Add(new
                        {
                            MaSach = reader["MaSach"].ToString(),
                            TenSach = reader["TenSach"].ToString(),
                            NgayMuon = Convert.ToDateTime(reader["NgayMuon"]).ToString("dd/MM/yyyy"),
                            HanTra = hanTra.ToString("dd/MM/yyyy"),
                            TinhTrangTra = reader["TinhTrangTra"].ToString(),
                            SoNgayTre = soNgayTre, // Đã bổ sung đẩy ngày trễ ra UI
                            TienPhat = tienPhat    // Đã bổ sung đẩy tiền phạt ra UI
                        });
                    }
                }

                return Json(new { success = true, info, dsSach });
            }
        }

        // ==========================================
        // 5. XÁC NHẬN TRẢ SÁCH + PHẠT
        // ==========================================
        [HttpPost]
        public IActionResult XacNhanTra([FromBody] TraSachRequest request)
        {
            if (request.MaSachTra == null || request.MaSachTra.Count == 0)
            {
                return Json(new { success = false, message = "Chưa chọn sách trả!" });
            }

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                SqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    SqlCommand cmdHanTra = new SqlCommand("SELECT HanTra FROM PhieuMuon WHERE MaPhieuMuon = @MaP", conn, transaction);
                    cmdHanTra.Parameters.AddWithValue("@MaP", request.MaPhieuMuon);
                    DateTime hanTra = Convert.ToDateTime(cmdHanTra.ExecuteScalar());
                    DateTime ngayTra = DateTime.Now;

                    int soNgayTre = (ngayTra - hanTra).Days;
                    if (soNgayTre < 0) soNgayTre = 0;

                    // Phạt = Số ngày trễ * 5.000đ * Tổng số lượng cuốn sách được chọn trả
                    decimal tienPhat = soNgayTre * 5000 * request.MaSachTra.Count;

                    foreach (var maSach in request.MaSachTra)
                    {
                        SqlCommand cmdTra = new SqlCommand(@"
                            UPDATE ChiTietPhieuMuon SET TinhTrangTra = N'Đã trả'
                            WHERE MaPhieuMuon = @MaP AND MaSach = @MaS", conn, transaction);
                        cmdTra.Parameters.AddWithValue("@MaP", request.MaPhieuMuon);
                        cmdTra.Parameters.AddWithValue("@MaS", maSach);
                        cmdTra.ExecuteNonQuery();

                        SqlCommand cmdSL = new SqlCommand(@"
                            SELECT SoLuong FROM ChiTietPhieuMuon
                            WHERE MaPhieuMuon = @MaP AND MaSach = @MaS", conn, transaction);
                        cmdSL.Parameters.AddWithValue("@MaP", request.MaPhieuMuon);
                        cmdSL.Parameters.AddWithValue("@MaS", maSach);
                        int soLuong = Convert.ToInt32(cmdSL.ExecuteScalar());

                        SqlCommand cmdCong = new SqlCommand("UPDATE Sach SET SoLuongTon = SoLuongTon + @SL WHERE MaSach = @MaS", conn, transaction);
                        cmdCong.Parameters.AddWithValue("@SL", soLuong);
                        cmdCong.Parameters.AddWithValue("@MaS", maSach);
                        cmdCong.ExecuteNonQuery();
                    }

                    SqlCommand cmdCheck = new SqlCommand(@"
                        SELECT COUNT(*) FROM ChiTietPhieuMuon
                        WHERE MaPhieuMuon = @MaP AND TinhTrangTra != N'Đã trả'", conn, transaction);
                    cmdCheck.Parameters.AddWithValue("@MaP", request.MaPhieuMuon);
                    int remaining = Convert.ToInt32(cmdCheck.ExecuteScalar());

                    if (remaining == 0)
                    {
                        // Thêm chi tiết phạt vào Ghi chú nếu có phạt
                        string ghiChuThem = (soNgayTre > 0) ? $" | Trễ: {soNgayTre} ngày, Phạt: {tienPhat} VNĐ" : "";

                        SqlCommand cmdUpdate = new SqlCommand(@"
                            UPDATE PhieuMuon SET TrangThai = N'Đã trả', GhiChu = ISNULL(GhiChu,'') + @GC
                            WHERE MaPhieuMuon = @MaP", conn, transaction);
                        cmdUpdate.Parameters.AddWithValue("@MaP", request.MaPhieuMuon);
                        cmdUpdate.Parameters.AddWithValue("@GC", ghiChuThem);
                        cmdUpdate.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    return Json(new { success = true, message = "Trả sách thành công!", soNgayTre, tienPhat });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = ex.Message });
                }
            }
        }
    }

    // ==========================================
    // CÁC CLASS MODEL DÙNG ĐỂ HỨNG DATA TỪ VIEW
    // ==========================================
    public class PhieuMuonRequest
    {
        public string MaDocGia { get; set; }
        public DateTime NgayMuon { get; set; }
        public DateTime HanTra { get; set; }
        public string GhiChu { get; set; }
        public List<SachMuonDto> DanhSachSach { get; set; }
    }

    public class SachMuonDto
    {
        public string MaSach { get; set; }
        public int SoLuong { get; set; }
    }

    public class TraSachRequest
    {
        public string MaPhieuMuon { get; set; }
        public string GhiChu { get; set; }
        public List<string> MaSachTra { get; set; }
    }
}