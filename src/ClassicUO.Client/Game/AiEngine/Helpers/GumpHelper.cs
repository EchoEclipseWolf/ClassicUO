using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Button = ClassicUO.Game.UI.Controls.Button;

namespace ClassicUO.Game.AiEngine.Helpers
{
    internal static class GumpHelper
    {
        internal static Gump GetRunebookGump() {
            var gumps = UIManager.Gumps.ToList();

            foreach (var gump in gumps) {
                if (gump.Children.Count > 0) {
                    foreach (var gumpChild in gump.Children) {
                        if (gumpChild is Label label) {
                            if (label.Text != null && label.Text.ToLower().Contains("max charges")) {
                                return gump as Gump;
                            }
                        }

                        if (gumpChild is HtmlControl html) {
                            if (html.Text != null && html.Text.ToLower().Contains("max charges")) {
                                return gump as Gump;
                            }
                        }
                    }
                }
            }

            return null;
        }

        internal static string GetRunebookTextForIndex(Gump runebookGump, int index) {
            if (runebookGump == null || runebookGump.Children.Count < 200) {
                return "";
            }

            var startIndex = 31;

            if (runebookGump.Children[startIndex] is not Button) {
                return "";
            }

            var stringList = new List<string>();

            for (int i = 1; i < 32; i += 2) {
                var control = runebookGump.Children[startIndex + i];

                if (control is CroppedText croppedTextLabel) {
                    stringList.Add(croppedTextLabel.GameText.Text);
                }
            }

            return stringList.Count > index ? stringList[index] : "";
        }
    }
}
