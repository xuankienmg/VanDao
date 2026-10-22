# Thái Cổ Lôi Long

Người dùng yêu cầu bộ ảnh Ấu thể → Trưởng thành → Thượng Cổ theo concept rồng ba đầu, chiêu sét tím từ trời xuống. Đã chốt **mỗi 3 lượt, 50% tê liệt trong 1 lượt**; yêu cầu 3 lượt tê liệt trước đó đã thay thế.

- Thêm loài Thượng Cổ hệ Lôi, ID `thai_co_loi_long`, vào trứng Thượng Cổ chung với Thất Thải Hồ Điệp: mỗi loài50%; không đổi tỉ lệ nhận trứng0,1% hay thời gianấp12giờ.
- Thiên phú Lôi Uy Xuyên Giáp: +12% xuyên thủ chủ nhân, hưởng sao/trần xuyên thủ đang có.
- Kỹ năng **Tử Điện Giáng / Thiên Lôi Phán Quyết / Thái Cổ Lôi Phạt**:100/150/200% công chủ nhân, tăng theo sao. Mỗi3lượt chủ nhân kích hoạt;50% têliệt làm quái bỏ một lượt phản công ngay lượt trúng chiêu. Xác suất cố định, không tăng theo sao; làm mới, không cộng dồn; xóa khi sang quái mới.
- RNG riêng theo hạt giống chuyến, không tiêu thụ RNG đòn đánh/nè/trang bị cũ. Server trả sự kiện têliệt; client vẽ sét tím giáng từ trên trời và nhãn têliệt khi server xác nhận.
- Ảnh3dạng riêng alpha, cùng identity3đầu/vảy bạc đen/giáp vàng cổ/bờm trắng, quayphải. Bổ sung vào Thú điển, xuất chiến và các cỡ ảnhpet hiệncó; không thay dữliệu loài cũ.

Kiểm tra: biênngẫu nhiên50%, têliệt đúng1lượt và làm mới/xóa khi đổiquái, chu kỳ3lượt, damage3dạng+sao; deterministicbattle và no-pet unchanged; cập nhật testtrứng2loài; hiệuứng client/ảnh/build/UI.


## Hoàn tất

- Đã xuất12ảnh alpha cho3dạng/4cỡ. Build desktop riêng `bin/thunder-check/NguoiLuoiTuTien.exe` qua (cảnhbáoWindowsBase cósẵn).
-106kiểmtra LôiLong +83HồĐiệp +98chu kỳ14loài đều qua;2.799kiểmtra hồi quy vậtphẩm/rương/lưutrữ qua.
- FX kiểmtra tia bắtđầu ởtrời, kếtthúc ởquái, nhãn têliệt chỉtheo server và khônglặp, giảmchuyểnđộng antoàn.
- UI/API thật trên DB thửriêng:14loài, ảnh/menu3dạng, mô tả50%/1lượt, xuấtchiến420×300 và khônglỗiJS. Không sửa save thật/không commit/phát hành.
