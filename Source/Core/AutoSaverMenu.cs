namespace Celeste.Mod.ExSrt;

public static class ExSrtMenu {
    public static void Create(TextMenu menu, bool inGame) {
        menu.Add(new TextMenu.OnOff("Enabled", ExSrtModule.Settings.Enabled).Change(value => {
            ExSrtModule.Settings.Enabled = value;
        }));
        menu.Add(new TextMenu.Slider("Trigger Enter Count", value => value.ToString(), 1, 20, ExSrtModule.Settings.TriggerEnterCountK).Change(value => {
            ExSrtModule.Settings.TriggerEnterCountK = value;
        }));
        menu.Add(new TextMenu.OnOff("Disable SRT Freeze On Auto-Save", ExSrtModule.Settings.DisableSrtFreezeOnAutoSave).Change(value => {
            ExSrtModule.Settings.DisableSrtFreezeOnAutoSave = value;
        }));
        menu.Add(new TextMenu.OnOff("Show Overlay In Level", ExSrtModule.Settings.ShowOverlayInLevel).Change(value => {
            ExSrtModule.Settings.ShowOverlayInLevel = value;
        }));
        menu.Add(new TextMenu.OnOff("Show Lookout Edit Overlay", ExSrtModule.Settings.ShowLookoutEditOverlay).Change(value => {
            ExSrtModule.Settings.ShowLookoutEditOverlay = value;
        }));
        menu.Add(new TextMenu.OnOff("Show Popup On Auto Save", ExSrtModule.Settings.ShowPopupOnAutoSave).Change(value => {
            ExSrtModule.Settings.ShowPopupOnAutoSave = value;
        }));
    }
}
