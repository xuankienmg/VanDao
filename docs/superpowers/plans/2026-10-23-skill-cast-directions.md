# Hướng thi triển kỹ năng

Yêu cầu tiếp nối của người dùng: chiêu chủ động có thể chưởng ngang, từ trên rơi xuống, từ dưới lên hoặc bùng phát giữa quái. Triển khai tiếp theo phạm vi bốn hệ vừa hoàn thiện.

## Thiết kế

- Mỗi chiêu khai báo castDirection: left, above, below, center. Đòn thường vẫn bay ngang; chủ động gán theo hình tượng chiêu. Giữ motion cũ để tương thích dữ liệu cũ.
- Tụ lực tại nguồn; di chuyển trong pha .25–.45; chạm mục tiêu ở .45; tan theo timeline hiện tại. Center đứng tại tâm quái và tăng kích thước qua pha tụ/thi triển.
- Khi di chuyển dọc, chỉ xoay frame phóng của các chiêu tia/thương có hình hướng ngang; không xoay sơn ấn hoặc địa thứ dựng đứng.
- Dùng tâm vùng hình làm neo cho hướng mới, fit vùng hình sau xoay vào canvas. Giảm chuyển động giữ vị trí tại mục tiêu, vẫn giữ frame báo trúng.
- Metadata và runtime dùng chung giữa chiến đấu/preview. Ảnh PNG/WebP gốc không cần thay; các clip độc lập chỉ thể hiện hình trong ô, hướng di chuyển được thể hiện trên cảnh runtime.
- Manifest Hỏa/Thủy/Mộc không có castDirection tiếp tục đường vẽ cũ.

## Các bước

- [x] Thêm test đỏ cho bốn quỹ đạo, va chạm đồng nhất, không clipping khi xoay và reduced motion.
- [x] Khai báo hướng trong catalog/manifest và bộ xuất; thêm phép tính vị trí/kích thước/xoay vào renderer chung.
- [x] Cập nhật preview hiển thị hướng; kiểm tra browser trên cảnh thật và viewport nhỏ.
- [x] Chạy kiểm tra cũ/mới, build riêng và rà soát thay đổi; ghi kết quả.

Không thay chỉ số, nộ, cơ chế sát thương hay save. Giữ nguyên những thay đổi khác đang có trong workspace.
