using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game.AiEngine.Helpers
{
    internal static class JournalHelper
    {
        internal static List<JournalEntry> GetJournalEntriesContaining(string containsString, DateTime afterTime) {
            var list = new List<JournalEntry>();
            var entriesAfterTime = JournalManager.Entries.Where(j => j.Time > afterTime).ToList();

            foreach (var entry in entriesAfterTime.Where(e => e != null)) {
                if (entry.Text.ToLower().Contains(containsString.ToLower())) {
                    list.Add(entry);
                }
            }

            return list;
        }
    }
}
