using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AkatoshQuester.Helpers.Cartography;
using AkatoshQuester.Helpers.LightGeometry;
using AkatoshQuester.Helpers.Nodes;
using ClassicUO.Configuration;
using ClassicUO.DatabaseUtility;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.NavigationTravel.AkatoshMesh;
using Microsoft.Xna.Framework;
using Pathfinder = ClassicUO.NavigationNew.AkatoshMesh.New.Pathfinder;

namespace ClassicUO.AiEngine
{
    public static class Navigation {
        private static int _loadedMapIndex = -1;

        public static AkatoshTravelSystem TravelSystem = new AkatoshTravelSystem();

        public static bool MapUpdating = false;
        public static bool NavigationNeedsSaving = false;
        private static int _failedCount = 0;

        private static double _previousX = 0;
        private static double _previousY = 0;
        private static double _previousZ = 0;
        private static int _previousMapIndex = -1;

        private static List<Point2D> LoadedNavigationFiles = new List<Point2D>();

        public static List<Node> Path = new List<Node>();

        public static float SearchForNeighboursDistance => 1F;
        public static float SearchForNeighboursHeightDistance => 10F;

        public static MeshGraph CurrentMesh { get; set; }

        public static bool IsNavigationBusy {
            get { return MapUpdating || CurrentMesh == null || World.Player == null; }
        }

        public static Point3D ToPoint3D(this Vector3 position) {
            return new Point3D(position.X, position.Y, position.Z);
        }

        public static string GetMasterSavePath {
            get {
                var mapName = MobileCache.MapNameFromIndex(World.MapIndex);

                var profilePath = "Navigation";
                return System.IO.Path.Combine(profilePath, $"{mapName}_Master.grid");
            }
        }

        public static string GetSubSavePath(Point2D filePoint) {
            var mapName = MobileCache.MapNameFromIndex(World.MapIndex);

            var profilePath = "Navigation";
            return System.IO.Path.Combine(profilePath, $"{mapName}_{filePoint.X}_{filePoint.Y}.grid");
        }

        public static Graphics MapGraphic;
        public static Bitmap MapBitmap;

        public static void SaveMapGraphic() {
            MapGraphic.Dispose();
            MapBitmap.Save("Rendered.png", ImageFormat.Png);
        }

        public static async Task<bool> LoadCurrentMap() {
            if (_loadedMapIndex == World.MapIndex || World.Player == null || string.IsNullOrEmpty(World.Player.Name)) {
                MapUpdating = false;
                return false;
            }

            CurrentMesh?.Clear();

            LoadedNavigationFiles.Clear();

            var stopwatch = Stopwatch.StartNew();

            MapUpdating = true;
            var mapName = MobileCache.MapNameFromIndex(World.MapIndex);
            _loadedMapIndex = World.MapIndex;
            //GameActions.MessageOverhead("Loading current navigation map", World.Player.Serial);

            if (File.Exists(GetMasterSavePath)) {
                using (var stream = new FileStream(GetMasterSavePath, FileMode.Open)) {
                    using (var reader = new BinaryReader(stream)) {
                        CurrentMesh = new MeshGraph(reader);
                    }
                }
            } else {
                CurrentMesh = new MeshGraph();
            }

            var origmap = Image.FromFile("Maps\\map1.png");
            MapBitmap = new Bitmap(origmap.Width, origmap.Height);
            using (Graphics g1 = Graphics.FromImage(MapBitmap))
            {
                g1.DrawImage(origmap, 0, 0);
                //g1.DrawRectangle(new Pen(Color.Aqua));
            }

            MapGraphic = Graphics.FromImage(MapBitmap);

            /* var fileList = Directory.GetFiles(Profile.ProfilePath).ToList();
             foreach (var filePath in fileList) {
                 if (filePath.Contains($"{MobileCache.MapNameFromIndex(World.MapIndex)}_") && !filePath.Contains("aster")) {
                     using (var stream = new FileStream(filePath, FileMode.Open)) {
                         using (var reader = new BinaryReader(stream)) {
                             var count = reader.ReadInt32();
                             for (int i = 0; i < count; i++) {
                                 try {
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

                                     //var node = new Node(reader);
                                     if (node == null) {
                                         continue;
                                     }

                                     CurrentMesh.NodesById[node.Id] = node;
                                 } catch (Exception e) {
                                     int bob = 1;
                                 }
                             }
                         }
                     }
                 }
             }*/


            /*foreach (var node in CurrentMesh.NodesById.Values)
            {
                if (node.Passable) {
                    g.DrawRectangle(new Pen(Color.FromArgb(50, 0, 200, 200), 0), (int) node.X, (int) node.Y, 1, 1);
                } else {
                    g.DrawRectangle(new Pen(Color.Red, 0), (int)node.X, (int)node.Y, 1, 1);
                }

                //g.DrawImage(pin, new Point((int)(x - (pin.Width / 2.0f)), (int)(y - (pin.Height / 2.0f))));
            }*/

            //


            /*foreach (var nodePair in CurrentMesh.NodesById) {
                nodePair.Value.UpdateLinked();
            }

            var dict = new Dictionary<Point2D, List<Node>>();
            foreach (var node in CurrentMesh.NodesById.Values) {
                var point = GetFilePointFromPoint(
                    new Point3D(node.Position.X, node.Position.Y, node.Position.Z));
                if (!dict.ContainsKey(point)) {
                    dict[point] = new List<Node>();
                }

                dict[point].Add(node);
            }

            foreach (var pair in dict) {
                var filePoint = pair.Key;

                using (FileStream stream = new FileStream(GetSubSavePath(filePoint), FileMode.Create)) {
                    using (BinaryWriter writer = new BinaryWriter(stream)) {
                        writer.Write(pair.Value.Count);
                        foreach (var node in pair.Value) {
                            node.Save(writer);
                        }
                    }
                }
            }*/

            Clear();



            if (File.Exists(GetMasterSavePath)) {
                using (var stream = new FileStream(GetMasterSavePath, FileMode.Open)) {
                    using (var reader = new BinaryReader(stream)) {
                        CurrentMesh = new MeshGraph(reader);
                    }
                }
            } else {
                CurrentMesh = new MeshGraph();
            }

            if (World.MapIndex == 1) {
                //New Haven Teleporter
                new AkatoshTeleporterNode(new Point3D(3490, 2565, 40), new Point3D(3493, 2565, 15), CurrentMesh); // New Haven Bank Top To Bottom
                new AkatoshTeleporterNode(new Point3D(3493, 2565, 15), new Point3D(3490, 2565, 40), CurrentMesh); // New Haven Bank Bottom To Top


                //------------//
                //--Dungeons--//
                //------------//

                //SHAME
                new AkatoshTeleporterNode(new Point3D(513, 1559, 0), new Point3D(5395, 126, 0), CurrentMesh); // Shame World To Dungeon
                new AkatoshTeleporterNode(new Point3D(5395, 127, 0), new Point3D(513, 1560, 0), CurrentMesh); // Shame Dungeon To World
                new AkatoshTeleporterNode(new Point3D(512, 1559, 0), new Point3D(5394, 126, 0), CurrentMesh); // Shame World To Dungeon
                new AkatoshTeleporterNode(new Point3D(5394, 127, 0), new Point3D(512, 1560, 0), CurrentMesh); // Shame Dungeon To World
                new AkatoshTeleporterNode(new Point3D(514, 1559, 0), new Point3D(5396, 126, 0), CurrentMesh); // Shame World To Dungeon
                new AkatoshTeleporterNode(new Point3D(5396, 127, 0), new Point3D(514, 1560, 0), CurrentMesh); // Shame Dungeon To World

                new AkatoshTeleporterNode(new Point3D(5490, 19, -25), new Point3D(5515, 10, 5), CurrentMesh); // Shame Level 1 To Level 2
                new AkatoshTeleporterNode(new Point3D(5514, 10, 5), new Point3D(5489, 19, -25), CurrentMesh); // Shame Level 2 To Level 1




                //------------//
                //--Blacklist--//
                //------------//
                SetPassableArea(new Point3D(3490, 2563, 15), new Point3D(3495, 2568, 8), false);
                SetPassableArea(new Point3D(1437, 1680, 10), new Point3D(1444, 1693, 0), false);
            }

            Pathfinder.LoadWorld();

            // GameActions.MessageOverhead(
            //     $"Finished: Loading current navigation map in {stopwatch.ElapsedMilliseconds}ms.", World.Player.Serial);
            MapUpdating = false;

            return false;
        }

        private static void SetPassableArea(Point3D start, Point3D end, bool passable) {
            LoadGridForPoint(start);
            LoadGridForPoint(end);

            for (var y = start.Y; y < end.Y; y++) {
                for (var x = start.X; x < end.X; x++) {
                    var point = new Point3D(x, y, 0);
                    LoadGridForPoint(point);

                    var node = GetNode(point, 100);
                    if (node != null) {
                        if (node.Passable != passable) {
                            node.Passable = passable;
                            NavigationNeedsSaving = true;
                        }
                    }
                }
            }
        }

        public static Point2D GetFilePointFromPoint(Point3D point) {
            var fileSize = 400.0;
            return new Point2D((int) Math.Ceiling(point.X / fileSize), (int) Math.Ceiling(point.Y / fileSize));
        }

        public static bool IsGridLoadedForPoint(Point2D point) {
            return LoadedNavigationFiles.Contains(new Point2D(point.X, point.Y));
        }

        public static void LoadGridForPoint(Point3D point, bool loadNeighbors = false)
        {
            var filePoint = GetFilePointFromPoint(point);

            LoadGridForFilePoint(filePoint);

            if (loadNeighbors) {
                LoadGridForFilePoint(new Point2D(filePoint.X - 1, filePoint.Y));
                LoadGridForFilePoint(new Point2D(filePoint.X + 1, filePoint.Y));
                LoadGridForFilePoint(new Point2D(filePoint.X, filePoint.Y - 1));
                LoadGridForFilePoint(new Point2D(filePoint.X, filePoint.Y + 1));
                LoadGridForFilePoint(new Point2D(filePoint.X - 1, filePoint.Y - 1));
                LoadGridForFilePoint(new Point2D(filePoint.X - 1, filePoint.Y + 1));
                LoadGridForFilePoint(new Point2D(filePoint.X + 1, filePoint.Y - 1));
                LoadGridForFilePoint(new Point2D(filePoint.X + 1, filePoint.Y + 1));
            }
        }

        public static void LoadGridForFilePoint(Point2D filePoint) {
            try {
                if (IsGridLoadedForPoint(filePoint) || !File.Exists(GetSubSavePath(filePoint))) {
                    return;
                }

                using (var stream = new FileStream(GetSubSavePath(filePoint), FileMode.Open)) {
                    using (var reader = new BinaryReader(stream)) {
                        var count = reader.ReadInt32();
                        for (int i = 0; i < count; i++) {
                            var type = (AkatoshNodeType)reader.ReadInt32();
                            Node node = null;

                            switch (type)
                            {
                                case AkatoshNodeType.Ground:
                                {
                                    node = new AkatoshGroundNode(reader);
                                    break;
                                }
                                case AkatoshNodeType.Runebook:
                                {
                                    node = new AkatoshRunebookNode(reader);
                                    break;
                                }
                                case AkatoshNodeType.PublicMoongate:
                                {
                                    node = new AkatoshMoongateNode(reader);
                                    break;
                                }
                                case AkatoshNodeType.Teleport:
                                {
                                    node = new AkatoshTeleporterNode(reader);
                                    break;
                                }
                            }

                            if (node == null) {
                                continue;
                            }

                            CurrentMesh.NodesById[node.Id] = node;
                        }
                    }
                }

                //GameActions.MessageOverhead($"Loaded Grid: {filePoint.X} : {filePoint.Y}", World.Player.Serial);
                LoadedNavigationFiles.Add(filePoint);
            } catch (Exception e) {
                int bob = 1;
            }
        }

        public static async Task<bool> SaveGrid() {
            MapUpdating = true;
            GameActions.MessageOverhead("Saving current navigation map", World.Player.Serial);
            var mapName = MobileCache.MapNameFromIndex(_loadedMapIndex);

            if (File.Exists(GetMasterSavePath)) {
                File.Delete(GetMasterSavePath);
            }
            
            using (FileStream stream = new FileStream(GetMasterSavePath, FileMode.Create)) {
                using (BinaryWriter writer = new BinaryWriter(stream)) {
                    CurrentMesh.Save(writer);
                }
            }

            var dict = new Dictionary<Point2D, List<Node>>();
            foreach (var node in CurrentMesh.NodesById.Values) {
                var point = GetFilePointFromPoint(new Point3D(node.Position.X, node.Position.Y, node.Position.Z));
                if (!dict.ContainsKey(point)) {
                    dict[point] = new List<Node>();
                }

                dict[point].Add(node);
            }

            foreach (var pair in dict) {
                var filePoint = pair.Key;
                using (FileStream stream = new FileStream(GetSubSavePath(filePoint), FileMode.Create)) {
                    using (BinaryWriter writer = new BinaryWriter(stream)) {
                        writer.Write(pair.Value.Count);
                        foreach (var node in pair.Value) {
                            node.Save(writer);
                        }
                    }
                }
            }


            GameActions.MessageOverhead("Finished: saving current navigation map", World.Player.Serial);
            NavigationNeedsSaving = false;
            MapUpdating = false;

            GC.Collect();
            return true;
        }

        internal static async Task<bool> AddWalkableTile(Vector3 point) {
            LoadGridForPoint(new Point3D(point.X, point.Y, point.Z));

            CurrentMesh.AddAndConnect(
                new Point3D(point.X, point.Y, point.Z));
            NavigationNeedsSaving = true;
            return false;
        }

        public static Node GetNode(Point3D from, int distance = 10) {
            //var node = CurrentMesh.NodesWithinRange(from, 0).FirstOrDefault(ntf => ntf.Passable);
            try {
                return CurrentMesh.NodesById.Values.FirstOrDefault(n => n.X == from.X && n.Y == from.Y && Math.Abs(n.Z - from.Z) <= distance);
            } catch (Exception e) {
                return null;
            }
        }

        public static List<Node> GetNodes(double x, double y) {
            return CurrentMesh.NodesById.Values.Where(n => n.X == x && n.Y == y).ToList();
        }

        public static async Task<bool> CanFullyNavigateTo(Point3D point, int distance, int mapIndex, int maxSearchPath = 10000, bool saveToCache = true) {
            if (IsNavigationBusy) {
                return false;
            }

            if (CurrentMesh == null) {
                return false;
            }

            LoadGridForPoint(new Point3D(World.Player.Position.X, World.Player.Position.Y,
                    World.Player.Position.Z));
            LoadGridForPoint(new Point3D(point.X, point.Y, point.Z));

            var stopwatch = Stopwatch.StartNew();

            var startingNode = GetNode(World.Player.Position.ToPoint3D());
            if (startingNode == null)
            {
                GameActions.Print($"[Navigation]: False Start.");
                return false;
            }

            var endingNode = GetNode(new Point3D(point.X, point.Y, point.Z));
            if (endingNode == null)
            {
                GameActions.Print($"[Navigation]: False End.");
                return false;
            }

            var pathFinder = Pathfinder.FindPath(startingNode, endingNode, maxSearchPath, saveToCache);
            return pathFinder.Count > 0;
        }

        internal static async Task<bool> NavigateTo(Mobile mob) {
            return await NavigateTo(mob.Position.ToPoint3D(), World.MapIndex, false, false);
        }

        public static async Task<bool> NavigateTo(Point3D point) {
            return await NavigateTo(point, World.MapIndex);
        }

        public static async Task<bool> NavigateTo(Point3D point, int mapIndex, bool useTravelSystem = true, bool saveToCache = true) {
            if (IsNavigationBusy) {
                return true;
            }

            if (CurrentMesh == null) {
                return false;
            }

           

            if (Path.Count == 0 || _previousX != point.X || _previousY != point.Y || _previousZ != point.Z ||
                _previousMapIndex != mapIndex) {

                var stopwatch = Stopwatch.StartNew();

                if (useTravelSystem) {
                    var travelMethod = await TravelSystem.TravelSystemToUse(point, mapIndex);
                    if (travelMethod != AkatoshTravelSystem.TravelSystemMode.Walk) {
                        _previousX = point.X;
                        _previousY = point.Y;
                        _previousZ = point.Z;
                        _previousMapIndex = mapIndex;

                        await TravelSystem.NavigateTo(travelMethod, point, mapIndex);
                        return true;
                    }
                }

                LoadGridForPoint(new Point3D(World.Player.Position.X, World.Player.Position.Y,
                    World.Player.Position.Z));
                LoadGridForPoint(new Point3D(point.X, point.Y, point.Z));

                var startingNode = GetNode(World.Player.Position.ToPoint3D());

                if (startingNode.Type == AkatoshNodeType.Teleport) {
                    startingNode = CurrentMesh.NodesWithinRange(World.Player.Position.ToPoint3D(), 1).FirstOrDefault(n =>
                        n != null && n.Type != AkatoshNodeType.Teleport &&
                        n.Position.Distance(World.Player.Position.ToPoint3D()) != 0);
                }


                if (startingNode == null) {
                    GameActions.Print($"[Navigation]: False Start.");
                    Path.Clear();
                    await Task.Delay(1000);
                    return false;
                }

                var endingNode = GetNode(new Point3D(point.X, point.Y, point.Z));
                if (endingNode == null) {
                    GameActions.Print($"[Navigation]: False End.");
                    Path.Clear();
                    await Task.Delay(1000); 
                    return false;
                }

                /*var found = pathGeneration.SearchPath(startingNode, endingNode, 10000);
                if (!found) {
                    GameActions.Print($"[Navigation]: False Failed to find path.");
                    return false;
                }*/

                var newPath = Pathfinder.FindPath(startingNode, endingNode, 500000, saveToCache);
                if (newPath.Count == 0) {
                    GameActions.Print($"[Navigation]: Failed to find path");
                    await Task.Delay(1000);
                    return false;
                }

                Path = newPath;

                if (Path.Count > 10) {
                    GameActions.Print($"[Navigation]: Path Found: {Path.Count} points  Time: {stopwatch.ElapsedMilliseconds}");
                }
            }

            _previousX = point.X;
            _previousY = point.Y;
            _previousZ = point.Z;
            _previousMapIndex = mapIndex;

            if (Path.Count > 0) {
                await ProcessAutoWalk();
            } else {
                StopNavigation();
            }

            return true;
        }

        public static async Task<List<Node>> GetPath(Point3D start, Point3D end, int maxSearch = 10000) {
            var list = new List<Node>();

            LoadGridForPoint(new Point3D(start.X, start.Y, start.Z));
            LoadGridForPoint(new Point3D(end.X, end.Y, end.Z));

            var startingNode = GetNode(start);

            if (startingNode == null)
            {
                //GameActions.Print($"[Navigation]: False Start.");
                return list;
            }

            if (startingNode.Type == AkatoshNodeType.Teleport) {
                startingNode = CurrentMesh.NodesWithinRange(start, 1).FirstOrDefault(n => n != null && n.Type != AkatoshNodeType.Teleport && n.Position.Distance(World.Player.Position.ToPoint3D()) != 0);
            }

            var endingNode = GetNode(new Point3D(end.X, end.Y, end.Z));
            if (endingNode == null) {
                //GameActions.Print($"[Navigation]: False End.");
                return list;
            }

            var newPath = Pathfinder.FindPath(startingNode, endingNode, maxSearch);

            list.AddRange(newPath);

            return list;
        }

        public static bool StopNavigation()
        {
            _previousNode = null;
            Path.Clear();
            _previousX = 0;
            _previousY = 0;
            _previousZ = 0;
            _previousMapIndex = -1;
            Pathfinder.Clear();

            return false;
        }

        public static void Clear() {
            Path.Clear();
            LoadedNavigationFiles.Clear();
            CurrentMesh.Clear();

            _previousX = 0;
            _previousY = 0;
            _previousZ = 0;
            _previousMapIndex = -1;

            Pathfinder.Clear();

            GC.Collect();

            GameActions.MessageOverhead($"Cleared Navigation", World.Player.Serial);
        }

        private static Node _previousNode;
        private static Node _previousBlocked;

        private static async Task<bool> ProcessAutoWalk() {
            if (Path.Count > 0 && World.InGame) {
                if (Path.Count > 0) {
                    if (_previousNode == null) {
                        _previousNode = Path[0];
                    }
                    
                    Path = Path.Where(a =>
                                          Vector2.Distance(new Vector2((ushort) a.X, (ushort) a.Y), new Vector2(World.Player.X, World.Player.Y)) != 0).ToList();
                    Path = Path.Where(a => a.EndLocation == null || a.EndLocation.Equals(Point3D.Empty) ||
                                           Vector2.Distance(new Vector2((ushort)a.EndLocation.X, (ushort)a.EndLocation.Y), new Vector2(World.Player.X, World.Player.Y)) != 0).ToList();

                }

                if (Path.Count > 0) {
                    var point = Path.FirstOrDefault();
                    if (point == null) {
                        return false;
                    }

                    if (!await point.Run()) {
                        await Task.Delay(10);
                        ++_failedCount;
                        if (_failedCount > 50) {
                            Pathfinder.RemoveCachedPath(Path.FirstOrDefault(), Path.LastOrDefault());

                            _failedCount = 0;
                            GameActions.MessageOverhead("Blacklisted Tile", World.Player.Serial);
                            point.Passable = false;
                            Pathfinder.RemoveCurrentCache();
                            NavigationNeedsSaving = true;

                            /*if (point.X == _previousX && point.Y == _previousY) {
                                
                            } else {
                                var currentNode = GetNode(World.Player.Position.ToPoint3D());
                                if (currentNode != null) {
                                    currentNode.RemoveLink(point);
                                    _previousNode?.RemoveLink(point);

                                    if (_previousBlocked != null) {
                                        currentNode.RemoveLink(_previousBlocked);
                                        _previousNode?.RemoveLink(_previousBlocked);
                                    }

                                    _previousBlocked = point;
                                    var test = 1;
                                } else {
                                    point.Passable = false;
                                }

                                // _navigationGrid.SetCellCost(new RoyT.AStar.Position(point.X, point.Y),
                                //     float.PositiveInfinity); //Blacklist the tile we failed on
                                //  NavigationNeedsSaving = true;
                            }*/

                            StopNavigation();

                        }

                        return false;
                    }

                    _previousNode = point;
                    _failedCount = 0;
                    await Task.Delay(100);
                    Path = Path.Where(a =>
                                          Vector2.Distance(new Vector2((ushort)a.X, (ushort)a.Y), new Vector2(World.Player.X, World.Player.Y)) != 0).ToList();
                    if (Path.Count == 0) {
                        StopNavigation();
                        return true;
                    }
                } else {
                    _failedCount = 0;
                    StopNavigation();
                    Path.Clear();
                    return false;
                }
            }

            return true;
        }

        internal static Direction DirectionForNextPosition(Vector2 start, Vector2 facing) {
            //return DirectionHelper.DirectionFromVectors(new Vector2(start.X, start.Y), new Vector2(facing.X, facing.Y));

            var xDiff = start.X - facing.X;
            var yDiff = start.Y - facing.Y;

            if (xDiff < 0 && yDiff == 0) {
                return Direction.West;
            }

            if (xDiff > 0 && yDiff == 0)
            {
                return Direction.East;
            }

            if (xDiff == 0 && yDiff < 0)
            {
                return Direction.North;
            }

            if (xDiff == 0 && yDiff > 0)
            {
                return Direction.South;
            }

            if (xDiff > 0 && yDiff > 0)
            {
                return Direction.Down;
            }

            if (xDiff < 0 && yDiff > 0)
            {
                return Direction.Left;
            }

            if (xDiff < 0 && yDiff < 0)
            {
                return Direction.Up;
            }

            if (xDiff > 0 && yDiff < 0)
            {
                return Direction.Right;
            }

            return Direction.NONE;
        }

    }
}
