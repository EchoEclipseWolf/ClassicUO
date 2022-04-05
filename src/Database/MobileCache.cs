using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using HtmlAgilityPack;
using Fizzler.Systems.HtmlAgilityPack;
using Newtonsoft.Json;

namespace ClassicUO.DatabaseUtility
{
    internal class MobileCache {
        internal class SingleMobCache {
            public string Name;
            public string MapName;
            public double X;
            public double Y;
            public string Fame;
            public string Karma;
            public string Alignment;
            public int GoldMin;
            public int GoldMax;
            public int StrengthMin;
            public int StrengthMax;
            public int HitPointsMin;
            public int HitPointsMax;
            public int DexterityMin;
            public int DexterityMax;
            public int StaminaMin;
            public int StaminaMax;
            public int IntelligenceMin;
            public int IntelligenceMax;
            public int ManaMin;
            public int ManaMax;
            public double BardingDifficulty;
            public double TamingDifficulty;
            public int BaseDamageMin;
            public int BaseDamageMax;
            public string PreferredFoods;

            public double WrestlingMin;
            public double WrestlingMax;
            public double PoisoningMin;
            public double PoisoningMax;
            public double TacticsMin;
            public double TacticsMax;
            public double MageryMin;
            public double MageryMax;
            public double ResistingSpellsMin;
            public double ResistingSpellsMax;
            public double EvaluatingIntelligenceMin;
            public double EvaluatingIntelligenceMax;
            public double AnatomyMin;
            public double AnatomyMax;
            public double MeditationMin;
            public double MeditationMax;
            public double DetectingHiddenMin;
            public double DetectingHiddenMax;
            public double HidingMin;
            public double HidingMax;
            public double ParryingMin;
            public double ParryingMax;
            public double HealingMin;
            public double HealingMax;
            public double NecromancyMin;
            public double NecromancyMax;
            public double SpiritSpeakMin;
            public double SpiritSpeakMax;
            public double MysticismMin;
            public double MysticismMax;
            public double FocusMin;
            public double FocusMax;
            public double SpellweavingMin;
            public double SpellweavingMax;
            public double DiscordanceMin;
            public double DiscordanceMax;
            public double BushidoMin;
            public double BushidoMax;
            public double NinjitsuMin;
            public double NinjitsuMax;
            public double ChivalryMin;
            public double ChivalryMax;
                   
            public int PhysicalResistanceMin;
            public int PhysicalResistanceMax;
            public int FireResistanceMin;
            public int FireResistanceMax;
            public int ColdResistanceMin;
            public int ColdResistanceMax;
            public int PoisonResistanceMin;
            public int PoisonResistanceMax;
            public int EnergyResistanceMin;
            public int EnergyResistanceMax;

            public SingleMobCache() {

            }
        }

        public static SingleMobCache MobCacheFromReader(SQLiteDataReader reader)
        {
            var mobCache = new SingleMobCache();
            mobCache.Name = (string)reader["Name"];
            mobCache.MapName = (string)reader["Map"];
            mobCache.X = (Int64)reader["X"];
            mobCache.Y = (Int64)reader["Y"];
            return mobCache;
        }

        private const string CachedMobilesFilename = "CachedMobiles.json";

        public static MobileCache Instance;

        private Dictionary<string, SingleMobCache> CachedMobiles = new Dictionary<string, SingleMobCache>();
        internal MobileCache() {
            Instance = this;

            if (File.Exists(CachedMobilesFilename)) {
                var text = File.ReadAllText(CachedMobilesFilename);
                CachedMobiles = JsonConvert.DeserializeObject<Dictionary<string, SingleMobCache>>(text);
            }
        }


        internal bool Contains(Mobile mobile) {
            if (CachedMobiles.ContainsKey(GetSaveName(mobile))) {
                return true;
            }

            return false;
        }

        internal SingleMobCache GetCachedMobile(Mobile mobile) {
            if (CachedMobiles.ContainsKey(GetSaveName(mobile))) {
                return CachedMobiles[GetSaveName(mobile)];
            }
            return null;
        }

        private string TrimmedTDSData(string tds) {
            var data = tds;
            data = Regex.Replace(data, @"\r\n?|\n", "");
            data = data.Trim();
            return data;
        }

        public static string MapNameFromIndex(int index) {
            if (index == 0) {
                return "Felucca";
            }

            if (index == 1) {
                return "Trammel";
            }

            if (index == 2) {
                return "Ilshenar";
            }

            if (index == 3) {
                return "Malas";
            }

            if (index == 4) {
                return "Tokuno";
            }

            if (index == 5) {
                return "TerMur";
            }

            return "Trammel";
        }

        private string ReplacedTrimmedNames(string name) {
            if (name.Equals("a raven") 
                || name.Equals("a chickadee") 
                || name.Equals("a crow") 
                || name.Equals("a magpie") 
                || name.Equals("a hawk") 
                || name.Equals("a warbler")
                || name.Equals("a swift")
                || name.Equals("a wren")
                || name.Equals("a thrush")
                || name.Equals("a towhee")
                || name.Equals("a tern")
                || name.Equals("a starling")
                || name.Equals("a tess")
                || name.Equals("a plover")
                || name.Equals("a nuthatch")
                || name.Equals("a nightingale")
                || name.Equals("a kingfisher")
                || name.Equals("a swallow")
                || name.Equals("a woodpecker")
                || name.Equals("a crossbill")
                || name.Equals("a lapwing")) {
                return "a bird";
            }

            return name;
        }

        private string ReplaceGraphicNames(string name, int graphic) {
            if (graphic == 36) {
                return "a lizardman";
            }
            if (graphic == 42) {
                return "a ratman";
            }

            return name;
        }

        public string GetSaveName(Mobile mobile) {
            var name = mobile.Name;
            name = ReplaceGraphicNames(name, mobile.Graphic);
            return name;
        }

        internal async Task<bool> CacheMobileFromWeb(Mobile mobile) {
            var saveName = GetSaveName(mobile);

            var trimmedName = ReplacedTrimmedNames(saveName);

            if (trimmedName.StartsWith("a ")) {
                trimmedName = trimmedName.TrimStart('a');
                trimmedName = trimmedName.Trim();
            }

            if (trimmedName.StartsWith("an ")) {
                trimmedName = trimmedName.TrimStart('a');
                trimmedName = trimmedName.TrimStart('n');
                trimmedName = trimmedName.Trim();
            }

            var websiteName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(trimmedName);
            websiteName = websiteName.Replace(" ", "_");

            var fullWebsite = "http://www.uoguide.com/" + websiteName;

            var web = new HtmlWeb();
            var document = web.Load(fullWebsite);
            var page = document.DocumentNode;
            var trs = page.QuerySelectorAll("tr").ToList();
            if (trs.Count == 0) {
                return false;
            }

            Dictionary<string, string> wikiData = new Dictionary<string, string>();

            foreach (var tr in trs) {
                var ths = tr.QuerySelectorAll("th").ToList();
                var tds = tr.QuerySelectorAll("td").ToList();

                if (ths.Count > 0 && tds.Count == 5) {
                    var first = ths[0].InnerText;
                    if (first.Contains("Resistances")) {
                        var physicalData = TrimmedTDSData(tds[0].InnerText);
                        var fireData = TrimmedTDSData(tds[1].InnerText);
                        var coldData = TrimmedTDSData(tds[2].InnerText);
                        var poisonData = TrimmedTDSData(tds[3].InnerText);
                        var energyData = TrimmedTDSData(tds[4].InnerText);

                        wikiData["Physical"] = physicalData;
                        wikiData["Fire"] = fireData;
                        wikiData["Cold"] = coldData;
                        wikiData["Poison"] = poisonData;
                        wikiData["Energy"] = energyData;
                    }
                }

                if (ths.Count == tds.Count) {
                    for (int i = 0; i < ths.Count; i++) {
                        var label = ths[i].InnerText;
                        var data = tds[i].InnerText;

                        label = Regex.Replace(label, @"\r\n?|\n", "");
                        data = Regex.Replace(data, @"\r\n?|\n", "");

                        label = label.Trim();
                        data = data.Trim();

                        wikiData[label] = data;
                    }
                }
            }

            if (wikiData.Count == 0) {
                return false;
            }

            var mob = new SingleMobCache();
            mob.Name = saveName;
            mob.MapName = MapNameFromIndex(World.Map.Index);

            if (saveName.Contains("wyvern")) {
                int bob = 1;
            }

            if (wikiData.ContainsKey("Fame")) {
                mob.Fame = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(wikiData["Fame"]);
            }

            if (wikiData.ContainsKey("Karma")) {
                mob.Karma = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(wikiData["Karma"]);
            }

            if (wikiData.ContainsKey("Alignment")) {
                mob.Alignment = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(wikiData["Alignment"]);
            }

            if (wikiData.ContainsKey("Preferred Foods")) {
                mob.PreferredFoods = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(wikiData["Preferred Foods"]);
            }

            if (wikiData.ContainsKey("Gold")) {
                mob.GoldMin = GetMin(wikiData["Gold"]);
                mob.GoldMax = GetMax(wikiData["Gold"]);
                if (mob.GoldMax == 0 && mob.GoldMin != 0) {
                    mob.GoldMax = mob.GoldMin;
                }
            }

            if (wikiData.ContainsKey("Strength")) {
                mob.StrengthMin = GetMin(wikiData["Strength"]);
                mob.StrengthMax = GetMax(wikiData["Strength"]);
            }

            if (wikiData.ContainsKey("Hit Points")) {
                mob.HitPointsMin = GetMin(wikiData["Hit Points"]);
                mob.HitPointsMax = GetMax(wikiData["Hit Points"]);
            }

            if (wikiData.ContainsKey("Dexterity")) {
                mob.DexterityMin = GetMin(wikiData["Dexterity"]);
                mob.DexterityMax = GetMax(wikiData["Dexterity"]);
            }

            if (wikiData.ContainsKey("Stamina")) {
                mob.StaminaMin = GetMin(wikiData["Stamina"]);
                mob.StaminaMax = GetMax(wikiData["Stamina"]);
            }

            if (wikiData.ContainsKey("Intelligence")) {
                mob.IntelligenceMin = GetMin(wikiData["Intelligence"]);
                mob.IntelligenceMax = GetMax(wikiData["Intelligence"]);
            }

            if (wikiData.ContainsKey("Mana")) {
                mob.ManaMin = GetMin(wikiData["Mana"]);
                mob.ManaMax = GetMax(wikiData["Mana"]);
            }

            if (wikiData.ContainsKey("Barding Difficulty")) {
                mob.BardingDifficulty = GetDouble(wikiData["Barding Difficulty"]);
            }

            if (wikiData.ContainsKey("Taming Difficulty")) {
                mob.TamingDifficulty = GetDouble(wikiData["Taming Difficulty"]);
            }

            if (wikiData.ContainsKey("Base Damage")) {
                mob.BaseDamageMin = GetMin(wikiData["Base Damage"]);
                mob.BaseDamageMax = GetMax(wikiData["Base Damage"]);
            }

            if (wikiData.ContainsKey("Wrestling")) {
                mob.WrestlingMin = GetMinDouble(wikiData["Wrestling"]);
                if (mob.WrestlingMin == 0) {
                    mob.WrestlingMax = 0;
                } else {
                    mob.WrestlingMax = GetMaxDouble(wikiData["Wrestling"]);
                }
            }

            if (wikiData.ContainsKey("Poisoning")) {
                mob.PoisoningMin = GetMinDouble(wikiData["Poisoning"]);
                if (mob.PoisoningMin == 0) {
                    mob.PoisoningMax = 0;
                } else {
                    mob.PoisoningMax = GetMaxDouble(wikiData["Poisoning"]);
                }
            }

            if (wikiData.ContainsKey("Tactics")) {
                mob.TacticsMin = GetMinDouble(wikiData["Tactics"]);
                if (mob.TacticsMin == 0) {
                    mob.TacticsMax = 0;
                } else {
                    mob.TacticsMax = GetMaxDouble(wikiData["Tactics"]);
                }
            }

            if (wikiData.ContainsKey("Magery")) {
                mob.MageryMin = GetMinDouble(wikiData["Magery"]);
                if (mob.MageryMin == 0) {
                    mob.MageryMax = 0;
                } else {
                    mob.MageryMax = GetMaxDouble(wikiData["Magery"]);
                }
            }

            if (wikiData.ContainsKey("Resisting Spells")) {
                mob.ResistingSpellsMin = GetMinDouble(wikiData["Resisting Spells"]);
                if (mob.ResistingSpellsMin == 0) {
                    mob.ResistingSpellsMax = 0;
                } else {
                    mob.ResistingSpellsMax = GetMaxDouble(wikiData["Resisting Spells"]);
                }
            }

            if (wikiData.ContainsKey("Evaluating Intelligence")) {
                mob.EvaluatingIntelligenceMin = GetMinDouble(wikiData["Evaluating Intelligence"]);
                if (mob.EvaluatingIntelligenceMin == 0) {
                    mob.EvaluatingIntelligenceMax = 0;
                } else {
                    mob.EvaluatingIntelligenceMax = GetMaxDouble(wikiData["Evaluating Intelligence"]);
                }
            }

            if (wikiData.ContainsKey("Anatomy")) {
                mob.AnatomyMin = GetMinDouble(wikiData["Anatomy"]);
                if (mob.AnatomyMin == 0) {
                    mob.AnatomyMax = 0;
                } else {
                    mob.AnatomyMax = GetMaxDouble(wikiData["Anatomy"]);
                }
            }

            if (wikiData.ContainsKey("Meditation")) {
                mob.MeditationMin = GetMinDouble(wikiData["Meditation"]);
                if (mob.MeditationMin == 0) {
                    mob.MeditationMax = 0;
                } else {
                    mob.MeditationMax = GetMaxDouble(wikiData["Meditation"]);
                }
            }

            if (wikiData.ContainsKey("Detecting Hidden")) {
                mob.DetectingHiddenMin = GetMinDouble(wikiData["Detecting Hidden"]);
                if (mob.DetectingHiddenMin == 0) {
                    mob.DetectingHiddenMax = 0;
                } else {
                    mob.DetectingHiddenMax = GetMaxDouble(wikiData["Detecting Hidden"]);
                }
            }

            if (wikiData.ContainsKey("Hiding")) {
                mob.HidingMin = GetMinDouble(wikiData["Hiding"]);
                if (mob.HidingMin == 0) {
                    mob.HidingMax = 0;
                } else {
                    mob.HidingMax = GetMaxDouble(wikiData["Hiding"]);
                }
            }

            if (wikiData.ContainsKey("Parrying")) {
                mob.ParryingMin = GetMinDouble(wikiData["Parrying"]);
                if (mob.ParryingMin == 0) {
                    mob.ParryingMax = 0;
                } else {
                    mob.ParryingMax = GetMaxDouble(wikiData["Parrying"]);
                }
            }

            if (wikiData.ContainsKey("Healing")) {
                mob.HealingMin = GetMinDouble(wikiData["Healing"]);
                if (mob.HealingMin == 0) {
                    mob.HealingMax = 0;
                } else {
                    mob.HealingMax = GetMaxDouble(wikiData["Healing"]);
                }
            }

            if (wikiData.ContainsKey("Necromancy")) {
                mob.NecromancyMin = GetMinDouble(wikiData["Necromancy"]);
                if (mob.NecromancyMin == 0) {
                    mob.NecromancyMax = 0;
                } else {
                    mob.NecromancyMax = GetMaxDouble(wikiData["Necromancy"]);
                }
            }

            if (wikiData.ContainsKey("Spirit Speak")) {
                mob.SpiritSpeakMin = GetMinDouble(wikiData["Spirit Speak"]);
                if (mob.SpiritSpeakMin == 0) {
                    mob.SpiritSpeakMax = 0;
                } else {
                    mob.SpiritSpeakMax = GetMaxDouble(wikiData["Spirit Speak"]);
                }
            }

            if (wikiData.ContainsKey("Mysticism")) {
                mob.MysticismMin = GetMinDouble(wikiData["Mysticism"]);
                if (mob.MysticismMin == 0) {
                    mob.MysticismMax = 0;
                } else {
                    mob.MysticismMax = GetMaxDouble(wikiData["Mysticism"]);
                }
            }

            if (wikiData.ContainsKey("Focus")) {
                mob.FocusMin = GetMinDouble(wikiData["Focus"]);
                if (mob.FocusMin == 0) {
                    mob.FocusMax = 0;
                } else {
                    mob.FocusMax = GetMaxDouble(wikiData["Focus"]);
                }
            }

            if (wikiData.ContainsKey("Spellweaving")) {
                mob.SpellweavingMin = GetMinDouble(wikiData["Spellweaving"]);
                if (mob.SpellweavingMin == 0) {
                    mob.SpellweavingMax = 0;
                } else {
                    mob.SpellweavingMax = GetMaxDouble(wikiData["Spellweaving"]);
                }
            }

            if (wikiData.ContainsKey("Discordance")) {
                mob.DiscordanceMin = GetMinDouble(wikiData["Discordance"]);
                if (mob.DiscordanceMin == 0) {
                    mob.DiscordanceMax = 0;
                } else {
                    mob.DiscordanceMax = GetMaxDouble(wikiData["Discordance"]);
                }
            }

            if (wikiData.ContainsKey("Bushido")) {
                mob.BushidoMin = GetMinDouble(wikiData["Bushido"]);
                if (mob.BushidoMin == 0) {
                    mob.BushidoMax = 0;
                } else {
                    mob.BushidoMax = GetMaxDouble(wikiData["Bushido"]);
                }
            }

            if (wikiData.ContainsKey("Ninjitsu")) {
                mob.NinjitsuMin = GetMinDouble(wikiData["Ninjitsu"]);
                if (mob.NinjitsuMin == 0) {
                    mob.NinjitsuMax = 0;
                } else {
                    mob.NinjitsuMax = GetMaxDouble(wikiData["Ninjitsu"]);
                }
            }

            if (wikiData.ContainsKey("Chivalry")) {
                mob.ChivalryMin = GetMinDouble(wikiData["Chivalry"]);
                if (mob.ChivalryMin == 0) {
                    mob.ChivalryMax = 0;
                } else {
                    mob.ChivalryMax = GetMaxDouble(wikiData["Chivalry"]);
                }
            }

            if (wikiData.ContainsKey("Physical")) {
                mob.PhysicalResistanceMin = GetMin(wikiData["Physical"]);
                mob.PhysicalResistanceMax = GetMax(wikiData["Physical"]);
            }

            if (wikiData.ContainsKey("Fire")) {
                mob.FireResistanceMin = GetMin(wikiData["Fire"]);
                mob.FireResistanceMax = GetMax(wikiData["Fire"]);
            }

            if (wikiData.ContainsKey("Cold")) {
                mob.ColdResistanceMin = GetMin(wikiData["Cold"]);
                mob.ColdResistanceMax = GetMax(wikiData["Cold"]);
            }

            if (wikiData.ContainsKey("Poison")) {
                mob.PoisonResistanceMin = GetMin(wikiData["Poison"]);
                mob.PoisonResistanceMax = GetMax(wikiData["Poison"]);
            }

            if (wikiData.ContainsKey("Energy")) {
                mob.EnergyResistanceMin = GetMin(wikiData["Energy"]);
                mob.EnergyResistanceMax = GetMax(wikiData["Energy"]);
            }

            CachedMobiles[saveName] = mob;

            Save();

            GameActions.Print($"[MobileCache] Saved new mobile: [{saveName}] {CachedMobiles.Count} cached mobiles.", 0x58, MessageType.System);

            return true;
        }

        private string CleanDataString(string data) {
            var newData = data.Replace("+/-", "");

            return newData;
        }

        private int GetMin(string data) {
            data = CleanDataString(data);

            

            if (int.TryParse(data, out var beforeParse)) {
                if (beforeParse == 0) {
                    return 0;
                }

                if (!data.Contains("-")) {
                    return beforeParse;
                }
            }

            var split = data.Split('-');
            if (split.Length != 2) {
                return 0;
            }

            var before = split[0];
            before = before.Trim();

            int num;
            if (int.TryParse(before, out num)) {
                return num;
            }

            return 0;
        }

        private int GetMax(string data) {
            data = CleanDataString(data);
            //data = Regex.Match(data, @"\d+").Value;

            var split = data.Split('-');
            if (split.Length != 2) {
                return 0;
            }

            var after = split[1];
            after = after.Trim();

            int num;
            if (int.TryParse(after, out num)) {
                return num;
            }

            return 0;
        }

        private double GetMinDouble(string data) {
            data = CleanDataString(data);
            //data = Regex.Match(data, @"\d+").Value;

            if (double.TryParse(data, out var beforeParse)) {
                if (beforeParse == 0) {
                    return 0;
                }

                if (!data.Contains("-")) {
                    return beforeParse;
                }
            }

            var split = data.Split('-');
            if (split.Length != 2) {
                return 0;
            }

            var before = split[0];
            before = before.Trim();

            double num;
            if (double.TryParse(before, out num)) {
                return num;
            }

            return 0;
        }

        private double GetMaxDouble(string data) {
            data = CleanDataString(data);
            //data = Regex.Match(data, @"\d+").Value;

            var split = data.Split('-');
            if (split.Length != 2) {
                return 0;
            }

            var after = split[1];
            after = after.Trim();

            double num;
            if (double.TryParse(after, out num)) {
                return num;
            }

            return 0;
        }

        private double GetDouble(string data) {
            data = CleanDataString(data);

            if (data.Contains("-")) {
                return GetMinDouble(data);
            } else {
                double parsedDouble;
                if (Double.TryParse(data, out parsedDouble)) {
                    return parsedDouble;
                }
            }

            return 0;
        }

        public void Save() {
            var json = JsonConvert.SerializeObject(CachedMobiles);

            if (File.Exists(CachedMobilesFilename)) {
                File.Delete(CachedMobilesFilename);
            }

            File.WriteAllText(CachedMobilesFilename, json);
        }

    }
}
