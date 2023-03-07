using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using Microsoft.Data.Sqlite;

namespace ClassicUO.DatabaseUtility
{
    public static class Database {
        private static SQLiteConnection _connection;

        public static void CreateConnection() {
            if (_connection == null) {
                _connection = new SQLiteConnection("Data Source=database.db; Version = 3; New = True; Compress = True; ");
                try {
                    _connection.Open();
                } catch (Exception ex) {
                    int bob = 1;
                }
            }
        }

        internal static async Task<bool> AddMobile(Mobile mobile) {
            if (!string.IsNullOrEmpty(mobile.Name)) {

                if (!MobileCache.Instance.Contains(mobile)) {
                    await MobileCache.Instance.CacheMobileFromWeb(mobile);

                    if (!MobileCache.Instance.Contains(mobile)) {
                        return false;
                    }
                }

                var mob = MobileCache.Instance.GetCachedMobile(mobile);
                if (mob == null || RowCountForMobile(mob.Name, mobile.X, mobile.Y) > 0) {
                    return false;
                }

                if (mob.HitPointsMin == 0) {
                    int bob1 = 1;
                }

                mob.MapName = MobileCache.MapNameFromIndex(World.MapIndex);

                SQLiteCommand command;
                command = _connection.CreateCommand();
                command.CommandText = BuildAddCommandFromCachedMobile(mob, mobile);
                command.ExecuteNonQuery();

                return true;
            }

            return false;
        }

        private static string BuildAddCommandFromCachedMobile(MobileCache.SingleMobCache cachedMob, Mobile mobile) {
            var commandString = $"INSERT INTO Mobiles VALUES ('{cachedMob.Name}', '{MobileCache.MapNameFromIndex(World.MapIndex)}', {mobile.Position.X}, {mobile.Position.Y}, {cachedMob.HitPointsMin}, {cachedMob.HitPointsMax}, '{cachedMob.Fame}', '{cachedMob.Karma}', '{cachedMob.Alignment}', {cachedMob.GoldMin}, {cachedMob.GoldMax}, {cachedMob.StrengthMin}, {cachedMob.StrengthMax}, {cachedMob.DexterityMin}, {cachedMob.DexterityMax}, {cachedMob.StaminaMin}, {cachedMob.StaminaMax}, {cachedMob.IntelligenceMin}, {cachedMob.IntelligenceMax}, {cachedMob.ManaMin}, {cachedMob.ManaMax}, {cachedMob.BardingDifficulty}, {cachedMob.TamingDifficulty}, {cachedMob.BaseDamageMin}, {cachedMob.BaseDamageMax}, '{cachedMob.PreferredFoods}', {cachedMob.WrestlingMin}, {cachedMob.WrestlingMax}, {cachedMob.PoisoningMin}, {cachedMob.PoisoningMax}, {cachedMob.TacticsMin}, {cachedMob.TacticsMax}, {cachedMob.MageryMin}, {cachedMob.MageryMax}, {cachedMob.ResistingSpellsMin}, {cachedMob.ResistingSpellsMax}, {cachedMob.EvaluatingIntelligenceMin}, {cachedMob.EvaluatingIntelligenceMax}, {cachedMob.AnatomyMin}, {cachedMob.AnatomyMax}, {cachedMob.MeditationMin}, {cachedMob.MeditationMax}, {cachedMob.DetectingHiddenMin}, {cachedMob.DetectingHiddenMax}, {cachedMob.HidingMin}, {cachedMob.HidingMax}, {cachedMob.ParryingMin}, {cachedMob.ParryingMax}, {cachedMob.HealingMin}, {cachedMob.HealingMax}, {cachedMob.NecromancyMin}, {cachedMob.NecromancyMax}, {cachedMob.SpiritSpeakMin}, {cachedMob.SpiritSpeakMax}, {cachedMob.MysticismMin}, {cachedMob.MysticismMax}, {cachedMob.FocusMin}, {cachedMob.FocusMax}, {cachedMob.SpellweavingMin}, {cachedMob.SpellweavingMax}, {cachedMob.DiscordanceMin}, {cachedMob.DiscordanceMax}, {cachedMob.BushidoMin}, {cachedMob.BushidoMax}, {cachedMob.NinjitsuMin}, {cachedMob.NinjitsuMax}, {cachedMob.ChivalryMin}, {cachedMob.ChivalryMax}, {cachedMob.PhysicalResistanceMin}, {cachedMob.PhysicalResistanceMax}, {cachedMob.FireResistanceMin}, {cachedMob.FireResistanceMax}, {cachedMob.ColdResistanceMin}, {cachedMob.ColdResistanceMax}, {cachedMob.PoisonResistanceMin}, {cachedMob.PoisonResistanceMax}, {cachedMob.EnergyResistanceMin}, {cachedMob.EnergyResistanceMax});";
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
