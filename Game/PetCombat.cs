namespace VanDao.Game;

/// <summary>Trạng thái kỹ năng linh thú trong một trận; hiệu ứng cùng loại chỉ làm mới thời gian.</summary>
public sealed class PetCombat(PetDef definition, int form, CombatStats owner, int stars = 0)
{
    private readonly int _form = Math.Clamp(form, 0, 2);
    private readonly double _starScale = PetData.StarScale(stars);
    private double TalentValue => definition.TalentValue * _starScale;
    private int _turn, _markUntil = -1, _downUntil = -1, _dodgeUntil = -1, _reflectUntil = -1;
    private int _burnLeft, _hotLeft;
    public int Shield { get; private set; }
    public bool Burning => _burnLeft > 0;
    private double Tier(double a, double b, double c) => (_form == 0 ? a : _form == 1 ? b : c) * _starScale;
    private string Name => definition.Forms[_form].SkillName;
    public double AttackDown => _turn <= _downUntil ? Tier(5, 8, 12) : 0;
    public double DodgeBonus => _turn <= _dodgeUntil ? Tier(5, 8, 12) : 0;
    public double GuardBonus => definition.Skill == "turtle" && Shield > 0 ? Tier(3, 5, 7) : 0;
    public int InitialShield()
    {
        Shield = definition.Talent == "shield" ? (int)(owner.Hp * TalentValue / 100) : 0;
        return Shield;
    }
    public void Absorb(int damage) => Shield = Math.Max(0, Shield - damage);
    public int Healing(int requested, int hp)
    {
        if (hp <= 0) return 0;
        var bonus = definition.Talent == "healing" && hp < owner.Hp * .5 ? 1 + TalentValue / 100 : 1;
        return Math.Clamp((int)Math.Round(requested * bonus), 0, Math.Max(0, owner.Hp - hp));
    }
    public int AmplifyOwner(int damage, bool critical, bool targetBurning)
    {
        var scale = critical && _turn <= _markUntil ? 1 + Tier(5, 8, 12) / 100 : 1;
        if (definition.Talent == "burn_damage" && targetBurning) scale *= 1 + TalentValue / 100;
        return (int)Math.Round(damage * scale);
    }
    private int Hit(double percent, int defense, double takeK, double pen = 0) =>
        Math.Max(1, (int)Math.Round(Math.Max(1, owner.Atk * percent / 100 - Math.Floor(defense * (1 - pen / 100))) * takeK));
    private int AddShield(double percent)
    {
        var old = Shield;
        Shield = Math.Min((int)(owner.Hp * .30), Shield + (int)(owner.Hp * percent / 100));
        return Shield - old;
    }
    public PetCastDto? BeginTurn(int turn, ref int hp, int monsterHp, int defense, double takeK)
    {
        _turn = turn;
        if (_burnLeft > 0)
        {
            _burnLeft--;
            var damage = Math.Min(monsterHp, Hit(Tier(15, 25, 35), defense, takeK));
            return new(turn, damage, 0, 0, Name + " · Thiêu đốt", Kind: "burn");
        }
        if (_hotLeft > 0)
        {
            _hotLeft--;
            var heal = Healing((int)(owner.Hp * Tier(1, 1.5, 2) / 100), hp); hp += heal;
            return new(turn, 0, heal, 0, Name + " · Hồi phục", Kind: "regen");
        }
        return null;
    }
    public PetCastDto Cast(int turn, ref int hp, int monsterHp, int monsterMaxHp, int defense, double takeK)
    {
        _turn = turn;
        var damage = 0; var heal = 0; var guard = 0; var hits = new List<int>();
        void Strike(double percent, double pen = 0)
        {
            var hit = Math.Min(Math.Max(0, monsterHp - damage), Hit(percent, defense, takeK, pen));
            hits.Add(hit); damage += hit;
        }
        switch (definition.Skill)
        {
            case "fox": Strike(Tier(70, 105, 140)); _markUntil = turn + 2; break;
            case "frost": guard = AddShield(Tier(6, 9, 12)); _downUntil = turn + 1; break;
            case "deer": heal = Healing((int)(owner.Hp * Tier(5, 8, 11) / 100), hp); _hotLeft = 2; break;
            case "turtle": guard = AddShield(Tier(8, 12, 16)); break;
            case "eagle": for (var i = 0; i < 3; i++) Strike(Tier(25, 38, 52)); break;
            case "leech":
                Strike(Tier(65, 100, 135));
                heal = Healing(Math.Min((int)(damage * Tier(20, 25, 30) / 100), (int)(owner.Hp * Tier(4, 6, 8) / 100)), hp);
                break;
            case "crane": heal = Healing((int)(owner.Hp * Tier(4, 6, 8) / 100), hp); _dodgeUntil = turn + 1; break;
            case "cat": Strike(Tier(90, 135, 180), Tier(15, 25, 35)); break;
            case "phoenix": Strike(Tier(80, 120, 160)); _burnLeft = 2; break;
            case "xuanwu": guard = AddShield(Tier(10, 15, 20)); _reflectUntil = turn + 1; break;
            case "dragon": for (var i = 0; i < 3; i++) Strike(Tier(35, 55, 75), _form == 2 && i == 2 ? Tier(20,20,20) : 0); break;
            case "tiger": Strike(Tier(110, 165, 220) * (monsterHp < monsterMaxHp * .30 ? 1 + Tier(25,25,25)/100 : 1)); break;
        }
        hp += heal;
        return new(turn, damage, heal, guard, Name, hits.Count > 0 ? hits.ToArray() : null, definition.Skill);
    }
    public PetCastDto? Reflect(int turn, int monsterHp, int defense, double takeK)
    {
        if (turn > _reflectUntil || monsterHp <= 0) return null;
        return new(turn, Math.Min(monsterHp, Hit(Tier(15, 25, 35), defense, takeK)), 0, 0, Name + " · Phản đòn", Kind: "reflect");
    }
    public void NextWave() { _burnLeft = 0; _markUntil = _downUntil = -1; }
}
