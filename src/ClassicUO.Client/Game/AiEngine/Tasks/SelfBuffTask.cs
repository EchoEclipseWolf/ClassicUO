using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using ClassicUO.AiEngine.AiEngineTasks;
using ClassicUO.Game.AiEngine.Helpers;
using ClassicUO.Game.AiEngine.Memory;
using ClassicUO.Game.Data;
using ClassicUO.Game.Enums;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.Game.AiEngine.Tasks
{
    public class SelfBuffTask : BaseAITask
    {
        public SelfBuffTask() : base("Self buff Task")
        {
        }

        public override int Priority()
        {
            return 1;
        }

        private bool HasBuff(BuffIconType type)
        {
            var buffIcons = World.Player.BuffIcons.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            foreach (var buff in buffIcons)
            {
                if (buff.Key == type)
                {
                    return true;
                }
            }

            if (buffIcons.ContainsKey(type))
            {
                return true;
            }

            return false;
        }
        
        public override async Task<bool> Pulse()
        {
            if (!AiSettings.Instance.SelfBuff || Player.Mana < 40 || !Player.InWarMode)
            {
                return false;
            }

            if (GetSkill("Magery")?.Base > 60) {
                if (!HasBuff(BuffIconType.Protection))
                {
                    await CastSpell(SpellConsts.Protection);
                    await WaitForHelper.WaitFor(() => HasBuff(BuffIconType.Protection), 3000);
                    return true;
                }

                if (!HasBuff(BuffIconType.Bless)) {
                    await CastSpellOnMobile(SpellConsts.Bless, Player);
                    return true;
                }
            }

            if (GetSkill("Necromancy")?.Base > 100) {
                if (!HasBuff(BuffIconType.VampiricEmbrace)) {
                    await CastSpell(SpellConsts.Vampiricembrace);
                    await WaitForHelper.WaitFor(() => HasBuff(BuffIconType.VampiricEmbrace), 3000);
                    return true;
                }
            }

            return true;
        }
    }
}