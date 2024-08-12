using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mugen.AI;
using Mugen.Core;
using Mugen.GFX;
using Mugen.Physics;

namespace BattleSystem
{
    public class Cell : PassLevel
    {

        Arena _arena;

        public Unit _unit;
        public bool _isFree = true;
        public int _id = 0;
        float _alpha = 0f;
        float _acc = .025f;

        Vector2 _position = new();
        Point _mapPosition = new();
        Point _size = new();

        public bool _isMouseOver = false;

        Rectangle _rect;

        public Cell()
        {
            _passLevel = 0;
        }
        public Cell(Arena arena, Point mapPosition, Point size)
        {
            _arena = arena;
            _mapPosition = mapPosition;
            _size = size;

            _position.X = _mapPosition.X * _size.X;
            _position.Y = _mapPosition.Y * _size.Y;
            _rect = new Rectangle((int)_position.X, (int)_position.Y, size.X, size.Y);

            _passLevel = 0;

        }

        public void Update()
        {
            _isMouseOver = false;

            if (Misc.PointInRect(Game1.MouseControl.GetPosition() - _arena.XY.ToPoint(), _rect))
            {
                _isMouseOver = true;
            }

            if (_isMouseOver)
            {
                _alpha = 1f;
                _acc = .025f;
            }
            else
            {
                _acc += .002f;
                _alpha -= _acc;
                if (_alpha <= 0)
                    _alpha = 0;
            }
        }
        public void Draw(SpriteBatch batch, Point arenaTopLeft, int indexLayer)
        {
            if (indexLayer == (int)Layers.BackFX)
            {
                Rectangle rectCursor = new Rectangle(_position.ToPoint() + arenaTopLeft, _size);

                GFX.FillRectangle(batch, RectangleF.Extend(rectCursor, -(1 - _alpha) * 20f), Color.White * _alpha * .5f);
                GFX.Rectangle(batch, RectangleF.Extend(rectCursor, -(1 - _alpha) * 20f), Color.White * _alpha * .5f);
            }
            if (indexLayer == (int)Layers.Main)
            {

            }
        }

    }
}
