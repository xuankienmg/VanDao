# Bộ kỹ năng linh thú đã duyệt

Mỗi loài có một thiên phú khi xuất chiến và một kỹ năng theo ba hình thái. Mỗi sao tăng 6% chỉ số, hệ số kỹ năng và thiên phú so với mức 0 sao, tối đa 30% ở 5 sao. Mô tả trong chuồng hiển thị duy nhất mức hiệu lực hiện tại của hình thái và sao; bảng bên dưới là số gốc ở 0 sao. Số đòn, chu kỳ, thời gian hiệu lực, ngưỡng kích hoạt và trần bảo vệ không tăng theo sao. Sát thương chủ động tính từ công chủ nhân, không bạo kích, không kích hoạt liên kích. Các đòn nhiều lần trừ phòng thủ riêng từng lần; kỹ năng hút máu tính từ sát thương thực, không tính phần dư khi mục tiêu hết máu.

| Loài | Thiên phú | Kỹ năng theo Ấu thể / Thành thể / Thần thú | Chu kỳ |
|---|---|---|---|
| Hỏa Linh Hồ | +4% bạo kích | 70/105/140% công; đánh dấu hai lượt kế, sát thương bạo kích chủ nhân +5/8/12% | 5 |
| Băng Tinh Thố | Giảm 4% sát thương nhận | Khiên 6/9/12% máu; giảm công địch 5/8/12% trong hai lượt | 5 |
| Thanh Mộc Lộc | Dưới 50% máu: hiệu quả hồi máu +15% | Hồi 5/8/11% máu; hồi thêm 1/1,5/2% trong mỗi lượt của hai lượt kế | 4 |
| Kim Giáp Quy | +10% phòng thủ | Khiên 8/12/16% máu; khi khiên linh thú còn giảm thêm 3/5/7% sát thương nhận | 5 |
| Lôi Vân Điêu | +5% liên kích | Ba đòn, mỗi đòn 25/38/52% công | 4 |
| Bích Thủy Giao | +10% tốc độ thu tu vi | 65/100/135% công; hút 20/25/30% sát thương, giới hạn 4/6/8% máu | 4 |
| Thanh Phong Hạc | +5% thân pháp thám hiểm | Hồi 4/6/8% máu; né tránh +5/8/12% trong hai lượt | 4 |
| Tử Điện Miêu | +8% xuyên thủ | 90/135/180% công; bỏ qua thêm 15/25/35% phòng thủ còn lại | 4 |
| Chu Tước | Sát thương chủ nhân lên mục tiêu cháy +10% | 80/120/160% công; cháy 15/25/35% công trong mỗi lượt của hai lượt kế | 3 |
| Huyền Vũ | Khiên đầu trận 10% máu | Khiên 10/15/20% máu; phản 15/25/35% công khi bị đánh trong hai lượt | 4 |
| Thanh Long | +8% công | Ba tia sét, mỗi tia 35/55/75% công; dạng Thần thú tia cuối bỏ qua 20% phòng thủ còn lại | 4 |
| Bạch Hổ | Sát thương bạo kích +20 điểm phần trăm | 110/165/220% công; mục tiêu dưới 30% máu chịu thêm 25% sát thương chiêu | 4 |

Khiên linh thú tách khỏi khiên trang bị, tối đa 30% máu chủ nhân. Giảm sát thương và né tránh chịu trần chiến đấu hiện hành. Hiệu ứng cùng loại làm mới, không cộng dồn. Hiệu ứng trên mục tiêu (đánh dấu, giảm công, cháy) xóa khi chuyển đợt quái; hiệu ứng bảo vệ chủ nhân giữ thời hạn còn lại. Hồi phục không hồi sinh và không vượt máu tối đa.

Thanh Long giữ nguyên ID, cấp, phẩm, sao, tiến hóa và ngoại hình đã chọn của dữ liệu cũ; thay hệ Mộc bằng Lôi. Hình mới ở `wwwroot/img/pet/thanh_long_loi_*`, nguồn và tuyến tạo ảnh ở `tools/pet_redesign/art-manifest.json`. Ba hình là chân dung tĩnh, không phải bộ animation mới.

Kiểm tra: `dotnet run --project tools/pet_skill_checks -- obj/pet-skill-ui.json`, dùng đường dẫn tuyệt đối cho file fixture. Kiểm tra UI: `node tools/pet_skill_checks/ui.cjs <đường dẫn playwright>`.
