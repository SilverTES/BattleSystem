using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mugen.Animation;
using Mugen.Core;
using Mugen.GFX;
using Mugen.Input;


namespace BattleSystem
{
    public class Unit : Node
    {
        public enum State
        {
            NONE = -1,
            WAIT,
            MOVE,
            ATTACK,
            DAMAGE,
            LAST
        }

        State _state = State.WAIT;

        // Move position
        public Vector2 _from;
        public Vector2 _to;
        public Point _toMap;

        // Tempo Move
        public int _ticMove;
        public int _tempoMove;

        Arena _arena;

        int _mapGoalX;
        int _mapGoalY;

        int _mapW;
        int _mapH;

        int _mapX;
        int _mapY;

        int _cellW;
        int _cellH;

        MouseControl _mouse;
        
        public Addon.Draggable _draggable;

        public bool _isDroppable = false;
        public bool _isDropped = false;

        public DropZone _dropZone;

        public Unit(MouseControl mouse, Arena arena, int mapW, int mapH, int cellW, int cellH) 
        {
            _type = UID.Get<Unit>();
            _mouse = mouse;
            _arena = arena;
            _mapW = mapW;
            _mapH = mapH;
            _cellW = cellW;
            _cellH = cellH;

            SetSize(_mapW * _cellW, _mapH * _cellH);

            _draggable = new Addon.Draggable(this, _mouse);
            _draggable.SetDragRectNode(true);
            _draggable.SetDraggable(true);

            AddAddon(_draggable);
        }

        public override Node Init()
        {
            return base.Init();
        }
        public void SetState(State state)
        {
            _state = state;
        }
        public void MoveTo(int mapGoalX, int mapGoalY, int durationMove = 8)
        {
            _mapGoalX = mapGoalX;
            _mapGoalY = mapGoalY;

            _tempoMove = durationMove;
        }

        public void MoveTo(Vector2 goal, int durationMove = 8)
        {
            _from = XY;
            _to = goal;

            _ticMove = 0;
            _tempoMove = durationMove;
        }

        public Unit SetMapPosition(int mapX, int mapY)
        {
            _mapX = mapX;
            _mapY = mapY;

            UpdatePosition();

            return this;
        }

        public void UpdatePosition()
        {
            _x = _mapX * _cellW;
            _y = _mapY * _cellH;
        }

        public override Node Update(GameTime gameTime)
        {
            UpdateRect();

            switch (_state)
            {
                case State.NONE:
                    break;
                case State.WAIT:
                    if (_navi._isMouseOver && _mouse._onClick && !_mouse._isOverAny && !_mouse._isActiveReSize)
                    {
                        _parent.GotoFront(_index);
                    }

                    if (_draggable._isDragged)
                    {
                        Arena.CurrentDragged = this;
                        _isDropped = false;

                    }
                    else
                    {
                        if (_isDroppable)
                        {
                            MoveTo(_dropZone._rect.TopLeft - _parent.XY);
                            SetState(State.MOVE);
                        }
                    }

                    break;
                case State.MOVE:

                    _x = Easing.GetValue(Easing.QuadraticEaseInOut, _ticMove, _from.X, _to.X, _tempoMove);
                    _y = Easing.GetValue(Easing.QuadraticEaseInOut, _ticMove, _from.Y, _to.Y, _tempoMove);

                    _ticMove++;
                    if (_ticMove >= _tempoMove)
                    {
                        _x = _to.X;
                        _y = _to.Y;

                        //OnGoal = true;
                        //IsGoal = true;

                        SetState(State.WAIT);

                        _mapX = _toMap.X;
                        _mapY = _toMap.Y;

                        //_ticAction = 0;
                        //_directionX = 0;

                        if (_isDroppable)
                        {
                            _isDroppable = false;
                            _isDropped = true;

                            _dropZone._nearNode = this;
                            _dropZone._containedNode = this;
                        }
                    }
                    break;
                case State.ATTACK:
                    break;
                case State.DAMAGE:
                    break;
                case State.LAST:
                    break;
                default:
                    break;
            }

            return base.Update(gameTime);
        }
        public override Node Draw(SpriteBatch batch, GameTime gameTime, int indexLayer)
        {
            if (indexLayer == (int)Layers.Main)
            {
                GFX.FillRectangle(batch, AbsRect, Color.Black * .5f);
                
                if (_draggable._isDragged)
                    GFX.Rectangle(batch, AbsRect, Color.Orange * .5f, 2f);

                batch.Draw(Game1._texAvatar1x1, AbsXY, Color.White);

                if (_isDroppable)
                    GFX.Rectangle(batch, AbsRect, Color.Red * .5f, 2f);
                    //batch.Draw(Game1._texAvatar1x1, AbsXY, Color.Yellow);
            }

            return base.Draw(batch, gameTime, indexLayer);
        }

    }
}
