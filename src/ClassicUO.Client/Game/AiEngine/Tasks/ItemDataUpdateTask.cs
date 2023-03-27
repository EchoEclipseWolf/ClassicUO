using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AkatoshQuester.Helpers.LightGeometry;
using ClassicUO.AiEngine;
using ClassicUO.AiEngine.AiEngineTasks;
using ClassicUO.Game.AiEngine.AiClasses;
using ClassicUO.Game.AiEngine.Helpers;
using ClassicUO.Game.AiEngine.Memory;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Network;
// ReSharper disable InconsistentNaming

namespace ClassicUO.Game.AiEngine.Tasks
{
    public class ItemDataUpdateTask : BaseAITask {
        internal static AiContainer PlayerBackpack;

        private Stopwatch _timer = Stopwatch.StartNew();
        private const int DELAY = 500;

        internal static List<AIItem> EquippedItems = new();
        internal static List<AIItem> AllEquipableItems = new();


        public static int LowerReagentCost = 0;
        public static int LowerManaCost = 0;
        public static int SpellDamageIncrease = 0;
        public static int FasterCasting = 0;
        public static int FasterCastRecovery = 0;
        public static int WeaponDamageIncrease = 0;
        public static int SwingSpeedIncrease = 0;
        public static int DefenseChanceIncrease = 0;
        public static int HitChanceIncrease = 0;
        public static int Luck = 0;

        public const int FasterCastingCap = 4;
        public const int FasterCastRecoveryCap = 6;
        public const int LowerManaCostCap = 40;
        public const int LowerReagentCostCap = 100;
        public const int DefenseChanceIncreaseCap = 45;
        public const int DamageIncreaseCap = 100;
        public const int HitChanceIncreaseCap = 45;
        public const int SwingSpeedIncreaseCap = 60;

        

        public ItemDataUpdateTask() : base("Item Data Update Task")
        {
        }

        public bool IsFasterCastingMax => FasterCasting >= FasterCastingCap;
        public bool IsFasterCastRecoveryMax => FasterCastRecovery >= FasterCastRecoveryCap;
        public bool IsLowerManaCostCap => LowerManaCost >= LowerManaCostCap;
        public bool IsLowerReagentCostCap => LowerReagentCost >= LowerReagentCostCap;
        public bool IsDefenseChanceIncreaseCap => DefenseChanceIncrease >= DefenseChanceIncreaseCap;
        public bool IsDamageIncreaseCap => WeaponDamageIncrease >= DamageIncreaseCap;
        public bool IsHitChanceIncreaseCap => HitChanceIncrease >= HitChanceIncreaseCap;
        public bool IsSwingSpeedIncreaseCap => SwingSpeedIncrease >= SwingSpeedIncreaseCap;


        public override int Priority()
        {
            return 1;
        }

        public override async Task<bool> Pulse() {
            if (_timer.ElapsedMilliseconds < DELAY)
            {
                return false;
            }
            _timer.Restart();
            var stopwatch = Stopwatch.StartNew();



            var backpack = World.Player.FindItemByLayer(Layer.Backpack);
            if (PlayerBackpack == null && backpack != null) {
                PlayerBackpack = AiContainer.GetContainer(backpack.Serial);
            }

            if (PlayerBackpack == null) {
                return false;
            }

            //await PlayerBackpack.UpdateContents();


            var equippedItemTuples = ItemsHelper.GetEquippedItemsWithData();
            var equippedItems = new List<AIItem>();

            foreach (var equippedItem in equippedItemTuples) {
                var aiItem = new AIItem(equippedItem.Item1.Serial, equippedItem);
                equippedItems.Add(aiItem);
                if (AllEquipableItems.All(i => i.Serial != aiItem.Serial)) {
                    AllEquipableItems.Add(aiItem);
                }
            }

            EquippedItems = equippedItems;

            LowerReagentCost = equippedItems.Where(equippedItem => equippedItem.LowerReagentCost > 0).Sum(equippedItem => equippedItem.LowerReagentCost);
            LowerManaCost = equippedItems.Where(equippedItem => equippedItem.LowerManaCost > 0).Sum(equippedItem => equippedItem.LowerManaCost);
            SpellDamageIncrease = equippedItems.Where(equippedItem => equippedItem.SpellDamageIncrease > 0).Sum(equippedItem => equippedItem.SpellDamageIncrease);
            FasterCasting = equippedItems.Where(equippedItem => equippedItem.FasterCasting > 0).Sum(equippedItem => equippedItem.FasterCasting);
            FasterCastRecovery = equippedItems.Where(equippedItem => equippedItem.FasterCastRecovery > 0).Sum(equippedItem => equippedItem.FasterCastRecovery);
            WeaponDamageIncrease = equippedItems.Where(equippedItem => equippedItem.DamageIncrease > 0).Sum(equippedItem => equippedItem.DamageIncrease);
            SwingSpeedIncrease = equippedItems.Where(equippedItem => equippedItem.SwingSpeedIncrease > 0).Sum(equippedItem => equippedItem.SwingSpeedIncrease);
            DefenseChanceIncrease = equippedItems.Where(equippedItem => equippedItem.DefenseChanceIncrease > 0).Sum(equippedItem => equippedItem.DefenseChanceIncrease);
            HitChanceIncrease = equippedItems.Where(equippedItem => equippedItem.HitChanceIncrease > 0).Sum(equippedItem => equippedItem.HitChanceIncrease);
            Luck = equippedItems.Where(equippedItem => equippedItem.Luck > 0).Sum(equippedItem => equippedItem.Luck);


            var backpackPlayerItemsTuple = await ItemsHelper.GetPlayerBackpackItems(false);
            var equippableBackpackItems = new List<AIItem>();

            foreach (var tuple in backpackPlayerItemsTuple) {
                var item = tuple.Item2;
                if (SerialHelper.IsValid(item.Serial) && World.OPL.TryGetNameAndData(item.Serial, out string name, out string data)) {
                    var layer = (Layer) item.ItemData.Layer;

                    if (IsEquippable(layer)) {
                        var aiItem = new AIItem(item.Serial, new Tuple<Item, string, Layer, string>(item, data, layer, name));
                        equippableBackpackItems.Add(aiItem);

                        if (AllEquipableItems.All(i => i.Serial != item.Serial)) {
                            AllEquipableItems.Add(aiItem);
                        }
                    }
                }
                else if (SerialHelper.IsValid(item.Serial))
                {
                    PacketHandlers.AddMegaClilocRequest(item.Serial);
                }
            }

            var containerGumpsToParse = new List<ContainerGump>();
            foreach (var gump in GumpHelper.GetContainerGrumps()) {
                if (gump.LocalSerial > 0) {
                    var item = World.Items.Get(gump.LocalSerial);
                    if (item != null && item.Serial != PlayerBackpack.Serial && !PlayerBackpack.ContainsSerial(item.Serial, true)) {
                        containerGumpsToParse.Add(gump);
                    }
                }
            }

            var otherOpenContainers = new List<AiContainer>();
            var allOtherOpenItems = new List<AIItem>();
            foreach (var containerGump in containerGumpsToParse) {
                continue;
                var item = World.Items.Get(containerGump.LocalSerial);
                if (item != null && SerialHelper.IsValid(item.Serial)) {
                    var containerItem = AiContainer.GetContainer(item.Serial);
                    await containerItem.UpdateContents();

                    allOtherOpenItems.AddRange(containerItem.GetItems(true));
                    otherOpenContainers.Add(containerItem);
                }
            }

            foreach (var item in allOtherOpenItems.Where(i => IsEquippable(i.Slot))) {
                if (AllEquipableItems.Any(i => i.Serial == item.Serial)) {
                    continue;
                }

                AllEquipableItems.Add(item);
            }

            foreach (var item in AllEquipableItems) {
                item.Update();
            }

            foreach (var item in EquippedItems) {
                item.Update();
            }

            var time = stopwatch.ElapsedMilliseconds;

            return false;
        }

        private bool IsEquippable(Layer layer) {
            if (layer != Layer.Invalid && 
                layer != Layer.Backpack && 
                layer != Layer.Bank && 
                layer != Layer.Hair && 
                layer != Layer.Beard && 
                layer != Layer.Face) {
                return true;
            }
            return false;
        }

        
    }
}