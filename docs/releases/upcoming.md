# Nội dung cập nhật

- Tách giới hạn quay mỗi ngày: 1.000 lượt trang bị và 300 lượt trứng linh thú. Bộ đếm độc lập, đặt lại 00:00 giờ Việt Nam, giữ hạn mức khi trùng tu.
- Launcher hiển thị tên, ngày và ghi chú GitHub Release ngay cả khi đã cài bản mới nhất; bỏ nội dung mẫu cũ và bổ sung trạng thái khi mất mạng/chưa có ghi chú.
- Thêm trang phục nữ Hợp Thể Thiên Hợp Tiên Cơ cùng ảnh đứng, thiền, chiến đấu và bay. Mở khi đạt Hợp Thể.
- Đổi icon Trang phục; giữ riêng trạng thái đóng/mở các bảng chi tiết trong Hành trang khi cập nhật giao diện.
- Admin dev được miễn cảnh báo dữ liệu và có thể gỡ cờ cho bản thân hoặc người khác trên Thiên bảng. Không xóa lịch sử từng sử dụng admin.
- Thêm Thiên bảng Vượt ải (độ khó → chương → ải đã hoàn thành) và Linh thú (lực chiến linh thú mạnh nhất).

## Khi triển khai

1. Chạy lại `docs/leaderboard/supabase.sql` trên Supabase SQL Editor, sau đó mở lại game để dùng hai bảng mới. Hai bảng Cảnh giới/Lực chiến vẫn tương thích schema cũ.
2. Chức năng gỡ cờ online chỉ dùng trong dev và cần biến môi trường server `SUPABASE_SERVICE_ROLE_KEY`; không đặt khóa này trong game phát hành hoặc Launcher.
3. Gỡ cờ người khác trên Supabase không tự sửa DB trên máy họ; nếu máy còn cờ cũ, lần đồng bộ sau có thể gắn cờ lại.
4. Người chơi cần mở bản mới để gửi điểm Vượt ải/Linh thú. Người chưa gửi hiển thị điểm 0.

## Kiểm tra

Build Debug thành công (còn cảnh báo WindowsBase/WebView2). Đạt 14 kiểm tra lượt quay, 11 kiểm tra admin, 24 kiểm tra tính điểm/kết nối giả lập/tương thích bảng xếp hạng; 168 trường hợp khung hoạt ảnh và kiểm tra idle của sáu trang phục. Chưa chạy migration hoặc gỡ cờ trên Supabase thực tế.
