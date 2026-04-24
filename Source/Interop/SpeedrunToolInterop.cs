namespace Celeste.Mod.ExSrt.Interop;

public static class SpeedrunToolInterop {
    public const string AutoSaveSlotName = "Default Slot";
    private const string LogTag = "ex-srt";

    [ModImportName("SpeedrunTool.TasAction")]
    private static class TasImports {
        public static Func<string, bool>? TasIsSaved;
    }

    private static MethodInfo? reflectedSwitchSlotMethod;
    private static MethodInfo? reflectedSaveStateMethod;
    private static MethodInfo? reflectedIsSavedMethod;
    private static PropertyInfo? reflectedStateManagerInstanceProperty;
    private static PropertyInfo? reflectedStateManagerStateProperty;
    private static MethodInfo? reflectedStateManagerOutOfFreezeMethod;
    private static PropertyInfo? reflectedSettingsInstanceProperty;
    private static PropertyInfo? reflectedFreezeAfterLoadStateProperty;
    private static object? reflectedFreezeAfterLoadStateOffValue;
    private static object? reflectedStateNoneValue;
    private static object? pendingFreezeRestoreValue;
    private static bool pendingFreezeRestore;
    private static bool pendingAutoSaveFreezeCancel;
    private static bool loggedMissingManager;

    public static bool CanAutoSave { get; private set; }

    public static void Load() {
        try {
            typeof(TasImports).ModInterop();
        }
        catch (Exception ex) {
            Logger.Log(LogTag, $"Speedrun Tool ModInterop init failed: {ex.GetType().Name}: {ex.Message}");
        }

        InitializeReflectionMethods();

        CanAutoSave = reflectedSwitchSlotMethod != null && reflectedSaveStateMethod != null;

        Logger.Log(LogTag,
            $"SpeedrunTool interop status: CanAutoSave={CanAutoSave}, HasTasIsSaved={TasImports.TasIsSaved != null}, HasReflectionSwitch={reflectedSwitchSlotMethod != null}, HasReflectionSave={reflectedSaveStateMethod != null}, HasReflectionState={reflectedStateManagerStateProperty != null}, HasReflectionFreezeSetting={reflectedFreezeAfterLoadStateProperty != null}, HasReflectionIsSaved={reflectedIsSavedMethod != null}");

        if (!CanAutoSave) {
            Logger.Log(LogTag, "Speedrun Tool save API unavailable; auto-save will stay disabled.");
        }
    }

    public static void Unload() {
        reflectedSwitchSlotMethod = null;
        reflectedSaveStateMethod = null;
        reflectedIsSavedMethod = null;
        reflectedStateManagerInstanceProperty = null;
        reflectedStateManagerStateProperty = null;
        reflectedStateManagerOutOfFreezeMethod = null;
        reflectedSettingsInstanceProperty = null;
        reflectedFreezeAfterLoadStateProperty = null;
        reflectedFreezeAfterLoadStateOffValue = null;
        reflectedStateNoneValue = null;
        pendingFreezeRestoreValue = null;
        pendingFreezeRestore = false;
        pendingAutoSaveFreezeCancel = false;
        CanAutoSave = false;
        loggedMissingManager = false;
    }

    public static void UpdatePendingState(Level? level) {
        if (pendingAutoSaveFreezeCancel) {
            TryCancelCurrentAutoSaveFreeze(level);
        }

        if (!pendingFreezeRestore ||
            reflectedStateManagerInstanceProperty == null ||
            reflectedStateManagerStateProperty == null ||
            reflectedSettingsInstanceProperty == null ||
            reflectedFreezeAfterLoadStateProperty == null) {
            return;
        }

        try {
            object? stateManager = reflectedStateManagerInstanceProperty.GetValue(null);
            object? state = reflectedStateManagerStateProperty.GetValue(stateManager);
            if (!Equals(state, reflectedStateNoneValue)) {
                return;
            }

            object? settingsInstance = reflectedSettingsInstanceProperty.GetValue(null);
            reflectedFreezeAfterLoadStateProperty.SetValue(settingsInstance, pendingFreezeRestoreValue);
            pendingFreezeRestoreValue = null;
            pendingFreezeRestore = false;
        }
        catch (Exception ex) {
            Logger.Log(LogTag, $"Failed to restore Speedrun Tool freeze setting after auto-save: {ex}");
            pendingFreezeRestoreValue = null;
            pendingFreezeRestore = false;
        }
    }

    public static bool TryAutoSave(out string message, bool disableSrtFreezeOnAutoSave) {
        if (!CanAutoSave || reflectedSwitchSlotMethod == null || reflectedSaveStateMethod == null) {
            message = "Speedrun Tool unavailable";
            return false;
        }

        try {
            bool switched = (bool) reflectedSwitchSlotMethod.Invoke(null, [AutoSaveSlotName])!;
            if (!switched) {
                message = $"Failed to switch to slot [{AutoSaveSlotName}]";
                return false;
            }

            object?[] args = [null];
            bool saveResult = (bool) reflectedSaveStateMethod.Invoke(null, args)!;
            string savePopup = args[0] as string ?? "";
            if (!saveResult) {
                message = $"Failed to auto-save slot [{AutoSaveSlotName}] {savePopup}".Trim();
                return false;
            }

            if (disableSrtFreezeOnAutoSave &&
                reflectedSettingsInstanceProperty != null &&
                reflectedFreezeAfterLoadStateProperty != null &&
                reflectedFreezeAfterLoadStateOffValue != null) {
                object? settingsInstance = reflectedSettingsInstanceProperty.GetValue(null);
                pendingFreezeRestoreValue = reflectedFreezeAfterLoadStateProperty.GetValue(settingsInstance);
                reflectedFreezeAfterLoadStateProperty.SetValue(settingsInstance, reflectedFreezeAfterLoadStateOffValue);
                pendingFreezeRestore = true;
            }

            if (disableSrtFreezeOnAutoSave) {
                pendingAutoSaveFreezeCancel = true;
            }

            message = disableSrtFreezeOnAutoSave
                ? $"Auto-saved slot [{AutoSaveSlotName}] without SRT freeze or wipe"
                : $"Auto-saved slot [{AutoSaveSlotName}]";
            return true;
        }
        catch (Exception ex) {
            message = $"Speedrun Tool auto-save failed: {ex.GetType().Name}";
            Logger.Log(LogTag, $"Speedrun Tool auto-save invocation failed: {ex}");
            return false;
        }
    }

    public static bool TryCurrentSlotHasState(out bool hasState) {
        hasState = true;
        try {
            if (reflectedIsSavedMethod != null) {
                hasState = (bool) reflectedIsSavedMethod.Invoke(null, [AutoSaveSlotName])!;
                return true;
            }

            if (TasImports.TasIsSaved == null) {
                Logger.Log(LogTag, "Speedrun Tool saved-state query unavailable; skipping auto-save to avoid overwriting an existing slot.");
                return false;
            }

            hasState = TasImports.TasIsSaved(AutoSaveSlotName);
            return true;
        }
        catch (Exception ex) {
            Logger.Log(LogTag, $"Speedrun Tool saved-state query failed: {ex}");
            return false;
        }
    }

    private static void InitializeReflectionMethods() {
        Type? saveSlotsManager = AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType("Celeste.Mod.SpeedrunTool.SaveLoad.SaveSlotsManager", throwOnError: false))
            .FirstOrDefault(type => type != null);

        if (saveSlotsManager == null) {
            if (!loggedMissingManager) {
                Logger.Log(LogTag, "Speedrun Tool SaveSlotsManager type was not found; reflection auto-save bridge disabled.");
                loggedMissingManager = true;
            }
            return;
        }

        reflectedSwitchSlotMethod = saveSlotsManager.GetMethod("SwitchSlot", BindingFlags.Public | BindingFlags.Static, null, [typeof(string)], null);
        reflectedSaveStateMethod = saveSlotsManager.GetMethod("SaveState", BindingFlags.Public | BindingFlags.Static, null, [typeof(string).MakeByRefType()], null);
        reflectedIsSavedMethod = saveSlotsManager.GetMethod("IsSaved", BindingFlags.Public | BindingFlags.Static, null, [typeof(string)], null);
        InitializeStateManagerReflection();
        InitializeFreezeSettingReflection();

        if (reflectedSwitchSlotMethod == null || reflectedSaveStateMethod == null) {
            Logger.Log(LogTag,
                $"Speedrun Tool reflection bridge incomplete: SwitchSlot={reflectedSwitchSlotMethod != null}, SaveState={reflectedSaveStateMethod != null}, IsSaved={reflectedIsSavedMethod != null}");
        }
    }

    private static void InitializeStateManagerReflection() {
        Type? stateManagerType = AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType("Celeste.Mod.SpeedrunTool.SaveLoad.StateManager", throwOnError: false))
            .FirstOrDefault(type => type != null);
        Type? stateEnumType = AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType("Celeste.Mod.SpeedrunTool.SaveLoad.State", throwOnError: false))
            .FirstOrDefault(type => type != null);

        if (stateManagerType == null || stateEnumType == null) {
            return;
        }

        reflectedStateManagerInstanceProperty = stateManagerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        reflectedStateManagerStateProperty = stateManagerType.GetProperty("State", BindingFlags.Public | BindingFlags.Instance);
        reflectedStateManagerOutOfFreezeMethod = stateManagerType.GetMethod("OutOfFreeze", BindingFlags.NonPublic | BindingFlags.Instance, null, [typeof(Level)], null);
        if (reflectedStateManagerInstanceProperty != null && reflectedStateManagerStateProperty != null) {
            reflectedStateNoneValue = Enum.Parse(stateEnumType, "None");
        }
    }

    private static void InitializeFreezeSettingReflection() {
        Type? settingsType = AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType("Celeste.Mod.SpeedrunTool.SpeedrunToolSettings", throwOnError: false))
            .FirstOrDefault(type => type != null);
        Type? freezeEnumType = AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType("Celeste.Mod.SpeedrunTool.FreezeAfterLoadStateType", throwOnError: false))
            .FirstOrDefault(type => type != null);

        if (settingsType == null || freezeEnumType == null) {
            return;
        }

        reflectedSettingsInstanceProperty = settingsType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        reflectedFreezeAfterLoadStateProperty = settingsType.GetProperty("FreezeAfterLoadStateType", BindingFlags.Public | BindingFlags.Instance);

        if (reflectedSettingsInstanceProperty != null && reflectedFreezeAfterLoadStateProperty != null) {
            reflectedFreezeAfterLoadStateOffValue = Enum.Parse(freezeEnumType, "Off");
        }
    }

    private static void TryCancelCurrentAutoSaveFreeze(Level? level) {
        if (level == null ||
            reflectedStateManagerInstanceProperty == null ||
            reflectedStateManagerStateProperty == null ||
            reflectedStateManagerOutOfFreezeMethod == null) {
            return;
        }

        try {
            object? stateManager = reflectedStateManagerInstanceProperty.GetValue(null);
            object? state = reflectedStateManagerStateProperty.GetValue(stateManager);
            if (Equals(state, reflectedStateNoneValue)) {
                pendingAutoSaveFreezeCancel = false;
                return;
            }

            RemoveWaitingEntities(level);
            level.Wipe = null;
            reflectedStateManagerOutOfFreezeMethod.Invoke(stateManager, [level]);
            pendingAutoSaveFreezeCancel = false;
        }
        catch (Exception ex) {
            Logger.Log(LogTag, $"Failed to cancel Speedrun Tool freeze after auto-save: {ex}");
            pendingAutoSaveFreezeCancel = false;
        }
    }

    private static void RemoveWaitingEntities(Level level) {
        Type? waitingEntityType = AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType("Celeste.Mod.SpeedrunTool.SaveLoad.StateManager+WaitingEntity", throwOnError: false))
            .FirstOrDefault(type => type != null);

        if (waitingEntityType == null) {
            return;
        }

        foreach (Entity entity in level.Entities.ToAdd.Concat(level.Entities).Where(entity => waitingEntityType.IsInstanceOfType(entity)).ToArray()) {
            entity.RemoveSelf();
        }
    }
}
