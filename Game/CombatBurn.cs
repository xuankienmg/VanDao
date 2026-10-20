namespace VanDao.Game;

/// <summary>Thiêu đốt không cộng dồn; kích hoạt lại làm mới hai lượt.</summary>
public sealed class CombatBurn
{
    public const int Percent = 5;
    public const int Duration = 2;
    public int TurnsLeft { get; private set; }
    public int Tick(int maxHp, bool triggered)
    {
        if (triggered) TurnsLeft = Duration;
        if (TurnsLeft <= 0) return 0;
        TurnsLeft--;
        return Math.Max(1, (int)Math.Round(maxHp * (Percent / 100.0)));
    }
    public void Reset() => TurnsLeft = 0;
}
