namespace Celeste.Mod.ExSrt;

public class ExSrtSession : EverestModuleSession {
    public string LastAutoSaveMessage { get; set; } = "";
    public Dictionary<string, Dictionary<int, int>> EnterCounts { get; set; } = new();
}
