# Animation công pháp Thủy và Mộc

16 animation, 16 khung/chiêu, RGBA 768×768; mỗi khung 192×192.

Nguồn ảnh: `local:codex-cli`, model `codex-image_gen`, thông qua generate2dmedia/route_media.py. Gemini trả quota; các yêu cầu này không tạo ảnh. Giá ước tính CLI theo receipt: 0 USD; sử dụng hạn mức đăng nhập. Prompt, hash và receipt nằm trong `.forge/water-wood-fx/<skillId>/raw/`.

## Sử dụng

- Xem `wwwroot/fx-preview/index.html` qua HTTP: tạm dừng, từng khung, tốc độ và nền sáng/tối.
- Sprite runtime: `wwwroot/img/fx/thuy/` và `wwwroot/img/fx/moc/`.
- Manifest: `wwwroot/js/water-wood-fx-data.js`.
- Frames PNG, clip WebP, Aseprite JSON và Godot: `.forge/water-wood-fx/<skillId>/`.
- Thủy và Mộc nối vào preview và trận đấu với cơ chế fallback khi ảnh chưa tải; cây được mở sau khi học bí kíp.
- Animation dành cho đòn thường/chủ động; bị động giữ hình tĩnh. Chưa thêm cơ chế Thấm Thủy hoặc Ký Sinh; hồi máu Mộc được xử lý riêng ở server.

## QA và giới hạn

Bảng dưới giữ nguyên id, value, threshold và đường dẫn của mọi WARN/FAIL từ các báo cáo đã xuất. Phép đo identity_head_ratio, torso_drift, row_baseline và seam dựa trên cơ thể nhân vật: không dùng để quyết định chất lượng VFX biến hình/nổ/tan. safe_frame cảnh báo vượt vùng an toàn 15%, không đồng nghĩa vượt ranh ô. faint_specks là hạt alpha thấp ở glow. Bản nguồn có đường kẻ được làm sạch bằng trim-border/edge-clean-depth của skill. Không tuyên bố mọi phép kiểm tra đều đạt.

| File | ID | Status | Value | Threshold |
|---|---|---|---|---|
| .forge/water-wood-fx/bach_diep/final-qc/sheet-qc.json | safe_frame | warn | 8 | 0 |
| .forge/water-wood-fx/bach_diep/final-qc/sheet-qc.json | faint_specks | warn | {'floor_px': 14635, 'detached_components': 27} | 0 |
| .forge/water-wood-fx/bach_diep/final-qc-v4/sheet-qc.json | safe_frame | warn | 8 | 0 |
| .forge/water-wood-fx/bach_diep/final-qc-v4/sheet-qc.json | faint_specks | warn | {'floor_px': 14635, 'detached_components': 27} | 0 |
| .forge/water-wood-fx/bach_diep/frames-qc-v4/sheet-qc.json | identity_head_ratio | fail | {'0': 1.111, '1': 1.0, '2': 1.0, '3': 1.0, '4': 1.0, '5': 1.0, '6': 1.0, '7': 1.0, '8': 1.0, '9': 1.0, '10': 1.0, '11': 1.0, '12': 1.0, '13': 1.0, '14': 1.0, '15': 1.0} | [0.95, 1.05] |
| .forge/water-wood-fx/bach_diep/frames-qc-v4/sheet-qc.json | row_baseline | warn | 21.0 | 0.5 |
| .forge/water-wood-fx/bach_diep/frames-qc-v4/sheet-qc.json | seam | warn | 1.71 | 1.5 |
| .forge/water-wood-fx/bach_diep/raw-qc/sheet-qc.json | cross_cell_components | fail | 1 | 0 |
| .forge/water-wood-fx/bach_diep/raw-qc/sheet-qc.json | cross_cell_visible | fail | 1 | 0 |
| .forge/water-wood-fx/bach_diep/raw-qc/sheet-qc.json | sheet_edge | fail | 1 | 0 |
| .forge/water-wood-fx/bach_diep/raw-qc/sheet-qc.json | boundary_band | warn | 2609 | 0 |
| .forge/water-wood-fx/bach_diep/raw-qc/sheet-qc.json | safe_frame | warn | 11 | 0 |
| .forge/water-wood-fx/co_moc/final-qc/sheet-qc.json | safe_frame | warn | 8 | 0 |
| .forge/water-wood-fx/co_moc/final-qc/sheet-qc.json | faint_specks | warn | {'floor_px': 15136, 'detached_components': 1} | 0 |
| .forge/water-wood-fx/co_moc/final-qc-v3/sheet-qc.json | safe_frame | warn | 10 | 0 |
| .forge/water-wood-fx/co_moc/final-qc-v3/sheet-qc.json | faint_specks | warn | {'floor_px': 16571, 'detached_components': 39} | 0 |
| .forge/water-wood-fx/co_moc/final-qc-v4/sheet-qc.json | safe_frame | warn | 11 | 0 |
| .forge/water-wood-fx/co_moc/final-qc-v4/sheet-qc.json | faint_specks | warn | {'floor_px': 16571, 'detached_components': 39} | 0 |
| .forge/water-wood-fx/co_moc/frames-qc/sheet-qc.json | identity_head_ratio | fail | {'0': 1.042, '1': 1.104, '2': 1.083, '3': 1.104, '4': 1.0, '5': 1.0, '6': 0.938, '7': 1.0, '8': 1.146, '9': 1.146, '10': 1.021, '11': 1.0, '12': 1.0, '13': 1.0, '14': 1.0, '15': 1.0} | [0.95, 1.05] |
| .forge/water-wood-fx/co_moc/frames-qc/sheet-qc.json | torso_drift | warn | 52.22 | 2.0 |
| .forge/water-wood-fx/co_moc/frames-qc/sheet-qc.json | row_baseline | warn | 12.0 | 0.5 |
| .forge/water-wood-fx/co_moc/frames-qc/sheet-qc.json | seam | warn | 1.65 | 1.5 |
| .forge/water-wood-fx/co_moc/frames-qc-v3/sheet-qc.json | identity_head_ratio | fail | {'0': 1.111, '1': 1.156, '2': 1.0, '3': 1.0, '4': 1.0, '5': 1.2, '6': 1.0, '7': 1.0, '8': 1.0, '9': 1.0, '10': 1.156, '11': 1.0, '12': 1.089, '13': 1.0, '14': 1.0, '15': 1.0} | [0.95, 1.05] |
| .forge/water-wood-fx/co_moc/frames-qc-v3/sheet-qc.json | torso_drift | warn | 75.75 | 2.0 |
| .forge/water-wood-fx/co_moc/frames-qc-v3/sheet-qc.json | row_baseline | warn | 12.0 | 0.5 |
| .forge/water-wood-fx/co_moc/frames-qc-v4/sheet-qc.json | identity_head_ratio | fail | {'0': 1.111, '1': 1.156, '2': 1.0, '3': 1.0, '4': 1.0, '5': 1.2, '6': 1.0, '7': 1.0, '8': 1.0, '9': 1.0, '10': 1.156, '11': 1.0, '12': 1.089, '13': 1.0, '14': 1.0, '15': 1.0} | [0.95, 1.05] |
| .forge/water-wood-fx/co_moc/frames-qc-v4/sheet-qc.json | torso_drift | warn | 75.75 | 2.0 |
| .forge/water-wood-fx/co_moc/frames-qc-v4/sheet-qc.json | row_baseline | warn | 12.0 | 0.5 |
| .forge/water-wood-fx/co_moc/processed-v3/pipeline-meta.json | source_edge_touch_frames | warn | [[1, 2], [1, 3], [2, 2], [2, 3]] | [] |
| .forge/water-wood-fx/co_moc/processed-v4/pipeline-meta.json | source_edge_touch_frames | warn | [[1, 2], [1, 3], [2, 2], [2, 3]] | [] |
| .forge/water-wood-fx/co_moc/raw-qc/sheet-qc.json | cross_cell_components | fail | 1 | 0 |
| .forge/water-wood-fx/co_moc/raw-qc/sheet-qc.json | cross_cell_visible | fail | 1 | 0 |
| .forge/water-wood-fx/co_moc/raw-qc/sheet-qc.json | sheet_edge | fail | 1 | 0 |
| .forge/water-wood-fx/co_moc/raw-qc/sheet-qc.json | boundary_band | warn | 2564 | 0 |
| .forge/water-wood-fx/co_moc/raw-qc/sheet-qc.json | safe_frame | warn | 10 | 0 |
| .forge/water-wood-fx/dang_man/final-qc/sheet-qc.json | safe_frame | warn | 8 | 0 |
| .forge/water-wood-fx/dang_man/final-qc/sheet-qc.json | faint_specks | warn | {'floor_px': 15643, 'detached_components': 41} | 0 |
| .forge/water-wood-fx/dang_man/final-qc-v4/sheet-qc.json | safe_frame | warn | 8 | 0 |
| .forge/water-wood-fx/dang_man/final-qc-v4/sheet-qc.json | faint_specks | warn | {'floor_px': 15643, 'detached_components': 41} | 0 |
| .forge/water-wood-fx/dang_man/raw-qc/sheet-qc.json | cross_cell_components | fail | 2 | 0 |
| .forge/water-wood-fx/dang_man/raw-qc/sheet-qc.json | cross_cell_visible | fail | 2 | 0 |
| .forge/water-wood-fx/dang_man/raw-qc/sheet-qc.json | sheet_edge | fail | 2 | 0 |
| .forge/water-wood-fx/dang_man/raw-qc/sheet-qc.json | boundary_band | warn | 2126 | 0 |
| .forge/water-wood-fx/dang_man/raw-qc/sheet-qc.json | safe_frame | warn | 12 | 0 |
| .forge/water-wood-fx/hon_nguyen_moc/final-qc/sheet-qc.json | safe_frame | warn | 10 | 0 |
| .forge/water-wood-fx/hon_nguyen_moc/final-qc/sheet-qc.json | faint_specks | warn | {'floor_px': 16646, 'detached_components': 25} | 0 |
| .forge/water-wood-fx/hon_nguyen_moc/final-qc-v4/sheet-qc.json | safe_frame | warn | 10 | 0 |
| .forge/water-wood-fx/hon_nguyen_moc/final-qc-v4/sheet-qc.json | faint_specks | warn | {'floor_px': 16646, 'detached_components': 25} | 0 |
| .forge/water-wood-fx/hon_nguyen_moc/frames-qc-v4/sheet-qc.json | identity_head_ratio | fail | {'0': 1.065, '1': 1.196, '2': 1.196, '3': 1.13, '4': 1.196, '5': 1.0, '6': 1.0, '7': 0.978, '8': 0.978, '9': 0.978, '10': 1.196, '11': 0.978, '12': 0.978, '13': 1.0, '14': 0.978, '15': 0.978} | [0.95, 1.05] |
| .forge/water-wood-fx/hon_nguyen_moc/frames-qc-v4/sheet-qc.json | torso_drift | warn | 49.92 | 2.0 |
| .forge/water-wood-fx/hon_nguyen_moc/frames-qc-v4/sheet-qc.json | row_baseline | warn | 14.0 | 0.5 |
| .forge/water-wood-fx/hon_nguyen_moc/frames-qc-v4/sheet-qc.json | seam | warn | 1.69 | 1.5 |
| .forge/water-wood-fx/hon_nguyen_moc/processed-v4/pipeline-meta.json | source_edge_touch_frames | warn | [[1, 3], [2, 1]] | [] |
| .forge/water-wood-fx/hon_nguyen_moc/raw-qc/sheet-qc.json | cross_cell_components | fail | 1 | 0 |
| .forge/water-wood-fx/hon_nguyen_moc/raw-qc/sheet-qc.json | cross_cell_visible | fail | 1 | 0 |
| .forge/water-wood-fx/hon_nguyen_moc/raw-qc/sheet-qc.json | sheet_edge | fail | 1 | 0 |
| .forge/water-wood-fx/hon_nguyen_moc/raw-qc/sheet-qc.json | boundary_band | warn | 2672 | 0 |
| .forge/water-wood-fx/hon_nguyen_moc/raw-qc/sheet-qc.json | safe_frame | warn | 12 | 0 |
| .forge/water-wood-fx/hon_nguyen_thuy/final-qc/sheet-qc.json | safe_frame | warn | 9 | 0 |
| .forge/water-wood-fx/hon_nguyen_thuy/final-qc/sheet-qc.json | faint_specks | warn | {'floor_px': 16973, 'detached_components': 18} | 0 |
| .forge/water-wood-fx/hon_nguyen_thuy/final-qc-v3/sheet-qc.json | safe_frame | warn | 8 | 0 |
| .forge/water-wood-fx/hon_nguyen_thuy/final-qc-v3/sheet-qc.json | faint_specks | warn | {'floor_px': 16973, 'detached_components': 18} | 0 |
| .forge/water-wood-fx/hon_nguyen_thuy/final-qc-v4/sheet-qc.json | safe_frame | warn | 9 | 0 |
| .forge/water-wood-fx/hon_nguyen_thuy/final-qc-v4/sheet-qc.json | faint_specks | warn | {'floor_px': 17550, 'detached_components': 1} | 0 |
| .forge/water-wood-fx/hon_nguyen_thuy/frames-qc-v3/sheet-qc.json | identity_head_ratio | fail | {'0': 1.043, '1': 0.957, '2': 0.957, '3': 0.957, '4': 1.128, '5': 0.979, '6': 0.957, '7': 1.021, '8': 1.128, '9': 1.021, '10': 0.957, '11': 1.128, '12': 0.957, '13': 0.957, '14': 1.043, '15': 1.043} | [0.95, 1.05] |
| .forge/water-wood-fx/hon_nguyen_thuy/frames-qc-v3/sheet-qc.json | identity_residual | warn | {'0': None, '1': 0.79, '2': 0.91, '3': 0.72, '4': 1.53, '5': 1.37, '6': 1.07, '7': 0.81, '8': 0.97, '9': 1.0, '10': 1.05, '11': 1.14, '12': 1.02, '13': 1.21, '14': 0.92, '15': 0.44} | 1.5 |
| .forge/water-wood-fx/hon_nguyen_thuy/frames-qc-v3/sheet-qc.json | torso_drift | warn | 60.71 | 2.0 |
| .forge/water-wood-fx/hon_nguyen_thuy/frames-qc-v3/sheet-qc.json | row_baseline | warn | 23.0 | 0.5 |
| .forge/water-wood-fx/hon_nguyen_thuy/frames-qc-v3/sheet-qc.json | seam | warn | 2.06 | 1.5 |
| .forge/water-wood-fx/hon_nguyen_thuy/frames-qc-v4/sheet-qc.json | identity_head_ratio | fail | {'0': 1.01, '1': 0.929, '2': 0.929, '3': 0.929, '4': 1.111, '5': 0.949, '6': 1.111, '7': 0.909, '8': 1.111, '9': 0.909, '10': 1.111, '11': 1.111, '12': 0.929, '13': 0.99, '14': 1.111, '15': 1.111} | [0.95, 1.05] |
| .forge/water-wood-fx/hon_nguyen_thuy/frames-qc-v4/sheet-qc.json | torso_drift | warn | 61.56 | 2.0 |
| .forge/water-wood-fx/hon_nguyen_thuy/frames-qc-v4/sheet-qc.json | row_baseline | warn | 22.0 | 0.5 |
| .forge/water-wood-fx/hon_nguyen_thuy/frames-qc-v4/sheet-qc.json | seam | warn | 2.01 | 1.5 |
| .forge/water-wood-fx/hon_nguyen_thuy/processed-v3/pipeline-meta.json | source_edge_touch_frames | warn | [[2, 2]] | [] |
| .forge/water-wood-fx/hon_nguyen_thuy/processed-v4/pipeline-meta.json | source_edge_touch_frames | warn | [[2, 2]] | [] |
| .forge/water-wood-fx/hon_nguyen_thuy/raw-qc/sheet-qc.json | boundary_band | warn | 6 | 0 |
| .forge/water-wood-fx/hon_nguyen_thuy/raw-qc/sheet-qc.json | safe_frame | warn | 12 | 0 |
| .forge/water-wood-fx/hu_hai/final-qc/sheet-qc.json | safe_frame | warn | 8 | 0 |
| .forge/water-wood-fx/hu_hai/final-qc/sheet-qc.json | faint_specks | warn | {'floor_px': 14813, 'detached_components': 24} | 0 |
| .forge/water-wood-fx/hu_hai/final-qc-v3/sheet-qc.json | safe_frame | warn | 7 | 0 |
| .forge/water-wood-fx/hu_hai/final-qc-v3/sheet-qc.json | faint_specks | warn | {'floor_px': 14813, 'detached_components': 24} | 0 |
| .forge/water-wood-fx/hu_hai/final-qc-v4/sheet-qc.json | safe_frame | warn | 8 | 0 |
| .forge/water-wood-fx/hu_hai/final-qc-v4/sheet-qc.json | faint_specks | warn | {'floor_px': 15346, 'detached_components': 0} | 0 |
| .forge/water-wood-fx/hu_hai/frames-qc-v3/sheet-qc.json | identity_head_ratio | fail | {'0': 1.089, '1': 1.0, '2': 1.0, '3': 1.0, '4': 1.0, '5': 1.0, '6': 1.0, '7': 1.0, '8': 1.178, '9': 1.067, '10': 1.0, '11': 1.067, '12': 1.0, '13': 1.0, '14': 1.044, '15': 1.0} | [0.95, 1.05] |
| .forge/water-wood-fx/hu_hai/frames-qc-v3/sheet-qc.json | torso_drift | warn | 11.01 | 2.0 |
| .forge/water-wood-fx/hu_hai/frames-qc-v3/sheet-qc.json | row_baseline | warn | 18.0 | 0.5 |
| .forge/water-wood-fx/hu_hai/frames-qc-v3/sheet-qc.json | seam | warn | 1.64 | 1.5 |
| .forge/water-wood-fx/hu_hai/frames-qc-v4/sheet-qc.json | identity_head_ratio | fail | {'0': 1.064, '1': 0.979, '2': 0.957, '3': 0.979, '4': 1.085, '5': 1.085, '6': 0.979, '7': 0.979, '8': 1.128, '9': 1.021, '10': 1.17, '11': 1.128, '12': 0.957, '13': 0.957, '14': 1.128, '15': 0.979} | [0.95, 1.05] |
| .forge/water-wood-fx/hu_hai/frames-qc-v4/sheet-qc.json | torso_drift | warn | 8.08 | 2.0 |
| .forge/water-wood-fx/hu_hai/frames-qc-v4/sheet-qc.json | row_baseline | warn | 20.0 | 0.5 |
| .forge/water-wood-fx/hu_hai/frames-qc-v4/sheet-qc.json | seam | warn | 1.68 | 1.5 |
| .forge/water-wood-fx/hu_hai/raw-qc/sheet-qc.json | cross_cell_components | fail | 1 | 0 |
| .forge/water-wood-fx/hu_hai/raw-qc/sheet-qc.json | cross_cell_visible | fail | 1 | 0 |
| .forge/water-wood-fx/hu_hai/raw-qc/sheet-qc.json | sheet_edge | fail | 1 | 0 |
| .forge/water-wood-fx/hu_hai/raw-qc/sheet-qc.json | boundary_band | warn | 2608 | 0 |
| .forge/water-wood-fx/hu_hai/raw-qc/sheet-qc.json | safe_frame | warn | 11 | 0 |
| .forge/water-wood-fx/hu_khong_kien_moc/final-qc/sheet-qc.json | safe_frame | warn | 8 | 0 |
| .forge/water-wood-fx/hu_khong_kien_moc/final-qc/sheet-qc.json | faint_specks | warn | {'floor_px': 16750, 'detached_components': 25} | 0 |
| .forge/water-wood-fx/hu_khong_kien_moc/final-qc-v3/sheet-qc.json | safe_frame | warn | 7 | 0 |
| .forge/water-wood-fx/hu_khong_kien_moc/final-qc-v3/sheet-qc.json | faint_specks | warn | {'floor_px': 16750, 'detached_components': 25} | 0 |
| .forge/water-wood-fx/hu_khong_kien_moc/final-qc-v4/sheet-qc.json | safe_frame | warn | 8 | 0 |
| .forge/water-wood-fx/hu_khong_kien_moc/final-qc-v4/sheet-qc.json | faint_specks | warn | {'floor_px': 16750, 'detached_components': 25} | 0 |
| .forge/water-wood-fx/hu_khong_kien_moc/frames-qc-v3/sheet-qc.json | identity_head_ratio | fail | {'0': 1.111, '1': 1.178, '2': 1.0, '3': 1.0, '4': 1.067, '5': 1.044, '6': 1.044, '7': 1.044, '8': 1.0, '9': 1.0, '10': 1.0, '11': 1.0, '12': 1.0, '13': 1.0, '14': 1.044, '15': 1.0} | [0.95, 1.05] |
| .forge/water-wood-fx/hu_khong_kien_moc/frames-qc-v3/sheet-qc.json | torso_drift | warn | 13.36 | 2.0 |
| .forge/water-wood-fx/hu_khong_kien_moc/frames-qc-v3/sheet-qc.json | row_baseline | warn | 26.0 | 0.5 |
| .forge/water-wood-fx/hu_khong_kien_moc/frames-qc-v3/sheet-qc.json | seam | warn | 1.73 | 1.5 |
| .forge/water-wood-fx/hu_khong_kien_moc/frames-qc-v4/sheet-qc.json | identity_head_ratio | fail | {'0': 1.111, '1': 1.178, '2': 1.0, '3': 1.0, '4': 1.067, '5': 1.044, '6': 1.044, '7': 1.044, '8': 1.0, '9': 1.0, '10': 1.0, '11': 1.0, '12': 1.0, '13': 1.0, '14': 1.044, '15': 1.0} | [0.95, 1.05] |
| .forge/water-wood-fx/hu_khong_kien_moc/frames-qc-v4/sheet-qc.json | torso_drift | warn | 13.36 | 2.0 |
| .forge/water-wood-fx/hu_khong_kien_moc/frames-qc-v4/sheet-qc.json | row_baseline | warn | 26.0 | 0.5 |
| .forge/water-wood-fx/hu_khong_kien_moc/frames-qc-v4/sheet-qc.json | seam | warn | 1.73 | 1.5 |
| .forge/water-wood-fx/hu_khong_kien_moc/raw-qc/sheet-qc.json | safe_frame | warn | 10 | 0 |
| .forge/water-wood-fx/lam_khoa/final-qc/sheet-qc.json | safe_frame | warn | 11 | 0 |
| .forge/water-wood-fx/lam_khoa/final-qc/sheet-qc.json | faint_specks | warn | {'floor_px': 18088, 'detached_components': 32} | 0 |
| .forge/water-wood-fx/lam_khoa/frames-qc-v4/sheet-qc.json | identity_head_ratio | fail | {'0': 1.01, '1': 0.909, '2': 1.091, '3': 1.091, '4': 0.99, '5': 1.111, '6': 0.909, '7': 0.949, '8': 0.949, '9': 1.091, '10': 0.909, '11': 0.909, '12': 1.091, '13': 0.909, '14': 1.051, '15': 1.091} | [0.95, 1.05] |
| .forge/water-wood-fx/lam_khoa/frames-qc-v4/sheet-qc.json | torso_drift | warn | 117.31 | 2.0 |
| .forge/water-wood-fx/lam_khoa/frames-qc-v4/sheet-qc.json | row_baseline | warn | 9.0 | 0.5 |
| .forge/water-wood-fx/lam_khoa/frames-qc-v4/sheet-qc.json | seam | warn | 1.55 | 1.5 |
| .forge/water-wood-fx/lam_khoa/processed-v4/pipeline-meta.json | source_edge_touch_frames | warn | [[1, 3]] | [] |
| .forge/water-wood-fx/lam_khoa/raw-qc/sheet-qc.json | boundary_band | warn | 11 | 0 |
| .forge/water-wood-fx/lam_khoa/raw-qc/sheet-qc.json | safe_frame | warn | 13 | 0 |
| .forge/water-wood-fx/thanh_de/final-qc/sheet-qc.json | safe_frame | warn | 13 | 0 |
| .forge/water-wood-fx/thanh_de/final-qc/sheet-qc.json | faint_specks | warn | {'floor_px': 20177, 'detached_components': 29} | 0 |
| .forge/water-wood-fx/thanh_de/final-qc-v3/sheet-qc.json | safe_frame | warn | 10 | 0 |
| .forge/water-wood-fx/thanh_de/final-qc-v3/sheet-qc.json | faint_specks | warn | {'floor_px': 19211, 'detached_components': 43} | 0 |
| .forge/water-wood-fx/thanh_de/frames-qc-v3/sheet-qc.json | identity_head_ratio | fail | {'0': 1.087, '1': 0.978, '2': 1.109, '3': 1.174, '4': 1.022, '5': 0.978, '6': 1.174, '7': 1.022, '8': 0.978, '9': 0.978, '10': 0.978, '11': 1.022, '12': 0.978, '13': 0.978, '14': 1.022, '15': 0.978} | [0.95, 1.05] |
| .forge/water-wood-fx/thanh_de/frames-qc-v3/sheet-qc.json | torso_drift | warn | 64.47 | 2.0 |
| .forge/water-wood-fx/thanh_de/frames-qc-v3/sheet-qc.json | row_baseline | warn | 18.0 | 0.5 |
| .forge/water-wood-fx/thanh_de/frames-qc-v3/sheet-qc.json | seam | warn | 1.66 | 1.5 |
| .forge/water-wood-fx/thanh_de/frames-qc-v4/sheet-qc.json | identity_head_ratio | fail | {'0': 0.962, '1': 1.0, '2': 1.058, '3': 1.0, '4': 1.058, '5': 0.865, '6': 1.0, '7': 0.865, '8': 0.904, '9': 0.885, '10': 1.058, '11': 1.0, '12': 1.058, '13': 1.058, '14': 1.058, '15': 0.885} | [0.95, 1.05] |
| .forge/water-wood-fx/thanh_de/frames-qc-v4/sheet-qc.json | torso_drift | warn | 27.74 | 2.0 |
| .forge/water-wood-fx/thanh_de/frames-qc-v4/sheet-qc.json | row_baseline | warn | 13.0 | 0.5 |
| .forge/water-wood-fx/thanh_de/raw-qc/sheet-qc.json | cross_cell_components | fail | 1 | 0 |
| .forge/water-wood-fx/thanh_de/raw-qc/sheet-qc.json | cross_cell_visible | fail | 1 | 0 |
| .forge/water-wood-fx/thanh_de/raw-qc/sheet-qc.json | sheet_edge | fail | 1 | 0 |
| .forge/water-wood-fx/thanh_de/raw-qc/sheet-qc.json | boundary_band | warn | 2614 | 0 |
| .forge/water-wood-fx/thanh_de/raw-qc/sheet-qc.json | safe_frame | warn | 13 | 0 |
| .forge/water-wood-fx/thanh_diep_nhan/final-qc/sheet-qc.json | safe_frame | warn | 7 | 0 |
| .forge/water-wood-fx/thanh_diep_nhan/final-qc/sheet-qc.json | faint_specks | warn | {'floor_px': 12617, 'detached_components': 32} | 0 |
| .forge/water-wood-fx/thanh_diep_nhan/final-qc-v4/sheet-qc.json | safe_frame | warn | 7 | 0 |
| .forge/water-wood-fx/thanh_diep_nhan/final-qc-v4/sheet-qc.json | faint_specks | warn | {'floor_px': 12617, 'detached_components': 32} | 0 |
| .forge/water-wood-fx/thanh_diep_nhan/frames-qc-v4/sheet-qc.json | identity_head_ratio | fail | {'0': 1.111, '1': 1.0, '2': 1.0, '3': 1.0, '4': 1.0, '5': 1.133, '6': 1.178, '7': 1.067, '8': 1.178, '9': 1.0, '10': 1.0, '11': 1.089, '12': 1.0, '13': 1.089, '14': 1.0, '15': 1.0} | [0.95, 1.05] |
| .forge/water-wood-fx/thanh_diep_nhan/frames-qc-v4/sheet-qc.json | torso_drift | warn | 38.41 | 2.0 |
| .forge/water-wood-fx/thanh_diep_nhan/frames-qc-v4/sheet-qc.json | row_baseline | warn | 32.0 | 0.5 |
| .forge/water-wood-fx/thanh_diep_nhan/frames-qc-v4/sheet-qc.json | seam | warn | 3.1 | 1.5 |
| .forge/water-wood-fx/thanh_diep_nhan/processed-v4/pipeline-meta.json | source_edge_touch_frames | warn | [[1, 3]] | [] |
| .forge/water-wood-fx/thanh_diep_nhan/raw-qc/sheet-qc.json | cross_cell_components | fail | 1 | 0 |
| .forge/water-wood-fx/thanh_diep_nhan/raw-qc/sheet-qc.json | cross_cell_visible | fail | 1 | 0 |
| .forge/water-wood-fx/thanh_diep_nhan/raw-qc/sheet-qc.json | sheet_edge | fail | 1 | 0 |
| .forge/water-wood-fx/thanh_diep_nhan/raw-qc/sheet-qc.json | boundary_band | warn | 2591 | 0 |
| .forge/water-wood-fx/thanh_diep_nhan/raw-qc/sheet-qc.json | safe_frame | warn | 9 | 0 |
| .forge/water-wood-fx/thien_ha/final-qc/sheet-qc.json | safe_frame | warn | 10 | 0 |
| .forge/water-wood-fx/thien_ha/final-qc/sheet-qc.json | faint_specks | warn | {'floor_px': 13789, 'detached_components': 0} | 0 |
| .forge/water-wood-fx/thien_ha/final-qc-v3/sheet-qc.json | safe_frame | warn | 10 | 0 |
| .forge/water-wood-fx/thien_ha/final-qc-v3/sheet-qc.json | faint_specks | warn | {'floor_px': 13640, 'detached_components': 38} | 0 |
| .forge/water-wood-fx/thien_ha/final-qc-v4/sheet-qc.json | safe_frame | warn | 11 | 0 |
| .forge/water-wood-fx/thien_ha/final-qc-v4/sheet-qc.json | faint_specks | warn | {'floor_px': 13789, 'detached_components': 0} | 0 |
| .forge/water-wood-fx/thien_ha/frames-qc/sheet-qc.json | identity_head_ratio | fail | {'0': 1.075, '1': 1.118, '2': 0.968, '3': 0.968, '4': 0.968, '5': 0.968, '6': 0.968, '7': 1.032, '8': 0.968, '9': 1.032, '10': 1.032, '11': 1.032, '12': 1.032, '13': 1.032, '14': 0.968, '15': 0.968} | [0.95, 1.05] |
| .forge/water-wood-fx/thien_ha/frames-qc/sheet-qc.json | row_baseline | warn | 26.0 | 0.5 |
| .forge/water-wood-fx/thien_ha/frames-qc/sheet-qc.json | seam | warn | 1.9 | 1.5 |
| .forge/water-wood-fx/thien_ha/frames-qc-v3/sheet-qc.json | identity_head_ratio | fail | {'0': 1.043, '1': 1.021, '2': 1.106, '3': 0.957, '4': 1.149, '5': 1.17, '6': 1.0, '7': 1.0, '8': 1.0, '9': 1.0, '10': 1.021, '11': 1.0, '12': 1.043, '13': 1.0, '14': 1.0, '15': 1.0} | [0.95, 1.05] |
| .forge/water-wood-fx/thien_ha/frames-qc-v3/sheet-qc.json | torso_drift | warn | 26.44 | 2.0 |
| .forge/water-wood-fx/thien_ha/frames-qc-v3/sheet-qc.json | row_baseline | warn | 25.0 | 0.5 |
| .forge/water-wood-fx/thien_ha/frames-qc-v3/sheet-qc.json | seam | warn | 1.97 | 1.5 |
| .forge/water-wood-fx/thien_ha/frames-qc-v4/sheet-qc.json | identity_head_ratio | fail | {'0': 1.075, '1': 1.118, '2': 0.968, '3': 0.968, '4': 0.968, '5': 0.968, '6': 0.968, '7': 1.032, '8': 0.968, '9': 1.032, '10': 1.032, '11': 1.032, '12': 1.032, '13': 1.032, '14': 0.968, '15': 0.968} | [0.95, 1.05] |
| .forge/water-wood-fx/thien_ha/frames-qc-v4/sheet-qc.json | row_baseline | warn | 26.0 | 0.5 |
| .forge/water-wood-fx/thien_ha/frames-qc-v4/sheet-qc.json | seam | warn | 1.9 | 1.5 |
| .forge/water-wood-fx/thien_ha/processed-v2/pipeline-meta.json | source_edge_touch_frames | warn | [[1, 2]] | [] |
| .forge/water-wood-fx/thien_ha/processed-v3/pipeline-meta.json | source_edge_touch_frames | warn | [[1, 2]] | [] |
| .forge/water-wood-fx/thien_ha/processed-v4/pipeline-meta.json | source_edge_touch_frames | warn | [[1, 2]] | [] |
| .forge/water-wood-fx/thien_ha/raw-qc/sheet-qc.json | cross_cell_components | fail | 1 | 0 |
| .forge/water-wood-fx/thien_ha/raw-qc/sheet-qc.json | cross_cell_visible | fail | 1 | 0 |
| .forge/water-wood-fx/thien_ha/raw-qc/sheet-qc.json | sheet_edge | fail | 1 | 0 |
| .forge/water-wood-fx/thien_ha/raw-qc/sheet-qc.json | boundary_band | warn | 2601 | 0 |
| .forge/water-wood-fx/thien_ha/raw-qc/sheet-qc.json | safe_frame | warn | 15 | 0 |
| .forge/water-wood-fx/thuong_hai/final-qc/sheet-qc.json | safe_frame | warn | 11 | 0 |
| .forge/water-wood-fx/thuong_hai/final-qc/sheet-qc.json | faint_specks | warn | {'floor_px': 19228, 'detached_components': 2} | 0 |
| .forge/water-wood-fx/thuong_hai/final-qc-v3/sheet-qc.json | safe_frame | warn | 11 | 0 |
| .forge/water-wood-fx/thuong_hai/final-qc-v3/sheet-qc.json | faint_specks | warn | {'floor_px': 17578, 'detached_components': 84} | 0 |
| .forge/water-wood-fx/thuong_hai/final-qc-v4/sheet-qc.json | safe_frame | warn | 13 | 0 |
| .forge/water-wood-fx/thuong_hai/final-qc-v4/sheet-qc.json | faint_specks | warn | {'floor_px': 19228, 'detached_components': 2} | 0 |
| .forge/water-wood-fx/thuong_hai/frames-qc/sheet-qc.json | identity_head_ratio | fail | {'0': 1.111, '1': 1.0, '2': 1.0, '3': 1.133, '4': 1.0, '5': 1.0, '6': 1.067, '7': 1.0, '8': 1.044, '9': 1.0, '10': 1.0, '11': 1.022, '12': 1.044, '13': 1.0, '14': 1.0, '15': 1.0} | [0.95, 1.05] |
| .forge/water-wood-fx/thuong_hai/frames-qc/sheet-qc.json | row_baseline | warn | 14.0 | 0.5 |
| .forge/water-wood-fx/thuong_hai/frames-qc/sheet-qc.json | seam | warn | 2.03 | 1.5 |
| .forge/water-wood-fx/thuong_hai/frames-qc-v3/sheet-qc.json | identity_head_ratio | fail | {'0': 1.111, '1': 1.0, '2': 1.0, '3': 1.0, '4': 1.222, '5': 1.044, '6': 1.222, '7': 1.0, '8': 1.0, '9': 1.022, '10': 1.0, '11': 1.022, '12': 1.0, '13': 1.0, '14': 1.022, '15': 1.0} | [0.95, 1.05] |
| .forge/water-wood-fx/thuong_hai/frames-qc-v3/sheet-qc.json | row_baseline | warn | 14.0 | 0.5 |
| .forge/water-wood-fx/thuong_hai/frames-qc-v3/sheet-qc.json | seam | warn | 2.04 | 1.5 |
| .forge/water-wood-fx/thuong_hai/frames-qc-v4/sheet-qc.json | identity_head_ratio | fail | {'0': 1.111, '1': 1.0, '2': 1.0, '3': 1.133, '4': 1.0, '5': 1.0, '6': 1.067, '7': 1.0, '8': 1.044, '9': 1.0, '10': 1.0, '11': 1.022, '12': 1.044, '13': 1.0, '14': 1.0, '15': 1.0} | [0.95, 1.05] |
| .forge/water-wood-fx/thuong_hai/frames-qc-v4/sheet-qc.json | row_baseline | warn | 14.0 | 0.5 |
| .forge/water-wood-fx/thuong_hai/frames-qc-v4/sheet-qc.json | seam | warn | 2.03 | 1.5 |
| .forge/water-wood-fx/thuong_hai/raw-qc/sheet-qc.json | safe_frame | warn | 12 | 0 |
| .forge/water-wood-fx/thuy_cau/clips/animation-clips.json | tick_grid | warn | 8.333 | None |
| .forge/water-wood-fx/thuy_cau/final-qc/sheet-qc.json | safe_frame | warn | 5 | 0 |
| .forge/water-wood-fx/thuy_cau/final-qc/sheet-qc.json | faint_specks | warn | {'floor_px': 14232, 'detached_components': 0} | 0 |
| .forge/water-wood-fx/thuy_cau/final-qc-v3/sheet-qc.json | safe_frame | warn | 5 | 0 |
| .forge/water-wood-fx/thuy_cau/final-qc-v3/sheet-qc.json | faint_specks | warn | {'floor_px': 13517, 'detached_components': 20} | 0 |
| .forge/water-wood-fx/thuy_cau/final-qc-v4/sheet-qc.json | safe_frame | warn | 7 | 0 |
| .forge/water-wood-fx/thuy_cau/final-qc-v4/sheet-qc.json | faint_specks | warn | {'floor_px': 14232, 'detached_components': 0} | 0 |
| .forge/water-wood-fx/thuy_cau/frames-qc/sheet-qc.json | identity_head_ratio | fail | {'0': 1.064, '1': 1.021, '2': 1.0, '3': 1.021, '4': 1.085, '5': 1.149, '6': 0.979, '7': 1.0, '8': 0.957, '9': 0.957, '10': 1.0, '11': 1.043, '12': 1.0, '13': 1.0, '14': 1.149, '15': 1.0} | [0.95, 1.05] |
| .forge/water-wood-fx/thuy_cau/frames-qc/sheet-qc.json | torso_drift | warn | 18.54 | 2.0 |
| .forge/water-wood-fx/thuy_cau/frames-qc/sheet-qc.json | row_baseline | warn | 33.0 | 0.5 |
| .forge/water-wood-fx/thuy_cau/frames-qc/sheet-qc.json | seam | warn | 2.82 | 1.5 |
| .forge/water-wood-fx/thuy_cau/frames-qc-v3/sheet-qc.json | identity_head_ratio | fail | {'0': 1.087, '1': 1.0, '2': 1.0, '3': 1.043, '4': 0.978, '5': 1.196, '6': 0.978, '7': 1.0, '8': 1.109, '9': 1.109, '10': 0.978, '11': 1.196, '12': 0.978, '13': 0.978, '14': 1.152, '15': 0.978} | [0.95, 1.05] |
| .forge/water-wood-fx/thuy_cau/frames-qc-v3/sheet-qc.json | torso_drift | warn | 20.76 | 2.0 |
| .forge/water-wood-fx/thuy_cau/frames-qc-v3/sheet-qc.json | row_baseline | warn | 33.0 | 0.5 |
| .forge/water-wood-fx/thuy_cau/frames-qc-v3/sheet-qc.json | seam | warn | 2.83 | 1.5 |
| .forge/water-wood-fx/thuy_cau/frames-qc-v4/sheet-qc.json | identity_head_ratio | fail | {'0': 1.064, '1': 1.021, '2': 1.0, '3': 1.021, '4': 1.085, '5': 1.149, '6': 0.979, '7': 1.0, '8': 0.957, '9': 0.957, '10': 1.0, '11': 1.043, '12': 1.0, '13': 1.0, '14': 1.149, '15': 1.0} | [0.95, 1.05] |
| .forge/water-wood-fx/thuy_cau/frames-qc-v4/sheet-qc.json | torso_drift | warn | 18.54 | 2.0 |
| .forge/water-wood-fx/thuy_cau/frames-qc-v4/sheet-qc.json | row_baseline | warn | 33.0 | 0.5 |
| .forge/water-wood-fx/thuy_cau/frames-qc-v4/sheet-qc.json | seam | warn | 2.82 | 1.5 |
| .forge/water-wood-fx/thuy_cau/raw-qc/sheet-qc.json | safe_frame | warn | 10 | 0 |
| .forge/water-wood-fx/thuy_lao/final-qc/sheet-qc.json | safe_frame | warn | 9 | 0 |
| .forge/water-wood-fx/thuy_lao/final-qc/sheet-qc.json | faint_specks | warn | {'floor_px': 17006, 'detached_components': 0} | 0 |
| .forge/water-wood-fx/thuy_lao/final-qc-v3/sheet-qc.json | safe_frame | warn | 9 | 0 |
| .forge/water-wood-fx/thuy_lao/final-qc-v3/sheet-qc.json | faint_specks | warn | {'floor_px': 16364, 'detached_components': 38} | 0 |
| .forge/water-wood-fx/thuy_lao/final-qc-v4/sheet-qc.json | safe_frame | warn | 9 | 0 |
| .forge/water-wood-fx/thuy_lao/final-qc-v4/sheet-qc.json | faint_specks | warn | {'floor_px': 17006, 'detached_components': 0} | 0 |
| .forge/water-wood-fx/thuy_lao/frames-qc/sheet-qc.json | identity_head_ratio | fail | {'0': 1.042, '1': 1.021, '2': 0.979, '3': 1.062, '4': 0.938, '5': 1.062, '6': 0.938, '7': 1.042, '8': 1.083, '9': 0.938, '10': 1.021, '11': 0.938, '12': 0.938, '13': 0.938, '14': 0.938, '15': 1.104} | [0.95, 1.05] |
| .forge/water-wood-fx/thuy_lao/frames-qc/sheet-qc.json | torso_drift | warn | 43.91 | 2.0 |
| .forge/water-wood-fx/thuy_lao/frames-qc/sheet-qc.json | row_baseline | warn | 21.0 | 0.5 |
| .forge/water-wood-fx/thuy_lao/frames-qc/sheet-qc.json | seam | warn | 1.6 | 1.5 |
| .forge/water-wood-fx/thuy_lao/frames-qc-v3/sheet-qc.json | identity_head_ratio | fail | {'0': 1.099, '1': 1.011, '2': 0.989, '3': 0.989, '4': 1.011, '5': 0.989, '6': 0.989, '7': 1.209, '8': 1.011, '9': 0.989, '10': 0.989, '11': 0.989, '12': 1.121, '13': 1.033, '14': 0.989, '15': 1.011} | [0.95, 1.05] |
| .forge/water-wood-fx/thuy_lao/frames-qc-v3/sheet-qc.json | torso_drift | warn | 58.96 | 2.0 |
| .forge/water-wood-fx/thuy_lao/frames-qc-v3/sheet-qc.json | row_baseline | warn | 19.0 | 0.5 |
| .forge/water-wood-fx/thuy_lao/frames-qc-v3/sheet-qc.json | seam | warn | 1.69 | 1.5 |
| .forge/water-wood-fx/thuy_lao/frames-qc-v4/sheet-qc.json | identity_head_ratio | fail | {'0': 1.042, '1': 1.021, '2': 0.979, '3': 1.062, '4': 0.938, '5': 1.062, '6': 0.938, '7': 1.042, '8': 1.083, '9': 0.938, '10': 1.021, '11': 0.938, '12': 0.938, '13': 0.938, '14': 0.938, '15': 1.104} | [0.95, 1.05] |
| .forge/water-wood-fx/thuy_lao/frames-qc-v4/sheet-qc.json | torso_drift | warn | 43.91 | 2.0 |
| .forge/water-wood-fx/thuy_lao/frames-qc-v4/sheet-qc.json | row_baseline | warn | 21.0 | 0.5 |
| .forge/water-wood-fx/thuy_lao/frames-qc-v4/sheet-qc.json | seam | warn | 1.6 | 1.5 |
| .forge/water-wood-fx/thuy_lao/raw-qc/sheet-qc.json | safe_frame | warn | 11 | 0 |
| .forge/water-wood-fx/thuy_nguyet/clips/animation-clips.json | tick_grid | warn | 8.333 | None |
| .forge/water-wood-fx/thuy_nguyet/final-qc/sheet-qc.json | safe_frame | warn | 7 | 0 |
| .forge/water-wood-fx/thuy_nguyet/final-qc/sheet-qc.json | faint_specks | warn | {'floor_px': 16104, 'detached_components': 0} | 0 |
| .forge/water-wood-fx/thuy_nguyet/final-qc-v3/sheet-qc.json | safe_frame | warn | 5 | 0 |
| .forge/water-wood-fx/thuy_nguyet/final-qc-v3/sheet-qc.json | faint_specks | warn | {'floor_px': 15481, 'detached_components': 45} | 0 |
| .forge/water-wood-fx/thuy_nguyet/final-qc-v4/sheet-qc.json | safe_frame | warn | 7 | 0 |
| .forge/water-wood-fx/thuy_nguyet/final-qc-v4/sheet-qc.json | faint_specks | warn | {'floor_px': 16104, 'detached_components': 0} | 0 |
| .forge/water-wood-fx/thuy_nguyet/frames-qc/sheet-qc.json | identity_head_ratio | fail | {'0': 1.087, '1': 1.152, '2': 1.043, '3': 1.0, '4': 0.978, '5': 1.043, '6': 1.0, '7': 0.978, '8': 1.109, '9': 1.0, '10': 0.978, '11': 0.978, '12': 0.978, '13': 1.0, '14': 1.0, '15': 0.978} | [0.95, 1.05] |
| .forge/water-wood-fx/thuy_nguyet/frames-qc/sheet-qc.json | torso_drift | warn | 70.0 | 2.0 |
| .forge/water-wood-fx/thuy_nguyet/frames-qc/sheet-qc.json | row_baseline | warn | 14.0 | 0.5 |
| .forge/water-wood-fx/thuy_nguyet/frames-qc/sheet-qc.json | seam | warn | 2.17 | 1.5 |
| .forge/water-wood-fx/thuy_nguyet/frames-qc-v3/sheet-qc.json | identity_head_ratio | fail | {'0': 1.087, '1': 0.978, '2': 1.043, '3': 1.0, '4': 1.043, '5': 1.043, '6': 0.978, '7': 0.978, '8': 0.978, '9': 0.978, '10': 0.978, '11': 1.13, '12': 0.978, '13': 1.0, '14': 1.0, '15': 1.0} | [0.95, 1.05] |
| .forge/water-wood-fx/thuy_nguyet/frames-qc-v3/sheet-qc.json | torso_drift | warn | 68.94 | 2.0 |
| .forge/water-wood-fx/thuy_nguyet/frames-qc-v3/sheet-qc.json | row_baseline | warn | 16.0 | 0.5 |
| .forge/water-wood-fx/thuy_nguyet/frames-qc-v3/sheet-qc.json | seam | warn | 2.2 | 1.5 |
| .forge/water-wood-fx/thuy_nguyet/frames-qc-v4/sheet-qc.json | identity_head_ratio | fail | {'0': 1.087, '1': 1.152, '2': 1.043, '3': 1.0, '4': 0.978, '5': 1.043, '6': 1.0, '7': 0.978, '8': 1.109, '9': 1.0, '10': 0.978, '11': 0.978, '12': 0.978, '13': 1.0, '14': 1.0, '15': 0.978} | [0.95, 1.05] |
| .forge/water-wood-fx/thuy_nguyet/frames-qc-v4/sheet-qc.json | torso_drift | warn | 70.0 | 2.0 |
| .forge/water-wood-fx/thuy_nguyet/frames-qc-v4/sheet-qc.json | row_baseline | warn | 14.0 | 0.5 |
| .forge/water-wood-fx/thuy_nguyet/frames-qc-v4/sheet-qc.json | seam | warn | 2.17 | 1.5 |
| .forge/water-wood-fx/thuy_nguyet/processed/pipeline-meta.json | source_edge_touch_frames | warn | [[0, 0], [0, 1], [0, 2], [0, 3], [1, 0], [1, 1], [1, 2], [1, 3], [2, 0], [2, 1], [2, 2], [2, 3], [3, 0], [3, 1], [3, 2]] | [] |
| .forge/water-wood-fx/thuy_nguyet/raw-qc/sheet-qc.json | cross_cell_components | fail | 5 | 0 |
| .forge/water-wood-fx/thuy_nguyet/raw-qc/sheet-qc.json | cross_cell_visible | fail | 5 | 0 |
| .forge/water-wood-fx/thuy_nguyet/raw-qc/sheet-qc.json | boundary_band | warn | 2475 | 0 |
| .forge/water-wood-fx/thuy_nguyet/raw-qc/sheet-qc.json | safe_frame | warn | 13 | 0 |
| .forge/water-wood-fx/van_thuy_tien/final-qc/sheet-qc.json | safe_frame | warn | 8 | 0 |
| .forge/water-wood-fx/van_thuy_tien/final-qc/sheet-qc.json | faint_specks | warn | {'floor_px': 16176, 'detached_components': 0} | 0 |
| .forge/water-wood-fx/van_thuy_tien/final-qc-v3/sheet-qc.json | empty_cells | fail | [[3, 3]] | [] |
| .forge/water-wood-fx/van_thuy_tien/final-qc-v3/sheet-qc.json | safe_frame | warn | 8 | 0 |
| .forge/water-wood-fx/van_thuy_tien/final-qc-v3/sheet-qc.json | faint_specks | warn | {'floor_px': 16465, 'detached_components': 57} | 0 |
| .forge/water-wood-fx/van_thuy_tien/final-qc-v4/sheet-qc.json | safe_frame | warn | 8 | 0 |
| .forge/water-wood-fx/van_thuy_tien/final-qc-v4/sheet-qc.json | faint_specks | warn | {'floor_px': 16176, 'detached_components': 0} | 0 |
| .forge/water-wood-fx/van_thuy_tien/frames-qc/sheet-qc.json | identity_head_ratio | fail | {'0': 1.111, '1': 1.156, '2': 1.022, '3': 1.022, '4': 1.0, '5': 1.0, '6': 1.0, '7': 1.0, '8': 1.178, '9': 1.022, '10': 1.0, '11': 1.0, '12': 1.111, '13': 1.0, '14': 1.0, '15': 1.0} | [0.95, 1.05] |
| .forge/water-wood-fx/van_thuy_tien/frames-qc/sheet-qc.json | torso_drift | warn | 52.08 | 2.0 |
| .forge/water-wood-fx/van_thuy_tien/frames-qc/sheet-qc.json | row_baseline | warn | 19.0 | 0.5 |
| .forge/water-wood-fx/van_thuy_tien/frames-qc/sheet-qc.json | seam | warn | 1.92 | 1.5 |
| .forge/water-wood-fx/van_thuy_tien/frames-qc-v4/sheet-qc.json | identity_head_ratio | fail | {'0': 1.111, '1': 1.156, '2': 1.022, '3': 1.022, '4': 1.0, '5': 1.0, '6': 1.0, '7': 1.0, '8': 1.178, '9': 1.022, '10': 1.0, '11': 1.0, '12': 1.111, '13': 1.0, '14': 1.0, '15': 1.0} | [0.95, 1.05] |
| .forge/water-wood-fx/van_thuy_tien/frames-qc-v4/sheet-qc.json | torso_drift | warn | 52.08 | 2.0 |
| .forge/water-wood-fx/van_thuy_tien/frames-qc-v4/sheet-qc.json | row_baseline | warn | 19.0 | 0.5 |
| .forge/water-wood-fx/van_thuy_tien/frames-qc-v4/sheet-qc.json | seam | warn | 1.92 | 1.5 |
| .forge/water-wood-fx/van_thuy_tien/raw-qc/sheet-qc.json | safe_frame | warn | 10 | 0 |

## Kiểm tra cuối

- PASS: 16 atlas 768×768, 256 khung có alpha hiển thị; xem final-integrity.json.
- PASS: ánh xạ 16 chiêu qua 3 cảnh giới, đủ 16 frame, pha nghỉ, input không hợp lệ, biến thể Hỏa và giới hạn cache.
- Gallery: xác nhận 16 tên chiêu, nút dừng/tiếp tục, xem từng khung (khung 10/16), đổi nền sáng; ảnh kiểm tra gallery-preview.png.
- Chưa kiểm thử trận đấu thực tế với tài khoản game; tích hợp renderer đã kiểm tra cú pháp và timeline.


## Hiệu chỉnh vị trí và tỷ lệ

- Dùng kích thước ô ảnh quái thực tế (thường, tinh anh ×1,25, Boss ×1,5) thay cho chiều cao tu sĩ.
- Điểm neo mặt đất theo biên alpha của sprite; Cổ Mộc và Thanh Đế đặt đầu rễ về phía mục tiêu. Lao/cage đặt đáy ở chân quái.
- Tỷ lệ riêng theo chiêu; phần ảnh trong suốt không làm tuyệt kỹ bị thu nhỏ khi sát mép canvas.
- PASS 288 trường hợp vị trí: 16 chiêu × 3 cỡ quái × 2 viewport × 3 khung va chạm. Kiểm tra biên hiển thị, neo và kích thước ổn định.
- Trang đối chiếu dùng renderer thật: /fx-preview/layout.html.
