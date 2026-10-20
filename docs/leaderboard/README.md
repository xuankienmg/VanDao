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
