using VanDao.Game;
using System.Text.Json;
using System.Text;

static class ChestPlan
{
    record Outcome(int Quality,double Value,double Probability);
    static int Pick(double[] w,double roll){var total=w.Sum();for(var i=0;i<w.Length;i++)if((roll-=w[i]/total)<0)return i;return w.Length-1;}
    public static void Run(){
        if(GameData.Chests["ruong_thuong"].Herb>0){Console.WriteLine("Plan đã áp dụng. Giữ nguyên báo cáo mô phỏng trước triển khai trong docs/chests; chạy ChestChecks để kiểm tra cấu hình hiện tại.");return;}
        const int draws=100000;var results=new List<object>();
        double PriceMultiplier(int q)=>q switch{5=>1.25,6=>1.5,7=>1.75,8=>2,9=>2.5,_=>1};
        var report=new StringBuilder("# Đề xuất cập nhật rương và giá bán trang bị — bản 6\n\nTrạng thái: **chưa áp dụng vào game**. Diễn giải phẩm Thành là Thánh. Thay thế đề xuất trước: từ Nguyên Anh mở Thiên trong rương thường và Thần trong rương hiếm, tăng dần theo cảnh giới. Từ Luyện Hư, rương hiếm bổ sung Tiên và Đạo; tỉ lệ tăng ở Hợp Thể. Rương thường vẫn tối đa Thiên. Bổ sung linh thảo và Ngộ Đạo Đan (chỉ từ Nguyên Anh), lấy xác suất từ nhánh đan lên tầng. Giữ số lượng đan mỗi lần trúng, cấp/bộ trang bị và tỉ lệ rơi rương. Bổ sung bảng rương Boss: phẩm nền cao hơn, Thần từ Nguyên Anh, Tiên/Đạo từ Luyện Hư; Đạo gấp 10 lần rương hiếm trong nhánh trang bị/pháp bảo. Giữ nguyên tỉ lệ rơi rương Boss và số rương theo độ khó. Giá bán mới áp dụng theo phẩm, bất kể nguồn nhận.\n\n## Giá bán trang bị đề xuất\n\nChỉ tăng giá của trang bị/pháp bảo có ô mặc, không tăng giá nguyên liệu, đan hoặc vé cùng phẩm. Giữ hệ số cảnh giới hiện có; giá được tính lại theo định nghĩa vật phẩm nên đồ cũ trong túi cũng hưởng giá mới.\n\n| Phẩm | Hệ số so với giá hiện tại |\n|---|---:|\n| Thánh | ×1,25 |\n| Thần | ×1,5 |\n| Tiên | ×1,75 |\n| Đạo | ×2 |\n| Huyền Tiên | ×2,5 |\n\nChỉ số giá trị bên dưới: giá bán NPC kỳ vọng, không đại diện toàn bộ giá trị sử dụng. Không giữ mục tiêu +12% sau khi thêm nhánh thưởng mới; báo cáo lại chênh lệch cho từng cảnh giới. Giá rương/nguyên liệu và số đan không thay đổi.\n\n");
        foreach(var chest in new[]{"ruong_thuong","ruong_hiem","ruong_boss"}){
            var cd=GameData.Chests[chest];report.AppendLine($"## {GameData.ItemById[chest].Name}\n");
            report.AppendLine("Mỗi dòng phẩm bên dưới là xác suất **có điều kiện khi nhận trang bị hoặc pháp bảo**, không phải xác suất trên mọi rương.\n");
            for(var realm=0;realm<Math.Min(GameData.OpenChapters,GameData.Realms.Count);realm++){
                var r=GameData.Realms[realm];var before=new double[10];var original=cd.QualityAt(realm);for(var q=0;q<original.Length;q++)before[q]=original[q]/original.Sum()*100;
                var treasures=GameData.GearBases.Where(b=>b.Slot=="offhand"&&b.MinRealm<=Math.Max(1,realm)).ToArray();
                var gear=r.Drops.Where(d=>GameData.GearBaseById.TryGetValue(d.Item,out var b)&&b.Slot!="offhand").ToArray();
                var pt=treasures.Length>0?cd.Treasure:0;var pg=gear.Length>0?(1-pt)*cd.GearShare:0;var pp=1-pt-pg;
                var pill=GameData.ItemById[r.StageCost(0).Keys.First(k=>k!="linh_thach")];
                var herbChance=chest=="ruong_thuong"?.20:chest=="ruong_hiem"?.25:.15;
                var enlightenmentChance=realm>=GameData.ItemById["ngo_dao_dan"].MinRealm ? (chest=="ruong_thuong"?.03:chest=="ruong_hiem"?.04:.05) : 0;
                var newPillChance=pp-herbChance-enlightenmentChance;
                if(newPillChance<0)throw new Exception("New reward branches exceed available pill probability");
                var herbMin=chest=="ruong_thuong"?1:chest=="ruong_hiem"?2:4;
                var herbMax=chest=="ruong_thuong"?2:chest=="ruong_hiem"?4:6;
                var herbs=GameData.HerbDrops.Where(h=>h.Chapters.Contains(realm)).GroupBy(h=>h.Item).Select(g=>new {Item=GameData.ItemById[g.Key],Weight=g.Sum(h=>h.Chance)}).ToArray();
                if(herbs.Length==0)throw new Exception("No eligible realm herbs");
                double Value(ItemDef b,int q)=>GameData.ItemById[GameData.GearId(b,q)].Sell;
                double NewValue(ItemDef b,int q)=>Math.Round(Value(b,q)*PriceMultiplier(q),MidpointRounding.AwayFromZero);
                double EquipmentValue(int q)=>pt*(treasures.Length>0?treasures.Average(b=>Value(b,q)):0)+pg*(gear.Length>0?gear.Sum(g=>Value(GameData.GearBaseById[g.Item],q)*g.Chance)/gear.Sum(g=>g.Chance):0);
                var ev=Enumerable.Range(0,before.Length).Select(EquipmentValue).ToArray();
                var pillValue=pp*pill.Sell*(cd.PillMin+cd.PillMax)/2.0;
                double Expected(double[] weights)=>pillValue+weights.Select((w,q)=>w/100*ev[q]).Sum();
                var oldExpected=Expected(before);var top=Array.FindLastIndex(before,v=>v>0);var from=top-1;
                if(from<0||before[from]<=0)throw new Exception("No adjacent quality to shift");
                var shift=Math.Round(oldExpected*.12/(ev[top]-ev[from])*100,1);
                if(shift<=0||shift>before[from])throw new Exception("Proposed shift outside available mass");
                var after=(double[])before.Clone();after[from]-=shift;after[top]+=shift;
                if(realm>=3){
                    double[] proposal=chest=="ruong_thuong" ? realm switch{
                        3=>new double[]{0,15,45,30,10},4=>new double[]{0,0,30,45,25},5=>new double[]{0,0,20,40,40},_=>new double[]{0,0,10,40,50}
                    } : realm switch{
                        3=>new double[]{0,5,35,42,15,2.8,.2},4=>new double[]{0,0,20,42,30,7.5,.5},5=>new double[]{0,0,15,35,36,12.9,1,.09,.01},_=>new double[]{0,0,10,25,42,20.7,2,.27,.03}
                    };
                    after=new double[10];Array.Copy(proposal,after,proposal.Length);
                }
                if(chest=="ruong_boss"){
                    double[] proposal=realm switch{
                        0=>new double[]{0,45,55},1=>new double[]{0,0,65,35},2=>new double[]{0,0,25,68,7},
                        3=>new double[]{0,0,8,46,36,9,1},4=>new double[]{0,0,0,30,50,17,3},
                        5=>new double[]{0,0,0,20,49,24,6,.9,.1},_=>new double[]{0,0,0,10,40,35,12,2.7,.3}
                    };
                    after=new double[10];Array.Copy(proposal,after,proposal.Length);
                }
                if(Math.Abs(after.Sum()-100)>1e-7||after.Any(w=>w<0))throw new Exception("Invalid proposed probabilities");
                List<Outcome> Outcomes(double[] weights,bool updated){var o=new List<Outcome>();for(var q=0;q<weights.Length;q++){
                    foreach(var t in treasures)o.Add(new(q,updated?NewValue(t,q):Value(t,q),pt/treasures.Length*weights[q]/100));
                    foreach(var g in gear)o.Add(new(q,updated?NewValue(GameData.GearBaseById[g.Item],q):Value(GameData.GearBaseById[g.Item],q),pg*g.Chance/gear.Sum(x=>x.Chance)*weights[q]/100));
                }for(var n=cd.PillMin;n<=cd.PillMax;n++)o.Add(new(-1,pill.Sell*n,(updated?newPillChance:pp)/(cd.PillMax-cd.PillMin+1)));
                if(updated){
                    foreach(var herb in herbs)for(var n=herbMin;n<=herbMax;n++)o.Add(new(-2,herb.Item.Sell*n,herbChance*herb.Weight/herbs.Sum(x=>x.Weight)/(herbMax-herbMin+1)));
                    if(enlightenmentChance>0)o.Add(new(-3,GameData.ItemById["ngo_dao_dan"].Sell,enlightenmentChance));
                }
                if(Math.Abs(o.Sum(x=>x.Probability)-1)>1e-7)throw new Exception("Reward outcome probabilities do not sum to one");
                return o;}
                object Simulate(double[] weights,int seed,bool updated){var outcomes=Outcomes(weights,updated);var rng=new Random(seed);double sum=0;var counts=new int[weights.Length];int pills=0,herbCount=0,enlightenment=0;
                    for(var i=0;i<draws;i++){var roll=rng.NextDouble();var selected=outcomes[^1];foreach(var o in outcomes){roll-=o.Probability;if(roll<0){selected=o;break;}}sum+=selected.Value;if(selected.Quality==-1)pills++;else if(selected.Quality==-2)herbCount++;else if(selected.Quality==-3)enlightenment++;else counts[selected.Quality]++;}
                    return new{draws,meanNpcValue=sum/draws,qualityCounts=counts,pillRewards=pills,herbRewards=herbCount,enlightenmentRewards=enlightenment};}
                var nextExpected=Outcomes(after,true).Sum(o=>o.Value*o.Probability);var ratio=nextExpected/oldExpected-1;
                var baseline=Simulate(before,20000+realm,false);var proposed=Simulate(after,20000+realm,true);
                results.Add(new{chest,realm=r.Name,before,after,treasurePercent=pt*100,gearPercent=pg*100,oldPillPercent=pp*100,pillPercent=newPillChance*100,herbPercent=herbChance*100,enlightenmentPercent=enlightenmentChance*100,herbMin,herbMax,herbPool=herbs.Select(h=>h.Item.Id).ToArray(),oldExpected,newExpected=nextExpected,increasePercent=ratio*100,baseline,proposed});
                report.AppendLine($"### {r.Name}\n\nNhóm thưởng đề xuất: pháp bảo {pt*100:0.#}%, trang bị {pg*100:0.#}%, đan lên tầng {newPillChance*100:0.#}% ({cd.PillMin}–{cd.PillMax} viên), linh thảo {herbChance*100:0.#}% ({herbMin}–{herbMax} món), Ngộ Đạo Đan {enlightenmentChance*100:0.#}% (1 viên). Linh thảo phù hợp: {string.Join(", ",herbs.Select(h=>h.Item.Name))}.\n");
                report.AppendLine("| Phẩm | Hiện tại | Đề xuất |\n|---|---:|---:|");
                for(var q=0;q<before.Length;q++)report.AppendLine($"| {GameData.QualityName[q]} | {before[q]:0.#}% | {after[q]:0.##}% |");
                report.AppendLine($"\nGiá bán NPC kỳ vọng: **{oldExpected:0.##} → {nextExpected:0.##}** linh thạch/rương (**+{ratio*100:0.##}%**). Mô phỏng 100.000 lượt trước và 100.000 lượt sau, cùng seed.\n");
                if(realm>=3){var q=chest=="ruong_thuong"?4:6;report.AppendLine($"Xác suất phẩm **{GameData.QualityName[q]} trên mọi rương**: {after[q]*(pt+pg):0.###}% (tỉ lệ nhóm trang bị/pháp bảo × tỉ lệ phẩm).\n");}
                if(chest!="ruong_thuong"&&realm>=5)report.AppendLine($"Bổ sung từ Luyện Hư: Tiên **{after[7]:0.##}%**, Đạo **{after[8]:0.##}%** trong nhánh trang bị/pháp bảo. Tỉ lệ Đạo trên mọi rương: **{after[8]*(pt+pg):0.###}%**. Chưa mở Huyền Tiên. Đây là phần thưởng rất hiếm; 100.000 lượt không đủ để ước lượng chính xác, cần dựa xác suất lý thuyết và kiểm tra mốc chọn phẩm.\n");
            }
        }
        report.AppendLine("## Kế hoạch áp dụng sau phê duyệt\n\n1. Tạo bảng phẩm riêng cho cả ba loại rương với dòng riêng cho từng cảnh giới đang mở; không sửa DropQuality dùng chung.\n2. Thêm hệ số giá bán chỉ cho trang bị phẩm Thánh trở lên trong SellPrice; cập nhật mọi màn bán/tooltip từ cùng dữ liệu server, dùng kiểu số đủ lớn khi tính tổng.\n3. Áp dụng bảng phẩm Boss đề xuất, giữ xác suất tuyệt đối pháp bảo/trang bị và số đan mỗi lần trúng; thêm nhánh linh thảo, Ngộ Đạo Đan, giảm xác suất nhánh đan lên tầng. Không tiếp tục dùng GearShare trên phần xác suất còn lại đã bị giảm. Giá bán mới vẫn áp dụng cho trang bị lấy từ Boss, quay vé hoặc đồ cũ.\n4. Kiểm tra không ra Thiên từ rương thường trước Nguyên Anh; không ra Thần từ rương hiếm trước Nguyên Anh; không ra Tiên/Đạo trước Luyện Hư; không ra Huyền Tiên; tỉ lệ tăng dần ở các cảnh giới sau.\n5. Chọn đúng một trong năm nhóm thưởng; Ngộ Đạo Đan chỉ từ Nguyên Anh, mỗi lần 1 viên, +1 điểm công pháp như vật phẩm hiện có. Linh thảo chọn theo trọng số HerbDrops của chương tương ứng cảnh giới (bỏ điều kiện ải vì là thưởng rương), không có linh thảo chương tương lai. Nếu pool trống phải phát hiện cấu hình lỗi trước khi mở, không nuốt rương. ChestOdds và OpenOne cùng cấu hình; kiểm tra mở đơn/nhiều, hoàn rương khi túi đầy, tính tiền bán và đồ đang mặc không bị bán.\n6. Kiểm tra rương Boss có giá trị NPC kỳ vọng và xác suất phẩm cao hơn rương hiếm ở cùng cảnh giới. Giữ số rương Boss theo độ khó hiện tại; kiểm tra tổng tiền thưởng mỗi chuyến ở Khó/Ác mộng vì giá đồ cao hơn.\n7. Mô phỏng giá bán không đo hết độ hữu ích của đan, pháp bảo và ghép đồ; cần chơi thử trước khi chốt cân bằng.\n");
        var root=new DirectoryInfo(Directory.GetCurrentDirectory());
        while(root!=null&&!File.Exists(Path.Combine(root.FullName,"VanDao.csproj")))root=root.Parent;
        if(root==null)throw new Exception("Project root not found");
        var output=Path.Combine(root.FullName,"docs","chests");Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output,"2026-10-20-drop-rate-proposal.md"),report.ToString());
        File.WriteAllText(Path.Combine(output,"2026-10-20-simulation.json"),JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"PASS revised proposal only: {results.Count} cases × {draws:N0} draws × before/after; all probability sums 100%; five reward branches, rare tiers and gear-only sale multipliers simulated. No production configuration changed.");
    }
}
