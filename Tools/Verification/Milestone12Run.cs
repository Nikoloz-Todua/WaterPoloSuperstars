using System.Threading.Tasks;
using ToduStudios.WaterPolo.Editor;
public static class Milestone12Run
{
    public static Task<object> Navigation()=>HubVerification.NavigationAndPointer();
    public static Task<object> Layouts()=>HubVerification.ActualLayouts();
    public static Task<object> MatchLoop()=>HubVerification.MatchLoop();
}
