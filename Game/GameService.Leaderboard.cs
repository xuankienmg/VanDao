using VanDao.Data;

namespace VanDao.Game;

public partial class GameService
{
    // Highest completed stage, ordered by difficulty, chapter, then stage.
    // AreaMax is the next unlocked stage, not a completed stage.
    public static int LeaderboardProgress(Player p)
    {
        var best = 0;
        for (var d = 0; d < GameData.DiffNames.Length; d++)
        for (var c = 0; c < GameData.OpenChapters; c++)
        {
            var cleared = IsCleared(p, d, c);
            var unlocked = d == 0 ? (c < p.ChapterMax ? GameData.Areas : c == p.ChapterMax ? p.AreaMax : 1)
                : DiffProgMap(p).GetValueOrDefault($"{d}.{c}", 1);
            var completed = cleared ? GameData.Areas : Math.Clamp(unlocked - 1, 0, GameData.Areas - 1);
            if (completed > 0) best = Math.Max(best, d * 10000 + c * 100 + completed);
        }
        return best;
    }

    public static long LeaderboardPetPower(Player p) => p.Pets
        .Where(x => PetData.ById.ContainsKey(x.DefId))
        .Select(x => { var s = PetStats(p, x); return (long)Math.Round(s.Atk * 2.0 + s.Def * 3.0 + s.Hp * .4 + (s.Crit + s.Speed) * 4); })
        .DefaultIfEmpty(0).Max();
}
