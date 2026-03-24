namespace Celeste.Mod.AutoSaver;

public class AutoSaverSaveData : EverestModuleSaveData {
    public Dictionary<string, Model.RoomRegionMask> Rooms { get; set; } = new();
}
