using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassicUO.Game.Enums;
using System.Text.RegularExpressions;
using System.Globalization;
using AkatoshQuester.Helpers.LightGeometry;

namespace ClassicUO.AiEngine.AiEngineTasks
{
    public class MageryTrainingScript : BaseAITask
    {
        public MageryTrainingScript() : base("Magery Training")
        {
        }

        public override async Task<bool> Pulse() {
            var magerySkill = GetSkill(SkillConsts.Magery);

            if (magerySkill.Base >= 180) {
                return false;
            }

            var groundCastPoint = new Point3D(World.Player.Position.X + 2, World.Player.Position.Y + 2, World.Player.Position.Z);

            if (Player.Mana < 50) {
                return true;
            }

            if (magerySkill.Base < 30.0) {
                await CastSpell(SpellConsts.Createfood, 1000, true, 6000);
                return true;
            }

            if (magerySkill.Base < 45.0) {
                if (World.Player.HitsPercentage < 65) {
                    await CastSpellOnMobile(SpellConsts.Heal, World.Player, 1000, true, 6000);
                }
                else {
                    await CastSpellOnMobile(SpellConsts.Fireball, World.Player, 1000, true, 6000);
                }
                
                return true;
            }

            if (magerySkill.Base < 55.0)
            {
                await CastSpellOnMobile(SpellConsts.Manadrain, World.Player, 1000, true, 6000);
                return true;
            }

            if (World.Player.HitsPercentage < 65) {
                await CastSpellOnMobile(SpellConsts.Greaterheal, World.Player, 1000, true, 6000);
                return true;
            }

            if (magerySkill.Base < 65)
            {
                await CastSpellOnMobile(SpellConsts.Paralyze, World.Player, 1000, true, 6000);
                return true;
            }

            if (magerySkill.Base < 75)
            {
                await CastSpellOnMobile(SpellConsts.Reveal, World.Player, 1000, true, 6000);
                return true;
            }

            if (magerySkill.Base < 90)
            {
                await CastSpellOnMobile(SpellConsts.Manavampire, World.Player, 1000, true, 6000);
                return true;
            }

            if (magerySkill.Base < 180)
            {
                await CastSpell(SpellConsts.Earthquake, 1000, true, 6000);
                return true;
            }


            //await CastSpellOnPoint(SpellConsts.Bladespirits, groundCastPoint, 1000, true, 12000);
            await Task.Delay(1000);

            /*var fileLoc = "C:\\Users\\natfo\\Desktop\\spells.txt";

            var spellLines = File.ReadAllLines(fileLoc);

            var regex = new Regex("Cast(\\w+),\\s*.*CastSpell\\((\\d+)\\)", RegexOptions.IgnoreCase);
            var builtString = "";
            var textinfo = new CultureInfo("en-US", false).TextInfo;

            foreach (var line in spellLines) {
                var match = regex.Match(line);

                if (match.Success && match.Groups.Count > 2) {
                    var first = match.Groups[1].Value;
                    var second = match.Groups[2].Value;


                    var constName = textinfo.ToTitleCase(first);
                    constName = constName.Replace(" ", "");
                    builtString += $"public const int {constName} = {second};" + Environment.NewLine;
                }
            }*/


            //
            return true;
        }
    }
}
