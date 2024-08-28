using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mugen.Animation;
using Mugen.Core;
using Mugen.Event;
using Mugen.GFX;
using Mugen.Physics;


namespace BattleSystem
{
    public class DragAndDrop : Node
    {
        public enum Timers
        {
            BeforeSpawn,
            Trail,
            Death,
            Spawn,
            Count,
        }
        public enum States
        {
            IsNull,
            IsSpawn,
            IsPlay,
            IsWait,
            IsMove,
            IsAttack,
            IsDamaged,
            IsDead,
            Count,
        }

        #region Attributes
        public Point MapPosition => _mapPosition;
        public Point Size => _size;
        public Vector2 PrevPosition => _prevPosition;
        public bool IsDropped => _isDropped;


        protected TimerEvent _timer;
        // Statistic of the Card
        //protected States _state;
        //protected States _prevState;
        protected Specs _specs = new();

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
        protected Point _size = new(1, 1);
        protected Point _mapPosition = new();
        // Come back to prev map position when drop in case is not possible
        protected bool _isBackToPrevPosition = false;
        //protected int _prevMapX;
        //protected int _prevMapY;
        protected Point _prevMapPosition = new();
        protected Vector2 _prevPosition = new();

        protected int _cellW;
        protected int _cellH;

        protected bool _isNearDropZone = false;
        protected bool _isDropped = false;
        protected DropZone _curDropZone;

        protected Addon.Draggable _draggable;
        protected Addon.Loop _loop;
        protected Shake _shake = new();

        protected float _ticScale = 0f;
        protected float _tempoScale = 60;
        protected float _scaleSpawn = 0f;
        protected float _alphaSpawn = 0f;

        protected float _tempoBeforeSpawn = 0f;

        static float _zIndex = 0;
        #endregion
        public DragAndDrop()
        {
            _type = UID.Get<DragAndDrop>();
        }

        public static void ResetZIndexDragAndDrop()
        {
            _zIndex = 0;
        }
        public static float GetZIndexDragAndDrop()
        {
            return _zIndex++;
        }
        public override Node Init()
        {
            return base.Init();
        }
        public void IsNearDropZone(bool isNearDropZone)
        {
            _isNearDropZone = isNearDropZone;
        }
        public void SetDropZone(DropZone dropZone)
        {
            _curDropZone = dropZone;
        }
        public DragAndDrop SetMapPosition(int mapX, int mapY)
        {
            _mapPosition.X = mapX;
            _mapPosition.Y = mapY;

            _x = _mapPosition.X * _cellW;
            _y = _mapPosition.Y * _cellH;

            return this;
        }
        public DragAndDrop SetCardSize(int sizeW, int sizeH)
        {
            _size.X = sizeW;
            _size.Y = sizeH;
            SetSize(_size.X * _cellW, _size.Y * _cellH);
            return this;
        }
        public override Node Update(GameTime gameTime)
        {
            _isNearDropZone = false;
            return base.Update(gameTime);
        }

    }

    public class Card : DragAndDrop
    {
        public Card(Arena arena, bool isDropped = true, float tempoBeforeSpawn = 0f) 
        {
            _z = GetZIndexDragAndDrop();

            _type = UID.Get<Card>();
            
            _arena = arena;
            _isDropped = isDropped;
            _tempoBeforeSpawn = tempoBeforeSpawn;

            _cellW = _arena.CellSize.X;
            _cellH = _arena.CellSize.Y;

            SetSize(_size.X * _cellW, _size.Y * _cellH);

            _draggable = new Addon.Draggable(this, Game1.MouseControl);
            _draggable.SetDragRectNode(true);
            _draggable.SetDraggable(true);
            AddAddon(_draggable);

            float angleDelta = .005f;
            _loop = new Addon.Loop(this);
            _loop.SetLoop(0, -Geo.RAD_225 * angleDelta, Geo.RAD_225 * angleDelta, .001f, Loops.PINGPONG);
            _loop.Start();
            AddAddon(_loop);

            _timer = new TimerEvent((int)Timers.Count);
            _timer.SetTimer((int)Timers.Trail, TimerEvent.Time(0, 0, .001f));
            _timer.SetTimer((int)Timers.Death, TimerEvent.Time(0, 0, .5f));
            _timer.SetTimer((int)Timers.Spawn, TimerEvent.Time(0, 1.5f, 0));
            _timer.SetTimer((int)Timers.BeforeSpawn, tempoBeforeSpawn);

            _timer.StartTimer((int)Timers.BeforeSpawn);

            _timer.StartTimer((int)Timers.Trail);

            //_timer.SetTimer((int)Timer.CheckPath, TimerEvent.Time(0, 0, .02f));
            //_timer.StartTimer((int)Timer.CheckPath);

            SetState((int)States.IsNull);

            if (tempoBeforeSpawn == 0)
                SetState((int)States.IsSpawn);
        }
        public override Node Init()
        {
            return base.Init();
        }

        public bool MoveToStep(Point mapStep, int durationMove = 6) // true if move possible
        {
            if (!_arena.IsCardInMap(this, mapStep) || _state == (int)States.IsMove)
                return false;

            Point mapDestPosition = _mapPosition + mapStep;

            // Test if move is possible by unit size
            for (int i = 0; i < _size.X; i++)
            {
                for (int j = 0; j < _size.Y; j++)
                {
                    var unit = _arena.GetCellCard(mapDestPosition + new Point(i, j));
                    if (unit != null)
                    {
                        if (unit._index != _index)
                        return false;
                    }
                }
            }

            _arena.EraseCellCard(this);

            _from = XY;

            _to.X = mapDestPosition.X * _cellW;
            _to.Y = mapDestPosition.Y * _cellH;

            _ticMove = 0;
            _tempoMove = durationMove;

            SetState((int)States.IsMove);

            return true;
        }
        public void MoveTo(Vector2 goal, int durationMove = 6)
        {
            _from = XY;
            _to = goal;

            _ticMove = 0;
            _tempoMove = durationMove;

            SetState((int)States.IsMove);
        }
        public void OnAttacked(int damage, float intensity = 10f)
        {
            if (_state != (int)States.IsPlay)
                return;

            _shake.SetIntensity(intensity, .25f);
            int overKill = _specs.SetDamage(damage);

            string str = "-" + damage;

            if (overKill < 0)
                str = "OVERKILL " + overKill;

            new PopInfo(str, Color.Yellow, Color.Red, 0, 24, 24)
                .SetPosition(_rect.TopCenter)
                .AppendTo(_parent);

            SetState((int)States.IsDamaged);
            Game1._soundSword.Play(.25f, 1f, 0f);

            new Slash().SetPosition(_rect.Center).AppendTo(_parent);
            
        }
        public void DestroyMe()
        {
            _arena.EraseCellCard(_mapPosition.X, _mapPosition.Y, this);
            Game1._soundBlockHit.Play(.5f, 1f, 0f);
            KillMe();
        }
        #region Dragged Methods
        private void OnDragged()
        {
            Misc.Log($"OnDragged {_index} GOTO FRONT");
            _arena.GotoFront(_index, UID.Get<Card>());

            _isDropped = false;
            _prevPosition = XY;
            _prevMapPosition = _mapPosition;

            _arena.EraseCellCard(_mapPosition.X, _mapPosition.Y, this);
        }
        private void IsDragged()
        {
            _arena.SetCurrentDragged(this);

            if (_timer.OnTimer((int)Timers.Trail))
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
        private void OffDragged()
        {
            if (_isNearDropZone && _arena.IsPossibleToDropCard(this))
            {
                MoveTo(_curDropZone._rectDropZone.TopLeft - _parent.XY);
                _isBackToPrevPosition = false;
            }
            else
            {
                MoveTo(_prevPosition);
                _isBackToPrevPosition = true;
            }

            Misc.Log($"_isBacktoPrevPosition = {_isBackToPrevPosition}");
        }
        #endregion

        void IsNull(GameTime gameTime)
        {
            if (_timer.OnTimer((int)Timers.BeforeSpawn))
            {
                SetState((int)States.IsSpawn);
                _ticScale = 0f;
            }
        }
        void IsSpawn(GameTime gameTime)
        {
            _scaleSpawn = Easing.GetValue(Easing.QuinticEaseOut, _ticScale, 2, 1, _tempoScale);
            _alphaSpawn = 2 - _scaleSpawn;

            _ticScale++;
            if (_ticScale >= _tempoScale)
            {
                _scaleSpawn = 1;

                SetState((int)States.IsPlay);
            }
        }
        void IsPlay(GameTime gameTime)
        {
            if (_draggable._isDragged)
            {
                IsDragged();
            }

            if (_draggable._onDragged)
            {
                OnDragged();
            }

            if (_draggable._offDragged)
            {
                OffDragged();
            }
        }
        void IsWait(GameTime gameTime)
        {

        }
        void IsMove(GameTime gameTime)
        {
            //Console.WriteLine("IsMove");
            if (_timer.OnTimer((int)Timers.Trail))
                new Trail(AbsRectF.Center, _size.ToVector2(), .025f, Color.WhiteSmoke).AppendTo(_parent);

            _x = Easing.GetValue(Easing.QuarticEaseOut, _ticMove, _from.X, _to.X, _tempoMove); // QuadraticEaseOut au lieu de Q***InOut pour eviter bug de détection de la dropZone _isNear car le mouvement est trop rapide a la fin
            _y = Easing.GetValue(Easing.QuarticEaseOut, _ticMove, _from.Y, _to.Y, _tempoMove);

            _ticMove++;
            if (_ticMove >= _tempoMove)
            {
                Misc.Log("..FinishMove..");

                _x = _to.X;
                _y = _to.Y;

                _mapPosition.X = (int)((_x + _cellW / 2) / _cellW);
                _mapPosition.Y = (int)((_y + _cellH / 2) / _cellH);

                _arena.SetCellCard(_mapPosition.X, _mapPosition.Y, this);

                _isDropped = true;

                bool playSound = false;

                if (_isNearDropZone)
                {
                    //_isNearDropZone = false;
                    _curDropZone.SetContainerNode(this);

                    playSound = true;
                }

                if (_isBackToPrevPosition)
                {
                    playSound = true;
                }

                if (playSound)
                    Game1._soundClock.Play(Game1._volumeMaster * .5f, 1f, 0f);

                BackState();
            }
            else
            {
                // Efface les traces de la carte dans l'Arena quand elle bouge toute seule
                if (!_isNearDropZone || !_draggable._isDragged)
                    _arena.EraseCellCard(_mapPosition.X, _mapPosition.Y, this);
            }
        }
        void IsAttack(GameTime gameTime)
        {

        }
        void IsDamaged(GameTime gameTime)
        {
            if (!_shake.IsShake)// && _specs.OffDamage)
                BackState();
        }
        void IsDead(GameTime gameTime)
        {
            if (_timer.OnTimer((int)Timers.Death))
            {
                //Console.WriteLine("Le est venu !!");
                new FireExplosion().SetPosition(_rect.Center).AppendTo(_arena);
                DestroyMe();
            }
        }
        protected override void ExitState()
        {
            switch ((States)_state)
            {
                case States.IsNull:
                    break;
                case States.IsSpawn:
                    break;
                case States.IsPlay:
                    break;
                case States.IsWait:
                    break;
                case States.IsMove:
                    break;
                case States.IsAttack:
                    break;
                case States.IsDamaged:
                    break;
                case States.IsDead:
                    break;
            }
        }
        protected override void EnterState()
        {
            switch ((States)_state)
            {
                case States.IsNull:
                    _draggable.SetDraggable(false);
                    break;
                case States.IsSpawn:
                    _draggable.SetDraggable(false);
                    break;
                case States.IsPlay:
                    _draggable.SetDraggable(true);
                    break;
                case States.IsWait:
                    _draggable.SetDraggable(false);
                    break;
                case States.IsMove:
                    _draggable.SetDraggable(false);
                    break;
                case States.IsAttack:
                    _draggable.SetDraggable(false);
                    break;
                case States.IsDamaged:
                    _draggable.SetDraggable(false);
                    break;
                case States.IsDead:
                    _draggable.SetDraggable(false);
                    break;
            }
        }
        protected override void RunState(GameTime gameTime)
        {
            switch ((States)_state)
            {
                case States.IsNull: IsNull(gameTime);
                    break;
                case States.IsSpawn: IsSpawn(gameTime);
                    break;
                case States.IsPlay: IsPlay(gameTime);
                    break;
                case States.IsWait: IsWait(gameTime);
                    break;
                case States.IsMove: IsMove(gameTime);
                    break;
                case States.IsAttack: IsAttack(gameTime);
                    break;
                case States.IsDamaged: IsDamaged(gameTime);
                    break;
                case States.IsDead: IsDead(gameTime);
                    break;
            }
        }
        public override Node Update(GameTime gameTime)
        {
            UpdateRect();

            _mapPosition.X = (int)Math.Floor((_x+_cellW/2)/_cellW);
            _mapPosition.Y = (int)Math.Floor((_y+_cellH/2)/_cellH);

            _specs.Update(gameTime);
            _timer.Update();

            if (_specs.Energy <= 0 && _state != (int)States.IsDead)
            {
                SetState((int)States.IsDead);
                _timer.StartTimer((int)Timers.Death);
            }

            if (_isDropped && _arena.IsCardInMap(this, Point.Zero))
                _arena.SetCellCard(_mapPosition.X, _mapPosition.Y, this);

            // invisible if _state is States.IsNull
            _isVisible = _state != (int)States.IsNull;

            RunState(gameTime);

            return base.Update(gameTime);
        }
        public override Node Draw(SpriteBatch batch, GameTime gameTime, int indexLayer)
        {
            switch ((Layers)indexLayer)
            {
                case Layers.ImGui:
                    break;

                case Layers.Gui:
                    break;

                case Layers.Main:

                    GFX.FillRectangle(batch, AbsRectF.Extend(-4), Color.Black * .8f);
                    //var canvas = GFX.FillRectangleCentered(batch, AbsRectF.Center, AbsRectF.GetSize() * _scaleSpawn, Color.Black *.4f * _alphaSpawn, _loop._current);
                    var canvas = RectangleF.GetRectangleCentered(AbsRectF.Center, AbsRectF.GetSize() * _scaleSpawn);

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

                    if (_state == (int)States.IsDamaged)
                        color = Color.IndianRed * 1f;

                    if (_state == (int)States.IsDead)
                        color = Color.Red;

                    GFX.Draw(batch, tex, color * (_arena.IsCardInMap(this, Point.Zero) ? 1f : .75f) * _alphaSpawn, _loop._current, AbsXY + (tex.Bounds.Size.ToVector2() / 2) + _shake.GetVector2(), Position.CENTER, Vector2.One * _scaleSpawn);


                    //if (_isDroppable)
                    //    GFX.Rectangle(batch, AbsRect, Color.Red * .5f, 2f);
                    //batch.Draw(Game1._texAvatar1x1, AbsXY, Color.Yellow);

                    Color fg = Color.GreenYellow;
                    Color bg = Color.Green;

                    if (_specs.Energy <= 20)
                    {
                        fg = Color.Yellow;
                        bg = Color.Red;
                    }
                    //GFX.Point(batch, AbsRectF.TopLeft + Vector2.One * 20, 12, Color.Red *.5f);

                    // Show Stats
                    //if (_state != State.IsSpawn)
                    {
                        GFX.Bar(batch, canvas.TopCenter + Vector2.UnitY * 2 - Vector2.UnitX * (_specs.MaxEnergy / 2) + _shake.GetVector2() * .5f, _specs.MaxEnergy, 8, Color.Red * _alphaSpawn);
                        GFX.Bar(batch, canvas.TopCenter + Vector2.UnitY * 2 - Vector2.UnitX * (_specs.MaxEnergy / 2) + _shake.GetVector2() * .5f, _specs.Energy, 8, fg * _alphaSpawn);
                        GFX.BarLines(batch, canvas.TopCenter + Vector2.UnitY * 2 - Vector2.UnitX * (_specs.MaxEnergy / 2) + _shake.GetVector2() * .5f, _specs.MaxEnergy, 8, Color.Black * _alphaSpawn, 2);

                        GFX.Bar(batch, canvas.TopCenter + (Vector2.UnitY * -0.25f) - Vector2.UnitX * (_specs.MaxEnergy / 2) + _shake.GetVector2() * .5f, _specs.MaxEnergy, 2, Color.White * .5f);

                        GFX.CenterBorderedStringXY(batch, Game1._fontMain2, $"{_specs.Energy}", canvas.TopLeft + Vector2.One * 20 + _shake.GetVector2() * .5f, fg * _alphaSpawn, bg * _alphaSpawn);
                        GFX.CenterBorderedStringXY(batch, Game1._fontMain2, $"{_specs.Mana}", canvas.TopRight - Vector2.UnitX * 20 + Vector2.UnitY * 20, Color.MediumSlateBlue * _alphaSpawn, Color.DarkBlue * _alphaSpawn);
                        GFX.CenterBorderedStringXY(batch, Game1._fontMain2, $"{_specs.PowerAttack}", canvas.BottomLeft + Vector2.UnitX * 20 - Vector2.UnitY * 20, Color.Yellow * _alphaSpawn, Color.Red * _alphaSpawn);
                    }
                    break;

                case Layers.Debug:

                    GFX.CenterStringXY(batch, Game1._fontMain, $"_isDropped{_isDropped}\n{(States)_state}\n_type={UID.Name(_type)}.{UID.Name(_subType)}\n_curDropZone={_curDropZone?._index}", AbsRectF.BottomCenter, Color.Yellow);

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
                    break;

                case Layers.FrontFX:

                    if (_state == (int)States.IsDamaged)
                    {
                        //GFX.Rectangle(batch, AbsRectF.Extend(2), Color.Orange * .5f, 2f);
                        GFX.BevelledRectangle(batch, AbsRectF.Extend(2) + _shake.GetVector2(), Vector2.One * 10, Color.PaleVioletRed * .75f, 4f);
                    }
                    break;

                case Layers.BackFX:

                    if (_state == (int)States.IsMove)
                    {
                        float alpha = _tempoMove / (float)(_ticMove * 5 + .01f);

                        GFX.Line(batch, _from + _parent.XY + new Vector2(_cellW / 2, _cellH / 2), AbsXY + new Vector2(_cellW / 2, _cellH / 2), Color.White * alpha, _cellW / 2);
                        GFX.Line(batch, _from + _parent.XY + new Vector2(_cellW / 2, _cellH / 2), AbsXY + new Vector2(_cellW / 2, _cellH / 2), Color.White * alpha, _cellW / 3);
                        GFX.Line(batch, _from + _parent.XY + new Vector2(_cellW / 2, _cellH / 2), AbsXY + new Vector2(_cellW / 2, _cellH / 2), Color.White * alpha, _cellW / 4);
                        GFX.Line(batch, _from + _parent.XY + new Vector2(_cellW / 2, _cellH / 2), AbsXY + new Vector2(_cellW / 2, _cellH / 2), Color.White * alpha, _cellW / 5);

                    }
                    break;

                case Layers.Count:
                    break;
            }


            return base.Draw(batch, gameTime, indexLayer);
        }


    }
}
