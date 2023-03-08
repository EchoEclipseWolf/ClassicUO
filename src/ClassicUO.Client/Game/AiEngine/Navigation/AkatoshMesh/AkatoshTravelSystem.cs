using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassicUO.Game.UI.Gumps;
using AkatoshQuester.Helpers.Cartography;
using AkatoshQuester.Helpers.LightGeometry;
using ClassicUO.AiEngine;
using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;

namespace ClassicUO.NavigationTravel.AkatoshMesh {
    public class AkatoshTravelSystem {
        public class PublicMoongatePoint {
            public int Category;
            public int Name;
            public int MapIndex;
            public Point3D EndPoint;

            public PublicMoongatePoint(int category, int name, int mapIndex, Point3D endPoint) {
                Category = category;
                Name = name;
                MapIndex = mapIndex;
                EndPoint = endPoint;
            }
            public double Distance(Point3D otherPoint) {
                return otherPoint.Distance(EndPoint);
            }
        }

        public class PublicRunebookPoint {
            public string Name;
            public int MapIndex;
            public Point3D EndPoint;

            public PublicRunebookPoint(string name, int mapIndex, Point3D endPoint) {
                Name = name;
                MapIndex = mapIndex;
                EndPoint = endPoint;
            }

            public double Distance(Point3D otherPoint)
            {
                return otherPoint.Distance(EndPoint);
            }
        }

        public class Dungeon {
            public readonly string Name;
            public readonly Point2D TopLeft;
            public readonly Point2D BottomRight;
            public readonly Point3D MoongateLocation;

            public Dungeon(string name, Point2D topLeft, Point2D bottomRight, Point3D moongateLocation) {
                Name = name;
                TopLeft = topLeft;
                BottomRight = bottomRight;
                MoongateLocation = moongateLocation;
            }
            public bool WithinBounds(Point3D point) {
                if (point.X >= TopLeft.X && point.Y >= TopLeft.Y && point.X <= BottomRight.X &&
                    point.Y <= BottomRight.Y) {
                    return true;
                }

                return false;
            }
        }

        public enum TravelSystemMode {
            Walk,
            PublicMoongate,
            Runebook
        }

        public List<PublicMoongatePoint> PublicMoongatePoints = new List<PublicMoongatePoint>();
        public List<PublicRunebookPoint> PublicRunebookPoints = new List<PublicRunebookPoint>();
        public List<Dungeon> Dungeons = new List<Dungeon>();

        public AkatoshTravelSystem() {
            BuildDungeons();
            BuildRunebookList();
            BuildMoongateList();
        }

        public async Task<double> GetWalkDistance(Point3D start, int startMapIndex, Point3D end, int endMapIndex, int maxSearch = 500000) {
            var path = await Navigation.GetPath(start, startMapIndex, end, endMapIndex, maxSearch);
            if (path.Count == 0) {
                return 9999999;
            }

            return path.Count;
        }


        public double BestDistance = -1;

        public async Task<TravelSystemMode> TravelSystemToUse(Point3D endPoint, int endMapIndex, int maxPathDistance = 500000) {
            var stopwatch = Stopwatch.StartNew();

            //Short walk check
            /*if (endPoint.Distance(World.Player.Position.ToPoint3D()) < 100) {
                var totalWalkDistance = await GetWalkDistance(World.Player.Position.ToPoint3D(), endPoint, 1000);
                if (totalWalkDistance < 9999999) {
                    BestDistance = totalWalkDistance;
                    return TravelSystemMode.Walk;
                }
            }*/

            var time1 = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();
            var startMoongate = await GetClosestMoongate(World.Player.Position.ToPoint3D(), World.MapIndex, false);
            PublicMoongatePoint endMoongate = null;
            var dungeon = GetDungeon(World.Player.Position.ToPoint3D());
            var endPointDungeon = GetDungeon(endPoint);

            var time2 = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();

            bool useNewHavenMoongate = false;

            if (dungeon != null) {
                startMoongate = await GetClosestMoongate(dungeon.MoongateLocation, World.MapIndex, false);
            } else {
                if (startMoongate.EndPoint.Distance() > 30) {
                    useNewHavenMoongate = true;
                    //startMoongate =
                    //    PublicMoongatePoints.FirstOrDefault(IsNewHavenMoongate);
                }
            }

            if (endPointDungeon != null) {
                endMoongate = await GetClosestMoongate(endPointDungeon.MoongateLocation, World.MapIndex, false);
            } else {
                endMoongate = await GetClosestMoongate(endPoint, World.MapIndex, false, maxPathDistance);
            }

            var time3 = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();

            if (startMoongate == null || endMoongate == null) {
                return TravelSystemMode.Walk; // Something went wrong, this should never happen
            }

            var dict = new Dictionary<TravelSystemMode, double>();

            var runes = PublicRunebookPoints.Where(r => r.MapIndex == endMapIndex).ToList();
            double bestRuneDistance = 9999999;
            PublicRunebookPoint bestRune = null;
            foreach (var rune in runes) {
                var runeWalkDistance = await GetWalkDistance(rune.EndPoint, World.MapIndex, endPoint, World.MapIndex, maxPathDistance);
                if (runeWalkDistance < bestRuneDistance) {
                    bestRuneDistance = runeWalkDistance;
                    bestRune = rune;
                }

            }

            var time4 = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();

            if (bestRune != null) {
                dict[TravelSystemMode.Runebook] = bestRuneDistance + 50;
            }

            if (startMoongate.MapIndex != endMoongate.MapIndex || startMoongate.Category != endMoongate.Category ||
                startMoongate.Name != endMoongate.Name) {
                var totalMoongateDistance = 0.0;
                if (useNewHavenMoongate) {
                    totalMoongateDistance = 5.0;
                } else {
                    totalMoongateDistance += await GetWalkDistance(World.Player.Position.ToPoint3D(), World.MapIndex, startMoongate.EndPoint, World.MapIndex, maxPathDistance);
                }
                    
                totalMoongateDistance += await GetWalkDistance(endMoongate.EndPoint, World.MapIndex, endPoint, World.MapIndex, maxPathDistance);// endMoongate.Distance(endPoint);
                totalMoongateDistance += 5;
                dict[TravelSystemMode.PublicMoongate] = totalMoongateDistance;
            }

            var time5 = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();

            if (endMapIndex == World.MapIndex)
            {
                var totalWalkDistance = await GetWalkDistance(World.Player.Position.ToPoint3D(), World.MapIndex, endPoint, World.MapIndex, maxPathDistance);
                dict[TravelSystemMode.Walk] = totalWalkDistance;
            }

            var time6 = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();

            double bestDistance = 99999999;
            var bestMode = TravelSystemMode.Walk;
            foreach (var d in dict) {
                if (d.Value < bestDistance) {
                    bestDistance = d.Value;
                    bestMode = d.Key;
                }
            }

            var time7 = stopwatch.ElapsedMilliseconds;
            BestDistance = bestDistance;
            return bestMode;
        }

        public async Task<PublicMoongatePoint> GetClosestMoongate(Point3D point, int mapIndex,
            bool useNavigation = true, int maxPathDistance = 20000) {
            var moongates = PublicMoongatePoints.Where(m => m.MapIndex == mapIndex)
                .OrderBy(m => m.Distance(point)).ToList();

            if (moongates.Count == 0) {
                return null;
            }

            
            if (!useNavigation) {
                return moongates.FirstOrDefault();
            }

            PublicMoongatePoint moongate = null;
            double bestDistance = 99999999;


            foreach (var publicMoongatePoint in moongates) {
                Navigation.LoadGridForPoint(point, mapIndex);
                Navigation.LoadGridForPoint(publicMoongatePoint.EndPoint, mapIndex);
                var totalMoongateDistance = await GetWalkDistance(point, mapIndex, publicMoongatePoint.EndPoint, mapIndex, maxPathDistance);
                if (totalMoongateDistance < 10 || totalMoongateDistance < 9999999) {
                    return publicMoongatePoint;
                }

                if (totalMoongateDistance < bestDistance) {
                    bestDistance = totalMoongateDistance;
                    moongate = publicMoongatePoint;
                }
            }

            return moongate;
        }

        public async Task<bool> NavigateTo(TravelSystemMode mode, Point3D endPoint, int mapIndex, int maxPathDistance = 1000) {
            /*if (mode == TravelSystemMode.PublicMoongate) {
                return await TravelByMoongate(endPoint, mapIndex, maxPathDistance);
            } else if (mode == TravelSystemMode.Runebook) {
                await TravelByRunebook(endPoint, mapIndex, maxPathDistance);
            }*/

            return true;
        }

        /*private async Task<bool> TravelByRunebook(Point3D endPoint, int mapIndex, int maxPathDistance = 1000) {
            var runebook = World.Player.FindItem("Runebook");
            if (runebook != null) {
                GameActions.DoubleClick(runebook.Serial);
                await Task.Delay(500);
                var runebookGump = GetRunebookGump();
                if (runebookGump != null) {
                    runebookGump.OnButtonClick(50);
                    await Task.Delay(2500);
                    Navigation.StopNavigation();
                }
            }

            return true;
        }

        private bool IsNewHavenMoongate(PublicMoongatePoint moongate) {
            return moongate.Category == 1 && moongate.MapIndex == 1 && moongate.Name == 4;
        }

        private async Task<bool> TravelByMoongate(Point3D endPoint, int mapIndex, int maxPathDistance = 1000) {
            var startMoongate = await GetClosestMoongate(World.Player.Position.ToPoint3D(), World.MapIndex, false);
            PublicMoongatePoint endMoongate = null;
            var dungeon = GetDungeon(World.Player.Position.ToPoint3D());
            var endPointDungeon = GetDungeon(endPoint);

            if (dungeon != null) {
                startMoongate = await GetClosestMoongate(dungeon.MoongateLocation, World.MapIndex, false);
            } else {
                if (startMoongate.EndPoint.Distance() > 30) {
                    startMoongate =
                        PublicMoongatePoints.FirstOrDefault(IsNewHavenMoongate);
                }
            }

            if (endPointDungeon != null) {
                endMoongate = await GetClosestMoongate(endPointDungeon.MoongateLocation, World.MapIndex, false);
            } else {
                endMoongate = await GetClosestMoongate(endPoint, World.MapIndex, false, maxPathDistance);
            }

            if (endMoongate.Distance(World.Player.Position.ToPoint3D()) <= 3 || startMoongate == null) {
                Navigation.Clear();
                return true;
            }

            if (startMoongate.EndPoint.Distance() > 40) {
                var runebook = World.Player.FindItem("Runebook");
                if (runebook != null) {
                    GameActions.DoubleClick(runebook.Serial);
                    await Task.Delay(500);
                    var runebookGump = GetRunebookGump();
                    if (runebookGump != null) {
                        runebookGump.OnButtonClick(50);
                        await Task.Delay(2500);
                        Navigation.StopNavigation();
                    }
                    return true;
                }
            }

            if (startMoongate.Distance(World.Player.Position.ToPoint3D()) <= 0) {
                await Task.Delay(500);

                Control worldTeleporterGump = GetMoongateGump();

                if (worldTeleporterGump == null) {
                    var publicMoongate = World.Items
                        .Where(i => i?.Name != null &&
                                    i.Name.Equals("World Omniporter", StringComparison.OrdinalIgnoreCase))
                        .OrderBy(i => i.Distance).FirstOrDefault();
                    if (publicMoongate != null && publicMoongate.Distance <= 3) {
                        GameActions.DoubleClick(publicMoongate.Serial);
                        await Task.Delay(1000);
                        worldTeleporterGump = GetMoongateGump();
                    }
                }

                if (worldTeleporterGump == null) {
                    return true;
                }

                worldTeleporterGump.OnButtonClick(endMoongate.Category);
                await Task.Delay(500);
                worldTeleporterGump = GetMoongateGump();
                worldTeleporterGump.OnButtonClick(endMoongate.Name + (100 * endMoongate.Category));
                await Task.Delay(500);
                Navigation.Clear();

                return true;
            }

            Navigation.LoadGridForPoint(new Point3D(World.Player.Position.X, World.Player.Position.Y,
                World.Player.Position.Z));
            Navigation.LoadGridForPoint(new Point3D(startMoongate.EndPoint.X, startMoongate.EndPoint.Y,
                startMoongate.EndPoint.Z));
            Navigation.LoadGridForPoint(new Point3D(endMoongate.EndPoint.X, endMoongate.EndPoint.Y,
                endMoongate.EndPoint.Z));

            var pathGeneration = new AStar();

            var startingNode = Navigation.GetNode(World.Player.Position.ToPoint3D());
            if (startingNode == null) {
                GameActions.Print($"[Navigation]: False Start.");
                return false;
            }

            var endingNode = Navigation.GetNode(startMoongate.EndPoint);
            if (endingNode == null) {
                GameActions.Print($"[Navigation]: False End.");
                return false;
            }

            var newPath = NavigationNew.AkatoshMesh.New.Pathfinder.FindPath(startingNode, endingNode);

            Navigation.Path = newPath;

            return true;
        }*/

        private Dungeon GetDungeon(Point3D point) {
            return Dungeons.FirstOrDefault(d => d.WithinBounds(point));
        }

        private Gump GetMoongateGump() {
            var gumps = UIManager.Gumps;
            foreach (var gump in gumps) {
                if (gump.Children.Count > 0) {
                    foreach (var gumpChild in gump.Children) {
                        if (gumpChild is Label label) {
                            if (label.Text != null && label.Text.ToLower().Contains("world teleporter")) {
                                return gump as Gump;
                            }
                        }

                        if (gumpChild is HtmlControl html) {
                            if (html.Text != null && html.Text.ToLower().Contains("world teleporter")) {
                                return gump as Gump;
                            }
                        }
                    }
                }
            }

            return null;
        }

        internal static Gump GetRunebookGump() {
            var gumps = UIManager.Gumps;
            foreach (var gump in gumps) {
                if (gump.Children.Count > 0) {
                    foreach (var gumpChild in gump.Children) {
                        if (gumpChild is Label label) {
                            if (label.Text != null && label.Text.ToLower().Contains("max charges")) {
                                return gump as Gump;
                            }
                        }

                        if (gumpChild is HtmlControl html) {
                            if (html.Text != null && html.Text.ToLower().Contains("max charges")) {
                                return gump as Gump;
                            }
                        }
                    }
                }
            }

            return null;
        }

        private void BuildDungeons() {
            Dungeons.Add(new Dungeon("Shame", new Point2D(5376, 0), new Point2D(5631, 255), new Point3D(511, 1565, 0)));
            Dungeons.Add(new Dungeon("Shame2", new Point2D(5646, 0), new Point2D(5890, 120), new Point3D(511, 1565, 0)));


        }

        private void BuildRunebookList() {
            PublicRunebookPoints.Add(new PublicRunebookPoint("New Haven", 1, new Point3D(3493, 2577, 14)));
        }

        private void BuildMoongateList() {
            /*Trammel Towns = 1
            Trammel Dungeons = 2
            Fel Moongates = 4
            Trammel Moongates = 5
            Ishenar = 6
            ishenar shrines = 7
            malas = 8
            Tokuno = 9
            TerMur = 10
            Custom Locations = 11*/

            //--------------//
            //---Trammel---//
            //------------//
            PublicMoongatePoints.Add(new PublicMoongatePoint(1, 0, 1, new Point3D(1434, 1699, 2))); //Trammel Britain
            PublicMoongatePoints.Add(new PublicMoongatePoint(1, 1, 1, new Point3D(2705, 2162, 0))); //Trammel Bucs Den
            PublicMoongatePoints.Add(new PublicMoongatePoint(1, 2, 1, new Point3D(2237, 1214, 0))); //Trammel Cove
            PublicMoongatePoints.Add(new PublicMoongatePoint(1, 3, 1, new Point3D(5274, 3991, 37))); //Trammel Delucia
            PublicMoongatePoints.Add(new PublicMoongatePoint(1, 4, 1, new Point3D(3493, 2577, 14))); //Trammel New Haven
            PublicMoongatePoints.Add(new PublicMoongatePoint(1, 5, 1, new Point3D(1417, 3821, 0))); //Trammel Jhelom
            PublicMoongatePoints.Add(new PublicMoongatePoint(1, 6, 1, new Point3D(3791, 2230, 20))); //Trammel Magincia
            PublicMoongatePoints.Add(new PublicMoongatePoint(1, 7, 1, new Point3D(2525, 582, 0))); //Trammel Minoc
            PublicMoongatePoints.Add(new PublicMoongatePoint(1, 8, 1, new Point3D(4471, 1177, 0))); //Trammel Moonglow
            PublicMoongatePoints.Add(new PublicMoongatePoint(1, 9, 1, new Point3D(3770, 1308, 0))); //Trammel Nujelm
            PublicMoongatePoints.Add(new PublicMoongatePoint(1, 10, 1, new Point3D(5729, 3208, -6))); //Trammel Papua
            PublicMoongatePoints.Add(new PublicMoongatePoint(1, 11, 1, new Point3D(2895, 3479, 15))); //Trammel Serpents Hold
            PublicMoongatePoints.Add(new PublicMoongatePoint(1, 12, 1, new Point3D(596, 2138, 0))); //Trammel Skara Brae
            PublicMoongatePoints.Add(new PublicMoongatePoint(1, 13, 1, new Point3D(1823, 2821, 0))); //Trammel Trinsic
            PublicMoongatePoints.Add(new PublicMoongatePoint(1, 14, 1, new Point3D(2899, 676, 0))); //Trammel Vesper
            PublicMoongatePoints.Add(new PublicMoongatePoint(1, 15, 1, new Point3D(1361, 895, 0))); //Trammel Wind
            PublicMoongatePoints.Add(new PublicMoongatePoint(1, 16, 1, new Point3D(542, 985, 0))); //Trammel Yew

            PublicMoongatePoints.Add(new PublicMoongatePoint(2, 0, 1, new Point3D(586, 1643, -5))); //Trammel Blighted Grove
            PublicMoongatePoints.Add(new PublicMoongatePoint(2, 1, 1, new Point3D(2498, 921, 0))); //Trammel Covetous
            PublicMoongatePoints.Add(new PublicMoongatePoint(2, 2, 1, new Point3D(4591, 3647, 80))); //Trammel Daemon Temple
            PublicMoongatePoints.Add(new PublicMoongatePoint(2, 3, 1, new Point3D(4111, 434, 5))); //Trammel Deceit
            PublicMoongatePoints.Add(new PublicMoongatePoint(2, 4, 1, new Point3D(1176, 2640, 2))); //Trammel Destard
            PublicMoongatePoints.Add(new PublicMoongatePoint(2, 5, 1, new Point3D(2923, 3409, 8))); //Trammel Fire
            PublicMoongatePoints.Add(new PublicMoongatePoint(2, 6, 1, new Point3D(4721, 3824, 0))); //Trammel Hythloth
            PublicMoongatePoints.Add(new PublicMoongatePoint(2, 7, 1, new Point3D(1999, 81, 4))); //Trammel Ice
            PublicMoongatePoints.Add(new PublicMoongatePoint(2, 8, 1, new Point3D(5766, 2634, 43))); //Trammel Ophidian Temple
            PublicMoongatePoints.Add(new PublicMoongatePoint(2, 9, 1, new Point3D(1017, 1429, 0))); //Trammel Orc Caves
            PublicMoongatePoints.Add(new PublicMoongatePoint(2, 10, 1, new Point3D(1716, 2993, 0))); //Trammel Painted Caves
            PublicMoongatePoints.Add(new PublicMoongatePoint(2, 11, 1, new Point3D(5569, 3019, 31))); //Trammel Paroxysmus
            PublicMoongatePoints.Add(new PublicMoongatePoint(2, 12, 1, new Point3D(3789, 1095, 20))); //Trammel Prism of Light
            PublicMoongatePoints.Add(new PublicMoongatePoint(2, 13, 1, new Point3D(759, 1642, 0))); //Trammel Sanctuary
            PublicMoongatePoints.Add(new PublicMoongatePoint(2, 14, 1, new Point3D(511, 1565, 0))); //Trammel Shame
            PublicMoongatePoints.Add(new PublicMoongatePoint(2, 15, 1, new Point3D(2607, 763, 0))); //Trammel Solen Hive
            PublicMoongatePoints.Add(new PublicMoongatePoint(2, 16, 1, new Point3D(5451, 3143, -60))); //Trammel Terathan Keep
            PublicMoongatePoints.Add(new PublicMoongatePoint(2, 17, 1, new Point3D(2043, 238, 10))); //Trammel Wrong

            PublicMoongatePoints.Add(new PublicMoongatePoint(5, 0, 1, new Point3D(1336, 1997, 5))); //Trammel Britain
            PublicMoongatePoints.Add(new PublicMoongatePoint(5, 1, 1, new Point3D(3450, 2677, 25))); //Trammel New Haven
            PublicMoongatePoints.Add(new PublicMoongatePoint(5, 2, 1, new Point3D(1499, 3771, 5))); //Trammel Jhelom
            PublicMoongatePoints.Add(new PublicMoongatePoint(5, 3, 1, new Point3D(3563, 2139, 34))); //Trammel Magincia
            PublicMoongatePoints.Add(new PublicMoongatePoint(5, 4, 1, new Point3D(2701, 692, 5))); //Trammel Minoc
            PublicMoongatePoints.Add(new PublicMoongatePoint(5, 5, 1, new Point3D(4467, 1283, 5))); //Trammel Moonglow
            PublicMoongatePoints.Add(new PublicMoongatePoint(5, 6, 1, new Point3D(643, 2067, 5))); //Trammel Skara Brae
            PublicMoongatePoints.Add(new PublicMoongatePoint(5, 7, 1, new Point3D(1828, 2948, -20))); //Trammel Trinsic
            PublicMoongatePoints.Add(new PublicMoongatePoint(5, 8, 1, new Point3D(771, 752, 5))); //Trammel Yew

            //--------------//
            //---Felucca---//
            //------------//
            PublicMoongatePoints.Add(new PublicMoongatePoint(4, 0, 0, new Point3D(1336, 1997, 5))); //Felucca Britain
            PublicMoongatePoints.Add(new PublicMoongatePoint(4, 1, 0, new Point3D(1499, 3771, 5))); //Felucca Jhelom
            PublicMoongatePoints.Add(new PublicMoongatePoint(4, 2, 0, new Point3D(3563, 2139, 34))); //Felucca Magincia
            PublicMoongatePoints.Add(new PublicMoongatePoint(4, 3, 0, new Point3D(2701, 692, 5))); //Felucca Minoc
            PublicMoongatePoints.Add(new PublicMoongatePoint(4, 4, 0, new Point3D(4467, 1283, 5))); //Felucca Moonglow
            PublicMoongatePoints.Add(new PublicMoongatePoint(4, 6, 0, new Point3D(643, 2067, 5))); //Felucca Skara Brae
            PublicMoongatePoints.Add(new PublicMoongatePoint(4, 7, 0, new Point3D(1828, 2948, -20))); //Felucca Trinsic
            PublicMoongatePoints.Add(new PublicMoongatePoint(4, 8, 0, new Point3D(771, 752, 5))); //Felucca Yew


            //--------------//
            //---Custom---//
            //------------//
            PublicMoongatePoints.Add(new PublicMoongatePoint(11, 0, 1, new Point3D(1301, 1080, 0))); //CustomLocation Newb Dungeon
            PublicMoongatePoints.Add(new PublicMoongatePoint(11, 1, 1, new Point3D(5509, 1250, 0))); //CustomLocation Hue Garden
            PublicMoongatePoints.Add(new PublicMoongatePoint(11, 2, 3, new Point3D(152, 209, -1))); //CustomLocation LoS Display Gates
            PublicMoongatePoints.Add(new PublicMoongatePoint(11, 3, 1, new Point3D(5505, 1261, 0))); //CustomLocation LoS Store
            PublicMoongatePoints.Add(new PublicMoongatePoint(11, 4, 3, new Point3D(1069, 1443, -90))); //CustomLocation Mook Town
            PublicMoongatePoints.Add(new PublicMoongatePoint(11, 5, 1, new Point3D(5273, 307, 15))); //CustomLocation Pomona's Farm
            PublicMoongatePoints.Add(new PublicMoongatePoint(11, 6, 1, new Point3D(5205, 367, 25))); //CustomLocation Pomona's Farmers
            PublicMoongatePoints.Add(new PublicMoongatePoint(11, 7, 1, new Point3D(5173, 457, 20))); //CustomLocation Pomona's Orchard
            PublicMoongatePoints.Add(new PublicMoongatePoint(11, 8, 1, new Point3D(4551, 2359, -2))); //CustomLocation Sea Market
            PublicMoongatePoints.Add(new PublicMoongatePoint(11, 9, 3, new Point3D(159, 224, 79))); //CustomLocation Training Room
            PublicMoongatePoints.Add(new PublicMoongatePoint(11, 10, 1, new Point3D(902, 912, 0))); //CustomLocation Yew Forest

        }
    }
}
