﻿#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2018 ClassicUO Development Team)
//    
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion
using ClassicUO.Game;
using ClassicUO.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer
{
    public sealed class CursorRenderer
    {
        private static readonly ushort[,] _cursorData = new ushort[2, 16] { { 0x206A, 0x206B, 0x206C, 0x206D, 0x206E, 0x206F, 0x2070, 0x2071, 0x2072, 0x2073, 0x2074, 0x2075, 0x2076, 0x2077, 0x2078, 0x2079 }, { 0x2053, 0x2054, 0x2055, 0x2056, 0x2057, 0x2058, 0x2059, 0x205A, 0x205B, 0x205C, 0x205D, 0x205E, 0x205F, 0x2060, 0x2061, 0x2062 } };

        private readonly int[,] _cursorOffset = new int[2, 16];

        private Texture2D _blackTexture;
        private Graphic _graphic = 0x2073;

        private bool _needGraphicUpdate;

        private InputManager _inputManager;

        public CursorRenderer()
        {
            _inputManager = Service.Get<InputManager>();

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 16; j++)
                {
                    ushort id = _cursorData[i, j];

                    Texture2D texture = IO.Resources.Art.GetStaticTexture(id);


                    if (i == 0)
                    {
                        if (texture != null)
                        {
                            float offX = 0;
                            float offY = 0;

                            float dw = texture.Width;
                            float dh = texture.Height;

                            if (id == 0x206A)
                            {
                                offX = -4f;
                            }
                            else if (id == 0x206B)
                            {
                                offX = -dw + 3f;
                            }
                            else if (id == 0x206C)
                            {
                                offX = -dw + 3f;
                                offY = -(dh / 2f);
                            }
                            else if (id == 0x206D)
                            {
                                offX = -dw;
                                offY = -dh;
                            }
                            else if (id == 0x206E)
                            {
                                offX = -(dw * 0.66f);
                                offY = -dh;
                            }
                            else if (id == 0x206F)
                            {
                                offY = -dh + 4f;
                            }
                            else if (id == 0x2070)
                            {
                                offY = -dh + 4f;
                            }
                            else if (id == 0x2075)
                            {
                                offY = -4f;
                            }
                            else if (id == 0x2076)
                            {
                                offX = -12f;
                                offY = -14f;
                            }
                            else if (id == 0x2077)
                            {
                                offX = -(dw / 2f);
                                offY = -(dh / 2f);
                            }
                            else if (id == 0x2078)
                            {
                                offY = -(dh * 0.66f);
                            }
                            else if (id == 0x2079)
                            {
                                offY = -(dh / 2f);
                            }

                            switch (id)
                            {
                                case 0x206B:
                                    offX = -29;
                                    offY = -1;
                                    break;
                                case 0x206C:
                                    offX = -41;
                                    offY = -9;
                                    break;
                                case 0x206D:
                                    offX = -36;
                                    offY = -25;
                                    break;
                                case 0x206E:
                                    offX = -14;
                                    offY = -33;
                                    break;
                                case 0x206F:
                                    offX = -2;
                                    offY = -26;
                                    break;
                                case 0x2070:
                                    offX = -3;
                                    offY = -8;
                                    break;
                                case 0x2071:
                                    offX = -1;
                                    offY = -1;
                                    break;
                                case 0x206A:
                                    offX = -4;
                                    offY = -2;
                                    break;
                                case 0x2075:
                                    offX = -2;
                                    offY = -10;
                                    break;
                                default:
                                    break;
                            }

                            _cursorOffset[0, j] = (int)offX;
                            _cursorOffset[1, j] = (int)offY;
                        }
                        else
                        {
                            _cursorOffset[0, j] = 0;
                            _cursorOffset[1, j] = 0;
                        }
                    }
                }
            }
        }


        public Graphic Graphic
        {
            get => _graphic;
            set
            {
                if (_graphic != value)
                {
                    _graphic = value;
                    _needGraphicUpdate = true;
                }
            }
        }

        public SpriteTexture Texture { get; private set; }
        public Point ScreenPosition => _inputManager.MousePosition;

        public void Update(double totalMS, double frameMS)
        {
            if (Texture == null || Texture.IsDisposed || _needGraphicUpdate)
            {
                Texture = IO.Resources.Art.GetStaticTexture(Graphic);
                _blackTexture = new Texture2D(Service.Get<SpriteBatch3D>().GraphicsDevice, 1, 1);
                _blackTexture.SetData(new[] { Color.Black });
                _needGraphicUpdate = false;
            }
            else
            {
                Texture.Ticks = World.Ticks;
            }
        }

        public void Draw(SpriteBatchUI sb)
        {
            ushort id = Graphic;

            if (id < 0x206A)
            {
                id -= 0x2053;
            }
            else
            {
                id -= 0x206A;
            }

            if (id < 16)
            {
                Vector3 v = new Vector3(ScreenPosition.X + _cursorOffset[0, id], ScreenPosition.Y + _cursorOffset[1, id], 0);
                sb.Draw2D(Texture, v, Vector3.Zero);


                //        // tooltip testing, very nice!
                //sb.Draw2D(_blackTexture, new Rectangle(ScreenPosition.X + _cursorOffset[0, id] - 100, ScreenPosition.Y + _cursorOffset[1, id] - 50, 100, 50), RenderExtentions.GetHueVector(0, false, true, false));
            }
        }
    }
}