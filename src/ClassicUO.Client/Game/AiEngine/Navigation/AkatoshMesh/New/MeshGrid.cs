using AkatoshQuester.Helpers.LightGeometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AkatoshQuester.Helpers.Cartography;
using FastHashes;
using System.IO;
using AkatoshQuester.Helpers.Nodes;

namespace ClassicUO.AiEngine
{
    public class MeshGrid {
        public bool NeedsToSave;
        public Point2D FilePoint;
        public int MapIndex;
        public long Hash;
        public Dictionary<long, List<Node>> Points = new();

        public MeshGrid(Point2D point, int mapIndex)
        {
            FilePoint = point;
            MapIndex = mapIndex;
            Hash = GetHash(point, MapIndex);
        }

        public MeshGrid(BinaryReader reader) {
            FilePoint = new Point2D(reader.ReadInt16(), reader.ReadInt16());
            MapIndex = reader.ReadInt32();
            Hash = reader.ReadInt64();

            var count = reader.ReadInt32();

            for (int i = 0; i < count; i++) {
                var type = (AkatoshNodeType)reader.ReadInt32();
                Node node = null;

                switch (type) {
                    case AkatoshNodeType.Ground: {
                        node = new AkatoshGroundNode(reader);

                        break;
                    }

                    case AkatoshNodeType.Runebook: {
                        node = new AkatoshRunebookNode(reader);

                        break;
                    }

                    case AkatoshNodeType.PublicMoongate: {
                        node = new AkatoshMoongateNode(reader);

                        break;
                    }

                    case AkatoshNodeType.Teleport: {
                        node = new AkatoshTeleporterNode(reader);

                        break;
                    }
                }

                if (node == null) {
                    continue;
                }

                if (!Points.ContainsKey(node.XyHash))
                {
                    Points[node.XyHash] = new List<Node> {
                        node
                    };
                }
                else {
                    Points[node.XyHash].Add(node);
                }
            }
        }

        public void AddNode(Node node) {
            if (!Points.ContainsKey(node.XyHash)) {
                Points[node.XyHash] = new List<Node> {
                    node
                };
            }
            else {
                Points[node.XyHash].Add(node);
            }

            NeedsToSave = true;
        }

        public Node GetNodeByPosition(Point3D position, int distance = 10) {
            var hash = GetXYPositionHash(position, MapIndex);

            if (Points.TryGetValue(hash, out var nodes)) {
                if (nodes.Count == 1) {
                    var nodePosition = new Point3D(nodes[0].X, nodes[0].Y, nodes[0].Z);
                    if (nodePosition.Distance(position) > distance) {
                        return null;
                    }
                    return nodes.FirstOrDefault();
                } else if (nodes.Count > 1) {
                    Node bestNode = null;
                    double bestDistance = distance + 1;

                    foreach (Node node in nodes) {
                        var nodeDistance = Math.Abs(node.Z - position.Z);

                        if (nodeDistance < bestDistance) {
                            bestNode = node;
                            bestDistance = nodeDistance;
                        }
                    }
                    return bestNode;
                }
            }

            return null;
        }

        public void Save(BinaryWriter writer) {
            writer.Write((short)FilePoint.X);
            writer.Write((short)FilePoint.Y);
            writer.Write((int)MapIndex);
            writer.Write((long)Hash);


            var nodesToSave = new List<Node>();

            foreach (var points in Points.Values) {
                nodesToSave.AddRange(points);
            }

            writer.Write((int)nodesToSave.Count);
            foreach (var node in nodesToSave) {
                node.Save(writer);
            }
        }

        public static long GetXYPositionHash(Point3D position, int mapIndex) {

            var index = mapIndex * 486187739;

            var hashCode = (long)position.X;
            hashCode = (hashCode * 92821) ^ (long)position.Y;
            hashCode = (hashCode * 92821) ^ (long)index;

            return hashCode;
        }

        public static long GetHash(Point2D filePoint, int mapIndex) {
            if (filePoint == null) {
                return 0;
            }

            var index = mapIndex * 486187739;

            var hashCode = (long)filePoint.X;
            hashCode = (hashCode * 397) ^ (long)filePoint.Y;
            hashCode = (hashCode * 397) ^ (long)index;

            return hashCode;
        }

    }
}
