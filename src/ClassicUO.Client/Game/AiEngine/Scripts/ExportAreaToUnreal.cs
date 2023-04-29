using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AkatoshQuester.Helpers.LightGeometry;
using ClassicUO.AiEngine.AiEngineTasks;
using ClassicUO.Assets;
using ClassicUO.Game.Enums;
using ClassicUO.Game.GameObjects;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using static ClassicUO.Game.UltimaLive;
// ReSharper disable ConvertIfStatementToConditionalTernaryExpression

namespace ClassicUO.Game.AiEngine.Scripts
{
    public class ExportAreaToUnreal : BaseAITask
    {

        public ExportAreaToUnreal() : base("Export Area To Unreal")
        {
        }

        public static List<Point2D> GetPointsBetween(Point2D start, Point2D end)
        {
            List<Point2D> points = new();

            int minX = Math.Min((int)start.X, (int)end.X);
            int maxX = Math.Max((int)start.X, (int)end.X);
            int minY = Math.Min((int)start.Y, (int)end.Y);
            int maxY = Math.Max((int)start.Y, (int)end.Y);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    points.Add(new Point2D(x, y));
                }
            }

            return points;
        }

        public override async Task<bool> Pulse() {

            var startPos = new Point2D(3336, 2360);
            var endPos = new Point2D(3836, 2838);
            var graphicIds = new List<ushort> {
                1175,
                1173,
                1174,
                1176,
                //2760,
                1300,
                1299,
                1298,
                1297,
                1295,
                1296,
                1295,
                1294,
                1293,
                2760,
                1848,
                663,
                657,
                658,
                659,
                660,
                661,
                662,
                664,
                665,
                666,
                667,
                668,
                669,
                670,
                671,
                672,
                673,
                674,
                675,
                676,
                677,
                678,
                679,
            };

            var pointsBetween = GetPointsBetween(startPos, endPos);
            var stringList = new List<string>();

            foreach (var point2D in pointsBetween) {
                stringList.AddRange(BuildTile((int) point2D.X, (int) point2D.Y, graphicIds));
            }

            File.WriteAllLines("C:\\testoutput\\spawntest.txt", stringList);

            AiCore.Instance.StopScript();
            GameActions.MessageOverhead($"[UoMapToHeightmap] Finished", Player.Serial);

            return true;
        }

        internal static List<Static> GetAllStaticsOnTile(GameObject go) {
            if (go == null) {
                return new List<Static>();
            }

            var list = new List<Static>();

            var tile = go;

            while (tile != null) {
                if (tile is Static s) {
                    list.Add(s);
                }
                tile = tile.TNext;
            }

            return list;
        }

        public static List<string> BuildTile(int x, int y, List<ushort> graphicIds) {
            if (x >= MapLoader.Instance.MapsDefaultSize[World.MapIndex, 0] || y >= MapLoader.Instance.MapsDefaultSize[World.MapIndex, 1]) {
                return new List<string>();
            }

            var list = new List<string>();
            var point = new Point2D(x, y);
            var addedList = new List<Tuple<Point3D, int>>();

            try {
                var tile = World.Map.GetTile(x, y);

                if (x == 3494 && y == 2572) {
                    int asdf = 1;
                }

                var allStaticsOnTile = GetAllStaticsOnTile(tile);

                foreach (var staticTile in allStaticsOnTile.Where(s => graphicIds.Contains(s.Graphic) || true)) {
                    var tuple = new Tuple<Point3D, int>(new Point3D(staticTile.X, staticTile.Y, staticTile.Z), staticTile.Graphic);

                    if (addedList.Contains(tuple)) {
                        continue;
                    }

                    addedList.Add(tuple);
                    list.Add($"\"x\":{staticTile.X},\"y\":{staticTile.Y},\"z\":{staticTile.Z},\"graphic\":{staticTile.Graphic},\"hue\":{staticTile.Hue}");

                }
            }
            catch (Exception) {

            }


            return list;
        }
    }
}
