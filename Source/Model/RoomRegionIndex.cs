namespace Celeste.Mod.ExSrt.Model;

public sealed class RoomRegionIndex {
    private readonly int[] regionIdsByCell;

    public RoomRegionIndex(int widthCells, int heightCells, List<ConnectedRegion> regions, int[] regionIdsByCell) {
        WidthCells = widthCells;
        HeightCells = heightCells;
        Regions = regions;
        this.regionIdsByCell = regionIdsByCell;
    }

    public int WidthCells { get; }
    public int HeightCells { get; }
    public List<ConnectedRegion> Regions { get; }

    public int GetRegionIdAtCell(Point cell) {
        if (cell.X < 0 || cell.Y < 0 || cell.X >= WidthCells || cell.Y >= HeightCells) {
            return 0;
        }

        return regionIdsByCell[cell.Y * WidthCells + cell.X];
    }
}
