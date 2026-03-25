using Celeste.Mod.AutoSaver.Model;

namespace Celeste.Mod.AutoSaver;

public static class RegionStorage {
    public static RoomRegionMask GetOrCreate(RoomKey key, Rectangle bounds) {
        if (!AutoSaverModule.SaveData.Rooms.TryGetValue(key.ToString(), out RoomRegionMask? mask)) {
            mask = RoomRegionMask.Create(bounds);
            AutoSaverModule.SaveData.Rooms[key.ToString()] = mask;
        }

        int widthCells = Math.Max(1, (int) Math.Ceiling(bounds.Width / (float) RoomRegionMask.CellSize));
        int heightCells = Math.Max(1, (int) Math.Ceiling(bounds.Height / (float) RoomRegionMask.CellSize));
        if (mask.OriginX != bounds.X || mask.OriginY != bounds.Y || mask.WidthCells != widthCells || mask.HeightCells != heightCells) {
            mask = RoomRegionMask.Create(bounds);
            AutoSaverModule.SaveData.Rooms[key.ToString()] = mask;
        }

        return mask.WithInitializedRows();
    }

    public static RoomRegionMask? TryGet(RoomKey key) {
        return AutoSaverModule.SaveData.Rooms.TryGetValue(key.ToString(), out RoomRegionMask? mask) ? mask.WithInitializedRows() : null;
    }

    public static bool ClearRoom(RoomKey key) {
        bool removed = AutoSaverModule.SaveData.Rooms.Remove(key.ToString());
        AutoSaverModule.Session.EnterCounts.Remove(key.ToString());
        return removed;
    }

    public static int ClearChapter(string sid, AreaMode mode) {
        List<string> keys = AutoSaverModule.SaveData.Rooms.Keys.Where(key => key.StartsWith($"{sid}|{mode}|", StringComparison.Ordinal)).ToList();
        foreach (string key in keys) {
            AutoSaverModule.SaveData.Rooms.Remove(key);
            AutoSaverModule.Session.EnterCounts.Remove(key);
        }

        return keys.Count;
    }

    public static void ClearAll() {
        AutoSaverModule.SaveData.Rooms.Clear();
        AutoSaverModule.Session.EnterCounts.Clear();
        UI.Toast.Show(Engine.Scene, "Cleared all auto-save markers");
    }

    public static void TryClearCurrentRoom() {
        if (Engine.Scene is Level level) {
            RoomKey key = RoomKey.From(level);
            if (ClearRoom(key)) {
                UI.Toast.Show(level, $"Cleared markers for room [{key.RoomName}]");
            }
        }
        else if (Engine.Scene is MapEditor editor && MapEditorHelper.TryGetCurrentRoom(editor, out LevelTemplate? template) && template is { Type: not LevelTemplateType.Filler }) {
            RoomKey key = RoomKey.From(editor, template);
            if (ClearRoom(key)) {
                UI.Toast.Show(editor, $"Cleared markers for room [{key.RoomName}]");
            }
        }
    }

    public static void TryClearCurrentChapter() {
        if (Engine.Scene is Level level) {
            int cleared = ClearChapter(level.Session.Area.GetSID(), level.Session.Area.Mode);
            if (cleared > 0) {
                UI.Toast.Show(level, $"Cleared markers for {cleared} room(s)");
            }
        }
        else if (Engine.Scene is MapEditor editor) {
            if (MapEditorHelper.TryGetArea(editor, out AreaKey area)) {
                int cleared = ClearChapter(area.GetSID(), area.Mode);
                if (cleared > 0) {
                    UI.Toast.Show(editor, $"Cleared markers for {cleared} room(s)");
                }
            }
        }
    }
}
