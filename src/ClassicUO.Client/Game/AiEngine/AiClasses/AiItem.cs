using ClassicUO.Game.GameObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ClassicUO.Game.AiEngine.Helpers;
using ClassicUO.Game.AiEngine.Tasks;
using ClassicUO.Game.Data;

namespace ClassicUO.Game.AiEngine.AiClasses
{
    // ReSharper disable once InconsistentNaming
    public class AIItem {
        private const string DurabilityRegex = "Durability\\s+(\\d+)[^\\d]+(\\d+)";
        private const string WeaponDamageRegex = "(\\d+) *- *(\\d+)";
        private const string BagContentsRegex = "(\\d+)\\/(\\d+) *Items, *(\\d+)/(\\d+) *Stones";
        private const string SetRegexString = "(<br>Only When Full Set Is Present:[\\s\\S]*)";
        private Regex SetRegex = new Regex(SetRegexString);

        public uint Serial;

        public double AiScore;

        public string Name;
        internal Item Item;
        public uint ContainerSerial;
        public int Weight;
        public int Durability;
        public int MaxDurability;
        public int BattleRating;
        public int RequiredLevel;
        public float WeaponSpeed;
        public int Range = 1;
        public int SelfRepair;
        public bool Exceptional;
        public bool SpellChanneling;
        public bool Blessed;
        public bool MageArmor;
        public bool NightSight;
        public Layer Slot;
        public string Data;
        public int HitChanceIncrease;
        public int DefenseChanceIncrease;
        public int LowerReagentCost;
        public int LowerManaCost;
        public int Luck;
        public int Level;
        public int Experience;
        public int ArtifactRarity;
        public int DurabilityBoost;
        public int StrengthRequirement;

        //Bag Stats
        public int ItemCountCurrent;
        public int ItemCountMax;
        public int StoneCountCurrent;
        public int StoneCountMax;


        //Hit Stats
        public int HitPhysical;
        public int HitPhysicalArea;
        public int HitFireball;
        public int HitFireballArea;
        public int HitLightning;
        public int HitLightningArea;
        public int HitCold;
        public int HitColdArea;
        public int HitPoison;
        public int HitPoisonArea;
        public int HitEnergy;
        public int HitEnergyArea;
        public int HitLifeLeech;
        public int HitStaminaLeech;
        public int HitManaLeech;
        public int HitDispel;
        public int HitHarm;
        public int HitExplosion;
        public int HitMagicArrow;
        public int HitLowerAttack;
        public int HitLowerDefense;

        public int PhysicalDamage;
        public int FireDamage;
        public int ColdDamage;
        public int EnergyDamage;
        public int ChaosDamage;
        public int DirectDamage;


        public int Swarm;
        public int Sparks;

        public int WeaponDamageMin;
        public int WeaponDamageMax;
        public int MageWeaponSkill;
        public string SlayerType = "";

        //Bows
        public int Ammo;
        public int BowDamageModifier;
        public int LowerAmmoCost;
        public int WeightReduction;

        //Spell Books
        public int SpellBookCount;

        //Defense Stats
        public int FireEater;
        public int ColdEater;
        public int PoisonEater;
        public int EnergyEater;
        public int KineticEater;
        public int DamageEater;
        public int FireResonance;
        public int ColdResonance;
        public int PoisonResonance;
        public int EnergyResonance;
        public int KineticResonance;

        //Regen / Buff Stats
        public int IntBonus;
        public int StrBonus;
        public int DexBonus;
        public int HitPointIncrease;
        public int StaminaIncrease;
        public int ManaIncrease;
        public int HitPointRegeneration;
        public int ManaRegeneration;
        public int StaminaRegeneration;
        public int FasterCastRecovery;
        public int FasterCasting;
        public int SwingSpeedIncrease;
        public int DamageIncrease;
        public int Velocity;
        public int EnhancePotions;
        public int SpellDamageIncrease;
        


        //ArmorStats
        public int ReflectPhysicalDamage;
        public int PhysicalResist;
        public int FireResist;
        public int ColdResist;
        public int EnergyResist;
        public int PoisonResist;

        //Skill Boosts
        public int TacticsBoost;
        public int ArcheryBoost;
        public int AnatomyBoost;
        public int ChivalryBoost;
        public int AnimalLoreBoost;
        public int AnimalTamingBoost;
        public int PeacemakingBoost;
        public int BushidoBoost;
        public int HealingBoost;
        public int ParryingBoost;

        internal AIItem() {

        }

        internal AIItem(uint serial, Tuple<Item, string, Layer, string> itemTuple) {
            Serial = serial;
            ParseData(itemTuple.Item1, itemTuple.Item2, itemTuple.Item3, itemTuple.Item4);
        }

        internal AIItem(uint serial, Tuple<Item, string, string> itemTuple) {
            Serial = serial;
            var layer = (Layer) itemTuple.Item1.ItemData.Layer;
            ParseData(itemTuple.Item1, itemTuple.Item3, layer, itemTuple.Item2);
        }

        internal bool IsWeapon => Slot is Layer.OneHanded or Layer.TwoHanded;
        internal bool IsArmor => !IsWeapon;
        internal bool UsesDurability => MaxDurability > 0;
        internal bool IsSlayerWeapon => !string.IsNullOrEmpty(SlayerType);
        internal int Amount => Item.Amount;
        internal bool IsEquipped => ItemsHelper.IsEquipped(Serial);

        private void Reset() {
            AiScore = 0;
            Name = "";
            Item = null;
            Weight = 0;
            Durability = 0;
            MaxDurability = 0;
            BattleRating = 0;
            RequiredLevel = 0;
            WeaponSpeed = 0;
            Range = 1;
            SelfRepair = 0;
            Exceptional = false;
            SpellChanneling = false;
            Blessed = false;
            MageArmor = false;
            NightSight = false;
            Slot = 0;
            Data = "";
            HitChanceIncrease = 0;
            DefenseChanceIncrease = 0;
            LowerReagentCost = 0;
            LowerManaCost = 0;
            Luck = 0;
            Level = 0;
            Experience = 0;
            ArtifactRarity = 0;
            DurabilityBoost = 0;
            StrengthRequirement = 0;
            ItemCountCurrent = 0;
            ItemCountMax = 0;
            StoneCountCurrent = 0;
            StoneCountMax = 0;
            HitPhysical = 0;
            HitPhysicalArea = 0;
            HitFireball = 0;
            HitFireballArea = 0;
            HitLightning = 0;
            HitLightningArea = 0;
            HitCold = 0;
            HitColdArea = 0;
            HitPoison = 0;
            HitPoisonArea = 0;
            HitEnergy = 0;
            HitEnergyArea = 0;
            HitLifeLeech = 0;
            HitStaminaLeech = 0;
            HitManaLeech = 0;
            HitDispel = 0;
            HitHarm = 0;
            HitExplosion = 0;
            HitMagicArrow = 0;
            HitLowerAttack = 0;
            HitLowerDefense = 0;
            PhysicalDamage = 0;
            FireDamage = 0;
            ColdDamage = 0;
            EnergyDamage = 0;
            ChaosDamage = 0;
            DirectDamage = 0;
            Swarm = 0;
            Sparks = 0;
            WeaponDamageMin = 0;
            WeaponDamageMax = 0;
            MageWeaponSkill = 0;
            SlayerType = "";
            Ammo = 0;
            BowDamageModifier = 0;
            LowerAmmoCost = 0;
            WeightReduction = 0;
            SpellBookCount = 0;
            FireEater = 0;
            ColdEater = 0;
            PoisonEater = 0;
            EnergyEater = 0;
            KineticEater = 0;
            DamageEater = 0;
            FireResonance = 0;
            ColdResonance = 0;
            PoisonResonance = 0;
            EnergyResonance = 0;
            KineticResonance = 0;
            IntBonus = 0;
            StrBonus = 0;
            DexBonus = 0;
            HitPointIncrease = 0;
            StaminaIncrease = 0;
            ManaIncrease = 0;
            HitPointRegeneration = 0;
            ManaRegeneration = 0;
            StaminaRegeneration = 0;
            FasterCastRecovery = 0;
            FasterCasting = 0;
            SwingSpeedIncrease = 0;
            DamageIncrease = 0;
            Velocity = 0;
            EnhancePotions = 0;
            SpellDamageIncrease = 0;
            ReflectPhysicalDamage = 0;
            PhysicalResist = 0;
            FireResist = 0;
            ColdResist = 0;
            EnergyResist = 0;
            PoisonResist = 0;
            TacticsBoost = 0;
            ArcheryBoost = 0;
            AnatomyBoost = 0;
            ChivalryBoost = 0;
            AnimalLoreBoost = 0;
            AnimalTamingBoost = 0;
            PeacemakingBoost = 0;
            BushidoBoost = 0;
            HealingBoost = 0;
            ParryingBoost = 0;
        }

        private void ParseData(Item item, string data, Layer slot, string name) {
            Item = item;
            Data = data;
            Slot = slot;
            Name = name;

            if (string.IsNullOrEmpty(Name))
            {
                Name = item.Name;
            }

            var setMatch = SetRegex.Match(data);

            if (setMatch.Success) {
                Data = Data.Replace(setMatch.Value, "");
            }

            if (!string.IsNullOrEmpty(Data))
            {
                var split = Data.Split('\n').ToList();
                split.RemoveAt(0); // Removes the name

                if (split.Count > 0)
                {
                    for (int i = 0; i < 2; i++) {
                        var exceptionalString = FindBy("Exceptional", split);
                        Exceptional = !string.IsNullOrEmpty(exceptionalString);

                        var spellChannelingString = FindBy("Spell Channeling", split);
                        SpellChanneling = !string.IsNullOrEmpty(spellChannelingString);

                        var blessedString = FindBy("Blessed", split);
                        Blessed = !string.IsNullOrEmpty(blessedString);

                        var mageArmorString = FindBy("Mage Armor", split);
                        MageArmor = !string.IsNullOrEmpty(mageArmorString);

                        var nightSightString = FindBy("Night Sight", split);
                        NightSight = !string.IsNullOrEmpty(nightSightString);

                        var weightString = FindBy("Weight:", split);

                        if (!string.IsNullOrEmpty(weightString)) {
                            Weight = AIDataParser.ParseInt(weightString);
                        }
                        else {
                            Weight = 0;
                        }

                        var weaponSpeedString = FindBy("Weapon Speed", split);

                        if (!string.IsNullOrEmpty(weaponSpeedString)) {
                            WeaponSpeed = AIDataParser.ParseFloat(weaponSpeedString);
                        }
                        else {
                            WeaponSpeed = 0;
                        }

                        var rangeString = FindBy("Range", split);

                        if (!string.IsNullOrEmpty(rangeString)) {
                            Range = AIDataParser.ParseInt(rangeString);
                        }
                        else {
                            Range = 1;
                        }

                        var durabilityLineString = FindByRegex(DurabilityRegex, split);
                        if (!string.IsNullOrEmpty(durabilityLineString)) {
                            var regex = new Regex(DurabilityRegex);
                            var match = regex.Match(durabilityLineString);

                            if (match.Success && match.Groups.Count > 2) {
                                var durabilityString = match.Groups[1].Value;
                                var maxDurabilityString = match.Groups[2].Value;

                                if (int.TryParse(durabilityString, out var durability) && int.TryParse(maxDurabilityString, out var maxDurability)) {
                                    Durability = durability;
                                    MaxDurability = maxDurability;
                                }
                                else {
                                    Durability = 0;
                                    MaxDurability = 0;
                                }
                            }
                        }

                        var weaponDamageString = FindBy("Weapon Damage ", split);
                        if (!string.IsNullOrEmpty(weaponDamageString)) {
                            
                            var regex = new Regex(WeaponDamageRegex);
                            var match = regex.Match(weaponDamageString);

                            if (match.Success && match.Groups.Count > 2) {
                                var minString = match.Groups[1].Value;
                                var maxString = match.Groups[2].Value;

                                if (int.TryParse(minString, out var min) && int.TryParse(maxString, out var max)) {
                                    WeaponDamageMin = min;
                                    WeaponDamageMax = max;
                                }
                                else {
                                    WeaponDamageMin = 0;
                                    WeaponDamageMax = 0;
                                }
                            }
                        }

                        var bagContentsString = FindBy("Contents: ", split);
                        if (!string.IsNullOrEmpty(bagContentsString)) {
                            
                            var regex = new Regex(BagContentsRegex);
                            var match = regex.Match(bagContentsString);

                            if (match.Success && match.Groups.Count > 4) {
                                var minString = match.Groups[1].Value;
                                var maxString = match.Groups[2].Value;
                                var minStonesString = match.Groups[3].Value;
                                var maxStonesString = match.Groups[4].Value;

                                if (int.TryParse(minString, out var min) && int.TryParse(maxString, out var max) && int.TryParse(minStonesString, out var currentStones) && int.TryParse(maxStonesString, out var maxStones)) {
                                    ItemCountCurrent = min;
                                    ItemCountMax = max;
                                    StoneCountCurrent = currentStones;
                                    StoneCountMax = maxStones;
                                }
                                else {
                                    ItemCountCurrent = 0;
                                    ItemCountMax = 0;
                                    StoneCountCurrent = 0;
                                    StoneCountMax = 0;
                                }
                            }
                        }


                        var battleRatingString = FindBy("<BaseFont Color=#0070FF>[Battle Rating", split);

                        if (!string.IsNullOrEmpty(battleRatingString)) {
                            BattleRating = AIDataParser.ParseInt(battleRatingString.Replace("<BaseFont Color=#0070FF>[Battle Rating]:<BASEFONT COLOR=#FFFFFF>", ""));
                        }
                        else {
                            BattleRating = 0;
                        }

                        var requiredLevelString = FindBy("<BaseFont Color=#FF0000>[Required Level", split);

                        if (!string.IsNullOrEmpty(requiredLevelString)) {
                            RequiredLevel = AIDataParser.ParseInt(requiredLevelString.Replace("<BaseFont Color=#FF0000>[Required Level]:<BASEFONT COLOR=#FFFFFF>", ""));
                        }
                        else {
                            RequiredLevel = 0;
                        }

                        var selfRepairString = FindBy("Self Repair", split);

                        if (!string.IsNullOrEmpty(selfRepairString)) {
                            SelfRepair = AIDataParser.ParseInt(selfRepairString);
                        }


                        var foundString = FindBy("Hit Chance Increase", split);

                        if (!string.IsNullOrEmpty(foundString)) {
                            HitChanceIncrease = AIDataParser.ParseInt(foundString);
                        }

                        foundString = FindBy("Defense Chance Increase", split);

                        if (!string.IsNullOrEmpty(foundString)) {
                            DefenseChanceIncrease = AIDataParser.ParseInt(foundString);
                        }

                        foundString = FindBy("Lower Reagent Cost", split);

                        if (!string.IsNullOrEmpty(foundString)) {
                            LowerReagentCost = AIDataParser.ParseInt(foundString);
                        }

                        foundString = FindBy("Lower Mana Cost", split);

                        if (!string.IsNullOrEmpty(foundString)) {
                            LowerManaCost = AIDataParser.ParseInt(foundString);
                        }

                        foundString = FindBy("Luck", split);

                        if (!string.IsNullOrEmpty(foundString)) {
                            Luck = AIDataParser.ParseInt(foundString);
                        }

                        foundString = FindBy("Artifact Rarity", split);

                        if (!string.IsNullOrEmpty(foundString)) {
                            ArtifactRarity = AIDataParser.ParseInt(foundString);
                        }

                        foundString = FindBy("<BASEFONT COLOR=#FF8000>[Level", split);

                        if (!string.IsNullOrEmpty(foundString)) {
                            Level = AIDataParser.ParseInt(foundString.Replace("<BASEFONT COLOR=#FF8000>[Level", ""));
                        }

                        foundString = FindBy("<BASEFONT COLOR=#1EFF00>[Experience", split);

                        if (!string.IsNullOrEmpty(foundString)) {
                            Experience = AIDataParser.ParseInt(foundString.Replace("<BASEFONT COLOR=#1EFF00>[Experience", ""));
                        }

                        foundString = FindBy("Durability", split);

                        if (!string.IsNullOrEmpty(foundString)) {
                            DurabilityBoost = AIDataParser.ParseInt(foundString);
                        }

                        foundString = FindBy("Strength Requirement", split);

                        if (!string.IsNullOrEmpty(foundString)) {
                            StrengthRequirement = AIDataParser.ParseInt(foundString);
                        }

                        //Hit Stats
                        var hitString = FindByWithoutWord("Hit Physical", "Area", split);

                        if (!string.IsNullOrEmpty(hitString)) {
                            HitPhysical = AIDataParser.ParseInt(hitString);
                        }

                        hitString = FindBy("Hit Physical Area", split);

                        if (!string.IsNullOrEmpty(hitString)) {
                            HitPhysicalArea = AIDataParser.ParseInt(hitString);
                        }

                        hitString = FindByWithoutWord("Hit Fireball", "Area", split);

                        if (!string.IsNullOrEmpty(hitString)) {
                            HitFireball = AIDataParser.ParseInt(hitString);
                        }

                        hitString = FindBy("Hit Fire Area", split);

                        if (!string.IsNullOrEmpty(hitString)) {
                            HitFireballArea = AIDataParser.ParseInt(hitString);
                        }


                        hitString = FindByWithoutWord("Hit Lightning", "Area", split);

                        if (!string.IsNullOrEmpty(hitString)) {
                            HitLightning = AIDataParser.ParseInt(hitString);
                        }

                        hitString = FindBy("Hit Lightning Area", split);

                        if (!string.IsNullOrEmpty(hitString)) {
                            HitLightningArea = AIDataParser.ParseInt(hitString);
                        }


                        hitString = FindByWithoutWord("Hit Cold", "Area", split);

                        if (!string.IsNullOrEmpty(hitString)) {
                            HitCold = AIDataParser.ParseInt(hitString);
                        }

                        hitString = FindBy("Hit Cold Area", split);

                        if (!string.IsNullOrEmpty(hitString)) {
                            HitColdArea = AIDataParser.ParseInt(hitString);
                        }


                        hitString = FindByWithoutWord("Hit Poison", "Area", split);

                        if (!string.IsNullOrEmpty(hitString)) {
                            HitPoison = AIDataParser.ParseInt(hitString);
                        }

                        hitString = FindBy("Hit Poison Area", split);

                        if (!string.IsNullOrEmpty(hitString)) {
                            HitPoisonArea = AIDataParser.ParseInt(hitString);
                        }

                        hitString = FindByWithoutWord("Hit Energy", "Area", split);

                        if (!string.IsNullOrEmpty(hitString)) {
                            HitEnergy = AIDataParser.ParseInt(hitString);
                        }

                        hitString = FindBy("Hit Energy Area", split);

                        if (!string.IsNullOrEmpty(hitString)) {
                            HitEnergyArea = AIDataParser.ParseInt(hitString);
                        }

                        hitString = FindBy("Hit Life Leech", split);

                        if (!string.IsNullOrEmpty(hitString)) {
                            HitLifeLeech = AIDataParser.ParseInt(hitString);
                        }

                        hitString = FindBy("Hit Stamina Leech", split);

                        if (!string.IsNullOrEmpty(hitString)) {
                            HitStaminaLeech = AIDataParser.ParseInt(hitString);
                        }

                        hitString = FindBy("Hit Mana Leech", split);

                        if (!string.IsNullOrEmpty(hitString)) {
                            HitManaLeech = AIDataParser.ParseInt(hitString);
                        }

                        hitString = FindBy("Hit Dispel", split);

                        if (!string.IsNullOrEmpty(hitString)) {
                            HitDispel = AIDataParser.ParseInt(hitString);
                        }

                        hitString = FindBy("Hit Harm", split);

                        if (!string.IsNullOrEmpty(hitString)) {
                            HitHarm = AIDataParser.ParseInt(hitString);
                        }

                        hitString = FindBy("Hit Explosion", split);

                        if (!string.IsNullOrEmpty(hitString)) {
                            HitExplosion = AIDataParser.ParseInt(hitString);
                        }

                        hitString = FindBy("Hit Magic Arrow", split);

                        if (!string.IsNullOrEmpty(hitString)) {
                            HitMagicArrow = AIDataParser.ParseInt(hitString);
                        }

                        hitString = FindBy("Hit Lower Attack", split);

                        if (!string.IsNullOrEmpty(hitString)) {
                            HitLowerAttack = AIDataParser.ParseInt(hitString);
                        }

                        hitString = FindBy("Hit Lower Defense", split);

                        if (!string.IsNullOrEmpty(hitString)) {
                            HitLowerDefense = AIDataParser.ParseInt(hitString);
                        }

                        hitString = FindBy("Physical Damage", split);

                        if (!string.IsNullOrEmpty(hitString)) {
                            PhysicalDamage = AIDataParser.ParseInt(hitString);
                        }

                        hitString = FindBy("Fire Damage", split);

                        if (!string.IsNullOrEmpty(hitString)) {
                            FireDamage = AIDataParser.ParseInt(hitString);
                        }

                        hitString = FindBy("Cold Damage", split);

                        if (!string.IsNullOrEmpty(hitString)) {
                            ColdDamage = AIDataParser.ParseInt(hitString);
                        }

                        hitString = FindBy("Energy Damage", split);

                        if (!string.IsNullOrEmpty(hitString)) {
                            EnergyDamage = AIDataParser.ParseInt(hitString);
                        }

                        hitString = FindBy("Chaos Damage", split);

                        if (!string.IsNullOrEmpty(hitString)) {
                            ChaosDamage = AIDataParser.ParseInt(hitString);
                        }

                        hitString = FindBy("Direct Damage", split);

                        if (!string.IsNullOrEmpty(hitString)) {
                            DirectDamage = AIDataParser.ParseInt(hitString);
                        }

                        hitString = FindBy("Swarm", split);

                        if (!string.IsNullOrEmpty(hitString)) {
                            Swarm = AIDataParser.ParseInt(hitString);
                        }

                        hitString = FindBy("Sparks", split);

                        if (!string.IsNullOrEmpty(hitString)) {
                            Sparks = AIDataParser.ParseInt(hitString);
                        }

                        hitString = FindBy("Mage Weapon", split);
                        if (!string.IsNullOrEmpty(hitString)) {
                            var trimmedString = hitString.Replace("Mage Weapon ", "");
                            trimmedString = trimmedString.Replace("Skill", "").Trim();

                            if (int.TryParse(trimmedString, out var value)) {
                                MageWeaponSkill = value;
                            }
                        }

                        foundString = FindByEndsWith("Slayer", split);
                        if (!string.IsNullOrEmpty(foundString)) {
                            SlayerType = foundString.Replace("Slayer", "").Trim();
                        }

                        //Bow Stats
                        foundString = FindBy("Ammo:", split);

                        if (!string.IsNullOrEmpty(foundString)) {
                            Ammo = AIDataParser.ParseInt(foundString);
                        }

                        foundString = FindBy("Damage Modifier:", split);

                        if (!string.IsNullOrEmpty(foundString)) {
                            BowDamageModifier = AIDataParser.ParseInt(foundString);
                        }

                        foundString = FindBy("Lower Ammo Cost", split);

                        if (!string.IsNullOrEmpty(foundString)) {
                            LowerAmmoCost = AIDataParser.ParseInt(foundString);
                        }

                        foundString = FindBy("Weight Reduction", split);
                        if (!string.IsNullOrEmpty(foundString)) {
                            WeightReduction = AIDataParser.ParseInt(foundString);
                        }

                        //Spell Book Stats
                        foundString = FindByEndsWith("Spells", split);
                        if (!string.IsNullOrEmpty(foundString)) {
                            SpellBookCount = AIDataParser.ParseInt(foundString);
                        }

                        //Defense Stats
                        foundString = FindBy("Fire Eater", split);

                        if (!string.IsNullOrEmpty(foundString)) {
                            FireEater = AIDataParser.ParseInt(foundString);
                        }

                        foundString = FindBy("Cold Eater", split);

                        if (!string.IsNullOrEmpty(foundString)) {
                            ColdEater = AIDataParser.ParseInt(foundString);
                        }

                        foundString = FindBy("Poison Eater", split);

                        if (!string.IsNullOrEmpty(foundString)) {
                            PoisonEater = AIDataParser.ParseInt(foundString);
                        }

                        foundString = FindBy("Energy Eater", split);

                        if (!string.IsNullOrEmpty(foundString)) {
                            EnergyEater = AIDataParser.ParseInt(foundString);
                        }

                        foundString = FindBy("Kinetic Eater", split);

                        if (!string.IsNullOrEmpty(foundString)) {
                            KineticEater = AIDataParser.ParseInt(foundString);
                        }

                        foundString = FindBy("Damage Eater", split);

                        if (!string.IsNullOrEmpty(foundString)) {
                            DamageEater = AIDataParser.ParseInt(foundString);
                        }

                        foundString = FindBy("Fire Resonance", split);

                        if (!string.IsNullOrEmpty(foundString)) {
                            FireResonance = AIDataParser.ParseInt(foundString);
                        }

                        foundString = FindBy("Cold Resonance", split);

                        if (!string.IsNullOrEmpty(foundString)) {
                            ColdResonance = AIDataParser.ParseInt(foundString);
                        }

                        foundString = FindBy("Poison Resonance", split);

                        if (!string.IsNullOrEmpty(foundString)) {
                            PoisonResonance = AIDataParser.ParseInt(foundString);
                        }

                        foundString = FindBy("Energy Resonance", split);

                        if (!string.IsNullOrEmpty(foundString)) {
                            EnergyResonance = AIDataParser.ParseInt(foundString);
                        }

                        foundString = FindBy("Kinetic Resonance", split);

                        if (!string.IsNullOrEmpty(foundString)) {
                            KineticResonance = AIDataParser.ParseInt(foundString);
                        }

                        //Regen / Buff Stats
                        var regenString = FindBy("Intelligence Bonus", split);

                        if (!string.IsNullOrEmpty(regenString)) {
                            IntBonus = AIDataParser.ParseInt(regenString);
                        }

                        regenString = FindBy("Dexterity Bonus", split);

                        if (!string.IsNullOrEmpty(regenString)) {
                            DexBonus = AIDataParser.ParseInt(regenString);
                        }

                        regenString = FindBy("Strength Bonus", split);

                        if (!string.IsNullOrEmpty(regenString)) {
                            StrBonus = AIDataParser.ParseInt(regenString);
                        }

                        regenString = FindBy("Hit Point Increase", split);

                        if (!string.IsNullOrEmpty(regenString)) {
                            HitPointIncrease = AIDataParser.ParseInt(regenString);
                        }

                        regenString = FindBy("Stamina Increase", split);

                        if (!string.IsNullOrEmpty(regenString)) {
                            StaminaIncrease = AIDataParser.ParseInt(regenString);
                        }

                        regenString = FindBy("Mana Increase", split);

                        if (!string.IsNullOrEmpty(regenString)) {
                            ManaIncrease = AIDataParser.ParseInt(regenString);
                        }

                        regenString = FindBy("Hit Point Regeneration", split);

                        if (!string.IsNullOrEmpty(regenString)) {
                            HitPointRegeneration = AIDataParser.ParseInt(regenString);
                        }

                        regenString = FindBy("Mana Regeneration", split);

                        if (!string.IsNullOrEmpty(regenString)) {
                            ManaRegeneration = AIDataParser.ParseInt(regenString);
                        }

                        regenString = FindBy("Stamina Regeneration", split);

                        if (!string.IsNullOrEmpty(regenString)) {
                            StaminaRegeneration = AIDataParser.ParseInt(regenString);
                        }

                        regenString = FindBy("Faster Cast Recovery", split);

                        if (!string.IsNullOrEmpty(regenString)) {
                            FasterCastRecovery = AIDataParser.ParseInt(regenString);
                        }

                        regenString = FindBy("Faster Casting", split);

                        if (!string.IsNullOrEmpty(regenString)) {
                            FasterCasting = AIDataParser.ParseInt(regenString);
                        }

                        regenString = FindBy("Swing Speed Increase", split);

                        if (!string.IsNullOrEmpty(regenString)) {
                            SwingSpeedIncrease = AIDataParser.ParseInt(regenString);
                        }

                        regenString = FindBy("Damage Increase", split);

                        if (!string.IsNullOrEmpty(regenString)) {
                            DamageIncrease = AIDataParser.ParseInt(regenString);
                        }

                        regenString = FindBy("Velocity", split);

                        if (!string.IsNullOrEmpty(regenString)) {
                            Velocity = AIDataParser.ParseInt(regenString);
                        }

                        regenString = FindBy("Enhance Potions", split);

                        if (!string.IsNullOrEmpty(regenString)) {
                            EnhancePotions = AIDataParser.ParseInt(regenString);
                        }

                        regenString = FindBy("Spell Damage Increase", split);

                        if (!string.IsNullOrEmpty(regenString)) {
                            SpellDamageIncrease = AIDataParser.ParseInt(regenString);
                        }

                        //Armor Stats
                        foundString = FindBy("Reflect Physical Damage", split);

                        if (!string.IsNullOrEmpty(foundString)) {
                            ReflectPhysicalDamage = AIDataParser.ParseInt(foundString);
                        }

                        foundString = FindBy("Physical Resist", split);

                        if (!string.IsNullOrEmpty(foundString)) {
                            PhysicalResist = AIDataParser.ParseInt(foundString);
                        }

                        foundString = FindBy("Fire Resist", split);

                        if (!string.IsNullOrEmpty(foundString)) {
                            FireResist = AIDataParser.ParseInt(foundString);
                        }

                        foundString = FindBy("Cold Resist", split);

                        if (!string.IsNullOrEmpty(foundString)) {
                            ColdResist = AIDataParser.ParseInt(foundString);
                        }

                        foundString = FindBy("Energy Resist", split);

                        if (!string.IsNullOrEmpty(foundString)) {
                            EnergyResist = AIDataParser.ParseInt(foundString);
                        }

                        foundString = FindBy("Poison Resist", split);

                        if (!string.IsNullOrEmpty(foundString)) {
                            PoisonResist = AIDataParser.ParseInt(foundString);
                        }

                        //Skill Boosts
                        foundString = FindBy("Tactics +", split);

                        if (!string.IsNullOrEmpty(foundString)) {
                            TacticsBoost = AIDataParser.ParseInt(foundString);
                        }

                        foundString = FindBy("Archery +", split);

                        if (!string.IsNullOrEmpty(foundString)) {
                            ArcheryBoost = AIDataParser.ParseInt(foundString);
                        }

                        foundString = FindBy("Anatomy +", split);

                        if (!string.IsNullOrEmpty(foundString)) {
                            AnatomyBoost = AIDataParser.ParseInt(foundString);
                        }

                        foundString = FindBy("Chivalry +", split);

                        if (!string.IsNullOrEmpty(foundString)) {
                            ChivalryBoost = AIDataParser.ParseInt(foundString);
                        }

                        foundString = FindBy("Animal Taming +", split);

                        if (!string.IsNullOrEmpty(foundString)) {
                            AnimalTamingBoost = AIDataParser.ParseInt(foundString);
                        }

                        foundString = FindBy("Animal Lore +", split);

                        if (!string.IsNullOrEmpty(foundString)) {
                            AnimalLoreBoost = AIDataParser.ParseInt(foundString);
                        }

                        foundString = FindBy("Peacemaking +", split);

                        if (!string.IsNullOrEmpty(foundString)) {
                            PeacemakingBoost = AIDataParser.ParseInt(foundString);
                        }

                        foundString = FindBy("Bushido +", split);
                        if (!string.IsNullOrEmpty(foundString)) {
                            BushidoBoost = AIDataParser.ParseInt(foundString);
                        }

                        foundString = FindBy("Healing +", split);
                        if (!string.IsNullOrEmpty(foundString)) {
                            HealingBoost = AIDataParser.ParseInt(foundString);
                        }

                        foundString = FindBy("Parrying +", split);
                        if (!string.IsNullOrEmpty(foundString)) {
                            ParryingBoost = AIDataParser.ParseInt(foundString);
                        }

                        foundString = FindBy("<BaseFont Color=#1EFF00>[Uncommon]", split);
                        foundString = FindBy("<BaseFont Color=#D6D6D6>[Common]", split);
                        foundString = FindBy("Antique", split);
                        foundString = FindBy("Lesser Artifact", split);
                        foundString = FindBy("Major Artifact", split);
                        foundString = FindBy("Fully Charged", split);
                        foundString = FindBy("Requirement: Mondain", split);
                        foundString = FindBy("Part Of A Weapon", split);
                        foundString = FindBy("Giant Serpent Protection", split);
                        foundString = FindBy("Skill Required", split);
                        foundString = FindBy("Two-handed Weapon", split);
                        foundString = FindBy("One-handed Weapon", split);
                        foundString = FindBy("Use Best Weapon Skill", split);

                        var usesRemainingString = FindBy("Uses Remaining", split);
                        if (!string.IsNullOrEmpty(usesRemainingString)) {
                            foundString = FindBy("Level: ", split);
                        }
                    }

                    int finalCount = split.Count;

                    if (finalCount > 0) {
                        int asdf = 1;
                    }

                    CalculateAiScore();
                }
            }
        }

        internal void Update() {
            if (SerialHelper.IsValid(Serial) && World.OPL.TryGetNameAndData(Serial, out string name, out string data)) {

                var item = World.Items.Get(Serial);

                if (item != null) {
                    Reset();

                    var layer = (Layer) item.ItemData.Layer;
                    ParseData(item, data, layer, name);
                }
            }
        }

        private void CalculateAiScore() {
            if (Serial == 1078309278) {
                int bob = 1;
            }

            AiScore = 0;

            AiScore += ((WeaponDamageMax + WeaponDamageMin) / 2.0) * 4.0;
            AiScore += MageWeaponSkill;

            if (IsSlayerWeapon) {
                AiScore += 10.0;
            }

            AiScore += BowDamageModifier * 10.0f;
            AiScore += LowerAmmoCost * 2.0;
            AiScore += WeightReduction;
            AiScore += Velocity * 7.0;

            AiScore += MaxDurability;

            if (WeaponSpeed > 0) {
                AiScore += (5 - WeaponSpeed) * 5.0;
            }

            AiScore += SelfRepair * 5.0;

            if (WeaponSpeed > 0) {
                AiScore += Range * 5.0;
            }

            AiScore += SpellChanneling ? 5.0 : 0.0;
            AiScore += MageArmor ? 2.0 : 0.0;

            AiScore += LowerReagentCost * 2.0;
            AiScore += LowerManaCost * 2.0;

            AiScore += HitChanceIncrease;
            AiScore += DefenseChanceIncrease;
            AiScore += Luck * 0.05;
            AiScore += DurabilityBoost;

            AiScore += HitPhysical;
            AiScore += HitPhysicalArea * 2.0;
            AiScore += HitFireball;
            AiScore += HitFireballArea * 2.0;
            AiScore += HitLightning;
            AiScore += HitLightningArea * 2.0;
            AiScore += HitCold;
            AiScore += HitColdArea * 2.0;
            AiScore += HitPoison;
            AiScore += HitPoisonArea * 2.0;
            AiScore += HitEnergy;
            AiScore += HitEnergyArea * 2.0;

            AiScore += HitLifeLeech;
            AiScore += HitStaminaLeech;
            AiScore += HitManaLeech;
            AiScore += HitDispel;
            AiScore += HitHarm;
            AiScore += HitExplosion;
            AiScore += HitMagicArrow;
            AiScore += HitLowerAttack * 3.0;
            AiScore += HitLowerDefense * 3.0;

            AiScore += PhysicalDamage * 2.0;
            AiScore += FireDamage * 2.0;
            AiScore += ColdDamage * 2.0;
            AiScore += EnergyDamage * 2.0;
            AiScore += ChaosDamage * 2.0;
            AiScore += DirectDamage * 2.0;

            AiScore += Swarm * 4.0;
            AiScore += Sparks * 2.0;

            AiScore += FireEater * 5.0;
            AiScore += ColdEater * 5.0;
            AiScore += PoisonEater * 5.0;
            AiScore += EnergyEater * 5.0;
            AiScore += KineticEater * 5.0;
            AiScore += DamageEater * 15.0;

            AiScore += FireResonance;
            AiScore += ColdResonance;
            AiScore += PoisonResonance;
            AiScore += EnergyResonance;
            AiScore += KineticResonance;

            AiScore += IntBonus * 10.0;
            AiScore += StrBonus * 10.0;
            AiScore += DexBonus * 10.0;
            AiScore += HitPointIncrease * 10.0;
            AiScore += StaminaIncrease * 10.0;
            AiScore += ManaIncrease * 10.0;
            AiScore += HitPointRegeneration * 10.0;
            AiScore += ManaRegeneration * 10.0;
            AiScore += StaminaRegeneration * 10.0;

            AiScore += Math.Min(FasterCastRecovery * 10.0, ItemDataUpdateTask.FasterCastRecoveryCap * 10.0);
            AiScore += Math.Min(FasterCasting * 10.0, ItemDataUpdateTask.FasterCastingCap * 10.0);
            AiScore += Math.Min(SwingSpeedIncrease * 3.0, ItemDataUpdateTask.SwingSpeedIncreaseCap * 3.0);
            AiScore += Math.Min(DamageIncrease * 3.0, ItemDataUpdateTask.WeaponDamageIncrease * 3.0);

            AiScore += EnhancePotions * 3.0;
            AiScore += SpellDamageIncrease * 2.0;

            AiScore += ReflectPhysicalDamage * 2.0;

            AiScore += PhysicalResist;
            AiScore += FireResist;
            AiScore += ColdResist;
            AiScore += EnergyResist;
            AiScore += PoisonResist;

            AiScore += TacticsBoost;
            AiScore += ArcheryBoost;
            AiScore += AnatomyBoost;
            AiScore += ChivalryBoost;
            AiScore += AnimalLoreBoost;
            AiScore += AnimalTamingBoost;
            AiScore += PeacemakingBoost;
            AiScore += BushidoBoost;
            AiScore += HealingBoost;
            AiScore += ParryingBoost;
        }

        private string FindBy(string startsWith, List<string> array) {
            var foundString = "";
            foreach (var s in array) {
                if (s.StartsWith(startsWith)) {
                    foundString = s; 
                    break;
                }
            }
            array.Remove(foundString);
            return foundString;
        }

        private string FindByEndsWith(string endsWith, List<string> array) {
            var foundString = "";
            foreach (var s in array) {
                if (s.EndsWith(endsWith)) {
                    foundString = s; 
                    break;
                }
            }
            array.Remove(foundString);
            return foundString;
        }

        private string FindByWithoutWord(string startsWith, string withoutWord, List<string> array)
        {
            var foundString = "";

            foreach (var s in array) {
                if (s.StartsWith(startsWith) && !s.ToLower().Contains(withoutWord.ToLower())) {
                    foundString = s;

                    break;
                }
            }
            array.Remove(foundString);
            return foundString;
        }

        private string FindByRegex(string regexToFind, List<string> array) {
            var regex = new Regex(regexToFind);

            var foundString = "";

            foreach (var s in array) {
                var match = regex.Match(s);

                if (match.Success) {
                    foundString = s;

                    break;
                }
            }
            array.Remove(foundString);
            return foundString;
        }

        public override string ToString() {
            var returnString = $"AIItem: {Name}       Score: {AiScore}        Slot: {Slot}";

            if (UsesDurability) {
                returnString += $"        Durability {Durability} / {MaxDurability}";
            }


            return returnString;
        }
    }
}
