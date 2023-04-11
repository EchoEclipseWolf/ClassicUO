// Copyright 2003 Eric Marchesin - <eric.marchesin@laposte.net>
//
// This source file(s) may be redistributed by any means PROVIDING they
// are not sold for profit without the authors expressed written consent,
// and providing that this notice and the authors name and all copyright
// notices remain intact.
// THIS SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED. USE IT AT YOUR OWN RISK. THE AUTHOR ACCEPTS NO
// LIABILITY FOR ANY DATA DAMAGE/LOSS THAT THIS PRODUCT MAY CAUSE.
//-----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AkatoshQuester.Helpers.LightGeometry;
using AkatoshQuester.Helpers.Nodes;
using ClassicUO.AiEngine;
using ClassicUO.Game;
using Microsoft.Xna.Framework;

namespace AkatoshQuester.Helpers.Cartography
{
    [Serializable]
    public class MeshGraph
    {
        public int CurrentMeshId { get; set; }
        public ConcurrentDictionary<long, MeshGrid> Grids = new ();


        public MeshGraph(BinaryReader reader) {
            CurrentMeshId = reader.ReadInt32();
        }

        public MeshGraph() {

        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(CurrentMeshId);
        }

        public void Clear()
        {
            //NodesById.Clear();
        }

        public Node FindByNodeLink(NodeLink nodeLink) {
            var grid = GetGridByFilePoint(nodeLink.FilePoint, nodeLink.MapIndex);

            if (grid != null) {
                if (grid.Points.TryGetValue(nodeLink.XYHash, out var nodes)) {
                    var node = nodes.FirstOrDefault(n => n.Id == nodeLink.Id);
                    return node;
                }
            }

            return null;
        }

        public Node FindById(long id) {
            foreach (var grid in Grids) {
                foreach (var valuePoint in grid.Value.Points) {
                    foreach (Node node in valuePoint.Value) {
                        if(node.Id == id) 
                            return node;
                    }
                }
            }

            return null;
        }

        public MeshGrid GetGridByPosition(Point3D point, int mapIndex) {
            var filePoint = Navigation.GetFilePointFromPoint(point);

            return GetGridByFilePoint(filePoint, mapIndex);
        }

        public MeshGrid GetGridByFilePoint(Point2D filePoint, int mapIndex) {
            var gridHash = MeshGrid.GetHash(filePoint, mapIndex);

            if (Grids.TryGetValue(gridHash, out var grid)) {
                return grid;
            }

            var newGrid = new MeshGrid(filePoint, mapIndex);
            Grids[gridHash] = newGrid;
            return newGrid;
        }

        public bool GridContainsPoint(Point3D point) {
            return false;
            //Node newNode = new AkatoshGroundNode(point, new Point3D(0, 0, 0));
            //return NodesById.Values.Contains(newNode);
        }

        public bool AddAndConnect(Node newNode, int mapIndex, int searchDistance = 1, bool oneWay = false)
        {
            if (newNode == null)
                return false;

            var candidates = new HashSet<Node>();
            List<Node> pointsInRange = NodesWithinRange(newNode.Location, mapIndex, searchDistance);

            if (searchDistance == 0 && pointsInRange.Count == 1) {
                newNode.Id = pointsInRange[0].Id;
            } else {
                if (searchDistance == 0 && pointsInRange.Count == 0) {
                    pointsInRange = NodesWithinRange(newNode.Location, mapIndex, 1);
                }

                foreach (Node node in pointsInRange) {
                    if (!candidates.Contains(node) &&
                        (searchDistance == 0 || !node.Position.Equals(newNode.Location))) {
                        candidates.Add(node);
                    }

                }

                foreach (var akatoshMeshNode in candidates) {
                    newNode.AddLink(akatoshMeshNode);
                }
            }



            if (!Equals(newNode.EndLocation, Point3D.Empty)) {
                candidates.Clear();
                var pointsInRangeEnd = NodesWithinRange(newNode.EndLocation, mapIndex, searchDistance);

                foreach (Node node in pointsInRangeEnd) {
                    if (!candidates.Contains(node) && (searchDistance == 0 || !node.Position.Equals(newNode.Location))) {
                        candidates.Add(node);
                    }

                }

                foreach (var akatoshMeshNode in candidates) {
                    newNode.AddLink(akatoshMeshNode, oneWay);
                }
            }

            CurrentMeshId++;
            return true;
        }

        public bool AddAndConnect(Point3D newPoint, int mapIndex, int distance)
        {
            if (Equals(newPoint, Point3D.Empty))
                return false;

            var currentNode = Navigation.GetNode(newPoint, mapIndex, distance);

            if (currentNode != null || GridContainsPoint(newPoint)) {

                if (currentNode != null) {
                    var candidatesExisting = new HashSet<Node>();

                    List<Node> pointsInRangeExisting =
                        NodesWithinRange(currentNode.Location, mapIndex, Navigation.SearchForNeighboursDistance);

                    foreach (Node node in pointsInRangeExisting) {
                        if (!candidatesExisting.Contains(node) && !node.Position.Equals(newPoint)) {
                            candidatesExisting.Add(node);
                        }

                    }

                    foreach (var akatoshMeshNode in candidatesExisting) {
                        currentNode.AddLink(akatoshMeshNode);
                    }
                }

                return false;
            }

            Node newNode = new AkatoshGroundNode(newPoint, mapIndex, new Point3D(0, 0, 0), 0);
            
            newNode.Id = (int) Node.GetNodeHash(newPoint);
            var meshGrid = GetGridByPosition(newPoint, mapIndex);
            meshGrid.AddNode(newNode);
            //NodesById[newNode.Id] = newNode;
            CurrentMeshId++;

            var candidates = new HashSet<Node>();

            List<Node> pointsInRange = NodesWithinRange(newNode.Location, mapIndex, Navigation.SearchForNeighboursDistance);

            foreach (Node node in pointsInRange) {
                if (!candidates.Contains(node) && !node.Position.Equals(newPoint)) {
                    candidates.Add(node);
                }

            }

            foreach (var akatoshMeshNode in candidates) {
                newNode.AddLink(akatoshMeshNode);
            }

            if (candidates.Count == 0) {
                //GameActions.MessageOverhead($"Added link with 0 count", World.Player.Serial);
            }

            //Console.WriteLine($"[Navigation]: Saved Point: Linked {candidates.Count}.");
            return true;
        }

        public Node ClosestNode(double PtX, double PtY, double PtZ, out double Distance, bool IgnorePassableProperty)
        {
            /*Node NodeMin = null;
            double DistanceMin = -1;
            var P = new Point3D(PtX, PtY, PtZ);
            foreach (Node N in NodesById.Values)
            {
                if (IgnorePassableProperty && N.Passable == false) continue;
                double DistanceTemp = P.Distance(N.Position);
                if (DistanceMin == -1 || DistanceMin > DistanceTemp)
                {
                    DistanceMin = DistanceTemp;
                    NodeMin = N;
                }
            }
            Distance = DistanceMin;
            return NodeMin;*/
            Distance = 0;
            return null;
        }

        public Node ClosestNode(Vector3 point, bool ignorePassableProperty)
        {
            /*Node nodeMin = null;
            double distanceMin = -1;
            var p = new Point3D(point.X, point.Y, point.Z);
            foreach (Node n in NodesById.Values)
            {
                if (ignorePassableProperty && n.Passable == false) continue;
                double distanceTemp = p.Distance(n.Position);
                if (distanceMin == -1 || distanceMin > distanceTemp)
                {
                    distanceMin = distanceTemp;
                    nodeMin = n;
                }
            }

            return nodeMin;*/
            return null;
        }

        private List<Node> NodesAroundNode(Point3D point, int mapIndex)
        {
            var list = new List<Node>();
            list.Add(Navigation.GetNode(new Point3D(point.X, point.Y, point.Z), mapIndex));
            list.Add(Navigation.GetNode(new Point3D(point.X - 1, point.Y, point.Z), mapIndex));
            list.Add(Navigation.GetNode(new Point3D(point.X + 1, point.Y, point.Z), mapIndex));
            list.Add(Navigation.GetNode(new Point3D(point.X, point.Y - 1, point.Z), mapIndex));
            list.Add(Navigation.GetNode(new Point3D(point.X, point.Y + 1, point.Z), mapIndex));
            list.Add(Navigation.GetNode(new Point3D(point.X - 1, point.Y - 1, point.Z), mapIndex));
            list.Add(Navigation.GetNode(new Point3D(point.X - 1, point.Y + 1, point.Z), mapIndex));
            list.Add(Navigation.GetNode(new Point3D(point.X + 1, point.Y - 1, point.Z), mapIndex));
            list.Add(Navigation.GetNode(new Point3D(point.X + 1, point.Y + 1, point.Z), mapIndex));

            return list;
        }

        public List<Node> NodesWithinRange(Point3D point, int mapIndex, double distance)
        {
            Node nodeMin = null;
            var list = new List<Node>();

            foreach (var checkNode in NodesAroundNode(point, mapIndex))
            {
                if (checkNode == null) {
                    continue;
                }

                int distanceTemp = (int)checkNode.Location.Distance2D(point);
                int heightDifference = (int)Math.Abs(point.Z - checkNode.Z);

                if (distanceTemp <= distance && heightDifference <= Navigation.SearchForNeighboursHeightDistance)
                {
                    list.Add(checkNode);
                }
            }

            return list;
        }
    }
}