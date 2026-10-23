# Hiệu ứng phụ của chiêu chủ động

Yêu cầu tiếp nối: ngoài sát thương, mỗi chiêu chủ động có xác suất gây hiệu ứng phụ. Áp dụng cho cả bảy hệ; đòn thường/bị động không gieo hiệu ứng này.

## Quy tắc

- 10% mỗi cấp, giới hạn cấp hợp lệ: Lv.1 10% → Lv.5 50%, kế thừa tỉ lệ Hỏa hiện có.
- Hỏa: thiêu đốt 5% máu tối đa quái mỗi lượt, hai lượt gồm lượt kích hoạt; giữ logic không đốt hai lần/lượt.
- Thủy: giảm 20% công quái trong hai lượt gồm lượt thi triển.
- Mộc: cơ hội hồi 5% máu tối đa cuối lượt thi triển nếu còn sống; cộng với hồi phục hiện có rồi giới hạn máu tối đa, không hồi sinh.
- Thổ: choáng một lượt. Lôi: tê liệt một lượt. Băng: đóng băng một lượt và lượt sau nhận thêm 10% sát thương nếu chưa bị hiệu ứng khác thay thế.
- Phong: tăng 20 điểm phần trăm né tránh trong hai lượt gồm lượt thi triển, tuân giới hạn né hiện có.
- Debuff của quái dùng quy tắc hiện có: mới nhất thay cũ, kể cả khi do linh thú gây; không cộng dồn. Buff Phong thuộc tu sĩ, độc lập debuff quái, không cộng dồn khi kích hoạt lại.
- Sang đợt quái xóa debuff quái; buff tu sĩ chỉ tồn tại đúng thời gian. Không gây debuff lên quái đã bị chiêu kết liễu.
- Kết quả xác suất tính ở server bằng seed trận; client chỉ hiển thị sự kiện và trạng thái. Metadata hiệu ứng dùng chung cho API và tooltip để không lệch mô tả/tỉ lệ.

## Triển khai và kiểm chứng

- [x] Test đỏ metadata cho mọi chủ động; biên xác suất/cấp, thời hạn, không cộng dồn, thay debuff, buff khác đợt.
- [x] Thêm SkillSecondaryEffects và SkillEffectCombat; nối trận thật, CastDto sự kiện, mảng trạng thái và sampling.
- [x] Hiển thị tooltip, nhãn kích hoạt, suy yếu/né và mốc xuất hiện khống đúng lúc chiêu trúng.
- [x] Kiểm tra trận thật có/không linh thú, heal không hồi sinh, determinism, tương thích API; chạy suite công pháp/linh thú và browser/build riêng.
- [x] Reviewer độc lập và báo cáo bằng chứng. Không sửa save hoặc ghi đè thay đổi đồng thời của người dùng.
