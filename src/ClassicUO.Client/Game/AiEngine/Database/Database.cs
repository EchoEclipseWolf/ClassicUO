using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ClassicUO.AiEngine;
using ClassicUO.Game;
using ClassicUO.Game.AiEngine.Memory;
using ClassicUO.Game.GameObjects;
using Microsoft.Data.Sqlite;

namespace ClassicUO.DatabaseUtility
{
    public static class Database {
        private static SQLiteConnection _connection;

        public static void CreateConnection() {
            if (_connection == null) {
                _connection = new SQLiteConnection("Data Source=C:\\UltimaOnlineSharedData\\database.db; Version = 3; New = True; Compress = True; ");
                try {
                    _connection.Open();
                } catch (Exception ex) {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine("Failed to connect to database file");
                    int bob = 1;
                }
            }
        }

        internal static async Task<bool> AddMobile(Mobile mobile) {
            #if DEBUG
            return true;
            #endif
            if (!string.IsNullOrEmpty(mobile.Name) && AiSettings.Instance.RecordDatabase) {
                if (LandmarksMemory.Instance.IsCloseToChampion(mobile.Position.ToPoint3D(), World.MapIndex)) {
                    return false;
                }

                string completeName;
                var mobileData = "";
                int level = 1;
                if (SerialHelper.IsValid(mobile.Serial) && World.OPL.TryGetNameAndData(mobile.Serial, out string name, out string data)) {
                    completeName = name;
                    mobileData = data;
                    if (data.ToLower().Contains("(tame)") || data.ToLower().Contains("(bonded)")) {
                        return false;
                    }
                }
                else {
                    return false;
                }

                if (!MobileCache.Instance.Contains(mobile) && !mobile.IsHuman) {
                    await MobileCache.Instance.CacheMobileFromWeb(mobile);
                }

                var mob = MobileCache.Instance.GetCachedMobile(mobile);
                if (mob == null || RowCountForMobile(mob.Name, mobile.X, mobile.Y) > 0) {
                    return false;
                }

                completeName = completeName.Trim();

                var regex = new Regex("\\(level\\s+(\\d+)\\)");
                var match = regex.Match(completeName);
                if (match.Success && match.Groups.Count > 1) {
                    var wholeWord = match.Groups[0].Value;
                    var levelOnly = match.Groups[1].Value;

                    if (int.TryParse(levelOnly, out var outLevel)) {
                        level = outLevel;
                    }

                    completeName = completeName.Replace(wholeWord, "").Trim();
                }

                mob.MapName = MobileCache.MapNameFromIndex(World.MapIndex);

                var command = _connection.CreateCommand();
                command.CommandText = BuildAddCommandFromCachedMobile(mob, mobile, completeName, level);
                command.ExecuteNonQuery();

                Console.WriteLine($"[Database] Added {completeName}  Level: {level}");

                return true;
            }

            return false;
        }

        private static string BuildAddCommandFromCachedMobile(MobileCache.SingleMobCache cachedMob, Mobile mobile, string name, int level) {
            var commandString = $"INSERT INTO Mobiles VALUES ('{name}', '{level}', '{(int)mobile.Race}', '{mobile.Graphic}', '{MobileCache.MapNameFromIndex(World.MapIndex)}', {mobile.Position.X}, {mobile.Position.Y}, {cachedMob.HitPointsMin}, {cachedMob.HitPointsMax}, '{cachedMob.Fame}', '{cachedMob.Karma}', '{cachedMob.Alignment}', {cachedMob.GoldMin}, {cachedMob.GoldMax}, {cachedMob.StrengthMin}, {cachedMob.StrengthMax}, {cachedMob.DexterityMin}, {cachedMob.DexterityMax}, {cachedMob.StaminaMin}, {cachedMob.StaminaMax}, {cachedMob.IntelligenceMin}, {cachedMob.IntelligenceMax}, {cachedMob.ManaMin}, {cachedMob.ManaMax}, {cachedMob.BardingDifficulty}, {cachedMob.TamingDifficulty}, {cachedMob.BaseDamageMin}, {cachedMob.BaseDamageMax}, '{cachedMob.PreferredFoods}', {cachedMob.WrestlingMin}, {cachedMob.WrestlingMax}, {cachedMob.PoisoningMin}, {cachedMob.PoisoningMax}, {cachedMob.TacticsMin}, {cachedMob.TacticsMax}, {cachedMob.MageryMin}, {cachedMob.MageryMax}, {cachedMob.ResistingSpellsMin}, {cachedMob.ResistingSpellsMax}, {cachedMob.EvaluatingIntelligenceMin}, {cachedMob.EvaluatingIntelligenceMax}, {cachedMob.AnatomyMin}, {cachedMob.AnatomyMax}, {cachedMob.MeditationMin}, {cachedMob.MeditationMax}, {cachedMob.DetectingHiddenMin}, {cachedMob.DetectingHiddenMax}, {cachedMob.HidingMin}, {cachedMob.HidingMax}, {cachedMob.ParryingMin}, {cachedMob.ParryingMax}, {cachedMob.HealingMin}, {cachedMob.HealingMax}, {cachedMob.NecromancyMin}, {cachedMob.NecromancyMax}, {cachedMob.SpiritSpeakMin}, {cachedMob.SpiritSpeakMax}, {cachedMob.MysticismMin}, {cachedMob.MysticismMax}, {cachedMob.FocusMin}, {cachedMob.FocusMax}, {cachedMob.SpellweavingMin}, {cachedMob.SpellweavingMax}, {cachedMob.DiscordanceMin}, {cachedMob.DiscordanceMax}, {cachedMob.BushidoMin}, {cachedMob.BushidoMax}, {cachedMob.NinjitsuMin}, {cachedMob.NinjitsuMax}, {cachedMob.ChivalryMin}, {cachedMob.ChivalryMax}, {cachedMob.PhysicalResistanceMin}, {cachedMob.PhysicalResistanceMax}, {cachedMob.FireResistanceMin}, {cachedMob.FireResistanceMax}, {cachedMob.ColdResistanceMin}, {cachedMob.ColdResistanceMax}, {cachedMob.PoisonResistanceMin}, {cachedMob.PoisonResistanceMax}, {cachedMob.EnergyResistanceMin}, {cachedMob.EnergyResistanceMax});";
            return commandString;
        }

        private static int RowCountForMobile(string name, int x, int y) {
            SQLiteCommand command;
            command = _connection.CreateCommand();
            command.CommandText = $"SELECT * FROM Mobiles WHERE Name='{name}' AND X={x} AND Y={y}";
            var reader = command.ExecuteReader();

            int count = 0;
            while (reader.Read()) {
                ++count;
            }

            return count;
        }

        internal static List<MobileCache.SingleMobCache> Search(string name, string map, bool exact)
        {
            List<MobileCache.SingleMobCache> list = new List<MobileCache.SingleMobCache>();
            SQLiteDataReader reader;

            SQLiteCommand command;
            command = _connection.CreateCommand();
            command.CommandText = $"SELECT * FROM Mobiles WHERE Name LIKE '%{name}%' AND Map='{map}'";
            if (exact) {
                command.CommandText = $"SELECT * FROM Mobiles WHERE Name='{name}' AND Map='{map}'";
            }
            if (name.Equals("*")) {
                command.CommandText = $"SELECT * FROM Mobiles WHERE Map='{map}'";
            }

            reader = command.ExecuteReader();
            while (reader.Read())
            {
                list.Add(MobileCache.MobCacheFromReader(reader));
            }

            return list;
        }
    }
}
