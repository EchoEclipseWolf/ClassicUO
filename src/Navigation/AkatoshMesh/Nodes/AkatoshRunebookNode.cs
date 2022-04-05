using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AkatoshQuester.Helpers.Cartography;
using AkatoshQuester.Helpers.LightGeometry;

namespace AkatoshQuester.Helpers.Nodes
{
    [Serializable]
    public class AkatoshRunebookNode : Node
    {
        public AkatoshRunebookNode()
        {
            Type = AkatoshNodeType.Runebook;
        }

        public AkatoshRunebookNode(BinaryReader reader) : base(reader)
        {
            Type = AkatoshNodeType.Runebook;
        }

        public AkatoshRunebookNode(Point3D location, Point3D endLocation) 
            : base(location, endLocation, AkatoshNodeType.Ground)
        {
            
        }


    }
}
