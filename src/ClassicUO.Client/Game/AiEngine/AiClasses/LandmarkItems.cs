using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AkatoshQuester.Helpers.LightGeometry;

namespace ClassicUO.Game.AiEngine.AiClasses
{
    internal class ChampionPlatform {
        public Point3D Point;
        public int MapIndex;
        public uint Serial;

        internal ChampionPlatform() {}

        internal ChampionPlatform(Point3D point, int mapIndex, uint serial)
        {
            Point = point;
            MapIndex = mapIndex;
            Serial = serial;
        }
    }
}
