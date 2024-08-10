using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Mugen.Core;
using Mugen.GFX;

namespace BattleSystem
{
    public class Trail : Node
    {
        Color _color;
        Vector2 _position = new Vector2();
        Vector2 _scale;
        float _stepAlpha;
        public Trail(Vector2 position, Vector2 scale, float stepAplha = 0.5f, Color color = default)
        {
            _position = position;
            _scale = scale;
            _color = color;
            _stepAlpha = stepAplha;
        }

        public override Node Update(GameTime gameTime)
        {
            _alpha += -_stepAlpha;

            if (_alpha <= 0f)
                KillMe();
            return base.Update(gameTime);
        }

        public override Node Draw(SpriteBatch batch, GameTime gameTime, int indexLayer)
        {

            if (indexLayer == (int)Layers.FX)
            {
                //GFX.Circle(batch, pos, _size * _alpha, (int)_size, _color * _alpha, 4f);
                //GFX.FillRectangleCentered(batch, pos, Vector2.One * _size, _color * _alpha, 0);

                //GFX.Draw(batch, Game1._texTrail, _color * _alpha, 0, pos, Mugen.Physics.Position.CENTER, Vector2.One * (_alpha/2));
                //GFX.Draw(batch, Game1._texTrail, _color * _alpha, 0, pos, Mugen.Physics.Position.CENTER, Vector2.One * _alpha);
                GFX.Draw(batch, Game1._texTrail, _color * _alpha, 0, _position, Mugen.Physics.Position.CENTER, _scale);

            }


            return base.Draw(batch, gameTime, indexLayer);
        }
    }
}
