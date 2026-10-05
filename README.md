# Vấn Đạo (C# / ASP.NET Core + SQLite)

Game tu tiên 2D. Server C# xử lý toàn bộ luật chơi và lưu tiến độ vào SQLite; trình duyệt chỉ hiển thị canvas và gửi hành động.

## Chạy

Cần .NET 8 SDK và Windows.

```
cd VanDao
dotnet run
```

Game chạy dưới dạng **dải ngang nằm sát ngay trên thanh taskbar**, giống cách "TBH: Task Bar Hero" hoạt động (cần Microsoft Edge WebView2 Runtime, có sẵn trên Windows 10/11):

- **Nền trong suốt**: chỉ thấy nhân vật và bảng thông tin gọn; phần trống bấm xuyên qua được. Tắt trong menu nếu màn hình bị đen.
- **Chuột phải** lên dải (hoặc nút ⋯): menu nhanh gồm bảng tu luyện, bố cục màn hình, màn hình, kích thước (75–200%), độ trong, ghim luôn hiển thị, nền trong suốt, đặt lại vị trí, ẩn xuống khay, thoát.
- **Bố cục màn hình**: tự động theo vị trí taskbar, hoặc neo trái / giữa / phải ở mép dưới hay mép trên. Kéo ⋮⋮ để đặt tự do.
- **Nút Bảng**: mở Đột phá / Túi đồ / Luyện đan trong cửa sổ bật lên phía trên dải. Esc hoặc nút đóng để quay về dạng dải.
- **Shift+F12**: đặt lại vị trí khi dải bị lạc. **Shift+F11**: đặt lại kích thước.
- **Khay hệ thống**: bấm biểu tượng để ẩn/hiện, chuột phải để mở menu.

Mọi cài đặt được nhớ cho lần mở sau.

Muốn chơi trên trình duyệt như trước: `dotnet run -- --web` rồi mở http://localhost:5080.

Dữ liệu nằm trong `%LocalAppData%\VanDao`: `vandao.db` (tiến độ), `window.json` (cài đặt cửa sổ), `WebView2\` (cache).

## Cấu trúc

| File | Nội dung |
|---|---|
| `Program.cs` | Cấu hình DI, EF Core, các API endpoint |
| `Game/GameData.cs` | Vật phẩm, cảnh giới, công thức luyện đan, tỉ lệ rơi |
| `Game/GameService.cs` | Luật chơi: tu vi theo thời gian, vận công, thám hiểm, đột phá, luyện đan |
| `Game/Dtos.cs` | Dữ liệu trả về cho client |
| `Data/Entities.cs` | Bảng `Players`, `Inventory`, `Logs` |
| `Data/GameDbContext.cs` | EF Core DbContext |
| `Desktop/GameWindow.cs` | Dải game trên taskbar: neo vị trí, nền trong suốt, menu chuột phải, phím tắt, khay hệ thống |
| `Desktop/PanelWindow.cs` | Cửa sổ bảng bật lên (Đột phá / Túi đồ / Luyện đan) |
| `Desktop/WindowSettings.cs` | Lưu cài đặt cửa sổ |
| `Desktop/WebHost.cs`, `Native.cs`, `Theme.cs` | WebView2 dùng chung, Win32 API, giao diện menu tối |
| `wwwroot/index.html` | Giao diện và nhân vật 2D vẽ bằng canvas |

## API

| Method | Đường dẫn | Mô tả |
|---|---|---|
| GET | `/api/data` | Dữ liệu tĩnh (vật phẩm, cảnh giới, công thức) |
| POST | `/api/players` | Tạo tu sĩ mới |
| GET | `/api/players/{id}` | Trạng thái hiện tại |
| POST | `/api/players/{id}/channel` | Vận công |
| POST | `/api/players/{id}/explore` | Thám hiểm |
| POST | `/api/players/{id}/advance` | Đột phá tầng / cảnh giới |
| POST | `/api/players/{id}/craft/{index}` | Luyện đan theo công thức |
| POST | `/api/players/{id}/reset` | Tu lại từ đầu |

## Thêm cảnh giới mới

Trong `Game/GameData.cs`:

1. Thêm vật phẩm mới vào `Items`.
2. Thêm `RealmDef` vào cuối `Realms`.
3. Thêm công thức vào `Recipes`.
4. Điền `BreakCost` và `BreakChance` cho Kim Đan để mở đường đột phá.
5. Xóa tên cảnh giới đó khỏi `LockedRealms`.

## Đổi sang SQL Server

Thay package `Microsoft.EntityFrameworkCore.Sqlite` bằng `Microsoft.EntityFrameworkCore.SqlServer`, đổi `UseSqlite` thành `UseSqlServer` trong `Program.cs` và cập nhật connection string trong `appsettings.json`.
