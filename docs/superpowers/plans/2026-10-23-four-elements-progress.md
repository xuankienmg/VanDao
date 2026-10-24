# Ledger — plan: docs/superpowers/plans/2026-10-23-four-elements.md

- Người dùng đã duyệt thiết kế và yêu cầu triển khai liên tục.
- Ruling: thực hiện trực tiếp trong checkout hiện có để giữ các thay đổi gameplay đang có; không chuyển sang bản checkout thiếu các thay đổi đó. Không tự commit các file người dùng đã sửa.
- Ruling: không xin duyệt thêm execution method; yêu cầu “triển khai đi” đã cho phép làm việc cần thiết.
- Pre-flight: SkillData -> manifest -> renderer dùng chung ID; test kiểm tra tính toàn vẹn xuyên cả ba.
- Task 1: complete — 714 checks server qua: 76 nút, 28 chiêu chủ động, học bí kíp/đổi hệ/hoàn điểm/lưu nạp/phần thưởng. Manual suite: 3675 checks qua.
- Task 2: complete — 32 nguồn image_gen, 32 PNG RGBA 768×768, 512 frame, 76 icon, 32 WebP có 16 frame và pha nghỉ. Prompt lưu docs/four-elements-art-prompts.json; nguồn tools/four_elements/sources/.
- Ruling: nguồn square 1254×1254 không chia hết cho 4 — cắt theo tọa độ lưới làm tròn, không bỏ hai pixel ở mép; một transform chung giữ tỷ lệ các frame.
- Ruling: glow giữ alpha gốc; max alpha 253/254 sau resize là hợp lệ. Validator kiểm tra nền alpha=0 và subject alpha>=128 thay vì ép pixel lõi=255.
- Task 3: complete — 32 mapping, 512 frame, 1152 placement; reduced-motion, ảnh lỗi và cache <=2. UI thật qua 4 cây, 76 icon, 32 hiệu ứng, không pageerror/404.
- Ruling: khi kích hoạt công pháp đang là fallback, SyncActive vẫn phải ghi hệ và đòn thường vào player; test đã tái hiện lỗi rồi qua sau sửa.
- Review: một P2 về tooltip Rương Boss còn nói 3 hệ — đã xác nhận, thêm test đỏ, sửa tooltip và bản dịch; 714 server checks qua.
- Review tự kiểm tra: Fire manifest không có element; thêm regression test đỏ rồi dùng allowlist smoothing để giữ nét Hỏa cũ.
- Task 4: complete — build cuối 0 lỗi/1 cảnh báo WindowsBase; UI chạy lại trên bản build mới nhất qua, kể cả 32 URL WebP; git diff --check và JS syntax qua. Reviewer rà soát lại: không còn phát hiện cần sửa. Báo cáo docs/four-elements-qa.md đã lưu.
- Giữ thay đổi trên codex/four-elements, không tự merge/push hoặc commit file người dùng đã sửa. Bản build kiểm tra: build-check/four-elements/.
