namespace VanDao.Game;

/// <summary>Né là sự kiện thật, không phải hệ số giảm sát thương kỳ vọng.</summary>
public static class CombatDodge
{
    public const double MaxChance = 50;
    public static double Chance(double speed, double gear, double skill) =>
        Math.Clamp(speed + gear + skill, 0, MaxChance);
    public static bool Roll(Random rng, double chance) => chance > 0 && rng.NextDouble() * 100 < Math.Min(MaxChance, chance);
}
