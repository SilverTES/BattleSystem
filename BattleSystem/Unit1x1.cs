using Mugen.Core;

namespace BattleSystem
{
    public class Unit1x1 : Card
    {

        public Unit1x1(Arena arena, bool isDropped = true, float tempoBeforeSpawn = 0): base(arena, isDropped, tempoBeforeSpawn)
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
