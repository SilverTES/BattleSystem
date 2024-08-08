using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Mugen.Core;
using Mugen.GFX;
using Mugen.Input;
using Mugen.Physics;

namespace BattleSystem
{
    // LABEL : Card
    internal class Card : Node
    {
        protected int _energy = 24;
        protected int _power = 7;

        public Addon.Resizable _resizable;
        public Addon.Draggable _draggable;
        public Addon.Loop _loop;

        Game1 _game;
        //ScreenPlay _screenPlay;
        MouseControl _mouse;
        public Card(Game1 game, MouseControl mouse) 
        {
            _type = UID.Get<Card>();
            _game = game;
            _mouse = mouse;

            SetSize(128, 128);
            SetPivot(Position.CENTER);

            _resizable = new Addon.Resizable(this,_mouse);
            _resizable.SetResizable(true);
            _resizable.Init(8);
            //AddAddon(_resizable);

            _draggable = new Addon.Draggable(this, _mouse);
            _draggable.SetLimitRect(true);
            _draggable.SetLimitRect(_nodeRoot);
            _draggable.SetDraggable(true);
            _draggable.SetDragRectNode(true);
            AddAddon(_draggable);

            _loop = new Addon.Loop(this);
            _loop.SetLoop(0, -Geo.RAD_22_5/10, Geo.RAD_22_5/10, .001f, Mugen.Animation.Loops.PINGPONG);
            _loop.Start();
            AddAddon(_loop);
        }
        public override Node Init()
        {
            return base.Init();
        }
        public override Node Update(GameTime gameTime)
        {
            UpdateRect();

            if (_navi._isMouseOver && _mouse._onClick && !_mouse._isOverAny && !_mouse._isActiveReSize)
            {
                _parent.GotoFront(_index);
            }

            if (_navi._isMouseOver && _mouse._onClick && Keyboard.GetState().IsKeyDown(Keys.LeftAlt))
            {
                KillMe();
            }

            if (_draggable._isDragged)
                Arena.CurrentDragged = this;

            return base.Update(gameTime);
        }
        public override Node Draw(SpriteBatch batch, GameTime gameTime, int indexLayer)
        {
            if (indexLayer == (int)Layers.Main)
            {
                //GFX.FillRectangle(batch, AbsRect, Color.DarkSlateGray);
                //GFX.Grid(batch, AbsX - _oX, AbsY - _oY, _rect.Width, _rect.Height, 4, 4, Color.Black * .25f);

                //Game1._effectColor.Parameters["SpriteTextureSampler"]?.SetValue(Game1._texFace);
                //Game1._effectColor.CurrentTechnique.Passes[0].Apply();

                //Game1._effectBasic.Parameters["surface_alpha"]?.SetValue(.5f);
                //Game1._effectBasic.CurrentTechnique.Passes[0].Apply();

            

                batch.Draw(Game1._texFace, (Rectangle)(AbsRectF + OXY), Game1._texFace.Bounds, Color.White, _loop._current, new Vector2(Game1._texFace.Width/2, Game1._texFace.Height/2), SpriteEffects.FlipHorizontally, 0);
                //batch.Draw(_game._texFace, (Rectangle)(AbsRectF + OXY), _game._texFace.Bounds, Color.White, _loop._current, Vector2.Zero, SpriteEffects.None, 0);


                Vector2[] v = GFX.RectangleEx(batch, AbsXY, _rect.CloneSize(), -_rect.GetSize()/2, Color.Orange * .5f, _loop._current, false);

                //if (_resizable._isMouseOver)
                //    GFX.Rectangle(batch, AbsRect, Color.Red);
                //Draw.Rectangle(batch, Gfx.AddRect(AbsRect, -2,-2,4,4), Color.White);
                //batch.Draw(_game._texFace, AbsXY, Color.White);


                GFX.CenterBorderedStringXY(batch, Game1._fontMain, $"{_energy}", v[RectangleF.VertexTopCenter] + new Vector2(0,10), Color.Gold, Color.Red);
                GFX.CenterBorderedStringXY(batch, Game1._fontMain, $"{_power}", v[RectangleF.VertexBottomLeft] + new Vector2(10,-10), Color.Yellow, Color.Red);

                //GFX.CenterStringXY(batch, _game._fontMain, $"{_power}", AbsRectF.TopLeft.X, AbsRectF.TopLeft.Y, Color.Gold);
                //GFX.CenterStringXY(batch, _game._fontMain, $"{_energy}", AbsRectF.BottomRight.X, AbsRectF.BottomRight.Y, Color.MediumSlateBlue);

                //GFX.LeftTopString(batch, _game._fontMain, $"_mouse._isActiveDrag={_screenPlay._inputMouse._isActiveDrag}", AbsX, AbsY - 32, Color.CadetBlue);
                //GFX.LeftTopString(batch, _game._fontMain, $"_mouse._isOverAny={_screenPlay._inputMouse._isOverAny}", AbsX, AbsY - 48, Color.LightSteelBlue);
            }

            if (indexLayer == (int)Layers.FX)
            {
                //_game.GraphicsDevice.BlendState = BlendState.Additive;

                //if (_navi._isFocus)
                //    //    //GFX.Rectangle(batch, AbsRect, Color.Yellow);
                //    GFX.RectangleEx(batch, AbsXY, _rect.CloneSize(), -_rect.GetSize() / 2, Color.Yellow, _loop._current, true, 3);
                //else
                //    //    //GFX.Rectangle(batch, AbsRect, Color.Red * .25f);
                //    GFX.RectangleEx(batch, AbsXY, _rect.CloneSize(), -_rect.GetSize() / 2, Color.Yellow * .25f, _loop._current);

                //_game.GraphicsDevice.BlendState = BlendState.AlphaBlend;
            }

            return base.Draw(batch, gameTime, indexLayer);
        }

    }
}
