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
using ClassicUO.Game.AiEngine.AiClasses;
using ClassicUO.Game.AiEngine.Memory;
using ClassicUO.Game.AiEngine.Tasks;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using System.Windows;
using System.Windows.Forms;
using static ClassicUO.Renderer.UltimaBatcher2D;
using Button = ClassicUO.Game.UI.Controls.Button;
using Label = ClassicUO.Game.UI.Controls.Label;

namespace ClassicUO.Game.UI.Gumps
{
    [Flags]
    internal enum AiEngineButtons
    {
        None = 1,
        SETTESTING = 2,
        PLAYSCRIPT = 3,
        STOPSCRIPT = 4,
        RESETGOLDTOKENS = 5,
        ADDHOUSE = 6,
        SEARCHHOUSE = 7,
        SEARCHHOUSEOPEN = 8,
        COPYLOCATION = 9,
    }

    internal sealed class AIEngineOverlayGump : Gump {
        public static AIEngineOverlayGump Instance;

        private const string HEADER_LABEL_TEXT = "AI Engine";
        private const string SETTESTING_LABEL_TEXT = "Set Testing Location";
        private const string ADDHOUSE_LABEL_TEXT = "Add House";
        private const string SEARCHHOUSE_LABEL_TEXT = "Search House";
        private const string SEARCHHOUSE_OPEN_LABEL_TEXT = "Open Cont";
        private const string COPY_POINT_LABEL_TEXT = "Copy Point";
        
        private static Point _lastPosition = new Point(-1, -1);

        private uint _timeToUpdate;
        private readonly AlphaBlendControl _alphaBlendControl;
        private readonly Checkbox _enableSelfBandageHealingCheckbox;
        private readonly Checkbox _enableSelfBuffCheckbox;
        private readonly Checkbox _enableRecordDatabaseCheckbox;
        private readonly Checkbox _enableNavigationRecordingCheckbox;
        private readonly Checkbox _enableNavigationRecordingPathfinderCheckbox;
        private readonly Checkbox _enableNavigationCheckbox;
        private readonly Checkbox _enableNavigationMovementCheckbox;
        private readonly Checkbox _updateContainersCheckbox;
        private readonly Combobox _scriptComboBox;
        private readonly Button _playPauseButton;
        private readonly Button _setTestingButton;
        private readonly Button _resetGoldTokensButton;
        private readonly Button _addHouseButton;
        private string _cacheText = string.Empty;
        private readonly Label _setTestingLabel;
        private readonly Label _gainedGoldLabel;
        private readonly Label _gainedTokenLabel;
        private readonly Label _addHouseLabel;
        private readonly Label _currentTileLabel;
        private readonly Label _copyMapPointLabel;
        private readonly StbTextBox _searchHouseTextBox;
        private readonly Label _searchHouseLabel;
        private readonly Label _searchHouseOpenLabel;
        private readonly Button _searchHouseButton;
        private readonly Button _searchHouseOpenButton;
        private readonly Button _copyLocationButton;
        private readonly ResizePic _searchResizePic;
        public static int GainedGold = 0;
        public static int GainedTokens = 0;

        private Point3D _firstHousePoint = Point3D.Empty;
        private int _firstHouseMapIndex = 0;
        private Point3D _secondHousePoint = Point3D.Empty;

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

        public AIEngineOverlayGump(int x, int y) : base(0, 0) {
            Instance = this;
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
                _gainedGoldLabel = new Label($"Gained Gold: {GainedGold:N0}", true, textColor, font: (byte) 1) {
                    X = 90,
                    Y = 7
                }
            );

            Add
            (
                _gainedTokenLabel = new Label($"Gained Tokens: {GainedTokens:N0}", true, textColor, font: (byte) 1) {
                    X = 270,
                    Y = 7
                }
            );

            Add
            (
                _resetGoldTokensButton = new Button((int)AiEngineButtons.RESETGOLDTOKENS, 0x0481, 0x0483, 0x0482)
                {
                    X = 460,
                    Y = 7,
                    ButtonAction = ButtonAction.Activate
                }
            );

            Add
            (
                _currentTileLabel = new Label($"Current Tile: ", true, textColor, font: (byte) 1) {
                    X = 2,
                    Y = currentY
                }
            );

            currentY += SPACING;

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

            Add
            (
                _enableNavigationRecordingPathfinderCheckbox = new Checkbox
                (
                    0x00D2,
                    0x00D3,
                    "Use Pathfinder",
                    255,
                    1153
                )
                {
                    IsChecked = AiSettings.Instance.NavigationRecordingUsePathfinder,
                    X = 200,
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

            Add
            (
                _enableSelfBuffCheckbox = new Checkbox
                (
                    0x00D2,
                    0x00D3,
                    "Self Buff",
                    255,
                    1153
                )
                {
                    IsChecked = AiSettings.Instance.SelfBuff,
                    X = 2,
                    Y = currentY
                }
            );

            Add
            (
                _updateContainersCheckbox = new Checkbox
                (
                    0x00D2,
                    0x00D3,
                    "Update Bags",
                    255,
                    1153
                )
                {
                    IsChecked = AiSettings.Instance.UpdateContainers,
                    X = 150,
                    Y = currentY
                }
            );

            currentY += SPACING;

            Add
            (
                _addHouseButton = new Button((int)AiEngineButtons.ADDHOUSE, 0x0481, 0x0483, 0x0482)
                {
                    X = 2,
                    Y = currentY,
                    ButtonAction = ButtonAction.Activate
                }
            );

            _addHouseLabel = new Label($"{ADDHOUSE_LABEL_TEXT}  X: {_firstHousePoint.X}  Y: {_firstHousePoint.Y}  Z: {_firstHousePoint.Z}  MapIndex: {_firstHouseMapIndex}", true, textColor, font: (byte)1)
            {
                X = 40,
                Y = currentY
            };
            Add(_addHouseLabel);

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

            // Text Inputs
            Add
            (
                _searchResizePic = new ResizePic(0x0BB8)
                {
                    X = 2,
                    Y = currentY,
                    Width = 210,
                    Height = 30
                }
            );

            Add
            (
                _searchHouseTextBox = new StbTextBox
                (
                    5,
                    16,
                    190,
                    false,
                    hue: 0x034F
                )
                {
                    X = 5,
                    Y = currentY,
                    Width = 190,
                    Height = 25
                }
            );

            _searchHouseTextBox.SetText("");

            _searchHouseLabel = new Label(SEARCHHOUSE_LABEL_TEXT, true, textColor, font: (byte)1)
            {
                X = 220,
                Y = currentY + 4
            };
            Add(_searchHouseLabel);

            Add
            (
                _searchHouseButton = new Button((int)AiEngineButtons.SEARCHHOUSE, 0x0481, 0x0483, 0x0482)
                {
                    X = 310,
                    Y = currentY + 4,
                    ButtonAction = ButtonAction.Activate
                }
            );

            _searchHouseOpenLabel = new Label(SEARCHHOUSE_OPEN_LABEL_TEXT, true, textColor, font: (byte)1)
            {
                X = 345,
                Y = currentY + 4
            };
            Add(_searchHouseOpenLabel);

            Add
            (
                _searchHouseOpenButton = new Button((int)AiEngineButtons.SEARCHHOUSEOPEN, 0x0481, 0x0483, 0x0482)
                {
                    X = 380,
                    Y = currentY + 4,
                    ButtonAction = ButtonAction.Activate
                }
            );

            currentY += SPACING;

            Add
            (
                _copyLocationButton = new Button((int)AiEngineButtons.COPYLOCATION, 0x0481, 0x0483, 0x0482)
                {
                    X = 2,
                    Y = currentY,
                    ButtonAction = ButtonAction.Activate
                }
            );

            Add
            (
                _copyMapPointLabel = new Label(COPY_POINT_LABEL_TEXT, true, textColor, font: (byte) 1) {
                    X = 40,
                    Y = currentY
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
                ValueStringBuilder sb = new (span);

                sb.Append(string.Format(HEADER_LABEL_TEXT, CUOEnviroment.CurrentRefreshRate));
                

                _cacheText = sb.ToString();

                sb.Dispose();

                _alphaBlendControl.Width = IsMinimized ? 450 : 450;
                _alphaBlendControl.Height = IsMinimized ? 30 : 500;

                _gainedGoldLabel.Text = $"Gained Gold: {GainedGold:N0}";
                _gainedTokenLabel.Text = $"Gained Tokens: {GainedTokens:N0}";

                if (World.Player != null) {
                    var filePoint = Navigation.GetFilePointFromPoint(World.Player.Position.ToPoint3D());

                    _currentTileLabel.Text = $"Tile X: {filePoint.X}  Y: {filePoint.Y}";
                }

                var selectedScriptToRun = AiCore.Instance.MainScripts.Keys.ToList()[_scriptComboBox.SelectedIndex];

                bool shouldSave = AiSettings.Instance.SelfBandageHealing != _enableSelfBandageHealingCheckbox.IsChecked ||
                                  AiSettings.Instance.NavigationRecording != _enableNavigationRecordingCheckbox.IsChecked ||
                                  AiSettings.Instance.NavigationMovement != _enableNavigationMovementCheckbox.IsChecked ||
                                  AiSettings.Instance.NavigationTesting != _enableNavigationCheckbox.IsChecked ||
                                  AiSettings.Instance.RecordDatabase != _enableRecordDatabaseCheckbox.IsChecked ||
                                  AiSettings.Instance.SelfBuff != _enableSelfBuffCheckbox.IsChecked ||
                                  AiSettings.Instance.UpdateContainers != _updateContainersCheckbox.IsChecked ||
                                  AiSettings.Instance.NavigationRecordingUsePathfinder != _enableNavigationRecordingPathfinderCheckbox.IsChecked ||
                                  AiSettings.Instance.ScriptToRun != selectedScriptToRun;


                AiSettings.Instance.SelfBandageHealing = _enableSelfBandageHealingCheckbox.IsChecked;
                AiSettings.Instance.NavigationTesting = _enableNavigationCheckbox.IsChecked;
                AiSettings.Instance.NavigationRecording = _enableNavigationRecordingCheckbox.IsChecked;
                AiSettings.Instance.NavigationRecordingUsePathfinder = _enableNavigationRecordingPathfinderCheckbox.IsChecked;
                AiSettings.Instance.NavigationMovement = _enableNavigationMovementCheckbox.IsChecked;
                AiSettings.Instance.RecordDatabase = _enableRecordDatabaseCheckbox.IsChecked;
                AiSettings.Instance.SelfBuff = _enableSelfBuffCheckbox.IsChecked;
                AiSettings.Instance.UpdateContainers = _updateContainersCheckbox.IsChecked;
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
                _gainedGoldLabel.IsVisible = true;
                _gainedTokenLabel.IsVisible = true;
                _resetGoldTokensButton.IsVisible = !IsMinimized;
                _enableSelfBuffCheckbox.IsVisible = !IsMinimized;
                _addHouseButton.IsVisible = !IsMinimized;
                _addHouseLabel.IsVisible = !IsMinimized;
                _currentTileLabel.IsVisible = !IsMinimized;
                _enableNavigationRecordingPathfinderCheckbox.IsVisible = !IsMinimized;
                _searchHouseTextBox.IsVisible = !IsMinimized;
                _searchHouseButton.IsVisible = !IsMinimized;
                _searchHouseLabel.IsVisible = !IsMinimized;
                _searchResizePic.IsVisible = !IsMinimized;
                _searchHouseOpenLabel.IsVisible = !IsMinimized;
                _searchHouseOpenButton.IsVisible = !IsMinimized;
                _updateContainersCheckbox.IsVisible = !IsMinimized;
                _copyLocationButton.IsVisible = !IsMinimized;
                _copyMapPointLabel.IsVisible = !IsMinimized;


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

        internal void UpdatePlayPauseButton() {
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
                    if (AiCore.IsScriptRunning) {
                        AiCore.Instance.StopScript();
                    }
                    else {
                        AiCore.Instance.StartScript();
                    }
                    break;
                }

                case (int) AiEngineButtons.RESETGOLDTOKENS: {
                    ItemLearnerTask.ShouldResetGoldTokens = true;
                    break;
                }

                case (int) AiEngineButtons.ADDHOUSE: {
                    if (Equals(_firstHousePoint, Point3D.Empty) || _firstHouseMapIndex == 0) {
                        _firstHousePoint = World.Player.Position.ToPoint3D();
                        _firstHouseMapIndex = World.MapIndex;
                        _addHouseLabel.Text = $"{ADDHOUSE_LABEL_TEXT}  X: {_firstHousePoint.X}  Y: {_firstHousePoint.Y}  Z: {_firstHousePoint.Z}  MapIndex: {_firstHouseMapIndex}";
                        GameActions.MessageOverhead("Added First House Point", World.Player.Serial);

                        break;
                    }

                    if (Equals(_secondHousePoint, Point3D.Empty)) {
                        _secondHousePoint = World.Player.Position.ToPoint3D();
                        _addHouseLabel.Text = $"{ADDHOUSE_LABEL_TEXT}  X: {_secondHousePoint.X}  Y: {_secondHousePoint.Y}  Z: {_secondHousePoint.Z}  MapIndex: {_firstHouseMapIndex}";
                        GameActions.MessageOverhead("Added Second House Point", World.Player.Serial);

                        break;
                    }

                    var aiHouse = new AiHouse(_firstHousePoint, _secondHousePoint, World.Player.Position.ToPoint3D(), _firstHouseMapIndex);
                    HouseMemory.Instance.AddHouse(aiHouse);

                    _firstHousePoint = Point3D.Empty;
                    _firstHouseMapIndex = 0;
                    _addHouseLabel.Text = $"{ADDHOUSE_LABEL_TEXT}  X: {_firstHousePoint.X}  Y: {_firstHousePoint.Y}  Z: {_firstHousePoint.Z}  MapIndex: {_firstHouseMapIndex}";
                    GameActions.MessageOverhead("Added House to the list", World.Player.Serial);

                    break;
                }

                case (int) AiEngineButtons.SEARCHHOUSE: {
                    if (!string.IsNullOrEmpty(_searchHouseTextBox.Text)) {
                        HouseMemory.Instance.SearchForItemsToHighlight(_searchHouseTextBox.Text);
                    }

                    break;
                }

                case (int) AiEngineButtons.SEARCHHOUSEOPEN: {
                    HouseMemory.Instance.OpenClosestContainer();
                    break;
                }

                case (int) AiEngineButtons.COPYLOCATION: {
                    var position = World.Player.Position;
                    var mapIndex = World.MapIndex;
                    var location = $"new MapPoint3D({position.X}, {position.Y}, {position.Z}, {mapIndex});";
                    GameActions.MessageOverhead("Copied Point", World.Player.Serial);
                    Clipboard.SetText(location);
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