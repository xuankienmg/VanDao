using VanDao.Data;
namespace VanDao.Game;

public partial class GameService
{
    private static int PetLevelCap(Player p) => Player.AdminEnabled && p.Tainted ? 100 : PetData.LevelCap(p.Realm);
    private static Pet FindPet(Player p,Guid id) => p.Pets.FirstOrDefault(x=>x.Id==id) ?? throw new GameException("Không sở hữu linh thú này.");
    private static List<(int Slot,string Egg,DateTime Ready)> Hatches(Player p) => p.Hatch.Split(';',StringSplitOptions.RemoveEmptyEntries).Select(x=>x.Split('|'))
        .Where(x=>x.Length==3 && int.TryParse(x[0],out _) && PetData.EggIds.Contains(x[1]) && long.TryParse(x[2],out var sec) && sec>=0 && sec<=253402300799)
        .Select(x=>(int.Parse(x[0]),x[1],DateTimeOffset.FromUnixTimeSeconds(long.Parse(x[2])).UtcDateTime)).ToList();
    private static void SaveHatches(Player p,List<(int Slot,string Egg,DateTime Ready)> h) => p.Hatch=string.Join(';',h.Select(x=>$"{x.Slot}|{x.Egg}|{new DateTimeOffset(x.Ready).ToUnixTimeSeconds()}"));
    private static int InstantCost(DateTime ready) => Math.Max(0,(int)Math.Ceiling((ready-DateTime.UtcNow).TotalSeconds/60))*100;
    public (string,string) Hatch(Player p,string egg,int slot=0)
    {
        var q=Array.IndexOf(PetData.EggIds,egg);var h=Hatches(p);
        if(q<0 || slot<0 || slot>=PetData.HatchSlots(p.Realm)) throw new GameException("Trứng hoặc ổ ấp không hợp lệ.");
        if(h.Any(x=>x.Slot==slot)) throw new GameException("Ổ ấp đang bận.");
        if(p.Pets.Count+h.Count>=PetData.Capacity(p.Realm)) throw new GameException("Chuồng thú đã đầy.");
        if(p.Count(egg)<1) throw new GameException("Không có trứng.");
        p.Add(egg,-1);h.Add((slot,egg,DateTime.UtcNow.AddSeconds(PetData.HatchSeconds[q])));SaveHatches(p,h);
        return ("Đã bắt đầu ấp trứng.","good");
    }
    public (string,string) HatchNow(Player p,int slot=0)
    {
        var h=Hatches(p);var i=h.FindIndex(x=>x.Slot==slot);if(i<0)throw new GameException("Ổ ấp trống.");
        var cost=InstantCost(h[i].Ready);if(p.Count("linh_thach")<cost)throw new GameException("Không đủ linh thạch.");
        p.Add("linh_thach",-cost);h[i]=(slot,h[i].Egg,DateTime.UtcNow);SaveHatches(p,h);return ("Trứng đã sẵn sàng nở.","good");
    }
    public (string,string) CollectHatch(Player p,int slot=0)
    {
        var h=Hatches(p);var i=h.FindIndex(x=>x.Slot==slot);if(i<0||h[i].Ready>DateTime.UtcNow)throw new GameException("Trứng chưa sẵn sàng.");
        if(p.Pets.Count>=PetData.Capacity(p.Realm))throw new GameException("Chuồng thú đã đầy.");
        var q=Array.IndexOf(PetData.EggIds,h[i].Egg);var pool=PetData.Pool(q);var d=pool[Random.Shared.Next(pool.Length)];var lo=q==4?80:60;
        var pet=new Pet{PlayerId=p.Id,DefId=d.Id,AptAtk=Random.Shared.Next(lo,101),AptDef=Random.Shared.Next(lo,101),AptHp=Random.Shared.Next(lo,101)};
        p.Pets.Add(pet);db.Pets.Add(pet);
        h.RemoveAt(i);SaveHatches(p,h);return ($"Linh thú {d.Name} đã nở!","gold");
    }
    public (string,string) Feed(Player p,Guid id,int n=1)
    {
        var pet=FindPet(p,id);if(n!=0&&n!=1&&n!=10)throw new GameException("Chọn 1, 10 hoặc tối đa.");
        var cap=PetLevelCap(p);if(pet.Level>=cap)throw new GameException("Đã đạt giới hạn cấp cảnh giới.");
        var have=p.Count("linh_thu_dan");if(have<1)throw new GameException("Không có Linh Thú Đan.");
        var limit=n==0?have:Math.Min(n,have);var used=0;
        while(used<limit&&pet.Level<cap){used++;pet.Exp+=PetData.FoodExp;while(pet.Level<cap&&pet.Exp>=PetData.ExpNeed(pet.Level)){pet.Exp-=PetData.ExpNeed(pet.Level);pet.Level++;}}
        if(pet.Level>=cap)pet.Exp=0;
        pet.FoodSpent+=used;p.Add("linh_thu_dan",-used);return ($"Đã cho ăn {used} Linh Thú Đan.","good");
    }
    private static bool EvolveGate(Player p,Pet pet) => pet.Form<2 && pet.Level >= (pet.Form==0?30:60) && (pet.Form==0||p.Realm>=3);
    public (string,string) Evolve(Player p,Guid id)
    {
        var pet=FindPet(p,id);var cost=PetData.EvolveCost(pet.Form);
        if(!EvolveGate(p,pet))throw new GameException("Cần cấp 30 / 60; thần thú cần Nguyên Anh.");
        if(!p.Has(cost))throw new GameException("Thiếu nguyên liệu tiến hóa.");p.Pay(cost);pet.Form++;return ("Linh thú đã tiến hóa!","gold");
    }
    public (string,string) SetPetSlot(Player p,Guid id,int slot)
    {
        var pet=FindPet(p,id);if(slot<0||slot>1+PetData.GuardSlots(p.Realm))throw new GameException("Ô linh thú chưa mở.");
        if(slot>0)foreach(var other in p.Pets.Where(x=>x.Slot==slot))other.Slot=0;
        pet.Slot=slot;return ("Đã đổi vị trí linh thú.","good");
    }
    public (string,string) Release(Player p,Guid id)
    {
        var pet=FindPet(p,id);var refund=pet.FoodSpent/2;db.Pets.Remove(pet);p.Pets.Remove(pet);p.Add("linh_thu_dan",refund);return ($"Đã phóng sinh, hoàn {refund} Linh Thú Đan.","good");
    }
    public static CombatStats PetStats(Player p,Pet pet)
    {
        var d=PetData.ById[pet.DefId];var lv=Math.Min(pet.Level,PetLevelCap(p));
        var scale=PetData.StatScale(p.Realm,d);
        int Stat(int b,int apt)=> (int)Math.Round(b*(1+(lv-1)*.22*apt/100.0)*(1+pet.Form*.4)*scale);
        return new(Stat(d.Atk,pet.AptAtk),Stat(d.Hp,pet.AptHp),Stat(d.Def,pet.AptDef));
    }
    private static double PetWeight(Player p,Pet pet) => pet.Slot==1?1:pet.Slot>=2&&pet.Slot<=1+PetData.GuardSlots(p.Realm)?.3:0;
    public static double PetTalent(Player p,string talent) => p.Pets.Where(x=>PetData.ById.ContainsKey(x.DefId)).Sum(x=>PetData.ById[x.DefId].Talent==talent?PetData.ById[x.DefId].TalentValue*PetWeight(p,x):0);
    public static CombatStats PetBonus(Player p)
    {
        double a=0,d=0,h=0;foreach(var pet in p.Pets.Where(x=>PetData.ById.ContainsKey(x.DefId))){var st=PetStats(p,pet);var k=PetData.ById[pet.DefId].Forms[Math.Clamp(pet.Form,0,2)].ShareMult*PetWeight(p,pet);a+=Math.Floor(st.Atk*k);d+=Math.Floor(st.Def*k);h+=Math.Floor(st.Hp*k);}
        return new((int)a,(int)h,(int)d,PetTalent(p,"speed"),PetTalent(p,"crit"));
    }
    public static PetState PetStateFor(Player p) => new(p.Pets.Where(x=>PetData.ById.ContainsKey(x.DefId)).Select(pet=>{
        var d=PetData.ById[pet.DefId];var st=PetStats(p,pet);var form=d.Forms[Math.Clamp(pet.Form,0,2)];var k=form.ShareMult*PetWeight(p,pet);var cost=PetData.EvolveCost(pet.Form);
        return new PetDto(pet.Id,d.Id,d.Name,d.Element,d.Rarity,pet.Level,pet.Exp,PetData.ExpNeed(pet.Level),pet.Form,form.Name,pet.Slot,st.Atk,st.Def,st.Hp,pet.AptAtk,pet.AptDef,pet.AptHp,(int)(st.Atk*k),(int)(st.Def*k),(int)(st.Hp*k),d.TalentText,d.SkillText,d.SkillEvery,cost.Select(x=>new CostDto(x.Key,x.Value,p.Count(x.Key))).ToArray(),EvolveGate(p,pet)&&p.Has(cost),form.Image);
    }).ToList(),Hatches(p).Select(h=>new HatchDto(h.Slot,h.Egg,h.Ready,Math.Max(0,(int)Math.Ceiling((h.Ready-DateTime.UtcNow).TotalSeconds)),h.Ready<=DateTime.UtcNow,InstantCost(h.Ready))).ToList(),PetData.Capacity(p.Realm),PetLevelCap(p),PetData.GuardSlots(p.Realm),PetData.HatchSlots(p.Realm));
    public (string msg,string kind,CraftResultDto craft) PetSpin(Player p,int n)
    {
        n=Math.Clamp(n,1,10);if(p.Count(GameData.SpinTicket)<n)throw new GameException("Không đủ vé quay.");
        var outs=Enumerable.Range(0,n).Select(_=>PetData.EggIds[GameData.RollQuality(PetData.PetSpinWeights)]).ToArray();
        var need=outs.Distinct().Count(x=>p.Count(x)==0);if(BagCap(p)-BagUsed(p)+(p.Count(GameData.SpinTicket)==n?1:0)<need)throw new GameException("Hành trang đầy.");
        p.Add(GameData.SpinTicket,-n);foreach(var egg in outs)p.Add(egg,1);
        p.SpinHistory=string.Join(';',p.SpinHistory.Split(';',StringSplitOptions.RemoveEmptyEntries).Prepend($"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}|{string.Join(',',outs)}").Take(GameData.SpinHistoryKeep));
        var got=outs.GroupBy(x=>x).Select(g=>new ItemQtyDto(g.Key,g.Count())).ToList();return ("Đã quay Linh Thú.","good",new("spin:pet",got[0],new(){new(GameData.SpinTicket,n)},got));
    }
}
