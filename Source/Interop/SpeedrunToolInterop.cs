namespace Celeste.Mod.AutoSaver.Interop;

public static class SpeedrunToolInterop {
    public const string AutoSaveSlotName = "Default Slot";
    private const string LogTag = "AutoSaver";

    [ModImportName("SpeedrunTool.TasAction")]
    private static class TasImports {
        public static Func<string, bool>? TasIsSaved;
    }

    private static MethodInfo? reflectedSwitchSlotMethod;
    private static MethodInfo? reflectedSaveStateMethod;
    private static MethodInfo? reflectedIsSavedMethod;
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
            $"SpeedrunTool interop status: CanAutoSave={CanAutoSave}, HasTasIsSaved={TasImports.TasIsSaved != null}, HasReflectionSwitch={reflectedSwitchSlotMethod != null}, HasReflectionSave={reflectedSaveStateMethod != null}, HasReflectionIsSaved={reflectedIsSavedMethod != null}");

        if (!CanAutoSave) {
            Logger.Log(LogTag, "Speedrun Tool save API unavailable; auto-save will stay disabled.");
        }
    }

    public static void Unload() {
        reflectedSwitchSlotMethod = null;
        reflectedSaveStateMethod = null;
        reflectedIsSavedMethod = null;
        CanAutoSave = false;
        loggedMissingManager = false;
    }

    public static bool TryAutoSave(out string message) {
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
            bool result = (bool) reflectedSaveStateMethod.Invoke(null, args)!;
            string popup = args[0] as string ?? "";
            message = result
                ? $"Auto-saved to Speedrun Tool slot [{AutoSaveSlotName}]"
                : $"Failed to auto-save slot [{AutoSaveSlotName}] {popup}".Trim();
            return result;
        }
        catch (Exception ex) {
            message = $"Speedrun Tool auto-save failed: {ex.GetType().Name}";
            Logger.Log(LogTag, $"Speedrun Tool auto-save invocation failed: {ex}");
            return false;
        }
    }

    public static bool CurrentSlotHasState() {
        try {
            if (reflectedIsSavedMethod != null) {
                return (bool) reflectedIsSavedMethod.Invoke(null, [AutoSaveSlotName])!;
            }

            if (TasImports.TasIsSaved == null) {
                return false;
            }

            return TasImports.TasIsSaved(AutoSaveSlotName);
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

        if (reflectedSwitchSlotMethod == null || reflectedSaveStateMethod == null) {
            Logger.Log(LogTag,
                $"Speedrun Tool reflection bridge incomplete: SwitchSlot={reflectedSwitchSlotMethod != null}, SaveState={reflectedSaveStateMethod != null}, IsSaved={reflectedIsSavedMethod != null}");
        }
    }
}
