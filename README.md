# Vấn Đạo (C# / ASP.NET Core + SQLite)

Game tu tiên 2D. Server C# xử lý toàn bộ luật chơi và lưu tiến độ vào SQLite; trình duyệt chỉ hiển thị canvas và gửi hành động.

## Chạy

Cần .NET 8 SDK và Windows.

```
cd VanDao
dotnet run
```

Game chạy dưới dạng **dải nhỏ 280×200 nằm sát ngay trên thanh taskbar**, giống cách "TBH: Task Bar Hero" hoạt động (cần Microsoft Edge WebView2 Runtime, có sẵn trên Windows 10/11):

- **Nền trong suốt**: chỉ thấy nhân vật và bảng thông tin gọn; phần trống bấm xuyên qua được. Tắt trong menu nếu màn hình bị đen.
- **Chuột phải** lên dải (hoặc nút bánh răng): menu cài đặt gồm bố cục màn hình, kích thước (75–200%), ghim luôn hiển thị, nền trong suốt, đặt lại vị trí, ẩn xuống khay, thoát.
- **Bố cục màn hình**: tự động theo vị trí taskbar, hoặc neo trái / giữa / phải ở mép dưới hay mép trên. Kéo thanh tiêu đề (chỗ tên cảnh giới) để đặt tự do.
- **Nút Tu luyện** (quyển sách): mở cửa sổ nhỏ Đột phá / Hành trang / Luyện đan bật lên phía trên dải. Esc hoặc nút đóng để quay về dạng dải.
- **Shift+F12**: đặt lại vị trí khi dải bị lạc. **Shift+F11**: đặt lại kích thước.
- **Khay hệ thống**: bấm biểu tượng để ẩn/hiện, chuột phải để mở menu.

Mọi cài đặt được nhớ cho lần mở sau.

## Cách chơi

- **Tự động thám hiểm** (luôn bật): mỗi chuyến nhân vật đi qua rừng, gặp quái và giao chiến theo lượt. Cảnh đánh là pixel art (sprite 48×48 trong `wwwroot/img/*_48.png`, vẽ ở độ phân giải thấp rồi phóng to không làm mịn): tu sĩ bên trái, quái bên phải, vệt kiếm khí, số sát thương, thanh máu và vòng chọn mục tiêu; hoạt ảnh khớp đúng kết quả trận do server tính; xong chuyến nhân vật nhận thêm tu vi và vật phẩm rơi theo tỉ lệ của cảnh giới. Thanh nhỏ cạnh các nút cho biết thời gian đến chuyến tiếp theo.
- **Tu vi tự tăng** (mặc định 3/giây ở Luyện Khí tầng 1), kể cả khi tắt game (tối đa 2 giờ). Khi mở lại, các chuyến thám hiểm lúc vắng mặt được cộng gộp vào một dòng nhật ký.
- **Chỉ số chiến đấu**: Tấn công, Máu, Phòng thủ = chỉ số gốc theo cảnh giới/tầng + trang bị. Trận đánh theo lượt, không ngẫu nhiên: sát thương = tấn công − phòng thủ đối thủ. Thắng thì nhận tu vi và vật phẩm; thua thì rút lui, chỉ nhận nửa tu vi. Tấn công cao hạ quái nhanh hơn nên chuyến thám hiểm ngắn hơn.
- **Trang bị** (rơi hiếm khi thám hiểm): kiếm/đao tăng tấn công; áo, quần, nón tăng máu và phòng thủ; găng tay tăng tấn công và phòng thủ; nhẫn tăng tu vi và may mắn. Bấm vào món trong Hành trang để mặc, bấm vào ô đang mặc để tháo.
- Các nút là icon; rê chuột lên để xem tooltip mô tả.

Ảnh nhân vật và trang bị nằm trong `wwwroot/img`.

Muốn chơi trên trình duyệt như trước: `dotnet run -- --web` rồi mở http://localhost:5080.

Dữ liệu nằm trong `%LocalAppData%\VanDao`: `vandao.db` (tiến độ), `window.json` (cài đặt cửa sổ), `WebView2\` (cache).

## Cấu trúc

| File | Nội dung |
|---|---|
| `Program.cs` | Cấu hình DI, EF Core, các API endpoint |
| `Game/GameData.cs` | Vật phẩm, cảnh giới, công thức luyện đan, tỉ lệ rơi |
| `Game/GameService.cs` | Luật chơi: tu vi theo thời gian, vận công, thám hiểm, đột phá, luyện đan |
| `Game/Dtos.cs` | Dữ liệu trả về cho client |
| `Data/Entities.cs` | Bảng `Players`, `Inventory`, `Equipment`, `Logs` |
| `Data/GameDbContext.cs` | EF Core DbContext |
| `Desktop/GameWindow.cs` | Dải game trên taskbar: neo vị trí, nền trong suốt, menu chuột phải, phím tắt, khay hệ thống |
| `Desktop/PanelWindow.cs` | Cửa sổ Tu luyện bật lên (Đột phá / Hành trang / Luyện đan) |
| `Desktop/WindowSettings.cs` | Lưu cài đặt cửa sổ |
| `Desktop/WebHost.cs`, `Native.cs`, `Theme.cs` | WebView2 dùng chung, Win32 API, giao diện menu tối |
| `wwwroot/index.html` | Giao diện, tooltip, cảnh 2D trên canvas |
| `wwwroot/img/` | Sprite nhân vật theo cảnh giới, yêu lang, icon trang bị và vũ khí |

## API

| Method | Đường dẫn | Mô tả |
|---|---|---|
| GET | `/api/data` | Dữ liệu tĩnh (vật phẩm, cảnh giới, công thức) |
| POST | `/api/players` | Tạo tu sĩ mới |
| GET | `/api/players/current` | Tu sĩ đang chơi trên máy này (gần nhất), chưa có thì tạo mới |
| GET | `/api/players/{id}` | Trạng thái hiện tại |
| POST | `/api/players/{id}/channel` | Vận công |
| POST | `/api/players/{id}/equip/{itemId}` | Mặc trang bị |
| POST | `/api/players/{id}/unequip/{slot}` | Tháo trang bị |
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
