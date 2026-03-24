using System.Runtime.Serialization;

namespace Celeste.Mod.AutoSaver.Model;

[DataContract]
public class RoomRegionMask {
    public const int CellSize = 1;

    [DataMember] public int OriginX { get; set; }
    [DataMember] public int OriginY { get; set; }
    [DataMember] public int WidthCells { get; set; }
    [DataMember] public int HeightCells { get; set; }
    [DataMember] public List<string> Rows { get; set; } = new();
    [DataMember] public int Version { get; set; }

    public static RoomRegionMask Create(Rectangle bounds) {
        RoomRegionMask mask = new() {
            OriginX = bounds.X,
            OriginY = bounds.Y,
            WidthCells = Math.Max(1, (int) Math.Ceiling(bounds.Width / (float) CellSize)),
            HeightCells = Math.Max(1, (int) Math.Ceiling(bounds.Height / (float) CellSize))
        };
        return mask.WithInitializedRows();
    }

    public RoomRegionMask WithInitializedRows() {
        while (Rows.Count < HeightCells) {
            Rows.Add(new string('.', WidthCells));
        }

        for (int i = 0; i < Rows.Count; i++) {
            string row = Rows[i] ?? "";
            if (row.Length < WidthCells) {
                row = row.PadRight(WidthCells, '.');
            }
            else if (row.Length > WidthCells) {
                row = row[..WidthCells];
            }

            Rows[i] = row;
        }

        return this;
    }

    public bool GetCell(int x, int y) {
        return x >= 0 && y >= 0 && x < WidthCells && y < HeightCells && Rows[y][x] == '#';
    }

    public bool SetCell(int x, int y, bool value) {
        if (x < 0 || y < 0 || x >= WidthCells || y >= HeightCells) {
            return false;
        }

        char next = value ? '#' : '.';
        char[] chars = Rows[y].ToCharArray();
        if (chars[x] == next) {
            return false;
        }

        chars[x] = next;
        Rows[y] = new string(chars);
        Version++;
        return true;
    }

    public Point WorldToCell(Vector2 world) {
        return new Point((int) Math.Floor((world.X - OriginX) / CellSize), (int) Math.Floor((world.Y - OriginY) / CellSize));
    }

    public Rectangle CellToWorldRect(int x, int y) {
        return new Rectangle(OriginX + x * CellSize, OriginY + y * CellSize, CellSize, CellSize);
    }

    public bool IsEmpty() {
        return Rows.All(row => row.All(cell => cell != '#'));
    }

    public int CountMarkedCells() {
        int count = 0;
        foreach (string row in Rows) {
            foreach (char cell in row) {
                if (cell == '#') {
                    count++;
                }
            }
        }

        return count;
    }
}
