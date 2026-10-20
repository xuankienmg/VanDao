namespace VanDao.Game;

public record PetForm(string Name, string Image, double ShareMult, string SkillName);
public record PetDef(string Id, string Name, string Element, string Rarity, string Role, string Desc,
    PetForm[] Forms, string TalentText, string SkillText, int SkillEvery,
    string Talent, double TalentValue, string Skill, int Atk, int Def, int Hp);

public static class PetData
{
    public static readonly string[] Qualities = { "common", "uncommon", "rare", "epic", "legendary" };
    public static readonly string[] EggIds = { "thu_dan_pham", "thu_dan_linh", "thu_dan_huyen", "thu_dan_dia", "thu_dan_thien" };
    public static readonly int[] HatchSeconds = { 600, 1800, 3600, 14400, 28800 };
    public static readonly double[] PetSpinWeights = { 45, 30, 17, 7, 1 };
    // Calibrated against peak-stage, full same-realm Huyen gear at aptitude 90.
    // Runtime scaling depends only on realm, never on the owner's actual gear.
    public static readonly double[] RealmStatScale = { .36, .385, 1.47, 2.98, 13.3, 59.8, 299.0 };
    public static readonly double[] RarityStatScale = { .8, 1, 1.2, 1.4, 1.6 };
    // Thanh Long already grants 10% owner HP; reserve some of its flat-stat budget
    // for that talent so its total contribution stays near the other Heavenly pets.
    public const double HpTalentStatBudget = .74;
    public static double StatScale(int realm, PetDef def) =>
        RealmStatScale[Math.Clamp(realm, 0, RealmStatScale.Length - 1)] * (def.Talent == "hp" ? HpTalentStatBudget : 1);
    public const int FoodExp = 100;
    public static readonly double[] PetEggDrops = { .15, .15, .12, .10, .10, .10, .08, .08, .08, .08 };
    public static int EggQuality(int chapter) => chapter>=6?4:chapter>=3?3:chapter>=2?2:1;
    public static int ExpNeed(int level) => 50 + level * 25;
    public static int LevelCap(int realm) => new[] {20,40,60,80,90,100}[Math.Clamp(realm,0,5)];
    public static int Capacity(int realm) => 20 + Math.Max(0,realm) * 5;
    public static int GuardSlots(int realm) => realm >= 4 ? 2 : realm >= 2 ? 1 : 0;
    public static int HatchSlots(int realm) => realm >= 2 ? 2 : 1;
    private static PetDef Make(string id,string name,string el,int q,string role,string talent,double value,string text,string skill)
    {
        var k = RarityStatScale[q];
        return new(id,name,el,Qualities[q],role,"Ấp từ Thú Đản; trưởng thành ở cấp 30 và 60.",
            Enumerable.Range(0,3).Select(f=>new PetForm(new[]{"Ấu thể","Thành thể","Thần thú"}[f],$"img/pet/{id}_{f}.png",new[]{.10,.15,.22}[f],name+" · "+new[]{"Linh thuật","Huyền thuật","Thần thuật"}[f])).ToArray(),
            text,skill=="heal"?"Hồi 6/9/12% máu tối đa.":skill=="guard"?"Tạo khiên 8/12/16% máu tối đa.":"Gây 60/90/120% công chủ nhân (dao động 90–110%).",q>=4?3:q>=2?4:5,
            talent,value,skill,(int)Math.Round(24*k),(int)Math.Round(12*k),(int)Math.Round(120*k));
    }
    public static readonly PetDef[] Definitions = {
        Make("hoa_linh_ho","Hỏa Linh Hồ","hoa",1,"atk","crit",3,"+3% bạo kích","damage"),
        Make("bang_tinh_tho","Băng Tinh Thố","bang",1,"def","guard",4,"+4% giảm sát thương","guard"),
        Make("thanh_moc_loc","Thanh Mộc Lộc","moc",2,"hp","regen",2,"Hồi 2% máu mỗi lượt","heal"),
        Make("kim_giap_quy","Kim Giáp Quy","tho",2,"def","def",8,"+8% thủ","guard"),
        Make("loi_van_dieu","Lôi Vân Điêu","loi",2,"atk","extra",5,"+5% liên kích","damage"),
        Make("bich_thuy_giao","Bích Thủy Giao","thuy",3,"qi","qi",8,"+8% tu vi","heal"),
        Make("thanh_phong_hac","Thanh Phong Hạc","gio",3,"crit","speed",4,"+4% thân pháp","heal"),
        Make("tu_dien_mieu","Tử Điện Miêu","loi",3,"atk","pen",6,"+6% bỏ qua thủ","damage"),
        Make("chu_tuoc","Chu Tước","hoa",4,"atk","burn",1,"Kỹ năng gây thêm 3% máu quái","damage"),
        Make("huyen_vu","Huyền Vũ","thuy",4,"def","shield",10,"Khiên đầu trận 10% máu","guard"),
        Make("thanh_long","Thanh Long","moc",4,"hp","hp",10,"+10% máu","heal"),
        Make("bach_ho","Bạch Hổ","gio",4,"crit","critdmg",15,"+15% sát thương bạo kích","damage") };
    public static readonly IReadOnlyDictionary<string,PetDef> ById = Definitions.ToDictionary(x=>x.Id);
    // Common eggs contain the two entry species; egg quality controls aptitude, species retains its own rarity.
    public static PetDef[] Pool(int quality) => quality==0 ? Definitions.Take(2).ToArray() : Definitions.Where(d=>d.Rarity==Qualities[quality]).ToArray();
    public static Dictionary<string,int> EvolveCost(int form) => form==0 ? new(){["hoa_hinh_dan"]=10,["linh_thach"]=10000,["kim_o_tinh_hach"]=2} : form==1 ? new(){["than_thu_huyet"]=10,["hoa_hinh_dan"]=20,["linh_thach"]=100000,["huyet_giao_hach"]=3} : new();
    public static readonly ItemDef[] Items = EggIds.Select((id,q)=>new ItemDef(id,new[]{"Phàm","Linh","Huyền","Địa","Thiên"}[q]+" Thú Đản","egg","#bda5ef","Ấp trong menu Linh thú.",Icon:$"img/pet/egg_{new[]{"pham","linh","huyen","dia","thien"}[q]}.png",Rarity:Qualities[q],Kind:"Thú Đản"))
        .Concat(new[]{new ItemDef("linh_thu_dan","Linh Thú Đan","petfood","#abd78a","+100 EXP linh thú.",Icon:"img/pet/linh_thu_dan.png"),new ItemDef("hoa_hinh_dan","Hóa Hình Đan","petmaterial","#bda5ef","Nguyên liệu tiến hóa.",Icon:"img/pet/hoa_hinh_dan.png"),new ItemDef("than_thu_huyet","Thần Thú Huyết","petmaterial","#efab86","Nguyên liệu thần thú.",Icon:"img/pet/than_thu_huyet.png")}).ToArray();
}
