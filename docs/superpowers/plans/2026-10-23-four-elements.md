# Four Elements Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Hoàn thiện Thổ, Lôi, Băng, Phong và 32 animation dùng được trong game.
**Architecture:** SkillDef giữ ID cũ; manifest mới dùng renderer chung với Hỏa/Thủy/Mộc. Ảnh PNG AI được chuẩn hóa theo lưới trước khi tích hợp.
**Tech Stack:** .NET 8, C#, JavaScript Canvas, PNG RGBA, Python/Pillow cho đóng gói và kiểm tra.
**Spec:** docs/superpowers/specs/2026-10-23-four-elements-design.md (người dùng duyệt ngày 2026-10-23).

## Global Constraints

- Mỗi hệ: 1 đòn thường, 7 chủ động, 11 căn cơ/bị động, Luyện Khí đến Hợp Thể.
- 32 PNG 768×768, 16 ô 192×192, nền alpha thật; chiêu có hình riêng.
- Giữ ID cũ, cache 2 sheet, timeline .25/.45/.60/.75 và công thức nộ hiện tại.
- Không ghi đè thay đổi người dùng; không sửa trang phục hoặc thêm cơ chế khống chế chưa hỗ trợ.

## Review Focus

- Save từng học nút khung: giữ ID, loại chủ động và hoàn điểm đúng.
- Bí kíp khóa/mở, học lặp và đổi hệ: không tiêu sai vật phẩm, đòn thường đúng hệ.
- Frame tại biên pha, ảnh lỗi/chậm: không mất nhân vật hoặc phát trùng FX.
- Hình hộ thể và dư ảnh: không thể hiện khiên/stun chưa có logic.
- Mọi đường dẫn icon/manifest tồn tại; cả game và preview dùng ảnh thật.

### Task 1: Cây skill và công pháp

**Files:** Game/SkillData.cs, Game/ElementSkillData.cs, Game/CultivationManuals.cs, Game/GameService.cs; tools/four_element_checks/.
**Interface:** ElementSkillData.Skills trả IReadOnlyList<SkillDef>, SkillData.Skills nối dữ liệu mới, IDs theo spec.
- [x] Viết/chạy test server thất bại: đủ 7 hệ, mỗi hệ mới 19 nút, đòn thường duy nhất, tiền đề và ID hợp lệ; học/nâng/trang bị/đổi hệ/hoàn điểm/save.
- [x] Thay 20 nút khung bằng dữ liệu đầy đủ, bật hệ, sửa mô tả bí kíp; chỉ sửa hunk liên quan trong file đã bẩn.
- [x] Chạy test server thành công, cập nhật manual_checks từ 3 hệ sang số hệ hiện có.

### Task 2: Ảnh và metadata

**Files:** tools/four_elements/manifest.json, prompts.json, pack.py, validate.py; wwwroot/img/fx/{tho,loi,bang,gio}/; wwwroot/js/four-elements-fx-data.js.
**Interface:** FOUR_ELEMENTS_FX_DATA theo schema WATER_WOOD_FX_DATA, motion projectile/target/ground.
- [x] Viết/chạy validate trước ảnh; xác nhận báo thiếu 32 sheet.
- [x] Dùng image_gen cho từng asset; lưu nguồn/prompt; đóng gói frame và icon, giữ alpha.
- [x] Kiểm tra kích thước, frame, padding/alpha; duyệt hình từng chiêu, sửa ảnh lỗi.

### Task 3: Runtime, icon và preview

**Files:** wwwroot/js/fire-fx.js, skills.js, skill-preview.js; wwwroot/index.html; wwwroot/fx-preview/index.html; tools/check_four_elements.cjs.
**Interface:** fireFxSpec/fireFxPlan/drawFireSkillFx giữ nguyên API, thêm manifest lookup và icon.
- [x] Viết/chạy test frame/lookup thất bại cho 32 ID.
- [x] Nạp manifest trước renderer, nối lookup và icon, tăng độ phân giải preview cho hệ mới.
- [x] Trang fx-preview thêm bộ lọc và đầy đủ animation bốn hệ, dùng renderer thật để xem trên cảnh.
- [x] Chạy test timeline, cache, ảnh lỗi/context; smoke test UI/ảnh khi có browser.

### Task 4: Kiểm chứng và bàn giao

**Files:** docs/four-elements-qa.md; docs/superpowers/plans/2026-10-23-four-elements-progress.md.
- [x] Build output riêng, chạy checks server và JS hiện có liên quan; ghi giới hạn thật.
- [x] Reviewer độc lập kiểm tra diff cuối và tính tương thích; sửa phát hiện quan trọng rồi chạy lại checks.
- [x] Cập nhật trạng thái/đường dẫn preview và kết quả; bàn giao không tự push/merge.
