using Celeste.Mod.ExSrt.Interop;

namespace Celeste.Mod.ExSrt;

public class ExSrtModule : EverestModule {
    public static ExSrtModule Instance { get; private set; } = null!;

    public static ExSrtSettings Settings => (ExSrtSettings) Instance._Settings!;
    public static ExSrtSaveData SaveData => (ExSrtSaveData) Instance._SaveData!;
    public static ExSrtSession Session => (ExSrtSession) Instance._Session!;

    public ExSrtModule() {
        Instance = this;
    }

    public override Type SettingsType => typeof(ExSrtSettings);
    public override Type SaveDataType => typeof(ExSrtSaveData);
    public override Type SessionType => typeof(ExSrtSession);

    public override void Load() {
        SpeedrunToolInterop.Load();
        HotkeyHelper.Load();
        RegionEditorController.Load();
        LookoutEditController.Load();
        RegionTriggerController.Load();
    }

    public override void Unload() {
        RegionTriggerController.Unload();
        LookoutEditController.Unload();
        RegionEditorController.Unload();
        HotkeyHelper.Unload();
        SpeedrunToolInterop.Unload();
    }

    public override void CreateModMenuSection(TextMenu menu, bool inGame, FMOD.Studio.EventInstance snapshot) {
        CreateModMenuSectionHeader(menu, inGame, snapshot);
        ExSrtMenu.Create(menu, inGame);
        CreateModMenuSectionKeyBindings(menu, inGame, snapshot);
    }
}
