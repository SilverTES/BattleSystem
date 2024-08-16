using System;
using Microsoft.Xna.Framework;
using Mugen.Animation;

namespace BattleSystem
{
    public class Stats
    {
        internal int _nbAction = 1;
        internal int _maxEnergy = 80;
        internal int _energy = 80;
        internal int _mana = 10;
        internal int _speed = 10;
        internal int _strength = 10;
        internal int _powerAttack = 10;
        internal int _powerDefense = 10;
        internal int _rangeAttack = 10;

        internal int _damage = 10;
        
        Animate _animate = new();
        public Stats() 
        { 
            _animate.Add("damage");
        }

        public void SetDamage(int damage = 1)
        {
            _damage = damage;
            
            int prevEnergy = _energy;

            _energy -= _damage;

            if (_energy <= 0) 
            { 
                _energy = 0;
                return;
            }

            _animate.SetMotion("damage", Easing.QuadraticEaseOut, new Tweening(prevEnergy, _energy, 32));
            _animate.Start("damage");
        }

        public void Update(GameTime gameTime) 
        {
            if (_animate.IsPlay())
            {
                _energy = (int)_animate.Value();
            }

            if (_animate.Off("damage"))
            {
                //Console.WriteLine("setdamage finish !");
            }


            _animate.NextFrame();
        }
    }
}
