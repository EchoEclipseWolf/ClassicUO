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
using System.Text;
using System.Xml;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class AIEngineOverlayGump : Gump
    {
        private const string DEBUG_STRING_SMALL_NO_ZOOM = "AI Engine";
        
        private static Point _last_position = new Point(-1, -1);

        private uint _timeToUpdate;
        private readonly AlphaBlendControl _alphaBlendControl;
        private Checkbox _enableSelfBandageHealingCheckbox;
        private Checkbox _enableTamingTrainerCheckbox;
        private Checkbox _enableNavigationCheckbox;
        private string _cacheText = string.Empty;

        public AIEngineOverlayGump(int x, int y) : base(0, 0)
        {
            CanMove = true;
            CanCloseWithEsc = false;
            CanCloseWithRightClick = true;
            AcceptMouseInput = true;
            AcceptKeyboardInput = false;
            IsMinimized = true;

            Width = 100;
            Height = 50;
            X = _last_position.X <= 0 ? x : _last_position.X;
            Y = _last_position.Y <= 0 ? y : _last_position.Y;

            Add
            (
                _alphaBlendControl = new AlphaBlendControl(.7f)
                {
                    Width = Width, Height = Height
                }
            );

            Add
            (
                _enableNavigationCheckbox = new Checkbox
                (
                    0x00D2,
                    0x00D3,
                    "Navigation Test",
                    255,
                    1153
                )
                {
                    IsChecked = AiEngine.AiEngine.Instance.Navigation,
                    X = 2,
                    Y = 40
                }
            );

            Add
            (
                _enableSelfBandageHealingCheckbox = new Checkbox
                (
                    0x00D2,
                    0x00D3,
                    "Self Healing Bandage",
                    255,
                    1153
                )
                {
                    IsChecked = AiEngine.AiEngine.Instance.SelfBandageHealing,
                    X = 2,
                    Y = 70
                }
            );

            Add
            (
                _enableTamingTrainerCheckbox = new Checkbox
                (
                    0x00D2,
                    0x00D3,
                    "Taming Training",
                    255,
                    1153
                )
                {
                    IsChecked = AiEngine.AiEngine.Instance.TamingTraining,
                    X = 2,
                    Y = 70
                }
            );

            LayerOrder = UILayer.Over;

            WantUpdateSize = true;
        }

        public bool IsMinimized { get; set; }

        public override GumpType GumpType => GumpType.Debug;

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                IsMinimized = !IsMinimized;

                return true;
            }

            return false;
        }

        public override void Update()
        {
            base.Update();

            if (Time.Ticks > _timeToUpdate)
            {
                _timeToUpdate = Time.Ticks + 100;

                GameScene scene = Client.Game.GetScene<GameScene>();
                Span<char> span = stackalloc char[256];
                ValueStringBuilder sb = new ValueStringBuilder(span);

                sb.Append(string.Format(DEBUG_STRING_SMALL_NO_ZOOM, CUOEnviroment.CurrentRefreshRate));
                

                _cacheText = sb.ToString();

                sb.Dispose();

                _alphaBlendControl.Width = IsMinimized ? 85 : 150;
                _alphaBlendControl.Height = IsMinimized ? 30 : 500;
                AiEngine.AiEngine.Instance.SelfBandageHealing = _enableSelfBandageHealingCheckbox.IsChecked;
                AiEngine.AiEngine.Instance.Navigation = _enableNavigationCheckbox.IsChecked;
                AiEngine.AiEngine.Instance.TamingTraining = _enableTamingTrainerCheckbox.IsChecked;

                _enableSelfBandageHealingCheckbox.IsVisible = !IsMinimized;
                _enableNavigationCheckbox.IsVisible = !IsMinimized;
                _enableTamingTrainerCheckbox.IsVisible = !IsMinimized;

                WantUpdateSize = true;
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (!base.Draw(batcher, x, y))
            {
                return false;
            }

            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

            batcher.DrawString
            (
                Fonts.Bold,
                _cacheText,
                x + 10,
                y + 10,
                hueVector
            );

            return true;
        }


        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);

            writer.WriteAttributeString("minimized", IsMinimized.ToString());
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);

            bool.TryParse(xml.GetAttribute("minimized"), out bool b);
            IsMinimized = b;
        }

        protected override void OnDragEnd(int x, int y)
        {
            base.OnDragEnd(x, y);
            _last_position.X = ScreenCoordinateX;
            _last_position.Y = ScreenCoordinateY;
        }

        protected override void OnMove(int x, int y)
        {
            base.OnMove(x, y);

            _last_position.X = ScreenCoordinateX;
            _last_position.Y = ScreenCoordinateY;
        }
    }
}