# Điều chỉnh luyện khí

Giữ công thức 9 trang bị cùng phẩm → 1 trang bị; thành công lên một phẩm, thất bại trả một món cùng phẩm. Không thêm phí linh thạch. Món đầu ra lấy loại và cảnh giới từ một trong chín đầu vào, nên số bản sao một loại vẫn ảnh hưởng xác suất ra loại đó.

| Chuyển phẩm | Cũ | Mới | Số nguyên liệu tiêu hao trung bình cho một lần lên phẩm |
|---|---:|---:|---:|
| Phàm → Linh | 90% | 100% | 9,00 |
| Linh → Huyền | 75% | 95% | 9,42 |
| Huyền → Địa | 60% | 85% | 10,41 |
| Địa → Thiên | 45% | 75% | 11,67 |
| Thiên → Thánh | 30% | 65% | 13,31 |
| Thánh → Thần | 20% | 55% | 15,55 |
| Thần → Tiên | 15% | 45% | 18,78 |
| Tiên → Đạo | 10% | 35% | 23,86 |
| Đạo → Huyền Tiên | 5% | 25% | 33,00 |

Với xác suất p, số nguyên liệu tiêu hao trung bình là 9 + 8 × (1-p)/p = 1 + 8/p, giả định tái dùng món trả lại và tiếp tục bổ sung nguyên liệu tới khi thành công. Mốc cuối giảm từ trung bình 161 xuống 33 món; mỗi phẩm chỉ tăng khoảng 23–27% chỉ số gốc nên xác suất cũ quá nặng. Đây là chi phí kỳ vọng của một bước, không phải cam kết số lần sẽ thành công hoặc chi phí từ phẩm đầu lên phẩm cuối.

Phẩm cao vẫn có chi phí cao hơn rõ rệt. Giá bán đầu ra kỳ vọng thấp hơn tổng giá bán chín món đầu vào ở mọi bước, tránh luyện khí để tạo linh thạch vô hạn.

UI dùng tỉ lệ server cung cấp; xóa bảng dự phòng cũ chỉ có năm mốc. Tự điền duyệt toàn bộ chín phẩm ghép được, thay cho chỉ năm phẩm đầu. Mô tả thất bại nêu rõ mất tám món và nêu nguồn loại/cảnh giới của đầu ra.

Kiểm tra máy chủ: `dotnet run --project tools/fuse_checks -p:NuGetAudit=false -- D:/VanDao/obj/fuse-ui.json`.
Kiểm tra UI: `node tools/fuse_checks/ui.cjs <đường dẫn playwright>`.
