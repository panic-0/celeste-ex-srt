namespace Celeste.Mod.AutoSaver;

public static class HotkeyHelper {
    private static GamePadState previousGamePadState;

    public static void Load() {
        On.Monocle.MInput.Update += OnInputUpdate;
        previousGamePadState = default;
    }

    public static void Unload() {
        On.Monocle.MInput.Update -= OnInputUpdate;
        previousGamePadState = default;
    }

    private static void OnInputUpdate(On.Monocle.MInput.orig_Update orig) {
        orig();

        if (!AutoSaverModule.Settings.Enabled) {
            return;
        }

        if (Pressed(AutoSaverModule.Settings.KeyboardClearMarkedRegions, AutoSaverModule.Settings.ControllerClearMarkedRegions)) {
            RegionStorage.TryClearCurrentRoom();
        }

        if (AutoSaverModule.Settings.ToggleLevelOverlay.Pressed) {
            AutoSaverModule.Settings.ShowOverlayInLevel = !AutoSaverModule.Settings.ShowOverlayInLevel;
            UI.Toast.Show(Engine.Scene, AutoSaverModule.Settings.ShowOverlayInLevel
                ? "Level overlay shown"
                : "Level overlay hidden");
        }
    }

    private static bool Pressed(List<Keys> keys, List<Buttons> buttons) {
        bool keyboardPressed = keys.Count > 0 &&
                              keys.All(key => MInput.Keyboard.CurrentState.IsKeyDown(key)) &&
                              keys.Any(key => MInput.Keyboard.Pressed(key));
        if (keyboardPressed) {
            return true;
        }

        return ControllerPressed(buttons);
    }

    private static bool ControllerPressed(List<Buttons> buttons) {
        if (buttons.Count == 0) {
            previousGamePadState = GamePad.GetState(PlayerIndex.One);
            return false;
        }

        GamePadState currentState = GamePad.GetState(PlayerIndex.One);
        bool comboHeld = currentState.IsConnected && buttons.All(currentState.IsButtonDown);
        bool newlyPressed = comboHeld && buttons.Any(button => currentState.IsButtonDown(button) && !previousGamePadState.IsButtonDown(button));
        previousGamePadState = currentState;
        return newlyPressed;
    }
}
