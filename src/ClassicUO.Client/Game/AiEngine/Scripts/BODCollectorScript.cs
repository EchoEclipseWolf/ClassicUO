using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AkatoshQuester.Helpers.Cartography;
using AkatoshQuester.Helpers.LightGeometry;
using ClassicUO.AiEngine;
using ClassicUO.AiEngine.AiEngineTasks;
using ClassicUO.DatabaseUtility;
using ClassicUO.Game.AiEngine.Enums;
using ClassicUO.Game.AiEngine.Helpers;
using ClassicUO.Game.AiEngine.Memory;
using ClassicUO.Game.AiEngine.Tasks;
using ClassicUO.Game.Enums;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.Game.AiEngine.Scripts
{
    public class BODCollectorScript : BaseAITask {
        private MobileCache.SingleMobCache _currentMobToTry = null;
        private Node _currentNode = null;
        private BodKeys _currentTryingBod = BodKeys.None;
        private List<MobileCache.SingleMobCache> _blacklistedMobs = new();
        private DepositItemsIntoChestTask _depositItemsIntoChestTask = new DepositItemsIntoChestTask(new Point3D(3419, 2627, 85));
        
        public BODCollectorScript() : base("BOD Collector")
        {
        }

        public override async Task<bool> Pulse() {
            if (GumpHelper.GetBulkOrderAcceptanceGump() != null) {
                var gump = GumpHelper.GetBulkOrderAcceptanceGump();
                AiCore.GumpsToClickButton.Push(new Tuple<Gump, int>(gump, 1));
                await Task.Delay(1000);
                return true;
            }
            if (_currentTryingBod == BodKeys.None) {
                _currentTryingBod = GetNextBodCollectionToTry();
            }

            if (_currentTryingBod == BodKeys.None) {
                var deedsInBag = await ItemsHelper.GetPlayerBackpackItemsById(false, 0x2258);

                if (deedsInBag.Count > 0) {
                    await _depositItemsIntoChestTask.Pulse(deedsInBag);
                    return true;
                }
                return false;
            }

            if (_currentMobToTry == null) {
                var npcTitle = GetBodNpcTitleFromKey(_currentTryingBod);

                if (string.IsNullOrEmpty(npcTitle)) {
                    ItemsMemory.Instance.AddBodNextAvailableTime(_currentTryingBod, new TimeSpan(1, 0, 0));
                    _currentNode = null;
                    _currentMobToTry = null;
                    _currentTryingBod = BodKeys.None;
                    return false;
                }

                var foundNpc = FindNpcByTitle(npcTitle, out var node);
                _currentNode = node;

                if (foundNpc != null) {
                    _currentMobToTry = foundNpc;
                }
            }

            if (_currentMobToTry == null || _currentNode == null) {
                ItemsMemory.Instance.AddBodNextAvailableTime(_currentTryingBod, new TimeSpan(1, 0, 0));
                _currentNode = null;
                _currentMobToTry = null;
                _currentTryingBod = BodKeys.None;
                return false;
            }
            
            var npc = FindMobileByTitle(GetBodNpcTitleFromKey(_currentTryingBod));

            if (npc != null) {
                var npcPosition = npc.Position.ToPoint3D();
                if(npcPosition.Distance2D(Player.Position.ToPoint3D()) > 3) {
                    await Navigation.NavigateTo(npcPosition, 1, true);
                    return true;
                }

                GameActions.OpenPopupMenu(npc.Serial);
                await WaitForHelper.WaitFor(() => GumpHelper.GetPopupMenu() != null, 1000);
                var popup = GumpHelper.GetPopupMenu();

                if (popup != null) {
                    var journalStartTime = DateTime.Now;

                    if (_currentTryingBod == BodKeys.Taming) {
                        GameActions.ResponsePopupMenu(npc.Serial, 2);
                    }
                    else {
                        GameActions.ResponsePopupMenu(npc.Serial, 1);
                    }

                    await Task.Delay(100);
                    AiCore.GumpsToClose.Push(popup);
                    await Task.Delay(500);
                    await WaitForHelper.WaitFor(() => GumpHelper.GetBulkOrderAcceptanceGump() != null, 2000);
                    await Task.Delay(1000);
                    var gump = GumpHelper.GetBulkOrderAcceptanceGump();

                    var unavailableEntry = JournalHelper.GetJournalEntriesContaining("An offer may be available", journalStartTime).FirstOrDefault();

                    if (unavailableEntry != null) {
                        var text = unavailableEntry.Text;

                        var pattern = @"\b(\d+)\s+minutes\b";
                        var match = Regex.Match(text, pattern);

                        if (match.Success) {
                            int totalMinutes = 400;

                            if (int.TryParse(match.Groups[1].Value, out var minutes)) {
                                totalMinutes = minutes;
                            }

                            ItemsMemory.Instance.AddBodNextAvailableTime(_currentTryingBod, new TimeSpan(0, totalMinutes, 0));
                            _currentNode = null;
                            _currentMobToTry = null;
                            _currentTryingBod = BodKeys.None;
                            return true;
                        }
                    }

                    if (gump == null) {
                        ItemsMemory.Instance.AddBodNextAvailableTime(_currentTryingBod, new TimeSpan(0, 400, 0));
                        _currentNode = null;
                        _currentMobToTry = null;
                        _currentTryingBod = BodKeys.None;
                        return true;
                    }

                    AiCore.GumpsToClickButton.Push(new Tuple<Gump, int>(gump, 1));
                    await WaitForHelper.WaitFor(() => GumpHelper.GetBulkOrderAcceptanceGump() == null, 2000);
                    await Task.Delay(1000);
                    return true;

                }

                int bob = 1;
            }
            
            if (_currentNode.Distance2D > 4) {
                await Navigation.NavigateTo(_currentNode.Position, 1, true);
                return true;
            }
            return true;
        }

        public static string GetBodNpcTitleFromKey(BodKeys bodkey) {
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            return bodkey switch {
                BodKeys.Alchemy => "alchemist",
                BodKeys.Carpentry => "carpenter",
                BodKeys.Cooking => "cook",
                BodKeys.Fletching => "bowyer",
                BodKeys.Inscription => "scribe",
                BodKeys.Blacksmithing => "weaponsmith",
                BodKeys.Tailoring => "tailor",
                BodKeys.Taming => "animal trainer",
                BodKeys.Tinkering => "tinker",
                _ => ""
            };
        }

        public BodKeys GetNextBodCollectionToTry() {
            foreach (BodKeys bodKey in Enum.GetValues(typeof(BodKeys))) {
                if (ItemsMemory.Instance.IsBodReadyForTry(bodKey)) {
                    return bodKey;
                }
            }

            return BodKeys.None;
        }

        internal MobileCache.SingleMobCache FindNpcByTitle(string title, out Node node) {
            var search = Database.Search(title, "Trammel", false).
                                  OrderBy(a => new Point2D(a.X, a.Y).Distance(new Point2D(Player.X, Player.Y))).ToList();
            
            foreach (var mob in search) {
                var point = new Point3D(mob.X, mob.Y, 0);
                var twoDistance = point.Distance2D(Player.Position.ToPoint3D());
                
                Navigation.LoadGridForPoint(point, 1);
                node = Navigation.GetNode(point, 1, 100);

                if (node != null) {
                    return mob;
                }
            }

            node = null;
            return null;
        }

        private Mobile FindMobileByTitle(string title) {
            var mobiles = World.Mobiles.Values.Where(a => a != null && SerialHelper.IsValid(a.Serial) && a is not PlayerMobile).ToList();
            //var mobiles = World.Mobiles.Values.Where(a => a != null && a.Title.ToLower().Replace("the ", "").Equals(title.ToLower())).ToList();

            foreach (var mobile in mobiles) {
                if (World.OPL.TryGetNameAndData(mobile.Serial, out string name, out string data)) {
                    if (!string.IsNullOrEmpty(data) && !title.Contains("animal")) {
                        continue;
                    }
                    
                    const string PATTERN = @"(?<=The\s).*(?=\s\()";
                    var match = Regex.Match(name, PATTERN);

                    if (match.Success) {
                        var mobTitle = match.Value;
                        if (mobTitle.ToLower().Replace("the ", "").Equals(title.ToLower())) {
                            return mobile;
                        }
                    }
                }
            }
            return null;
        }
    }
}
