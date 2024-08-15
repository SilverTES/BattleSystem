using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Mugen.AI;
using Mugen.Animation;
using Mugen.Core;
using Mugen.Event;
using Mugen.GFX;
using Mugen.Input;
using Mugen.Physics;


namespace BattleSystem
{
    public class Unit : Node
    {
        enum Timer
        {
            Trail,
            //CheckPath,
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
        public bool Is(State state) { return _state == state; }
        // Statistic of the Unit
        protected State _state = State.WAIT;
        protected Stats _stats;

        // Move position
        protected Vector2 _from;
        protected Vector2 _to;
        protected Point _toMap;

        // Tempo Move
        protected int _ticMove;
        protected int _tempoMove;

        // Dependencies
        protected Arena _arena;

        //public List<List<Point>> _paths = new();
        //protected bool _isCanMove = true;

        protected Point _size = new Point();
        public Point Size { get { return _size; } }
        protected Point _mapPosition = new();
        public Point MapPosition { get { return _mapPosition; } }

        // Come back to prev map position when drop in case is not possible
        protected bool _backToPrevPosition = false;
        //protected int _prevMapX;
        //protected int _prevMapY;
        protected Point _prevMapPosition = new();
        protected Vector2 _prevPosition = new();
        public Vector2 PrevPosition { get { return _prevPosition; } }

        protected List<Point> _attackPoints = new List<Point>();

        protected int _cellW;
        protected int _cellH;

        protected MouseControl _mouse;

        protected Addon.Draggable _draggable;

        public bool _isDroppable = false;
        public bool _isDropped = true;
        public DropZone _dropZone;
        protected bool _isPossibleToDrop = false;
        public bool IsPossibleToDrop { get { return _isPossibleToDrop; } }

        Addon.Loop _loop;
        Shake _shake;

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

            //_timer.SetTimer((int)Timer.CheckPath, TimerEvent.Time(0, 0, .02f));
            //_timer.StartTimer((int)Timer.CheckPath);

            float angleDelta = .005f;

            _loop = new Addon.Loop(this);
            _loop.SetLoop(0, -Geo.RAD_225 * angleDelta, Geo.RAD_225 * angleDelta, .001f, Loops.PINGPONG);
            _loop.Start();
            AddAddon(_loop);

            _shake = new();

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
        public bool MoveToStep(Point mapStep, int durationMove = 6) // true if move possible
        {
            if (!_arena.IsUnitInMap(this, mapStep) || _state == State.MOVE)
                return false;

            Point mapDestPosition = _mapPosition + mapStep;

            // Test if move is possible by unit size
            for (int i = 0; i < _size.X; i++)
            {
                for (int j = 0; j < _size.Y; j++)
                {
                    var unit = _arena.GetCellUnit(mapDestPosition + new Point(i, j));
                    if (unit != null)
                    {
                        if (unit._index != _index)
                        return false;
                    }
                }
            }

            _arena.EraseCellUnit(this);
            _arena.SetCellUnit(mapDestPosition.X, mapDestPosition.Y, this);

            _from = XY;

            _to.X = mapDestPosition.X * _cellW;
            _to.Y = mapDestPosition.Y * _cellH;

            _ticMove = 0;
            _tempoMove = durationMove;

            SetState(State.MOVE);

            return true;
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

        public void AttackUnit(int damage, float intensity = 10f)
        {
            _shake.SetIntensity(intensity, .25f);
            _stats.SetDamage(damage);

            new PopInfo("-" + damage, Color.Yellow, Color.Red, 0, 24, 24)
                .SetPosition(_rect.Center)
                .AppendTo(_parent);

            SetState(State.DAMAGE);
            Game1._soundWoodHit.Play(.25f, 1f, 0f);
        }
        public void DestroyMe()
        {
            _arena.EraseCellUnit(_mapPosition.X, _mapPosition.Y, this);
            Game1._soundBlockHit.Play(.5f, 1f, 0f);
            KillMe();
        }
        public override Node Update(GameTime gameTime)
        {
            _stats.Update(gameTime);
            _timer.Update();
            UpdateRect();

            _mapPosition.X = (int)Math.Floor((_x+_cellW/2)/_cellW);
            _mapPosition.Y = (int)Math.Floor((_y+_cellH/2)/_cellH);

            if (_stats._energy <= 0)
            {
                DestroyMe();
            }
                

            if (_navi._isMouseOver && _mouse._onClick && !_mouse._isOverAny && !_mouse._isActiveReSize)
            {
                _parent.GotoFront(_index);
            }


            switch (_state)
            {
                case State.NONE:
                    break;
                case State.WAIT:


                    // Debug test SetDammage ! 
                    if (_navi._isMouseOver && ButtonControl.OnePress("DebugAttackUnit", Mouse.GetState().RightButton == ButtonState.Pressed))
                    {
                        AttackUnit(10);
                    }
                    // Keep the cell if is dropped and set draggable

                    _draggable.SetDraggable(true);

                    if (_isDropped)
                    {
                        _arena.SetCellUnit(_mapPosition.X, _mapPosition.Y, this);
                    }
                    else
                    {
                        if (!_draggable._isDragged)
                        {
                            _draggable._offDragged = true;
                        }
                    }

                    if (_draggable._isDragged)
                    {
                        Arena.CurrentUnitDragged = this;
                        // test si l'unit dragué est le même unit dans la case , si oui on enlève l'unit de la case
                        var cellOver = _arena.GetCell(_mapPosition.X, _mapPosition.Y);

                        // Test si l'unité est sur ces traces, si oui il efface
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

                        if (_timer.OnTimer((int)Timer.Trail))
                            new Trail(AbsRectF.Center, _size.ToVector2(), .025f, Color.WhiteSmoke).AppendTo(_parent);

                        //if (_timer.OnTimer((int)Timer.CheckPath))
                        //{
                        //    // Reset Paths
                        //    if (_paths != null)
                        //    {
                        //        if (_paths.Count > 0)
                        //        {
                        //            for (int i = 0; i < _paths.Count; i++)
                        //            {
                        //                if (_paths[i] != null)
                        //                    if (_paths[i].Count > 0)
                        //                        _paths[i].Clear();
                        //            }
                        //            _paths.Clear();
                        //        }
                        //    }

                        //    // AStar Paths

                        //    List<Point> _cells = new(); // all cells compose the unit
                        //    for (int i = 0; i < _size.X; i++)
                        //    {
                        //        for (int j = 0; j < _size.Y; j++)
                        //        {
                        //            _cells.Add(new Point(i, j));
                        //        }
                        //    }

                        //    for (int pointToSearch = 0; pointToSearch < _cells.Count; pointToSearch++)
                        //    {
                        //        _isCanMove = true;

                        //        Point start = _prevMapPosition;
                        //        Point end = _mapPosition;
                        //        if (_arena.IsInMap(start) && _arena.IsInMap(end))
                        //        {
                        //            _paths.Add(new Astar2DList<Cell>(_arena.GetMap(), start, end, Find.Diagonal, 1, false)._path);
                        //        }

                        //        for (int i = 0; i < _cells.Count; i++)
                        //        {
                        //            int x = _cells[i].X;
                        //            int y = _cells[i].Y;

                        //            if (x == _cells[pointToSearch].X && y == _cells[pointToSearch].Y) // Path to be copied
                        //            {

                        //            }
                        //            else // Copy the first path for all Cell of the unit by size
                        //            {
                        //                List<Point> pathCopy = new List<Point>();
                        //                for (int c = 0; c < _paths[0].Count; c++)
                        //                {
                        //                    Point pointCopy = _paths[0][c] + new Point(x,y);

                        //                    if (_arena.GetCellUnit(pointCopy) != null)
                        //                        _isCanMove = false;

                        //                    pathCopy.Add(pointCopy);
                        //                }
                        //                _paths.Add(pathCopy);
                        //            }
                        //        }

                        //        if (_isCanMove)
                        //            break;
                        //    }


                        //}


                    }

                    _backToPrevPosition = false;

                    if (_draggable._offDragged)
                    {
                        //Console.Write("<unit offDrag>");

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
                                    //Vector2 prevPosition = new Vector2(_prevMapX * _cellW, _prevMapY * _cellH);
                                    Vector2 prevPosition = _prevMapPosition.ToVector2() * _cellW;

                                    MoveTo(prevPosition);
                                    _backToPrevPosition = true;
                                }
                                else
                                {
                                    MoveTo(_prevPosition);
                                    _backToPrevPosition = true;
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
                                _backToPrevPosition = true;
                            }
                        }

                    }

                    if (_draggable._onDragged)
                    {
                        _isDropped = false;

                        _prevPosition = XY;
                        _prevMapPosition = _mapPosition;
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

                        //_arena.SetCellUnit(_mapPosition.X, _mapPosition.Y, this);

                        bool playSound = false;

                        if (_isDroppable)
                        {
                            _isDroppable = false;
                            _isDropped = true;

                            _dropZone._nearNode = this;
                            _dropZone._containedNode = this;
                            //Console.WriteLine("DropZone ContainedNode Affected !");
                            playSound = true;
                        }

                        if (_backToPrevPosition)
                        {
                            _isDropped = true;
                            playSound = true;
                        }

                        if (playSound)
                            Game1._soundClock.Play(Game1._volumeMaster * .5f, 1f, 0f);

                        SetState(State.WAIT);
                    }
                    else
                    {
                        // Efface les traces de l'unit dans l'Arena quand elle bouge toute seule
                        if (!_isDroppable || !_draggable._isDragged)
                            _arena.EraseCellUnit(_mapPosition.X, _mapPosition.Y, this);
                    }
                    break;
                case State.ATTACK:
                    break;
                case State.DAMAGE:

                    if (!_shake.IsShake)
                        SetState(State.WAIT);

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
                    //GFX.Rectangle(batch, AbsRectF.Extend(2), Color.Orange * .5f, 2f);
                    GFX.BevelledRectangle(batch, AbsRectF.Extend(2), Vector2.One * 10, Color.Orange * .5f, 4f);
                }

                Texture2D tex = Game1._texAvatar1x1;

                if (_size.X == 2 && _size.Y == 2) tex = Game1._texAvatar2x2;
                if (_size.X == 2 && _size.Y == 3) tex = Game1._texAvatar2x3;

                //batch.Draw(tex, AbsXY, Color.White);

                Color color = Color.White;

                if (Is(State.DAMAGE)) color = Color.IndianRed * .5f;

                GFX.Draw(batch, tex, color * (_arena.IsUnitInMap(this, Point.Zero)?1f:.75f), _loop._current, AbsXY + (tex.Bounds.Size.ToVector2()/2) + _shake.GetVector2(), Position.CENTER, Vector2.One);


                //if (_isDroppable)
                //    GFX.Rectangle(batch, AbsRect, Color.Red * .5f, 2f);
                //batch.Draw(Game1._texAvatar1x1, AbsXY, Color.Yellow);

                //GFX.Point(batch, AbsRectF.TopLeft + Vector2.One * 20, 12, Color.Red *.5f);
                GFX.CenterBorderedStringXY(batch, Game1._fontMain2, $"{_stats._energy}", AbsRectF.TopLeft + Vector2.One * 20 + _shake.GetVector2()*.5f, Color.GreenYellow, Color.Green);
                GFX.CenterBorderedStringXY(batch, Game1._fontMain2, $"{_stats._mana}", AbsRectF.TopRight - Vector2.UnitX * 20 + Vector2.UnitY * 20, Color.MediumSlateBlue, Color.DarkBlue);
                GFX.CenterBorderedStringXY(batch, Game1._fontMain2, $"{_stats._powerAttack}", AbsRectF.BottomLeft + Vector2.UnitX * 20 - Vector2.UnitY * 20, Color.Yellow, Color.Red);
            }

            if (indexLayer == (int)Layers.Debug)
            {
                GFX.CenterStringXY(batch, Game1._fontMain, $"{_mapPosition}\n{_isDropped}\n{_state}", AbsRectF.BottomCenter, Color.Yellow);

                //if (_paths != null && _draggable._isDragged)
                //    if (_paths.Count > 0)
                //    {
                //        for (int path = 0; path< _paths.Count; path++) 
                //        { 
                //            for (int i = 1; i < _paths[path].Count; i++)
                //            {
                //                Vector2 p1 = _paths[path][i - 1].ToVector2() * GetCellSize() + GetCellSize() / 2;
                //                Vector2 p2 = _paths[path][i].ToVector2() * GetCellSize() + GetCellSize() / 2;

                //                Color color = Color.LawnGreen;

                //                if (!_isCanMove)
                //                    color = Color.Red;
                                
                //                if (i == 1)
                //                    GFX.Point(batch, p1 + _arena.AbsXY, 8f, color);

                //                GFX.Line(batch, p1 + _arena.AbsXY, p2 + _arena.AbsXY, color * .25f, 4f);
                //                GFX.Point(batch, p2 + _arena.AbsXY, 10f, color * 1f);
                //            }
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

            if (indexLayer == (int)Layers.FrontFX)
            {
                if (Is(State.DAMAGE))
                {
                    //GFX.Rectangle(batch, AbsRectF.Extend(2), Color.Orange * .5f, 2f);
                    GFX.BevelledRectangle(batch, AbsRectF.Extend(2) + _shake.GetVector2(), Vector2.One * 10, Color.PaleVioletRed *.75f, 4f);
                }
            }

            return base.Draw(batch, gameTime, indexLayer);
        }

    }
}
