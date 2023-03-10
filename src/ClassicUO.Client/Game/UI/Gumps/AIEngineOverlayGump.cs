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
using System.Linq;
using System.Text;
using System.Xml;
using AkatoshQuester.Helpers.LightGeometry;
using ClassicUO.AiEngine;
using ClassicUO.Configuration;
using ClassicUO.Game.AiEngine;
using ClassicUO.Game.AiEngine.Memory;
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
    [Flags]
    internal enum AiEngineButtons
    {
        None = 1,
        SETTESTING = 2,
        PLAYSCRIPT = 3,
        STOPSCRIPT = 4,
    }

    internal sealed class AIEngineOverlayGump : Gump
    {
        private const string HEADER_LABEL_TEXT = "AI Engine";
        private const string SETTESTING_LABEL_TEXT = "Set Testing Location";
        
        private static Point _lastPosition = new Point(-1, -1);

        private uint _timeToUpdate;
        private readonly AlphaBlendControl _alphaBlendControl;
        private readonly Checkbox _enableSelfBandageHealingCheckbox;
        private readonly Checkbox _enableRecordDatabaseCheckbox;
        private readonly Checkbox _enableNavigationRecordingCheckbox;
        private readonly Checkbox _enableNavigationCheckbox;
        private readonly Checkbox _enableNavigationMovementCheckbox;
        private readonly Combobox _scriptComboBox;
        private readonly Button _playPauseButton;
        private readonly Button _setTestingButton;
        private string _cacheText = string.Empty;
        private readonly Label _setTestingLabel;

        private Combobox AddCombobox
        (
            ScrollArea area,
            string[] values,
            int currentIndex,
            int x,
            int y,
            int width
        )
        {
            Combobox combobox = new Combobox(x, y, width, values)
            {
                SelectedIndex = currentIndex
            };

            return combobox;
        }

        public AIEngineOverlayGump(int x, int y) : base(0, 0)
        {
            CanMove = true;
            CanCloseWithEsc = false;
            CanCloseWithRightClick = true;
            AcceptMouseInput = true;
            AcceptKeyboardInput = false;
            IsMinimized = true;

            Width = 300;
            Height = 50;
            X = _lastPosition.X <= 0 ? x : _lastPosition.X;
            Y = _lastPosition.Y <= 0 ? y : _lastPosition.Y;

            const int SPACING = 35;
            int currentY = 40;

            ushort textColor = 0xFFFF;

            AiSettings.Load();

            Add
            (
                _alphaBlendControl = new AlphaBlendControl(.7f)
                {
                    Width = Width, Height = Height
                }
            );

            Add
            (
                _enableRecordDatabaseCheckbox = new Checkbox
                (
                    0x00D2,
                    0x00D3,
                    "Record Database",
                    255,
                    1153
                )
                {
                    IsChecked = AiSettings.Instance.RecordDatabase,
                    X = 2,
                    Y = currentY
                }
            );

            currentY += SPACING;

            Add
            (
                _enableNavigationRecordingCheckbox = new Checkbox
                (
                    0x00D2,
                    0x00D3,
                    "Navigation Recording",
                    255,
                    1153
                )
                {
                    IsChecked = AiSettings.Instance.NavigationRecording,
                    X = 2,
                    Y = currentY
                }
            );

            currentY += SPACING;

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
                    IsChecked = AiSettings.Instance.NavigationTesting,
                    X = 2,
                    Y = currentY
                }
            );

            currentY += SPACING;

            Add
            (
                _setTestingButton = new Button((int)AiEngineButtons.SETTESTING, 0x0481, 0x0483, 0x0482)
                {
                    X = 2,
                    Y = currentY,
                    ButtonAction = ButtonAction.Activate
                }
            );

            _setTestingLabel = new Label($"{SETTESTING_LABEL_TEXT}  X: {AiSettings.Instance.TestingNavigationPoint.X}  Y: {AiSettings.Instance.TestingNavigationPoint.Y}  Z: {AiSettings.Instance.TestingNavigationPoint.Z}  MapIndex: {AiSettings.Instance.TestingNavigationMapIndex}", true, textColor, font: (byte)1)
            {
                X = 40,
                Y = currentY
            };

            Add(_setTestingLabel);

            currentY += SPACING;

            Add
            (
                _enableNavigationMovementCheckbox = new Checkbox
                (
                    0x00D2,
                    0x00D3,
                    "Navigation Movement",
                    255,
                    1153
                )
                {
                    IsChecked = AiSettings.Instance.NavigationMovement,
                    X = 2,
                    Y = currentY
                }
            );


            currentY += SPACING;

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
                    IsChecked = AiSettings.Instance.SelfBandageHealing,
                    X = 2,
                    Y = currentY
                }
            );

            currentY += SPACING;


            _scriptComboBox = AddCombobox
                (null, AiCore.Instance.MainScripts.Keys.ToArray(), 0, 2, currentY, 150);
            Add(_scriptComboBox);

            _playPauseButton = new Button((int) AiEngineButtons.PLAYSCRIPT, 0x07e5, 0x07e6, 0x07e7) {
                X = 160,
                Y = currentY,
                ButtonAction = ButtonAction.Activate
            };

            Add
            (
                _playPauseButton
            );

            currentY += SPACING;
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
                ValueStringBuilder sb = new (span);

                sb.Append(string.Format(HEADER_LABEL_TEXT, CUOEnviroment.CurrentRefreshRate));
                

                _cacheText = sb.ToString();

                sb.Dispose();

                _alphaBlendControl.Width = IsMinimized ? 85 : 450;
                _alphaBlendControl.Height = IsMinimized ? 30 : 500;

                var selectedScriptToRun = AiCore.Instance.MainScripts.Keys.ToList()[_scriptComboBox.SelectedIndex];

                bool shouldSave = AiSettings.Instance.SelfBandageHealing != _enableSelfBandageHealingCheckbox.IsChecked ||
                                  AiSettings.Instance.NavigationRecording != _enableNavigationRecordingCheckbox.IsChecked ||
                                  AiSettings.Instance.NavigationMovement != _enableNavigationMovementCheckbox.IsChecked ||
                                  AiSettings.Instance.NavigationTesting != _enableNavigationCheckbox.IsChecked ||
                                  AiSettings.Instance.RecordDatabase != _enableRecordDatabaseCheckbox.IsChecked ||
                                  AiSettings.Instance.ScriptToRun != selectedScriptToRun;


                AiSettings.Instance.SelfBandageHealing = _enableSelfBandageHealingCheckbox.IsChecked;
                AiSettings.Instance.NavigationTesting = _enableNavigationCheckbox.IsChecked;
                AiSettings.Instance.NavigationRecording = _enableNavigationRecordingCheckbox.IsChecked;
                AiSettings.Instance.NavigationMovement = _enableNavigationMovementCheckbox.IsChecked;
                AiSettings.Instance.RecordDatabase = _enableRecordDatabaseCheckbox.IsChecked;
                AiSettings.Instance.ScriptToRun = selectedScriptToRun;

                if (shouldSave) {
                    AiSettings.Save();
                }

                _enableSelfBandageHealingCheckbox.IsVisible = !IsMinimized;
                _enableNavigationCheckbox.IsVisible = !IsMinimized;
                _enableNavigationRecordingCheckbox.IsVisible = !IsMinimized;
                _scriptComboBox.IsVisible = !IsMinimized;
                _setTestingLabel.IsVisible = !IsMinimized;
                _playPauseButton.IsVisible = !IsMinimized;
                _enableNavigationMovementCheckbox.IsVisible = !IsMinimized;
                _setTestingButton.IsVisible = !IsMinimized;
                _enableRecordDatabaseCheckbox.IsVisible = !IsMinimized;


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

        public override void OnButtonClick(int buttonID) {
            switch (buttonID) {
                case (int) AiEngineButtons.SETTESTING: {
                    Navigation.UpdateNavigationTestingLocation();

                    _setTestingLabel.Text =
                        $"{SETTESTING_LABEL_TEXT}  X: {AiSettings.Instance.TestingNavigationPoint.X}  Y: {AiSettings.Instance.TestingNavigationPoint.Y}  Z: {AiSettings.Instance.TestingNavigationPoint.Z}  MapIndex: {AiSettings.Instance.TestingNavigationMapIndex}";

                    AiSettings.Save();
                    break;
                }

                case (int) AiEngineButtons.PLAYSCRIPT: {
                    AiCore.IsScriptRunning = !AiCore.IsScriptRunning;

                    if (AiCore.IsScriptRunning) {
                        _playPauseButton.ButtonGraphicNormal = 0x07e8;
                        _playPauseButton.ButtonGraphicOver = 0x07e9;
                        _playPauseButton.ButtonGraphicPressed = 0x07ea;
                    }
                    else {
                        _playPauseButton.ButtonGraphicNormal = 0x07e5;
                        _playPauseButton.ButtonGraphicOver = 0x07e6;
                        _playPauseButton.ButtonGraphicPressed = 0x07e7;
                    }

                    break;
                }
            }
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
            _lastPosition.X = ScreenCoordinateX;
            _lastPosition.Y = ScreenCoordinateY;
        }

        protected override void OnMove(int x, int y)
        {
            base.OnMove(x, y);

            _lastPosition.X = ScreenCoordinateX;
            _lastPosition.Y = ScreenCoordinateY;
        }
    }
}