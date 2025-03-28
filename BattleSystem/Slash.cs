using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Aseprite;
using Mugen.Core;

namespace BattleSystem
{
    public class Slash : Node
    {
        AnimatedSprite _sprite;

        public Slash() 
        {
            _sprite = Game1._spriteSheetSlash.CreateAnimatedSprite("slash");
            _sprite.Speed = 1.5f;

            //_spriteSlash.Color = Color.White;
            //var origin = _spriteSlash.CurrentFrame.TextureRegion.GetSlice("Slice1").Origin;
            _sprite.ScaleX = .5f;
            _sprite.ScaleY = .5f;
            _sprite.Origin = new Vector2(_sprite.Width / 2, _sprite.Height / 2);
            //_spriteSlash.Origin = origin;
            _sprite.Play(1);
        }
        public override Node Update(GameTime gameTime)
        {
            UpdateRect();
            _sprite.Update(gameTime);

            if (!_sprite.IsAnimating)
                KillMe();

            return base.Update(gameTime);
        }
        public override Node Draw(SpriteBatch batch, GameTime gameTime, int indexLayer)
        {
            if (indexLayer == (int)Layers.FrontFX) 
                _sprite.Draw(batch, AbsXY);

            return base.Draw(batch, gameTime, indexLayer);
        }
    }
}
