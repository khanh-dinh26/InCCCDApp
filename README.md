# InCCCDApp

Phần mềm Windows Forms hỗ trợ **in thẻ CCCD 2 mặt đối xứng trên giấy A4**, tự nắn phẳng ảnh chụp thẻ và xuất ra file Word để in ngay.

## Tính năng

- Nạp ảnh mặt trước / mặt sau thẻ, **click 4 góc** để nắn thẳng ảnh (hiệu chỉnh phối cảnh bằng OpenCV).
- Chuẩn hóa ảnh về đúng kích thước thẻ **85.6 × 54 mm** (1024 × 646 px).
- Xoay ảnh, phóng to / kéo ảnh (Ctrl + lăn chuột, giữ chuột phải để kéo).
- Danh sách chờ in: **4 thẻ / tờ A4**, xếp dọc 1 cột, quá 4 thẻ tự động sang tờ mới.
- Xuất Word (`.docx`):
  - Trang lẻ = **mặt trước**, sát lề trái, chừa trống bên phải.
  - Trang chẵn = **mặt sau**, lật gương sang lề phải để khớp khi in 2 mặt.
  - Khoảng hở giữa các thẻ ~14 mm và lề trên/dưới rộng, dễ cắt.

> **Khi in 2 mặt:** chọn **"Lật theo cạnh dài" (Flip on long edge)** để mặt sau khớp đúng vị trí mặt trước.

## Yêu cầu hệ thống

- Windows 10/11
- .NET Framework 4.7.2
- Visual Studio 2022 trở lên (kèm workload **.NET desktop development**)

## Cài đặt & chạy

```bash
git clone https://github.com/khanh-dinh26/InCCCDApp.git
```

1. Mở `InCCCDApp.slnx` (hoặc `InCCCDApp/InCCCDApp.csproj`) bằng Visual Studio.
2. Chờ Visual Studio **restore NuGet packages** (xem lưu ý bên dưới).
3. Nhấn `F5` để chạy.

## Lưu ý quan trọng: thư mục `packages/`

Thư mục `packages/` **không được đưa lên GitHub** (đã nằm trong `.gitignore`) vì rất nặng (~226 MB, chủ yếu là OpenCV). Thư viện sẽ được NuGet tự tải lại về máy khi build.

Các gói cần có (khai báo trong `InCCCDApp/packages.config`):

| Package | Version |
|---|---|
| DocX | 2.5.0 |
| OpenCvSharp4 | 4.8.0.20230708 |
| OpenCvSharp4.Extensions | 4.8.0.20230708 |
| OpenCvSharp4.runtime.win | 4.8.0.20230708 |
| OpenCvSharp4.Windows | 4.8.0.20230708 |
| System.Buffers | 4.5.1 |
| System.Drawing.Common | 7.0.0 |
| System.Memory | 4.5.5 |
| System.Numerics.Vectors | 4.5.0 |
| System.Runtime.CompilerServices.Unsafe | 6.0.0 |

**Nếu build báo thiếu package / không tìm thấy DLL:**

- Trong Visual Studio: chuột phải Solution → **Restore NuGet Packages**, hoặc
- Chạy lệnh:
  ```bash
  nuget restore InCCCDApp.slnx
  ```
  (hoặc `msbuild InCCCDApp.slnx /t:Restore`)

**Nếu muốn mang thư viện theo repo (dùng offline, không cần mạng):** xóa dòng `[Pp]ackages/` trong `.gitignore` rồi commit thư mục `packages/` — repo sẽ nặng thêm khoảng 226 MB.

> Đường dẫn thư viện trong `.csproj` đang trỏ tới `..\packages\...`, nên thư mục `packages/` phải nằm **cùng cấp với file solution**, không nằm trong thư mục project.

## Cấu trúc thư mục

```
InCCCDApp/
├── InCCCDApp.slnx              # Solution
├── .gitignore
├── packages/                   # NuGet (không commit)
└── InCCCDApp/
    ├── Form1.cs                # Giao diện + xử lý chính + xuất Word
    ├── Form1.Designer.cs
    ├── ImageHelper.cs          # Nắn phẳng phối cảnh (OpenCV)
    ├── Program.cs
    ├── App.config
    ├── packages.config
    └── Properties/
```

## Công nghệ sử dụng

- C# / WinForms — .NET Framework 4.7.2
- [OpenCvSharp4](https://github.com/shimat/opencvsharp) — nắn phẳng 4 góc thẻ
- [DocX (Xceed)](https://github.com/xceedsoftware/DocX) — tạo file Word

## Tác giả

**Nguyễn Đình Khánh** — [@khanh-dinh26](https://github.com/khanh-dinh26)
