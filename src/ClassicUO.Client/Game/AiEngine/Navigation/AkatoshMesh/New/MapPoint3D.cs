using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AkatoshQuester.Helpers.LightGeometry;

namespace AkatoshQuester.Helpers.LightGeometry
{
    public class MapPoint3D {
        public Point3D Point;
        public int MapIndex;

        public MapPoint3D() {
            Point = Point3D.Empty;
            MapIndex = 0;
        }

        public MapPoint3D(int x, int y, int z, int mapIndex) : this(new Point3D(x, y, z), mapIndex) {
        }

        public MapPoint3D(Point3D point, int mapIndex) {
            Point = point;
            MapIndex = mapIndex;
        }

        public override string ToString() {
            return $"Point: {Point}  MapIndex: {MapIndex}";
        }
    }
}
