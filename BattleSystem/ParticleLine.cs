using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mugen.Core;
using Mugen.GFX;


namespace BattleSystem
{
    public class ParticleLine : Node
    {
        Vector2 _start;
        Vector2 _goal;

        public ParticleLine(Vector2 start, Vector2 goal) 
        {
            _start = start;
            _goal = goal;
        }

        public void UpdateLine(Vector2 start, Vector2 goal)
        {

        }
        public override Node Init()
        {
            return base.Init();
        }

        public override Node Update(GameTime gameTime)
        {
            return base.Update(gameTime);
        }

        public override Node Draw(SpriteBatch batch, GameTime gameTime, int indexLayer)
        {
            if (indexLayer == (int)Layers.FX)
            {
                GFX.Line(batch, _start, _goal, Color.White * .5f, 40f);
            }

            return base.Draw(batch, gameTime, indexLayer);
        }
    }
}
