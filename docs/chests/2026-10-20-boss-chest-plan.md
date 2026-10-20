# Plan rương Boss

Trạng thái: đã áp dụng vào game ngày 20/10/2026. Bảng trước/sau và mô phỏng đầy đủ nằm trong [plan tổng](2026-10-20-drop-rate-proposal.md); kết quả số trong [simulation JSON](2026-10-20-simulation.json).

## Mục tiêu

Rương Boss có phân bố phẩm tốt hơn rương hiếm ở mọi cảnh giới; không làm mất giá rương hiếm. Phẩm tối đa tăng theo tiến trình: Thần từ Nguyên Anh, Tiên/Đạo từ Luyện Hư. Chưa mở Huyền Tiên.

## Nhóm phần thưởng — cập nhật linh thảo và Ngộ Đạo Đan

- Pháp bảo: 40%.
- Trang bị: 36% (60% của phần 60% còn lại).
- Linh thảo: 15%, nhận 4–6 món cùng loại theo chương tương ứng cảnh giới.
- Ngộ Đạo Đan: 5% từ Nguyên Anh, nhận 1 viên; trước Nguyên Anh là 0%.
- Đan lên tầng: 4% từ Nguyên Anh, 9% trước Nguyên Anh; nhận 4–7 viên khi trúng nhánh này (trước đây nhánh đan chiếm 24%).
- Mỗi rương nhận đúng một trong năm nhóm thưởng. Không nhận linh thảo/Ngộ Đạo Đan cộng thêm ngoài trang bị. Tổng các nhóm luôn bằng 100%.
- Giữ số rương Boss theo độ khó; không đổi tỉ lệ rơi rương hoặc chi phí vào Boss.

## Phẩm đề xuất khi nhận trang bị/pháp bảo

| Cảnh giới | Phân bố phẩm (%) |
|---|---|
| Luyện Khí | Linh 45; Huyền 55 |
| Trúc Cơ | Huyền 65; Địa 35 |
| Kim Đan | Huyền 25; Địa 68; Thiên 7 |
| Nguyên Anh | Huyền 8; Địa 46; Thiên 36; Thánh 9; Thần 1 |
| Hóa Thần | Địa 30; Thiên 50; Thánh 17; Thần 3 |
| Luyện Hư | Địa 20; Thiên 49; Thánh 24; Thần 6; Tiên 0,9; Đạo 0,1 |
| Hợp Thể | Địa 10; Thiên 40; Thánh 35; Thần 12; Tiên 2,7; Đạo 0,3 |

Các dòng tổng đúng 100%. Đây là xác suất có điều kiện, không phải trên mọi lần mở rương. Tỉ lệ Đạo trên mọi rương Boss: Luyện Hư **0,076%**, Hợp Thể **0,228%**. So với rương hiếm, Đạo cao gấp 10 lần trong nhánh trang bị/pháp bảo; cao khoảng 12,67 lần trên mọi rương vì nhóm trang bị/pháp bảo của rương Boss chiếm 76% thay vì 60%.

## Giá bán và cân bằng

Áp dụng hệ số giá trang bị từ plan tổng: Thánh ×1,25; Thần ×1,5; Tiên ×1,75; Đạo ×2; Huyền Tiên ×2,5. Không tăng giá đan/nguyên liệu theo hệ số này.

Giá bán NPC kỳ vọng được mô phỏng lại trong plan tổng sau khi thêm nhánh linh thảo và Ngộ Đạo Đan. Không dùng lại kết quả so sánh cũ của phương án ba nhóm thưởng. Đây là chỉ số tham chiếu, không phải đánh giá toàn bộ giá trị sử dụng, đặc biệt Ngộ Đạo Đan cho điểm công pháp vĩnh viễn. Kiểm tra cả tổng tiền mỗi chuyến Boss ở Khó/Ác mộng trước khi phát hành.

## Triển khai sau khi duyệt

1. Thay bảng Quality riêng của rương Boss, đủ dòng cho từng cảnh giới; không dùng lại ngầm dòng Hóa Thần cho Luyện Hư/Hợp Thể.
2. Dùng chung bảng giữa OpenOne và ChestOdds; hiển thị rõ nhánh thưởng và xác suất phẩm.
3. Kiểm tra các ngưỡng chọn phẩm để không vô tình cho ra Thần/Tiên/Đạo sớm hoặc Huyền Tiên.
4. Kiểm tra mở đơn, mở nhiều, túi đầy hoàn rương, đúng cấp/bộ, đúng lượng đan và không trừ rương khi thất bại.
5. Kiểm tra giá bán từng phẩm, tổng tiền không tràn số; đồ cũ dùng cùng giá mới, không tăng giá vật phẩm không phải trang bị.
6. Thêm cấu hình nhóm thưởng độc lập cho linh thảo/Ngộ Đạo Đan; không làm giảm xác suất trang bị/pháp bảo khi thêm nhánh. Ngộ Đạo Đan không rơi trước Nguyên Anh. Kiểm tra pool linh thảo không rỗng, không có thảo dược chương tương lai; không dùng điều kiện ải để loại linh thảo khỏi rương.

## Đã kiểm chứng ở bước plan

- Mô phỏng 100.000 lần trước và 100.000 lần sau cho mỗi cảnh giới; 1,4 triệu lượt riêng Boss, 4,2 triệu lượt tổng ba loại rương.
- Tổng xác suất từng dòng bằng 100%; phân bố phẩm Boss tốt hơn rương hiếm ở mọi ngưỡng phẩm.
- Giá trị NPC kỳ vọng Boss cao hơn rương hiếm ở cả bảy cảnh giới.
- Với phần thưởng cực hiếm, dùng xác suất lý thuyết và kiểm tra ngưỡng làm chuẩn; không suy ra tỉ lệ chính xác chỉ từ số lần trúng trong mô phỏng.

## Xác nhận triển khai

Đã áp dụng bảng phẩm, năm nhóm thưởng và giá bán trang bị. `ChestRewards` là nguồn chung cho mở rương và bảng tỉ lệ; không thay đổi tỉ lệ rương rơi khi đánh quái. Báo cáo JSON lưu kết quả mô phỏng trước triển khai, không phải dữ liệu lượt mở của người chơi.
