USE master;
GO

-- Xóa database cũ nếu đã tồn tại để làm mới hoàn toàn
IF DB_ID('QuanLyThuVien') IS NOT NULL
BEGIN
    ALTER DATABASE QuanLyThuVien SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE QuanLyThuVien;
END
GO

-- Tạo mới Database
CREATE DATABASE QuanLyThuVien;
GO
USE QuanLyThuVien;
GO

-- Bảng Độc Giả
CREATE TABLE DocGia (
    MaDocGia VARCHAR(10) PRIMARY KEY,
    TenDocGia NVARCHAR(100) NOT NULL,
    SoDienThoai VARCHAR(15),
    DiaChi NVARCHAR(255)
);

-- Bảng Sách
CREATE TABLE Sach (
    MaSach VARCHAR(10) PRIMARY KEY,
    TenSach NVARCHAR(200) NOT NULL,
    TacGia NVARCHAR(100),
    SoLuongTon INT DEFAULT 0
);

-- Bảng Phiếu Mượn
CREATE TABLE PhieuMuon (
    MaPhieuMuon VARCHAR(10) PRIMARY KEY,
    MaDocGia VARCHAR(10) FOREIGN KEY REFERENCES DocGia(MaDocGia),
    NgayMuon DATE NOT NULL,
    HanTra DATE NOT NULL,
    ThuThu NVARCHAR(50),
    GhiChu NVARCHAR(255),
    TrangThai NVARCHAR(50) DEFAULT N'Đang mượn' 
);

-- Bảng Chi Tiết Phiếu Mượn
CREATE TABLE ChiTietPhieuMuon (
    MaPhieuMuon VARCHAR(10) FOREIGN KEY REFERENCES PhieuMuon(MaPhieuMuon),
    MaSach VARCHAR(10) FOREIGN KEY REFERENCES Sach(MaSach),
    SoLuong INT NOT NULL DEFAULT 1,
    TinhTrangTra NVARCHAR(50) DEFAULT N'Chưa trả', 
    PRIMARY KEY (MaPhieuMuon, MaSach)
);
GO

-- ==============================================
-- THÊM DỮ LIỆU MẪU (MOCK DATA)
-- ==============================================

-- 1. Thêm Độc Giả
INSERT INTO DocGia (MaDocGia, TenDocGia, SoDienThoai, DiaChi) VALUES 
('DG001', N'Nguyễn Văn An', '0912345678', N'123 Lê Lợi, Quận 1, TP.HCM'),
('DG002', N'Trần Thị Bích', '0987654321', N'45 Võ Văn Ngân, TP.Thủ Đức'),
('DG003', N'Lê Hoàng Nam', '0901112233', N'89 Nguyễn Đình Chiểu, Quận 3, TP.HCM'),
('DG004', N'Phạm Minh Tuấn', '0933445566', N'12 Huỳnh Thúc Kháng, Biên Hòa, Đồng Nai'),
('DG005', N'Hoàng Yến Nhi', '0922334455', N'Ký túc xá Khu A, Dĩ An, Bình Dương');

-- 2. Thêm Sách
INSERT INTO Sach (MaSach, TenSach, TacGia, SoLuongTon) VALUES 
('MS001', N'Lập trình C# từ cơ bản đến nâng cao', N'Phạm Hữu Khang', 5),
('MS002', N'Nhập môn Công nghệ phần mềm', N'Lê Văn Phùng', 10),
('MS003', N'Phân tích thiết kế hệ thống thông tin', N'Nguyễn Văn Ba', 7),
('MS004', N'Cấu trúc dữ liệu và giải thuật', N'Nguyễn Đức Nghĩa', 4),
('MS005', N'Cơ sở dữ liệu SQL Server', N'Trần Thị Anh', 8),
('MS006', N'Lập trình Web với ASP.NET MVC', N'Hoàng Lương', 6);

-- 3. Thêm Phiếu Mượn (Giả lập một số phiếu mượn cũ)
INSERT INTO PhieuMuon (MaPhieuMuon, MaDocGia, NgayMuon, HanTra, ThuThu, GhiChu, TrangThai) VALUES 
('PM00025', 'DG001', '2026-05-20', '2026-06-05', 'admin', N'Độc giả VIP', N'Đang mượn'),
('PM00026', 'DG003', '2026-05-15', '2026-05-30', 'admin', NULL, N'Đang mượn'),
('PM00027', 'DG002', '2026-04-10', '2026-04-25', 'admin', N'Trả muộn', N'Quá hạn');

-- 4. Thêm Chi Tiết Phiếu Mượn
-- Phiếu PM00025 của Nguyễn Văn An (Khớp với hình ảnh mẫu)
INSERT INTO ChiTietPhieuMuon (MaPhieuMuon, MaSach, SoLuong, TinhTrangTra) VALUES 
('PM00025', 'MS001', 1, N'Đúng hạn'),
('PM00025', 'MS005', 1, N'Chưa trả');

-- Phiếu PM00026 của Lê Hoàng Nam
INSERT INTO ChiTietPhieuMuon (MaPhieuMuon, MaSach, SoLuong, TinhTrangTra) VALUES 
('PM00026', 'MS002', 1, N'Chưa trả'),
('PM00026', 'MS006', 2, N'Chưa trả');

-- Phiếu PM00027 của Trần Thị Bích
INSERT INTO ChiTietPhieuMuon (MaPhieuMuon, MaSach, SoLuong, TinhTrangTra) VALUES 
('PM00027', 'MS004', 1, N'Quá hạn');


-- 1. Thêm thêm Độc giả (Tổng ~10 người)
INSERT INTO DocGia VALUES 
('DG006', N'Lý Hải Đăng', '0944556677', N'22/5 Phan Đăng Lưu, Quận Phú Nhuận'),
('DG007', N'Vũ Thu Thảo', '0955667788', N'789 Cách Mạng Tháng 8, Quận 10'),
('DG008', N'Đặng Minh Khôi', '0966778899', N'Làng Đại Học, Dĩ An, Bình Dương'),
('DG009', N'Mai Phương Thúy', '0977889900', N'15 Tố Hữu, Quận Nam Từ Liêm, Hà Nội'),
('DG010', N'Bùi Tiến Dũng', '0988990011', N'Căn hộ Landmark 81, Quận Bình Thạnh');

-- 2. Thêm thêm Sách (Tổng ~20 cuốn)
INSERT INTO Sach VALUES 
('MS007', N'Clean Code: A Handbook of Agile Software', N'Robert C. Martin', 5),
('MS008', N'Design Patterns: Elements of Reusable Object-Oriented Software', N'Erich Gamma', 3),
('MS009', N'Lập trình Java từ cơ bản đến nâng cao', N'Nguyễn Tiến Huy', 12),
('MS010', N'Python Crash Course', N'Eric Matthes', 8),
('MS011', N'Data Science for Business', N'Foster Provost', 4),
('MS012', N'The Pragmatic Programmer', N'Andrew Hunt', 6),
('MS013', N'Kỹ nghệ phần mềm hiện đại', N'Trần Minh Thái', 9),
('MS014', N'Trí tuệ nhân tạo - AI', N'Russell & Norvig', 2),
('MS015', N'Mạng máy tính cơ bản', N'James Kurose', 7),
('MS016', N'Lập trình JavaScript cho người mới', N'Marijn Haverbeke', 15),
('MS017', N'Cấu trúc dữ liệu nâng cao', N'Sartaj Sahni', 3),
('MS018', N'Hệ điều hành Windows & Linux', N'William Stallings', 5),
('MS019', N'An toàn thông tin mạng', N'Stallings', 10),
('MS020', N'Thiết kế UI/UX hiện đại', N'Steve Krug', 20);

-- 3. Thêm thêm Phiếu mượn ngẫu nhiên
INSERT INTO PhieuMuon VALUES 
('PM00028', 'DG006', '2026-05-01', '2026-05-15', 'admin', N'Mượn học tập', N'Đã trả'),
('PM00029', 'DG008', '2026-05-18', '2026-06-01', 'admin', NULL, N'Đang mượn'),
('PM00030', 'DG010', '2026-05-19', '2026-06-02', 'admin', N'Sách quý', N'Đang mượn');

-- 4. Thêm thêm Chi tiết phiếu mượn
INSERT INTO ChiTietPhieuMuon VALUES 
('PM00028', 'MS007', 1, N'Đúng hạn'),
('PM00028', 'MS012', 1, N'Đúng hạn'),
('PM00029', 'MS010', 1, N'Chưa trả'),
('PM00030', 'MS014', 1, N'Chưa trả'),
('PM00030', 'MS020', 1, N'Chưa trả');