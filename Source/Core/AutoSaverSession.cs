namespace Celeste.Mod.AutoSaver;

public class AutoSaverSession : EverestModuleSession {
    public string LastAutoSaveMessage { get; set; } = "";
    public Dictionary<string, Dictionary<int, int>> EnterCounts { get; set; } = new();
}
