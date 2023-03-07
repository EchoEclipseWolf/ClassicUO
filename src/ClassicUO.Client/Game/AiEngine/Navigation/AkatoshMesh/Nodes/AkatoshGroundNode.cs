using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AkatoshQuester.Helpers.Cartography;
using AkatoshQuester.Helpers.LightGeometry;
using ClassicUO;
using ClassicUO.AiEngine;
using ClassicUO.Game;
using Microsoft.Xna.Framework;

namespace AkatoshQuester.Helpers.Nodes
{
    [Serializable]
    public class AkatoshGroundNode : Node
    {
        public AkatoshGroundNode()
        {
            Type = AkatoshNodeType.Ground;
        }

        public AkatoshGroundNode(BinaryReader reader) : base(reader)
        {
            Type = AkatoshNodeType.Ground;
        }

        public AkatoshGroundNode(Point3D location, Point3D endLocation) 
            : base(location, endLocation, AkatoshNodeType.Ground)
        {
            
        }

        public override async Task<bool> Run() {
            var direction = Navigation.DirectionForNextPosition(new Vector2((ushort)Location.X, (ushort)Location.Y),
                new Vector2(World.Player.X, World.Player.Y));

            if (!World.Player.Walk(direction, true)) {
                return false;
            }

            return true;
        }
    }
}
