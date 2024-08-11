﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mugen.AI;
using Mugen.Core;
using Mugen.GFX;
using Mugen.Input;
using Mugen.Physics;
using System.Collections.Generic;


namespace BattleSystem
{
    public class Cell : BaseTile
    {

        Arena _arena;

        public Unit _unit;
        public bool _isFree = true;
        public int _id = 0;
        float _alpha = 0f;
        float _acc = .025f;

        Vector2 _position = new();
        Point _mapPosition = new();
        Point _size = new();

        public bool _isMouseOver = false;
        public bool _isSelected = false;

        Rectangle _rect;

        public Cell()
        {
            _passLevel = 0;
        }
        public Cell(Arena arena, Point mapPosition, Point size) 
        {
            _arena = arena;
            _mapPosition = mapPosition;
            _size = size;

            _position.X = _mapPosition.X * _size.X;
            _position.Y = _mapPosition.Y * _size.Y;
            _rect = new Rectangle((int)_position.X, (int)_position.Y,size.X,size.Y);

            _passLevel = 0;

        }

        public void Update()
        {
            _isMouseOver = false;
            _isSelected = false;

            if (Misc.PointInRect(_arena._mouseControl.GetPosition() - _arena.XY.ToPoint(), _rect))
            {
                _isMouseOver = true;
            }

            if (_isMouseOver)
            {
                _alpha = 1f;
                _acc = .025f;
            }
            else
            {
                _acc += .002f;
                _alpha -= _acc;
                if (_alpha <= 0) 
                    _alpha = 0;
            }
        }
        public void Draw(SpriteBatch batch, Point arenaTopLeft, int indexLayer)
        {
            if (indexLayer == (int)Layers.FX)
            {
                Rectangle rectCursor = new Rectangle(_position.ToPoint() + arenaTopLeft, _size);

                //if (!_isMouseOver)
                {
                    GFX.FillRectangle(batch, RectangleF.Extend(rectCursor, -(1-_alpha)*20f), Color.White * _alpha * .5f);
                    GFX.Rectangle(batch, RectangleF.Extend(rectCursor, -(1-_alpha)*20f), Color.White * _alpha * .5f);

                    //GFX.Draw(batch, Game1._texGlow0, Color.White * _alpha * .5f, 0, _position + arenaTopLeft.ToVector2() + (_size.ToVector2() /2), Game1._texGlow0.Bounds.Size.ToVector2() / 2, Vector2.One * _alpha * .03f);
                }

                if (_isSelected) 
                { 
                    GFX.FillRectangle(batch, rectCursor, Color.DarkSlateBlue * .5f);
                }
            }
            if (indexLayer == (int)Layers.Main)
            {

            }
        }

    }

    public class Arena : Node
    {
        enum State
        {
            PLAYER_PHASE,
            ENEMY_PHASE,
            LAST
        }

        public static Unit CurrentDragged;

        int _mapW;
        int _mapH;

        RectangleF _rectZone;

        public Point MapSize { get; private set; }


        Point _mapCursor = new();
        Vector2 _cursor = new();
        RectangleF _rectCursor;

        int _cellW;
        int _cellH;

        public Point CellSize { get; private set; }

        Map2D<Cell> _cells;

        public MouseControl _mouseControl;

        Vector2 _mouse;
        public bool _isMouseOver = false;

        Addon.Loop _loop;

        Game1 _game;

        DropZoneManager _dropZoneManager;
        DropZone _dropZoneInGrid;
        
        List<Node> _listItems;
        
        public Arena(Game1 game, MouseControl mouseEvent, int mapW, int mapH, int cellW = 32, int cellH = 32) 
        { 
            _game = game;
            _mouseControl = mouseEvent;

            _mapW = mapW;
            _mapH = mapH;
            MapSize = new Point(mapW, mapH);

            _cells = new Map2D<Cell>(mapW, mapH);

            _rectZone = new RectangleF(_rect.X, _rect.Y, _rect.Width, _rect.Height);

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
            //_dropZoneManager.AddZone(new DropZone(new Rectangle(20, 120 + 180 * 0, _cellW, _cellH), -10, _droppables));
            //_dropZoneManager.AddZone(new DropZone(new Rectangle(20, 120 + 180 * 1, _cellW, _cellH), -10, _droppables));
            //_dropZoneManager.AddZone(new DropZone(new Rectangle(20, 120 + 180 * 2, _cellW, _cellH), -10, _droppables));
            //_dropZoneManager.AddZone(new DropZone(new Rectangle(20, 120 + 180 * 3, _cellW, _cellH), -10, _droppables));
            //_dropZoneManager.AddZone(new DropZone(new Rectangle(20, 120 + 180 * 4, _cellW, _cellH), -10, _droppables));

            _dropZoneInGrid = new DropZone(new Rectangle(0, 0, _cellW, _cellH), -10, _droppables);
            _dropZoneInGrid.Show(false);
            _dropZoneManager.AddZone(_dropZoneInGrid);
        }
        public void InitAllCells()
        {
            for (int i = 0; i < _cellW; i++)
            {
                for (int j = 0; j < _cellH; j++)
                {
                    var cell = new Cell(this, new Point(i, j), new Point(_cellW, _cellH));
                    _cells.Put(i, j, cell);
                    //_cells[i][j] = cell;
                }
            }
        }
        public void AddUnit(int mapX, int mapY, int sizeW, int sizeH)
        {
            var unit = new Unit(_mouseControl, this, sizeW, sizeH, _cellW, _cellH);
            unit.SetMapPosition(mapX, mapY).AppendTo(this);

            SetCellUnit(mapX, mapY, unit);
        }
        public List<List<Cell>> GetMap()
        {
            return _cells.GetMap();
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
        public Unit GetCellUnit(int mapX, int mapY)
        {
            if (mapX < 0 || mapX > _mapW || mapY < 0 || mapY > _mapH)
                return null;

            var cell = _cells.Get(mapX, mapY);
            
            if (cell != null)
                return cell._unit;

            return null;
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


            _rectZone.X = _rect.X;
            _rectZone.Y = _rect.Y;

            if (CurrentDragged != null)
            {
                // Quand drag un Unit , on change la position du pointeur (souris) par le milieu de l'Unit qui est en train d'être dragué
                _mouse.X = CurrentDragged._rect.TopLeft.X + _cellW/2;
                _mouse.Y = CurrentDragged._rect.TopLeft.Y + _cellH/2;

                _rectZone.Width =  _rect.Width - ((CurrentDragged.Size.X-1)*_cellW) + _cellW/2;
                _rectZone.Height = _rect.Height - ((CurrentDragged.Size.Y-1)*_cellH) + _cellH/2;
            }
            else
            {
                _mouse.X = _mouseControl.GetPosition().X - _x;
                _mouse.Y = _mouseControl.GetPosition().Y - _y;
            }

            _isMouseOver = Misc.PointInRect(_mouse.X + _x + _cellW/2, _mouse.Y + _y + _cellH/2, _rectZone);

            _mapCursor.X = (int)(_mouse.X / _cellW);
            _mapCursor.Y = (int)(_mouse.Y / _cellH);

            if (_mapCursor.X < 0) _mapCursor.X = 0;
            if (_mapCursor.X > _mapW - 1) _mapCursor.X = _mapW - 1;
            if (_mapCursor.Y < 0) _mapCursor.Y = 0;
            if (_mapCursor.Y > _mapH - 1) _mapCursor.Y = _mapH - 1;

            _cursor.X = _mapCursor.X * _cellW;
            _cursor.Y = _mapCursor.Y * _cellH;


            CurrentDragged = null;
            SortZAscending();
            UpdateChildsSort(gameTime);
            
            //ResetCells();
            //_cells[_mapCursor.X, _mapCursor.Y]._isMouseOver = true;

            UpdateCells();

            _listItems = GroupOf(new int[] { UID.Get<Unit>() });
            // reset focus
            if (_mouseControl._onClick)
            {
                foreach (Node node in _listItems)
                {
                    //Node node = node.This<Card>();

                    if (!node._navi._isMouseOver)// && !node._resizable._isMouseOver)
                        node._navi._isFocus = false;
                }
            }

            _dropZoneManager.Update(gameTime, _listItems);

            // Manage Drag & Drop Zone
            if (_isMouseOver && _mouseControl._down)
            {
                if (CurrentDragged != null)
                    _rectCursor = new RectangleF(_cursor.ToPoint() + new Point(AbsX, AbsY), new Size2(CurrentDragged._rect.Width, CurrentDragged._rect.Height));
                else
                    _rectCursor = new RectangleF(_cursor.ToPoint() + new Point(AbsX, AbsY), new Size2(_cellW, _cellH));

                //if (GetCellUnit(_mapCursor.X, _mapCursor.Y) == null)
                    _dropZoneInGrid.UpdateZone(_rectCursor, -10);
            }

            return base.Update(gameTime);
        }
        public override Node Draw(SpriteBatch batch, GameTime gameTime, int indexLayer)
        {
            SortZDescending();

            if (indexLayer == (int)Layers.Main)
            {
                //GFX.FillRectangle(batch, AbsRect, Color.DarkBlue * .1f);


                GFX.FillRectangle(batch, AbsRect, Color.DarkBlue * .2f);
                GFX.Grid(batch, AbsXY, _rect.Width, _rect.Height, _cellW, _cellH, Color.Gray * .25f, 3);
                GFX.Grid(batch, AbsXY, _rect.Width, _rect.Height, _cellW, _cellH, Color.Black * .5f, 1);
                GFX.Rectangle(batch, AbsRect, Color.WhiteSmoke * .5f);

                _dropZoneManager.Draw(batch);

                DrawChilds(batch, gameTime, indexLayer);
                DrawCells(batch, indexLayer);

                GFX.LeftTopString(batch, Game1._fontMain, $"{_mouse} -- {_mapCursor} -- {CurrentDragged} {CurrentDragged?._index} {_isMouseOver}", AbsXY + new Vector2(10, -20), Color.AntiqueWhite);

                // Debug Zone when is Drag Unit is possible to drop !!
                //if (!_isMouseOver) 
                //    GFX.Rectangle(batch, _rectZone, Color.Red, 4f);

            }

            if (indexLayer == (int)Layers.FX)
            {

                DrawChilds(batch, gameTime, indexLayer);
                DrawCells(batch, indexLayer);

                if (_mouseControl._isActiveDrag)
                {
                    RectangleF rectCursorExtend = ((RectangleF)_rectCursor).Extend(_loop._current);

                    Color color = Color.ForestGreen;

                    if (CurrentDragged != null)
                    {
                        if (!CurrentDragged.IsPossibleToDrop)
                        {
                            color = Color.Red;
                        }
                        
                        GFX.FillRectangle(batch, CurrentDragged.PrevPosition + AbsXY - (Vector2.One*(_loop._current - 20)), CurrentDragged.AbsRectF.GetSize() + (Vector2.One*2*(_loop._current - 20)), Color.Yellow * .75f);
                    }

                    if (_isMouseOver)
                    {
                        GFX.FillRectangle(batch, rectCursorExtend, color * .75f);
                        GFX.Rectangle(batch, rectCursorExtend, color * .5f, 4f);
                    }
                }

            }

            if (indexLayer == (int)Layers.Debug)
            {
                ShowValue(batch);

                DrawChilds(batch, gameTime, indexLayer);
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

            if (_isMouseOver)
            {
                var unit = _cells.Get(_mapCursor.X, _mapCursor.Y)._unit;
                
                if (unit != null)
                    GFX.TopCenterString(batch, Game1._fontMain, $"{unit} {unit._index}", unit._rect.BottomCenter + AbsXY, Color.Red * .75f);
                //else
                //    GFX.TopCenterString(batch, Game1._fontMain, "No Unit Here", (_mapCursor * CellSize).ToVector2() + AbsXY + unit._rect.BottomCenter, Color.Red * .25f);

            }
        }

    }
}
