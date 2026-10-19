# Hỏa hệ — thực thi và QA

## Quyết định thực thi

- Người dùng duyệt mẫu Thần Hỏa Giáng Thế và yêu cầu triển khai theo plan ngày 19/10/2026.
- Nhánh làm việc: `codex/fire-sprite-sheets`. Giữ checkout hiện tại vì chứa giao diện Công pháp và các kỹ năng mới chưa commit; không chuyển sang worktree thiếu những thay đổi này.
- Không tự commit các thay đổi có sẵn ở trang bị, hành trang, GameData hoặc giao diện Công pháp.
- Hình gốc AI được giữ trong `tools/fire_fx/sources/`. Vì ảnh sinh ra có kích thước không chia hết cho 4, packer làm tròn biên từng ô, đưa vào vùng 176×176 và giữ đệm 8px trong frame 192×192. Không dùng ngưỡng màu để xóa nhân vật/nền.
- Bộ vẽ mới dùng mốc `.45` trùng thời điểm cập nhật máu trong `battle.js`; đến `.75` chặn cả renderer cũ để không lóe lại ở cuối lượt.
- Giữ nguyên dữ liệu damage, nộ, skill ID và điều kiện học. Các công pháp bị động giữ hình tĩnh.
- Hiệu ứng Hỏa Cầu cũ nằm một phần trong sprite nhân vật; khi ảnh mới sẵn sàng, renderer chọn tư thế tay không để không vẽ hai hiệu ứng.
- Trang QA `tools/fire_fx/preview.html` gọi renderer thật; phần dựng nền/nhân vật là trình diễn, không mô phỏng công thức sát thương.

## Cách chạy lại

1. `python tools/fire_fx/check_pack.py`
2. `python tools/fire_fx/pack.py --sources tools/fire_fx/sources --out wwwroot/img/fx/fire --runtime wwwroot/js/fire-fx-data.js`
3. `python tools/fire_fx/validate.py --manifest tools/fire_fx/manifest.json --out wwwroot/img/fx/fire`
4. `node tools/fire_fx/check-runtime.cjs`
5. `node tools/fire_fx/check-ui.cjs <đường dẫn tới playwright>`
6. `node tools/skill_checks/ui.cjs <đường dẫn tới playwright>`
7. `dotnet build --no-restore -c Debug -o bin/fire-fx-check`

Máy hiện tại có Python/Pillow và Playwright trong runtime đóng gói của Codex. Không thêm dependency runtime vào game.

## Giới hạn cần ghi rõ khi bàn giao

- Kết quả đo frame trên browser headless là chi phí renderer, không đại diện FPS toàn bộ game trên mọi máy.
- Kiểm tra DPR trong browser không thay thế hoàn toàn việc kiểm tra Windows đổi DPI khi kéo cửa sổ WebView2 sang màn hình khác.
- Bản duyệt hình đầu dùng asset AI; phải kiểm tra bằng mắt độ liên tục từng frame và hình dáng riêng của 7 chiêu trước khi coi là artwork hoàn thiện.

## Kết quả

- 9 sheet PNG RGBA 768×768, tổng 144 frame; validate lưới, anchor, alpha và đệm 8px: PASS. Tổng file runtime khoảng 4.82 MiB trên đĩa; cache texture tối đa 4.5 MiB giải mã.
- 4 bài kiểm tra packer: PASS, gồm nguồn 1254px, output lặp lại giống nhau, giữ alpha, từ chối ảnh đặc/metadata lỗi và không xuất dở bộ ảnh.
- Runtime: PASS mốc pha, 3 variant Hỏa Cầu, ảnh riêng của hai chiêu mới, metadata thiếu, lỗi tải PNG, trạng thái context, scale DPR và cache 2 sheet.
- Edge headless: PASS đủ 7 chiêu và 3 variant, các pha, quái ×1.5, viewport ×2, DPR 1.5, giảm chuyển động, lỗi tải ảnh và 100 lượt/1600 frame.
- Đo browser ở lần chạy xuất ảnh: p95 0.050 ms/frame cho vòng render trang QA. Không coi đây là FPS toàn game hoặc phép đo so sánh với renderer cũ.
- Kiểm tra trực tiếp `hcBattleDraw`: một lần vẽ FX mới, không vẽ sheet cũ cùng lượt; pha nghỉ không bật lại FX; lượt Thủy không phát FX Hỏa: PASS.
- Hồi quy Công pháp: cửa sổ mặc định và panel 700px không scroll, nút “+”/khóa/cấp tối đa/nhấp đúp trang bị: PASS.
- Build `bin/fire-fx-check`: 0 lỗi; còn cảnh báo WindowsBase MSB3277 đã có trước thay đổi.
- Đã chụp frame renderer thật cho 7 chiêu tại `obj/fire-frames/`; `export_gifs.py` mã hóa thành `obj/fire-<skill>.gif`, không dựng lại artwork.
- Hình sinh bằng công cụ imagegen tích hợp; prompt cho 8 ảnh tiếp theo lưu ở `tools/fire_fx/prompts.json`, mẫu gốc Thần Hỏa và storyboard lưu riêng. Không ghi đè ảnh cũ.
- Review độc lập tìm thấy lỗi thiếu metadata có thể ngắt vòng vẽ; đã sửa và thêm test hồi quy. Không tự commit vì checkout chứa thay đổi chưa commit của người dùng.
