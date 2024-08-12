﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Mugen.Core;
using Mugen.GFX;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;

namespace BattleSystem
{
    internal class ScreenPlay : Node
    {
        public const int CellW = 128;
        public const int CellH = 128;

        Game1 _game;
        
        Addon.Loop _loop;
        Arena _arena;

        Gui.Button _btnQuit;

        Node _layerGui;
        public ScreenPlay(Game1 game) 
        { 
            _game = game;

            SetSize(Game1.ScreenW, Game1.ScreenH);

            //_mouseControl = new();

            _loop = new(this);
            _loop.SetLoop(0, -8f, 8f, .05f, Mugen.Animation.Loops.PINGPONG);
            _loop.Start();

            AddAddon(_loop);

            _arena = new Arena(_game, 12, 8, CellW, CellH);
            _arena.SetPosition(320, 20);
            _arena.AppendTo(this);

            _layerGui = new Node();

            var style = (JObject)JsonConvert.DeserializeObject(File.ReadAllText("Content/Misc/styleBtn.json"));



            _btnQuit = (Gui.Button)new Gui.Button(Game1.MouseControl, "Quit", style)
                .SetPosition(160, Game1.ScreenH - 40)
                .AppendTo(_layerGui);

        }
        public override Node Init()
        {
            InitChilds();

            _arena.AddUnit(9, 1, 2, 2);
            //_arena.AddUnit(2, 6, 2, 1);
            //_arena.AddUnit(9, 2, 1, 2);
            _arena.AddUnit(6, 5, 1, 1);
            _arena.AddUnit(4, 3, 1, 1);
            _arena.AddUnit(7, 6, 1, 1);
            _arena.AddUnit(8, 4, 1, 1);

            return base.Init();
        }
        public override Node Update(GameTime gameTime)
        {

            //if (_mouseControl._onClick) Console.WriteLine("On Click");
            //if (_mouseControl._offClick) Console.WriteLine("Off Click");

            if (Game1.MouseControl._isOverAny)
                Mouse.SetCursor(Game1._mouseCursor2);
            else
                Mouse.SetCursor(Game1._mouseCursor);


            if (_btnQuit._navi._onPress)
                Game1.Quit();


            //_game.IsMouseVisible = !_mouseControl._isActiveDrag; // hide mouse when drag !

            UpdateChilds(gameTime);

            _layerGui.UpdateChilds(gameTime);

            return base.Update(gameTime);
        }

        public override Node Draw(SpriteBatch batch, GameTime gameTime, int indexLayer)
        {
            batch.GraphicsDevice.Clear(Color.Transparent);

            if (indexLayer == (int)Layers.Main)
            {
                batch.Draw(Game1._texBackground, new Vector2(0,_loop._current), Color.White * .5f);
                
                //GFX.Grid(batch, 0, 0, Game1.ScreenW, Game1.ScreenH, CellW, CellH, Color.Gray * .25f, 3);
                //GFX.Grid(batch, 0, 0, Game1.ScreenW, Game1.ScreenH, CellW, CellH, Color.Black * .5f, 1);

                //_arena.Draw(batch, gameTime);

                //Draw.Sight(batch, _game._mouseX, _game._mouseY, Game1.ScreenW, Game1.ScreenH, Color.OrangeRed * .5f, 1f);
                //GFX.Circle(batch, _game._mouse.X, _game._mouse.Y, 8, 16, Color.Yellow * .5f, 3f);

                //batch.DrawString(_game._fontMain, $"Mouse = {_mouse.X}-{_mouse.Y}", new Vector2(4, 2), Color.Yellow);
                //batch.DrawString(_game._fontMain, $"GamePad = {GamePad.GetState(PlayerIndex.One).ThumbSticks.Left}:{GamePad.GetState(PlayerIndex.One).ThumbSticks.Right}", new Vector2(4, 64), Color.Gold);

                //GFX.RectangleEx(batch, new Vector2(340, 200), new RectangleF(80, 60), new Vector2(-40,-30), Color.Green, Geo.RAD_45, 2, true);

                DrawChilds(batch, gameTime, indexLayer);
                GFX.LeftTopBorderedString(batch, Game1._fontMain, $"{_arena.NbActive()} - {_arena.NbNode()}", 10, 30, Color.White, Color.Red);


                //batch.Draw(Game1._texBtnBase, Vector2.One * 20, Color.White);

            }

            if (indexLayer == (int)Layers.Gui)
            {
                DrawChilds(batch, gameTime, indexLayer);
                _layerGui.DrawChilds(batch, gameTime, indexLayer);

            }

            if (indexLayer == (int)Layers.FrontFX)
            {
                DrawChilds(batch, gameTime, indexLayer);
                _layerGui.DrawChilds(batch, gameTime, indexLayer);

                //GFX.Sight(batch, _mouse, Game1.ScreenW, Game1.ScreenH, Color.Red * .5f, 3f);
                //GFX.Sight(batch, _mouse, Game1.ScreenW, Game1.ScreenH, Color.Yellow, 1f);
            }

            if (indexLayer == (int)Layers.BackFX)
            {
                DrawChilds(batch, gameTime, indexLayer);

                //GFX.Sight(batch, _mouse, Game1.ScreenW, Game1.ScreenH, Color.Red * .5f, 3f);
                //GFX.Sight(batch, _mouse, Game1.ScreenW, Game1.ScreenH, Color.Yellow, 1f);
            }

            if (indexLayer == (int)Layers.Debug)
            {
                DrawChilds(batch, gameTime, indexLayer);
            }

            return base.Draw(batch, gameTime, indexLayer);
        }

    }
}
