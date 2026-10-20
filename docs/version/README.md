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

1. Commit code của bản mới, rồi đánh tag (chưa cần push):
   ```
   git tag -a v0.7.3 -m "Sửa lỗi Chợ, thêm Hợp Thể"
   ```
   Không cần sửa `<Version>` trong `VanDao.csproj`: lúc build, target `VersionFromGitTag` lấy số từ tag gần nhất
   (`git describe`), nên file .exe mang đúng 0.7.3. Dòng "Phiên bản game: 0.7.3" hiện trong log build để kiểm tra.
2. Publish, nén và đưa lên Google Drive / OneDrive (link "Bất kỳ ai có link"). Link mới thì sửa `download_url` trong `app_config`.
3. Push tag **sau khi** file đã tải được: `git push origin v0.7.3`. GitHub Actions (`.github/workflows/release-version.yml`)
   tự đặt `latest_version = 0.7.3` và `message` = dòng đầu nội dung tag.
4. Khi muốn bắt buộc mọi người lên bản mới: đặt `min_version = 0.7.3` bằng tay trong Supabase. Game bản cũ đang mở sẽ bị chặn
   trong tối đa 15 phút (hoặc ngay lần mở tiếp theo).

Cài workflow một lần: GitHub → repo → Settings → Secrets and variables → Actions → thêm `SUPABASE_URL` và
`SUPABASE_SERVICE_ROLE_KEY` (Supabase → Project Settings → API → khóa `service_role`; khóa này toàn quyền DB, chỉ để ở secret,
không bao giờ cho vào game hay appsettings.json).

Tiến trình người chơi lưu ở `%LocalAppData%\VanDao\vandao.db` nên giữ nguyên khi đổi sang bản mới.

## Giới hạn

- Chỉ áp dụng từ bản có tính năng này trở đi. Các bản đã phát trước đó không có đoạn kiểm tra nên vẫn chạy được
  offline — nhưng không dùng được Chợ / Bảng xếp hạng (không gửi `X-Game-Version`).
- Người chơi tắt mạng thì vẫn chơi offline được bản cũ (theo lựa chọn "vẫn cho chơi, khóa tính năng online").
