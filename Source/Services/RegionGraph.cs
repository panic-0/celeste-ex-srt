using Celeste.Mod.ExSrt.Model;

namespace Celeste.Mod.ExSrt;

public static class RegionGraph {
    private static readonly Point[] Offsets = {
        new(-1, 0),
        new(1, 0),
        new(0, -1),
        new(0, 1)
    };

    public static RoomRegionIndex BuildRegions(RoomRegionMask mask) {
        bool[,] visited = new bool[mask.WidthCells, mask.HeightCells];
        List<(int firstIndex, ConnectedRegion region)> ordered = new();

        for (int y = 0; y < mask.HeightCells; y++) {
            for (int x = 0; x < mask.WidthCells; x++) {
                if (visited[x, y] || !mask.GetCell(x, y)) {
                    continue;
                }

                ordered.Add((y * mask.WidthCells + x, Flood(mask, visited, new Point(x, y))));
            }
        }

        int id = 1;
        List<ConnectedRegion> regions = new();
        int[] regionIdsByCell = new int[mask.WidthCells * mask.HeightCells];
        foreach ((_, ConnectedRegion region) in ordered.OrderBy(entry => entry.firstIndex)) {
            region.RegionId = id++;
            regions.Add(region);
            foreach (Point cell in region.Cells) {
                regionIdsByCell[cell.Y * mask.WidthCells + cell.X] = region.RegionId;
            }
        }

        return new RoomRegionIndex(mask.WidthCells, mask.HeightCells, regions, regionIdsByCell);
    }

    private static ConnectedRegion Flood(RoomRegionMask mask, bool[,] visited, Point start) {
        ConnectedRegion region = new();
        Queue<Point> queue = new();
        queue.Enqueue(start);
        visited[start.X, start.Y] = true;

        int minX = start.X;
        int minY = start.Y;
        int maxX = start.X;
        int maxY = start.Y;

        while (queue.Count > 0) {
            Point current = queue.Dequeue();
            region.Cells.Add(current);
            minX = Math.Min(minX, current.X);
            minY = Math.Min(minY, current.Y);
            maxX = Math.Max(maxX, current.X);
            maxY = Math.Max(maxY, current.Y);

            foreach (Point offset in Offsets) {
                Point next = current + offset;
                if (next.X < 0 || next.Y < 0 || next.X >= mask.WidthCells || next.Y >= mask.HeightCells) {
                    continue;
                }

                if (visited[next.X, next.Y] || !mask.GetCell(next.X, next.Y)) {
                    continue;
                }

                visited[next.X, next.Y] = true;
                queue.Enqueue(next);
            }
        }

        region.Bounds = new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
        return region;
    }
}
