namespace Celeste.Mod.AutoSaver;

public static class MapEditorHelper {
    private static readonly HashSet<string> LoggedFailures = new();

    public static bool TryGetEditingArea(MapEditor editor, out AreaKey area) {
        if (RegionEditorController.TryGetArmedArea(out area)) {
            return true;
        }

        return TryGetArea(editor, out area);
    }

    public static bool TryGetArea(MapEditor editor, out AreaKey area) {
        try {
            area = DynamicData.For(editor).Get<AreaKey>("area");
            return true;
        }
        catch (Exception ex) {
            LogFailure("read-area", ex);
            area = default;
            return false;
        }
    }

    public static bool TryGetMousePosition(MapEditor editor, out Vector2 mousePosition) {
        try {
            mousePosition = DynamicData.For(editor).Get<Vector2>("mousePosition");
            return true;
        }
        catch (Exception ex) {
            LogFailure("read-mouse-position", ex);
            mousePosition = Vector2.Zero;
            return false;
        }
    }

    public static bool TryGetHoveredRoom(MapEditor editor, out LevelTemplate? template) {
        template = null;
        try {
            DynamicData data = DynamicData.For(editor);
            Vector2 mousePosition = data.Get<Vector2>("mousePosition");
            template = data.Invoke<LevelTemplate?>("TestCheck", mousePosition);
            return template != null;
        }
        catch (Exception ex) {
            LogFailure("resolve-hovered-room", ex);
            return false;
        }
    }

    public static bool TryGetCurrentRoom(MapEditor editor, out LevelTemplate? template) {
        template = null;
        try {
            DynamicData data = DynamicData.For(editor);
            if (data.TryGet("selection", out HashSet<LevelTemplate>? selection) &&
                selection != null) {
                template = selection.FirstOrDefault(level => level.Type != LevelTemplateType.Filler);
                if (template != null) {
                    return true;
                }
            }

            if (data.TryGet("CurrentSession", out Session? currentSession) &&
                currentSession != null &&
                data.TryGet("levels", out List<LevelTemplate>? levels) &&
                levels != null) {
                template = levels.FirstOrDefault(level =>
                    level.Type != LevelTemplateType.Filler &&
                    string.Equals(level.Name, currentSession.Level, StringComparison.Ordinal));
                if (template != null) {
                    return true;
                }
            }

            return TryGetHoveredRoom(editor, out template);
        }
        catch (Exception ex) {
            LogFailure("resolve-current-room", ex);
            return false;
        }
    }

    private static void LogFailure(string operation, Exception ex) {
        string key = $"{operation}:{ex.GetType().FullName}:{ex.Message}";
        if (LoggedFailures.Add(key)) {
            Logger.Log("AutoSaver", $"MapEditorHelper failed to {operation}: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
