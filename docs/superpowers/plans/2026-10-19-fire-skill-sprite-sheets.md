# Hỏa hệ — kế hoạch cập nhật sprite sheet

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development hoặc superpowers:executing-plans khi triển khai. Theo dõi công việc bằng các checkbox bên dưới.

**Goal:** Mỗi chiêu Hỏa có hình dáng riêng, chuyển động mượt và rõ trong cửa sổ game mặc định; hai chiêu Hóa Thần/Luyện Hư không còn dùng lại hoạt ảnh của chiêu cũ.

**Architecture:** Tách hiệu ứng lửa khỏi sprite nhân vật. Mỗi sheet chứa duy nhất hiệu ứng của một chiêu; metadata xác định frame, pha và điểm neo. Xem trước và chiến đấu gọi cùng bộ vẽ, nhân vật vẫn lấy từ `skinArt('heroCast')` theo cảnh giới/trang phục.

**Tech Stack:** Canvas 2D, JavaScript thuần, PNG RGBA, Python/Pillow cho đóng gói và kiểm tra ảnh; Playwright cho kiểm tra trình duyệt. Không thêm thư viện runtime vào game.

**Spec:** Phần “Phạm vi và thiết kế đề xuất” trong tài liệu này. Đây là bản kế hoạch đề xuất theo yêu cầu ngày 19/10/2026, chưa phải thiết kế hình ảnh đã được duyệt.

## Hiện trạng đã kiểm tra

- Có **7 chiêu có đòn đánh**: Hỏa Cầu, Liệt Diễm, Hỏa Long Thuật, Phần Thiên, Xích Diễm Thiên, Thần Hỏa Giáng Thế, Hư Không Diễm. Có **10 công pháp căn cơ/bị động**.
- `fx_hoa_skills.png`: 8 hàng × 8 frame, ô 124×64. Các hàng chứa cả chiêu cũ không còn trong cây hiện tại.
- `fx_hoa_skills_a.png` là bản tách nền; `fx_hoa_skills_fx.png` tiếp tục lọc bỏ nhân vật. Quy trình lọc theo màu và tọa độ cố định, khó tái sử dụng cho tranh mới.
- `SK_FX_ROW.than_hoa = 4` trùng Phần Thiên; `SK_FX_ROW.hu_khong_hoa = 5` trùng Xích Diễm Thiên.
- Hỏa Cầu có pipeline riêng: `HC`, `HC_REF`, `hcPreview()`, `hcBattleDraw()`. Dù có sheet `fx_hoa_cau_anim.png` 18 frame × 3 biến thể, phần hiển thị hiện còn dùng hiệu ứng trong sprite nhân vật `heroCast`.
- Xem trước (`skFlip`) và chiến đấu (`skBattlePlan`/`skBattleDraw`) chọn frame theo hai nhịp khác nhau. Chỉ thay PNG sẽ không sửa được độ lệch nhịp và điểm neo.
- Màn Công pháp hiện dùng tooltip và nút “+” trực tiếp; không dựng lại box thông tin để thêm animation.

## Phạm vi và thiết kế đề xuất

### Nhận diện từng chiêu

- **Hỏa Cầu — Luyện Khí:** cầu lửa gọn, đuôi lửa ngắn, nổ nhỏ khi chạm mục tiêu. Giữ ba biến thể nhỏ/trung/lớn theo cảnh giới hiện đang dùng: chỉ số cảnh giới 0 / 1 / từ 2 trở lên.
- **Liệt Diễm — Luyện Khí:** bùng lửa sát mặt đất quanh mục tiêu, lan ra ngoài thành vòng; không giống cột lửa.
- **Hỏa Long Thuật — Trúc Cơ:** rồng lửa thân dài lao từ tay tu sĩ tới mục tiêu, đầu rồng luôn dẫn chuyển động; có đuôi và dư ảnh.
- **Phần Thiên — Trúc Cơ:** ấn hỏa xuất hiện phía trên mục tiêu, cột lửa đánh thẳng từ trên xuống.
- **Xích Diễm Thiên — Kim Đan:** hỏa long đỏ sẫm cuộn thành vòng rồi nổ xích diễm tại mục tiêu; bỏ hình thủy long màu xanh trong sheet hiện tại.
- **Thần Hỏa Giáng Thế — Hóa Thần:** pháp ấn vàng trắng gọi thiên hỏa, lõi sáng và vòng phù văn; khác hình cột đơn của Phần Thiên.
- **Hư Không Diễm — Luyện Hư:** khe nứt hư không tối tím mở ra rồi bùng viêm tím–vàng, có hiệu ứng co hút trước khi nổ; không chỉ phóng to Xích Diễm Thiên.

Giữ phong cách pixel-art của game. Tăng độ phức tạp theo cảnh giới nhưng giữ quái, thanh máu và con số sát thương nhìn rõ. Không dùng flash trắng toàn màn hình.

**Căn cơ/bị động:** giữ icon và hào quang nhẹ hiện có; không tạo animation tấn công cho 10 bị động vì chúng không có lượt thi triển. Làm mới toàn bộ icon là hạng mục riêng, không nằm trong đợt sprite sheet này.

### Quy cách ảnh và thời gian

- Tạo **9 sheet**: 3 biến thể Hỏa Cầu + 6 chiêu chủ động. Mỗi sheet PNG RGBA 768×768, lưới 4×4, **16 frame 192×192**, đọc trái sang phải rồi xuống hàng.
- Chỉ chứa hiệu ứng, nền trong suốt thật; không chứa nhân vật, quái, chữ, viền ô hoặc nền caro. Giữ 8px đệm trong mỗi ô để không cắt quầng sáng.
- Chuyển động: frame 0–3 tụ lực, 4–7 phóng/hiện hình, 8–11 trúng đích, 12–15 tan biến. Chiêu tại mục tiêu dùng pha “hiện hình” thay pha bay.
- Quy đổi theo tiến độ lượt `s`: tụ lực `[0,.25)`, phóng `[.25,.45)`, trúng đích `[.45,.60)`, tan `[.60,.75)`, không vẽ `[.75,1]`. Dùng chung quy đổi trong preview và chiến đấu; xác nhận mốc va chạm với nhịp trừ máu hiện hành trước khi nối renderer.
- Điểm neo khai báo theo pha: `hand`, `projectile`, `target`, `targetTop`, `ground`. Không suy ra vị trí từ màu ảnh hoặc số hàng atlas cũ.
- Mỗi sheet giải mã khoảng 2.25 MiB. Chỉ cache tối đa 2 sheet mới cùng lúc (khoảng 4.5 MiB, chưa tính nhân vật và tài nguyên cũ). Không nạp toàn bộ sheet ngay khi mở game.
- Giữ damage, nộ, chi phí điểm, điều kiện lĩnh ngộ và skill ID. Sprite không quyết định sát thương.

## Global Constraints

- Giữ bố cục Công pháp vừa cửa sổ mặc định 740×680; không thêm box info hoặc thanh tab cảnh giới.
- Không sửa hệ Thủy, dữ liệu nhân vật hoặc sprite trang phục để phục vụ sheet Hỏa.
- Giữ asset cũ trong thời gian chuyển đổi; ảnh mới tải lỗi phải có fallback cũ và không làm biến mất nhân vật.
- Tạo/chỉnh tranh mới bằng công cụ image generation khi triển khai; script đóng gói chỉ cắt, kiểm tra, ghép frame và xuất metadata. Không chạy script sinh lại toàn bộ ảnh cũ ngoài phạm vi nhiệm vụ.

## Review Focus

1. Sheet chưa tải hoặc tải lỗi: nhân vật và trận đánh vẫn hiển thị, không phát đồng thời cả hiệu ứng cũ và mới.
2. Pha `.45` và `.75`: không nhảy frame, không kéo dài dư ảnh sang lượt kế tiếp; trúng đích đúng nhịp trừ máu.
3. Nhân vật đổi cảnh giới/trang phục và quái có kích thước khác: hiệu ứng vẫn bám tay hoặc tâm quái, không mang theo hình tu sĩ cũ.
4. Hỏa Cầu khi trang bị chiêu Hỏa/Thủy: lượt tung chiêu không có thêm một cầu lửa ngoài ý muốn.
5. Màn hình nhỏ, DPI cao và giảm chuyển động: không cắt hiệu ứng ở mép canvas, không che thanh máu, không làm chậm dải game.

## Các bước triển khai

### Task 1: Chốt storyboard và làm mẫu Thần Hỏa Giáng Thế

**Files:** tạo `tools/fire_fx/storyboards.md`, `tools/fire_fx/sources/than_hoa.png`; xuất thử `wwwroot/img/fx/fire/than_hoa.png`.

- [ ] Viết prompt cho 7 chiêu, ghi rõ 4 pha, palette, nền alpha, hướng đánh trái → phải, cùng tỷ lệ nét. Với Hỏa Cầu mô tả cả ba biến thể.
- [ ] Tạo trước mẫu 16 frame Thần Hỏa Giáng Thế. Đây là chiêu mới đang dùng hình thay thế, phù hợp để chốt phong cách mà chưa thay toàn bộ tài nguyên.
- [ ] Xuất contact sheet và ảnh đặt trên nền trận đánh tối, ở tỷ lệ game 100% và 200%; kiểm tra bằng mắt từng frame, không chỉ xem toàn sheet.
- [ ] Duyệt mẫu với người dùng trước khi sản xuất 8 sheet còn lại. Nếu mẫu cần sửa, cập nhật storyboard trước, không nhân rộng mẫu chưa chốt.

**Hoàn thành khi:** thấy rõ pháp ấn/thiên hỏa khác Phần Thiên, không có nhân vật hoặc nền thừa, đặt vừa canvas mặc định.

### Task 2: Đóng gói bộ sheet và metadata

**Files:** tạo `tools/fire_fx/pack.py`, `tools/fire_fx/validate.py`, `tools/fire_fx/manifest.json`, `tools/fire_fx/sources/`; xuất vào `wwwroot/img/fx/fire/`.

**Interface:** manifest chứa `skillId`, `variant`, `src`, `frameWidth:192`, `frameHeight:192`, `columns:4`, `frameCount:16`, `phases`, `anchors` và `scale`. `variant` của Hỏa Cầu là `small|medium|large`; chiêu khác là `default`. Anchor lưu tọa độ pixel trong ô, không lưu tọa độ màn hình.

- [ ] Viết kiểm tra ảnh sai kích thước, thiếu alpha, thiếu variant/skill, anchor ngoài ô hoặc metadata tham chiếu ảnh không tồn tại; chạy trước trên fixture lỗi và xác nhận bị từ chối.
- [ ] Làm 8 sheet còn lại theo mẫu đã duyệt; giữ frame liên tục về hình dáng, kích thước và trọng tâm. Không chấp nhận sheet sai lưới chỉ vì có đủ hình.
- [ ] Viết packer giữ nguyên RGBA và xuất đúng kích thước; không dùng chroma-key tự động làm mất lõi lửa hoặc quầng tối.
- [ ] Chạy `python tools/fire_fx/validate.py`; yêu cầu đủ 9 sheet, mỗi sheet đúng lưới, anchor trong ô, không có vùng alpha chạm 8px đệm.
- [ ] Xuất contact sheet để duyệt cả 9 sheet. Kiểm tra nền caro/tu sĩ/chữ bằng mắt; test alpha không đủ để xác nhận những lỗi này.
- [ ] Commit bộ asset và pipeline sau khi được duyệt.

### Task 3: Bộ chọn frame và vẽ chung cho Hỏa

**Files:** tạo `wwwroot/js/fire-fx.js`, `tools/fire_fx/check-runtime.cjs`; sửa `wwwroot/index.html` để nạp `fire-fx.js` trước `skills.js`.

**Interfaces:**

- `fireFxPlan(skillId, s01, realmIndex)` trả `null` hoặc `{src, frame, anchorKind, anchorX, anchorY, scale, alpha}`. Skill không thuộc Hỏa, tiến độ âm hoặc `s01 >= .75` trả `null`.
- `drawFireFx(ctx, plan, layout)` trả `boolean` báo đã vẽ. `layout` là `{handX, handY, targetX, targetY, groundY, heroHeight}` theo tọa độ canvas của caller. Chưa có ảnh thì trả `false` để caller dùng fallback.
- Loader chỉ giữ tối đa 2 sheet, xử lý `onerror`; renderer luôn `save/restore` để không rò alpha, filter hoặc composite mode.

- [ ] Viết test biên `s=-.01,0,.2499,.25,.4499,.45,.5999,.60,.7499,.75,1`; frame không ra ngoài 0–15, hai chiêu mới dùng `src` khác hai chiêu cũ.
- [ ] Kiểm tra Hỏa Cầu chọn đúng variant ở realm 0, 1, 2 và 5; skill Thủy trả `null`.
- [ ] Cài bộ đọc manifest, chọn frame, cache và neo theo layout. Xuất cấu hình runtime từ manifest bằng packer để tránh hai bản metadata phải sửa tay.
- [ ] Kiểm tra ảnh tải chậm/lỗi và luân phiên 7 chiêu: không exception, cache không vượt 2 sheet; trạng thái context giữ nguyên sau khi vẽ.
- [ ] Chạy `node tools/fire_fx/check-runtime.cjs`; tất cả assertion phải qua. Commit renderer.

### Task 4: Nối vào preview và chiến đấu, loại trừ vẽ trùng

**Files:** sửa `wwwroot/js/skills.js`, `wwwroot/js/skill-preview.js`, `wwwroot/js/battle-fx.js`, `wwwroot/js/battle.js`.

- [ ] Tạo kịch bản browser kiểm tra 7 chiêu ở pha tụ/bay/trúng/tan và trường hợp sheet tải lỗi; lưu ảnh trước khi thay pipeline.
- [ ] Preview gọi `fireFxPlan`/`drawFireFx` bằng layout riêng; không thêm lại canvas preview vào box info đã bỏ. Có thể dùng trang QA riêng để duyệt hoạt ảnh.
- [ ] Trong `hcBattleDraw`, giữ phần vẽ nhân vật theo trang phục, chuyển phần FX sang renderer mới. Nhân vật vẫn vẽ kể cả khi sheet lỗi.
- [ ] Chỉ gọi pipeline cũ khi renderer mới trả `false`; xử lý nhánh `battle.js`, `skBattlePlan` và `ultFx` để mỗi lượt chỉ phát một chiêu, không vẽ thêm cầu lửa hoặc thủy long xanh.
- [ ] Bỏ phụ thuộc `SK_FX_ROW` cho đường vẽ mới của `than_hoa`/`hu_khong_hoa`; fallback cũ vẫn được phép giữ hàng thay thế. Không sửa `GameService` hoặc các giá trị damage.
- [ ] Kiểm tra chuyển trang phục, 6 cảnh giới, quái nhỏ/lớn và lượt tung chiêu Thủy; không còn tu sĩ đóng sẵn trong FX, anchor không trượt theo frame.
- [ ] Commit tích hợp sau khi test runtime và browser cùng qua.

### Task 5: Duyệt hình ảnh, hiệu năng và bàn giao

**Files:** tạo `tools/fire_fx/preview.html`, `tools/fire_fx/check-ui.cjs`, `docs/fire-fx-qa.md`; chỉ dọn tài nguyên cũ sau khi kiểm tra hết tham chiếu.

- [ ] Trang QA cho chọn chiêu, cảnh giới, kích thước quái, play/pause và kéo tiến độ. Đọc asset/renderer thật, không viết renderer thứ hai cho trang QA.
- [ ] Duyệt cửa sổ Tu luyện 740×680, dải game mặc định 420×300 và ×2; kiểm tra DPI 100%, 150%, 200% bằng cấu hình browser/deviceScaleFactor và kiểm tra thực tế WebView2 khi có thể.
- [ ] Thử `prefers-reduced-motion`: giảm nháy/quầng và chuyển động phụ, vẫn giữ tín hiệu trúng đích. Không đổi kết quả trận đánh.
- [ ] Chạy liên tiếp ít nhất 100 lượt có tung chiêu và chuyển chiêu; ghi thời gian frame p95, bộ nhớ cache và cấu hình máy. So với bản cũ trên cùng máy; mục tiêu p95 không tăng quá 2ms, không giật rõ khi phát lần đầu.
- [ ] Chạy `node tools/skill_checks/ui.cjs` với Playwright đã cấu hình để xác nhận Công pháp vẫn không scroll và nút “+”/trang bị hoạt động.
- [ ] Build `dotnet build --no-restore -c Debug -o bin/fire-fx-check`; nếu game đang chạy, giữ output riêng, không đóng game để ghi đè exe.
- [ ] Bàn giao ảnh contact sheet, video/GIF 7 chiêu trên cảnh thật và kết quả QA. Chỉ xóa sheet/hàm cũ nếu tìm kiếm toàn repo xác nhận không còn dùng cho fallback hoặc hệ khác.

## Thứ tự và điểm duyệt

Task 1 → người dùng duyệt mẫu → Task 2 → Task 3 → Task 4 → Task 5. Chốt nhận diện hai chiêu mới trước, sau đó mới thay asset cũ. Có thể làm renderer khi đang sản xuất ảnh, nhưng chỉ tích hợp khi metadata đã ổn định.

**Lúc lập plan:** chưa sinh ảnh hoặc thay runtime. Sau đó người dùng đã duyệt demo và yêu cầu triển khai. Bộ 9 sheet, renderer chung, tích hợp chiến đấu/preview, trang QA và bộ kiểm tra đã hoàn thành trên nhánh `codex/fire-sprite-sheets`; chi tiết bằng chứng và giới hạn ở `docs/fire-fx-qa.md`. Chưa đo DPI vật lý trên nhiều màn hình hoặc so sánh FPS toàn game với bản cũ; không thay đổi chỉ số kỹ năng.
