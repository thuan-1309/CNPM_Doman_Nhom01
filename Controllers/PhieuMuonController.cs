using CNPM_Doman_Nhom01.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CNPM_Doman_Nhom01.Controllers
{
    public class PhieuMuonController : Controller
    {
        private readonly string _connectionString;

        public PhieuMuonController(IConfiguration configuration)
        {
            // Bắt lỗi nếu không tìm thấy chuỗi kết nối trong appsettings.json
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Không tìm thấy chuỗi kết nối 'DefaultConnection'.");
        }

        // ==========================================
        // 1. GIAO DIỆN LẬP PHIẾU MƯỢN
        // ==========================================
        public IActionResult LapPhieu()
        {
            var listDocGia = new List<dynamic>();
            var listSach = new List<dynamic>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                // Lấy danh sách độc giả
                SqlCommand cmdDG = new SqlCommand("SELECT MaDocGia, TenDocGia, SoDienThoai, DiaChi FROM DocGia", conn);
                using (var reader = cmdDG.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        listDocGia.Add(new
                        {
                            MaDocGia = reader["MaDocGia"]?.ToString() ?? "",
                            TenDocGia = reader["TenDocGia"]?.ToString() ?? "",
                            SoDienThoai = reader["SoDienThoai"]?.ToString() ?? "",
                            DiaChi = reader["DiaChi"]?.ToString() ?? ""
                        });
                    }
                }

                // Lấy danh sách sách còn tồn kho
                SqlCommand cmdSach = new SqlCommand("SELECT MaSach, TenSach, TacGia, SoLuongTon FROM Sach WHERE SoLuongTon > 0", conn);
                using (var reader = cmdSach.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        listSach.Add(new
                        {
                            MaSach = reader["MaSach"]?.ToString() ?? "",
                            TenSach = reader["TenSach"]?.ToString() ?? "",
                            TacGia = reader["TacGia"]?.ToString() ?? "",
                            SoLuongTon = reader["SoLuongTon"] != DBNull.Value ? (int)reader["SoLuongTon"] : 0
                        });
                    }
                }
            }

            ViewBag.DocGias = listDocGia;
            ViewBag.Sachs = listSach;
            return View();
        }

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

                    string sqlPhieu = "INSERT INTO PhieuMuon (MaPhieuMuon, MaDocGia, NgayMuon, HanTra, ThuThu, GhiChu, TrangThai) VALUES (@Ma, @DG, @Ngay, @Han, 'admin', @GC, N'Đang mượn')";
                    SqlCommand cmdPhieu = new SqlCommand(sqlPhieu, conn, transaction);
                    cmdPhieu.Parameters.AddWithValue("@Ma", maPhieu);
                    cmdPhieu.Parameters.AddWithValue("@DG", request.MaDocGia ?? (object)DBNull.Value);
                    cmdPhieu.Parameters.AddWithValue("@Ngay", request.NgayMuon);
                    cmdPhieu.Parameters.AddWithValue("@Han", request.HanTra);
                    cmdPhieu.Parameters.AddWithValue("@GC", string.IsNullOrEmpty(request.GhiChu) ? DBNull.Value : request.GhiChu);
                    cmdPhieu.ExecuteNonQuery();

                    foreach (var sach in request.DanhSachSach)
                    {
                        string sqlCT = "INSERT INTO ChiTietPhieuMuon (MaPhieuMuon, MaSach, SoLuong, TinhTrangTra) VALUES (@MaP, @MaS, @SL, N'Chưa trả')";
                        SqlCommand cmdCT = new SqlCommand(sqlCT, conn, transaction);
                        cmdCT.Parameters.AddWithValue("@MaP", maPhieu);
                        cmdCT.Parameters.AddWithValue("@MaS", sach.MaSach ?? (object)DBNull.Value);
                        cmdCT.Parameters.AddWithValue("@SL", sach.SoLuong);
                        cmdCT.ExecuteNonQuery();

                        string sqlTruSach = "UPDATE Sach SET SoLuongTon = SoLuongTon - @SL WHERE MaSach = @MaS";
                        SqlCommand cmdTruSach = new SqlCommand(sqlTruSach, conn, transaction);
                        cmdTruSach.Parameters.AddWithValue("@SL", sach.SoLuong);
                        cmdTruSach.Parameters.AddWithValue("@MaS", sach.MaSach ?? (object)DBNull.Value);
                        cmdTruSach.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    return Json(new { success = true, message = $"Lập phiếu {maPhieu} thành công!" });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = "Lỗi: " + ex.Message });
                }
            }
        }

        // ==========================================
        // 2. GIAO DIỆN TRẢ SÁCH
        // ==========================================
        public IActionResult TraSach()
        {
            return View();
        }

        [HttpGet]
        public IActionResult TimKiemPhieu(string maPhieu)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                SqlCommand cmdCheck = new SqlCommand("SELECT p.MaPhieuMuon, d.TenDocGia, p.NgayMuon, p.HanTra FROM PhieuMuon p JOIN DocGia d ON p.MaDocGia = d.MaDocGia WHERE p.MaPhieuMuon = @MaP", conn);
                cmdCheck.Parameters.AddWithValue("@MaP", maPhieu);

                var phieuInfo = new Dictionary<string, string>();
                using (var reader = cmdCheck.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        // Thêm dấu ? và xử lý null để fix lỗi Dictionary
                        phieuInfo.Add("TenDocGia", reader["TenDocGia"]?.ToString() ?? "");

                        DateTime ngayMuon = reader["NgayMuon"] != DBNull.Value ? Convert.ToDateTime(reader["NgayMuon"]) : DateTime.MinValue;
                        DateTime hanTra = reader["HanTra"] != DBNull.Value ? Convert.ToDateTime(reader["HanTra"]) : DateTime.MinValue;

                        phieuInfo.Add("NgayMuon", ngayMuon.ToString("dd/MM/yyyy"));
                        phieuInfo.Add("HanTra", hanTra.ToString("dd/MM/yyyy"));
                    }
                    else
                    {
                        return Json(new { success = false, message = "Không tìm thấy mã phiếu này!" });
                    }
                }

                var listSach = new List<dynamic>();
                SqlCommand cmdSach = new SqlCommand(@"
                    SELECT c.MaSach, s.TenSach, p.NgayMuon, p.HanTra, c.TinhTrangTra 
                    FROM ChiTietPhieuMuon c JOIN Sach s ON c.MaSach = s.MaSach JOIN PhieuMuon p ON c.MaPhieuMuon = p.MaPhieuMuon
                    WHERE c.MaPhieuMuon = @MaP", conn);
                cmdSach.Parameters.AddWithValue("@MaP", maPhieu);

                using (var reader = cmdSach.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        DateTime ngayMuonSach = reader["NgayMuon"] != DBNull.Value ? Convert.ToDateTime(reader["NgayMuon"]) : DateTime.MinValue;
                        DateTime hanTraSach = reader["HanTra"] != DBNull.Value ? Convert.ToDateTime(reader["HanTra"]) : DateTime.MinValue;

                        listSach.Add(new
                        {
                            MaSach = reader["MaSach"]?.ToString() ?? "",
                            TenSach = reader["TenSach"]?.ToString() ?? "",
                            NgayMuon = ngayMuonSach.ToString("dd/MM/yyyy"),
                            HanTra = hanTraSach.ToString("dd/MM/yyyy"),
                            TinhTrangTra = reader["TinhTrangTra"]?.ToString() ?? ""
                        });
                    }
                }

                return Json(new { success = true, info = phieuInfo, dsSach = listSach });
            }
        }

        [HttpPost]
        public IActionResult XacNhanTra([FromBody] TraSachRequest request)
        {
            if (request.MaSachTra == null || request.MaSachTra.Count == 0)
                return Json(new { success = false, message = "Vui lòng chọn ít nhất 1 sách để trả!" });

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                SqlTransaction transaction = conn.BeginTransaction();
                try
                {
                    foreach (var maSach in request.MaSachTra)
                    {
                        SqlCommand cmdTra = new SqlCommand("UPDATE ChiTietPhieuMuon SET TinhTrangTra = N'Đã trả' WHERE MaPhieuMuon = @MaP AND MaSach = @MaS", conn, transaction);
                        cmdTra.Parameters.AddWithValue("@MaP", request.MaPhieuMuon ?? (object)DBNull.Value);
                        cmdTra.Parameters.AddWithValue("@MaS", maSach);
                        cmdTra.ExecuteNonQuery();

                        SqlCommand cmdGetSL = new SqlCommand("SELECT SoLuong FROM ChiTietPhieuMuon WHERE MaPhieuMuon = @MaP AND MaSach = @MaS", conn, transaction);
                        cmdGetSL.Parameters.AddWithValue("@MaP", request.MaPhieuMuon ?? (object)DBNull.Value);
                        cmdGetSL.Parameters.AddWithValue("@MaS", maSach);

                        object resultSL = cmdGetSL.ExecuteScalar();
                        int soLuong = resultSL != null && resultSL != DBNull.Value ? (int)resultSL : 0;

                        SqlCommand cmdCong = new SqlCommand("UPDATE Sach SET SoLuongTon = SoLuongTon + @SL WHERE MaSach = @MaS", conn, transaction);
                        cmdCong.Parameters.AddWithValue("@SL", soLuong);
                        cmdCong.Parameters.AddWithValue("@MaS", maSach);
                        cmdCong.ExecuteNonQuery();
                    }

                    SqlCommand cmdCheckFull = new SqlCommand("SELECT COUNT(*) FROM ChiTietPhieuMuon WHERE MaPhieuMuon = @MaP AND TinhTrangTra != N'Đã trả'", conn, transaction);
                    cmdCheckFull.Parameters.AddWithValue("@MaP", request.MaPhieuMuon ?? (object)DBNull.Value);

                    object resultRemaining = cmdCheckFull.ExecuteScalar();
                    int remaining = resultRemaining != null && resultRemaining != DBNull.Value ? (int)resultRemaining : 0;

                    if (remaining == 0)
                    {
                        SqlCommand cmdUpdatePhieu = new SqlCommand("UPDATE PhieuMuon SET TrangThai = N'Đã trả', GhiChu = ISNULL(GhiChu,'') + @GC WHERE MaPhieuMuon = @MaP", conn, transaction);
                        cmdUpdatePhieu.Parameters.AddWithValue("@MaP", request.MaPhieuMuon ?? (object)DBNull.Value);
                        cmdUpdatePhieu.Parameters.AddWithValue("@GC", string.IsNullOrEmpty(request.GhiChu) ? "" : " | Ghi chú trả: " + request.GhiChu);
                        cmdUpdatePhieu.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    return Json(new { success = true, message = "Xác nhận trả sách thành công!" });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = "Lỗi: " + ex.Message });
                }
            }
        }
    }
}