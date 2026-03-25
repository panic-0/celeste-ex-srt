using Celeste.Mod.AutoSaver.Model;
namespace Celeste.Mod.AutoSaver;

public static class RegionEditorController {
    private static bool mapEditorArmed;
    private static AreaKey armedArea;
    private static readonly Color EditorFillColor = new(255, 145, 60, 110);
    private static bool PaintModifierDown => MInput.Keyboard.CurrentState.IsKeyDown(Keys.LeftAlt) || MInput.Keyboard.CurrentState.IsKeyDown(Keys.RightAlt);

    public static void Load() {
        On.Monocle.Engine.Update += OnEngineUpdate;
        On.Celeste.Editor.MapEditor.Update += OnMapEditorUpdate;
        On.Celeste.Editor.MapEditor.Render += OnMapEditorRender;
        On.Celeste.Editor.LevelTemplate.RenderHighlight += OnLevelTemplateRenderHighlight;
    }

    public static void Unload() {
        On.Celeste.Editor.LevelTemplate.RenderHighlight -= OnLevelTemplateRenderHighlight;
        On.Celeste.Editor.MapEditor.Render -= OnMapEditorRender;
        On.Celeste.Editor.MapEditor.Update -= OnMapEditorUpdate;
        On.Monocle.Engine.Update -= OnEngineUpdate;
    }

    public static bool TryGetArmedArea(out AreaKey area) {
        area = armedArea;
        return mapEditorArmed;
    }

    private static void OnEngineUpdate(On.Monocle.Engine.orig_Update orig, Engine self, GameTime gameTime) {
        Scene? before = Engine.Scene;
        orig(self, gameTime);

        if (before is Level level && Engine.Scene is MapEditor) {
            mapEditorArmed = true;
            armedArea = level.Session.Area;
            Logger.Log("AutoSaver", $"MapEditor armed from level area={armedArea.GetSID()} mode={armedArea.Mode}");
            UI.Toast.Show(Engine.Scene, $"AutoSaver armed for {armedArea.GetSID()}");
        }
        else if (Engine.Scene is not MapEditor) {
            mapEditorArmed = false;
            armedArea = default;
        }
    }

    private static void OnMapEditorUpdate(On.Celeste.Editor.MapEditor.orig_Update orig, MapEditor self) {
        MouseState actualCurrent = MInput.Mouse.CurrentState;
        MouseState actualPrevious = MInput.Mouse.PreviousState;
        bool suppressRoomDrag = CanEdit(self) && PaintModifierDown;

        if (suppressRoomDrag) {
            MInput.Mouse.CurrentState = CreateSuppressedMouseState(actualCurrent);
            MInput.Mouse.PreviousState = CreateSuppressedMouseState(actualPrevious);
        }

        try {
            orig(self);
        }
        finally {
            if (suppressRoomDrag) {
                MInput.Mouse.CurrentState = actualCurrent;
                MInput.Mouse.PreviousState = actualPrevious;
            }
        }

        if (!CanEdit(self)) {
            return;
        }

        if (!MapEditorHelper.TryGetHoveredRoom(self, out LevelTemplate? hovered)) {
            return;
        }

        if (hovered == null || hovered.Type == LevelTemplateType.Filler) {
            return;
        }

        RoomKey key = RoomKey.From(self, hovered);
        Rectangle bounds = new((int) hovered.X, (int) hovered.Y, hovered.Width, hovered.Height);
        RoomRegionMask mask = RegionStorage.GetOrCreate(key, bounds);
        if (!MapEditorHelper.TryGetMousePosition(self, out Vector2 mousePosition)) {
            return;
        }

        Point cell = mask.WorldToCell(mousePosition);

        MouseState current = actualCurrent;
        bool changed = false;
        if (PaintModifierDown && current.LeftButton == ButtonState.Pressed) {
            changed = mask.SetCell(cell.X, cell.Y, true);
        }
        else if (PaintModifierDown && current.RightButton == ButtonState.Pressed) {
            changed = mask.SetCell(cell.X, cell.Y, false);
        }

        if (changed) {
            Logger.Log("AutoSaver", $"Edited room key [{key}] markedCells={mask.CountMarkedCells()} version={mask.Version}");
        }
    }

    private static void OnMapEditorRender(On.Celeste.Editor.MapEditor.orig_Render orig, MapEditor self) {
        orig(self);

        if (!CanEdit(self)) {
            return;
        }

        MapEditorHelper.TryGetHoveredRoom(self, out LevelTemplate? hovered);
        string text = hovered == null || hovered.Type == LevelTemplateType.Filler
            ? "AutoSaver: Alt+LMB paint, Alt+RMB erase"
            : $"AutoSaver: [{hovered.Name}] Alt+LMB paint | Alt+RMB erase | clear hotkey removes this room";

        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None,
            RasterizerState.CullNone, null, Engine.ScreenMatrix);
        Draw.Rect(8f, Engine.Height - 40f, ActiveFont.Measure(text).X * 0.7f + 18f, 28f, Color.Black * 0.72f);
        ActiveFont.DrawOutline(text, new Vector2(16f, Engine.Height - 34f), Vector2.Zero, Vector2.One * 0.7f, Color.White, 2f, Color.Black);
        Draw.SpriteBatch.End();
    }

    private static void OnLevelTemplateRenderHighlight(On.Celeste.Editor.LevelTemplate.orig_RenderHighlight orig, LevelTemplate self, Camera camera, bool hovered, bool selected) {
        orig(self, camera, hovered, selected);

        if (Engine.Scene is not MapEditor editor || !CanEdit(editor)) {
            return;
        }

        RoomKey key = RoomKey.From(editor, self);
        RoomRegionMask? mask = RegionStorage.TryGet(key);
        if (mask == null || mask.IsEmpty()) {
            return;
        }

        Color fill = hovered || selected ? EditorFillColor : EditorFillColor * 0.82f;
        for (int y = 0; y < mask.HeightCells; y++) {
            for (int x = 0; x < mask.WidthCells; x++) {
                if (!mask.GetCell(x, y)) {
                    continue;
                }

                Rectangle rect = mask.CellToWorldRect(x, y);
                Draw.Rect(rect.X, rect.Y, rect.Width, rect.Height, fill);
            }
        }
    }

    private static bool CanEdit(MapEditor editor) {
        return AutoSaverModule.Settings.Enabled &&
               mapEditorArmed &&
               MapEditorHelper.TryGetEditingArea(editor, out AreaKey area) &&
               area == armedArea;
    }

    private static MouseState CreateSuppressedMouseState(MouseState state) {
        return new MouseState(
            state.X,
            state.Y,
            state.ScrollWheelValue,
            ButtonState.Released,
            state.MiddleButton,
            ButtonState.Released,
            state.XButton1,
            state.XButton2
        );
    }
}
