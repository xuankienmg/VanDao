namespace VanDao.Game;

public record PetForm(string Name, string Image, double ShareMult, string SkillName);
public record PetStarRecipe(int TargetStars, int SameSpeciesStars, int OtherCount, int OtherStars);
public record PetDef(string Id, string Name, string Element, string Rarity, string Role, string Desc,
    PetForm[] Forms, string TalentText, string SkillText, int SkillEvery,
    string Talent, double TalentValue, string Skill, int Atk, int Def, int Hp);

public static class PetData
{
    public const string StarterEgg = "thu_dan_hoa_ho";
    public static int EggQuality(string id) => id == StarterEgg ? 1 : Array.IndexOf(EggIds, id);
    public static readonly string[] Qualities = { "common", "uncommon", "rare", "epic", "legendary" };
    public static readonly string[] EggIds = { "thu_dan_pham", "thu_dan_linh", "thu_dan_huyen", "thu_dan_dia", "thu_dan_thien" };
    public static readonly int[] HatchSeconds = { 600, 1800, 3600, 14400, 28800 };
    public static readonly double[] PetSpinWeights = { 45, 30, 17, 7, 1 };
    // Calibrated against peak-stage, full same-realm Huyen gear at aptitude 90.
    // Runtime scaling depends only on realm, never on the owner's actual gear.
    public static readonly double[] RealmStatScale = { .36, .385, 1.47, 2.98, 13.3, 59.8, 299.0 };
    // Phẩm Phàm → Thiên (10/2026: giãn từ .8–1.6 để các phẩm khác nhau rõ hơn).
    public static readonly double[] RarityStatScale = { .7, 1, 1.35, 1.8, 2.4 };
    /// <summary>Khoảng tư chất khi nở theo phẩm Thú Đản (Phàm → Thiên).</summary>
    public static readonly int[] AptMin = { 40, 50, 60, 70, 80 }, AptMax = { 75, 85, 90, 95, 100 };
    // ---- linh thú thừa: ghép cùng loài lên sao, phóng sinh (lẻ / hàng loạt) hoàn Linh Thú Đan ----
    public const int MaxStars = 5;
    /// <summary>Ngưỡng điểm cũ chỉ để đọc dữ liệu đã lưu, không còn là chi phí ghép.</summary>
    public static readonly int[] StarCost = { 2, 3, 4, 5, 6 };
    /// <summary>Mã ngưỡng sao tương thích dữ liệu cũ; công thức hiến nằm ở StarRecipes.</summary>
    public static int StarAt(int s) => StarCost.Take(Math.Clamp(s, 0, MaxStars)).Sum();
    public static int MaxPts => StarAt(MaxStars);
    public static int StarsFrom(int pts) { var s = 0; while (s < MaxStars && pts >= StarAt(s + 1)) s++; return s; }
    /// <summary>Điểm ghép của con: con có sao từ luật cũ (chưa có StarPts) quy đổi bằng mốc của số sao đó.</summary>
    public static int Pts(VanDao.Data.Pet pet) => pet.StarPts > 0 || pet.Stars <= 0 ? pet.StarPts : StarAt(pet.Stars);
    public static readonly PetStarRecipe[] StarRecipes =
    [
        new(1, 0, 0, 0), new(2, 1, 1, 0), new(3, 2, 1, 1),
        new(4, 3, 1, 2), new(5, 4, 1, 3),
    ];
    public static int StarsOf(VanDao.Data.Pet pet) => StarsFrom(Pts(pet));
    public static bool CanStarDonor(VanDao.Data.Pet main, VanDao.Data.Pet donor, bool sameSpecies)
    {
        var stars = StarsOf(main);
        if (stars >= MaxStars || donor.Id == main.Id || donor.Slot != 0 ||
            !ById.ContainsKey(main.DefId) || !ById.ContainsKey(donor.DefId)) return false;
        var recipe = StarRecipes[stars];
        if (sameSpecies) return donor.DefId == main.DefId && StarsOf(donor) == recipe.SameSpeciesStars;
        var quality = QualityOf(main, ById[main.DefId]);
        var donorQuality = QualityOf(donor, ById[donor.DefId]);
        return recipe.OtherCount > 0 && StarsOf(donor) == recipe.OtherStars &&
            donorQuality <= quality && donorQuality >= Math.Max(0, quality - 1);
    }
    /// <summary>Mỗi sao +6% chỉ số, hệ số kỹ năng và thiên phú (5 sao: +30%).</summary>
    public const double StarBonus = .06;
    /// <summary>Linh Thú Đan nhận khi phóng sinh theo phẩm Phàm → Thiên (cộng nửa số đan đã cho ăn; mỗi sao tính thêm một phần).</summary>
    public static readonly int[] ReleaseFood = { 5, 10, 20, 40, 80 };
    public static int ReleaseValue(VanDao.Data.Pet pet, PetDef d) => pet.FoodSpent / 2 + ReleaseFood[QualityOf(pet, d)] * (1 + StarsFrom(Pts(pet)));
    public static int SpeciesQuality(PetDef d) => Array.IndexOf(Qualities, d.Rarity);
    /// <summary>Phẩm của con linh thú: Quality riêng (vd. Phàm từ Thú Đản Phàm), không có thì theo loài.</summary>
    public static int QualityOf(VanDao.Data.Pet pet, PetDef d) => pet.Quality is >= 0 and < 5 ? pet.Quality : SpeciesQuality(d);
    /// <summary>Chỉ số gốc của loài đã nhân phẩm loài; con mang phẩm khác loài thì đổi theo tỉ lệ.</summary>
    public static double QualityAdjust(VanDao.Data.Pet pet, PetDef d) => RarityStatScale[QualityOf(pet, d)] / RarityStatScale[SpeciesQuality(d)];
    public static double StatScale(int realm, PetDef def) =>
        RealmStatScale[Math.Clamp(realm, 0, RealmStatScale.Length - 1)];
    public const int FoodExp = 100;
    // Giảm 75% so với tỉ lệ gốc 15% / 12% / 10% / 8%.
    public static readonly double[] PetEggDrops = { .0375, .0375, .03, .025, .025, .025, .02, .02, .02, .02 };
    public static int EggQuality(int chapter) => chapter>=6?4:chapter>=3?3:chapter>=2?2:1;
    // Mốc tính theo cấp hiện tại: từ cấp 25, 50, 65, 80 tăng dần chi phí lên cấp.
    public static int ExpNeed(int level) => (50 + level * 25) * (level switch
    {
        >= 80 => 5,
        >= 65 => 4,
        >= 50 => 3,
        >= 25 => 2,
        _ => 1,
    });
    public static int LevelCap(int realm) => new[] {20,40,60,80,90,100}[Math.Clamp(realm,0,5)];
    public static int Capacity(int realm) => 20 + Math.Max(0,realm) * 5;
    public static int GuardSlots(int realm) => 0;
    public static int HatchSlots(int realm) => realm >= 2 ? 2 : 1;
    private static string[] SkillNames(string id) => id switch {
        "hoa_linh_ho" => ["Hỏa Vĩ", "Liệt Diễm Hồ Ảnh", "Cửu Vĩ Phần Thiên"],
        "bang_tinh_tho" => ["Sương Giáp", "Băng Kính Hộ Thân", "Quảng Hàn Thiên Mạc"],
        "thanh_moc_loc" => ["Thanh Mộc Hồi Xuân", "Linh Lộc Cam Lộ", "Vạn Mộc Trường Sinh"],
        "kim_giap_quy" => ["Kim Giáp Hộ Thể", "Huyền Nham Bích Lũy", "Bất Diệt Kim Thành"],
        "loi_van_dieu" => ["Lôi Vũ", "Vân Tiêu Lôi Kích", "Cửu Tiêu Lôi Kiếp"],
        "bich_thuy_giao" => ["Giao Long Thủy Tức", "Bích Hải Triều Sinh", "Thương Hải Quy Nguyên"],
        "thanh_phong_hac" => ["Thanh Phong Dưỡng Khí", "Linh Hạc Hồi Nguyên", "Cửu Thiên Tiên Tức"],
        "tu_dien_mieu" => ["Tử Điện Trảo", "Lôi Ảnh Tập Sát", "Tịch Diệt Thiên Lôi"],
        "chu_tuoc" => ["Xích Vũ", "Phượng Diễm Liệu Nguyên", "Chu Tước Phần Thiên"],
        "huyen_vu" => ["Huyền Thủy Thuẫn", "Quy Xà Hộ Trận", "Bắc Minh Thiên Bích"],
        "thanh_long" => ["Thanh Lôi Thổ Tức", "Cửu Thiên Lôi Dẫn", "Thanh Long Thiên Phạt"],
        "bach_ho" => ["Bạch Hổ Liệt Trảo", "Canh Kim Phá Quân", "Bạch Hổ Tru Thiên"],
        _ => throw new ArgumentOutOfRangeException(nameof(id), id, "Chưa đặt tên tuyệt kỹ cho linh thú.")
    };
    private static PetDef Make(string id,string name,string el,int q,string role,string talent,double value,string text,string skill)
    {
        var k = RarityStatScale[q];
        return new(id,name,el,Qualities[q],role,"Ấp từ Thú Đản; trưởng thành ở cấp 30 và 60.",
            Enumerable.Range(0,3).Select(f=>new PetForm(new[]{"Ấu thể","Thành thể","Thần thú"}[f],$"img/pet/{id}{(id=="thanh_long"?"_loi":"")}_{f}.png",new[]{.10,.15,.22}[f],SkillNames(id)[f])).ToArray(),
            text,SkillDescription(skill,0,0),skill is "fox" or "frost" or "turtle"?5:skill=="phoenix"?3:4,
            talent,value,skill,(int)Math.Round(24*k),(int)Math.Round(12*k),(int)Math.Round(120*k));
    }
    public static double StarScale(int stars) => 1 + Math.Clamp(stars,0,MaxStars) * StarBonus;
    public static string Percent(double value) => value.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture).Replace('.',',');
    public static string SkillDescription(string skill,int form,int stars)
    {
        var f=Math.Clamp(form,0,2);var scale=StarScale(stars);
        string V(double a,double b,double c)=>Percent((f==0?a:f==1?b:c)*scale);
        return skill switch {
            "fox" => $"Gây {V(70,105,140)}% công. Đánh dấu 2 lượt kế tiếp: đòn bạo kích chủ nhân tăng {V(5,8,12)}% sát thương.",
            "frost" => $"Tạo khiên {V(6,9,12)}% máu tối đa, giảm {V(5,8,12)}% công địch trong 2 lượt.",
            "deer" => $"Hồi {V(5,8,11)}% máu tối đa; hồi thêm {V(1,1.5,2)}% mỗi lượt trong 2 lượt kế tiếp.",
            "turtle" => $"Tạo khiên {V(8,12,16)}% máu tối đa. Khi khiên còn: giảm thêm {V(3,5,7)}% sát thương nhận vào.",
            "eagle" => $"Đánh 3 lần, mỗi lần gây {V(25,38,52)}% công; tính phòng thủ riêng từng lần.",
            "leech" => $"Gây {V(65,100,135)}% công. Hồi {V(20,25,30)}% sát thương thực gây ra, tối đa {V(4,6,8)}% máu tối đa.",
            "crane" => $"Hồi {V(4,6,8)}% máu tối đa, tăng {V(5,8,12)}% né tránh trong 2 lượt.",
            "cat" => $"Gây {V(90,135,180)}% công, bỏ qua {V(15,25,35)}% phòng thủ địch.",
            "phoenix" => $"Gây {V(80,120,160)}% công; thiêu đốt thêm {V(15,25,35)}% công mỗi lượt trong 2 lượt kế tiếp.",
            "xuanwu" => $"Tạo khiên {V(10,15,20)}% máu tối đa. Trong 2 lượt, khi bị đánh phản {V(15,25,35)}% công.",
            "dragon" => $"Gọi 3 tia sét, mỗi tia gây {V(35,55,75)}% công."+(f==2?$" Tia cuối bỏ qua {V(20,20,20)}% phòng thủ địch.":""),
            "tiger" => $"Gây {V(110,165,220)}% công; mục tiêu dưới 30% máu chịu thêm {V(25,25,25)}% sát thương chiêu.",
            _ => throw new ArgumentOutOfRangeException(nameof(skill))
        };
    }
    public static string TalentDescription(PetDef def,int stars)
    {
        var value=def.TalentValue*StarScale(stars);var v=Percent(value);
        var effect=def.Talent switch {
            "crit"=>$"chủ nhân +{v}% bạo kích.",
            "guard"=>$"chủ nhân giảm {v}% sát thương nhận vào.",
            "healing"=>$"dưới 50% máu, chủ nhân nhận thêm {v}% hiệu quả hồi máu.",
            "def"=>$"chủ nhân +{v}% phòng thủ.",
            "extra"=>$"chủ nhân +{v}% liên kích.",
            "qi"=>$"chủ nhân +{v}% tốc độ thu tu vi.",
            "speed"=>$"chủ nhân +{v}% thân pháp thám hiểm.",
            "pen"=>$"chủ nhân +{v}% xuyên thủ.",
            "burn_damage"=>$"chủ nhân gây thêm {v}% sát thương lên mục tiêu đang cháy.",
            "shield"=>$"đầu trận nhận khiên bằng {v}% máu tối đa.",
            "atk"=>$"chủ nhân +{v}% tấn công.",
            "critdmg"=>$"chủ nhân +{v}% sát thương bạo kích (150% → {Percent(150+value)}%).",
            _=>def.TalentText
        };
        return def.TalentText.Split(':')[0]+": "+effect;
    }
    public static readonly PetDef[] Definitions = {
        Make("hoa_linh_ho","Hỏa Linh Hồ","hoa",1,"atk","crit",4,"Hồ Hỏa Linh Tâm: chủ nhân +4% bạo kích.","fox"),
        Make("bang_tinh_tho","Băng Tinh Thố","bang",1,"def","guard",4,"Băng Tâm Hộ Chủ: chủ nhân giảm 4% sát thương nhận vào.","frost"),
        Make("thanh_moc_loc","Thanh Mộc Lộc","moc",2,"hp","healing",15,"Sinh Sinh Bất Tức: dưới 50% máu, chủ nhân nhận thêm 15% hiệu quả hồi máu.","deer"),
        Make("kim_giap_quy","Kim Giáp Quy","tho",2,"def","def",10,"Kim Giáp Kiên Tâm: chủ nhân +10% phòng thủ.","turtle"),
        Make("loi_van_dieu","Lôi Vân Điêu","loi",2,"atk","extra",5,"Lôi Đình Truy Kích: chủ nhân +5% liên kích.","eagle"),
        Make("bich_thuy_giao","Bích Thủy Giao","thuy",3,"qi","qi",10,"Thủy Linh Dẫn Khí: chủ nhân +10% tốc độ thu tu vi.","leech"),
        Make("thanh_phong_hac","Thanh Phong Hạc","gio",3,"crit","speed",5,"Thanh Phong Đồng Hành: chủ nhân +5% thân pháp thám hiểm.","crane"),
        Make("tu_dien_mieu","Tử Điện Miêu","loi",3,"atk","pen",8,"Tử Điện Xuyên Tâm: chủ nhân +8% xuyên thủ.","cat"),
        Make("chu_tuoc","Chu Tước","hoa",4,"atk","burn_damage",10,"Bất Diệt Chân Hỏa: chủ nhân gây thêm 10% sát thương lên mục tiêu đang cháy.","phoenix"),
        Make("huyen_vu","Huyền Vũ","thuy",4,"def","shield",10,"Bắc Minh Hộ Chủ: đầu trận nhận khiên bằng 10% máu tối đa.","xuanwu"),
        Make("thanh_long","Thanh Long","loi",4,"atk","atk",8,"Lôi Long Chi Uy: chủ nhân +8% tấn công.","dragon"),
        Make("bach_ho","Bạch Hổ","gio",4,"crit","critdmg",20,"Canh Kim Sát Phạt: chủ nhân +20% sát thương bạo kích (150% → 170%).","tiger") };
    public static readonly IReadOnlyDictionary<string,PetDef> ById = Definitions.ToDictionary(x=>x.Id);
    // Common eggs contain the two entry species; egg quality controls aptitude, species retains its own rarity.
    public static PetDef[] Pool(int quality) => quality==0 ? Definitions.Take(2).ToArray() : Definitions.Where(d=>d.Rarity==Qualities[quality]).ToArray();
    public static Dictionary<string,int> EvolveCost(int form) => form==0 ? new(){["hoa_hinh_dan"]=10,["linh_thach"]=10000,["kim_o_tinh_hach"]=2} : form==1 ? new(){["than_thu_huyet"]=10,["hoa_hinh_dan"]=20,["linh_thach"]=100000,["huyet_giao_hach"]=3} : new();
    public static readonly ItemDef[] Items = EggIds.Select((id,q)=>new ItemDef(id,new[]{"Phàm","Linh","Huyền","Địa","Thiên"}[q]+" Thú Đản","egg","#bda5ef","Ấp trong menu Linh thú.",Icon:$"img/pet/egg_{new[]{"pham","linh","huyen","dia","thien"}[q]}.png",Rarity:Qualities[q],Kind:"Thú Đản"))
        .Concat(new[]{new ItemDef(StarterEgg,"Trứng Hỏa Hồ","egg","#edaa73","Quà khởi đầu. Ấp chắc chắn nhận Hỏa Linh Hồ.",Icon:"img/pet/egg_linh.png",Rarity:"uncommon",Kind:"Thú Đản"),new ItemDef("linh_thu_dan","Linh Thú Đan","petfood","#abd78a","+100 EXP linh thú. Rơi ở ải thường, chia tỉ lệ với đan tiến giai; chương và ải cao rơi nhiều hơn.",Icon:"img/pet/linh_thu_dan.png"),new ItemDef("hoa_hinh_dan","Hóa Hình Đan","petmaterial","#bda5ef","Nguyên liệu tiến hóa. Có thể rơi từ Boss và Rương Boss.",Icon:"img/pet/hoa_hinh_dan.png"),new ItemDef("than_thu_huyet","Thần Thú Huyết","petmaterial","#efab86","Nguyên liệu thần thú. Rơi từ Boss chương 5 trở đi hoặc mức Khó / Ác mộng và Rương Boss tương ứng.",Icon:"img/pet/than_thu_huyet.png")}).ToArray();
}
