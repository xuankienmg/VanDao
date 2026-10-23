# Website và wiki

`index.html` là trang giới thiệu / tải game; `wiki.html` là wiki tiếng Việt có tìm kiếm, lọc và liên kết trực tiếp tới chi tiết vật phẩm / linh thú.

Từ thư mục gốc repository, cập nhật dữ liệu và ảnh wiki bằng:

```powershell
dotnet run --project tools/wiki_export -- .
```

Công cụ đọc trực tiếp các lớp dữ liệu game, xuất `site/data/wiki.json` và sao chép ảnh được tham chiếu vào `site/img/wiki/`. Không đọc cơ sở dữ liệu người chơi hoặc cấu hình tài khoản. Các ải được tính bằng `GameData.AreaWaves` và `StoneRange`, gồm cả ba mức khó. Chỉ số linh thú là chỉ số gốc của loài, chưa áp dụng cấp / tư chất / cảnh giới / sao của từng con.

Xem thử bằng HTTP (không mở `wiki.html` qua `file://`, vì trang cần tải JSON):

```powershell
python -m http.server 4173 --directory site
```

Mở `http://localhost:4173/wiki.html`. Workflow `deploy-site.yml` tự xuất lại wiki trước khi triển khai GitHub Pages.

Kiểm tra dữ liệu, đường dẫn ảnh và logic tìm kiếm / điều hướng sau khi xuất:

```powershell
node tools/wiki_export/check.cjs
```
