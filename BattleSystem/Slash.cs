using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Aseprite;
using Mugen.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleSystem
{
    public class Slash : Node
    {
        AnimatedSprite _spriteSlash;

        public Slash() 
        {
            _spriteSlash = Game1._spriteSheet.CreateAnimatedSprite("slash");
            _spriteSlash.Speed = 1.5f;

            //_spriteSlash.Color = Color.White;
            //var origin = _spriteSlash.CurrentFrame.TextureRegion.GetSlice("Slice1").Origin;
            _spriteSlash.ScaleX = .5f;
            _spriteSlash.ScaleY = .5f;
            _spriteSlash.Origin = new Vector2(_spriteSlash.Width / 2, _spriteSlash.Height / 2);
            //_spriteSlash.Origin = origin;
            _spriteSlash.Play(1);
        }
        public override Node Update(GameTime gameTime)
        {
            UpdateRect();
            _spriteSlash.Update(gameTime);

            return base.Update(gameTime);
        }
        public override Node Draw(SpriteBatch batch, GameTime gameTime, int indexLayer)
        {
            if (indexLayer == (int)Layers.FrontFX) 
                _spriteSlash.Draw(batch, AbsXY);

            return base.Draw(batch, gameTime, indexLayer);
        }
    }
}
