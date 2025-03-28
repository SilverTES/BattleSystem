using Microsoft.Xna.Framework;
using Mugen.Core;

namespace BattleSystem
{
    public class Shake
    {
        float _intensity;
        float _step;
        public bool IsShake { get; private set; }
        bool _shakeX;
        bool _shakeY;

        public void SetIntensity(float intensity, float step = 2, bool shakeX = true, bool shakeY = true)
        {
            _intensity = intensity;
            _step = step;
            IsShake = true;
            _shakeX = shakeX;
            _shakeY = shakeY;
        }

        public Vector2 GetVector2()
        {
            Vector2 vec = new Vector2();

            if (_intensity > 0)
            {
                IsShake = true;

                vec.X = _shakeX ? Misc.Rng.Next(-(int)_intensity, (int)_intensity) : 0;
                vec.Y = _shakeY ? Misc.Rng.Next(-(int)_intensity, (int)_intensity) : 0;

                _intensity -= _step;

            }
            else
            {
                IsShake = false;
            }

            return vec;
        }
    }
}
