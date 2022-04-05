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
    public class AkatoshTeleporterNode : AkatoshGroundNode {
        public AkatoshTeleporterNode(BinaryReader reader) : base(reader) {
            Type = AkatoshNodeType.Teleport;
        }

        public AkatoshTeleporterNode(Point3D location, Point3D endLocation)
            : base(location, endLocation) {
            Type = AkatoshNodeType.Teleport;
        }

        public AkatoshTeleporterNode(Point3D location, Point3D endLocation, MeshGraph currentMesh)
            : this(location, endLocation) {

            Navigation.LoadGridForPoint(location);
            if (!endLocation.Equals(Point3D.Empty)) {
                Navigation.LoadGridForPoint(endLocation);
            }

            var currentNode = Navigation.GetNode(location);
            if (currentNode != null && currentNode.Type == AkatoshNodeType.Teleport) {
                return;
            }

            Id = currentMesh.CurrentMeshId;
            currentMesh.AddAndConnect(this, 0, true);
            Navigation.NavigationNeedsSaving = true;
        }

        public override async Task<bool> Run() {
            var direction = Navigation.DirectionForNextPosition(new Vector2((ushort)Location.X, (ushort)Location.Y),
                                                                new Vector2(World.Player.X, World.Player.Y));

            if (!World.Player.Walk(direction, true)) {
                return false;
            }

            await Task.Delay(50);
            Navigation.StopNavigation();
            return true;
        }
    }
}
