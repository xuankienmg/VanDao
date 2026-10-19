// Mô phỏng cân bằng: người chơi mặc đủ bộ trang bị cảnh giới của chương (phẩm Linh/Huyền/Địa), tầng đầu / tầng cuối, chỉ đòn thường.
// base.txt = kết quả trước khi thu nhỏ chỉ số trang bị (10/2026). Chạy: dotnet run -c Release -- [check|tune]
using VanDao.Game; using VanDao.Data;
int[] chRealm = {0,1,2,2,3,3,4,4,5};
int[] areas = {1,5,8,10,12,14,15,16};
var basel = File.ReadAllLines("base.txt").Select(l=>l.Split(" | ")).ToDictionary(x=>x[0], x=>x[1].Split(' ').Select(t=>(win:t.Split(':')[1][0]=='W', turns:int.Parse(t.Split(':')[1][1..]))).ToArray());
(bool win,int turns)[] Run(int c,int stage,int q){
  var ri = chRealm[c]; var r = GameData.Realms[ri];
  var p = new Player{ Realm = ri, Stage = Math.Min(stage, r.Stages.Length-1), Chapter = c, ChapterMax = c, Area = 1, AreaMax = 15 };
  foreach (var b in GameData.GearBases.Where(g => g.MinRealm == ri && r.EquipSlots.Contains(g.Slot!)).GroupBy(g=>g.Slot).Select(g=>g.First()))
    p.Equipment.Add(new EquippedItem{ PlayerId=p.Id, Slot=b.Slot!, ItemId=GameData.GearId(b, q)});
  return areas.Select(a=>{ var bt = GameService.BattleAt(p, a, DateTime.UnixEpoch); return (bt.Win, GameService.LastTurns); }).ToArray();
}
double Err(int c){
  double e=0;
  foreach (var s in new[]{0,9}) foreach (var q in new[]{1,2,3}){
    var key=$"ch{c+1} s{Math.Min(s,GameData.Realms[chRealm[c]].Stages.Length-1)} q{q}"; var bl=basel[key]; var cur=Run(c,s,q); var w = q==2?3.0:1.0;
    for(int i=0;i<areas.Length;i++){ if (bl[i].win!=cur[i].win) e+=10*w; else e+= w*Math.Abs(bl[i].turns-cur[i].turns)/(double)Math.Max(3,bl[i].turns); }
  }
  return e;
}
var mode = args.Length>0?args[0]:"check";   // check: in kết quả các ải · tune: dò lại ChapterMonHp / ChapterMonAtk khớp base.txt
for (int c=0;c<9;c++){
  if (mode=="tune"){
    double best=1e9, bh=1, ba=1;
    for (double h=.30; h<=1.0001; h+=.04) for (double a=.30; a<=1.0001; a+=.04){ GameData.ChapterMonHp[c]=h; GameData.ChapterMonAtk[c]=a; var e=Err(c); if(e<best){best=e;bh=h;ba=a;} }
    double best2=best; double h0=bh,a0=ba;
    for (double h=h0-.04; h<=h0+.0401; h+=.01) for (double a=a0-.04; a<=a0+.0401; a+=.01){ GameData.ChapterMonHp[c]=h; GameData.ChapterMonAtk[c]=a; var e=Err(c); if(e<best2){best2=e;bh=h;ba=a;} }
    GameData.ChapterMonHp[c]=bh; GameData.ChapterMonAtk[c]=ba;
    Console.WriteLine($"ch{c+1} hp/def {bh:0.00} atk {ba:0.00} err {best2:0.0}");
  }
}
Console.WriteLine("HP  = { " + string.Join(", ", GameData.ChapterMonHp.Select(x=>x.ToString("0.00"))) + " }");
Console.WriteLine("ATK = { " + string.Join(", ", GameData.ChapterMonAtk.Select(x=>x.ToString("0.00"))) + " }");
for (int c=0;c<9;c++) foreach (var s in new[]{0,9}) foreach (var q in new[]{1,2,3}){ var key=$"ch{c+1} s{Math.Min(s,GameData.Realms[chRealm[c]].Stages.Length-1)} q{q}"; var cur=Run(c,s,q); Console.WriteLine(key+" | "+string.Join(" ", areas.Select((a,i)=>$"{a}:{(cur[i].win?"W":"L")}{cur[i].turns}")) + "   (cũ: " + string.Join(" ", basel[key].Select((b,i)=>$"{(b.win?"W":"L")}{b.turns}"))+")"); }
