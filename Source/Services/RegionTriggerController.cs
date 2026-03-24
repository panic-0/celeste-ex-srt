using Celeste.Mod.AutoSaver.Interop;
using Celeste.Mod.AutoSaver.Model;

namespace Celeste.Mod.AutoSaver;

public static class RegionTriggerController {
    private const float RuntimeToEditorScale = 8f;
    private static readonly Color LevelOverlayColor = new(255, 150, 70, 135);
    private static readonly Dictionary<string, RoomRegionIndex> CachedRegions = new();
    private static readonly Dictionary<string, int> CachedVersions = new();
    private static readonly Dictionary<string, HashSet<int>> LastRegionsByRoom = new();

    public static void Load() {
        On.Celeste.Level.Update += OnLevelUpdate;
        On.Celeste.Level.LoadLevel += OnLevelLoadLevel;
        On.Celeste.Level.End += OnLevelEnd;
    }

    public static void Unload() {
        On.Celeste.Level.End -= OnLevelEnd;
        On.Celeste.Level.LoadLevel -= OnLevelLoadLevel;
        On.Celeste.Level.Update -= OnLevelUpdate;
        CachedRegions.Clear();
        CachedVersions.Clear();
        LastRegionsByRoom.Clear();
    }

    private static void OnLevelLoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes intro, bool isFromLoader) {
        orig(self, intro, isFromLoader);
        Logger.Log("AutoSaver", $"Level loaded sid={self.Session.Area.GetSID()} room={self.Session.Level}");
        LogRoomMaskStatus(self);
        EnsureRoomCache(self);
        EnsureOverlayRenderer(self);
    }

    private static void OnLevelEnd(On.Celeste.Level.orig_End orig, Level self) {
        LastRegionsByRoom.Remove(RoomKey.From(self).ToString());
        orig(self);
    }

    private static void OnLevelUpdate(On.Celeste.Level.orig_Update orig, Level self) {
        orig(self);

        if (!AutoSaverModule.Settings.Enabled) {
            return;
        }

        RoomKey key = RoomKey.From(self);
        RoomRegionMask? mask = RegionStorage.TryGet(key);
        if (mask == null || mask.IsEmpty()) {
            LastRegionsByRoom.Remove(key.ToString());
            return;
        }

        Player? player = self.Tracker.GetEntity<Player>();
        if (player == null || player.Dead) {
            LastRegionsByRoom[key.ToString()] = [];
            return;
        }

        RoomRegionIndex regions = EnsureRoomCache(self);
        HashSet<int> currentRegions = GetTouchedRegions(mask, regions, player);
        HashSet<int> lastRegions = LastRegionsByRoom.GetValueOrDefault(key.ToString(), []);

        if (currentRegions.Count == 0) {
            LastRegionsByRoom[key.ToString()] = [];
            return;
        }

        foreach (int regionId in currentRegions) {
            if (!lastRegions.Contains(regionId)) {
                OnEnterRegion(self, key, regionId);
            }
        }

        LastRegionsByRoom[key.ToString()] = currentRegions;
    }

    private static void OnEnterRegion(Level level, RoomKey key, int regionId) {
        Dictionary<int, int> roomCounts = GetRoomCounts(key);
        roomCounts[regionId] = roomCounts.GetValueOrDefault(regionId) + 1;
        bool slotHasState = SpeedrunToolInterop.CurrentSlotHasState();
        Logger.Log("AutoSaver", $"Entered region #{regionId} in room [{key.RoomName}], count={roomCounts[regionId]}, slotHasState={slotHasState}");

        if (slotHasState) {
            Logger.Log("AutoSaver", $"Skipped auto-save for region #{regionId} in room [{key.RoomName}] because slot [{SpeedrunToolInterop.AutoSaveSlotName}] already has a save");
            return;
        }

        if (roomCounts[regionId] < Math.Max(1, AutoSaverModule.Settings.TriggerEnterCountK)) {
            return;
        }

        if (!SpeedrunToolInterop.CanAutoSave) {
            Logger.Log("AutoSaver", "Region reached but Speedrun Tool auto-save API is unavailable");
            if (AutoSaverModule.Settings.ShowPopupOnAutoSave) {
                UI.Toast.Show(level, "Region reached, but Speedrun Tool auto-save API is unavailable");
            }
            return;
        }

        if (SpeedrunToolInterop.TryAutoSave(out string message)) {
            AutoSaverModule.Session.LastAutoSaveMessage = $"{message} from room [{key.RoomName}] region #{regionId}";
            Logger.Log("AutoSaver", AutoSaverModule.Session.LastAutoSaveMessage);
            if (AutoSaverModule.Settings.ShowPopupOnAutoSave) {
                UI.Toast.Show(level, AutoSaverModule.Session.LastAutoSaveMessage);
            }
        }
        else if (AutoSaverModule.Settings.ShowPopupOnAutoSave) {
            Logger.Log("AutoSaver", $"{message} from room [{key.RoomName}] region #{regionId}");
            UI.Toast.Show(level, $"{message} from room [{key.RoomName}] region #{regionId}");
        }
    }

    private static Dictionary<int, int> GetRoomCounts(RoomKey key) {
        if (!AutoSaverModule.Session.EnterCounts.TryGetValue(key.ToString(), out Dictionary<int, int>? counts)) {
            counts = new Dictionary<int, int>();
            AutoSaverModule.Session.EnterCounts[key.ToString()] = counts;
        }

        return counts;
    }

    private static RoomRegionIndex EnsureRoomCache(Level level) {
        RoomKey key = RoomKey.From(level);
        RoomRegionMask? mask = RegionStorage.TryGet(key);
        if (mask == null) {
            return CachedRegions[key.ToString()] = new RoomRegionIndex(0, 0, [], []);
        }

        if (!CachedVersions.TryGetValue(key.ToString(), out int version) || version != mask.Version || !CachedRegions.ContainsKey(key.ToString())) {
            CachedRegions[key.ToString()] = RegionGraph.BuildRegions(mask);
            CachedVersions[key.ToString()] = mask.Version;
        }

        return CachedRegions[key.ToString()];
    }

    private static void LogRoomMaskStatus(Level level) {
        RoomKey key = RoomKey.From(level);
        RoomRegionMask? mask = RegionStorage.TryGet(key);
        string[] roomNameMatches = AutoSaverModule.SaveData.Rooms.Keys
            .Where(savedKey => savedKey.EndsWith($"|{key.RoomName}", StringComparison.Ordinal))
            .Take(8)
            .ToArray();

        if (mask == null) {
            Logger.Log("AutoSaver",
                $"Level load key [{key}] has no saved mask. totalSavedRooms={AutoSaverModule.SaveData.Rooms.Count}, roomNameMatches=[{string.Join(", ", roomNameMatches)}]");
            return;
        }

        Logger.Log("AutoSaver",
            $"Level load key [{key}] mask found. markedCells={mask.CountMarkedCells()} version={mask.Version} totalSavedRooms={AutoSaverModule.SaveData.Rooms.Count}");
    }

    private static HashSet<int> GetTouchedRegions(RoomRegionMask mask, RoomRegionIndex regions, Player player) {
        HashSet<int> touched = [];
        Rectangle playerBounds = GetPlayerBounds(player);
        Vector2 topLeftEditor = new(playerBounds.Left / RuntimeToEditorScale, playerBounds.Top / RuntimeToEditorScale);
        Vector2 bottomRightEditor = new((playerBounds.Right - 1) / RuntimeToEditorScale, (playerBounds.Bottom - 1) / RuntimeToEditorScale);
        Point topLeft = mask.WorldToCell(topLeftEditor);
        Point bottomRight = mask.WorldToCell(bottomRightEditor);

        for (int y = topLeft.Y; y <= bottomRight.Y; y++) {
            for (int x = topLeft.X; x <= bottomRight.X; x++) {
                int regionId = regions.GetRegionIdAtCell(new Point(x, y));
                if (regionId != 0) {
                    touched.Add(regionId);
                }
            }
        }

        return touched;
    }

    private static Rectangle GetPlayerBounds(Player player) {
        Collider? collider = player.Collider;
        if (collider is Hitbox hitbox) {
            return new Rectangle(
                (int) Math.Floor(player.X + hitbox.Left),
                (int) Math.Floor(player.Y + hitbox.Top),
                (int) Math.Ceiling(hitbox.Width),
                (int) Math.Ceiling(hitbox.Height)
            );
        }

        return new Rectangle(
            (int) Math.Floor(player.Center.X - 4f),
            (int) Math.Floor(player.Center.Y - 6f),
            8,
            12
        );
    }

    private static void RenderLevelMask(Level level, RoomRegionMask mask) {
        for (int y = 0; y < mask.HeightCells; y++) {
            for (int x = 0; x < mask.WidthCells; x++) {
                if (!mask.GetCell(x, y)) {
                    continue;
                }

                Rectangle editorRect = mask.CellToWorldRect(x, y);
                float worldX = editorRect.X * RuntimeToEditorScale;
                float worldY = editorRect.Y * RuntimeToEditorScale;
                float size = editorRect.Width * RuntimeToEditorScale;
                Draw.Rect(worldX, worldY, size, size, LevelOverlayColor);
            }
        }
    }

    private static void EnsureOverlayRenderer(Level level) {
        if (level.Entities.FindFirst<RegionOverlayEntity>() != null) {
            return;
        }

        level.Add(new RegionOverlayEntity());
    }

    private sealed class RegionOverlayEntity : Entity {
        public RegionOverlayEntity() {
            Depth = int.MinValue + 1000;
            Tag = Tags.Persistent;
        }

        public override void Render() {
            base.Render();

            if (!AutoSaverModule.Settings.Enabled ||
                !AutoSaverModule.Settings.ShowOverlayInLevel ||
                Scene is not Level level) {
                return;
            }

            RoomRegionMask? mask = RegionStorage.TryGet(RoomKey.From(level));
            if (mask == null || mask.IsEmpty()) {
                return;
            }

            RenderLevelMask(level, mask);
        }
    }
}
