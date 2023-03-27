using AkatoshQuester.Helpers.LightGeometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Game.AiEngine.AiClasses
{
    public class AiHouse
    {
        public readonly Point3D TopLeft = Point3D.Empty;
        public readonly Point3D BottomRight = Point3D.Empty;
        public readonly Point3D MiddlePoint = Point3D.Empty;
        public int MapIndex;
        public Dictionary<uint, AiContainer> Containers = new();

        public AiHouse() {

        }

        public AiHouse(Point3D topLeft, Point3D bottomRight, Point3D middlePoint, int topLeftMapIndex) {
            if (topLeft.Equals(Point3D.Empty) || bottomRight.Equals(Point3D.Empty)) {
                MapIndex = -1;
                return;
            }

            TopLeft = topLeft;
            BottomRight = bottomRight;
            MiddlePoint = middlePoint;
            MapIndex = topLeftMapIndex;
        }

        public bool IsValid() {
            return !TopLeft.Equals(Point3D.Empty) && !BottomRight.Equals(Point3D.Empty) && !MiddlePoint.Equals(Point3D.Empty) && MapIndex >= 0;
        }

        internal bool IsInsideHouseRegion(Point3D point) {
            if (point.X < TopLeft.X || point.X > BottomRight.X || point.Y < TopLeft.Y || point.Y > BottomRight.Y) {
                return false;
            }

            return true;
        }

        internal void ResetHouseContainers() {
            Containers.Clear();
        }

        internal void AddContainerToHouse(AiContainer currentContainer) {
            Containers[currentContainer.Serial] = currentContainer;
        }

        internal AiContainer FindSubContainerBySerial(uint serial) {
            return Containers.Values.Select(subContainer => subContainer.FindSubContainerBySerial(serial)).FirstOrDefault(found => found != null);
        }
    }
}
