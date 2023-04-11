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

        internal static List<Gump> GetHealthbarGrumps()
        {
            var list = new List<Gump>();
            var gumps = UIManager.Gumps.ToList();

            foreach (var gump in gumps)
            {
                if (gump is BaseHealthBarGump healthbar)
                {
                    list.Add(healthbar);
                }

                if (gump is HealthBarGumpCustom customHealthbar)
                {
                    list.Add(customHealthbar);
                }
            }

            return list;
        }

        internal static List<ContainerGump> GetContainerGrumps() {
            var list = new List<ContainerGump>();
            var gumps = UIManager.Gumps.ToList();

            foreach (var gump in gumps) {
                if (gump is ContainerGump containerGump) {
                    list.Add(containerGump);
                }
            }

            return list;
        }

        internal static ContainerGump GetContainerGrumpByItemSerial(uint serial) {
            try {
                var gumps = UIManager.Gumps.ToList();

                foreach (var gump in gumps) {
                    if (gump is ContainerGump containerGump) {
                        if (containerGump.LocalSerial == serial) {
                            return containerGump;
                        }
                    }
                }
            }
            catch (Exception) {
                return null;
            }

            return null;
        }

        internal static List<Gump> GetBODBooks() {
            var list = new List<Gump>();

            try {
                var gumps = UIManager.Gumps.ToList();

                foreach (var gump in gumps.Where(g => g != null && g.Children.Count > 0)) {
                    foreach (var child in gump.Children) {
                        if (child is HtmlControl htmlControl) {
                            if (htmlControl.Text.ToLower().Contains("bulk order")) {
                                list.Add(gump);
                            }
                        }
                    }
                }
            }
            catch (Exception) {
                return list;
            }

            return list;
        }

        internal static Gump GetAnimalBODBook() {
            try {
                var gumps = UIManager.Gumps.ToList();

                bool foundBOD = false;
                bool foundAnimalLabel = false;
                Gump bodGump = null;
                foreach (var gump in gumps.Where(g => g != null && g.Children.Count > 0)) {
                    foreach (var child in gump.Children) {
                        if (foundBOD && foundAnimalLabel) {
                            break;
                        }
                        if (child is HtmlControl htmlControl) {
                            if (htmlControl.Text.ToLower().Contains("bulk order")) {
                                foundBOD = true;
                            }
                        }
                        else if (child is Label label) {
                            if (label.Text.ToLower().Contains("animal")) {
                                foundAnimalLabel = true;
                            }
                        }
                    }
                    if (foundBOD && foundAnimalLabel) {
                        return gump;
                    }
                }
            }
            catch (Exception) {
                return null;
            }

            return null;
        }

        internal static PopupMenuGump GetPopupMenu()
        {
            var gumps = UIManager.Gumps.ToList();

            foreach (var gump in gumps)
            {
                if (gump is PopupMenuGump popup) {
                    return popup;
                }
            }

            return null;
        }

        internal static Gump GetStaffToolbarGump() {
            try {
                var gumps = UIManager.Gumps.ToList();

                bool foundAdmin = false;
                bool foundStaffRunebook = false;
                foreach (var gump in gumps.Where(g => g != null && g.Children.Count > 0)) {
                    foreach (var child in gump.Children) {
                        if (foundAdmin && foundStaffRunebook) {
                            break;
                        }
                        if (child is HtmlControl htmlControl) {
                            if (htmlControl.Text.ToLower().Contains("[admin".ToLower())) {
                                foundAdmin = true;
                            }

                            if (htmlControl.Text.ToLower().Contains("[StaffRunebook".ToLower())) {
                                foundStaffRunebook = true;
                            }
                        }
                    }
                    if (foundAdmin && foundStaffRunebook) {
                        return gump;
                    }
                }
            }
            catch (Exception) {
                return null;
            }

            return null;
        }

        internal static Gump GetChatHistoryGump() {
            try {
                var gumps = UIManager.Gumps.ToList();

                foreach (var gump in gumps.Where(g => g != null && g.Children.Count > 0)) {
                    foreach (var child in gump.Children) {
                        if (child is HtmlControl htmlControl) {
                            if (htmlControl.Text.ToLower().Contains("History - Auto Refresh: ON".ToLower())) {
                                return gump;
                            }
                        }
                    }
                }
            }
            catch (Exception) {
                return null;
            }

            return null;
        }
        internal static Gump GetMessageOfTheDayGump() {
            try {
                var gumps = UIManager.Gumps.ToList();

                foreach (var gump in gumps.Where(g => g != null && g.Children.Count > 0)) {
                    foreach (var child in gump.Children) {
                        if (child is Label label) {
                            if (label.Text.ToLower().Contains("Message of the Day - Main".ToLower())) {
                                return gump;
                            }
                        }
                    }
                }
            }
            catch (Exception) {
                return null;
            }

            return null;
        }

        internal static Gump GetVetRewardGump() {
            try {
                var gumps = UIManager.Gumps.ToList();

                foreach (var gump in gumps.Where(g => g != null && g.Children.Count > 0)) {
                    foreach (var child in gump.Children) {
                        if (child is HtmlControl label && !string.IsNullOrEmpty(label.Text)) {
                            if (label.Text.ToLower().Contains("You have reward items available.".ToLower())) {
                                return gump;
                            }
                        }
                    }
                }
            }
            catch (Exception) {
                return null;
            }

            return null;
        }
    }

    
}
