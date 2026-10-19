# Kiểm chứng hiệu ứng và thưởng bộ — 19/10/2026

So sánh 2.160 tình huống trước/sau với cùng ID nhân vật, thời điểm, bộ đồ và kỹ năng: 6 cảnh giới × 2 tầng đầu/cuối × 10 phẩm × 3 độ khó × 3 cấu hình × 2 ải thường/boss. Cấu hình: đủ bộ đánh thường, đủ bộ kỹ năng Hỏa khởi đầu, và phối 5+5 với bộ cảnh giới trước (Luyện Khí giữ nguyên bộ). Không tự đổi chỉ số quái.

Baseline được tái dựng trong project cách ly obj/effects-legacy từ code trước cập nhật: hiệu ứng Huyền/Địa/Thiên cố định, thưởng Thiên 3/5/đủ ô và khuếch đại Thánh 3/6/9. Giữ công thức EquipmentBudget đã triển khai. Ma trận này không bao phủ mọi cách phối kỹ năng hoặc mọi chương phụ.

| Cảnh giới | Độ khó | Thắng trước/sau (Huyền, đánh thường) | Trung vị lượt trong các trận cùng thắng trước/sau | Độ lệch |
|---|---|---|---|---|
| Luyện Khí | Thường | 4/4 → 4/4 | 7 → 6.5 | -7.1% |
| Luyện Khí | Khó | 0/4 → 0/4 | Không có trận cùng thắng | — |
| Luyện Khí | Ác mộng | 0/4 → 0/4 | Không có trận cùng thắng | — |
| Trúc Cơ | Thường | 2/4 → 2/4 | 3 → 3 | 0% |
| Trúc Cơ | Khó | 0/4 → 0/4 | Không có trận cùng thắng | — |
| Trúc Cơ | Ác mộng | 0/4 → 0/4 | Không có trận cùng thắng | — |
| Kim Đan | Thường | 2/4 → 2/4 | 3 → 3 | 0% |
| Kim Đan | Khó | 0/4 → 0/4 | Không có trận cùng thắng | — |
| Kim Đan | Ác mộng | 0/4 → 0/4 | Không có trận cùng thắng | — |
| Nguyên Anh | Thường | 2/4 → 2/4 | 6 → 4.5 | -25% |
| Nguyên Anh | Khó | 0/4 → 0/4 | Không có trận cùng thắng | — |
| Nguyên Anh | Ác mộng | 0/4 → 0/4 | Không có trận cùng thắng | — |
| Hóa Thần | Thường | 2/4 → 2/4 | 4.5 → 4.5 | 0% |
| Hóa Thần | Khó | 0/4 → 0/4 | Không có trận cùng thắng | — |
| Hóa Thần | Ác mộng | 0/4 → 0/4 | Không có trận cùng thắng | — |
| Luyện Hư | Thường | 2/4 → 2/4 | 6 → 4.5 | -25% |
| Luyện Hư | Khó | 0/4 → 0/4 | Không có trận cùng thắng | — |
| Luyện Hư | Ác mộng | 0/4 → 0/4 | Không có trận cùng thắng | — |

Toàn ma trận: 20 tình huống đổi thắng/thua (2 chuyển thành thắng, 18 chuyển thành thua). Có 2 nhóm Huyền đánh thường có trung vị lượt lệch quá ±10%. Đây là kết quả kiểm tra, không phải xác nhận đã đạt ngưỡng cân bằng của plan.

Dữ liệu gốc: effects-before-matrix.csv và effects-matrix.csv trong tools/sim_balance. QiPerSecond là tốc độ tu luyện chưa xét trần tích lũy tu vi; ExploreQi là phần thưởng cơ sở/chuyến trước hệ số độ khó, thưởng boss/thua trận. Không diễn giải hai cột này thành tu vi/giờ thực nhận.

Đã giữ đúng bảng bonus được duyệt dù một số mục tiêu độ lệch không đạt. Trước phát hành cân bằng chính thức cần xem lại trị số bonus hoặc chấp nhận chủ đích thay đổi sức mạnh; không thay chỉ số quái trong lần cập nhật này.


Tệp effects-summary.csv tổng hợp 540 nhóm, gồm tỷ lệ thắng, trung vị và p90 lượt của các trận thắng, máu cuối trung bình và thay đổi tốc độ tu luyện. Mỗi nhóm chỉ có 4 mẫu (tầng đầu/cuối × thường/boss); p90 dùng nearest rank, không phải ước lượng thống kê từ nhiều lượt ngẫu nhiên. Có 15 nhóm Thiên trở lên giảm tỷ lệ thắng quá 5 điểm phần trăm và 42 nhóm đổi tốc độ tu luyện quá 15%. Đây là các nhóm cần cân nhắc khi bỏ thưởng phẩm cũ.
