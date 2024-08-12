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
        protected Stats _stats;

        // Move position
        protected Vector2 _from;
        protected Vector2 _to;
        protected Point _toMap;

        // Tempo Move
        protected int _ticMove;
        protected int _tempoMove;

        protected Arena _arena;

        public List<Point> _path = null;

        protected Point _size = new Point();

        public Point Size { get { return _size; } }

        //protected int _mapX;
        //protected int _mapY;

        protected Point _mapPosition = new();
        public Point MapPosition { get { return _mapPosition; } }

        // Come back to prev map position when drop in case is not possible
        protected bool _backPosition = false;
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

            _stats = new Stats();
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
        public void MoveTo(Point mapPosition, int durationMove = 10)
        {
            if (!_arena.IsInMap(mapPosition))
                return;

            // Test if move is possible by unit size
            for (int i = 0; i < _size.X; i++)
            {
                for (int j = 0; j < _size.Y; j++)
                {
                    var unit = _arena.GetCellUnit(mapPosition + new Point(i, j));
                    if (unit != null)
                    {
                        if (unit._index != _index)
                        return;
                    }
                }
            }

            _from = XY;

            _to.X = mapPosition.X * _cellW;
            _to.Y = mapPosition.Y * _cellH;

            _ticMove = 0;
            _tempoMove = durationMove;
            
            SetState(State.MOVE);
        }
        public void MoveTo(Vector2 goal, int durationMove = 6)
        {
            _from = XY;
            _to = goal;

            _ticMove = 0;
            _tempoMove = durationMove;

            SetState(State.MOVE);
        }

        public Unit SetMapPosition(int mapX, int mapY)
        {
            _mapPosition.X = mapX;
            _mapPosition.Y = mapY;

            UpdatePosition();

            return this;
        }

        public void UpdatePosition()
        {
            _x = _mapPosition.X * _cellW;
            _y = _mapPosition.Y * _cellH;
        }

        public override Node Update(GameTime gameTime)
        {
            UpdateRect();
            _timer.Update();

            _mapPosition.X = (int)((_x+_cellW/2)/_cellW);
            _mapPosition.Y = (int)((_y+_cellH/2)/_cellH);

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
                        if (_timer.OnTimer((int)Timer.Trail))
                            new Trail(AbsRectF.Center, _size.ToVector2(), .025f, Color.WhiteSmoke).AppendTo(_parent);
                        //if (_timer.OnTimer((int)Timer.CheckPath))
                        //{
                        //    Point start = new Point(_prevMapX, _prevMapY);
                        //    Point end = new Point(_mapX, _mapY);

                        //    if (_arena.IsInMap(start) && _arena.IsInMap(end))
                        //        _path = new Astar2DList<Cell>(_arena.GetMap(), start, end, Find.Diagonal, 1, true)._path;
                        //}

                        Arena.CurrentUnitDragged = this;
                        _isDropped = false;

                        // test si l'unit dragué est le même unit dans la case , si oui on enlève l'unit de la case
                        var cellOver = _arena.GetCell(_mapPosition.X, _mapPosition.Y);

                        if (cellOver != null)
                            if (cellOver._unit != null)
                            {
                                if (Arena.CurrentUnitDragged._index == cellOver._unit._index)
                                {
                                    _arena.EraseCellUnit(_mapPosition.X, _mapPosition.Y, cellOver._unit);
                                }
                            }



                        //check if Unit can be dropped
                        _isPossibleToDrop = true;

                        for (int i = 0; i < _size.X; i++)
                        {
                            for (int j = 0; j < _size.Y; j++)
                            {
                                cellOver = _arena.GetCell(_mapPosition.X + i, _mapPosition.Y + j);

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

                        _prevMapX = _mapPosition.X;
                        _prevMapY = _mapPosition.Y;

                        //Console.WriteLine($"On Drag : {_prevPosition} : {_prevMapX}x{_prevMapY}");

                        
                    }

                    _backPosition = false;

                    if (_draggable._offDrag)
                    {
                        //if (_path != null)
                        //    _path.Clear();

                        if (_arena._isMouseOverGrid)
                        {
                            Cell cellOver = null;

                            _isPossibleToDrop = true;

                             // If unit is out of arena isPossibleToDrop is false!
                            if (!_arena._isMouseOverGrid)
                                _isPossibleToDrop = false;

                            // check all cells compose the unit size if another unit occuped the cells
                            for (int i = 0; i < _size.X; i++)
                            {
                                for (int j = 0; j < _size.Y; j++)
                                {
                                    cellOver = _arena.GetCell(_mapPosition.X + i, _mapPosition.Y + j);

                                    if (cellOver == null)
                                    {
                                        //Console.Write("<GET CELL NULL>");
                                        _isPossibleToDrop = false;
                                    }

                                    if (cellOver != null)
                                    {
                                        if (cellOver._unit != null)
                                        {
                                            //Console.WriteLine("Trouve un cell occupé déjà");
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
                                    //SetState(State.MOVE);
                                }
                            }
                            else // Come back to previous position if not possible to drop
                            {
                                if (Misc.PointInRect(_prevPosition, _arena.AbsRectF))
                                {
                                    Vector2 prevPosition = new Vector2(_prevMapX * _cellW, _prevMapY * _cellH);

                                    MoveTo(prevPosition);
                                    _backPosition = true;
                                }
                                else
                                {
                                    MoveTo(_prevPosition);
                                    _backPosition = true;
                                }
                            }

                        }
                        else
                        {
                            if (_isDroppable)
                            {
                                MoveTo(_dropZone._rect.TopLeft - _parent.XY);
                            }
                            else
                            {
                                MoveTo(_prevPosition);
                                _backPosition = true;
                            }
                        }

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

                        _mapPosition.X = (int)((_x + _cellW / 2) / _cellW);
                        _mapPosition.Y = (int)((_y + _cellH / 2) / _cellH);

                        _arena.SetCellUnit(_mapPosition.X, _mapPosition.Y, this);

                        if (_isDroppable)
                        {
                            _isDroppable = false;
                            _isDropped = true;

                            _dropZone._nearNode = this;
                            _dropZone._containedNode = this;
                            //Console.WriteLine("DropZone ContainedNode Affected !");
                            Game1._soundClock.Play(Game1._volumeMaster * .5f, 1f, .5f);
                        }
                        else if (_backPosition)
                        {
                            Game1._soundClock.Play(Game1._volumeMaster * .5f, 1f, .5f);
                            //Console.WriteLine("Back Position");
                        }

                        SetState(State.WAIT);
                    }
                    else
                    {
                        // Efface les traces de l'unit dans l'Arena quand elle bouge toute seule
                        if (!_isDroppable)
                            _arena.EraseCellUnit(_mapPosition.X, _mapPosition.Y, this);
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

                GFX.CenterBorderedStringXY(batch, Game1._fontMain, $"{_stats._energy}", AbsRectF.TopLeft + Vector2.One * 20, Color.GreenYellow, Color.Green);
                GFX.CenterBorderedStringXY(batch, Game1._fontMain, $"{_stats._mana}", AbsRectF.TopRight - Vector2.UnitX * 20 + Vector2.UnitY * 20, Color.MediumSlateBlue, Color.DarkBlue);
                GFX.CenterBorderedStringXY(batch, Game1._fontMain, $"{_stats._powerAttack}", AbsRectF.BottomLeft + Vector2.UnitX * 20 - Vector2.UnitY * 20, Color.Yellow, Color.Red);
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

            if(indexLayer == (int)Layers.BackFX)
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
