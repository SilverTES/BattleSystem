using System;
using System.Collections.Generic;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mugen.AI;
using Mugen.Animation;
using Mugen.Core;
using Mugen.Event;
using Mugen.GFX;
using Mugen.Input;
using Mugen.Map2D;
using Mugen.Physics;


namespace BattleSystem
{
    public class Unit : Node
    {
        enum Timer
        {
            Trail,
            CheckPath,
            Count
        }
        TimerEvent _timer;
        public enum State
        {
            NONE = -1,
            WAIT,
            MOVE,
            ATTACK,
            DAMAGE,
            LAST
        }

        protected State _state = State.WAIT;

        // Move position
        protected Vector2 _from;
        protected Vector2 _to;
        protected Point _toMap;

        // Tempo Move
        protected int _ticMove;
        protected int _tempoMove;

        protected Arena _arena;

        int _mapGoalX;
        int _mapGoalY;

        public List<Point> _path = null;

        protected Point _size = new Point();

        public Point Size { get { return _size; } }

        protected int _mapX;
        protected int _mapY;

        // Come back to prev map position when drop in case is not possible
        protected int _prevMapX;
        protected int _prevMapY;
        protected Vector2 _prevPosition = new();
        public Vector2 PrevPosition { get { return _prevPosition; } }


        protected int _cellW;
        protected int _cellH;

        protected MouseControl _mouse;

        protected Addon.Draggable _draggable;

        public bool _isDroppable = false;
        public bool _isDropped = false;
        public DropZone _dropZone;
        protected bool _isPossibleToDrop = false;
        public bool IsPossibleToDrop { get { return _isPossibleToDrop; } }

        Addon.Loop _loop;

        public Unit(MouseControl mouse, Arena arena, int sizeW, int sizeH, int cellW, int cellH) 
        {
            _type = UID.Get<Unit>();
            _mouse = mouse;
            _arena = arena;
            _size.X = sizeW;
            _size.Y = sizeH;
            _cellW = cellW;
            _cellH = cellH;

            SetSize(_size.X * _cellW, _size.Y * _cellH);

            _draggable = new Addon.Draggable(this, _mouse);
            _draggable.SetDragRectNode(true);
            _draggable.SetDraggable(true);

            AddAddon(_draggable);

            _timer = new TimerEvent((int)Timer.Count);
            
            _timer.SetTimer((int)Timer.Trail, TimerEvent.Time(0, 0, .001f));
            _timer.StartTimer((int)Timer.Trail);

            _timer.SetTimer((int)Timer.CheckPath, TimerEvent.Time(0, 0, .02f));
            _timer.StartTimer((int)Timer.CheckPath);

            _loop = new Addon.Loop(this);
            _loop.SetLoop(0, -Geo.RAD_225 * .005f, Geo.RAD_225 *.005f, .001f, Loops.PINGPONG);
            _loop.Start();
            AddAddon(_loop);
        }

        public override Node Init()
        {
            return base.Init();
        }
        public Vector2 GetCellSize()
        {
            return new Vector2(_cellW, _cellH);
        }
        public void SetState(State state)
        {
            _state = state;
        }
        public void MoveTo(int mapGoalX, int mapGoalY, int durationMove = 10)
        {
            _mapGoalX = mapGoalX;
            _mapGoalY = mapGoalY;

            _ticMove = 0;
            _tempoMove = durationMove;
        }
        public void MoveTo(Vector2 goal, int durationMove = 6)
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
            _timer.Update();

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

                        //if (_timer.OnTimer((int)Timer.CheckPath))
                        //{
                        //    Point start = new Point(_prevMapX, _prevMapY);
                        //    Point end = new Point(_mapX, _mapY);

                        //    if (_arena.IsInMap(start) && _arena.IsInMap(end))
                        //        _path = new Astar2DList<Cell>(_arena.GetMap(), start, end, Find.Diagonal, 1, true)._path;
                        //}

                        Arena.CurrentDragged = this;
                        _isDropped = false;

                        // test si l'unit dragué est le même unit dans la case , si oui on enlève l'unit de la case
                        var cellOver = _arena.GetCell(_mapX, _mapY);

                        if (cellOver != null)
                            if (cellOver._unit != null)
                            {
                                if (Arena.CurrentDragged._index == cellOver._unit._index)
                                {
                                    _arena.EraseCellUnit(_mapX, _mapY, cellOver._unit);
                                }
                            }

                        if (_timer.OnTimer((int)Timer.Trail))
                            new Trail(AbsRectF.Center, _size.ToVector2(), .025f, Color.WhiteSmoke).AppendTo(_parent);


                        //check if Unit can be dropped
                        _isPossibleToDrop = true;

                        for (int i = 0; i < _size.X; i++)
                        {
                            for (int j = 0; j < _size.Y; j++)
                            {
                                cellOver = _arena.GetCell(_mapX + i, _mapY + j);

                                if (cellOver == null)
                                {
                                    _isPossibleToDrop = false;
                                }

                                if (cellOver != null)
                                {
                                    if (cellOver._unit != null)
                                    {
                                        _isPossibleToDrop = false;
                                    }
                                }
                            }
                        }

                    }
                    
                    if (_draggable._onDrag)
                    {
                        _prevPosition = XY;

                        _prevMapX = _mapX;
                        _prevMapY = _mapY;

                        Console.WriteLine($"On Drag : {_prevPosition} : {_prevMapX}x{_prevMapY}");
                    }

                    if (_draggable._offDrag)
                    {
                        //if (_path != null)
                        //    _path.Clear();

                        //if (_arena._isMouseOver)
                        {
                            Cell cellOver = null;

                            _isPossibleToDrop = true;

                             // If unit is out of arena isPossibleToDrop is false!
                            if (!_arena._isMouseOver)
                                _isPossibleToDrop = false;

                            // check all cells compose the unit size if another unit occuped the cells
                            for (int i = 0; i < _size.X; i++)
                            {
                                for (int j = 0; j < _size.Y; j++)
                                {
                                    cellOver = _arena.GetCell(_mapX + i, _mapY + j);

                                    if (cellOver == null)
                                    {
                                        Console.Write("<GET CELL NULL>");
                                        _isPossibleToDrop = false;
                                    }

                                    if (cellOver != null)
                                    {
                                        if (cellOver._unit != null)
                                        {
                                            Console.WriteLine("Trouve un cell occupé déjà");
                                            _isPossibleToDrop = false;
                                        }
                                    }
                                }
                            }


                            if (_isPossibleToDrop) // Unit move to goal when isPossibleToDrop is true
                            {
                                if (_isDroppable && cellOver._unit == null )
                                {
                                    MoveTo(_dropZone._rect.TopLeft - _parent.XY);
                                    SetState(State.MOVE);
                                }
                            }
                            else // Come back to previous position if not possible to drop
                            {
                                if (Misc.PointInRect(_prevPosition, _arena.AbsRectF))
                                {
                                    Vector2 prevPosition = new Vector2(_prevMapX * _cellW, _prevMapY * _cellH);

                                    MoveTo(prevPosition);
                                    SetState(State.MOVE);
                                }
                                else
                                {
                                    MoveTo(_prevPosition);
                                    SetState(State.MOVE);
                                }
                            }

                        }
                        //else
                        //{
                        //    if (_isDroppable)
                        //    {
                        //        MoveTo(_dropZone._rect.TopLeft - _parent.XY);
                        //        SetState(State.MOVE);
                        //    }
                        //    else
                        //    {
                        //        MoveTo(_prevPosition);
                        //        SetState(State.MOVE);
                        //    }
                        //}




                    }

                    break;
                case State.MOVE:

                    if (_timer.OnTimer((int)Timer.Trail))
                        new Trail(AbsRectF.Center, _size.ToVector2(), .025f, Color.WhiteSmoke).AppendTo(_parent);

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
                {
                    GFX.Rectangle(batch, AbsRectF.Extend(2), Color.Orange * .5f, 2f);
                }

                Texture2D tex = Game1._texAvatar1x1;

                if (_size.X == 2 && _size.Y == 2)
                    tex = Game1._texAvatar2x2;

                //batch.Draw(tex, AbsXY, Color.White);

                GFX.Draw(batch, tex, Color.White, _loop._current, AbsXY + tex.Bounds.Size.ToVector2()/2, Position.CENTER, Vector2.One);


                //if (_isDroppable)
                //    GFX.Rectangle(batch, AbsRect, Color.Red * .5f, 2f);
                //batch.Draw(Game1._texAvatar1x1, AbsXY, Color.Yellow);
            }

            if (indexLayer == (int)Layers.Debug)
            {
                //GFX.CenterStringXY(batch, Game1._fontMain, $"{_mapX}:{_mapY}\n{_isPossibleToDrop}", AbsRectF.TopCenter, Color.Yellow);

                //if (_path != null)
                //    if (_path.Count > 0)
                //    {
                //        for (int i = 1; i < _path.Count; i++)
                //        {
                //            Vector2 p1 = _path[i - 1].ToVector2() * GetCellSize() + GetCellSize() / 2;
                //            Vector2 p2 = _path[i].ToVector2() * GetCellSize() + GetCellSize() / 2;

                //            if (i == 1)
                //                GFX.Point(batch, p1 + _arena.AbsXY, 8f, Color.White);

                //            GFX.Line(batch, p1 + _arena.AbsXY, p2 + _arena.AbsXY, Color.White * .25f, 4f);
                //            GFX.Point(batch, p2 + _arena.AbsXY, 10f, Color.White * 1f);
                //        }
                //    }

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
