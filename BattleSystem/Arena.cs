using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Mugen.Core;
using Mugen.GFX;
using Mugen.Input;
using Mugen.Physics;
using System.Collections.Generic;
using System;


namespace BattleSystem
{
    public class Arena : Node
    {
        public enum States
        {
            PhasePlayer,
            PhaseEnemy,
            Transition,
            Count,
        }

        #region Attributes
        public int State { get { return _state; } }
        public Card CurrentDragged { get; private set; }
        int _mapW;
        int _mapH;
        RectangleF _rectZoneDroppable;
        public Point MapSize { get; private set; }
        Point _mapCursor = new();
        public Point MapCursor => _mapCursor;
        Vector2 _cursor = new();
        RectangleF _rectCursor;
        RectangleF _prevRectCursor;
        int _cellW;
        int _cellH;
        public Point CellSize { get; private set; }
        List2D<Cell> _grid;
        Vector2 _mouse;
        public bool _isMouseOverGrid = false;
        Addon.Loop _loop;
        DropZoneManager _dropZoneManager;
        DropZone _dropZoneInGrid;
        #endregion

        public Arena(int mapW, int mapH, int cellW = 32, int cellH = 32) 
        {

            _mapW = mapW;
            _mapH = mapH;
            MapSize = new Point(mapW, mapH);

            _grid = new List2D<Cell>(mapW, mapH);

            _rectZoneDroppable = new RectangleF(_rect.X, _rect.Y, _rect.Width, _rect.Height);

            _cellW = cellW;
            _cellH = cellH;
            CellSize = new Point(cellW, cellH);

            InitAllCells();

            SetSize(_mapW * _cellW, _mapH * _cellH);

            _loop = new Addon.Loop(this);
            _loop.SetLoop(0, 2f, 10f, .5f, Mugen.Animation.Loops.PINGPONG);
            _loop.Start();
            AddAddon(_loop);

            int[] _droppables = [UID.Get<Card>()]; // equivalent : new int[] {UID.Get<Card>()}


            _dropZoneManager = new DropZoneManager();
            _dropZoneManager.AddZone(new DropZone(new Rectangle(20, 120 + 180 * 0, _cellW, _cellH), -10, _droppables));
            _dropZoneManager.AddZone(new DropZone(new Rectangle(20, 120 + 180 * 1, _cellW, _cellH), -10, _droppables));

            _dropZoneInGrid = new DropZone(new Rectangle(0, 0, _cellW, _cellH), -10, _droppables);
            _dropZoneManager.AddZone(_dropZoneInGrid);
            _dropZoneInGrid.Show(false);

        }
        public override Node Init()
        {

            return base.Init();
        }

        #region Utils
        public Vector2 GetMouse()
        {
            return _mouse; 
        }
        public void SetCurrentDragged(Card card)
        {
            CurrentDragged = card;
        }
        public void InitAllCells()
        {
            for (int i = 0; i < _cellW; i++)
            {
                for (int j = 0; j < _cellH; j++)
                {
                    var cell = new Cell(this, new Point(i, j), new Point(_cellW, _cellH));
                    _grid.Put(i, j, cell);
                }
            }
        }
        public bool AddCard(int mapX, int mapY, int sizeW, int sizeH)
        {
            if (IsDetectCardInRect(mapX, mapY, sizeW, sizeH))
                return false;

            var card = new Card(this);
            card.SetCardSize(sizeW, sizeH);
            card.SetMapPosition(mapX, mapY).AppendTo(this);

            SetCellCard(mapX, mapY, card);

            return true;
        }
        public bool AddCard(int mapX, int mapY, Card card)
        {
            if (IsDetectCardInRect(mapX, mapY, card.Size.X, card.Size.Y))
                return false;

            card.SetMapPosition(mapX, mapY).AppendTo(this);

            SetCellCard(card.MapPosition.X, card.MapPosition.Y, card);

            return true;
        }
        public List<List<Cell>> GetMap()
        {
            return _grid.Get2DList();
        }
        public bool IsPossibleToDropCard(Card card)
        {
            if (!_isMouseOverGrid) return true;
            
            bool isPossibleToDrop = true;

            for (int i = 0; i < card.Size.X; i++)
            {
                for (int j = 0; j < card.Size.Y; j++)
                {
                    var cellOver = GetCell(card.MapPosition.X + i, card.MapPosition.Y + j);

                    if (cellOver == null)
                    {
                        isPossibleToDrop = false;
                    }

                    if (cellOver != null)
                    {
                        if (cellOver._card != null)
                        {
                            isPossibleToDrop = false;
                        }
                    }
                }
            }

            return isPossibleToDrop;
        }
        public bool IsPointInMap(int mapX, int mapY)
        {
            return !(mapX < 0 || mapX > _mapW || mapY < 0 || mapY > _mapH);
        }
        public bool IsPointInMap(Point mapPoint)
        {
            return IsPointInMap(mapPoint.X, mapPoint.Y);
        }
        public bool IsCardInMap(Card card, Point translate)
        {
            return !(card.MapPosition.X + translate.X < 0 || card.MapPosition.X + translate.X> _mapW - card.Size.X || card.MapPosition.Y + translate.Y < 0 || card.MapPosition.Y + translate.Y > _mapH - card.Size.Y);
        }
        public bool IsFullRectInsideMap(Point mapPosition, Point size, Point translate)
        {
            return !(mapPosition.X + translate.X < 0 || mapPosition.X + translate.X > _mapW - size.X || mapPosition.Y + translate.Y < 0 || mapPosition.Y + translate.Y > _mapH - size.Y);
        }
        public void SetCell(int mapX, int mapY, Cell cell)
        {
            if (mapX < 0 || mapX > _mapW || mapY < 0 || mapY > _mapH)
                return;

            //_cells[mapX, mapY] = cell;
            _grid.Put(mapX, mapY, cell);
        }
        public void SetCellCard(int mapX, int mapY, Card card)
        {
            if (card != null)
            {
                if (mapX < 0 || mapX + card.Size.X > _mapW || mapY < 0 || mapY + card.Size.Y > _mapH)
                    return;

                for (int i = 0; i < card.Size.X; i++)
                {
                    for (int j = 0; j < card.Size.Y; j++)
                    {
                        var cell = _grid.Get(mapX + i, mapY + j); 

                        if (cell != null)
                        {
                            cell._card = card;
                            cell._passLevel = 1;
                        }
                    }
                }
            }

        }
        public void ClearAllCellCard()
        {
            for (int i = 0; i < _mapW; i++)
            {
                for (int j = 0; j < _mapH; j++)
                {
                    var cell = _grid.Get(i, j);
                    
                    if (cell != null)
                        cell._card = null;
                }
            }
        }
        public void ClearArena()
        {
            KillAll(new int[] { UID.Get<Card>() });
            ClearAllCellCard();
        }
        public void EraseCellCard(int mapX, int mapY, Card card)
        {
            //Console.WriteLine("EraseCellCard");
            if (card != null)
            {
                if (mapX < 0 || mapX + card.Size.X > _mapW || mapY < 0 || mapY + card.Size.Y > _mapH)
                    return;

                for (int i = 0; i < card.Size.X; i++)
                {
                    for (int j = 0; j < card.Size.Y; j++)
                    {
                        var cell = _grid.Get(mapX + i, mapY + j);

                        if (cell != null)
                        {
                            cell._card = null;
                            cell._passLevel = 0;
                        }
                    }
                }
            }

        }
        public void EraseCellCard(Point mapPosition, Card card)
        {
            EraseCellCard(mapPosition.X, mapPosition.Y, card);
        }
        public void EraseCellCard(Card card)
        {
            EraseCellCard(card.MapPosition, card);
        }
        public Cell GetCell(int mapX, int mapY)
        {
            if (mapX < 0 || mapX > _mapW || mapY < 0 || mapY > _mapH)
                return null;
            
            return _grid.Get(mapX, mapY);
        }
        public Cell GetCell(Point mapPosition)
        {
            return GetCell(mapPosition.X, mapPosition.Y);
        }
        private bool IsDetectCardInRect(int mapX, int mapY, int sizeW, int sizeH)
        {
            for (int i = 0; i < sizeW; i++)
            {
                for (int j = 0; j < sizeH; j++)
                {
                    var cell = _grid.Get(mapX + i, mapY + j);
                    if (cell != null)
                        if (cell._card != null)
                            return true;
                }
            }

            return false;
        }
        public Card GetCellCard(int mapX, int mapY)//, int sizeW, int sizeH)
        {
            if (mapX < 0 || mapX > _mapW || mapY < 0 || mapY > _mapH)
                return null;

            var cell = _grid.Get(mapX, mapY);
            
            if (cell != null)
                return cell._card;

            return null;
        }
        public Card GetCellCard(Point mapPosition)//, Point size)
        {
            return GetCellCard(mapPosition.X, mapPosition.Y);//, size.X, size.Y);
        }
        private void UpdateCells()
        {
            for (int i = 0; i < _cellW; i++)
            {
                for (int j = 0; j < _cellH; j++)
                {
                    var cell = _grid.Get(i, j);

                    if (cell != null)
                        cell.Update();
                }
            }
        }
        private void DrawCells(SpriteBatch batch, int indexLayer)
        {
            for (int i = 0; i < _cellW; i++)
            {
                for (int j = 0; j < _cellH; j++)
                {
                    var cell = _grid.Get(i, j);
                    if (cell != null)
                        cell.Draw(batch, AbsXY.ToPoint(), indexLayer);
                }
            }
        }
        public void MoveAllCardLeft(int duration = 32)
        {
            for (int i = 0; i < _mapW; i++)
            {
                for (int j = 0; j < _mapH; j++)
                {
                    var card = GetCellCard(i, j);
                    if (card != null)
                        card.MoveToStep(new Point(-1, 0), duration);
                }
            }
        }
        public void MoveAllCardRight(int duration = 32)
        {
            for (int i = _mapW; i >= 0; i--)
            {
                for (int j = 0; j < _mapH; j++)
                {
                    var card = GetCellCard(i, j);
                    if (card != null)
                        card.MoveToStep(new Point(1, 0), duration);
                }
            }
        }
        public void MoveAllCardUp(int duration = 32)
        {
            for (int j = 0; j < _mapH; j++)
            {
                for (int i = 0; i < _mapW; i++)
                {
                    var card = GetCellCard(i, j);
                    if (card != null)
                        card.MoveToStep(new Point(0, -1), duration);
                }
            }
        }
        public void MoveAllCardDown(int duration = 32)
        {
            for (int j = _mapH; j >= 0; j--)
            {
                for (int i = 0; i < _mapW; i++)
                {
                    var card = GetCellCard(i, j);
                    if (card != null)
                        card.MoveToStep(new Point(0, 1), duration);
                }
            }
        }
        #endregion

        #region State Methods
        protected override void ExitState()
        {
            switch ((States)_state)
            {
                case States.PhasePlayer:
                    break;
                case States.PhaseEnemy:
                    break;
                case States.Transition:
                    break;
            }
        }
        protected override void EnterState()
        {
            switch ((States)_state)
            {
                case States.PhasePlayer:
                    break;

                case States.PhaseEnemy:
                    foreach (var node in GroupOf(UID.Get<Card>()))
                    {
                        Card card = node.This<Card>();
                        card.SetState((int)Card.States.IsWait);
                    }
                    break;

                case States.Transition:
                    break;
            }
        }
        protected override void RunState(GameTime gameTime)
        {
            switch ((States)_state)
            {
                case States.PhasePlayer: PhasePlayer(gameTime);
                    break;
                case States.PhaseEnemy: PhaseEnemy(gameTime);
                    break;
                case States.Transition: Transition(gameTime);
                    break;
            }            
        }
        void PhasePlayer(GameTime gameTime)
        {
            if (CurrentDragged != null)
            {
                // Quand drag un Unit , on change la position du pointeur (souris) par le milieu de l'Unit qui est en train d'être dragué
                _mouse.X = CurrentDragged._rect.TopLeft.X + _cellW / 2;
                _mouse.Y = CurrentDragged._rect.TopLeft.Y + _cellH / 2;

                _rectZoneDroppable.Width = _rect.Width - ((CurrentDragged.Size.X - 1) * _cellW) + _cellW / 2;
                _rectZoneDroppable.Height = _rect.Height - ((CurrentDragged.Size.Y - 1) * _cellH) + _cellH / 2;
            }
            else
            {
                _mouse.X = Game1.MouseControl.GetPosition().X - _x;
                _mouse.Y = Game1.MouseControl.GetPosition().Y - _y;

                _rectZoneDroppable.X = _rect.X;
                _rectZoneDroppable.Y = _rect.Y;
            }

            _isMouseOverGrid = Misc.PointInRect(_mouse.X + _x + _cellW / 2, _mouse.Y + _y + _cellH / 2, _rectZoneDroppable);

            _mapCursor.X = (int)Math.Floor(_mouse.X / _cellW);
            _mapCursor.Y = (int)Math.Floor(_mouse.Y / _cellH);

            if (_mapCursor.X < 0) _mapCursor.X = 0;
            if (_mapCursor.X > _mapW - 1) _mapCursor.X = _mapW - 1;
            if (_mapCursor.Y < 0) _mapCursor.Y = 0;
            if (_mapCursor.Y > _mapH - 1) _mapCursor.Y = _mapH - 1;

            _cursor.X = _mapCursor.X * _cellW;
            _cursor.Y = _mapCursor.Y * _cellH;


            if (CurrentDragged != null)
                _rectCursor = new RectangleF(_cursor.ToPoint() + new Point(AbsX, AbsY), new Size2(CurrentDragged._rect.Width, CurrentDragged._rect.Height));
            else
                _rectCursor = new RectangleF(_cursor.ToPoint() + new Point(AbsX, AbsY), new Size2(_cellW, _cellH));

            if (_prevRectCursor != _rectCursor && CurrentDragged != null)
                Game1._soundClock.Play(0.1f, 1f, .5f);

            _prevRectCursor = _rectCursor;

            _dropZoneInGrid.UpdateZone(_rectCursor, -10);


            if (!_isMouseOverGrid && !Game1.MouseControl._isActiveDrag && CurrentDragged != null)
                _dropZoneInGrid.SetActive(false);
            else 
                _dropZoneInGrid.SetActive(true);

            _dropZoneManager.Update(gameTime, this, [UID.Get<Card>()]);

            CurrentDragged = null;

            SortZAscending();
            UpdateChildsSort(gameTime);
            UpdateCells();

            // reset focus
            //if (_mouseControl._onClick)
            //{
            //    foreach (Node node in _listItems)
            //    {
            //        if (!node._navi._isMouseOver)// && !node._resizable._isMouseOver)
            //            node._navi._isFocus = false;
            //    }
            //}
        }
        void PhaseEnemy(GameTime gameTime)
        {
            SortZAscending();
            UpdateChildsSort(gameTime);
            UpdateCells();
        }
        void Transition(GameTime gameTime)
        {
            SortZAscending();
            UpdateChildsSort(gameTime);
            UpdateCells();
        }
        #endregion

        public override Node Update(GameTime gameTime)
        {
            UpdateRect();

            RunState(gameTime);

            #region DEBUG
            if (ButtonControl.OnePress("MoveLeft", Keyboard.GetState().IsKeyDown(Keys.Left)))   MoveAllCardLeft(16);    
            if (ButtonControl.OnePress("MoveRight", Keyboard.GetState().IsKeyDown(Keys.Right))) MoveAllCardRight(16);
            if (ButtonControl.OnePress("MoveUp", Keyboard.GetState().IsKeyDown(Keys.Up)))       MoveAllCardUp(16);    
            if (ButtonControl.OnePress("MoveDown", Keyboard.GetState().IsKeyDown(Keys.Down)))   MoveAllCardDown(16);
            #endregion

            return base.Update(gameTime);
        }
        public override Node Draw(SpriteBatch batch, GameTime gameTime, int indexLayer)
        {
            SortZDescending();

            switch ((Layers)indexLayer)
            {
                case Layers.ImGui:

                    break;

                case Layers.Gui:

                    DrawChilds(batch, gameTime, indexLayer);
                    break;

                case Layers.Main:

                    // Show prevPosition of the CurrentUnitDragged
                    if (CurrentDragged != null)
                        GFX.FillRectangle(batch, CurrentDragged.PrevPosition + AbsXY - (Vector2.One * (_loop._current - 20)), CurrentDragged.AbsRectF.GetSize() + (Vector2.One * 2 * (_loop._current - 20)), Color.Black * .25f);

                    //GFX.FillRectangle(batch, AbsRect, Color.DarkBlue * .2f);
                    GFX.FillRectangle(batch, AbsRect, Color.DarkBlue * .15f);

                    GFX.Grid(batch, AbsXY, _rect.Width, _rect.Height, _cellW, _cellH, Color.Gray * .25f, 3);
                    GFX.Grid(batch, AbsXY, _rect.Width, _rect.Height, _cellW, _cellH, Color.Black * .75f, 1);

                    //GFX.BevelledRectangle(batch, AbsRect, Vector2.One * 4, Color.WhiteSmoke * .5f, 2f);
                    GFX.Rectangle(batch, AbsRect, Color.WhiteSmoke * .5f, 2f);

                    _dropZoneManager.Draw(batch);

                    // Draw rectCursor if unit over in zone grid
                    if (Game1.MouseControl._isActiveDrag)
                    {
                        if (CurrentDragged != null)
                        {
                            RectangleF rectCursorExtend = ((RectangleF)_rectCursor).Extend(_loop._current);
                            Color color = Color.LawnGreen;

                            if (!IsPossibleToDropCard(CurrentDragged))
                            {
                                color = Color.OrangeRed;
                            }

                            if (IsCardInMap(CurrentDragged, Point.Zero))
                            {
                                GFX.FillRectangle(batch, rectCursorExtend, color * .25f);
                                //GFX.Rectangle(batch, rectCursorExtend, color * .25f, 8f);
                                GFX.BevelledRectangle(batch, rectCursorExtend, Vector2.One * 10, color * .25f, 4f);
                            }
                        }

                    }

                    DrawChilds(batch, gameTime, indexLayer);
                    //DrawCells(batch, indexLayer);
                    break;

                case Layers.Debug:

                    DrawChilds(batch, gameTime, indexLayer);
                    ShowDebug(batch);

                    GFX.LeftTopString(batch, Game1._fontMain, $"{_mouse} -- {_mapCursor} -- {CurrentDragged} {CurrentDragged?._index}", AbsXY + new Vector2(10, -20), Color.AntiqueWhite);

                    GFX.LeftTopString(batch, Game1._fontMain, $"{(States)_state} {_isMouseOverGrid}", AbsRectF.BottomLeft + new Vector2(10, 10), Color.AntiqueWhite);
                    break;

                case Layers.FrontFX:

                    DrawChilds(batch, gameTime, indexLayer);
                    break;

                case Layers.BackFX:

                    DrawChilds(batch, gameTime, indexLayer);
                    DrawCells(batch, indexLayer);

                    break;


            }

            return base.Draw(batch, gameTime, indexLayer);
        }
        private void ShowDebug(SpriteBatch batch)
        {

            // Debug Zone when is Drag Unit is possible to drop !!
            if (_isMouseOverGrid)
                GFX.Rectangle(batch, _rectZoneDroppable, Color.Red, 2f);

            for (int i = 0; i < _mapW; i++)
            {
                for (int j = 0; j < _mapH; j++)
                {
                    Vector2 pos = new Vector2(i * _cellW, j * _cellH) + AbsXY + new Vector2(_cellW/2, 20);

                    if (_grid.Get(i, j)._card != null)
                        GFX.CenterBorderedStringXY(batch, Game1._fontMain, $"{_grid.Get(i, j)._card._index}", pos + new Vector2(0, 40), Color.Yellow, Color.Red);
                    //else
                    //    GFX.LeftTopBorderedString(batch, Game1._fontMain, ".", pos, Color.Yellow, Color.Red);

                    // Show passLevel in cell
                    //GFX.CenterBorderedStringXY(batch, Game1._fontMain, $"{_cells.Get(i,j)._passLevel}", pos + new Vector2(0, 80), Color.Yellow, Color.Green);
                }
            }

            if (_isMouseOverGrid)
            {
                var card = _grid.Get(_mapCursor.X, _mapCursor.Y)._card;
                
                if (card != null)
                    GFX.TopCenterString(batch, Game1._fontMain, $"{card} {card._index}", card._rect.TopCenter + AbsXY - Vector2.UnitY * 20, Color.Red * .75f);
                //else
                //    GFX.TopCenterString(batch, Game1._fontMain, "No Unit Here", (_mapCursor * CellSize).ToVector2() + AbsXY + card._rect.BottomCenter, Color.Red * .25f);

            }
        }
    }
}
