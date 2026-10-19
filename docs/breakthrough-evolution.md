# Hoạt ảnh tích lũy theo cảnh giới

Đã áp dụng vào tab Đột phá ngày 19/10/2026. Dùng sprite ngồi thiền và trang phục đang có; các lớp hiệu ứng bổ sung không thay đổi luật đột phá.

| Cảnh giới | Chi tiết mới, giữ lại ở các cảnh giới sau |
|---|---|
| Luyện Khí | Linh tức theo nhịp thở, thân người chuyển động nhẹ |
| Trúc Cơ | Cánh sen linh lực dưới chân |
| Kim Đan | Kim đan phát sáng ở đan điền |
| Nguyên Anh | Linh ảnh nhỏ của chính nhân vật sau vai |
| Hóa Thần | Ba đạo kiếm khí quay hộ thân |
| Luyện Hư | Hai vòng không gian sau lưng |

Mỗi tầng nhỏ thắp thêm một hạt trên vòng tiến cảnh. Số hạt theo số tầng thực của cảnh giới. Bảng tên ghi số dấu ấn và chi tiết mới nhất; tooltip liệt kê các dấu ấn đã có.

Khi tụ lực, nhân vật nhấc nhẹ và linh khí hội tụ. Sau khi server xác nhận thành công, chi tiết của cảnh giới mới hiện dần trước khi bảng kết quả mở. Thất bại chỉ có hiệu ứng suy yếu, không thêm dấu ấn. Đột phá tầng không thêm chi tiết đại cảnh giới. Giữ cơ chế khóa nút và thời gian áp dụng state hiện có.

Hạt linh khí di chuyển theo delta thời gian, tránh tốc độ phụ thuộc FPS. Chế độ giảm chuyển động giữ nguyên chi tiết, dừng nhịp thở, xoay, hạt chuyển động, nở halo và tia lóe.

Kiểm chứng:

- `node tools/breakthrough_checks/check.cjs`: 240 khung hình tổ hợp cảnh giới/tầng/giai đoạn; hình học hữu hạn, canvas save/restore cân bằng, mở chi tiết khi thành công, không mở khi thất bại, idle giảm chuyển động ổn định.
- Kiểm tra trình duyệt với sprite thực: đủ sáu cảnh giới, tăng tầng, diễn thử thành công/thất bại và giảm chuyển động. Preview dùng dữ liệu giả, không gọi API thay đổi nhân vật.
- Preview: chạy `node tools/sell_checks/preview.cjs`, mở `http://127.0.0.1:4175/breakthrough`.
- Build Release trong `bin/budget-check` thành công; còn cảnh báo WindowsBase/WebView2 hiện có.

Ảnh xem trước: `breakthrough-evolution-luyenhu.jpg` cùng thư mục.
