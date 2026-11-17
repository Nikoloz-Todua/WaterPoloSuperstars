using ToduStudios.WaterPolo.Editor;
public static class ArenaPremiumAuthoring
{
    public static object Apply() { ChampionshipArenaRebuild.PremiumFinish(); return new { saved = true }; }
    public static object Reflection() { ChampionshipArenaRebuild.RefreshReflection(); return new { saved = true }; }
    public static object Review() { ChampionshipArenaRebuild.ReviewPremiumLanding(); return new { saved = true }; }
}
