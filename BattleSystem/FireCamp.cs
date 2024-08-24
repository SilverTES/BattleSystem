using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using MonoGame.Aseprite;
using Mugen.Core;


namespace BattleSystem
{
    public class FireCamp : Node
    {
        AnimatedSprite _sprite;

        public FireCamp()
        {
            _sprite = Game1._spriteSheetFireCamp.CreateAnimatedSprite("FireCamp");
            _sprite.Speed = 1.5f;

            //_spriteSlash.Color = Color.White;
            var origin = _sprite.CurrentFrame.TextureRegion.GetSlice("Slice1").Origin;
            _sprite.ScaleX = 2f;
            _sprite.ScaleY = 2f;
            //_sprite.Origin = new Vector2(_sprite.Width / 2, _sprite.Height / 2);
            _sprite.Origin = origin;
            _sprite.Play(0);
            _sprite.Color = Color.White * .95f;
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
                //_sprite.Draw(batch, AbsXY);
                batch.Draw(_sprite, AbsXY);

            return base.Draw(batch, gameTime, indexLayer);
        }
    }
}
