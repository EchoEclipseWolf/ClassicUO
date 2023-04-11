#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Utility;
// ReSharper disable InconsistentNaming

namespace ClassicUO.Game.Managers
{
    //enum MessageFont : byte
    //{
    //    INVALID = 0xFF,
    //    Bold = 0,
    //    Shadow = 1,
    //    BoldShadow = 2,
    //    Normal = 3,
    //    Gothic = 4,
    //    Italic = 5,
    //    SmallDark = 6,
    //    Colorful = 7,
    //    Rune = 8,
    //    SmallLight = 9
    //}

    internal enum AffixType : byte
    {
        Append = 0x00,
        Prepend = 0x01,
        System = 0x02,
        None = 0xFF
    }


    internal class MessageCombiner {
        internal int IdentifierIndex;
        internal int CombineIndex;
        internal string CurrentString;
        internal MessageCombinerType Type;
        internal ushort Hue;
        internal byte Font;
        internal bool IsUnicode;
        internal MessageType MessageType;
        internal TextType TextType;
        internal Entity Parent;
        internal int CombineTime;


        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

        public MessageCombiner(int identifierIndex, int combineIndex, string initString, MessageCombinerType type, ushort hue, byte font, bool isUnicode, MessageType messageType, TextType textType, Entity parent, int combineTime)
        {
            IdentifierIndex = identifierIndex;
            CombineIndex = combineIndex;
            CurrentString = initString;
            Type = type;
            Hue = hue;
            Font = font;
            IsUnicode = isUnicode;
            MessageType = messageType;
            TextType = textType;
            Parent = parent;
            CombineTime = combineTime;
        }

        private string[] SplitString() {
            return CurrentString.Split(' ');
        }

        internal void AddString(string toAdd) {
            var split = SplitString();
            if (int.TryParse(split[CombineIndex], out var oldNumber) && int.TryParse(toAdd.Split(' ')[CombineIndex], out var newNumber)) {
                var combinedNumber = newNumber + oldNumber;

                split[CombineIndex] = combinedNumber.ToString();

                var rebuiltString = split.Aggregate("", (current, item) => current + (item + " "));
                CurrentString = rebuiltString;
            }
        }

        internal string GetIdentifier() {
            return SplitString()[0];
        }

        internal bool MatchesIdentifier(string identifier) {
            return identifier.Equals(GetIdentifier(), StringComparison.InvariantCultureIgnoreCase);
        }

        internal bool IsReady() {
            return _stopwatch.ElapsedMilliseconds > CombineTime;
        }

        internal string GetCombinedString() {
            return CurrentString;
        }
    }

    internal enum MessageCombinerType {
        YouHaveGained,
        ForTheKill,
        DatTokens,
        CleaningTokens,
    }

    internal static class MessageManager
    {
        public static PromptData PromptData { get; set; }

        public static event EventHandler<MessageEventArgs> MessageReceived;

        public static event EventHandler<MessageEventArgs> LocalizedMessageReceived;
        private static List<MessageCombiner> _messageCombiners = new();


        public static void HandleMessage
            (Entity parent, string text, string name, ushort hue, MessageType type, byte font, TextType textType, bool unicode = false, string lang = null) {
            if (string.IsNullOrEmpty(text)) {
                return;
            }

            Profile currentProfile = ProfileManager.CurrentProfile;

            if (currentProfile != null && currentProfile.OverrideAllFonts) {
                font = currentProfile.ChatFont;
                unicode = currentProfile.OverrideAllFontsIsUnicode;
            }

            switch (type) {
                case MessageType.Command:
                case MessageType.Encoded:
                case MessageType.System:
                case MessageType.Party: break;

                case MessageType.Guild:
                    if (currentProfile.IgnoreGuildMessages)
                        return;

                    break;

                case MessageType.Alliance:
                    if (currentProfile.IgnoreAllianceMessages)
                        return;

                    break;

                case MessageType.Spell: {
                    //server hue color per default
                    if (!string.IsNullOrEmpty(text) && SpellDefinition.WordToTargettype.TryGetValue(text, out SpellDefinition spell)) {
                        if (currentProfile != null && currentProfile.EnabledSpellFormat && !string.IsNullOrWhiteSpace(currentProfile.SpellDisplayFormat)) {
                            ValueStringBuilder sb = new ValueStringBuilder(currentProfile.SpellDisplayFormat.AsSpan());

                            {
                                sb.Replace("{power}".AsSpan(), spell.PowerWords.AsSpan());
                                sb.Replace("{spell}".AsSpan(), spell.Name.AsSpan());

                                text = sb.ToString().Trim();
                            }

                            sb.Dispose();
                        }

                        //server hue color per default if not enabled
                        if (currentProfile != null && currentProfile.EnabledSpellHue) {
                            if (spell.TargetType == TargetType.Beneficial) {
                                hue = currentProfile.BeneficHue;
                            }
                            else if (spell.TargetType == TargetType.Harmful) {
                                hue = currentProfile.HarmfulHue;
                            }
                            else {
                                hue = currentProfile.NeutralHue;
                            }
                        }
                    }

                    goto case MessageType.Label;
                }

                default:
                case MessageType.Focus:
                case MessageType.Whisper:
                case MessageType.Yell:
                case MessageType.Regular:
                case MessageType.Label:
                case MessageType.Limit3Spell:

                    if (parent == null) {
                        break;
                    }

                    // If person who send that message is in ignores list - but filter out Spell Text
                    if (IgnoreManager.IgnoredCharsList.Contains(parent.Name) && type != MessageType.Spell)
                        break;

                    TextObject msg = CreateMessage(text, hue, font, unicode, type, textType);

                    msg.Owner = parent;

                    if (parent is Item it && !it.OnGround) {
                        msg.X = DelayedObjectClickManager.X;
                        msg.Y = DelayedObjectClickManager.Y;
                        msg.IsTextGump = true;
                        bool found = false;

                        for (LinkedListNode<Gump> gump = UIManager.Gumps.Last; gump != null; gump = gump.Previous) {
                            Control g = gump.Value;

                            if (!g.IsDisposed) {
                                switch (g) {
                                    case PaperDollGump paperDoll when g.LocalSerial == it.Container:
                                        paperDoll.AddText(msg);
                                        found = true;

                                        break;

                                    case ContainerGump container when g.LocalSerial == it.Container:
                                        container.AddText(msg);
                                        found = true;

                                        break;

                                    case TradingGump trade when trade.ID1 == it.Container || trade.ID2 == it.Container:
                                        trade.AddText(msg);
                                        found = true;

                                        break;
                                }
                            }

                            if (found) {
                                break;
                            }
                        }
                    }

                    parent.AddMessage(msg);

                    break;
            }

            MessageCombiner foundCombiner = null;

            /*if (text.ToLower().Contains("receiving a bonus loot item from".ToLower()) ||
                text.ToLower().Contains("You have gained in Valor".ToLower())) {
                return;
            }

            if (text.ToLower().Contains("have gained".ToLower()) && text.ToLower().Contains("from using".ToLower())) {
                var splitText = text.Split(' ');

                foundCombiner = _messageCombiners.FirstOrDefault(m => m.Type == MessageCombinerType.YouHaveGained && m.MatchesIdentifier(splitText[0]));

                if (foundCombiner == null) {
                    foundCombiner = new MessageCombiner(0, 3, text, MessageCombinerType.YouHaveGained, hue, font, unicode, type, textType, parent, 10000);
                    _messageCombiners.Add(foundCombiner);

                    return;
                }
            }

            if (text.ToLower().Contains("gained".ToLower()) && text.ToLower().Contains("exp for the".ToLower()))
            {
                var splitText = text.Split(' ');

                foundCombiner = _messageCombiners.FirstOrDefault(m => m.Type == MessageCombinerType.ForTheKill && m.MatchesIdentifier(splitText[0]));

                if (foundCombiner == null)
                {
                    foundCombiner = new MessageCombiner(0, 2, text, MessageCombinerType.ForTheKill, hue, font, unicode, type, textType, parent, 10000);
                    _messageCombiners.Add(foundCombiner);

                    return;
                }
            }

            if (text.ToLower().Contains("You added".ToLower()) && text.ToLower().Contains("Daat99Tokens to your Master Storage".ToLower()))
            {
                var splitText = text.Split(' ');

                foundCombiner = _messageCombiners.FirstOrDefault(m => m.Type == MessageCombinerType.DatTokens && m.MatchesIdentifier(splitText[0]));

                if (foundCombiner == null)
                {
                    foundCombiner = new MessageCombiner(0, 2, text, MessageCombinerType.DatTokens, hue, font, unicode, type, textType, parent, 10000);
                    _messageCombiners.Add(foundCombiner);

                    return;
                }
            }

            if (text.ToLower().Contains("You gained".ToLower()) && text.ToLower().Contains("tokens for cleaning".ToLower()))
            {
                var splitText = text.Split(' ');

                foundCombiner = _messageCombiners.FirstOrDefault(m => m.Type == MessageCombinerType.CleaningTokens && m.MatchesIdentifier(splitText[0]));

                if (foundCombiner == null)
                {
                    foundCombiner = new MessageCombiner(0, 2, text, MessageCombinerType.CleaningTokens, hue, font, unicode, type, textType, parent, 10000);
                    _messageCombiners.Add(foundCombiner);

                    return;
                }
            }

            if (foundCombiner != null) {
                foundCombiner.AddString(text);

                if (!foundCombiner.IsReady()) {
                    return;
                }
                return;
            }

            var readyMessages = _messageCombiners.Where(f => f.IsReady()).ToList();
            _messageCombiners = _messageCombiners.Where(f => !f.IsReady()).ToList();

            foreach (var combiner in readyMessages)
            {
                MessageReceived.Raise(new MessageEventArgs(combiner.Parent, combiner.GetCombinedString(), name, combiner.Hue, combiner.MessageType, combiner.Font, combiner.TextType, combiner.IsUnicode, lang), combiner.Parent);
            }

            if (foundCombiner == null) {*/
                MessageReceived.Raise(new MessageEventArgs(parent, text, name, hue, type, font, textType, unicode, lang), parent);
            //}
        }

        public static void OnLocalizedMessage(Entity entity, MessageEventArgs args)
        {
            LocalizedMessageReceived.Raise(args, entity);
        }

        public static TextObject CreateMessage
        (
            string msg,
            ushort hue,
            byte font,
            bool isunicode,
            MessageType type,
            TextType textType
        )
        {
            if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.OverrideAllFonts)
            {
                font = ProfileManager.CurrentProfile.ChatFont;
                isunicode = ProfileManager.CurrentProfile.OverrideAllFontsIsUnicode;
            }

            int width = isunicode ? FontsLoader.Instance.GetWidthUnicode(font, msg) : FontsLoader.Instance.GetWidthASCII(font, msg);

            if (width > 200)
            {
                width = isunicode ?
                    FontsLoader.Instance.GetWidthExUnicode
                    (
                        font,
                        msg,
                        200,
                        TEXT_ALIGN_TYPE.TS_LEFT,
                        (ushort) FontStyle.BlackBorder
                    ) :
                    FontsLoader.Instance.GetWidthExASCII
                    (
                        font,
                        msg,
                        200,
                        TEXT_ALIGN_TYPE.TS_LEFT,
                        (ushort) FontStyle.BlackBorder
                    );
            }
            else
            {
                width = 0;
            }


            ushort fixedColor = (ushort)(hue & 0x3FFF);

            if (fixedColor != 0)
            {
                if (fixedColor >= 0x0BB8)
                {
                    fixedColor = 1;
                }

                fixedColor |= (ushort)(hue & 0xC000);
            }
            else
            {
                fixedColor = (ushort)(hue & 0x8000);
            }


            TextObject textObject = TextObject.Create();
            textObject.Alpha = 0xFF;
            textObject.Type = type;
            textObject.Hue = fixedColor;

            if (!isunicode && textType == TextType.OBJECT)
            {
                fixedColor = 0x7FFF;
            }
            
            textObject.RenderedText = RenderedText.Create
            (
                msg,
                fixedColor,
                font,
                isunicode,
                FontStyle.BlackBorder,
                TEXT_ALIGN_TYPE.TS_LEFT,
                width,
                30,
                false,
                false,
                textType == TextType.OBJECT
            );

            textObject.Time = CalculateTimeToLive(textObject.RenderedText);
            textObject.RenderedText.Hue = textObject.Hue;

            return textObject;
        }

        private static long CalculateTimeToLive(RenderedText rtext)
        {
            Profile currentProfile = ProfileManager.CurrentProfile;

            if (currentProfile == null)
            {
                return 0;
            }

            long timeToLive;

            if (currentProfile.ScaleSpeechDelay)
            {
                int delay = currentProfile.SpeechDelay;

                if (delay < 10)
                {
                    delay = 10;
                }

                timeToLive = (long) (4000 * rtext.LinesCount * delay / 100.0f);
            }
            else
            {
                long delay = (5497558140000 * currentProfile.SpeechDelay) >> 32 >> 5;

                timeToLive = (delay >> 31) + delay;
            }

            timeToLive += Time.Ticks;

            return timeToLive;
        }
    }
}