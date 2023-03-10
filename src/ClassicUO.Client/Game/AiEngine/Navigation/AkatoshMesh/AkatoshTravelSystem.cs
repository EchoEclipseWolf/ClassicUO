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
using ClassicUO.DatabaseUtility;
using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Net;
using ClassicUO.Game.AiEngine.AiClasses;
using ClassicUO.Game.AiEngine.Memory;
using ClassicUO.Game.AiEngine.Helpers;

namespace ClassicUO.NavigationTravel.AkatoshMesh
{
    public class AkatoshTravelSystem {
        public class PublicMoongatePoint {
            public int Category;
            public int Index;
            public int MapIndex;
            public Point3D EndPoint;
            public string Name;
            public string MapName;

            public PublicMoongatePoint(int category, int index, int mapIndex, Point3D endPoint, string name) {
                Category = category;
                Index = index;
                MapIndex = mapIndex;
                EndPoint = endPoint;
                Name = name;

                MapName = MobileCache.MapNameFromIndex(MapIndex);
            }
            public double Distance(Point3D otherPoint) {
                return otherPoint.Distance(EndPoint);
            }

            public override string ToString() {
                return $"PublicMoongate: {Name} [{MapName}]";
            }
        }

        public class PublicRunebookPoint {
            public string Name;
            public int RuneIndex;
            public int MapIndex;
            public Point3D EndPoint;
            public string MapName;

            public PublicRunebookPoint(int runeIndex, string name, int mapIndex, Point3D endPoint) {
                RuneIndex = runeIndex;
                Name = name;
                MapIndex = mapIndex;
                EndPoint = endPoint;

                MapName = MobileCache.MapNameFromIndex(MapIndex);
            }

            public double Distance(Point3D otherPoint)
            {
                return otherPoint.Distance(EndPoint);
            }

            public override string ToString()
            {
                return $"RunePoint: {Name} [{MapName}]";
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
        //public List<PublicRunebookPoint> PublicRunebookPoints = new List<PublicRunebookPoint>();
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
                endMoongate = await GetClosestMoongate(endPointDungeon.MoongateLocation, endMapIndex, false);
            } else {
                endMoongate = await GetClosestMoongate(endPoint, endMapIndex, false, maxPathDistance);
            }

            var time3 = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();

            if (startMoongate == null || endMoongate == null) {
                return TravelSystemMode.Walk; // Something went wrong, this should never happen
            }

            var dict = new Dictionary<TravelSystemMode, double>();

            var bestRuneTuple = await GetBestRunePoint(endPoint, endMapIndex);

            if (bestRuneTuple.Item2 != null) {
                dict[TravelSystemMode.Runebook] = bestRuneTuple.Item1 + 25;
            }

            var time4 = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();

            

            if (startMoongate.MapIndex != endMoongate.MapIndex || startMoongate.Category != endMoongate.Category ||
                startMoongate.Index != endMoongate.Index) {
                var totalMoongateDistance = 0.0;
                if (useNewHavenMoongate) {
                    totalMoongateDistance = 5.0;
                } else {
                    totalMoongateDistance += await GetWalkDistance(World.Player.Position.ToPoint3D(), World.MapIndex, startMoongate.EndPoint, startMoongate.MapIndex, maxPathDistance);
                }
                    
                totalMoongateDistance += await GetWalkDistance(endMoongate.EndPoint, endMoongate.MapIndex, endPoint, endMapIndex, maxPathDistance);// endMoongate.Distance(endPoint);
                totalMoongateDistance += 50;
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

        public async Task<Tuple<double, SingleRuneMemory>> GetBestRunePoint(Point3D point, int mapIndex) {
            var runes = TeleportsMemory.Instance.RuneMemories.Where(r => r.MapIndex == mapIndex).ToList();
            double bestRuneDistance = 9999999;
            SingleRuneMemory bestRune = null;

            foreach (var rune in runes) {
                var runeWalkDistance = await GetWalkDistance(rune.Location, rune.MapIndex, point, mapIndex, 500000);

                if (runeWalkDistance < bestRuneDistance) {
                    bestRuneDistance = runeWalkDistance;
                    bestRune = rune;
                }
            }

            return new Tuple<double, SingleRuneMemory>(bestRuneDistance, bestRune);
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
            if (!AiSettings.Instance.NavigationMovement) {
                return false;
            }

            if (mode == TravelSystemMode.PublicMoongate) {
                return await TravelByMoongate(endPoint, mapIndex, maxPathDistance);
            } 
            
            if (mode == TravelSystemMode.Runebook) {
                return await TravelByRunebook(endPoint, mapIndex, maxPathDistance);
            }

            return true;
        }

        private async Task<bool> TravelByRunebook(Point3D endPoint, int mapIndex, int maxPathDistance = 1000) {
            var bestRuneTuple = await GetBestRunePoint(endPoint, mapIndex);
            if (bestRuneTuple.Item2 != null) {
                var runebook = await ItemsHelper.FindItemBySerial(bestRuneTuple.Item2.RunebookSerial, bestRuneTuple.Item2.ContainerSerial);

                if (runebook == null) {
                    return false;
                }

                var previousLocation = new Point3D(World.Player.Position.X, World.Player.Position.Y, World.Player.Position.Z);
                var previousMapId = World.MapIndex;

                

                GameActions.DoubleClick(runebook.Serial);
                await Task.Delay(500);
                var runebookGump = GumpHelper.GetRunebookGump();
                if (runebookGump != null) {
                    runebookGump.OnButtonClick(50 + bestRuneTuple.Item2.Index);
                    
                    await WaitForHelper.WaitFor(() => previousLocation.Distance() > 8.0f || World.MapIndex != previousMapId, 15000);
                    await Task.Delay(2500);
                    Navigation.StopNavigation();
                }
            }

            return true;
        }

        private bool IsNewHavenMoongate(PublicMoongatePoint moongate) {
            return moongate.Category == 1 && moongate.MapIndex == 1 && moongate.Index == 4;
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
                endMoongate = await GetClosestMoongate(endPoint, mapIndex, false, maxPathDistance);
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
                    var runebookGump = GumpHelper.GetRunebookGump();
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

                var previousLocation = new Point3D(World.Player.Position.X, World.Player.Position.Y, World.Player.Position.Z);
                var previousMapId = World.MapIndex;

                Control worldTeleporterGump = GetMoongateGump();

                /*if (worldTeleporterGump == null) {
                    var publicMoongate = World.Items
                        .Where(i => i?.Name != null &&
                                    i.Name.Equals("World Omniporter", StringComparison.OrdinalIgnoreCase))
                        .OrderBy(i => i.Distance).FirstOrDefault();
                    if (publicMoongate != null && publicMoongate.Distance <= 3) {
                        GameActions.DoubleClick(publicMoongate.Serial);
                        await Task.Delay(1000);
                        worldTeleporterGump = GetMoongateGump();
                    }
                }*/

                if (worldTeleporterGump == null) {
                    return true;
                }

                worldTeleporterGump.OnButtonClick(endMoongate.Category);
                await Task.Delay(500);
                worldTeleporterGump = GetMoongateGump();
                worldTeleporterGump.OnButtonClick(endMoongate.Index + (100 * endMoongate.Category));
                await WaitForHelper.WaitFor(() => previousLocation.Distance() > 8.0f || World.MapIndex != previousMapId, 15000);
                await Task.Delay(2500);
                Navigation.Clear();

                return true;
            }

            Navigation.LoadGridForPoint(new Point3D(World.Player.Position.X, World.Player.Position.Y,
                World.Player.Position.Z), World.MapIndex);
            Navigation.LoadGridForPoint(new Point3D(startMoongate.EndPoint.X, startMoongate.EndPoint.Y,
                startMoongate.EndPoint.Z), startMoongate.MapIndex);
            Navigation.LoadGridForPoint(new Point3D(endMoongate.EndPoint.X, endMoongate.EndPoint.Y,
                endMoongate.EndPoint.Z), endMoongate.MapIndex);

            var pathGeneration = new AStar();

            var startingNode = Navigation.GetNode(World.Player.Position.ToPoint3D(), World.MapIndex);
            if (startingNode == null) {
                GameActions.Print($"[Navigation]: False Start.");
                return false;
            }

            var endingNode = Navigation.GetNode(startMoongate.EndPoint, startMoongate.MapIndex);
            if (endingNode == null) {
                GameActions.Print($"[Navigation]: False End.");
                return false;
            }

            var newPath = NavigationNew.AkatoshMesh.New.Pathfinder.FindPath(startingNode, endingNode);

            Navigation.Path = newPath;

            return true;
        }

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


        private void BuildDungeons() {
            Dungeons.Add(new Dungeon("Shame", new Point2D(5376, 0), new Point2D(5631, 255), new Point3D(511, 1565, 0)));
            Dungeons.Add(new Dungeon("Shame2", new Point2D(5646, 0), new Point2D(5890, 120), new Point3D(511, 1565, 0)));


        }

        private void BuildRunebookList() {
            var index = 0;
            //PublicRunebookPoints.Add(new PublicRunebookPoint(index++, "New Haven", 1, new Point3D(3493, 2577, 14)));
            //PublicRunebookPoints.Add(new PublicRunebookPoint(index++, "Luna", 3, new Point3D(1015, 508, -70)));
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
            var name = 0;
            var category = 1;
            PublicMoongatePoints.Add(new PublicMoongatePoint(category, name++, 1, new Point3D(1434, 1699, 2), "Britain"));
            PublicMoongatePoints.Add(new PublicMoongatePoint(category, name++, 1, new Point3D(2705, 2162, 0), "Bucs Den"));
            PublicMoongatePoints.Add(new PublicMoongatePoint(category, name++, 1, new Point3D(2237, 1214, 0), "Cove"));
            PublicMoongatePoints.Add(new PublicMoongatePoint(category, name++, 1, new Point3D(5274, 3991, 37), "Delucia"));
            PublicMoongatePoints.Add(new PublicMoongatePoint(category, name++, 1, new Point3D(3493, 2577, 14), "New Haven"));
            PublicMoongatePoints.Add(new PublicMoongatePoint(category, name++, 1, new Point3D(3626, 2610, 0), "Haven"));
            PublicMoongatePoints.Add(new PublicMoongatePoint(category, name++, 1, new Point3D(1417, 3821, 0), "Jhelom"));
            PublicMoongatePoints.Add(new PublicMoongatePoint(category, name++, 1, new Point3D(3791, 2230, 20), "Magincia"));
            PublicMoongatePoints.Add(new PublicMoongatePoint(category, name++, 1, new Point3D(2525, 582, 0), "Minoc"));
            PublicMoongatePoints.Add(new PublicMoongatePoint(category, name++, 1, new Point3D(4471, 1177, 0), "Moonglow"));
            PublicMoongatePoints.Add(new PublicMoongatePoint(category, name++, 1, new Point3D(3770, 1308, 0), "Nujelm"));
            PublicMoongatePoints.Add(new PublicMoongatePoint(category, name++, 1, new Point3D(5729, 3208, -6), "Papua"));
            PublicMoongatePoints.Add(new PublicMoongatePoint(category, name++, 1, new Point3D(4551, 2343, -2), "Sea Market"));
            PublicMoongatePoints.Add(new PublicMoongatePoint(category, name++, 1, new Point3D(2895, 3479, 15), "Serpents Hold"));
            PublicMoongatePoints.Add(new PublicMoongatePoint(category, name++, 1, new Point3D(596, 2138, 0), "Skara Brae"));
            PublicMoongatePoints.Add(new PublicMoongatePoint(category, name++, 1, new Point3D(1823, 2821, 0), "Trinsic"));
            PublicMoongatePoints.Add(new PublicMoongatePoint(category, name++, 1, new Point3D(2899, 676, 0), "Vesper"));
            PublicMoongatePoints.Add(new PublicMoongatePoint(category, name++, 1, new Point3D(1361, 895, 0), "Wind"));
            PublicMoongatePoints.Add(new PublicMoongatePoint(category, name, 1, new Point3D(542, 985, 0), "Yew"));

            //--------------//
            //---Tram Dungeons---//
            //------------//
            name = 0;
            category = 2;

            PublicMoongatePoints.Add(new PublicMoongatePoint(category, name++, 1, new Point3D(586, 1643, -5), "Blighted Grove"));
            PublicMoongatePoints.Add(new PublicMoongatePoint(category, name++, 1, new Point3D(2498, 921, 0), "Covetous"));
            PublicMoongatePoints.Add(new PublicMoongatePoint(category, name++, 1, new Point3D(4591, 3647, 80), "Daemon Temple"));
            PublicMoongatePoints.Add(new PublicMoongatePoint(category, name++, 1, new Point3D(4111, 434, 5), "Deceit"));
            PublicMoongatePoints.Add(new PublicMoongatePoint(category, name++, 1, new Point3D(1176, 2640, 2), "Destard"));
            PublicMoongatePoints.Add(new PublicMoongatePoint(category, name++, 1, new Point3D(2923, 3409, 8), "Fire"));
            PublicMoongatePoints.Add(new PublicMoongatePoint(category, name++, 1, new Point3D(4721, 3824, 0), "Hythloth"));
            PublicMoongatePoints.Add(new PublicMoongatePoint(category, name++, 1, new Point3D(1999, 81, 4), "Ice"));
            PublicMoongatePoints.Add(new PublicMoongatePoint(category, name++, 1, new Point3D(5766, 2634, 43), "Ophidian Temple"));
            PublicMoongatePoints.Add(new PublicMoongatePoint(category, name++, 1, new Point3D(1017, 1429, 0), "Orc Caves"));
            PublicMoongatePoints.Add(new PublicMoongatePoint(category, name++, 1, new Point3D(1716, 2993, 0), "Painted Caves"));
            PublicMoongatePoints.Add(new PublicMoongatePoint(category, name++, 1, new Point3D(5569, 3019, 31), "Paroxysmus"));
            PublicMoongatePoints.Add(new PublicMoongatePoint(category, name++, 1, new Point3D(3789, 1095, 20), "Prism of Light"));
            PublicMoongatePoints.Add(new PublicMoongatePoint(category, name++, 1, new Point3D(759, 1642, 0), "Sanctuary"));
            PublicMoongatePoints.Add(new PublicMoongatePoint(category, name++, 1, new Point3D(511, 1565, 0), "Shame"));
            PublicMoongatePoints.Add(new PublicMoongatePoint(category, name++, 1, new Point3D(2607, 763, 0), "Solen Hive"));
            PublicMoongatePoints.Add(new PublicMoongatePoint(category, name++, 1, new Point3D(5451, 3143, -60), "Terathan Keep"));
            PublicMoongatePoints.Add(new PublicMoongatePoint(category, name, 1, new Point3D(2043, 238, 10), "Wrong"));



            //--------------//
            //---Felucca---//
            //------------//


            //--------------//
            //---Malas---//
            //------------//
            name = 0;
            category = 7;

            PublicMoongatePoints.Add(new PublicMoongatePoint(category, name++, 3, new Point3D(1015, 527, -65), "Luna"));
            PublicMoongatePoints.Add(new PublicMoongatePoint(category, name++, 3, new Point3D(1997, 1386, -85), "Umbra"));
            PublicMoongatePoints.Add(new PublicMoongatePoint(category, name++, 3, new Point3D(2368, 1267, -85), "Doom"));
            //PublicMoongatePoints.Add(new PublicMoongatePoint(category, name++, 3, new Point3D(1732, 979, -80), "Labirynth"));
            //PublicMoongatePoints.Add(new PublicMoongatePoint(category, name++, 3, new Point3D(124, 1679, -0), "Bedlam"));


            //--------------//
            //---Custom---//
            //------------//
            name = 0;
            category = 11;
            //PublicMoongatePoints.Add(new PublicMoongatePoint(11, 0, 1, new Point3D(1301, 1080, 0), "Newb Dungeon")); //CustomLocation Newb Dungeon
            //PublicMoongatePoints.Add(new PublicMoongatePoint(11, 1, 1, new Point3D(5509, 1250, 0), "Hue Garden")); //CustomLocation Hue Garden
            //PublicMoongatePoints.Add(new PublicMoongatePoint(11, 2, 3, new Point3D(152, 209, -1))); //CustomLocation LoS Display Gates
            //PublicMoongatePoints.Add(new PublicMoongatePoint(11, 3, 1, new Point3D(5505, 1261, 0))); //CustomLocation LoS Store
            //PublicMoongatePoints.Add(new PublicMoongatePoint(11, 4, 3, new Point3D(1069, 1443, -90))); //CustomLocation Mook Town
            // PublicMoongatePoints.Add(new PublicMoongatePoint(11, 5, 1, new Point3D(5273, 307, 15))); //CustomLocation Pomona's Farm
            //PublicMoongatePoints.Add(new PublicMoongatePoint(11, 6, 1, new Point3D(5205, 367, 25))); //CustomLocation Pomona's Farmers
            //PublicMoongatePoints.Add(new PublicMoongatePoint(11, 7, 1, new Point3D(5173, 457, 20))); //CustomLocation Pomona's Orchard
            //PublicMoongatePoints.Add(new PublicMoongatePoint(11, 8, 1, new Point3D(4551, 2359, -2))); //CustomLocation Sea Market
            //PublicMoongatePoints.Add(new PublicMoongatePoint(11, 9, 3, new Point3D(159, 224, 79))); //CustomLocation Training Room
            //PublicMoongatePoints.Add(new PublicMoongatePoint(11, 10, 1, new Point3D(902, 912, 0))); //CustomLocation Yew Forest

        }
    }
}
