# Bắt cập nhật phiên bản

Game đọc bảng `app_config` trên Supabase (cùng project với Bảng xếp hạng / Chợ) khi mở và mỗi 30 phút.

| Trạng thái | Khi nào | Game làm gì |
|---|---|---|
| `ok` | bản ≥ `latest_version` | chơi bình thường |
| `update` | `min_version` ≤ bản < `latest_version` | chơi được, hiện thanh "Đã có bản mới" + nút Tải (đóng thì không nhắc lại bản đó) |
| `required` | bản < `min_version` | màn hình chặn + nút Tải; server game trả 426 cho mọi thao tác nhân vật |
| `unknown` | mất mạng / chưa cấu hình | chơi offline được; Chợ và Bảng xếp hạng không dùng được |

Supabase cũng tự từ chối `submit_score` và mọi hàm `market_*` của bản thấp hơn `min_version` (game gửi header
`X-Game-Version`), nên bản cũ bị chặn tính năng online kể cả khi người chơi sửa giao diện.
Môi trường dev (`ASPNETCORE_ENVIRONMENT=Development`) không bị chặn chơi, chỉ hiện trạng thái.

## Cài đặt

Chạy `supabase.sql` trong thư mục này, rồi chạy lại `docs/leaderboard/supabase.sql` và `docs/market/supabase.sql`
(hai file đó đã gọi `require_version()`).

## Quy trình phát hành bản mới

Một workflow duy nhất `.github/workflows/build-release.yml` thực hiện tuần tự:

1. Kiểm tra tag `vX.Y.Z` và hai secret Supabase.
2. Build game đúng số phiên bản của tag và Launcher cho Windows x64, kèm .NET runtime (`--self-contained true`), single-file có nén (`-p:EnableCompressionInSingleFile=true`). Người chơi không cần cài .NET riêng. WebView2 Runtime vẫn là phụ thuộc riêng của giao diện game và Launcher.
3. Đóng gói game, Launcher và giao diện vào `VanDao_vX.Y.Z.zip`, upload GitHub Release.
4. Xác nhận asset ZIP đã upload, rồi mới cập nhật dòng `app_config.id = 1`: `latest_version`, `download_url` (link ZIP GitHub), `message` (nội dung tag). Không tự sửa `min_version`. Không ghi đè thông báo bản mới hơn bằng bản cũ.

Cách phát hành, thay `0.7.5` bằng phiên bản mới:

```powershell
git tag -a v0.7.5 -m "Sửa lỗi và cải tiến game"
git push origin v0.7.5
```

Push `master` chỉ cập nhật mã nguồn, không phát hành. Có thể chạy lại workflow bằng **Run workflow**, nhập tag đã tồn tại như `v0.7.5`; workflow checkout đúng tag đó. Nếu upload xong nhưng đồng bộ Supabase lỗi, sửa secret/cấu hình rồi chạy lại workflow cho cùng tag.

Cài đặt một lần: GitHub → Settings → Secrets and variables → Actions: thêm `SUPABASE_URL` và `SUPABASE_SERVICE_ROLE_KEY`. Chạy SQL tạo `app_config` trước. Repo phát hành cần public để Launcher và nút Tải trong game truy cập ZIP không cần token. Có thể giữ mã nguồn private và đặt các gói ZIP ở một repo phát hành public; cấu hình Repository của Launcher trỏ tới repo đó.

Launcher kiểm tra GitHub Releases để tải/cài ZIP; game đọc Supabase để thông báo và kiểm soát phiên bản. Khi muốn bắt buộc cập nhật, sau khi release hoàn tất hãy đặt `min_version` bằng phiên bản tối thiểu cần hỗ trợ trong Supabase. Backend cache 15 phút, làm mới nền khi có yêu cầu; giao diện kiểm tra định kỳ 30 phút hoặc khi mở game. Nút Kiểm tra lại bỏ qua cache.

Dữ liệu người chơi ở `%LocalAppData%\VanDao\vandao.db`, giữ nguyên khi thay bản game.

## Giới hạn

- Chỉ áp dụng từ bản có tính năng này trở đi. Các bản đã phát trước đó không có đoạn kiểm tra nên vẫn chạy được
  offline — nhưng không dùng được Chợ / Bảng xếp hạng (không gửi `X-Game-Version`).
- Người chơi tắt mạng thì vẫn chơi offline được bản cũ (theo lựa chọn "vẫn cho chơi, khóa tính năng online").

Secrets có thể đặt trong Environment tên `Supabase` (workflow đã khai báo `environment: Supabase`) hoặc Repository secrets. Nếu Environment có quy tắc duyệt triển khai, cần duyệt job trước khi workflow được dùng secrets.

## Token GitHub trong Launcher

Launcher không nhúng, nhận từ giao diện hoặc lưu GitHub token. File cấu hình cũ được ghi lại khi mở Launcher để loại trường token cũ. Token dùng cho GitHub Actions chỉ nằm trong môi trường workflow, không đưa vào ZIP.

Nếu token từng nằm trong mã hoặc EXE, phải thu hồi token đó trong GitHub Settings → Developer settings → Personal access tokens. Xóa khỏi mã không vô hiệu hóa token trong lịch sử Git hoặc bản phát hành cũ.
