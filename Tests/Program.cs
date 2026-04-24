using Celeste.Mod.ExSrt;
using Celeste.Mod.ExSrt.Model;
using Microsoft.Xna.Framework;

TestRoomRegionMaskSetCell();
TestRoomRegionMaskInitializationTrimsAndPadsRows();
TestRegionGraphBuildsStableRegionIds();
    TestRoomRegionIndexBoundsChecks();

Console.WriteLine("All ex-srt logic tests passed.");

static void TestRoomRegionMaskSetCell() {
    RoomRegionMask mask = RoomRegionMask.Create(new Rectangle(0, 0, 3, 2));

    Assert(mask.Version == 0, "New masks should start at version 0.");
    Assert(mask.SetCell(1, 0, true), "Setting an unmarked cell should report a change.");
    Assert(mask.GetCell(1, 0), "The changed cell should be marked.");
    Assert(mask.Version == 1, "Version should increment after a change.");
    Assert(!mask.SetCell(1, 0, true), "Setting the same value twice should report no change.");
    Assert(mask.Version == 1, "Version should not increment when nothing changed.");
}

static void TestRoomRegionMaskInitializationTrimsAndPadsRows() {
    RoomRegionMask mask = new() {
        OriginX = 0,
        OriginY = 0,
        WidthCells = 4,
        HeightCells = 3,
        Rows = new List<string> { "#", "#####"}
    };

    mask.WithInitializedRows();

    Assert(mask.Rows.Count == 3, "Row count should be padded to the configured height.");
    Assert(mask.Rows[0] == "#...", "Short rows should be padded with empty cells.");
    Assert(mask.Rows[1] == "####", "Long rows should be trimmed to width.");
    Assert(mask.Rows[2] == "....", "Missing rows should be initialized empty.");
}

static void TestRegionGraphBuildsStableRegionIds() {
    RoomRegionMask mask = RoomRegionMask.Create(new Rectangle(0, 0, 4, 3));
    mask.SetCell(0, 0, true);
    mask.SetCell(1, 0, true);
    mask.SetCell(3, 2, true);

    RoomRegionIndex runtime = RegionGraph.BuildRegions(mask);

    Assert(runtime.Regions.Count == 2, "Separated painted groups should produce two regions.");
    Assert(runtime.GetRegionIdAtCell(new Point(0, 0)) == 1, "Top-left group should keep the first region id.");
    Assert(runtime.GetRegionIdAtCell(new Point(1, 0)) == 1, "Connected cells should share the same region id.");
    Assert(runtime.GetRegionIdAtCell(new Point(3, 2)) == 2, "Later groups should get increasing region ids.");
    Assert(runtime.Regions[0].Bounds == new Rectangle(0, 0, 2, 1), "Region bounds should match the first component.");
    Assert(runtime.Regions[1].Bounds == new Rectangle(3, 2, 1, 1), "Region bounds should match the second component.");
}

static void TestRoomRegionIndexBoundsChecks() {
    RoomRegionIndex runtime = new(2, 2, [], [1, 0, 0, 2]);

    Assert(runtime.GetRegionIdAtCell(new Point(0, 0)) == 1, "Lookup should return stored region ids.");
    Assert(runtime.GetRegionIdAtCell(new Point(1, 1)) == 2, "Lookup should work at the last valid cell.");
    Assert(runtime.GetRegionIdAtCell(new Point(-1, 0)) == 0, "Out-of-bounds lookups should return 0.");
    Assert(runtime.GetRegionIdAtCell(new Point(2, 0)) == 0, "Out-of-range x should return 0.");
}

static void Assert(bool condition, string message) {
    if (!condition) {
        throw new InvalidOperationException(message);
    }
}
