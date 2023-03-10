using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassicUO.Game;
using ClassicUO.Game.AiEngine;
using ClassicUO.Game.AiEngine.Memory;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;

namespace ClassicUO.AiEngine.AiEngineTasks
{
    public class TamingTrainingAiTask : BaseAITask
    {
        public TamingTrainingAiTask() : base("Animal Training")
        {
        }

        public override int Priority()
        {
            return 100 - World.Player.HitsPercentage;
        }

        public override async Task<bool> Pulse() {
            return false;

            var nameToTame = GetTamingNameForSkill();
            var mobiles = MobilesThatMatchTameName(nameToTame);

            if (mobiles.Count == 0) {
                return true;
            }
            

            await Task.Delay(400);

            return true;
        }

        private string GetTamingNameForSkill() {
            var animalTamingSkill = World.Player.Skills.FirstOrDefault(s => s.Name.Equals("Animal Taming", StringComparison.InvariantCultureIgnoreCase));
            return "a Taming Hiryu";
        }

        private List<Mobile> MobilesThatMatchTameName(string name) {
            var list = new List<Mobile>();

            foreach (var mobile in World.Mobiles.Values.Where(m => m.Name.ToLower().Contains(name.ToLower()))) {
                if (mobile.NotorietyFlag == NotorietyFlag.Innocent || mobile.NotorietyFlag == NotorietyFlag.Unknown || mobile.NotorietyFlag == NotorietyFlag.Gray) {
                    list.Add(mobile);
                }
            }

            return list;
        }
    }
}
