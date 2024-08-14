using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mugen.Core;
using Mugen.GFX;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;
using System;

namespace BattleSystem
{
    internal class ScreenPlay : Node
    {
        public const int CellW = 128;
        public const int CellH = 128;

        public const int ArenaW = 12;
        public const int ArenaH = 8;

        Game1 _game;
        
        Addon.Loop _loop;
        Arena _arena;
        //ChainGrid _chainGrid;

        Gui.Button _btnRoll;
        Gui.Button _btnAction;

        Node _layerGui;
        public ScreenPlay(Game1 game) 
        { 
            _game = game;

            SetSize(Game1.ScreenW, Game1.ScreenH);

            _loop = new(this);
            _loop.SetLoop(0, -8f, 8f, .05f, Mugen.Animation.Loops.PINGPONG);
            _loop.Start();

            AddAddon(_loop);

            _arena = new Arena(ArenaW, ArenaH, CellW, CellH);
            _arena.SetPosition(320, 20);
            _arena.AppendTo(this);

            //_chainGrid = new ChainGrid(new Point(3, 3), new Point(80,80));
            //_chainGrid.SetPosition(40, Game1.ScreenH - 480).AppendTo(this);


            _layerGui = new Node();

            var style = (JObject)JsonConvert.DeserializeObject(File.ReadAllText("Content/Misc/styleBtn.json"));

            _btnRoll = (Gui.Button)new Gui.Button(Game1.MouseControl, "Roll", style)
                .SetPosition(160, Game1.ScreenH - 120)
                .AppendTo(_layerGui);

            _btnAction = (Gui.Button)new Gui.Button(Game1.MouseControl, "Action", style)
                .SetPosition(160, Game1.ScreenH - 40)
                .AppendTo(_layerGui);

        }
        public override Node Init()
        {
            _arena.ClearArena();

            InitChilds();

            _arena.AddUnit(9, 1, 2, 2);
            _arena.AddUnit(5, 2, 2, 3);

            for (int i = 0; i < 8; i++)
            {
                int x, y;

                do
                {
                    x = Misc.Rng.Next(0, ArenaW);
                    y = Misc.Rng.Next(0, ArenaH);

                } while (!_arena.AddUnit(x, y, 1, 1));


            }

            return base.Init();
        }
        public override Node Update(GameTime gameTime)
        {

            if (_btnRoll._navi._onClick)
            {
                Game1._soundClock.Play(Game1._volumeMaster * .5f, 1f, .5f);

                //_chainGrid.Init();
                //Game1.Quit();
            }

            if (_btnAction._navi._onClick)
            {
                //Console.WriteLine("ScreenPlay.Init");
                Game1._soundClock.Play(Game1._volumeMaster * .5f, 1f, .5f);
                Init();
            }

            //_game.IsMouseVisible = !_mouseControl._isActiveDrag; // hide mouse when drag !

            UpdateChilds(gameTime);

            _layerGui.UpdateChilds(gameTime);

            return base.Update(gameTime);
        }

        public override Node Draw(SpriteBatch batch, GameTime gameTime, int indexLayer)
        {
            batch.GraphicsDevice.Clear(Color.Transparent);

            switch (indexLayer)
            {
                case (int)Layers.Main:

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
                    break;

                case (int)Layers.Gui:

                    DrawChilds(batch, gameTime, indexLayer);
                    _layerGui.DrawChilds(batch, gameTime, indexLayer);
                    break;

                case (int)Layers.FrontFX:

                    DrawChilds(batch, gameTime, indexLayer);
                    _layerGui.DrawChilds(batch, gameTime, indexLayer);

                    //GFX.Sight(batch, _mouse, Game1.ScreenW, Game1.ScreenH, Color.Red * .5f, 3f);
                    //GFX.Sight(batch, _mouse, Game1.ScreenW, Game1.ScreenH, Color.Yellow, 1f);
                    break;

                case (int)Layers.BackFX:

                    DrawChilds(batch, gameTime, indexLayer);

                    //GFX.Sight(batch, _mouse, Game1.ScreenW, Game1.ScreenH, Color.Red * .5f, 3f);
                    //GFX.Sight(batch, _mouse, Game1.ScreenW, Game1.ScreenH, Color.Yellow, 1f);
                    break;

                case (int)Layers.Debug:

                    DrawChilds(batch, gameTime, indexLayer);
                    break;

                default:
                    break;
            }


            return base.Draw(batch, gameTime, indexLayer);
        }

    }
}
