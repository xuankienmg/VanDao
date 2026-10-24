# Hiệu ứng phụ chủ động — 2026-10-23

## Kết quả

49 chiêu chủ động của cả bảy hệ có thêm xác suất kích hoạt hiệu ứng phụ, ngoài sát thương. Lv.1 10%, thêm 10 điểm phần trăm mỗi cấp, tối đa Lv.5 50%; đòn thường và bị động không gieo hiệu ứng này.

- Hỏa: thiêu đốt 5% máu tối đa quái mỗi lượt, hai lượt gồm lượt kích hoạt; không đốt hai lần trong cùng lượt.
- Thủy: giảm 20% công quái trong hai lượt gồm lượt thi triển.
- Mộc: hồi 5% máu tối đa cuối lượt thi triển nếu còn sống; không vượt máu tối đa hoặc hồi sinh. Cộng với hồi phục sẵn có và chịu tăng cường hồi phục hiện có.
- Thổ: choáng một lượt phản công.
- Lôi: tê liệt một lượt phản công.
- Băng: đóng băng một lượt phản công; lượt sau nhận thêm 10% sát thương nếu băng chưa bị hiệu ứng mới thay thế.
- Phong: thêm 20 điểm phần trăm né tránh trong hai lượt gồm lượt thi triển; giữ trần né hiện có 50%.

Debuff mới thay debuff cũ, kể cả hiệu ứng do linh thú gây. Buff Phong thuộc tu sĩ nên độc lập debuff quái. Kích hoạt lại làm mới thời gian, không cộng dồn. Sang đợt mới xóa debuff quái; buff tu sĩ tiếp tục đúng số lượt còn lại. Không gieo debuff vào quái đã chết vì chiêu.

## Mã và hiển thị

SkillSecondaryEffects.cs là nguồn metadata cho API và tooltip; SkillEffectCombat.cs giữ trạng thái trong một trận. Kết quả gieo xác suất tính tại server bằng seed trận riêng; client không gieo lại. SkillDef thêm thuộc tính tính toán SecondaryEffect; không thêm cột database hoặc sửa save. CastDto thêm Secondary, BattleDto thêm Weakened/WindGuard/CounterDamage; đều có giá trị mặc định để giữ khả năng đọc response cũ.

Khống chế mới xuất hiện từ mốc chiêu trúng .45. Có nhãn kích hoạt, dấu suy yếu trên quái và phong hộ trên tu sĩ. Số phản công lấy CounterDamage thực tế sau suy yếu. Hồi máu và thanh máu cập nhật ở .95 kể cả lượt quái chết hoặc bị khống. Tooltip ghi rõ xác suất cấp hiện tại/cấp kế, mức tác dụng và thời gian. Bỏ các câu mô tả cũ mâu thuẫn với cơ chế mới. Giữ 32 sprite sheet và bốn hướng thi triển.

## Kiểm chứng

- tools/secondary_effect_checks: **951 checks qua** — metadata cho 49 chủ động, cơ sở/bị động không proc; biên xác suất, cấp không hợp lệ, thời hạn, làm mới/thay thế, một lượt khống, băng lượt sau, buff qua đợt; bảy trận có proc thật, seed lặp lại cùng kết quả, giảm công quái thực tế theo đúng đợt, cap máu, Mộc proc đúng lượt tử vong không hồi sinh. Có test quái sống sau giảm sát thương trang bị và hit kết liễu sát ngưỡng.
- tools/check_secondary_feedback.cjs: qua thời điểm khống/hiệu ứng, trạng thái duy trì, nhãn đúng tu sĩ/quái, không phát lặp, tỉ lệ tooltip và thanh máu hồi cuối lượt trên lượt khống/kết liễu.
- tools/manual_checks: **3708 checks qua** trên workspace hiện tại.
- tools/four_element_checks: **714 checks qua**; xuất lại docs/four-elements-skills.json từ dữ liệu mới.
- tools/pet_cadence_checks: **130** Lôi Long, **112** thần thông cổ đại, **89** Hồ Điệp, **129** khống chế quái, **98** nhịp linh thú qua. Không sửa triển khai linh thú để làm tính năng này.
- tools/check_pet_feedback.cjs: qua sau khi bổ sung fixture lấy HEAL_COL từ battle.js (biến dùng chung đã có trong workspace). Không sửa màu hoặc logic hồi máu linh thú.
- Giữ qua kiểm tra quỹ đạo **1920** vị trí và renderer **1152** vị trí; không đổi animation nguồn.
- Browser Edge headless chạy build thật: API đủ **49** secondary definitions, tooltip cả **7 hệ ở Lv.1 và Lv.5**, **7 fixture trận do server mô phỏng** vẽ được nhãn khống/buff, **0 lỗi JavaScript**. Script tools/check_secondary_ui.cjs; kết quả build-check/secondary-effects/secondary-ui-results.json; ảnh *-secondary-tooltip.png và *-secondary-combat.png cùng thư mục.
- Build: dotnet build --no-restore -c Debug -o build-check/secondary-effects — **0 lỗi, 1 cảnh báo WindowsBase/WebView2**. Log build-check/secondary-build.log. Kiểm tra cú pháp JS và git diff --check qua.

## Rà soát

Reviewer độc lập phát hiện hai P2: xác định quái sống bằng sát thương trang bị chưa giảm, và thanh máu hồi phục bị giữ cũ trên lượt khống/kết liễu. Đã thêm test tái hiện trước khi sửa, dùng TargetSurvives và battleHeroHpAt để thống nhất. Reviewer kiểm tra lại: không còn phát hiện cần sửa.

Fixture browser chỉ thay state cục bộ trong trang test để vẽ trận server đã xuất; không sửa save người dùng. Bộ test UI cố định requestAnimationFrame để chụp frame, dùng polling theo timer để tránh chờ vô hạn trong fixture. Mô phỏng vẫn là theo lượt và kết quả xác suất server; chưa chạy cân bằng dài hạn hoặc đo FPS toàn game. Cảnh báo NuGet audit NU1900 ở các project test hiện có do feed không truy cập được đã được giữ trong output; không tuyên bố audit dependency đã đạt.

Các thay đổi đồng thời của người dùng trong market, database và linh thú được giữ nguyên; chỉ sửa hunk cần cho skill/DTO/trận/UI. Không merge/push hoặc ghi đè exe đang chạy.


## Giới hạn hiệu ứng xấu theo công pháp kích hoạt

Áp dụng cho cả bảy hệ: hiệu ứng phụ nhắm vào quái chỉ kích hoạt khi hệ của kỹ năng trùng công pháp đang kích hoạt. Sát thương kỹ năng khác hệ giữ nguyên; hồi phục Mộc và Phong hộ vẫn được phép. Thiêu đốt và giảm công bị động từ công pháp khác hệ cũng bị chặn, cùng các đường khống chế/thiêu đốt cũ. Hiệu ứng trang bị và linh thú độc lập. Tooltip hiển thị 0% và thông báo vô hiệu khi khác hệ.

Xác minh: 1.727 kiểm tra C# gồm toàn bộ 343 cặp công pháp/kỹ năng, 30 trận chiến khác hệ xác nhận sát thương còn nhưng debuff không phát sinh; kiểm tra JavaScript đủ 49 cặp hệ và hiệu ứng có lợi. Build thành công (0 lỗi, 1 cảnh báo WindowsBase/WebView2 có sẵn). Rà soát độc lập không phát hiện lỗi cần sửa.
