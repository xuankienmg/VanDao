# Rà soát dung lượng source — 21/10/2026

Đã dọn sau khi người dùng duyệt. Các số ở bảng dưới là số đo trước khi dọn.
Dung lượng dưới đây tính theo MiB (1.048.576 byte), không tính thư mục `.git`.

## Kết quả dọn

- Đã xóa **2.044,78 MB** file build/cache và bản phát hành cũ: root `bin`, `obj`, `build-check`, `publish`, cùng bin/obj của project trong tools. Sau đó build lại nên bin/obj mới được tạo bình thường.
- Đã loại 2.244 file build-check cũ (188,72 MB) khỏi working tree; Git ghi nhận các file bị xóa, chưa commit và chưa viết lại lịch sử Git.
- Đã chuyển 6 ảnh nguồn (7,31 MB) sang `tools/art-sources/skin`, đối chiếu SHA-256 trước/sau và sửa 4 công cụ cùng tài liệu tham chiếu. Tài nguyên `wwwroot` giảm từ 62,01 xuống **54,70 MB**.
- Đã thêm `build-check/` vào gitignore; thêm build-check, publish, docs và Claude outputs vào DefaultItemExcludes để loại khỏi đầu vào project. Danh sách Content sau dọn không còn mục thuộc các thư mục này.
- Giữ các preview thiết kế trong `Claude outputs`, tài liệu, SQL, công cụ và tài nguyên runtime chưa xác nhận thừa.
- Build đầy đủ thành công, **0 lỗi**. Còn cảnh báo NU1900 do không truy cập được nguồn kiểm tra lỗ hổng NuGet và MSB3277 WindowsBase/WebView2. Kiểm tra 64 trường hợp nhịp chiến đấu đều đạt.

## Kết quả chính

| Nhóm | Trên ổ đĩa | Đang được Git theo dõi | Nhận định |
|---|---:|---:|---|
| `bin/` | 929,11 MB | 0 | Kết quả build, có thể tạo lại. Không dọn bản đang chạy. |
| `obj/` | Khoảng 177 MB | 0 | Cache build, báo cáo và ảnh kiểm tra; có thể tạo lại. |
| `build-check/` | 191,45 MB | 188,72 MB / 2.244 file | Các bản build kiểm tra cũ, gồm DLL và bản sao wwwroot; ưu tiên loại khỏi source. |
| `publish/` | 369,00 MB | 0 | Có bản phát hành và ZIP 0.6.0; lưu trữ phát hành, không phải source chạy hiện tại. |
| `tools/` | 446,86 MB | 68,71 MB | Gồm công cụ, ảnh gốc, kiểm thử và bin/obj của project kiểm thử. Không xóa cả thư mục. |
| `wwwroot/` | 62,01 MB | 62,01 MB | Tài nguyên runtime, có ảnh gốc và ảnh xem trước có thể tách riêng. |
| `docs/` | 37,39 MB | 37,39 MB | Tài liệu, demo, GIF. Có SQL phục vụ Supabase, cần giữ SQL. |
| `Claude outputs/` | 6,48 MB | 6,48 MB | 7 ảnh preview thiết kế; không tìm thấy tham chiếu trong mã runtime. |

## File nguồn ảnh không được renderer hiện tại tải

Sáu file sau tổng cộng khoảng **7,31 MB**. Đã chuyển từ các đường dẫn cũ dưới đây sang `tools/art-sources/skin/` với tên file giữ nguyên; công cụ tạo sprite vẫn dùng được:

- `wwwroot/img/skin/kimdan_flight_source.png`
- `wwwroot/img/skin/luyenhu_flight_source.png`
- `wwwroot/img/skin/trucco_flight_source.png`
- `wwwroot/img/skin/luyenkhi_run_source.png`
- `wwwroot/img/skin/luyenkhi_idle_breeze_sheet.png`
- `wwwroot/img/skin/luyenkhi_actions_clean.png`

Đối chiếu: renderer trong `wwwroot/js/skill-preview.js` dùng `*_actions.png`, `*_flight.png`, `luyenkhi_*_v2.png`; inventory dùng atlas riêng. Các công cụ `import_skin_flight.py`, `import_thanh_sam_actions.py`, `import_thanh_sam_run.py`, `pack_inventory_breeze.py` ghi/đọc các ảnh nguồn trên.

## Bị đưa vào danh sách Content của MSBuild

Đã chạy `dotnet msbuild VanDao.csproj -getItem:Content,Compile,EmbeddedResource`:

- `build-check/`: **93 Content, 6,67 MB**.
- `publish/`: **13 Content, 0,91 MB**.
- `docs/`: **7 Content, 0,04 MB**.

`VanDao.csproj` hiện loại `tools/**` và `VanDaoLauncher/**`, nhưng chưa loại `build-check/**`, `publish/**`, `docs/**`. Những mục này lọt vào Content mặc định; đây là danh sách đánh giá project, chưa phải đo kích thước gói publish cuối cùng. Nên loại rõ khỏi Content/None để tránh đóng gói file phụ, và thêm `build-check/` vào `.gitignore`. `.gitignore` không tự loại các file đã được Git theo dõi.

## Những nhóm không nên xóa bằng tìm kiếm tên đơn thuần

- Không thấy JS/CSS nào thiếu tham chiếu tên file trong mã runtime; điều này không chứng minh toàn bộ hàm bên trong còn dùng.
- Pet, skin, sprite `_24`, `_48`, `_hd`, icon navigation có đường dẫn sinh động. Không được kết luận ảnh thừa chỉ vì không thấy đường dẫn đầy đủ.
- Có **4 nhóm ảnh trùng nội dung, khoảng 0,38 MB** có thể tiết kiệm. Các tên khác nhau vẫn có thể đang được gọi nên cần đổi tham chiếu trước khi gộp.
- `VanDaoLauncher.exe` ở root khoảng 3,11 MB là launcher, chưa có căn cứ coi là file thừa.

## Thứ tự dọn đề xuất

1. Loại `build-check/` khỏi source và cấu hình đóng gói; giữ bản chạy cần thiết nếu đang dùng.
2. Dọn bin/obj của các project khi game và công cụ đã dừng; không ảnh hưởng lịch sử Git vì chúng đã được ignore.
3. Chuyển ảnh nguồn, GIF, preview sang kho thiết kế, cập nhật đường dẫn công cụ.
4. Rà tài nguyên runtime còn nghi ngờ bằng chạy đủ cảnh giới, skin, pet trước khi xóa.

Chi tiết máy đọc: `obj/source-audit/audit.json`. Tạo lại bằng lệnh MSBuild ở trên (ghi JSON vào `obj/source-audit-msbuild.json`), rồi chạy `node tools/audit-source.cjs`. Script chỉ đọc source và ghi báo cáo trong obj. Danh sách `assetCandidates` là ứng viên cần rà thủ công, không phải danh sách an toàn để xóa tự động.
