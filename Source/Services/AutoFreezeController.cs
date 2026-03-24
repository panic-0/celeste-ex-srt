namespace Celeste.Mod.AutoSaver;

public static class AutoFreezeController {
    private static bool active;
    private static FreezeControllerEntity? controllerEntity;

    public static void Load() {
        On.Monocle.Scene.BeforeUpdate += OnSceneBeforeUpdate;
    }

    public static void Unload() {
        On.Monocle.Scene.BeforeUpdate -= OnSceneBeforeUpdate;
        Release("module unload");
    }

    public static void Activate() {
        active = true;
        SetFreezeActive(Engine.Scene as Level, true);
        Logger.Log("AutoSaver", "Auto freeze activated");
    }

    private static void OnSceneBeforeUpdate(On.Monocle.Scene.orig_BeforeUpdate orig, Scene self) {
        if (active && self is not Level) {
            Release($"scene changed to {self.GetType().Name}");
        }
        else if (active) {
            if (ShouldUnfreeze()) {
                Release("input");
            }
            else {
                SetFreezeActive((Level) self, true);
            }
        }

        orig(self);
    }

    private static bool ShouldUnfreeze() {
        return Input.Pause.Pressed ||
               Input.ESC.Pressed ||
               Input.MenuConfirm.Pressed ||
               Input.MenuCancel.Pressed ||
               Input.Jump.Pressed ||
               Input.Dash.Pressed ||
               Input.Grab.Pressed ||
               Input.Talk.Pressed ||
               Input.MoveX.Value != 0 ||
               Input.MoveY.Value != 0 ||
               MInput.Mouse.PressedLeftButton ||
               MInput.Mouse.PressedRightButton;
    }

    private static void Release(string reason) {
        if (!active && controllerEntity?.Scene == null) {
            return;
        }

        SetFreezeActive(Engine.Scene as Level, false);
        active = false;
        Logger.Log("AutoSaver", $"Auto freeze released by {reason}");
    }

    private static void SetFreezeActive(Level? level, bool enabled) {
        FreezeControllerEntity? entity = EnsureFreezeEntity(level);
        if (entity == null) {
            return;
        }

        entity.SetEnabled(enabled);
    }

    private static FreezeControllerEntity? EnsureFreezeEntity(Level? level) {
        if (level == null) {
            controllerEntity?.SetEnabled(false);
            controllerEntity = null;
            return null;
        }

        if (controllerEntity?.Scene == level) {
            return controllerEntity;
        }

        controllerEntity = level.Entities.FindFirst<FreezeControllerEntity>();
        if (controllerEntity != null) {
            return controllerEntity;
        }

        controllerEntity = new FreezeControllerEntity();
        level.Add(controllerEntity);
        return controllerEntity;
    }

    private sealed class FreezeControllerEntity : Entity {
        private readonly global::Celeste.Mod.Entities.TimeRateModifier timeRateModifier = new(0f, false);

        public FreezeControllerEntity() {
            Tag = Tags.Persistent;
            Add(timeRateModifier);
        }

        public void SetEnabled(bool enabled) {
            timeRateModifier.Enabled = enabled;
        }
    }
}
