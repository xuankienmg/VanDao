# Thần thông Linh thú Thượng Cổ và hiệu ứng

Áp dụng hai thần thông đã chốt, chỉ cho **phẩm Thượng Cổ**, không áp dụng cho thú Thiên trở xuống. Đây là phẩm của thú, không phải điều kiện hình thái cuối. Không thay đổi tuyệt kỹ thường đã có; yêu cầu hiện tại là thần thông và hiệu ứng.

- Thất Thải Niết Bàn: khi chủ nhân thực sự chết do đòn quái sau xử lý cứu mạng trang bị, hồi sinh25% HP tối đa. Một lần cả trận, không reset khi đổi đợt; không thêm khiên/bất tử, không nhân hồi máu hoặc sao.
- Thái Cổ Thiên Kiếp: khi còn sống và HP≤30%, gây250% công chủ nhân, bỏqua30% phòng thủ, chắc chắn têliệt1lượt. Một lần cả trận. Không nhân sao và không thay đổi bộ đếm tuyệt kỹ3lượt. Nếu kích hoạt sau quái phản công, têliệt chặn lượt phản công kế tiếp; không cộng dồn lượt.
- Máy chủ quản lý điều kiện/phẩm/once-per-battle, trả sự kiện riêng. Client không tự suy diễn. Giữ lượt thần thông trong timeline lấy mẫu để hồi sinh không bị biến thành hiệu ứng chết giả.
- UI hiển thị mục **Thần thông** cho hai thú đủ phẩm, mô tả trong thẻ/Thú điển; thú phẩm thấp không có mục này.
- Hiệu ứng: niết bàn cánh bướm/sắc cầu vồng và sinh khí quanh chủ nhân; Thiên Kiếp tia sét tím dày từ trời và chấn vòng tại mục tiêu. Kỹ năng thường giữ FX riêng từng loài, tăng nhận biết hồi phục/khiên/đòn điện. Giảm chuyển động dùng một khung dịu, không flash/rung.

Các bước: testđỏ eligibility/once/revive/gearpriority/threshold/damage/stun/cadence; server + DTO; clientmô tả + FX; kiểmtra timeline/JS/reducedmotion; UI thật/build riêng. Không commit/phát hành, không sửa save thật.


## Kết quả triển khai

- Đã triển khai2thầnthông trong server/DTO/thẻchi tiết/Thúđiển, giới hạn theo phẩm thực tếancient. Thầnthông độc lập tuyệt kỹ thường, không nhân sao, không reset quađợt.
-112kiểmtra riêngthầnthông qua; tổng399kiểmtra pet/miracle/cadence qua;2.799kiểmtra hồi quy vậtphẩm/lưutrữ/rương qua.
- Đã thử sựkiện tửvong trong trận thật: hồi sinh25%máu và giữ snapshot trong timeline. Trận LôiLong thật kiểmtra≤30%máu, kíchhoạtduynhất và quái bỏ lượtphảncông kếtiếp.
- Nodekiểmtra FX14kỹnăng+2thầnthông qua cả bìnhthường/giảmchuyểnđộng; khôngphát nhãntrước phảncông, khôngsố/nhãnlặp, khôngtọađộNaN.
- Giao diện/API thật:2thầnthông hiểnthị, khôngcó mục ở thúThiên hoặc thúThượngCổ bị hạ phẩm thực tế; khônglỗiJS khi xuấtchiến.
- Reviewer phát hiện timing pha linhthú trước phảncông; đãsửa bằng `petSceneForPhase` và kiểmtra hồi quy pre/post. Review cuối không còn phát hiện.
- Buildriêng `bin/miracle-check/NguoiLuoiTuTien.exe` thànhcông, cảnhbáoWindowsBase sẵncó. Không phát hành/commit, không sửa save thật.
