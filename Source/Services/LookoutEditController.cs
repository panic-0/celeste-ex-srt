using Celeste.Mod.ExSrt.Model;

namespace Celeste.Mod.ExSrt;

public static class LookoutEditController {
    private const float RuntimeToEditorScale = 8f;
    private static bool isEditingActive;
    private static bool previousMouseVisibility;
    private static bool mouseVisibilityCaptured;

    public static bool IsEditingActive => isEditingActive;

    public static void Load() {
        On.Celeste.Level.Update += OnLevelUpdate;
        On.Celeste.Level.Render += OnLevelRender;
        On.Celeste.Level.End += OnLevelEnd;
    }

    public static void Unload() {
        On.Celeste.Level.End -= OnLevelEnd;
        On.Celeste.Level.Render -= OnLevelRender;
        On.Celeste.Level.Update -= OnLevelUpdate;
        RestoreMouseVisibility();
        isEditingActive = false;
    }

    private static void OnLevelEnd(On.Celeste.Level.orig_End orig, Level self) {
        RestoreMouseVisibility();
        isEditingActive = false;
        orig(self);
    }

    private static void OnLevelUpdate(On.Celeste.Level.orig_Update orig, Level self) {
        orig(self);

        if (!ExSrtModule.Settings.Enabled) {
            RestoreMouseVisibility();
            isEditingActive = false;
            return;
        }

        Lookout? activeLookout = GetActiveLookout(self);
        if (activeLookout == null) {
            RestoreMouseVisibility();
            isEditingActive = false;
            return;
        }

        if (ExSrtModule.Settings.ToggleLookoutEdit.Pressed) {
            isEditingActive = !isEditingActive;
            UI.Toast.Show(self, isEditingActive ? "Lookout edit enabled" : "Lookout edit disabled");
        }

        if (!isEditingActive) {
            RestoreMouseVisibility();
            return;
        }

        EnsureMouseVisible();

        if (!TryGetHoverTarget(self, out HoverTarget target)) {
            return;
        }

        bool changed = false;
        if (MInput.Mouse.CurrentState.LeftButton == ButtonState.Pressed) {
            changed = ApplyPaint(target, true);
        }
        else if (MInput.Mouse.CurrentState.RightButton == ButtonState.Pressed) {
            changed = ApplyPaint(target, false);
        }

        if (changed) {
            Logger.Info("ex-srt", $"Lookout edit updated room [{target.Key.RoomName}] cell=({target.Cell.X}, {target.Cell.Y}) markedCells={target.Mask.CountMarkedCells()}");
        }
    }

    private static void OnLevelRender(On.Celeste.Level.orig_Render orig, Level self) {
        orig(self);

        if (!ExSrtModule.Settings.Enabled ||
            !ExSrtModule.Settings.ShowLookoutEditOverlay ||
            GetActiveLookout(self) == null) {
            return;
        }

        string status = isEditingActive ? "On" : "Off";
        string text;
        if (TryGetHoverTarget(self, out HoverTarget target)) {
            text = $"Lookout Edit: {status} | Room: {target.Key.RoomName} | Cell: {target.Cell.X},{target.Cell.Y} | LMB Paint / RMB Erase";
        }
        else {
            text = $"Lookout Edit: {status} | Room: No room | LMB Paint / RMB Erase";
        }

        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None,
            RasterizerState.CullNone, null, Engine.ScreenMatrix);
        float width = ActiveFont.Measure(text).X * 0.7f + 18f;
        Draw.Rect(8f, 8f, width, 28f, Color.Black * 0.72f);
        ActiveFont.DrawOutline(text, new Vector2(16f, 14f), Vector2.Zero, Vector2.One * 0.7f, Color.White, 2f, Color.Black);
        Draw.SpriteBatch.End();
    }

    private static bool ApplyPaint(HoverTarget target, bool value) {
        return target.Mask.SetCell(target.Cell.X, target.Cell.Y, value);
    }

    private static void EnsureMouseVisible() {
        if (!mouseVisibilityCaptured) {
            previousMouseVisibility = Engine.Instance.IsMouseVisible;
            mouseVisibilityCaptured = true;
        }

        Engine.Instance.IsMouseVisible = true;
    }

    private static void RestoreMouseVisibility() {
        if (!mouseVisibilityCaptured) {
            return;
        }

        Engine.Instance.IsMouseVisible = previousMouseVisibility;
        mouseVisibilityCaptured = false;
    }

    private static Lookout? GetActiveLookout(Level level) {
        return level.Tracker.GetEntities<Lookout>().OfType<Lookout>().FirstOrDefault(IsLookoutInteracting);
    }

    private static bool IsLookoutInteracting(Lookout lookout) {
        try {
            return DynamicData.For(lookout).Get<bool>("interacting");
        }
        catch {
            return false;
        }
    }

    private static bool TryGetHoverTarget(Level level, out HoverTarget target) {
        target = default;
        if (!TryGetMouseWorldPosition(level, out Vector2 worldPosition)) {
            return false;
        }

        if (!TryGetViewedRoom(level, out RoomKey key, out Rectangle runtimeBounds, out Rectangle editorBounds) ||
            !runtimeBounds.Contains(new Point((int) Math.Floor(worldPosition.X), (int) Math.Floor(worldPosition.Y)))) {
            return false;
        }

        RoomRegionMask mask = RegionStorage.GetOrCreate(key, editorBounds);
        Vector2 editorPosition = worldPosition / RuntimeToEditorScale;
        Point cell = mask.WorldToCell(editorPosition);
        if (cell.X < 0 || cell.Y < 0 || cell.X >= mask.WidthCells || cell.Y >= mask.HeightCells) {
            return false;
        }

        target = new HoverTarget(key, mask, cell);
        return true;
    }

    private static bool TryGetMouseWorldPosition(Level level, out Vector2 worldPosition) {
        worldPosition = Vector2.Zero;
        if (!TryGetWindowMouseToScreen(out Vector2 screenPosition)) {
            return false;
        }

        worldPosition = level.ScreenToWorld(screenPosition);
        return true;
    }

    private static bool TryGetWindowMouseToScreen(out Vector2 screenPosition) {
        screenPosition = Vector2.Zero;
        MouseState mouse = Mouse.GetState();
        if (Engine.ViewWidth <= 0 || Engine.ViewHeight <= 0) {
            return false;
        }

        Rectangle clientBounds = Engine.Instance.Window.ClientBounds;
        if (clientBounds.Width <= 0 || clientBounds.Height <= 0) {
            return false;
        }

        float viewportOffsetX = (clientBounds.Width - Engine.ViewWidth) * 0.5f;
        float viewportOffsetY = (clientBounds.Height - Engine.ViewHeight) * 0.5f;
        float mouseViewportX = mouse.X - viewportOffsetX;
        float mouseViewportY = mouse.Y - viewportOffsetY;
        if (mouseViewportX < 0f || mouseViewportY < 0f || mouseViewportX >= Engine.ViewWidth || mouseViewportY >= Engine.ViewHeight) {
            return false;
        }

        float scaleX = Engine.ViewWidth / 320f;
        float scaleY = Engine.ViewHeight / 180f;
        if (scaleX <= 0f || scaleY <= 0f) {
            return false;
        }

        Vector2 viewportPosition = new(mouseViewportX / scaleX, mouseViewportY / scaleY);
        screenPosition = viewportPosition * 6f;
        return true;
    }

    private static bool TryGetViewedRoom(Level level, out RoomKey key, out Rectangle runtimeBounds, out Rectangle editorBounds) {
        Vector2 worldCenter = level.ScreenToWorld(new Vector2(960f, 540f));
        Point centerPoint = new((int) Math.Floor(worldCenter.X), (int) Math.Floor(worldCenter.Y));

        foreach (LevelData room in level.Session.MapData.Levels) {
            Rectangle roomRuntimeBounds = room.Bounds;
            if (!roomRuntimeBounds.Contains(centerPoint)) {
                continue;
            }

            key = new RoomKey(level.Session.Area.GetSID(), level.Session.Area.Mode, room.Name);
            runtimeBounds = roomRuntimeBounds;
            editorBounds = room.TileBounds;
            return true;
        }

        key = default;
        runtimeBounds = default;
        editorBounds = default;
        return false;
    }

    private readonly record struct HoverTarget(RoomKey Key, RoomRegionMask Mask, Point Cell);
}
