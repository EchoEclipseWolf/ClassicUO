using ClassicUO.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Game.AiEngine.Memory
{
    internal class BaseMemory {
        public uint PlayerSerial = 0;

        internal BaseMemory() {
        }

        internal virtual void LoadDefaults() {

        }

        internal virtual void SaveFile(string filename) {
            PlayerSerial = World.Player.Serial;
            var path = Path.Combine(ProfileManager.ProfilePath, filename);

            var serializedJson = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(path, serializedJson);
        }
    }
}
