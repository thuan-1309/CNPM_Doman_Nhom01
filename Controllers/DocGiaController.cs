using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace CNPM_Doman_Nhom01.Controllers
{
    public class DocGiaController : Controller
    {
        private readonly string _connectionString;

        public DocGiaController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Lỗi kết nối.");
        }

        public IActionResult Index()
        {
            var listDocGia = new List<dynamic>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT MaDocGia, TenDocGia, SoDienThoai, DiaChi FROM DocGia ORDER BY MaDocGia DESC", conn);
                using (var reader = cmd.ExecuteReader())
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
            }
            ViewBag.ListDocGia = listDocGia;
            return View();
        }

        [HttpPost]
        public IActionResult ThemDocGia(string maDG, string tenDG, string sdt, string diaChi)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string sql = "INSERT INTO DocGia (MaDocGia, TenDocGia, SoDienThoai, DiaChi) VALUES (@Ma, @Ten, @SDT, @DC)";
                    SqlCommand cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@Ma", maDG);
                    cmd.Parameters.AddWithValue("@Ten", tenDG);
                    cmd.Parameters.AddWithValue("@SDT", sdt);
                    cmd.Parameters.AddWithValue("@DC", diaChi);
                    cmd.ExecuteNonQuery();
                }
                return Json(new { success = true, message = "Thêm độc giả thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }
    }
}