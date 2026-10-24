# Trang phục nữ từ Luyện Khí đến Luyện Hư

Sáu bộ theo thứ tự cảnh giới:

| Cảnh giới | Trang phục | Phối màu |
| --- | --- | --- |
| Luyện Khí | Thanh Sam Nữ Đệ Tử | Xám lam, tóc đen, ít trang sức |
| Trúc Cơ | Bích Vân Tiên Tử | Xanh thẫm, ngân vân, ngọc |
| Kim Đan | Bạch Kim Tiên Nương | Trắng, hắc kim, tóc bạc |
| Nguyên Anh | Tử Hà Tiên Cơ | Tím nhạt, trắng, ngân vân |
| Hóa Thần | Nguyệt Hoa Thần Y | Nguyệt bạch, băng lam, vàng |
| Luyện Hư | Thương Long Tiên Cơ | Lam thẫm, kim long, kim quan |

Hình được tạo bằng công cụ image generation tích hợp, dùng tạo hình nữ Hợp Thể hiện có làm tham chiếu nhân vật trưởng thành và phong cách. Không thay ảnh Hợp Thể. Thông tin model/chi phí không được công cụ cung cấp.

Mỗi bộ có ba **tư thế tĩnh**: đứng, ngồi thiền, tung chiêu. Chưa có chuỗi khung hình chạy/chém riêng. Hiệu ứng kỹ năng và thời điểm sát thương dùng luồng hiện có của game.

Nguồn ảnh và bộ prompt nằm trong `sources.json`, `prompts.json` và các file `*-source.png`; ảnh tổng hợp là `female-realms-preview.jpg`. `export-qa.json` ghi kích thước và số pixel đục bị đường tách đi qua. Ảnh game nằm trong `wwwroot/img`, tư thế tung chiêu riêng ở `wwwroot/img/skin`.

Trong game: **Hành trang → Trang phục → Nam / Nữ**. Tất cả bảy trang phục nữ chỉ mở bằng rương tương ứng, không tự mở theo cảnh giới hay nút chuyển giới tính. Hiện không có nguồn nhận rương trong gameplay, không mua bán/giao dịch; có thể cấp thủ công bằng công cụ admin. Mở rương tiêu hao một rương, mở khóa vĩnh viễn và mặc được ở mọi cảnh giới. Chọn **Theo cảnh giới** chỉ tự chọn trong các bộ nữ đã mở khóa; nếu chưa mở bộ nào thì dùng ngoại hình cảnh giới mặc định. Chuyển giới tính không đổi chỉ số hay tiến trình.

Kiểm tra phát triển nằm trong `build-check/gender-checks`: kiểm tra lưu/đọc, nâng schema save cũ, API state, khóa cảnh giới/rương, đổi bộ tương ứng, chọn bộ tự động, cú pháp JS và render bảng trang phục/tư thế trong trình duyệt headless. Đây là kiểm tra cục bộ, chưa thay thế chơi thử trực tiếp trong ứng dụng desktop.
