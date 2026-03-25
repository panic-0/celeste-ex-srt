namespace Celeste.Mod.AutoSaver;

public static class AutoSaverMenu {
    public static void Create(TextMenu menu, bool inGame) {
        menu.Add(new TextMenu.OnOff("Enabled", AutoSaverModule.Settings.Enabled).Change(value => {
            AutoSaverModule.Settings.Enabled = value;
        }));
        menu.Add(new TextMenu.Slider("Trigger Enter Count", value => value.ToString(), 1, 20, AutoSaverModule.Settings.TriggerEnterCountK).Change(value => {
            AutoSaverModule.Settings.TriggerEnterCountK = value;
        }));
        menu.Add(new TextMenu.OnOff("Disable SRT Freeze On Auto-Save", AutoSaverModule.Settings.DisableSrtFreezeOnAutoSave).Change(value => {
            AutoSaverModule.Settings.DisableSrtFreezeOnAutoSave = value;
        }));
        menu.Add(new TextMenu.OnOff("Show Overlay In Level", AutoSaverModule.Settings.ShowOverlayInLevel).Change(value => {
            AutoSaverModule.Settings.ShowOverlayInLevel = value;
        }));
        menu.Add(new TextMenu.OnOff("Show Lookout Edit Overlay", AutoSaverModule.Settings.ShowLookoutEditOverlay).Change(value => {
            AutoSaverModule.Settings.ShowLookoutEditOverlay = value;
        }));
        menu.Add(new TextMenu.OnOff("Show Popup On Auto Save", AutoSaverModule.Settings.ShowPopupOnAutoSave).Change(value => {
            AutoSaverModule.Settings.ShowPopupOnAutoSave = value;
        }));
    }
}
