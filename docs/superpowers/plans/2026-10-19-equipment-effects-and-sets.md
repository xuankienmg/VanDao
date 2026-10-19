# Kế hoạch cập nhật thuộc tính cộng thêm và thưởng bộ

Trạng thái: đã triển khai ngày 19/10/2026; công thức và giao diện đã kiểm tra, các mục tiêu cân bằng còn có độ lệch được ghi trong `docs/equipment-effects-qa.md`. Tiếp nối bảng thuộc tính cơ bản đã triển khai trong `2026-10-19-equipment-stat-budget.md`.

## Kết quả triển khai

- `Game/EquipmentEffects.cs`: cấu hình sáu bộ, mốc tích lũy 3/5/8/10, hệ số hiệu ứng theo phẩm, tổng thô và trần nhóm trang bị/bộ. Không còn thưởng Thiên/Thánh toàn thân cũ.
- Server nối bonus vào chỉ số chiến đấu, tu luyện, tu vi thám hiểm và các hiệu ứng chiến đấu; trả SetId, metadata bộ, snapshot bonus, nguồn chỉ số và trần tổng với công pháp/kỹ năng cho client.
- Tooltip hiển thị hiệu ứng theo phẩm, số món và bốn mốc bộ; bảng nhân vật có phần mở rộng nguồn chỉ số và giới hạn. Client không tự tính thưởng bộ hoặc trần trang bị.
- Kiểm tra tự động: 540 ca mốc bộ và tháo đồ; 60 tổ hợp cảnh giới/phẩm; phối 5+5 và 3+3+4; loại đồ không hợp lệ; trần; SQLite save/load; response sau tháo đồ; 720 dòng thuộc tính và 670 biến thể thực tế.
- Kiểm tra template UI với fixture thật từ server cho trạng thái không bộ, một phần, đủ bộ, trộn bộ và chạm trần; chưa xác nhận bằng kiểm tra trực quan trong cửa sổ game.
- So sánh 2.160 tình huống giao chiến trước/sau và tổng hợp 540 nhóm. Có 20 tình huống đổi thắng/thua (2 tốt lên, 18 xấu đi); 2 nhóm Huyền đánh thường lệch trung vị lượt quá ±10%; 15 nhóm phẩm Thiên trở lên giảm tỷ lệ thắng quá 5 điểm %. Các bảng cấu hình giữ đúng giá trị được duyệt; không tự sửa quái. Chưa coi đây là bản cân bằng phát hành cuối.
- Build Release tại `bin/budget-check`; giữ ID/save, không migration database. Đồ đã có nhận hiệu ứng và thưởng bộ mới khi tải định nghĩa.

Các phần dưới ghi lại đặc tả được dùng để triển khai và các mục tiêu kiểm tra; mục tiêu chưa đạt được nêu ở báo cáo QA thay vì coi là đã hoàn tất nghiệm thu cân bằng.

## 1. Mục tiêu và phạm vi

Áp dụng cho sáu bộ: Thanh Vân (Luyện Khí, cấp 1), Huyền Băng (Trúc Cơ, cấp 10), Xích Kim (Kim Đan, cấp 15), Tử Vân (Nguyên Anh, cấp 20), Thái Hư (Hóa Thần, cấp 25), Huyền Long (Luyện Hư, cấp 30). Mỗi bộ có 10 ô đang mở trong dữ liệu hiện tại.

Tách rõ ba nguồn sức mạnh:

1. Thuộc tính trực tiếp: công, thủ, máu và bạo kích/thân pháp/tu luyện từ `EquipmentBudget`. Giữ nguyên bảng đã triển khai trong đợt này.
2. Hiệu ứng từng món: Hấp linh, Phá giáp, Hộ thể… Tăng theo phẩm, không tự tăng thêm theo cảnh giới.
3. Thưởng cùng bộ: chỉ kích hoạt khi mặc đúng số món thuộc cùng `SetId`. Mặc lẫn phẩm vẫn được tính.

Không bổ sung cơ chế cường hóa +1/+2, tẩy luyện hoặc dòng ngẫu nhiên trong phạm vi này. “Chỉ số + thêm” được hiểu là thuộc tính/hiệu ứng cộng thêm đang có trên món.

## 2. Vấn đề hiện tại cần xử lý

- `EffectsFor()` mở hiệu ứng ở Linh/Huyền/Địa/Thiên; phần lớn hiệu ứng giữ nguyên trị số khi lên Thánh → Huyền Tiên.
- `GearEffects()` tính thưởng bộ Thiên theo tổng số món phẩm Thiên trở lên, không kiểm tra cùng bộ. Đủ toàn bộ ô nhận +20% công, thủ, máu.
- Mốc Thánh trở lên 3/6/9 món tăng 10/20/30% mọi hiệu ứng, khiến nhiều hiệu ứng cùng phình lên và chạm trần.
- Giao diện `gearFx()` tự tính lại nhiều công thức và trần; khi server thay đổi có nguy cơ hiển thị lệch kết quả thật.

## 3. Hiệu ứng từng món theo phẩm

Giữ các mốc mở hiệu ứng, tăng nhẹ hiệu ứng cũ khi lên phẩm. Không thêm hiệu ứng mới ở từng phẩm cao trong đợt đầu.

| Phẩm | Hấp linh mỗi món | Hệ số hiệu ứng đã mở K | Hiệu ứng được mở |
|---|---:|---:|---|
| Phàm | 0% | — | Không có |
| Linh | 2% | 1,00 | Hấp linh |
| Huyền | 3% | 1,00 | Thêm hiệu ứng theo ô |
| Địa | 4% | 1,00 | Thêm hiệu ứng theo nhóm |
| Thiên | 6% | 1,00 | Thêm hiệu ứng theo nhóm |
| Thánh | 8% | 1,05 | Kế thừa |
| Thần | 10% | 1,10 | Kế thừa |
| Tiên | 12% | 1,15 | Kế thừa |
| Đạo | 15% | 1,20 | Kế thừa |
| Huyền Tiên | 20% | 1,25 | Kế thừa |

Hấp linh dùng giá trị riêng trong bảng, không nhân K. Với hiệu ứng còn lại: `giá trị món = nền hiệu ứng × K`. K chỉ nhân một lần, không nhân lại theo số phẩm đã vượt qua. Làm tròn phần trăm một chữ số thập phân, hệ số sát thương chí mạng hai chữ số, nộ về số nguyên, đều dùng nửa lên.

### Hiệu ứng mở từ Huyền

| Ô | Hiệu ứng | Nền Huyền | Ở Huyền Tiên |
|---|---|---:|---:|
| Vũ khí | Phá giáp | Bỏ qua 6% thủ | 7,5% |
| Pháp bảo | Linh áp | +6% sát thương kỹ năng | +7,5% |
| Nón | Tĩnh tâm | +25 nộ đầu trận | +31 nộ |
| Áo | Hộ thể | Giảm 4% sát thương nhận | 5% |
| Găng | Liên kích | 6% đánh thêm đòn thường | 7,5% |
| Quần | Vững thân | +4% máu tối đa | +5% |
| Giày | Thân pháp (né tránh) | 4% né đòn quái | 5% |
| Dây chuyền | Hồi khí | Hồi 1,5% máu/lượt | 1,9% |
| Nhẫn | Tụ linh | +8% tu vi thám hiểm | +10% |
| Hộ phù | Hộ thể | Giảm 4% sát thương nhận | 5% |

“Thân pháp” trên dòng tiện ích là tăng tốc độ thám hiểm; hiệu ứng giày ở bảng này là né tránh. Đổi nhãn hiệu ứng giày thành **Vân bộ** để tránh nhầm hai đại lượng.

### Hiệu ứng mở từ Địa và Thiên

| Mốc | Nhóm | Hiệu ứng | Nền | Ở Huyền Tiên |
|---|---|---|---|---|
| Địa | Vũ khí/pháp bảo | Địa chấn | Đòn đầu thêm 40% công, bỏ qua thủ | 50% công |
| Địa | Trang sức | Địa mạch | Phản 6% sát thương nhận | 7,5% |
| Địa | Phòng cụ | Thổ thuẫn | Khiên đầu trận = 4% máu | 5% máu |
| Thiên | Vũ khí/pháp bảo | Thiên lôi | 15% cơ hội, sát thương 60% công | 18,8% cơ hội, sát thương 60% công |
| Thiên | Trang sức | Thiên mệnh | Hệ số chí mạng +0,15 | +0,19 |
| Thiên | Phòng cụ | Thiên uy | Giảm 5% công quái | 6,3% |

Với Thiên lôi, K chỉ tăng xác suất; sát thương mỗi tia giữ 60% công. Khi cộng nhiều món, cộng xác suất và giữ một mức sát thương mỗi tia như logic hiện tại. Nhóm trang sức gồm dây chuyền, nhẫn, hộ phù; phòng cụ gồm nón, áo, găng, quần, giày.

## 4. Thưởng khi mặc cùng bộ

### Quy tắc đếm

- Dùng khóa ổn định `thanh_van`, `huyen_bang`, `xich_kim`, `tu_van`, `thai_hu`, `huyen_long`; tên hiển thị giữ riêng. Ánh xạ `Set` hiện có sang khóa khi sinh định nghĩa món, không cần sửa ID/save.
- Mỗi ô đang mặc hợp lệ tính tối đa một món. Vũ khí khác loại cùng bộ thay thế nhau; không thể tính kiếm và thương như hai ô.
- Chỉ tính trang bị thực sự đang mặc, đúng điều kiện cảnh giới và đúng ô hợp lệ. Đồ trong túi không tính. Kiểm tra đường trang bị hợp lệ của server, không tin dữ liệu đếm từ client.
- Không yêu cầu cùng phẩm. Một món Phàm vẫn đóng góp một món cho bộ; phẩm tăng sức mạnh từng món qua ngân sách và K.
- Bộ không có khóa hợp lệ không nhận thưởng bộ. Pháp bảo nhập môn `pb_*` không có Set không được tự gán sang Huyền Băng.
- Có thể kích hoạt hai bộ, ví dụ 5 Thanh Vân + 5 Huyền Băng hoặc 3+3+4. Mỗi bộ tính độc lập; phần thưởng cộng vào cùng nhóm chỉ số theo quy tắc ở mục 6.
- Mốc là 3/5/8/10 món cố định, không giảm theo số ô mở. Dữ liệu hiện tại mở 10 ô ở cả sáu cảnh giới; nếu tương lai khóa ô thì chưa đủ món sẽ chưa nhận mốc đó.

### Mốc chung cho cả sáu bộ

Các dòng là **phần cộng thêm ở từng mốc**, kích hoạt tích lũy khi đạt mốc.

| Số món | Phần thưởng mới ở mốc | Tổng thưởng chung đã nhận |
|---|---|---|
| 3 | +3% công, thủ, máu | +3% công/thủ/máu |
| 5 | Hiệu ứng bản sắc cấp I của bộ | +3% công/thủ/máu và cấp I |
| 8 | Hiệu ứng bản sắc cấp II của bộ | +3% công/thủ/máu và cấp I + II |
| 10 | Thêm +5% công, thủ, máu; +3% tốc độ tu luyện | +8% công/thủ/máu, +3% tu luyện và cấp I + II |

Các bonus công/thủ/máu này là phần trăm trên tổng nền nhân vật và chỉ số cộng thẳng trang bị, cùng nhóm cộng với công pháp; không phải phần trăm chỉ tính trên từng món.

### Bản sắc sáu bộ

| Bộ / cảnh giới | 5 món: cấp I | 8 món: cấp II, cộng thêm |
|---|---|---|
| Thanh Vân / Luyện Khí | +5% tốc độ tu luyện | +5% tu vi thám hiểm |
| Huyền Băng / Trúc Cơ | Giảm 3% sát thương nhận | Khiên đầu trận +5% máu tối đa |
| Xích Kim / Kim Đan | +5% sát thương kỹ năng | Bỏ qua thêm 5% phòng thủ quái |
| Tử Vân / Nguyên Anh | +2 điểm % bạo kích | Hệ số chí mạng +0,10 |
| Thái Hư / Hóa Thần | +3 điểm % né tránh | +4 điểm % tốc độ thám hiểm |
| Huyền Long / Luyện Hư | +5% sát thương kỹ năng | Giảm 5% công quái |

Không tăng số thưởng bộ thêm theo cảnh giới hoặc phẩm. Vai trò chiến đấu khác nhau tạo lựa chọn phối đồ; chỉ số cơ bản theo cảnh giới vẫn là nguồn tăng trưởng chính.

Ví dụ mặc 10 món Thanh Vân: +8% công/thủ/máu, +8% tốc độ tu luyện, +5% tu vi thám hiểm từ riêng bộ. Hấp linh và các dòng từng món được cộng riêng. Mặc 5 Thanh Vân + 5 Huyền Băng: +6% công/thủ/máu, +5% tu luyện, giảm 3% sát thương nhận; không nhận bonus 8 hoặc 10 món của bất kỳ bộ nào.

## 5. Thay thế thưởng theo phẩm cũ

- Bỏ bonus Thiên cũ 3/5/đủ ô = +5/+10/+20% công/thủ/máu; thay bằng thưởng cùng bộ ở mục 4. Không giữ hai hệ thống cộng chồng.
- Bỏ hệ số khuếch đại Thánh cũ 3/6/9 món = +10/+20/+30% mọi hiệu ứng. K theo phẩm của từng món ở mục 3 thay thế cơ chế này.
- Thánh → Huyền Tiên vẫn hơn phẩm dưới qua ngân sách cơ bản, tiện ích, Hấp linh và K; không có bonus phẩm toàn thân riêng trong phiên bản đầu.
- Đây là thay đổi cân bằng có chủ đích: bộ Thiên/Thánh cũ có thể yếu đi ở một số cấu hình dù phẩm không đổi. Không tuyên bố tương đương sức mạnh trước khi mô phỏng.
- Giữ tên, ID, khóa rarity, tỷ lệ rơi, hợp thành và giá bán hiện tại trong phạm vi này.

## 6. Quy tắc cộng dồn và trần

Thứ tự: thuộc tính trực tiếp → hiệu ứng từng món đã nhân K → thưởng bộ → cộng công pháp/nguồn khác → áp trần hiệu lực cuối. Không dùng thưởng bộ làm đầu vào để tính lại chính nó. Bonus bộ không nhân K.

```text
Công cuối = (công nền + công trang bị) × hệ số suy yếu × (1 + (công pháp% + bộ công%) / 100)
Máu cuối = (máu nền + máu trang bị + luyện thể) × (1 + (công pháp% + Vững thân% + bộ máu%) / 100)
Thủ cuối = (thủ nền + thủ trang bị + luyện thể) × hệ số suy yếu × (1 + (công pháp% + bộ thủ%) / 100)
Tốc độ tu luyện = nền cảnh giới × (1 + (tiện ích món% + Hấp linh% + công pháp% + bộ tu luyện%) / 100) × hệ số suy yếu
```

Giữ phép làm tròn chỉ số cuối của `Stats()` hiện tại trong đợt này; chỉ chuẩn hóa làm tròn dữ liệu hiệu ứng. Tu vi thám hiểm là nhóm thưởng riêng, không được cộng vào tốc độ tu luyện lần nữa.

| Đại lượng | Trần đề xuất | Ghi chú |
|---|---:|---|
| Bạo kích | 100% | Tổng nền, món, công pháp, bộ và bonus kỹ năng tại chỗ sử dụng |
| Tốc độ thám hiểm từ thân pháp | 60% | Giữ thời gian tối thiểu hiện tại |
| Bỏ qua thủ | 80% | Tổng kỹ năng + món + bộ |
| Giảm sát thương nhận | 60% | Tổng các nguồn trong cùng nhóm |
| Né tránh | 50% | Tổng các nguồn |
| Giảm công quái | 40% | Tổng các nguồn |
| Nộ đầu trận từ trang bị/bộ | 75 | Giữ phạm vi trần trang bị hiện có; không hạ trần nộ chiến đấu toàn cục |
| Liên kích từ trang bị/bộ | 30% | Giữ giới hạn nhóm hiện tại |
| Khiên đầu trận từ trang bị/bộ | 20% máu | Tính sau máu tối đa; không tự sửa cơ chế khiên kỹ năng |
| Phản đòn từ trang bị/bộ | 15% | Giữ giới hạn nhóm hiện tại |
| Xác suất Thiên lôi từ trang bị | 50% | Không nhân hai lần xác suất và sát thương |
| Hồi máu mỗi lượt từ trang bị/bộ | 5% máu | Trần mới; hồi không vượt máu tối đa và không hồi sinh |
| Hệ số chí mạng cộng từ trang bị/bộ | +0,60 | Trần mới; ví dụ nền ×1,5 thành tối đa ×2,1 từ hai nguồn này |

Các nhóm tu luyện, tu vi thám hiểm, sát thương kỹ năng, tăng máu không đặt trần mới trong vòng đầu; phải báo tổng lớn nhất trong kiểm tra. Giá trị hiển thị gồm tổng thô, trần và hiệu lực khi chạm trần.

## 7. Thiết kế dữ liệu và giao diện

- Bổ sung định nghĩa `EquipmentSetDef`: ID, tên, danh sách mốc, các bonus có mã chỉ số và đơn vị rõ ràng. Mỗi bonus là số tăng ở mốc, không lưu lẫn giá trị tích lũy.
- Bổ sung `SetId` tùy chọn cho `ItemDef`, gán khi sinh món từ tên bộ hiện có. Giữ `Set` làm nhãn để tương thích giao diện trong quá trình chuyển đổi.
- Tách hàm tính set thành hàm thuần dùng chung cho `Stats()`, `Rate()` và `GearEffects()`; một lần tính cho một snapshot, không tạo vòng gọi giữa ba hàm.
- DTO trạng thái trả kết quả authoritative: số món của từng bộ, mốc đang bật, mốc kế tiếp, bonus tổng và hiệu lực/trần từng hiệu ứng. Định nghĩa set được gửi qua data endpoint như metadata.
- Client chỉ trình bày snapshot đã tính; bỏ công thức bộ Vàng/Đỏ và danh sách trần hard-code ở `inventory.js`. Tooltip chi tiết từng món có thể đọc metadata; không tự quyết định bonus đang hoạt động.
- Thẻ món: thuộc tính cơ bản → thuộc tính hỗ trợ → hiệu ứng phẩm → tên bộ, số món, mốc 3/5/8/10. Mốc chưa đạt làm mờ; ghi rõ “cộng thêm” ở mỗi dòng.
- Bảng nhân vật hiển thị nguồn chỉ số: nền, trang bị trực tiếp, công pháp, hiệu ứng món, thưởng bộ. Nêu “Vân bộ: né tránh” và “Thân pháp: tốc độ thám hiểm” rõ ràng.
- Sau thay đồ, client cập nhật bonus bằng response server; không giữ snapshot bonus cũ. Save cũ tự nhận định nghĩa mới qua ID món, không thêm số bonus đã tính vào database.

## 8. Các bước triển khai

1. Lưu baseline trước thay đổi: chỉ số, tổng hiệu ứng, tu luyện, tu vi/chuyến, kết quả chiến đấu của sáu bộ, đủ 10 phẩm và các cấu hình trộn bộ.
2. Tạo cấu hình K, bảng nền hiệu ứng, định nghĩa sáu bộ và ánh xạ SetId; chưa nối vào chiến đấu.
3. Viết kiểm tra tính mốc, cộng dồn, trần và công thức không đếm hai lần, kể cả SetId trống và hai loại vũ khí.
4. Cập nhật `EffectsFor()`, thay bonus Thiên/Thánh cũ bằng hàm tính bộ; nối các bonus vào đúng chỗ trong `Stats()`, `Rate()` và chiến đấu. Bỏ các trường/hằng cũ khi không còn người dùng.
5. Cập nhật DTO, data endpoint và giao diện; kiểm tra tooltip và kết quả server sau mặc/tháo đồ, tải lại save.
6. Chạy mô phỏng, ghi kết quả theo chương/độ khó/phẩm/bộ; điều chỉnh bảng cấu hình trước khi cân nhắc đổi chỉ số quái. Nếu phải đổi quái thì lập phạm vi cân bằng riêng.
7. Cập nhật ghi chú phát hành: bonus bộ cũ được thay thế, đồ hiện có nhận bonus mới khi tải lại.

## 9. Điều kiện nghiệm thu

- Đếm đúng ở 0/2/3/4/5/7/8/9/10 món; tháo một món qua mốc phải gỡ đúng phần thưởng.
- 5+5 và 3+3+4 cộng đúng hai/ba bộ; 9 món cùng bộ + 1 không bộ không thành đủ bộ.
- Hai món cùng ô không được tính trùng; cùng tên hiển thị nhưng khác khóa không trộn; mọi phẩm trong cùng SetId được tính ngang nhau.
- Khớp bảng K ở cả 10 phẩm, đủ nhóm ô; Hấp linh không nhân K; Thiên lôi không tăng cả hai tham số.
- Không còn thưởng bộ Thiên/Thánh cũ chạy ngầm hoặc hiển thị trên client.
- Chỉ số hiệu lực không vượt trần; tooltip phân biệt tổng thô và hiệu lực. Mọi phép tính từ cùng snapshot cho cùng kết quả.
- Giữ nguyên bảng trực tiếp của 670 biến thể đã triển khai và ID/save tương thích; mở save mẫu không mất món hoặc tự tháo đồ hợp lệ.
- Mô phỏng sáu cảnh giới, tầng đầu/cuối, ba độ khó, quái thường/boss, 10 phẩm, đòn thường và build kỹ năng; báo tỷ lệ thắng, trung vị và p90 số lượt, máu còn lại, tu vi/giờ. Cơ chế đang tính theo kỳ vọng không cần giả lập ngẫu nhiên giả; nếu có RNG phải cố định seed hoặc chạy lặp đủ và nêu sai số.
- Ở phẩm Huyền, kiểm tra mục tiêu trung vị số lượt diệt quái thay đổi không quá ±10% theo từng nhóm cảnh giới/độ khó; báo riêng đổi thắng/thua, không dùng trung vị toàn game che các chương lệch.
- Ở Thiên trở lên, không ép giữ mức cũ vì chủ đích bỏ bonus 20%/30%. Mọi nhóm có tỷ lệ thắng giảm quá 5 điểm % hoặc tu vi/giờ đổi quá 15% phải được ghi nhận, cân lại cấu hình và giải thích trước phát hành.
- Build thành công, kiểm tra cú pháp JS và QA giao diện các trường hợp không bộ, bộ một phần, đủ bộ, trộn bộ và chạm trần.

## 10. Thứ tự ưu tiên

P1: quy tắc bộ và baseline → P2: hiệu ứng theo phẩm và hàm tính bộ → P3: nối công thức và DTO → P4: giao diện → P5: mô phỏng, cân bằng, kiểm tra save và phát hành.
