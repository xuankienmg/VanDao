# Thất Thải Hồ Điệp — Thượng Cổ

Người dùng chọn thay Kỳ Lân bằng Hồ Điệp, sau đó chốt tên **Thất Thải Hồ Điệp**, ảnh trắng trong suốt ánh lam/tím theo ảnh tham khảo và **trứng Thượng Cổ riêng**. Lựa chọn 1% từ trứng Thiên đã được thay thế.

- ID `that_thai_ho_diep`, rarity `ancient`, phẩm Thượng Cổ cao hơn Thiên, hệ Mộc, hỗ trợ hồi phục và bảo hộ. Giữ 12 loài cũ.
- Tư chất 90–100; hệ số chỉ số phẩm 3,2 so với Thiên 2,4. Cấp/sao/tiến hóa/ngoại hình dùng cơ chế hiện có; không đổi schema DB.
- Thiên phú **Thất Thải Tiên Duyên**: +18% hiệu quả hồi máu ở mọi ngưỡng HP, tăng theo sao.
- Tuyệt kỹ mỗi 3 lượt chủ nhân: **Thất Thải Linh Quang / Điệp Vũ Hồi Xuân / Thượng Cổ Thất Thải**. Hồi 5/8/11% HP tối đa, tạo khiên 6/10/14% HP tối đa; tăng theo sao. Không hồi sinh/không vượt HP tối đa, trần khiên 30% HP.
- **Thượng Cổ Thú Đản** (`thu_dan_thuong_co`), ấp 12 giờ, chắc chắn nở Thất Thải Hồ Điệp. Trứng Thiên chỉ nở 4 thần thú Thiên, không có cơ hội nở Thượng Cổ.
- Quay Linh Thú có 6 ô: Phàm45%, Linh30%, Huyền17%, Địa7%, Thiên0,9%, Thượng Cổ0,1%. Chỉ chuyển 0,1 điểm phần trăm từ Thiên. Không thêm trứng Thượng Cổ vào rương Boss.
- Admin phát triển: nút **Nhận trứng Thượng Cổ ×1**, dùng API give hiện có; bản phát hành không mở quyền Admin. Trứng cũng xuất hiện trong danh sách vật phẩm Admin.
- UI có nhãn/phân loại/sắp xếp phẩm mới, vòng quay và kết quả/lịch sử hỗ trợ ô thứ6; không thay đổi mười phẩm trang bị. Sprite3dạng và icon trứng riêng; FX hộ sinh trắng ngọc/lam tím.

## Kiểm chứng

- 83 kiểm tra Hồ Điệp: phẩm, trứng riêng, Admin, ấp/nhận thật, tư chất, hồi máu/khiên/sao/DTO/giới hạn.
- 92 kiểm tra chu kỳ kỹ năng trên 13 loài; kiểm tra feedback JS qua.
- 2.799 kiểm tra hồi quy vật phẩm/lưu trữ/di trú/rương qua.
- Giao diện thật với SQLite riêng: Admin nhận trứng, ấp ngay/nhận Hồ Điệp, xuất chiến, nhãn Thượng Cổ, vòng6ô/tỉ lệ0,1%/popup thưởng, ảnh và chiến đấu420×300 đều qua.
- Build riêng `bin/butterfly-check/NguoiLuoiTuTien.exe` thành công; có cảnh báo WindowsBase sẵn có.
- Công cụ kiểm tra nằm trong thư mục phát triển `tools/` đang được repo ignore: `pet_cadence_checks`, `check_butterfly_ui.cjs`, `prepare_butterfly.py`. Không đổi cấu hình ignore, không tự commit/phát hành.


Cập nhật tiếp theo cùngngày: trứng ThượngCổ nay chung2loài, ThấtThảiHồĐiệp/TháiCổLôiLong50% mỗi loài; xem kếhoạch `2026-10-22-thai-co-loi-long.md`. Tỉlệ nhậntrứng0,1% và thời gian12giờ giữnguyên.
