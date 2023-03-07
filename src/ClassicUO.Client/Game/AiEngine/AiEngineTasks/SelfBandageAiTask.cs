using System.Diagnostics;
using System.Threading.Tasks;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;

namespace ClassicUO.AiEngine.AiEngineTasks {
    public class SelfBandageAiTask : BaseAITask {
        public override int Priority() {
            if (World.Player == null) {
                return 0;
            }
            return 100-World.Player.HitsPercentage;
        }
        
        public override async Task<bool> Pulse() {
            if (World.Player.HitsPercentage > 95 || !ClassicUO.AiEngine.AiEngine.Instance.SelfBandageHealing) {
                return false;
            }

            var buffIcons = World.Player.BuffIcons;

            foreach (var buff in buffIcons) {
                if (buff.Key == BuffIconType.Healing && buff.Value != null && buff.Value.Graphic == 30102) {
                    return false;
                } 
            }

            if (buffIcons.ContainsKey(BuffIconType.Healing)) {
                /*foreach (var VARIABLE in buffIcons.) {
                    
                }*/
                return false;
            }
            
            Item bandage = World.Player.FindBandage();

            if (bandage != null) {
                GameActions.DoubleClick(bandage);
                var timer = Stopwatch.StartNew();

                while (timer.ElapsedMilliseconds < 1000) {
                    await Task.Delay(10);

                    if (TargetManager.IsTargeting) {
                        break;
                    }
                }
                TargetManager.Target(World.Player);
            }

            await Task.Delay(400);

            return true;
        }
    }
}