using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AkatoshQuester.Helpers.LightGeometry;
using ClassicUO.AiEngine;
using ClassicUO.AiEngine.AiEngineTasks;
using ClassicUO.Game.AiEngine.Enums;
using ClassicUO.Game.AiEngine.Helpers;
using ClassicUO.Game.AiEngine.Memory;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.Game.AiEngine.Tasks {
    public class ItemLearnerTask : BaseAITask {
        private Stopwatch _timer = Stopwatch.StartNew();
        private const int DELAY = 500;

        private const string GOLD_REGEX_STRING = "Gold:\\s*(\\d+)";
        private const string TOKENS_REGEX_STRING = "Tokens:\\s*(\\d+)";
        private readonly Regex _goldRegex = new Regex(GOLD_REGEX_STRING);
        private readonly Regex _tokensRegex = new Regex(TOKENS_REGEX_STRING);
        private int _startingGold = -1;
        private int _startingTokens = -1;
        private Stopwatch _masterStorageTimer = Stopwatch.StartNew();
        private const int MASTER_STORAGE_CHECK_DELAY = 100;
        public static bool ShouldResetGoldTokens = false;

        public ItemLearnerTask() : base("Item Learner Task") {
        }

        public override int Priority() {
            return 1;
        }

        public override async Task<bool> Pulse() {
            if (_timer.ElapsedMilliseconds < DELAY) {
                return false;
            }

            _timer.Restart();

            if (ItemDataUpdateTask.MasterLootContainer == null) {
                return false;
            }

            if (_masterStorageTimer.ElapsedMilliseconds >= MASTER_STORAGE_CHECK_DELAY) {
                if (SerialHelper.IsValid(ItemDataUpdateTask.MasterLootContainer.Serial) && World.OPL.TryGetNameAndData
                        (ItemDataUpdateTask.MasterLootContainer.Serial, out string name, out string data)) {

                    var match = _goldRegex.Match(data);
                    var tokenMatch = _tokensRegex.Match(data);

                    if (match.Success && match.Groups.Count > 1 && tokenMatch.Success && tokenMatch.Groups.Count > 1) {
                        var goldString = match.Groups[1].Value;

                        if (int.TryParse(goldString, out var gold)) {
                            if (_startingGold == -1 || ShouldResetGoldTokens) {
                                GameActions.Print($"[ItemLearner]: Setting Starting Gold: {gold:N}");
                                _startingGold = gold;
                            }
                            else {
                                var newGold = gold - _startingGold;
                                AIEngineOverlayGump.GainedGold = newGold;
                            }
                        }

                        var tokenString = tokenMatch.Groups[1].Value;

                        if (int.TryParse(tokenString, out var tokens)) {
                            if (_startingTokens == -1 || ShouldResetGoldTokens) {
                                GameActions.Print($"[ItemLearner]: Setting Starting Tokens: {tokens:N}");
                                _startingTokens = tokens;
                            }
                            else {
                                var newTokens = tokens - _startingTokens;
                                AIEngineOverlayGump.GainedTokens = newTokens;
                            }
                        }

                        if (ShouldResetGoldTokens) {
                            ShouldResetGoldTokens = false;
                        }
                    }

                    var elapsed = _masterStorageTimer.ElapsedMilliseconds;
                    _masterStorageTimer.Restart();
                }

            }


            ItemsMemory.Instance.Runebooks.Clear();

            var runebooksInBackpack = await ItemsHelper.GetPlayerBackpackItemsById(false, 0x22C5, -1);

            foreach (var runebookTuples in runebooksInBackpack.Where(i => i.Item != null)) {
                var item = runebookTuples.Item;

                if (item != null) {
                    ItemsMemory.Instance.AddRunebook(item, item.Container, ItemLocationEnum.PlayerBackpack);
                }
            }

            return true;
        }
    }
}