using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using AkatoshQuester.Helpers.Cartography;
using AkatoshQuester.Helpers.LightGeometry;
using AkatoshQuester.Helpers.Nodes;
using ClassicUO.AiEngine;
using ClassicUO.Configuration;
using ClassicUO.DatabaseUtility;
using ClassicUO.Game;
using Newtonsoft.Json;
using RoyT.AStar;

namespace ClassicUO.NavigationNew.AkatoshMesh.New
{
    public class Pathfinder
    {
        internal class Step
        {
            public Step(StepType type, Point3D position, IReadOnlyList<Point3D> path)
            {
                this.Type = type;
                this.Position = position;
                this.Path = path;
            }

            public StepType Type { get; }
            public Point3D Position { get; }
            public IReadOnlyList<Point3D> Path { get; }
        }

        internal enum StepType
        {
            Current,
            Open,
            Close
        }

        internal static List<Step> StepList { get; } = new List<Step>(0);

        private static void MessageCurrent(Point3D position, IReadOnlyList<Point3D> path)
        {
            StepList.Add(new Step(StepType.Current, position, path));
        }

        private static void MessageOpen(Point3D position)
            => StepList.Add(new Step(StepType.Open, position, new List<Point3D>(0)));

        private static void MessageClose(Point3D position)
            => StepList.Add(new Step(StepType.Close, position, new List<Point3D>(0)));

        private static void ClearStepList()
            => StepList.Clear();

        private static List<Point3D> PartiallyReconstructPath(Point3D start, Point3D end, Point3D[] cameFrom)
        {
            var path = new List<Point3D> { end };
            return path;
        }

        public class PathCache {
            public int StartNodeId;
            public int EndNodeId;
            public int MaxPathCount;
            public List<Node> Path;

            public PathCache(int startingNodeId, int endNodeId, int maxPathCount, List<Node> path) {
                StartNodeId = startingNodeId;
                EndNodeId = endNodeId;
                MaxPathCount = maxPathCount;
                Path = path;
            }
        }

        public static List<PathCache> PathCaches = new List<PathCache>();
        private const int MaxPaths = 5000;

        public static void LoadWorld() {
            if (PathCaches == null) {
                return;
            }

            PathCaches.Clear();

            if (File.Exists(GetSavePath)) {
                var text = File.ReadAllText(GetSavePath);
                PathCaches = JsonConvert.DeserializeObject<List<PathCache>>(text);
            }
        }

        public static void SaveWorld() {
            if (File.Exists(GetSavePath)) {
                File.Delete(GetSavePath);
            }

            Directory.CreateDirectory(GetSavePathDir);

            File.WriteAllText(GetSavePath, JsonConvert.SerializeObject(PathCaches));
        }

        public static string GetSavePathDir
        {
            get
            {
                var mapName = MobileCache.MapNameFromIndex(World.MapIndex);

                var profilePath = ProfileManager.ProfilePath;
                return Path.Combine(profilePath, "Caches");
            }
        }

        public static string GetSavePath {
            get {
                var mapName = MobileCache.MapNameFromIndex(World.MapIndex);

                var profilePath = ProfileManager.ProfilePath;
                profilePath = Path.Combine(profilePath, "Caches");
                return Path.Combine(profilePath, $"{mapName}_pathCaches.cache");
            }
        }

        private static PathCache GetCachedPath(Node start, Node end, int maxSearchPath) {
            return PathCaches.FirstOrDefault(p => p.StartNodeId == start.Id && p.EndNodeId == end.Id && p.MaxPathCount == maxSearchPath);
        }

        public static void RemoveCachedPath(Node start, Node end) {
            var path = new List<PathCache>();

            if (start == null || end == null) {
                return;  
            }

            foreach (var pathCach in PathCaches) {
                if (pathCach.StartNodeId == start.Id && pathCach.EndNodeId == end.Id) {
                    continue;
                }

                path.Add(pathCach);
            }

            PathCaches = path;
        }

        private static PathCache _lastCache;

        public static void Clear()
        {
            _lastCache = null;
        }

        public static void RemoveCurrentCache() {
            if(_lastCache == null) {
                return;
            }

            var newList = new List<PathCache>();
            foreach(var pathCache in PathCaches) {
                if(pathCache.StartNodeId == _lastCache.StartNodeId && pathCache.EndNodeId == _lastCache.EndNodeId && pathCache.MaxPathCount == _lastCache.MaxPathCount) {
                    continue;
                }

                newList.Add(pathCache);
            }

            PathCaches.Clear();
            PathCaches.AddRange(newList);

            Clear();
        }

        public static List<Node> FindPath(Node start, Node end, int maxSearch = 10000, bool saveToCache = true) {
            ClearStepList();
            SearchedIds.Clear();

            var cachedPath = GetCachedPath(start, end, maxSearch);
            if (cachedPath != null) {
                _lastCache = cachedPath;
                 return cachedPath.Path;
            }

            if (Equals(start, end)) {
                return new List<Node> {end};
            }

            if (start.LinkedIds.Contains(end.Id)) {
                return new List<Node> {end};
            }

            var head = new MinHeapNode(start, null, ManhattanDistance(start.Location, end.Location));
            var open = new MinHeap();
            open.Push(head);

            var costSoFar = new Dictionary<int, double>();
            int count = 0;

            while (open.HasNext() && count <= maxSearch) {
                ++count;

                var current = open.Pop();
                if (current.PositionHash == end.PositionHash) {
                    var listSuccesful = new List<Node>();
                    var currentSuc = current;
                    listSuccesful.Add(currentSuc.Node);

                    for (int i = 0; i < maxSearch; i++) {
                        if (currentSuc.Previous != null) {
                            listSuccesful.Add(currentSuc.Node);
                            currentSuc = currentSuc.Previous;
                        } else {
                            break;
                        }
                    }

                    listSuccesful.Reverse();

                    if (saveToCache) {
                        PathCaches.Add(new PathCache(start.Id, end.Id, maxSearch, listSuccesful));

                        while (PathCaches.Count > MaxPaths) {
                            PathCaches.RemoveAt(0);
                        }

                        Navigation.NavigationNeedsSaving = true;

                        /*foreach (var node in listSuccesful) {
                            Navigation.MapGraphic.DrawRectangle(new Pen(Color.FromArgb(255, 100, 200, 0), 0), (int)node.X, (int)node.Y, 1, 1);
                        }
    
                        Navigation.MapGraphic.DrawRectangle(new Pen(Color.HotPink, 0), (int)start.X, (int)start.Y, 1, 1);
                        Navigation.MapGraphic.DrawRectangle(new Pen(Color.HotPink, 0), (int)end.X, (int)end.Y, 1, 1);
    
                        Navigation.SaveMapGraphic();*/
                    }

                    return listSuccesful;
                }

                StepNext(open, costSoFar, current, end);
            }

            if (saveToCache) {
                PathCaches.Add(new PathCache(start.Id, end.Id, maxSearch, new List<Node>()));
                while (PathCaches.Count > MaxPaths) {
                    PathCaches.RemoveAt(0);
                }

                Navigation.NavigationNeedsSaving = true;
                /*Navigation.NavigationNeedsSaving = true;
                Navigation.MapGraphic.DrawRectangle(new Pen(Color.Magenta, 0), (int)end.X, (int)end.Y, 1, 1);
                Navigation.SaveMapGraphic();*/
            }

            return new List<Node>();
        }

        private static HashSet<int> SearchedIds = new HashSet<int>();

        private static void StepNext(MinHeap open, Dictionary<int, double> costSoFar, MinHeapNode current, Node end) {
            var initialCost = 0.0;
            costSoFar.TryGetValue(current.PositionHash, out initialCost);

            var bestCost = 999999.0;

            Navigation. LoadGridForPoint(current.Node.Position, true);
            foreach (var nodeLinkedFile in current.Node.LinkedFiles) {
                Navigation.LoadGridForFilePoint(nodeLinkedFile);
            }

            foreach (var linkedId in current.Node.LinkedIds) {
                if (!Navigation.CurrentMesh.NodesById.TryGetValue(linkedId, out var linkedNode)) {
                    continue;
                }

                if (!linkedNode.Passable) {
                    //Navigation.MapGraphic.DrawRectangle(new Pen(Color.Red, 0), (int)linkedNode.X, (int)linkedNode.Y, 1, 1);
                    continue;
                }

                if (SearchedIds.Contains(linkedId)) {
                    // Navigation.MapGraphic.DrawRectangle(new Pen(Color.FromArgb(255, 80, 0, 200), 0), (int)linkedNode.X, (int)linkedNode.Y, 1, 1);
                    continue;
                }

                foreach (var nodeLinkedFile in linkedNode.LinkedFiles) {
                    Navigation.LoadGridForFilePoint(nodeLinkedFile);
                }

                SearchedIds.Add(linkedId);

                var cost = current.Node.Location.Distance(linkedNode.Location);
                if (linkedNode is AkatoshTeleporterNode) {
                    cost += 5.0;
                }

                var isEndNode = linkedNode.PositionHash == end.PositionHash;

                var newCost = initialCost + (cost * 5);
                if (newCost > bestCost && !isEndNode) {
                    //continue;
                }

                if (isEndNode) {
                    int bob = 1;
                }

                bestCost = newCost;
                costSoFar[linkedNode.PositionHash] = newCost;

                var expectedCost = newCost + ManhattanDistance(linkedNode.Location, end.Location);
                open.Push(new MinHeapNode(linkedNode, current, expectedCost));

                //Navigation.MapGraphic.DrawRectangle(new Pen(Color.FromArgb(50, 0, 200, 200), 0), (int)linkedNode.X, (int)linkedNode.Y, 1, 1);

            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double ManhattanDistance(Point3D p0, Point3D p1) {
            var dx = Math.Abs(p0.X - p1.X);
            var dy = Math.Abs(p0.Y - p1.Y);
            return dx + dy;
        }
    }
}
