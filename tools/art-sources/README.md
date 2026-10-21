# Ảnh nguồn thiết kế

`skin/` giữ các bản gốc dùng để tạo atlas nhân vật. Chúng không được tải bởi game và không nằm trong gói publish.

- `import_skin_flight.py`: ghi `skin/*_flight_source.png`, xuất atlas vào wwwroot.
- `import_thanh_sam_actions.py`: ghi `skin/luyenkhi_actions_clean.png`.
- `import_thanh_sam_run.py`: ghi `skin/luyenkhi_run_source.png`.
- `pack_inventory_breeze.py`: đọc `skin/luyenkhi_idle_breeze_sheet.png`.

Các công cụ được chạy từ thư mục nào cũng dùng đường dẫn tính từ vị trí script.
