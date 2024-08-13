using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Mugen.Animation;
using Mugen.Core;
using Mugen.GUI;
using Mugen.Physics;
using Newtonsoft.Json.Linq;
using Mugen.GFX;
using Mugen.Input;

namespace BattleSystem.Gui
{
    public class BaseImage
    {
        public float _alpha = 1.0f;
        public Texture2D _texture = null;
        public Rectangle _rect = new Rectangle();
        public Color _color = Color.White;

        public static BaseImage Load(JObject style, string keyName)
        {
            BaseImage image = new BaseImage();

            image._texture = Field.Get<Game1, Texture2D>(Token.Get<string>(style, keyName + ".texture"));
            image._color = Style.ColorValue.GetColor(Token.Get<string>(style, keyName + ".color"));
            image._alpha = Token.Get<float>(style, keyName + ".alpha");

            image._rect.X = Token.Get<int>(style, keyName + ".rect.x");
            image._rect.Y = Token.Get<int>(style, keyName + ".rect.y");
            image._rect.Width = Token.Get<int>(style, keyName + ".rect.width");
            image._rect.Height = Token.Get<int>(style, keyName + ".rect.height");

            return image;
        }
    }

    public class BaseGui : Node
    {
        public enum State
        {
            Default,
            Over,
            Press,
            Count
        }

        protected State _state = State.Default;

        public string _label = "";
        protected SpriteFont _font;
        protected JObject _style;

        protected bool _isShadow = false;
        protected float _shadowAlpha = 0f;
        protected Color _shadowColor = Color.Transparent;
        protected Vector2 _offsetShadow = new Vector2();

        protected Color[] _textColors = new Color[(int)State.Count];
        protected Color[] _bgColors = new Color[(int)State.Count];

        protected BaseImage[] _bgImages = new BaseImage[(int)State.Count];
        protected BaseImage[] _fgImages = new BaseImage[(int)State.Count];

        protected int _curAnimateIndex = 0;
        protected List<string> _listAnimates = new List<string>();

        protected RectangleF _shape;

        protected Animate _animate = new();
        protected AnimateVec2 _animateVec2 = new();

        protected MouseControl _mouseControl;

        public void SetState(State state)
        {
            _state = state;
            //Console.WriteLine(_state);
        }
        public BaseGui(MouseControl mouseControl, string label, JObject style)
        {
            _mouseControl = mouseControl;
            _label = label;
            _type = UID.Get<BaseGui>();
            SetStyle(style);
        }
        public override Node Init()
        {

            return base.Init();
        }
        public virtual Node SetStyle(JObject style)
        {
            _style = style;

            for (int i = 0; i < (int)State.Count; i++)
            {
                _bgImages[i] = BaseImage.Load(style, "bgImage." + (State)i);
                _fgImages[i] = BaseImage.Load(style, "fgImage." + (State)i);

                _textColors[i] = Style.ColorValue.GetColor(Token.Get<string>(style, "textColor." + (State)i));
                _bgColors[i] = Style.ColorValue.GetColor(Token.Get<string>(style, "bgColor." + (State)i));
            }

            _isShadow = Token.Get<bool>(style, "shadow.visible");
            _shadowAlpha = Token.Get<float>(style, "shadow.alpha");
            _shadowColor = Style.ColorValue.GetColor(Token.Get<string>(style, "shadow.color"));
            _offsetShadow.X = Token.Get<float>(style, "shadow.offset.x");
            _offsetShadow.Y = Token.Get<float>(style, "shadow.offset.y");

            _rect.Width = Token.Get<int>(style, "shape.width");
            _rect.Height = Token.Get<int>(style, "shape.height");


            SetPivot((Position)Enum.Parse(typeof(Position), Token.Get<string>(style, "shape.pivot")));

            SetVisible(Token.Get<bool>(style, "visible"));

            _font = Field.Get<Game1, SpriteFont>(Token.Get<string>(style, "font"));

            // if animates exist
            if (Token.Exist(style, "animateVec2"))
            {
                var animates = Token.Get<JArray>(style, "animateVec2");

                int i = 0;
                foreach (var anim in animates)
                {
                    string animate = $"animateVec2[{i}]";
                    if (Token.Exist(style, animate))
                    {
                        string name = Token.Get<string>(style, animate + ".name");
                        string easing = Token.Get<string>(style, animate + ".easing");
                        float duration = Token.Get<float>(style, animate + ".duration");

                        Vector2 from = new Vector2();
                        Vector2 to = new Vector2();

                        from.X = Token.Get<float>(style, animate + ".from.x");
                        from.Y = Token.Get<float>(style, animate + ".from.y");
                        to.X = Token.Get<float>(style, animate + ".to.x");
                        to.Y = Token.Get<float>(style, animate + ".to.y");

                        var method = typeof(Easing).GetMethod(easing);

                        bool isStart = Token.Get<bool>(style, animate + ".start");

                        
                        _animateVec2.Add(name, Easing.Linear, new TweeningVec2(from, to, duration));

                        if (isStart)
                            _animateVec2.Start(name);
                    }
                    i++;
                }
            }

            // animate order
            if (Token.Exist(style, "animateOrder"))
            {
                var animateOrder = Token.Get<JArray>(style, "animateOrder");

                int i = 0;
                foreach (var animate in animateOrder)
                {
                    string animateName = $"animateOrder[{i}]";
                    if (Token.Exist(style, animateName))
                    {
                        string name = Token.Get<string>(style, animateName);
                        _listAnimates.Add(name);
                    }
                    i++;
                }
            }
            return this;
        }
        public override Node Update(GameTime gameTime)
        {
            UpdateRect();

            _shape = new RectangleF((int)(AbsX - _oX + _animateVec2.Value().X), (int)(AbsY - _oY + _animateVec2.Value().Y), (int)_rect.Width, (int)_rect.Height);

            //_navi._isPress = false
            _navi._onMouseOut = false;
            _navi._onPress = false;
            _navi._onRelease = false;
            _navi._onClick = false;

            _navi._isMouseOver = Collision2D.PointInRect(new Vector2(_mouseControl._x, _mouseControl._y), _rect);

            if (_navi._isMouseOver)
                SetState(State.Over);

            if (_navi._isMouseOver && _navi._isMouseOut)
            {
                Game1._soundClock.Play(.05f, .8f, 0f);
                _navi._onMouseOut = true;
                _navi._isMouseOut = false;
            }
            if (!_navi._isMouseOver)
            {
                _navi._isMouseOut = true;
                _navi._isPress = false;
                _navi._isRelease = false;
                SetState(State.Default);
            }

            if (_navi._isMouseOver && _mouseControl._onClick)
            {
                _navi._onClick = true;
            }

            if (_navi._isMouseOver && Game1._mouseState.LeftButton == ButtonState.Released && _navi._isPress)
            {

                if (!_navi._isRelease)
                {
                    _navi._onRelease = true;
                    //Game1._soundClock.Play(.1f, .1f, 0f);
                    //Console.WriteLine("On RELEASE !!");
                }

                _navi._isPress = false;
                _navi._isRelease = true;
            }


            if (_navi._isMouseOver && Game1._mouseState.LeftButton == ButtonState.Pressed)
            {

                if (!_navi._isPress)
                {
                    _navi._onPress = true;
                    Game1._soundClock.Play(.1f, .1f, 0f);
                    //Console.WriteLine("On PRESS !!");
                }

                _navi._isPress = true;
                _navi._isRelease = false;

                SetState(State.Press);
            }

            var animates = _animateVec2.GetAll();

            if (animates.Count > 0)
            {
                var curAnimateName = _listAnimates[_curAnimateIndex];

                if (_animateVec2.Off(curAnimateName))
                {
                    _curAnimateIndex++;

                    if (_curAnimateIndex > _listAnimates.Count - 1)
                        _curAnimateIndex = 0;

                    curAnimateName = _listAnimates[_curAnimateIndex];

                    _animateVec2.Start(curAnimateName);
                }

                _animateVec2.NextFrame();
            }


            return base.Update(gameTime);
        }

        protected virtual Node RenderImage(SpriteBatch batch, BaseImage[] images)
        {
            BaseImage image;

            if (_navi._isMouseOver)
            {
                image = images[(int)State.Over];

                if (image == null)
                    GFX.FillRectangle(batch, AbsRect, _bgColors[(int)State.Over]);
            }
            else
            {
                image = images[(int)State.Default];

                if (image == null)
                    GFX.FillRectangle(batch, AbsRect, _bgColors[(int)State.Default]);
            }

            if (_navi._isPress)
            {
                image = images[(int)State.Press];

                if (image == null)
                    GFX.FillRectangle(batch, AbsRect, _bgColors[(int)State.Press]);
            }

            if (image._texture != null)
            {
                if (_isShadow)
                    //batch.Draw(image._texture, _shape.TopLeft, image._rect, _shadowColor * _shadowAlpha);
                    batch.Draw(image._texture, (Rectangle)_shape, image._rect, _shadowColor * _shadowAlpha);

                //batch.Draw(image._texture, _shape.TopLeft, image._rect, image._color * image._alpha);
                batch.Draw(image._texture, (Rectangle)_shape, image._rect, image._color * image._alpha);
            }

            return this;
        }

        public override Node Draw(SpriteBatch batch, GameTime gameTime, int indexLayer)
        {
            if (indexLayer == (int)Layers.Gui)
            {
                RenderImage(batch, _bgImages);
                if (_font != null)
                {
                    GFX.CenterStringXY(batch, _font, _label, AbsX + _animateVec2.Value().X, AbsY + 2 + _animateVec2.Value().Y, _textColors[(int)State.Default]);
                }

            }

            if (indexLayer == (int)Layers.FrontFX)
            {
                BaseImage fgImage = null;

                if (_navi._isMouseOver)
                {
                    fgImage = _fgImages[(int)State.Over];
                }
                else
                {
                }

                if (_navi._isPress)
                {
                    fgImage = _fgImages[(int)State.Press];
                }

                if (fgImage != null)
                    if (fgImage._texture != null)
                        //batch.Draw(fgImage._texture, _shape.TopLeft, fgImage._rect, fgImage._color * fgImage._alpha);
                        batch.Draw(fgImage._texture, (Rectangle)_shape, fgImage._rect, fgImage._color * fgImage._alpha);
            }

            return base.Draw(batch, gameTime, indexLayer);
        }

    }


    public class Button : BaseGui
    {
        public Button(MouseControl mouseControl, string label, JObject style) : base(mouseControl, label, style)
        {
            _subType = UID.Get<Button>();

        }

        public override Node SetStyle(JObject style)
        {

            return base.SetStyle(style);
        }

    }
    public class CheckBox : BaseGui
    {
        protected bool _isChecked = false;
        protected BaseImage[] _checkedImages = new BaseImage[(int)State.Count];
        public CheckBox(MouseControl mouseControl, string label, JObject style) : base(mouseControl, label, style)
        {
            _subType = UID.Get<CheckBox>();
        }

        public override Node SetStyle(JObject style)
        {
            for (int i = 0; i < (int)State.Count; i++)
            {
                _checkedImages[i] = BaseImage.Load(style, "checkedImage." + (State)i);
            }

            return base.SetStyle(style);
        }
        public void SetChecked(bool isChecked = true)
        {
            _isChecked = isChecked;
        }
        public override Node Update(GameTime gameTime)
        {
            if (_navi._onRelease)
            {
                _isChecked = !_isChecked;
            }

            return base.Update(gameTime);
        }

        public override Node Draw(SpriteBatch batch, GameTime gameTime, int indexLayer)
        {
            if (indexLayer == (int)Layers.Gui)
            {
                if (_isChecked)
                    RenderImage(batch, _checkedImages);
                else
                    base.Draw(batch, gameTime, indexLayer);
            }

            return this;
        }

    }
}
