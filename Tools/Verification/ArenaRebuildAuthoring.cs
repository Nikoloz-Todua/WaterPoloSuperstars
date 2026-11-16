using ToduStudios.WaterPolo.Editor;
public static class ArenaRebuildAuthoring
{
 public static object Build(){ChampionshipArenaRebuild.SetUp();return new{saved=true};}
 public static object Review(){ChampionshipArenaRebuild.ApplyReviewFixes();return new{saved=true};}
 public static object Reflection(){ChampionshipArenaRebuild.RefreshReflection();return new{saved=true};}
}
