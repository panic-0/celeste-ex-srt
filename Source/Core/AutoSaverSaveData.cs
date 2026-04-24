namespace Celeste.Mod.ExSrt;

public class ExSrtSaveData : EverestModuleSaveData {
    public Dictionary<string, Model.RoomRegionMask> Rooms { get; set; } = new();
}
