namespace Celeste.Mod.AutoSaver;

[SettingName("AutoSaver")]
public class AutoSaverSettings : EverestModuleSettings {
    public bool Enabled { get; set; } = true;
    public int TriggerEnterCountK { get; set; } = 1;
    public bool ShowOverlayInLevel { get; set; } = true;
    public bool ShowPopupOnAutoSave { get; set; } = true;
    public ButtonBinding ToggleLevelOverlay { get; set; } = new();
    public List<Keys> KeyboardClearMarkedRegions { get; set; } = new() { Keys.LeftControl, Keys.Back };
    public List<Buttons> ControllerClearMarkedRegions { get; set; } = new();
}
