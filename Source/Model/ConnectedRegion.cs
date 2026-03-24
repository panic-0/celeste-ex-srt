namespace Celeste.Mod.AutoSaver.Model;

public sealed class ConnectedRegion {
    public int RegionId { get; set; }
    public HashSet<Point> Cells { get; } = new();
    public Rectangle Bounds { get; set; }
}
