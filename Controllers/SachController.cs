using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace CNPM_Doman_Nhom01.Controllers
{
    public class SachController : Controller
    {
        private readonly string _connectionString;

        public SachController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Lỗi kết nối.");
        }

        // 1. Trang danh sách sách
        public IActionResult Index()
        {
            var listSach = new List<dynamic>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT MaSach, TenSach, TacGia, SoLuongTon FROM Sach ORDER BY MaSach DESC", conn);
                using (var reader = cmd.ExecuteReader())
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
            ViewBag.ListSach = listSach;
            return View();
        }

        // 2. API Thêm sách mới
        [HttpPost]
        public IActionResult ThemSach(string maSach, string tenSach, string tacGia, int soLuong)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string sql = "INSERT INTO Sach (MaSach, TenSach, TacGia, SoLuongTon) VALUES (@Ma, @Ten, @TG, @SL)";
                    SqlCommand cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@Ma", maSach);
                    cmd.Parameters.AddWithValue("@Ten", tenSach);
                    cmd.Parameters.AddWithValue("@TG", tacGia);
                    cmd.Parameters.AddWithValue("@SL", soLuong);
                    cmd.ExecuteNonQuery();
                }
                return Json(new { success = true, message = "Thêm sách thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }
    }
}