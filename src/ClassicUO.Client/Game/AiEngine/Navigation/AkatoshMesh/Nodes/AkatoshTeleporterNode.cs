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

        public AkatoshTeleporterNode(Point3D location, int mapIndex, Point3D endLocation, int endMapIndex)
            : base(location, mapIndex, endLocation, endMapIndex) {
            Type = AkatoshNodeType.Teleport;
        }

        public AkatoshTeleporterNode(Point3D location, int mapIndex, Point3D endLocation, int endMapIndex, MeshGraph currentMesh)
            : this(location, mapIndex, endLocation, endMapIndex) {

            Navigation.LoadGridForPoint(location, mapIndex);
            if (!endLocation.Equals(Point3D.Empty)) {
                Navigation.LoadGridForPoint(endLocation, endMapIndex);
            }

            var currentNode = Navigation.GetNode(location, mapIndex);
            if (currentNode != null && currentNode.Type == AkatoshNodeType.Teleport) {
                return;
            }

            Id = currentMesh.CurrentMeshId;
            currentMesh.AddAndConnect(this, mapIndex, 0, true);
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
