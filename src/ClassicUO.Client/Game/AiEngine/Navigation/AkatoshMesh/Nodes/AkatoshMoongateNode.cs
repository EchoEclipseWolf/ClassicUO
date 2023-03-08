using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AkatoshQuester.Helpers.Cartography;
using AkatoshQuester.Helpers.LightGeometry;
using ClassicUO.AiEngine;

namespace AkatoshQuester.Helpers.Nodes
{
    [Serializable]
    public class AkatoshMoongateNode : Node {
        private string _category;
        public AkatoshMoongateNode(BinaryReader reader) : base(reader)
        {
            Type = AkatoshNodeType.PublicMoongate;
        }

        public AkatoshMoongateNode(Point3D location, int mapIndex, Point3D endLocation, int endMapIndex)
            : base(location, mapIndex, endLocation, endMapIndex, AkatoshNodeType.PublicMoongate) {

        }

        public AkatoshMoongateNode(Point3D location, int mapIndex, Point3D endLocation, int endMapIndex, MeshGraph currentMesh)
            : this(location, mapIndex, endLocation, endMapIndex) {

            Navigation.LoadGridForPoint(location, mapIndex);
            if (!endLocation.Equals(Point3D.Empty)) {
                Navigation.LoadGridForPoint(endLocation, endMapIndex);
            }

            Id = currentMesh.CurrentMeshId;
            currentMesh.AddAndConnect(this, mapIndex);
        }


    }
}
