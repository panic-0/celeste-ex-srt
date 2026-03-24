namespace Celeste.Mod.AutoSaver.Model;

public readonly record struct RoomKey(string Sid, AreaMode Mode, string RoomName) {
    public override string ToString() => $"{Sid}|{Mode}|{RoomName}";

    public static RoomKey From(Level level) => new(level.Session.Area.GetSID(), level.Session.Area.Mode, level.Session.Level);
    public static RoomKey From(MapEditor editor, LevelTemplate template) {
        if (!MapEditorHelper.TryGetEditingArea(editor, out AreaKey area)) {
            return new RoomKey("UnknownArea", AreaMode.Normal, template.Name);
        }

        return new RoomKey(area.GetSID(), area.Mode, template.Name);
    }
}
