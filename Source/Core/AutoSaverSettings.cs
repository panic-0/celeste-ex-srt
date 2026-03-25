namespace Celeste.Mod.AutoSaver;

[SettingName("AutoSaver")]
public class AutoSaverSettings : EverestModuleSettings {
    public bool Enabled { get; set; } = true;
    public int TriggerEnterCountK { get; set; } = 1;
    public bool DisableSrtFreezeOnAutoSave { get; set; } = true;
    public bool ShowOverlayInLevel { get; set; } = true;
    public bool ShowLookoutEditOverlay { get; set; } = true;
    public bool ShowPopupOnAutoSave { get; set; } = true;
    public ButtonBinding ToggleLookoutEdit { get; set; } = new();
    public ButtonBinding ToggleLevelOverlay { get; set; } = new();
    public ButtonBinding ClearCurrentRoomMarkers { get; set; } = new();
}
