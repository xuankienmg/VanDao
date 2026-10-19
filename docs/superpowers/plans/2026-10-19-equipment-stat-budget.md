# Kế hoạch phân bố điểm thuộc tính trang bị: Phàm → Huyền Tiên

Trạng thái: đã triển khai công thức và bảng thuộc tính ngày 19/10/2026. Các bảng bên dưới là cấu hình đã áp dụng; cân bằng giao chiến còn cần kiểm tra mở rộng.

## Kết quả triển khai

- `Game/EquipmentBudget.cs` chứa ngân sách, hồ sơ và hệ số của sáu cảnh giới; `Variant()` áp dụng cho toàn bộ món sinh theo phẩm.
- Đồng bộ tên phẩm Huyền Tiên, giữ khóa `supreme`, ID món và cấp món. Inventory/Equipment lưu ID nên đồ cũ nhận chỉ số mới khi tải định nghĩa; không cần migration database.
- Tooltip chi tiết chia thuộc tính cơ bản và hỗ trợ; giữ hiệu ứng phẩm/thưởng bộ hiện có.
- Kiểm tra `dotnet run --project tools/sim_balance -- budget-check` (chạy từ thư mục công cụ hoặc truyền đường dẫn CSV): khớp 720 dòng CSV và 670 biến thể thực tế, chỉ số không giảm khi tăng phẩm, ID/cấp món đúng.
- Build Release vào `bin/budget-check` thành công; còn cảnh báo WindowsBase/WebView2. Build Debug vào thư mục mặc định bị khóa EXE do game đang chạy.
- Mô phỏng có sẵn: 144 tình huống phẩm Huyền, 63 tình huống đổi số lượt hoặc kết quả, 2 tình huống đổi thắng/thua. Trong 100 tình huống cả hai bản cùng thắng, trung vị số lượt đều là 12. Kết quả này chưa chứng minh ngưỡng ±10% ở từng chương; chưa hoàn tất kiểm tra mọi độ khó, kỹ năng và phối đồ. Không tự thay chỉ số quái để che tác động tái phân bố.
- Kết quả mô phỏng trước/sau được lưu tại `tools/sim_balance/before-budget.txt` và `after-budget.txt`.

## 1. Phạm vi và nguyên tắc

- 10 phẩm: Phàm, Linh, Huyền, Địa, Thiên, Thánh, Thần, Tiên, Đạo, Huyền Tiên.
- Code hiện gọi phẩm cuối là Huyền Thiên (`supreme`). Bản này dùng tên Huyền Tiên theo yêu cầu; nếu triển khai đổi tên thì giữ khóa `supreme` và chỉ số phẩm 9 để tương thích đồ đã lưu.
- Cấp món/cảnh giới quyết định nền chỉ số; phẩm quyết định ngân sách trên nền đó. So sánh phẩm phải dùng cùng cấp và cùng loại món.
- Điểm là đơn vị thiết kế nội bộ, không phải điểm người chơi tự cộng.
- Giai đoạn đầu dùng thuộc tính cố định theo loại món, chưa thêm dòng ngẫu nhiên hay cơ chế tẩy luyện.

## 2. Ngân sách theo phẩm

Giữ đường tăng trưởng công–thủ–máu hiện có làm mốc thử nghiệm, tránh thay đổi đồng thời quá nhiều hệ thống. Tách ngân sách tiện ích để kiểm soát chỉ số phần trăm.

| Phẩm | Điểm cơ bản B | Hệ số cơ bản | Điểm tiện ích U | Hệ số tiện ích |
|---|---:|---:|---:|---:|
| Phàm | 100 | 1,00 | 100 | 1,00 |
| Linh | 125 | 1,25 | 110 | 1,10 |
| Huyền | 155 | 1,55 | 120 | 1,20 |
| Địa | 190 | 1,90 | 130 | 1,30 |
| Thiên | 240 | 2,40 | 145 | 1,45 |
| Thánh | 300 | 3,00 | 160 | 1,60 |
| Thần | 380 | 3,80 | 175 | 1,75 |
| Tiên | 480 | 4,80 | 190 | 1,90 |
| Đạo | 600 | 6,00 | 210 | 2,10 |
| Huyền Tiên | 750 | 7,50 | 230 | 2,30 |

B và U là hai ngân sách độc lập, không cộng hoặc chuyển đổi cho nhau. Hiệu ứng riêng và thưởng bộ hiện có nằm ngoài hai ngân sách và phải được đánh giá cùng kết quả chiến đấu; chưa thêm hiệu ứng mới trong đợt này.

## 3. Phân bổ điểm cơ bản theo ô

Mỗi món nhận B điểm và chia theo bảng. Ô có nhiều loại món dùng hồ sơ tương ứng; tổng mỗi hàng là 100%.

| Ô / hồ sơ | Công | Thủ | Máu |
|---|---:|---:|---:|
| Vũ khí | 100% | 0% | 0% |
| Pháp bảo công kích | 80% | 0% | 20% |
| Pháp bảo hộ thân | 0% | 55% | 45% |
| Nón | 0% | 35% | 65% |
| Áo | 0% | 45% | 55% |
| Găng tay | 60% | 40% | 0% |
| Quần | 0% | 40% | 60% |
| Giày | 0% | 25% | 75% |
| Dây chuyền | 20% | 0% | 80% |
| Hộ phù | 0% | 60% | 40% |
| Nhẫn | 70% | 0% | 30% |

Đây là đề xuất vai trò mới: nhẫn và dây chuyền hiện thiên về tu luyện sẽ có thêm chỉ số chiến đấu. Cần xem riêng tác động này khi so với dữ liệu cũ.

### Quy đổi điểm thành chỉ số

Với mỗi cấp trang bị L, có ba hệ số `aL`, `dL`, `hL`:

```text
Công = round(B × tỷ trọng công × aL)
Thủ  = round(B × tỷ trọng thủ  × dL)
Máu  = round(B × tỷ trọng máu  × hL)
```

Không dùng cùng một hệ số cho công, thủ và máu. Xác định bộ hệ số cho các cấp 1, 10, 15, 20, 25, 30 bằng tổng chỉ số bộ đồ Huyền hiện tại, sau khi áp dụng các hệ số giảm trang bị hiện có. Mục tiêu ban đầu: tổng công, thủ, máu của một bộ chuẩn Huyền mới lệch không quá ±5% so với bộ cũ, trước thưởng bộ. Cố định danh sách món của bộ chuẩn, kể cả loại vũ khí/pháp bảo, để phép so sánh có thể lặp lại.

Không nhân lại `GearAtkHpMult` hoặc `GearDefMult` nếu chúng đã được hấp thụ vào hệ số quy đổi. Phẩm gốc ghi trên món mẫu không được trở thành một lần nhân phẩm thứ hai.

### Ví dụ số để đọc bảng

Giả sử riêng cho minh họa `aL = 0,2`, `dL = 0,1`, `hL = 2`; đây chưa phải hệ số cân bằng đã chốt:

| Món | Phàm | Thiên | Huyền Tiên |
|---|---|---|---|
| Vũ khí | 20 công | 48 công | 150 công |
| Áo | 5 thủ, 110 máu | 11 thủ, 264 máu | 34 thủ, 825 máu |
| Găng | 12 công, 4 thủ | 29 công, 10 thủ | 90 công, 30 thủ |

Quy tắc làm tròn đề xuất: nửa lên, thuộc tính có tỷ trọng dương nhận tối thiểu 1; tỷ trọng 0 giữ 0. Triển khai phải chọn rõ quy tắc vì `Math.Round` mặc định của C# làm tròn về số chẵn ở điểm giữa.

## 4. Phân bổ tiện ích

Mỗi hồ sơ món có giá trị tiện ích Phàm làm nền. Với chỉ số phần trăm thông thường: `giá trị = nền Phàm × U / 100`, làm tròn một chữ số thập phân. Giá trị 1% trong bảng là một điểm phần trăm cộng vào chỉ số.

| Ô / hồ sơ | Tiện ích nền Phàm đề xuất |
|---|---|
| Kiếm | +2% bạo kích |
| Thương | +3% bạo kích; giảm 10% ngân sách B so với vũ khí chuẩn |
| Vũ khí khác | Định nghĩa riêng trước khi đưa vào, không mặc định nhận tiện ích miễn phí |
| Pháp bảo công kích | +2% tốc độ tu luyện |
| Pháp bảo hộ thân | Không có tiện ích trực tiếp; giữ vai trò phòng thủ,    |
| Nón | +1% tốc độ tu luyện |
| Áo, găng, quần, hộ phù | Không có tiện ích trực tiếp; sử dụng hiệu ứng phẩm hiện có |
| Giày | +2% thân pháp |
| Dây chuyền | +2% tốc độ tu luyện |
| Nhẫn | +3% tốc độ tu luyện |

Không bắt buộc mọi ô tiêu hết U. U là hệ số tăng của tiện ích đã gán cho hồ sơ, không phải tiền dư để tự sinh thêm dòng.

- Công–thủ–máu tăng theo cấp món và B; bạo kích, thân pháp, tốc độ tu luyện không tự tăng thêm theo cấp/cảnh giới trong đợt này.
- Giữ điều kiện mở thân pháp theo cảnh giới hiện tại.
- Mốc Huyền Tiên: kiếm +4,6% bạo kích; thương +6,9%; giày +4,6% thân pháp; nhẫn +6,9% tốc độ tu luyện.
- Bộ dùng kiếm và pháp bảo công kích ở Huyền Tiên có +4,6% bạo kích, +4,6% thân pháp và +18,4% tốc độ tu luyện trực tiếp từ các dòng trên. Chưa tính nền nhân vật, công pháp, Hấp linh và thưởng hiệu ứng hiện có.
- Chưa áp trần mới. Kiểm tra tổng cuối cùng với trần chiến đấu đang có: bạo kích hiệu lực 100%, thân pháp hiệu lực 60% ở các phép tính hiện tại. Kiểm tra riêng tốc độ tu luyện vì Hấp linh cộng từ nhiều món có thể lớn hơn các dòng tiện ích này.

## 5. Cách triển khai dự kiến

1. Chụp bảng chỉ số và kết quả giao chiến hiện tại của các bộ chuẩn ở sáu cấp, đủ 10 phẩm; lưu làm baseline.
2. Tách cấu hình ngân sách cơ bản, tiện ích, hồ sơ từng món và hệ số theo cấp trong `Game/GameData.cs`.
3. Chuyển `Variant()` sang công thức ngân sách; giữ ID món, `Base`, `ItemLevel`, khóa rarity và điều kiện mặc. Giữ hiệu ứng phẩm hiện có trong lượt cân bằng đầu.
4. Xác định hệ số cấp theo tổng bộ Huyền baseline; xuất bảng từng món để kiểm tra trước khi điều chỉnh quái.
5. Cập nhật tooltip: chỉ số cơ bản, tiện ích và hiệu ứng phẩm rõ ràng; đồng bộ tên Huyền Tiên ở nguồn dữ liệu và các chỗ giao diện đang ghi cứng nếu triển khai đổi tên.
6. Kiểm tra đường tải dữ liệu cũ: món chỉ lưu ID sẽ nhận chỉ số mới khi tra định nghĩa; món có lưu bản sao chỉ số cần migration riêng sau khi xác minh schema. Ghi rõ tác động tái cân bằng đồ đang sở hữu.
7. Chạy mô phỏng bằng công cụ cân bằng sẵn có trong `tools`, so sánh baseline, rồi mới quyết định cần chỉnh hệ số, hồ sơ hay hiệu ứng phẩm.

## 6. Điều kiện chấp nhận

- Cùng cấp/cùng hồ sơ: phẩm cao không giảm bất kỳ thuộc tính trực tiếp nào; không bắt buộc mọi dòng tăng ở mọi bước vì làm tròn.
- Tổng chỉ số cơ bản bộ chuẩn Huyền lệch tối đa ±5% baseline; nếu không đạt phải giải thích và điều chỉnh trước khi áp dụng.
- Với cùng nhân vật, kỹ năng và quái: bộ chuẩn Huyền không làm thay đổi quá ±10% số lượt diệt quái trung vị; báo thêm tỷ lệ thắng và khoảng dao động khi có hiệu ứng ngẫu nhiên.
- Đo riêng quái thường và boss, các mốc mở ô/mở thân pháp và cả ba độ khó đang có.
- Kiểm tra đầy đủ 10 ô, các hồ sơ pháp bảo, vũ khí và cấp phẩm tối đa; không lỗi chỉ số 0, âm hoặc tràn số.
- Đồ Phàm cảnh giới mới và đồ phẩm cao cảnh giới cũ được đối chiếu riêng. Không ép cảnh giới mới luôn thắng: cần giữ giá trị đồ hiếm và vẫn có đường nâng cấp dễ tiếp cận.
- Chưa chốt cân bằng toàn bộ 10 phẩm chỉ từ tỷ lệ ngân sách: hiệu ứng cộng dồn và thưởng bộ có thể làm sức mạnh thực tế phi tuyến.

## 7. Thứ tự ưu tiên

P1: baseline và bảng ngân sách → P2: quy đổi chỉ số cơ bản → P3: tiện ích và tooltip → P4: mô phỏng và chỉnh hệ số → P5: đồng bộ tên, kiểm tra save cũ và phát hành.

## 8. Chỉ số trang bị từ Luyện Khí đến Luyện Hư

Bảng dưới dùng phẩm **Huyền** làm chuẩn. Tệp CSV cùng thư mục chứa đủ 6 cảnh giới × 10 phẩm × 12 hồ sơ = 720 dòng, bao gồm ba chỉ số cơ bản và ba tiện ích. Đây là đề xuất, chưa áp dụng vào game.

Lấy tổng bộ Huyền hiện tại làm baseline, gồm vũ khí đầu tiên, pháp bảo công kích và tám ô còn lại. Không cộng Thương và pháp bảo hộ thân vào bộ chuẩn: đây là hai hồ sơ thay thế. Chúng là mẫu thiết kế, không khẳng định mọi bộ đã có món tương ứng. Pháp bảo Luyện Khí chỉ được mặc khi điều kiện mở ô trong game cho phép.

Hệ số cấp được xác định riêng: aL = tổng công cũ / (155 × 3,30); dL = tổng thủ cũ / (155 × 2,45); hL = tổng máu cũ / (155 × 4,25). Baseline đã tính hệ số giảm trang bị và cách làm tròn của code hiện tại. Chỉ số mới làm tròn nửa lên, thuộc tính có tỷ trọng dương tối thiểu 1.

Giữ tiện ích không tăng theo cảnh giới. Hồ sơ vũ khí chuẩn dùng nền bạo kích của kiếm; trượng Luyện Hư tạm dùng hồ sơ này trong đề xuất. Thương dùng 90% ngân sách cơ bản để đổi lấy bạo kích cao hơn. Hiệu ứng phẩm, thưởng bộ và công pháp chưa nằm trong bảng.

### Luyện Khí — cấp 1, bộ Thanh Vân

Baseline Huyền hiện tại: 13 công · 12 thủ · 115 máu.

| Hồ sơ | Công | Thủ | Máu | Bạo kích % | Thân pháp % | Tu luyện % |
|---|---:|---:|---:|---:|---:|---:|
| Vũ khí chuẩn | 4 | 0 | 0 | 2.4 | 0 | 0 |
| Thương | 4 | 0 | 0 | 3.6 | 0 | 0 |
| Pháp bảo công kích | 3 | 0 | 5 | 0 | 0 | 2.4 |
| Pháp bảo hộ thân | 0 | 3 | 12 | 0 | 0 | 0 |
| Nón | 0 | 2 | 18 | 0 | 0 | 1.2 |
| Áo | 0 | 2 | 15 | 0 | 0 | 0 |
| Găng tay | 2 | 2 | 0 | 0 | 0 | 0 |
| Quần | 0 | 2 | 16 | 0 | 0 | 0 |
| Giày | 0 | 1 | 20 | 0 | 2.4 | 0 |
| Dây chuyền | 1 | 0 | 22 | 0 | 0 | 2.4 |
| Hộ phù | 0 | 3 | 11 | 0 | 0 | 0 |
| Nhẫn | 3 | 0 | 8 | 0 | 0 | 3.6 |

Tổng bộ chuẩn mới: 13 công · 12 thủ · 115 máu; đạt sai lệch tối đa ±5% so với baseline.


### Trúc Cơ — cấp 10, bộ Huyền Băng

Baseline Huyền hiện tại: 53 công · 54 thủ · 499 máu.

| Hồ sơ | Công | Thủ | Máu | Bạo kích % | Thân pháp % | Tu luyện % |
|---|---:|---:|---:|---:|---:|---:|
| Vũ khí chuẩn | 16 | 0 | 0 | 2.4 | 0 | 0 |
| Thương | 14 | 0 | 0 | 3.6 | 0 | 0 |
| Pháp bảo công kích | 13 | 0 | 23 | 0 | 0 | 2.4 |
| Pháp bảo hộ thân | 0 | 12 | 53 | 0 | 0 | 0 |
| Nón | 0 | 8 | 76 | 0 | 0 | 1.2 |
| Áo | 0 | 10 | 65 | 0 | 0 | 0 |
| Găng tay | 10 | 9 | 0 | 0 | 0 | 0 |
| Quần | 0 | 9 | 70 | 0 | 0 | 0 |
| Giày | 0 | 6 | 88 | 0 | 2.4 | 0 |
| Dây chuyền | 3 | 0 | 94 | 0 | 0 | 2.4 |
| Hộ phù | 0 | 13 | 47 | 0 | 0 | 0 |
| Nhẫn | 11 | 0 | 35 | 0 | 0 | 3.6 |

Tổng bộ chuẩn mới: 53 công · 55 thủ · 498 máu; đạt sai lệch tối đa ±5% so với baseline.


### Kim Đan — cấp 15, bộ Xích Kim

Baseline Huyền hiện tại: 267 công · 266 thủ · 2499 máu.

| Hồ sơ | Công | Thủ | Máu | Bạo kích % | Thân pháp % | Tu luyện % |
|---|---:|---:|---:|---:|---:|---:|
| Vũ khí chuẩn | 81 | 0 | 0 | 2.4 | 0 | 0 |
| Thương | 73 | 0 | 0 | 3.6 | 0 | 0 |
| Pháp bảo công kích | 65 | 0 | 118 | 0 | 0 | 2.4 |
| Pháp bảo hộ thân | 0 | 60 | 265 | 0 | 0 | 0 |
| Nón | 0 | 38 | 382 | 0 | 0 | 1.2 |
| Áo | 0 | 49 | 323 | 0 | 0 | 0 |
| Găng tay | 49 | 43 | 0 | 0 | 0 | 0 |
| Quần | 0 | 43 | 353 | 0 | 0 | 0 |
| Giày | 0 | 27 | 441 | 0 | 2.4 | 0 |
| Dây chuyền | 16 | 0 | 470 | 0 | 0 | 2.4 |
| Hộ phù | 0 | 65 | 235 | 0 | 0 | 0 |
| Nhẫn | 57 | 0 | 176 | 0 | 0 | 3.6 |

Tổng bộ chuẩn mới: 268 công · 265 thủ · 2498 máu; đạt sai lệch tối đa ±5% so với baseline.


### Nguyên Anh — cấp 20, bộ Tử Vân

Baseline Huyền hiện tại: 1334 công · 1334 thủ · 12495 máu.

| Hồ sơ | Công | Thủ | Máu | Bạo kích % | Thân pháp % | Tu luyện % |
|---|---:|---:|---:|---:|---:|---:|
| Vũ khí chuẩn | 404 | 0 | 0 | 2.4 | 0 | 0 |
| Thương | 364 | 0 | 0 | 3.6 | 0 | 0 |
| Pháp bảo công kích | 323 | 0 | 588 | 0 | 0 | 2.4 |
| Pháp bảo hộ thân | 0 | 299 | 1323 | 0 | 0 | 0 |
| Nón | 0 | 191 | 1911 | 0 | 0 | 1.2 |
| Áo | 0 | 245 | 1617 | 0 | 0 | 0 |
| Găng tay | 243 | 218 | 0 | 0 | 0 | 0 |
| Quần | 0 | 218 | 1764 | 0 | 0 | 0 |
| Giày | 0 | 136 | 2205 | 0 | 2.4 | 0 |
| Dây chuyền | 81 | 0 | 2352 | 0 | 0 | 2.4 |
| Hộ phù | 0 | 327 | 1176 | 0 | 0 | 0 |
| Nhẫn | 283 | 0 | 882 | 0 | 0 | 3.6 |

Tổng bộ chuẩn mới: 1334 công · 1335 thủ · 12495 máu; đạt sai lệch tối đa ±5% so với baseline.


### Hóa Thần — cấp 25, bộ Thái Hư

Baseline Huyền hiện tại: 6668 công · 6669 thủ · 62475 máu.

| Hồ sơ | Công | Thủ | Máu | Bạo kích % | Thân pháp % | Tu luyện % |
|---|---:|---:|---:|---:|---:|---:|
| Vũ khí chuẩn | 2021 | 0 | 0 | 2.4 | 0 | 0 |
| Thương | 1819 | 0 | 0 | 3.6 | 0 | 0 |
| Pháp bảo công kích | 1616 | 0 | 2940 | 0 | 0 | 2.4 |
| Pháp bảo hộ thân | 0 | 1497 | 6615 | 0 | 0 | 0 |
| Nón | 0 | 953 | 9555 | 0 | 0 | 1.2 |
| Áo | 0 | 1225 | 8085 | 0 | 0 | 0 |
| Găng tay | 1212 | 1089 | 0 | 0 | 0 | 0 |
| Quần | 0 | 1089 | 8820 | 0 | 0 | 0 |
| Giày | 0 | 681 | 11025 | 0 | 2.4 | 0 |
| Dây chuyền | 404 | 0 | 11760 | 0 | 0 | 2.4 |
| Hộ phù | 0 | 1633 | 5880 | 0 | 0 | 0 |
| Nhẫn | 1414 | 0 | 4410 | 0 | 0 | 3.6 |

Tổng bộ chuẩn mới: 6667 công · 6670 thủ · 62475 máu; đạt sai lệch tối đa ±5% so với baseline.


### Luyện Hư — cấp 30, bộ Huyền Long

Baseline Huyền hiện tại: 33338 công · 33343 thủ · 312375 máu.

| Hồ sơ | Công | Thủ | Máu | Bạo kích % | Thân pháp % | Tu luyện % |
|---|---:|---:|---:|---:|---:|---:|
| Vũ khí chuẩn | 10102 | 0 | 0 | 2.4 | 0 | 0 |
| Thương | 9092 | 0 | 0 | 3.6 | 0 | 0 |
| Pháp bảo công kích | 8082 | 0 | 14700 | 0 | 0 | 2.4 |
| Pháp bảo hộ thân | 0 | 7485 | 33075 | 0 | 0 | 0 |
| Nón | 0 | 4763 | 47775 | 0 | 0 | 1.2 |
| Áo | 0 | 6124 | 40425 | 0 | 0 | 0 |
| Găng tay | 6061 | 5444 | 0 | 0 | 0 | 0 |
| Quần | 0 | 5444 | 44100 | 0 | 0 | 0 |
| Giày | 0 | 3402 | 55125 | 0 | 2.4 | 0 |
| Dây chuyền | 2020 | 0 | 58800 | 0 | 0 | 2.4 |
| Hộ phù | 0 | 8166 | 29400 | 0 | 0 | 0 |
| Nhẫn | 7072 | 0 | 22050 | 0 | 0 | 3.6 |

Tổng bộ chuẩn mới: 33337 công · 33343 thủ · 312375 máu; đạt sai lệch tối đa ±5% so với baseline.

Dữ liệu đầy đủ: 2026-10-19-equipment-stat-budget.csv. Phần trăm trong bảng là phần cộng trực tiếp. Đã kiểm tra số dòng và tổng bộ sáu cảnh giới; chưa mô phỏng giao chiến. Phân bổ lại vẫn có thể đổi sức mạnh từng món và phối đồ dù tổng bộ gần baseline.
