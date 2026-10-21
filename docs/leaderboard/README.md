# Bảng xếp hạng online (Supabase)

Game gửi tiến độ của nhân vật lên một bảng Postgres trên Supabase; tab **Xếp hạng** đọc lại top 50 theo
**Cảnh giới** (cảnh giới → tầng → tu vi) và **Lực chiến** (cùng công thức với ô "Lực chiến" trong Hành trang).

## Cài đặt một lần

1. Tạo project miễn phí tại https://supabase.com (chọn region Singapore cho gần).
2. Vào **SQL Editor** → dán toàn bộ `supabase.sql` → **Run**.
3. Vào **Project Settings → API**, chép **Project URL** và khóa **anon public**.
4. Điền vào `appsettings.json` (nằm cạnh file chạy):

   ```json
   "Leaderboard": {
     "Url": "https://xxxxxxxx.supabase.co",
     "AnonKey": "eyJhbGciOi..."
   }
   ```

   Bỏ trống thì bảng xếp hạng tắt, game vẫn chạy bình thường.

## Cách hoạt động

- Mỗi lần lưu nhân vật, server game gửi điểm chạy nền: khi đổi cảnh giới / tầng / lực chiến / đạo hiệu
  (tối đa 1 lần mỗi 15 giây), còn không thì 3 phút một lần để cập nhật tu vi. Mất mạng không ảnh hưởng game.
- Mỗi nhân vật có một khóa bí mật ngẫu nhiên (`Players.LbSecret`) lưu trong `vandao.db` của máy đó;
  Supabase chỉ giữ SHA-256 của khóa, nên người khác dùng anon key cũng không ghi đè được dòng của bạn.
  Tu lại từ đầu vẫn giữ khóa (ghi đè đúng dòng cũ).
- Anon key chỉ đọc được view `leaderboard_public` và gọi 2 hàm `submit_score` / `remove_score`;
  không đọc hay sửa bảng gốc trực tiếp được.
- **Admin mode**: dùng bất kỳ lệnh admin nào (hoặc đang bật cờ / còn điểm admin) → `Players.Tainted = true`,
  dòng của nhân vật bị xóa khỏi bảng và không gửi điểm nữa. Reset nhân vật về từ đầu khi **không** bật cờ
  admin nào thì được tham gia lại.
- Môi trường dev (`ASPNETCORE_ENVIRONMENT=Development`, có sẵn trong launchSettings) bỏ qua cờ admin: nhân vật
  thử nghiệm vẫn gửi điểm. Cờ `Tainted` vẫn được ghi vào DB nên bản publish (Production) vẫn loại nhân vật đó.
- Người chơi đổi **đạo hiệu** (2–20 ký tự) ngay trong tab Xếp hạng.

## Giới hạn cần biết

- Luật chơi chạy trên máy người chơi, nên người rành kỹ thuật vẫn có thể sửa `vandao.db` hoặc gọi thẳng
  `submit_score` với số liệu giả. Hàm SQL chỉ chặn giá trị vô lý (cảnh giới/tầng 0–50, lực chiến ≤ 10^12).
  Muốn chống gian lận chặt hơn cần server game chạy luật chơi (online thật).
- Xóa thủ công một dòng gian lận: Supabase → Table Editor → `leaderboard`.

## Bảng Vượt ải và Linh thú

Chạy lại toàn bộ `docs/leaderboard/supabase.sql` trong Supabase SQL Editor, rồi khởi động lại game. Script bổ sung bốn cột cuối view `leaderboard_public`: `progress`, `pet_power`, `progress_rank`, `pet_rank`. Không đổi chữ ký RPC hoặc xóa dữ liệu cũ.

- **Vượt ải**: độ khó cao hơn → chương cao hơn → số ải đã hoàn thành. Ải vừa mở chưa được tính; Boss chỉ tính sau khi hạ. Điểm mã hóa `độ khó × 10000 + chương (từ 0) × 100 + ải hoàn thành`.
- **Linh thú**: lực chiến của linh thú mạnh nhất đang sở hữu, dùng công thức lực chiến từ chỉ số thú (không cộng tổng chuồng, không phụ thuộc vị trí xuất chiến).
- Cùng điểm: lực chiến nhân vật rồi ID người chơi để thứ tự ổn định. Cờ dữ liệu và quy tắc top 10 giữ như hai bảng cũ.
- Điểm gửi trong `p_stats.progress` và `p_stats.pet_power`; bản game cũ chưa gửi các trường này hiển thị 0. Thứ hạng cập nhật sau khi người chơi mở bản mới và đồng bộ điểm.
- Nếu SQL chưa cập nhật, hai bảng cũ tiếp tục dùng được; hai tab mới hiện hướng dẫn chạy SQL. Sau khi cập nhật SQL hãy mở lại game.
