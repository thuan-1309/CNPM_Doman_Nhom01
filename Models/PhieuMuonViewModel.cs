namespace CNPM_Doman_Nhom01.Models
{
    // Class dùng để nhận dữ liệu từ AJAX Lập Phiếu
    public class PhieuMuonRequest
    {
        public string? MaDocGia { get; set; }
        public DateTime NgayMuon { get; set; }
        public DateTime HanTra { get; set; }
        public string? GhiChu { get; set; }
        public List<SachMuonRequest> DanhSachSach { get; set; } = new List<SachMuonRequest>();
    }

    public class SachMuonRequest
    {
        public string? MaSach { get; set; }
        public int SoLuong { get; set; }
    }

    // Class dùng để nhận dữ liệu từ AJAX Trả Sách
    public class TraSachRequest
    {
        public string? MaPhieuMuon { get; set; }
        public string? GhiChu { get; set; }
        public List<string> MaSachTra { get; set; } = new List<string>();
    }
}