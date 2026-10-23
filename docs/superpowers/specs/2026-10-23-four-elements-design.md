# Hoàn thiện Thổ, Lôi, Băng, Phong

Trạng thái: người dùng đã duyệt thiết kế và yêu cầu triển khai ngày 2026-10-23. Tiến độ và bằng chứng nằm trong docs/four-elements-qa.md.

## Mục tiêu và hiện trạng

Hoàn thiện bốn công pháp còn khóa thành cây chơi được, cùng phạm vi Luyện Khí đến Hợp Thể như Hỏa, Thủy, Mộc. Hiện mỗi hệ mới có 5 nút khung không có chỉ số chiến đấu đầy đủ; chưa có đòn thường riêng và sprite sheet. Mở bí kíp, lĩnh ngộ, nâng cấp, kích hoạt hệ, trang bị và thi triển phải hoạt động trong game thật.

Mỗi hệ gồm 19 nút: 1 đòn thường, 7 chủ động, 11 căn cơ/bị động. Tổng 76 nút cuối cùng, thay 20 nút khung. Giữ ID của mọi nút khung; các nút Thủ/Thân pháp hiện có tiếp tục là chủ động, có sát thương thi triển. Không chuyển loại nút đã tồn tại để tránh thay đổi ý nghĩa kỹ năng được trang bị trong save cũ.

## Cách triển khai được đề xuất

Dùng SkillDef, cây UI hiện có và renderer fire-fx.js đang phục vụ cả ba hệ. Bổ sung manifest bốn hệ, mở rộng phép tra cứu và icon. Giữ API renderer để tránh đổi đồng loạt caller.

Tách renderer mới sẽ nhân đôi timeline, cache và fallback. Dùng lại ảnh cũ bằng đổi màu rẻ hơn nhưng không đạt yêu cầu nhận diện riêng. Do đó đề xuất mở rộng renderer chung, ảnh mới cho từng đòn thường/chủ động. Đây là lựa chọn của người triển khai, chưa phải quyết định người dùng đã duyệt.

## Cây chiêu chủ động và đòn thường

Thứ tự dưới đây là đòn thường; sau đó 7 chủ động ở cảnh giới 0, 1, 2, 3, 4, 5, 6.

- Thổ: Thổ Linh Đạn (tho_linh_dan); Thạch Đạn (thach_dan), Thổ Tường (tho_tuong), Thổ Giáp (tho_giap), Sơn Nhạc Quyết (son_nhac), Hậu Thổ Giáng Lâm (hau_tho), Hư Không Trấn Nhạc (hu_khong_tran_nhac), Hỗn Nguyên Hậu Thổ (hon_nguyen_tho).
- Lôi: Lôi Quang Cầu (loi_quang_cau); Lôi Kích (loi_kich), Lôi Bộ (loi_bo), Liên Hoàn Lôi (lien_loi), Thiên Lôi (thien_loi), Cửu Tiêu Lôi Phạt (cuu_tieu), Hư Không Kiếp Lôi (hu_khong_kiep_loi), Hỗn Nguyên Thiên Kiếp (hon_nguyen_loi).
- Băng: Băng Linh Châm (bang_linh_cham); Băng Tiễn (bang_tien), Băng Thuẫn (bang_thuan), Đông Địa (dong_dia), Tuyệt Đối Linh Độ (tuyet_doi), Huyền Băng Giáng Thế (huyen_bang), Hư Không Băng Ngục (hu_khong_bang_nguc), Hỗn Nguyên Vĩnh Đông (hon_nguyen_bang).
- Phong: Phong Linh Tiễn (phong_linh_tien); Phong Nhận (phong_nhan), Phong Bộ (phong_bo), Cuồng Phong (cuong_phong), Thiên Phong Quyết (thien_phong), Cửu Thiên Phong Bạo (cuu_thien_phong), Hư Không Phong Nhận (hu_khong_phong_nhan), Hỗn Nguyên Vạn Phong (hon_nguyen_phong).

Đòn thường 115% công, không tốn điểm. Chủ động tối đa cấp 5, tăng sát thương tuyến tính theo cấp. Mốc sát thương cấp 1 đề xuất: Thổ 140/180/225/320/390/485/600; Lôi 150/195/245/345/415/515/635; Băng 140/180/225/325/390/485/600; Phong 145/185/235/335/400/500/620. Mức tăng mỗi cấp lần lượt 14/18/24/36/28/33/38, riêng Lôi 15/20/25/38/30/35/40 và Phong 14/19/24/37/29/34/39.

Lĩnh ngộ chủ động tốn 1/5/10/3/5/6/8 điểm; điều kiện chuỗi như Mộc: tiền đề cấp 5 cho chiêu cảnh 1, 2; tiền đề cấp 3 cho cảnh 3 trở lên. Chiêu đầu không cần tiền đề. Giữ công thức nâng cấp hiện tại. Cây chủ động dùng hàng 0/1/2/4/5/6/7, cột 1 trong tab riêng.

## Bị động và bản sắc

Mỗi hệ dùng cấu trúc 11 nút như Mộc: linh căn; nội công đầu; hai nhánh cơ sở; tâm pháp; chân ý; hai nhánh Nguyên Anh; pháp thân; Thái Hư; hợp nhất. Vị trí, cảnh giới, điểm và điều kiện theo Mộc để vừa UI hiện tại. Tên cụ thể chọn theo tên công pháp, ID duy nhất; linh căn giữ ID cũ.

- Thổ ưu tiên DefPct, HpPct, rồi SkillPct và Pen ở cảnh giới cao.
- Lôi ưu tiên AtkPct, Crit, SkillPct, Pen; tăng phòng thủ/máu thấp hơn Thổ.
- Băng ưu tiên MonAtkDown, DefPct, HpPct và SkillPct. Dùng giới hạn giảm công quái hiện có 40%.
- Phong ưu tiên Speed, Crit, AtkPct và SkillPct. Dùng giới hạn tốc độ 60% và né tránh hiện có.

Mỗi nút có mô tả chính xác chỉ số mỗi cấp. Dùng các trường server hiện hỗ trợ; không hứa hẹn stun, đóng băng mất lượt, dịch chuyển hoặc đòn lan khi chưa có logic. Thổ Tường/Thổ Giáp/Băng Thuẫn có hình hộ thể và phản kích; phòng thủ đến từ cây bị động, không ghi hiệu ứng khiên tạm thời chưa được xử lý. Lôi Bộ/Phong Bộ có dư ảnh VFX; thân pháp đến từ bị động, không sửa hệ tích nộ hay thêm lượt đánh.

## Sprite sheet và animation

32 PNG RGBA, mỗi chiêu một sheet 768×768, 4×4 ô, mỗi ô 192×192, 16 frame đọc từ trái sang phải và trên xuống dưới. Tạo ảnh bằng image_gen, lưu prompt và nguồn trong workspace, rồi đóng gói đúng lưới. Không dùng ảnh giả, hình vẽ thủ tục hoặc đổi màu hệ cũ thay cho asset cuối.

Mỗi chiêu có 4 pha dùng timeline hiện có: tụ lực [0,.25), phóng/hiện hình [.25,.45), trúng [.45,.60), tan [.60,.75). Ngoài khoảng đó không vẽ. Neo hand/projectile/target/ground theo manifest; vị trí phản kích của chiêu hộ thể phải khớp mốc va chạm. Không chứa tu sĩ, quái, chữ, viền ô hoặc nền caro. Giữ khoảng đệm quanh hình và alpha thật.

- Thổ: nâu đất, hoàng kim, đá nứt, địa chấn, sơn ấn; phân biệt đạn, tường, giáp phản kích và núi trấn áp.
- Lôi: tím, xanh điện, lõi trắng; phân biệt cầu, tia giáng, dư ảnh, xích điện và thiên kiếp.
- Băng: lam nhạt, tinh thể trắng; phân biệt châm, tiễn, thuẫn phản kích, băng địa và băng ngục.
- Phong: lục ngọc nhạt, trắng bạc; phân biệt tiễn, lưỡi cong, dư ảnh, lốc và khe gió hư không.

Icon từng skill lấy từ frame rõ nhất của asset chính khi phù hợp; bị động dùng biểu tượng cùng hệ với biến thể trực quan và tên đầy đủ trong tooltip. Không tạo animation thi triển cho bị động.

Giữ cache tối đa 2 sheet; tải lỗi dùng fallback và không làm mất nhân vật. Preview và chiến đấu cùng gọi renderer; không phát trùng hiệu ứng. Giữ hỗ trợ giảm chuyển động. Mở rộng trang fx-preview để duyệt đủ 32 chiêu.

## Mở công pháp và tương thích

Bật Open cho bốn hệ sau khi có dữ liệu và asset. Bí kíp hiện có tiếp tục mở cây; pool rương dùng CultivationManuals.Available cho cả 7 hệ, giữ tổng xác suất rơi công pháp hiện tại, cập nhật mô tả hết nhắc riêng 3 hệ. Kiểm tra các chỗ hardcode hoa/thuy/moc ở server, UI, bản dịch và save guard.

Không sửa dữ liệu người chơi hoặc cấp bí kíp miễn phí. Không ghi đè các thay đổi đang có trong GameService, ChestRewards, GameData, PetLoot và các JS ngoài phạm vi. Nếu phải sửa file bẩn, chỉ áp dụng hunk liên quan và kiểm tra diff.

## Kiểm tra và tiêu chí bàn giao

Kiểm tra 7 hệ đều có đúng 1 đòn thường; bốn cây mới đủ 19 nút, ID/code duy nhất, tiền đề tồn tại và không vòng; sát thương và chi phí đúng mốc; học, nâng cấp, trang bị, đổi hệ, hoàn điểm và save cũ hoạt động. Chạy kiểm tra server/skill/manual phù hợp và dotnet build với thư mục output riêng nếu game đang chạy.

Kiểm tra 32 ảnh tồn tại, đúng kích thước và alpha; toàn bộ manifest trỏ đúng ảnh; không có ô rỗng ngoài chủ ý hoặc hình chảy qua ô. Kiểm tra frame tại biên pha, cache, tải lỗi và context save/restore. Duyệt hình trên nền chiến đấu tối và sáng; xem từng animation ở tốc độ thật, quái nhỏ/lớn và cửa sổ mặc định. Báo rõ những kiểm tra không chạy được; không gọi build là bằng chứng chất lượng hình ảnh.

Đầu ra: code tích hợp, 32 sprite sheet, manifest, preview chạy được, prompt/nguồn ảnh và báo cáo kiểm chứng. Không bao gồm thay trang phục, cân bằng lại ba hệ cũ hoặc cơ chế chiến đấu mới.
