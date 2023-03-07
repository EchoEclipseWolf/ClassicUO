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
        public ConcurrentDictionary<int, Node> NodesById = new ConcurrentDictionary<int, Node>();


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
            NodesById.Clear();
        }

        public bool GridContainsPoint(Point3D point) {
            Node newNode = new AkatoshGroundNode(point, new Point3D(0, 0, 0));
            return NodesById.Values.Contains(newNode);
        }

        public bool AddNode(Node newNode)
        {
            if (newNode == null)
                return false;

            if (NodesById.Values.Contains(newNode)) {
                return false;
            }

            NodesById[newNode.Id] = newNode;
            CurrentMeshId++;

            return true;
        }

        public bool AddAndConnect(Node newNode, int searchDistance = 1, bool oneWay = false)
        {
            if (newNode == null)
                return false;

            var candidates = new HashSet<Node>();
            List<Node> pointsInRange = NodesWithinRange(newNode.Location, searchDistance);

            if (searchDistance == 0 && pointsInRange.Count == 1) {
                newNode.Id = pointsInRange[0].Id;
            } else {
                if (searchDistance == 0 && pointsInRange.Count == 0) {
                    pointsInRange = NodesWithinRange(newNode.Location, 1);
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
                var pointsInRangeEnd = NodesWithinRange(newNode.EndLocation, searchDistance);

                foreach (Node node in pointsInRangeEnd) {
                    if (!candidates.Contains(node) && (searchDistance == 0 || !node.Position.Equals(newNode.Location))) {
                        candidates.Add(node);
                    }

                }

                foreach (var akatoshMeshNode in candidates) {
                    newNode.AddLink(akatoshMeshNode, oneWay);
                }
            }

            NodesById[newNode.Id] = newNode;
            CurrentMeshId++;

            pointsInRange = NodesWithinRange(newNode.Location, searchDistance);

            return true;
        }

        public bool AddAndConnect(Point3D newPoint)
        {
            if (Equals(newPoint, Point3D.Empty))
                return false;

            var currentNode = Navigation.GetNode(newPoint);

            if (currentNode != null || GridContainsPoint(newPoint)) {

                if (currentNode != null) {
                    var candidatesExisting = new HashSet<Node>();

                    List<Node> pointsInRangeExisting =
                        NodesWithinRange(currentNode.Location, Navigation.SearchForNeighboursDistance);

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

            Node newNode = new AkatoshGroundNode(newPoint, new Point3D(0, 0, 0));
            

            newNode.Id = CurrentMeshId;

            NodesById[newNode.Id] = newNode;
            CurrentMeshId++;

            var candidates = new HashSet<Node>();

            List<Node> pointsInRange = NodesWithinRange(newNode.Location, Navigation.SearchForNeighboursDistance);

            foreach (Node node in pointsInRange) {
                if (!candidates.Contains(node) && !node.Position.Equals(newPoint)) {
                    candidates.Add(node);
                }

            }

            foreach (var akatoshMeshNode in candidates) {
                newNode.AddLink(akatoshMeshNode);
            }

            if (candidates.Count == 0) {
                GameActions.MessageOverhead($"Added link with 0 count", World.Player.Serial);
            }

            Console.WriteLine($"[Navigation]: Saved Point: Linked {candidates.Count}.");

            return true;
        }

        public Node AddNode(Point3D point)
        {
            Node newNode = new AkatoshGroundNode(point, new Point3D(0, 0, 0));

            return AddNode(newNode) ? newNode : null;
        }


        public Node ClosestNode(double PtX, double PtY, double PtZ, out double Distance, bool IgnorePassableProperty)
        {
            Node NodeMin = null;
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
            return NodeMin;
        }

        public Node ClosestNode(Vector3 point, bool ignorePassableProperty)
        {
            Node nodeMin = null;
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

            return nodeMin;
        }

        private List<Node> NodesAroundNode(Point3D point)
        {
            var list = new List<Node>();
            list.AddRange(Navigation.GetNodes(point.X, point.Y));
            list.AddRange(Navigation.GetNodes(point.X - 1, point.Y));
            list.AddRange(Navigation.GetNodes(point.X + 1, point.Y));
            list.AddRange(Navigation.GetNodes(point.X, point.Y - 1));
            list.AddRange(Navigation.GetNodes(point.X, point.Y + 1));
            list.AddRange(Navigation.GetNodes(point.X - 1, point.Y - 1));
            list.AddRange(Navigation.GetNodes(point.X - 1, point.Y + 1));
            list.AddRange(Navigation.GetNodes(point.X + 1, point.Y - 1));
            list.AddRange(Navigation.GetNodes(point.X + 1, point.Y + 1));

            return list;
        }

        public List<Node> NodesWithinRange(Point3D point, double distance)
        {
            Node nodeMin = null;
            var list = new List<Node>();

            foreach (var checkNode in NodesAroundNode(point))
            {
                int distanceTemp = (int)checkNode.Location.Distance2D(point);
                int heightDifference = (int)Math.Abs(point.Z - checkNode.Z);

                if (distanceTemp <= distance && heightDifference <= Navigation.SearchForNeighboursHeightDistance)
                {
                    list.Add(checkNode);
                }
            }

            return list;
        }

        public List<Node> GetPointsWithinDistance(Point3D from, float dist)
        {
            return NodesById.Values.Where(ntf => ntf.Location.Distance(from) < dist).ToList();
        }
    }
}