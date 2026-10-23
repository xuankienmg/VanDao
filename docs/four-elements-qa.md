# Bốn công pháp Thổ, Lôi, Băng, Phong — 2026-10-23

## Kết quả

Mỗi hệ có 19 nút từ Luyện Khí đến Hợp Thể: 1 đòn thường, 7 chủ động, 11 căn cơ/bị động. Giữ 20 ID khung cũ, bổ sung 56 nút; 76 icon. Cả bảy bí kíp hiện có thể học; tổng xác suất công pháp từ Rương Boss vẫn 1%, chia đều 7 hệ.

Thổ ưu tiên phòng thủ/máu; Lôi công/bạo kích/xuyên thấu; Băng giảm công quái/phòng thủ; Phong thân pháp/bạo kích. Các chiêu Thủ/Thân pháp cũ vẫn là chủ động có sát thương. Khiên tạm, mất lượt, đánh lan hoặc dịch chuyển không được thêm; mô tả đã ghi đúng khả năng thực tế. Chi tiết dữ liệu server: four-elements-skills.json.

## Tài nguyên

- Sprite runtime: wwwroot/img/fx/tho/, loi/, bang/, gio/ — 32 PNG RGBA 768×768, lưới 4×4, 192×192/ô.
- Manifest: wwwroot/js/four-elements-fx-data.js. Renderer dùng chung: wwwroot/js/fire-fx.js; preview và trận đấu đi cùng timeline.
- Clip xem độc lập: wwwroot/fx-preview/assets/<skillId>.webp — 16 frame và một frame nghỉ, chu kỳ 2,4 giây.
- Xem qua HTTP: /fx-preview/four-elements.html. Có lọc hệ, play/pause, bước frame, tốc độ, kéo tiến độ, nền sáng/tối, quái nhỏ/lớn và giảm chuyển động. Bấm “Xem chiến đấu” để xem bộ vẽ runtime thật.
- Tổng quan: wwwroot/fx-preview/four-elements-contact.jpg.
- Demo động bốn tuyệt kỹ: wwwroot/fx-preview/four-elements-demo.webp.
- 32 ảnh nguồn do công cụ image_gen tích hợp tạo, mỗi chiêu một yêu cầu, transparent_background=true; không dùng CLI/API trả phí thay thế. Nguồn được sao chép vào tools/four_elements/sources/. Prompt và mô tả chiêu: four-elements-art-prompts.json; pipeline: tools/four_elements/pack.py và validate.py.
- Pack chỉ cắt/resize/đặt frame, trích icon và xuất clip, không vẽ hiệu ứng thay ảnh AI. Áp dụng một transform chung cho cả sheet để giữ chuyển động và kích thước tương đối. Nguồn 1254px được chia ô bằng tọa độ làm tròn, xuất đúng 768px; thêm 8px alpha đệm từng ô.

## Bằng chứng kiểm chứng

- dotnet run --no-restore --project tools/four_element_checks/four_element_checks.csproj: **714 checks qua**. Kiểm tra đủ nút/ID/code, tiền đề không vòng, điểm/cảnh giới, học và tiêu bí kíp, từ chối học lặp, đòn thường, trang bị, 28 chủ động thực sự tung khi đầy nộ, chỉ số bị động, hoàn điểm và SQLite save/reload, tỉ lệ công pháp.
- dotnet run --no-restore --project tools/manual_checks/manual_checks.csproj: **3675 checks qua**, gồm migration, hoàn điểm/reset, phân phối rương qua cảnh giới/mức/chương.
- node tools/check_four_elements.cjs: **32 mapping, 512 frame, 1152 vị trí qua**; biên timeline, reduced motion, context restore, ảnh lỗi, icon đủ 76, cache <=2 và regression nét pixel Hỏa.
- node tools/check_water_wood_fx.cjs: **16 chiêu cũ qua**, đủ frame ở ba cảnh giới, input không hợp lệ và cache.
- node tools/check_spell_placement.cjs: **288 vị trí của 16 chiêu Thủy/Mộc qua**, ba cỡ quái, hai viewport, ba frame va chạm.
- Python tools/four_elements/validate.py: 32 PNG khác nhau, 512 frame khác nhau, đúng RGBA/kích thước/đệm/alpha, đủ clip WebP. Hash và kết quả máy đọc được trong four-elements-assets-qa.json.
- Browser headless Edge chạy bản build thật: API đủ 7 hệ mở và 76 nút, bốn cây hiển thị 18 nút học được + ô đòn thường, đủ 76 icon; 32 hiệu ứng runtime vẽ trên cảnh, cache đúng, bộ lọc, nền sáng, giảm chuyển động, viewport 740×680 và 420×300; không lỗi JavaScript hoặc ảnh 404. Script: tools/check_four_elements_ui.cjs. Cây client dùng state fixture cục bộ; các thao tác server thật được kiểm tra riêng bằng four_element_checks, không sửa save người dùng.
- Ảnh UI: build-check/four-elements/*-tree.png, four-elements-preview.png, four-elements-preview-light.png, four-elements-small.png; kết quả browser: ui-results.json cùng thư mục.
- Build kiểm tra: dotnet build --no-restore -c Debug -o build-check/four-elements. **0 lỗi, 1 cảnh báo MSB3277 WindowsBase/WebView2**. Log: build-check/four-elements-build.log.

## Rà soát và giới hạn

Reviewer độc lập phát hiện mô tả Rương Boss còn ghi 3 hệ; đã sửa cả tiếng Việt và bản dịch Anh, thêm test tái hiện trước khi sửa. Khi học công pháp đầu tiên rồi kích hoạt ngay, hệ fallback trước đây có thể chưa ghi đòn thường; đã đồng bộ cả nhánh “đang kích hoạt”.

Reviewer đã rà soát lại phần sửa và tự chạy kiểm tra JavaScript: không còn phát hiện cần sửa. Bản build cuối và lượt browser cuối đều qua; browser kiểm tra cả 32 đường dẫn WebP. Thay đổi giữ trên nhánh codex/four-elements, chưa commit/merge/push.

Đã xem tổng quan 32 chiêu, sprite mẫu và ảnh UI trên nền tối/sáng; không khẳng định kiểm duyệt thủ công từng pixel trong 512 frame. Kiểm tra layout và reduced motion là mô phỏng browser; chưa đo FPS/p95 toàn game, DPI vật lý nhiều màn hình hoặc WebView2 trên thiết bị khác. Các alpha lõi tối đa 253/254 do ảnh glow/resize được giữ nguyên, không ép thành 255.

Save từng chứa nút khung vẫn giữ ID/cấp/ô trang bị. Chi phí và yêu cầu được nâng thành thiết kế hoàn chỉnh; điểm đã dùng được tính lại theo chi phí mới như công thức hiện có. Bình thường bốn hệ trước đây chưa mở; không cấp miễn phí bí kíp hoặc sửa dữ liệu người chơi.

NuGet audit trả NU1900 vì các feed không truy cập được trong lần restore/check, kể cả sau khi chạy ngoài sandbox; dependency có sẵn nên build/test vẫn chạy. Không coi cảnh báo feed là kiểm tra bảo mật dependency đã đạt.

Các thay đổi có sẵn trong workspace về rương theo độ khó, vật phẩm, chợ và linh thú được giữ nguyên. Chỉ sửa hunk cần thiết ở file dùng chung; không merge/push và không ghi đè bản exe đang chạy.

## Bổ sung hướng thi triển — yêu cầu tiếp nối 2026-10-23

32 chiêu của bốn hệ có castDirection riêng: 8 bay ngang (gồm 4 đòn thường), 8 giáng từ trên, 6 từ dưới lên, 10 bùng tại tâm quái. Ví dụ Thạch Đạn → ngang; Sơn Nhạc → trên xuống; Đông Địa → dưới lên; Thổ Giáp → bùng giữa quái. Hỏa/Thủy/Mộc giữ manifest và đường vẽ cũ.

Pha tụ đặt tại nguồn; pha .25–.45 di chuyển vào tâm mục tiêu; từ .45 vẽ va chạm tại tâm quái. Chiêu center đứng tại quái và lớn dần khi tụ. Chỉ frame bay của Lôi Kích, Cửu Tiêu Lôi Phạt, Băng Tiễn và Huyền Băng xoay dọc theo hướng giáng; sơn ấn/địa thứ giữ dáng đứng. Giảm chuyển động bỏ dịch chuyển và xoay, vẫn giữ khung báo va chạm. Fit kiểm tra cả vùng hình sau xoay để tránh cắt ở canvas nhỏ.

- node tools/check_cast_directions.cjs: qua bốn quỹ đạo, hướng xoay, tâm bùng phát, reduced motion và **1920 vị trí sau biến đổi**.
- Các kiểm tra 1152 vị trí bốn hệ, 288 vị trí Thủy/Mộc, 16 mapping cũ và validator 32 sheet vẫn qua.
- Browser bản build mới: đủ 4 hướng, chụp **96 frame trên cảnh runtime thật**, không pageerror/ảnh thiếu; build 0 lỗi/1 cảnh báo WindowsBase.
- Reviewer độc lập chạy lại hai suite 1920 và 1152: không phát hiện cần sửa.
- Demo được ghép từ ảnh canvas trình duyệt thật: wwwroot/fx-preview/four-directions-demo.webp. Nguồn chụp: build-check/four-elements/cast-directions/. Các WebP từng chiêu cũ chỉ mô tả chuyển hình trong ô; hướng bay được thể hiện trên cảnh runtime và demo mới.

Không đổi sát thương, mốc trừ máu, dữ liệu save hoặc tạo lại ảnh nguồn. Metadata nguồn được cập nhật để pack.py xuất lại không mất castDirection/rotateWithTravel.

## Yêu cầu tiếp nối: hiệu ứng phụ

Người dùng sau đó yêu cầu chiêu chủ động có tỉ lệ gây hiệu ứng phụ ngoài sát thương. Cơ chế này đã được triển khai cho cả bảy hệ; phần giới hạn ban đầu về việc chưa thêm khống/buff đã được thay thế bởi yêu cầu mới. Xem docs/active-secondary-effects-qa.md để biết thông số và bằng chứng kiểm tra. Không tạo lại ảnh nguồn hoặc đổi hướng thi triển.
