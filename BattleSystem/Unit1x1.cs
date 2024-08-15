

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Mugen.Core;
using Mugen.Input;

namespace BattleSystem
{
    public class Unit1x1 : Card
    {
        public Unit1x1(Arena arena): base(arena)
        {
            _size.X = 1;
            _size.Y = 1;
            _subType = UID.Get<Unit1x1>();
        }
        //public override Node Init()
        //{
        //    return base.Init();
        //}
        //public override Node Update(GameTime gameTime)
        //{
        //    return base.Update(gameTime);
        //}
        //public override Node Draw(SpriteBatch batch, GameTime gameTime, int indexLayer)
        //{
        //    return base.Draw(batch, gameTime, indexLayer);
        //}


    }
}
