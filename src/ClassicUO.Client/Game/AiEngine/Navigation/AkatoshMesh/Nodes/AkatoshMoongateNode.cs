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

        public AkatoshMoongateNode(Point3D location, Point3D endLocation)
            : base(location, endLocation, AkatoshNodeType.PublicMoongate) {

        }

        public AkatoshMoongateNode(Point3D location, Point3D endLocation, MeshGraph currentMesh)
            : this(location, endLocation) {

            Navigation.LoadGridForPoint(location);
            if (!endLocation.Equals(Point3D.Empty)) {
                Navigation.LoadGridForPoint(endLocation);
            }

            Id = currentMesh.CurrentMeshId;
            currentMesh.AddAndConnect(this);
        }


    }
}
