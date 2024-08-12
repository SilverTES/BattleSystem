using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Mugen.Core;
using Mugen.GFX;
using Mugen.Input;
using Mugen.Physics;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;


namespace BattleSystem
{
    public class Arena : Node
    {
        enum State
        {
            Phase_Player,
            Phase_Enemy,
            Transition,
            LAST
        }

        State _state = State.Phase_Player;

        public static Unit CurrentUnitDragged;

        int _mapW;
        int _mapH;

        RectangleF _rectZoneDroppable;

        public Point MapSize { get; private set; }

        Node _layerGui;
        Gui.Button _btnAction;

        Point _mapCursor = new();
        Vector2 _cursor = new();
        RectangleF _rectCursor;

        int _cellW;
        int _cellH;

        public Point CellSize { get; private set; }

        List2D<Cell> _cells;

        Vector2 _mouse;
        public bool _isMouseOverGrid = false;

        Addon.Loop _loop;

        DropZoneManager _dropZoneManager;
        DropZone _dropZoneInGrid;
        
        List<Node> _listItems;
        
        public Arena(int mapW, int mapH, int cellW = 32, int cellH = 32) 
        { 
             _mapW = mapW;
            _mapH = mapH;
            MapSize = new Point(mapW, mapH);

            _cells = new List2D<Cell>(mapW, mapH);

            _rectZoneDroppable = new RectangleF(_rect.X, _rect.Y, _rect.Width, _rect.Height);

            _cellW = cellW;
            _cellH = cellH;
            CellSize = new Point(cellW, cellH);

            InitAllCells();

            SetSize(_mapW * _cellW, _mapH * _cellH);

            _loop = new Addon.Loop(this);
            _loop.SetLoop(0, 5f, 15f, .5f, Mugen.Animation.Loops.PINGPONG);
            _loop.Start();
            AddAddon(_loop);

            int[] _droppables = new int[] { UID.Get<Unit>() };


            _dropZoneManager = new DropZoneManager();
            _dropZoneManager.AddZone(new DropZone(new Rectangle(20, 120 + 180 * 0, _cellW, _cellH), -10, _droppables));
            //_dropZoneManager.AddZone(new DropZone(new Rectangle(20, 120 + 180 * 1, _cellW, _cellH), -10, _droppables));
            //_dropZoneManager.AddZone(new DropZone(new Rectangle(20, 120 + 180 * 2, _cellW, _cellH), -10, _droppables));
            //_dropZoneManager.AddZone(new DropZone(new Rectangle(20, 120 + 180 * 3, _cellW, _cellH), -10, _droppables));
            //_dropZoneManager.AddZone(new DropZone(new Rectangle(20, 120 + 180 * 4, _cellW, _cellH), -10, _droppables));

            _dropZoneInGrid = new DropZone(new Rectangle(0, 0, _cellW, _cellH), -10, _droppables);
            _dropZoneInGrid.Show(false);
            _dropZoneManager.AddZone(_dropZoneInGrid);

            _layerGui = new Node();

            var style = (JObject)JsonConvert.DeserializeObject(File.ReadAllText("Content/Misc/styleBtn.json"));

            _btnAction = (Gui.Button)new Gui.Button(Game1.MouseControl, "Action", style)
                .SetPosition(Game1.ScreenW/2, Game1.ScreenH - 40)
                .AppendTo(_layerGui);
        }
        public void InitAllCells()
        {
            for (int i = 0; i < _cellW; i++)
            {
                for (int j = 0; j < _cellH; j++)
                {
                    var cell = new Cell(this, new Point(i, j), new Point(_cellW, _cellH));
                    _cells.Put(i, j, cell);
                }
            }
        }
        public void AddUnit(int mapX, int mapY, int sizeW, int sizeH)
        {
            if (GetCellUnit(mapX, mapY) != null)
                return;

            var unit = new Unit(Game1.MouseControl, this, sizeW, sizeH, _cellW, _cellH);
            unit.SetMapPosition(mapX, mapY).AppendTo(this);

            SetCellUnit(mapX, mapY, unit);
        }
        public List<List<Cell>> GetMap()
        {
            return _cells.Get2DList();
        }
        public bool IsInMap(int mapX, int mapY)
        {
            return !(mapX < 0 || mapX > _mapW || mapY < 0 || mapY > _mapH);
        }
        public bool IsInMap(Point mapPoint)
        {
            return IsInMap(mapPoint.X, mapPoint.Y);
        }
        public void SetCell(int mapX, int mapY, Cell cell)
        {
            if (mapX < 0 || mapX > _mapW || mapY < 0 || mapY > _mapH)
                return;

            //_cells[mapX, mapY] = cell;
            _cells.Put(mapX, mapY, cell);
        }
        public void SetCellUnit(int mapX, int mapY, Unit unit)
        {
            if (unit != null)
            {
                if (mapX < 0 || mapX + unit.Size.X > _mapW || mapY < 0 || mapY + unit.Size.Y > _mapH)
                    return;

                for (int i = 0; i < unit.Size.X; i++)
                {
                    for (int j = 0; j < unit.Size.Y; j++)
                    {
                        var cell = _cells.Get(mapX + i, mapY + j); 

                        if (cell != null)
                        {
                            cell._unit = unit;
                            cell._passLevel = 1;
                        }
                    }
                }
            }

        }
        public void EraseCellUnit(int mapX, int mapY, Unit unit)
        {
            if (unit != null)
            {
                if (mapX < 0 || mapX + unit.Size.X > _mapW || mapY < 0 || mapY + unit.Size.Y > _mapH)
                    return;

                for (int i = 0; i < unit.Size.X; i++)
                {
                    for (int j = 0; j < unit.Size.Y; j++)
                    {
                        var cell = _cells.Get(mapX + i, mapY + j);

                        if (cell != null)
                        {
                            cell._unit = null;
                            cell._passLevel = 0;
                        }
                    }
                }
            }

        }
        public Cell GetCell(int mapX, int mapY)
        {
            if (mapX < 0 || mapX > _mapW || mapY < 0 || mapY > _mapH)
                return null;
            
            return _cells.Get(mapX, mapY);
        }
        public Cell GetCell(Point mapPosition)
        {
            return GetCell(mapPosition.X, mapPosition.Y);
        }
        public Unit GetCellUnit(int mapX, int mapY)
        {
            if (mapX < 0 || mapX > _mapW || mapY < 0 || mapY > _mapH)
                return null;

            var cell = _cells.Get(mapX, mapY);
            
            if (cell != null)
                return cell._unit;

            return null;
        }
        public Unit GetCellUnit(Point mapPosition)
        {
            return GetCellUnit(mapPosition.X, mapPosition.Y);
        }
        private void UpdateCells()
        {
            for (int i = 0; i < _cellW; i++)
            {
                for (int j = 0; j < _cellH; j++)
                {
                    var cell = _cells.Get(i, j);
                    
                    if (cell != null )
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
                    var cell = _cells.Get(i, j);
                    if (cell != null)
                        cell.Draw(batch, AbsXY.ToPoint(), indexLayer);
                }
            }
        }
        public override Node Init()
        {

            return base.Init();
        }
        public override Node Update(GameTime gameTime)
        {
            UpdateRect();
            _layerGui.UpdateChilds(gameTime);

            _listItems = GroupOf(new int[] { UID.Get<Unit>() });
            _dropZoneManager.Update(gameTime, _listItems);

            switch (_state)
            {
                case State.Phase_Player:

                    if (CurrentUnitDragged != null)
                    {
                        // Quand drag un Unit , on change la position du pointeur (souris) par le milieu de l'Unit qui est en train d'être dragué
                        _mouse.X = CurrentUnitDragged._rect.TopLeft.X + _cellW / 2;
                        _mouse.Y = CurrentUnitDragged._rect.TopLeft.Y + _cellH / 2;

                        _rectZoneDroppable.Width = _rect.Width - ((CurrentUnitDragged.Size.X - 1) * _cellW) + _cellW / 2;
                        _rectZoneDroppable.Height = _rect.Height - ((CurrentUnitDragged.Size.Y - 1) * _cellH) + _cellH / 2;
                    }
                    else
                    {
                        _mouse.X = Game1.MouseControl.GetPosition().X - _x;
                        _mouse.Y = Game1.MouseControl.GetPosition().Y - _y;

                        _rectZoneDroppable.X = _rect.X;
                        _rectZoneDroppable.Y = _rect.Y;
                    }

                    _isMouseOverGrid = Misc.PointInRect(_mouse.X + _x + _cellW / 2, _mouse.Y + _y + _cellH / 2, _rectZoneDroppable);

                    _mapCursor.X = (int)(_mouse.X / _cellW);
                    _mapCursor.Y = (int)(_mouse.Y / _cellH);

                    if (_mapCursor.X < 0) _mapCursor.X = 0;
                    if (_mapCursor.X > _mapW - 1) _mapCursor.X = _mapW - 1;
                    if (_mapCursor.Y < 0) _mapCursor.Y = 0;
                    if (_mapCursor.Y > _mapH - 1) _mapCursor.Y = _mapH - 1;

                    _cursor.X = _mapCursor.X * _cellW;
                    _cursor.Y = _mapCursor.Y * _cellH;



                    // Manage Drag & Drop Zone
                    if (_isMouseOverGrid && Game1.MouseControl._down)
                    {
                        if (CurrentUnitDragged != null)
                            _rectCursor = new RectangleF(_cursor.ToPoint() + new Point(AbsX, AbsY), new Size2(CurrentUnitDragged._rect.Width, CurrentUnitDragged._rect.Height));
                        else
                            _rectCursor = new RectangleF(_cursor.ToPoint() + new Point(AbsX, AbsY), new Size2(_cellW, _cellH));

                        //if (GetCellUnit(_mapCursor.X, _mapCursor.Y) == null)
                        _dropZoneInGrid.UpdateZone(_rectCursor, -10);
                    }

                    CurrentUnitDragged = null;
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

                    if (_btnAction._navi._onPress)
                    {
                        Game1._soundClock.Play(Game1._volumeMaster * .5f, 1f, .5f);
                        //Console.WriteLine("btnAction Pressed !");
                        foreach (Node node in GroupOf(new int[] { UID.Get<Unit>() }))
                        {
                            Unit unit = node.This<Unit>();

                            if (IsInMap(unit.MapPosition))
                                unit.MoveTo(unit.MapPosition + new Point(-1, 0), 32);

                        }

                    }

                    break;
                case State.Phase_Enemy:


                    break;
                case State.LAST:

                    break;
                case State.Transition:

                    break;
                default:
                    break;
            }


            #region DEBUG
            if (Button.OnePress("MoveLeft", Keyboard.GetState().IsKeyDown(Keys.Space)))
            {
                //Console.WriteLine("Move Left");

                foreach (Node node in _listItems)
                {
                    Unit unit = node.This<Unit>();

                    if (IsInMap(unit.MapPosition))
                        unit.MoveTo(unit.MapPosition + new Point(-1, 0), 32);

                }
            }
            #endregion

            return base.Update(gameTime);
        }
        public override Node Draw(SpriteBatch batch, GameTime gameTime, int indexLayer)
        {
            SortZDescending();

            if (indexLayer == (int)Layers.Main)
            {
                // Show prevPosition of the CurrentUnitDragged
                if (CurrentUnitDragged != null)
                    GFX.FillRectangle(batch, CurrentUnitDragged.PrevPosition + AbsXY - (Vector2.One * (_loop._current - 20)), CurrentUnitDragged.AbsRectF.GetSize() + (Vector2.One * 2 * (_loop._current - 20)), Color.Black * .25f);

                //GFX.FillRectangle(batch, AbsRect, Color.DarkBlue * .2f);
                GFX.FillRectangle(batch, AbsRect, Color.DarkBlue * .15f);

                GFX.Grid(batch, AbsXY, _rect.Width, _rect.Height, _cellW, _cellH, Color.Gray * .25f, 3);
                GFX.Grid(batch, AbsXY, _rect.Width, _rect.Height, _cellW, _cellH, Color.Black * .75f, 1);

                GFX.Rectangle(batch, AbsRect, Color.WhiteSmoke * .5f);

                _dropZoneManager.Draw(batch);

                DrawChilds(batch, gameTime, indexLayer);
                DrawCells(batch, indexLayer);

                GFX.LeftTopString(batch, Game1._fontMain, $"{_mouse} -- {_mapCursor} -- {CurrentUnitDragged} {CurrentUnitDragged?._index}", AbsXY + new Vector2(10, -20), Color.AntiqueWhite);

                // Debug Zone when is Drag Unit is possible to drop !!
                //if (!_isMouseOver) 
                //    GFX.Rectangle(batch, _rectZone, Color.Red, 4f);

                GFX.LeftTopString(batch, Game1._fontMain, $"{_state} {_isMouseOverGrid}", AbsRectF.BottomLeft + new Vector2(10, 10), Color.AntiqueWhite);
            }

            if (indexLayer == (int)Layers.BackFX)
            {

                DrawChilds(batch, gameTime, indexLayer);
                DrawCells(batch, indexLayer);

                if (Game1.MouseControl._isActiveDrag)
                {
                    RectangleF rectCursorExtend = ((RectangleF)_rectCursor).Extend(_loop._current);

                    Color color = Color.ForestGreen;

                    if (CurrentUnitDragged != null)
                    {
                        if (!CurrentUnitDragged.IsPossibleToDrop)
                        {
                            color = Color.Red;
                        }
                    }

                    if (_isMouseOverGrid)
                    {
                        GFX.FillRectangle(batch, rectCursorExtend, color * .75f);
                        GFX.Rectangle(batch, rectCursorExtend, color * .5f, 4f);
                    }

                }

            }

            if (indexLayer == (int)Layers.Gui)
            {
                _layerGui.DrawChilds(batch, gameTime, indexLayer);
            }

            if (indexLayer == (int)Layers.FrontFX)
            {
                DrawChilds(batch, gameTime, indexLayer);
                _layerGui.DrawChilds(batch, gameTime, indexLayer);
            }

            if (indexLayer == (int)Layers.Debug)
            {
                DrawChilds(batch, gameTime, indexLayer);
                ShowValue(batch);
            }

            return base.Draw(batch, gameTime, indexLayer);
        }

        private void ShowValue(SpriteBatch batch)
        {
            for (int i = 0; i < _mapW; i++)
            {
                for (int j = 0; j < _mapH; j++)
                {
                    Vector2 pos = new Vector2(i * _cellW, j * _cellH) + AbsXY + new Vector2(_cellW/2, 20);

                    if (_cells.Get(i, j)._unit != null)
                        GFX.CenterBorderedStringXY(batch, Game1._fontMain, $"{_cells.Get(i, j)._unit._index}", pos + new Vector2(0, 40), Color.Yellow, Color.Red);
                    //else
                    //    GFX.LeftTopBorderedString(batch, Game1._fontMain, ".", pos, Color.Yellow, Color.Red);

                    // Show passLevel in cell
                    //GFX.CenterBorderedStringXY(batch, Game1._fontMain, $"{_cells.Get(i,j)._passLevel}", pos + new Vector2(0, 80), Color.Yellow, Color.Green);
                }
            }

            if (_isMouseOverGrid)
            {
                var unit = _cells.Get(_mapCursor.X, _mapCursor.Y)._unit;
                
                if (unit != null)
                    GFX.TopCenterString(batch, Game1._fontMain, $"{unit} {unit._index}", unit._rect.TopCenter + AbsXY - Vector2.UnitY * 20, Color.Red * .75f);
                //else
                //    GFX.TopCenterString(batch, Game1._fontMain, "No Unit Here", (_mapCursor * CellSize).ToVector2() + AbsXY + unit._rect.BottomCenter, Color.Red * .25f);

            }
        }

    }
}
