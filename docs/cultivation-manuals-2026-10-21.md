# Bí kíp mở cây công pháp

Cập nhật: hiện chỉ mở Hỏa, Thủy, Mộc. Icon vật phẩm dùng sách bí kíp `wwwroot/img/bi_kip_cong_phap.png`, được tạo bằng imagegen, với bộ lọc màu riêng: Hỏa đỏ cam, Thủy xanh lam, Mộc xanh lục. Thổ/Lôi/Băng/Phong giữ định nghĩa và dữ liệu cũ nhưng không thể học, không thể trang bị, không áp dụng chỉ số và không rơi từ rương trong phiên bản hiện tại.

| Hệ | Vật phẩm | ID |
|---|---|---|
| Hỏa | Xích Diễm Phần Thiên Kinh | bi_kip_hoa |
| Thủy | Thương Hải Huyền Thủy Kinh | bi_kip_thuy |
| Mộc | Thanh Đế Trường Sinh Kinh | bi_kip_moc |
| Thổ | Hậu Thổ Trấn Sơn Kinh | bi_kip_tho |
| Lôi | Cửu Tiêu Ngự Lôi Kinh | bi_kip_loi |
| Băng | Huyền Băng Ngưng Thiên Kinh | bi_kip_bang |
| Phong | Thái Hư Ngự Phong Kinh | bi_kip_phong |

Dùng một bí kíp trong hành trang để mở vĩnh viễn cây tương ứng cho nhân vật. Học kỹ năng vẫn cần điểm, tiền đề và cảnh giới. Dùng trùng bị từ chối và giữ vật phẩm. Tẩy Tủy Đan không xóa quyền mở cây. Tu lại từ đầu xóa quyền mở cây và trả một bí kíp Hỏa chưa học.

Nhân vật mới bắt đầu với bí kíp Hỏa trong túi. Nhân vật cũ được chuyển đổi một lần: giữ Hỏa bẩm sinh và mọi hệ có kỹ năng đã học hoặc trang bị. Dữ liệu lưu riêng ở LearnedManuals, ManualVersion; SchemaUpgrade thêm cột cho DB cũ. API nâng và trang bị kiểm tra quyền mở cây, trạng thái trả unlockedElements cho giao diện.

Rương Boss có tổng xác suất 1% nhận đúng một bí kíp trong ba hệ Hỏa/Thủy/Mộc, mỗi hệ 1/300 (~0,333333%) mỗi rương, không loại bí kíp trùng. Rương thường/hiếm không rơi. Nhóm công pháp lấy ngân sách từ trang bị, đan tầng và linh thảo; giữ nguyên tỷ lệ Ngộ Đạo Đan và linh thú. Mở đơn/nhiều vẫn dùng chung bảng ChestRewards; hành trang đầy giữ rương theo luồng hiện có.

Mộc có 1 đòn thường, 7 chủ động và 11 bị động; Hồi Xuân hồi máu sau lượt tung chiêu Mộc, Khô Mộc Phùng Xuân hồi máu một lần mỗi trận khi dưới 30% máu và còn sống. Thủy Linh Chân Ý có 5 cấp, mỗi cấp +3% sát thương, −1% công quái, +2% máu. Thổ/Lôi/Băng/Phong giữ nguyên khung cây chưa mở.

Kiểm tra: tools/manual_checks dùng SQLite bộ nhớ, kiểm tra quyền truy cập, tiêu hao, trùng lặp, Tẩy Tủy, reset, lưu/nạp, chuyển đổi nhân vật và nâng cấp schema, bảng xác suất theo cảnh giới/chương/độ khó, và đường roll tới từng bí kíp.

Kết quả: PASS 2.769 kiểm tra; JavaScript qua kiểm tra cú pháp. Build chính thành công, còn cảnh báo WindowsBase/WebView2; NuGet không tải được dữ liệu kiểm toán lỗ hổng trong môi trường mạng hiện tại.
