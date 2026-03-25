using Celeste.Mod.AutoSaver.Model;
using System.Collections;
using System.Reflection;

namespace Celeste.Mod.AutoSaver;

public static class HotkeyHelper {
    private static bool suppressClearCurrentRoomUntilRelease;

    public static void Load() {
        On.Monocle.MInput.Update += OnInputUpdate;
        suppressClearCurrentRoomUntilRelease = false;
    }

    public static void Unload() {
        On.Monocle.MInput.Update -= OnInputUpdate;
        suppressClearCurrentRoomUntilRelease = false;
    }

    private static void OnInputUpdate(On.Monocle.MInput.orig_Update orig) {
        if (!AutoSaverModule.Settings.Enabled) {
            orig();
            return;
        }

        orig();

        KeyboardState actualKeyboardCurrent = MInput.Keyboard.CurrentState;
        KeyboardState actualKeyboardPrevious = MInput.Keyboard.PreviousState;
        bool bindingHeld = IsBindingHeld(AutoSaverModule.Settings.ClearCurrentRoomMarkers, actualKeyboardCurrent);
        bool suppressClearRoomBinding = suppressClearCurrentRoomUntilRelease ||
                                        ShouldSuppressClearCurrentRoomBinding(actualKeyboardCurrent, actualKeyboardPrevious);

        if (suppressClearCurrentRoomUntilRelease && !bindingHeld) {
            suppressClearCurrentRoomUntilRelease = false;
            suppressClearRoomBinding = false;
        }

        if (suppressClearRoomBinding) {
            MInput.Keyboard.CurrentState = CreateSuppressedKeyboardState(actualKeyboardCurrent, AutoSaverModule.Settings.ClearCurrentRoomMarkers);
            MInput.Keyboard.PreviousState = CreateSuppressedKeyboardState(actualKeyboardPrevious, AutoSaverModule.Settings.ClearCurrentRoomMarkers);
        }

        if (AutoSaverModule.Settings.ToggleLevelOverlay.Pressed) {
            AutoSaverModule.Settings.ShowOverlayInLevel = !AutoSaverModule.Settings.ShowOverlayInLevel;
            UI.Toast.Show(Engine.Scene, AutoSaverModule.Settings.ShowOverlayInLevel
                ? "Level overlay shown"
                : "Level overlay hidden");
        }

        if (suppressClearRoomBinding || AutoSaverModule.Settings.ClearCurrentRoomMarkers.Pressed) {
            RegionStorage.TryClearCurrentRoom();
            if (suppressClearRoomBinding && bindingHeld) {
                suppressClearCurrentRoomUntilRelease = true;
            }
        }
    }

    private static bool ShouldSuppressClearCurrentRoomBinding(KeyboardState currentKeyboardState, KeyboardState previousKeyboardState) {
        if (Engine.Scene is not MapEditor editor) {
            return false;
        }

        if (!CurrentMapRoomHasMarkers(editor)) {
            return false;
        }

        return IsBindingPressed(AutoSaverModule.Settings.ClearCurrentRoomMarkers, currentKeyboardState, previousKeyboardState);
    }

    private static bool CurrentMapRoomHasMarkers(MapEditor editor) {
        if (!MapEditorHelper.TryGetCurrentRoom(editor, out LevelTemplate? currentRoom) ||
            currentRoom == null ||
            currentRoom.Type == LevelTemplateType.Filler) {
            return false;
        }

        RoomRegionMask? mask = RegionStorage.TryGet(RoomKey.From(editor, currentRoom));
        return mask != null && !mask.IsEmpty();
    }

    private static bool IsBindingPressed(ButtonBinding binding, KeyboardState current, KeyboardState previous) {
        HashSet<Keys> keys = GetBoundKeys(binding);
        return keys.Count > 0 &&
               keys.All(current.IsKeyDown) &&
               keys.Any(key => current.IsKeyDown(key) && !previous.IsKeyDown(key));
    }

    private static bool IsBindingHeld(ButtonBinding binding, KeyboardState current) {
        HashSet<Keys> keys = GetBoundKeys(binding);
        return keys.Count > 0 && keys.All(current.IsKeyDown);
    }

    private static KeyboardState CreateSuppressedKeyboardState(KeyboardState state, ButtonBinding binding) {
        HashSet<Keys> suppressedKeys = GetBoundKeys(binding);
        Keys[] remainingKeys = state.GetPressedKeys().Where(key => !suppressedKeys.Contains(key)).ToArray();
        return new KeyboardState(remainingKeys);
    }

    private static HashSet<Keys> GetBoundKeys(ButtonBinding binding) {
        HashSet<Keys> keys = [];
        Type type = binding.GetType();

        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
            TryCollectKeys(property.GetValue(binding), keys);
        }

        foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
            TryCollectKeys(field.GetValue(binding), keys);
        }

        return keys;
    }

    private static void TryCollectKeys(object? value, HashSet<Keys> keys) {
        if (value == null) {
            return;
        }

        if (value is Keys key) {
            keys.Add(key);
            return;
        }

        if (value is IEnumerable enumerable and not string) {
            foreach (object? item in enumerable) {
                if (item is Keys nestedKey) {
                    keys.Add(nestedKey);
                }
            }
        }
    }
}
