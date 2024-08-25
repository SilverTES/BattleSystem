using System;
using Microsoft.Xna.Framework;
using Mugen.Animation;

namespace BattleSystem
{
    public class Specs
    {
        public int NbAction = 1;
        public int MaxEnergy = 80;
        public int Energy = 80;
        public int Mana = 10;
        public int Speed = 10;
        public int Strength = 10;
        public int PowerAttack = 10;
        public int PowerDefense = 10;
        public int RangeAttack = 10;

        int _damage = 10;

        public bool OffDamage = false;
        
        Animate _animate = new();
        public Specs() 
        { 
            _animate.Add("damage");
        }

        public int SetDamage(int damage = 1) // Return overkill or remaining energy
        {
            _damage = damage;
            
            int prevEnergy = Energy;

            Energy -= _damage;

            if (Energy <= 0) 
            {
                int overKill = Energy;
                Energy = 0;
                return overKill;
            }

            _animate.SetMotion("damage", Easing.QuadraticEaseOut, new Tweening(prevEnergy, Energy, 32));
            _animate.Start("damage");

            return Energy;
        }

        public void Update(GameTime gameTime) 
        {
            OffDamage = false;

            if (_animate.IsPlay())
            {
                Energy = (int)_animate.Value();
            }

            if (_animate.Off("damage"))
            {
                //Console.WriteLine("setdamage finish !");
                OffDamage = true;
            }


            _animate.NextFrame();
        }
    }
}
