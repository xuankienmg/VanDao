# Kiểm tra cục bộ

- Build Debug: 0 lỗi, 1 cảnh báo `MSB3277: Found conflicts between different versions of "WindowsBase" that could not be resolved.`
- C# regression: đạt kiểm tra lưu lựa chọn, schema save cũ, API state, khóa rương/cảnh giới, đổi bộ tương ứng, đầu vào sai và giữ tiến trình. Restore có cảnh báo `NU1900: Error occurred while getting package vulnerability data` vì không truy cập được nguồn NuGet.
- JavaScript: đạt kiểm tra chọn bộ theo sáu cảnh giới, khóa Hợp Thể nữ, bộ chọn riêng, lọc Nam/Nữ và cú pháp.
- Chrome headless: 42 file runtime tồn tại; ảnh trong wardrobe tải được; nút Nữ được chọn; không tràn ngang; cả sáu bộ vẽ được tư thế đứng và tung chiêu.
- Tách ảnh: cả 12 đường tách có `seamOpaquePixels = 0` tại ngưỡng alpha > 32. Mỗi ảnh đầu ra có khoảng đệm trong suốt.
- Đã xem ảnh tổng hợp và ảnh chụp wardrobe. Chưa chơi thử trực tiếp bản desktop.

Các bộ mới dùng tư thế tĩnh, chưa có chuyển động chạy/chém theo chuỗi khung hình riêng.
