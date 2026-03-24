using Celeste.Mod.AutoSaver.Interop;

namespace Celeste.Mod.AutoSaver;

public class AutoSaverModule : EverestModule {
    public static AutoSaverModule Instance { get; private set; } = null!;

    public static AutoSaverSettings Settings => (AutoSaverSettings) Instance._Settings!;
    public static AutoSaverSaveData SaveData => (AutoSaverSaveData) Instance._SaveData!;
    public static AutoSaverSession Session => (AutoSaverSession) Instance._Session!;

    public AutoSaverModule() {
        Instance = this;
    }

    public override Type SettingsType => typeof(AutoSaverSettings);
    public override Type SaveDataType => typeof(AutoSaverSaveData);
    public override Type SessionType => typeof(AutoSaverSession);

    public override void Load() {
        Logger.Log("AutoSaver", $"Module Load start, version={Metadata.VersionString}");
        SpeedrunToolInterop.Load();
        HotkeyHelper.Load();
        RegionEditorController.Load();
        RegionTriggerController.Load();
        Logger.Log("AutoSaver", "Module Load complete");
    }

    public override void Unload() {
        Logger.Log("AutoSaver", "Module Unload");
        RegionTriggerController.Unload();
        RegionEditorController.Unload();
        HotkeyHelper.Unload();
        SpeedrunToolInterop.Unload();
    }

    public override void CreateModMenuSection(TextMenu menu, bool inGame, FMOD.Studio.EventInstance snapshot) {
        CreateModMenuSectionHeader(menu, inGame, snapshot);
        AutoSaverMenu.Create(menu, inGame);
        CreateModMenuSectionKeyBindings(menu, inGame, snapshot);
    }
}
