using System.Linq;
using ClassicUO.Game.Data;
using ClassicUO.Game;
using System.Threading.Tasks;
using System;
using System.Diagnostics;
using System.Globalization;
using AkatoshQuester.Helpers.LightGeometry;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;

namespace ClassicUO.AiEngine.AiEngineTasks {
    public class BaseAITask {
        public string Name;

        internal PlayerMobile Player => World.Player;

        public BaseAITask(string name) {
            Name = name;
        }

        public virtual int Priority() {
            return 1;
        }
        
        public virtual async Task<bool> Start() {
            
            return true;
        }

        public virtual async Task<bool> Pulse() {
            
            return true;
        }

        public virtual bool HasBuff(BuffIconType icon, ushort graphic) {
            return World.Player.BuffIcons.Any(buff => buff.Key == icon && buff.Value != null && buff.Value.Graphic == graphic);
        }

        public virtual Skill GetSkill(string skillName) {
            return World.Player.Skills.FirstOrDefault(s => s.Name.Equals(skillName, StringComparison.InvariantCultureIgnoreCase)); ;
        }

        public async Task<bool> WaitForTargetting(int maxWaitTimeInMs = 5000) {
            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < maxWaitTimeInMs) {
                if (TargetManager.IsTargeting) {
                    return true;
                }
            }

            return false;
        }

        internal async Task<bool> CastSpell
            (int spellIndex, int waitTimeAfter = 1000, bool waitForJournal = true, int maxJournalWaitTime = 3000, Func<bool> waitForCondition = null) {
            return await CastSpell(spellIndex, new Point3D(0, 0, 0), null, waitTimeAfter, waitForJournal, maxJournalWaitTime, waitForCondition);
        }

        internal async Task<bool> CastSpellOnMobile
            (int spellIndex, Mobile mobileTarget, int waitTimeAfter = 1000, bool waitForJournal = true, int castTime = 3000, Func<bool> waitForCondition = null)
        {
            return await CastSpell(spellIndex, new Point3D(0, 0, 0), mobileTarget, waitTimeAfter, waitForJournal, castTime, waitForCondition);
        }

        internal async Task<bool> CastSpellOnPoint
            (int spellIndex, Point3D point, int waitTimeAfter = 1000, bool waitForJournal = true, int castTime = 3000, Func<bool> waitForCondition = null)
        {
            return await CastSpell(spellIndex, point, null, waitTimeAfter, waitForJournal, castTime, waitForCondition);
        }

        internal async Task<bool> CastSpell(int spellIndex, Point3D targetPoint, Mobile mobileTarget = null, int waitTimeAfter = 1000, bool waitForJournal = true, int castTime = 3000, Func<bool> waitForCondition = null) {
            var preCastTime = DateTime.Now;
            var stopwatch = Stopwatch.StartNew();

            GameActions.CastSpell(spellIndex);

            //The before wait for journal entries
            if (waitForJournal) {
                while (stopwatch.ElapsedMilliseconds < 1000) {
                    var entriesAfterTime = JournalManager.Entries.Where(j => j.Time > preCastTime).ToList();

                    foreach (var entry in entriesAfterTime) {
                        if (entry.Text.ToLower().Contains("not yet recovered") ||
                            entry.Text.ToLower().Contains("too many follower")) {

                            await Task.Delay(waitTimeAfter);
                            return true;
                        }
                    }
                }
            }

            var waitForTargat = mobileTarget != null || !Equals(targetPoint, Point3D.Empty);

            if (waitForTargat) {
                await WaitForTargetting(castTime);
                await Task.Delay(150);
                stopwatch.Restart();

                if (TargetManager.IsTargeting) {
                    if (mobileTarget != null) {
                        TargetManager.Target(mobileTarget.Serial);
                    } else if (!Equals(targetPoint, Point3D.Empty)) {
                        TargetManager.Target(0, (ushort) targetPoint.X, (ushort)targetPoint.Y, (short)targetPoint.Z);
                    }
                }
            }


            if (waitForJournal) {
                var found = false;
                while (stopwatch.ElapsedMilliseconds < castTime && !found) {
                    var entriesAfterTime = JournalManager.Entries.Where(j => j.Time > preCastTime).ToList();

                    foreach (var entry in entriesAfterTime) {
                        if (entry.Text.ToLower().Contains("spell fizzles") ||
                            entry.Text.ToLower().Contains("not yet recovered") ||
                            entry.Text.ToLower().Contains("too many follower") ||
                            entry.Text.ToLower().Contains("is blocked")) {
                            found = true;
                            break;
                        }

                        if (waitForCondition != null && waitForCondition()) {
                            found = true;
                            await Task.Delay(waitTimeAfter);
                            break;
                        }
                    }
                }

                if (!found) {
                    return true;
                }
            }

            await Task.Delay(waitTimeAfter);
            return true;
        }
    }
}