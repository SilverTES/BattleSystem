using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mugen.Animation;
using Mugen.Core;
using Mugen.GFX;
using Mugen.Input;
using Mugen.Physics;


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

        public Arena _arena;

        int _mapGoalX;
        int _mapGoalY;

        int _sizeW;
        int _sizeH;

        public int _mapX;
        public int _mapY;
        
        // Come back to prev map position when drop in case is not possible
        int _prevMapX;
        int _prevMapY;
        Vector2 _prevPosition = new();

        int _cellW;
        int _cellH;

        MouseControl _mouse;
        
        public Addon.Draggable _draggable;

        public bool _isDroppable = false;
        public bool _isDropped = false;

        public DropZone _dropZone;

        public Unit(MouseControl mouse, Arena arena, int sizeW, int sizeH, int cellW, int cellH) 
        {
            _type = UID.Get<Unit>();
            _mouse = mouse;
            _arena = arena;
            _sizeW = sizeW;
            _sizeH = sizeH;
            _cellW = cellW;
            _cellH = cellH;

            SetSize(_sizeW * _cellW, _sizeH * _cellH);

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
        //public void MoveTo(int mapGoalX, int mapGoalY, int durationMove = 10)
        //{
        //    _mapGoalX = mapGoalX;
        //    _mapGoalY = mapGoalY;

        //    _ticMove = 0;
        //    _tempoMove = durationMove;
        //}

        public void MoveTo(Vector2 goal, int durationMove = 10)
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

            _mapX = (int)((_x+_cellW/2)/_cellW);
            _mapY = (int)((_y+_cellH/2)/_cellH);

            switch (_state)
            {
                case State.NONE:
                    break;
                case State.WAIT:

                    _draggable.SetDraggable(true);

                    if (_navi._isMouseOver && _mouse._onClick && !_mouse._isOverAny && !_mouse._isActiveReSize)
                    {
                        _parent.GotoFront(_index);
                    }

                    if (_draggable._isDragged)
                    {
                        Arena.CurrentDragged = this;
                        _isDropped = false;

                        // test si l'unit dragué est le même unit dans la case , si oui on enlève l'unit de la case
                        var cellOver = _arena.GetCell(_mapX, _mapY);
                        if (cellOver != null)
                            if (cellOver._unit != null)
                            {
                                if (Arena.CurrentDragged._index == cellOver._unit._index)
                                    _arena.SetCellUnit(_mapX, _mapY, null);
                            }

                    }
                    
                    if (_draggable._onDrag)
                    {
                        _prevPosition = XY;

                        _prevMapX = _mapX;
                        _prevMapY = _mapY;
                    }
                    
                    if (_draggable._offDrag)
                    {
                        //Console.Write("< offDrag >");


                        if (_arena._isMouseOver)
                        {
                            bool isPossibleToDrop = false;
                            var cellOver = _arena.GetCell(_mapX, _mapY);
                            if (cellOver != null)
                                if (_isDroppable && cellOver._unit == null )
                                {
                                    MoveTo(_dropZone._rect.TopLeft - _parent.XY);
                                    SetState(State.MOVE);
                                    isPossibleToDrop = true;
                                }

                            // Come back to previous position if not possible to drop
                            if (!isPossibleToDrop)
                            {
                                if (Misc.PointInRect(_prevPosition, _arena.AbsRectF))
                                {
                                    Vector2 prevPosition = new Vector2(_prevMapX * _cellW, _prevMapY * _cellH);

                                    MoveTo( prevPosition);
                                    SetState(State.MOVE);
                                }
                                else
                                {
                                    MoveTo(_prevPosition);
                                    SetState(State.MOVE);
                                }
                            }
                        }
                        else
                        {
                            if (_isDroppable)
                            {
                                MoveTo(_dropZone._rect.TopLeft - _parent.XY);
                                SetState(State.MOVE);
                                //isPossibleToDrop = true;
                            }
                            else
                            {
                                MoveTo(_prevPosition);
                                SetState(State.MOVE);
                            }
                        }




                    }

                    break;
                case State.MOVE:

                    _draggable.SetDraggable(false);

                    _x = Easing.GetValue(Easing.QuarticEaseOut, _ticMove, _from.X, _to.X, _tempoMove); // QuadraticEaseOut au lieu de Q***InOut pour eviter bug de détection de la dropZone _isNear car le mouvement est trop rapide a la fin
                    _y = Easing.GetValue(Easing.QuarticEaseOut, _ticMove, _from.Y, _to.Y, _tempoMove);

                    _ticMove++;
                    if (_ticMove >= _tempoMove)
                    {
                        _x = _to.X;
                        _y = _to.Y;

                        _mapX = (int)((_x + _cellW / 2) / _cellW);
                        _mapY = (int)((_y + _cellH / 2) / _cellH);

                        _arena.SetCellUnit(_mapX, _mapY, this);

                        //if (_isDroppable)
                        {
                            _isDroppable = false;
                            _isDropped = true;

                            _dropZone._nearNode = this;
                            _dropZone._containedNode = this;
                            //Console.WriteLine("DropZone ContainedNode Affected !");
                        }

                        SetState(State.WAIT);
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
                GFX.FillRectangle(batch, AbsRectF.Extend(-4), Color.Black * .8f);

                if (_draggable._isDragged)
                    GFX.Rectangle(batch, AbsRect, Color.Orange * .5f, 2f);

                batch.Draw(Game1._texAvatar1x1, AbsXY, Color.White);

                //if (_isDroppable)
                //    GFX.Rectangle(batch, AbsRect, Color.Red * .5f, 2f);
                //batch.Draw(Game1._texAvatar1x1, AbsXY, Color.Yellow);
            }

            if (indexLayer == (int)Layers.Debug)
            {
                //GFX.CenterStringXY(batch, Game1._fontMain, $"{_mapX}:{_mapY}", AbsRectF.TopCenter, Color.Yellow);
            }

            if(indexLayer == (int)Layers.FX)
            {
                if (_state == State.MOVE) 
                {
                    float alpha = _tempoMove/(float)(_ticMove*5+.01f);

                    GFX.Line(batch, _from + _parent.XY + new Vector2(_cellW/2, _cellH/2), AbsXY + new Vector2(_cellW / 2, _cellH / 2), Color.White * alpha, _cellW/2);
                    GFX.Line(batch, _from + _parent.XY + new Vector2(_cellW/2, _cellH/2), AbsXY + new Vector2(_cellW / 2, _cellH / 2), Color.White * alpha, _cellW/3);
                    GFX.Line(batch, _from + _parent.XY + new Vector2(_cellW/2, _cellH/2), AbsXY + new Vector2(_cellW / 2, _cellH / 2), Color.White * alpha, _cellW/4);
                    GFX.Line(batch, _from + _parent.XY + new Vector2(_cellW/2, _cellH/2), AbsXY + new Vector2(_cellW / 2, _cellH / 2), Color.White * alpha, _cellW/5);

                }
            }

            return base.Draw(batch, gameTime, indexLayer);
        }

    }
}
