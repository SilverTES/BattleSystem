using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mugen.Core;
using Mugen.GFX;
using Mugen.Physics;
using System;
using System.Collections.Generic;

namespace BattleSystem
{
    public class CellChain
    {
        public int _type;
        public bool _isSelected = false;
        public Point _mapPosition = new Point();
    }


    public class ChainGrid : Node
    {
        List<Color> _colors = new List<Color>() 
        { 
            Color.Black,
            Color.Red,
            Color.Green, 
            Color.Blue, 
            Color.Yellow,
            Color.Violet,
            Color.Turquoise,

        };

        Point _cellSize;
        Point _mapSize;
        Point _sizeOfGrid;
        Point _mapCursor = new();
        
        Vector2 _cursor = new();
        Vector2 _mouse = new();
        RectangleF _rectCursor;

        List2D<CellChain> _grid;

        bool _isMouseOver = false;

        bool _isBeginChain = false;
        bool _onBeginChain = false;

        int _firstSelected = 0;
        List<CellChain> _listCells = new List<CellChain>();
        int _indexChain = 0;

        public ChainGrid(Point gridSize, Point cellSize) 
        {
            _mapSize = gridSize;
            _cellSize = cellSize;

            _sizeOfGrid = _mapSize * cellSize;
            SetSize(_sizeOfGrid.X, _sizeOfGrid.Y);

            _grid = new(gridSize.X, gridSize.Y);

            _rectCursor = new RectangleF(0,0,_cellSize.X, _cellSize.Y);

            Init();
        }
        public override Node Init()
        {
            InitAllCell();
            return base.Init();
        }
        public void InitAllCell()
        {
            for (int i = 0; i < _mapSize.X; i++)
            {
                for (int j = 0; j < _mapSize.Y; j++)
                {
                    CellChain cell = new();
                    cell._type = Misc.Rng.Next(1, 7);
                    cell._mapPosition.X = i;
                    cell._mapPosition.Y = j;
                    
                    _grid.Put(i, j, cell);
                }
            }
        }
        public void ResetAllCell(int type = -1)
        {
            if (_listCells != null)
                if (_listCells.Count > 0)
                    _listCells.Clear();

            for (int i = 0; i < _mapSize.X; i++)
            {
                for (int j = 0; j < _mapSize.Y; j++)
                {
                    CellChain cell = _grid.Get(i, j);
                    
                    if (type != -1)
                        cell._type = type;

                    cell._isSelected = false;
                    _grid.Put(i, j, cell);

                }
            }
        }
        public bool IsCellNear(CellChain cellA, CellChain cellB)
        {
            return !(Math.Abs(cellA._mapPosition.X - cellB._mapPosition.X) > 1 || Math.Abs(cellA._mapPosition.Y - cellB._mapPosition.Y) > 1);
        }
        public override Node Update(GameTime gameTime)
        {
            _onBeginChain = false;

            UpdateRect();

            _mouse.X = Game1.MouseControl.GetPosition().X - _x;
            _mouse.Y = Game1.MouseControl.GetPosition().Y - _y;

            _mapCursor.X = (int)Math.Floor(_mouse.X / _cellSize.X);
            _mapCursor.Y = (int)Math.Floor(_mouse.Y / _cellSize.Y);

            _cursor.X = _mapCursor.X * _cellSize.X;
            _cursor.Y = _mapCursor.Y * _cellSize.Y;

            _isMouseOver = Misc.PointInRect(_mouse.X + _x, _mouse.Y + _y, AbsRectF);

            _rectCursor.X = _cursor.X + AbsX;
            _rectCursor.Y = _cursor.Y + AbsY;

            if (Game1.MouseControl._isClick && _isMouseOver)
            {
                if (_isMouseOver && !_isBeginChain)
                {

                    var cell = _grid.Get(_mapCursor.X, _mapCursor.Y);
                    
                    if (cell != null)
                    {
                        if (cell._type > 0)
                        {
                            _firstSelected = cell._type;
                            cell._isSelected = true;
                            _onBeginChain = true;
                            _isBeginChain = true;

                            _listCells.Add(cell);

                            Game1._soundClock.Play(.5f, Math.Clamp(.1f * _indexChain, 0, 1f), .5f);
                        }
                    }


                }


                if (_isBeginChain)
                {
                    var cell = _grid.Get(_mapCursor.X, _mapCursor.Y);
                    if (cell != null)
                    {
                        CellChain prevCell = _listCells[_indexChain];
                        
                        if ((cell._type == prevCell._type || cell._type == prevCell._type + 1 ))
                        {

                            //if (prevCell == cell && cell._isSelected)
                            //{
                            //    _listCells.Remove(prevCell);
                            //    _indexChain--;
                            //}

                            if (IsCellNear(prevCell, cell) && !cell._isSelected)
                            {
                                cell._isSelected = true;

                                _listCells.Add(cell);
                                _indexChain++;

                                Game1._soundClock.Play(.5f, Math.Clamp(.1f * _indexChain, 0, 1f), .5f);
                            }


                        }
                    }
                }
            }
            else
            {
                if (_listCells.Count > 1) 
                {
                    for (int i = 0; i < _listCells.Count; i++)
                    {
                        _listCells[i]._type = 0;
                    }
                }

                _indexChain = 0;
                _isBeginChain = false;
                _firstSelected = 0;
                ResetAllCell();

            }


            


            return base.Update(gameTime);
        }
        public override Node Draw(SpriteBatch batch, GameTime gameTime, int indexLayer)
        {
            if (indexLayer == (int)Layers.Main)
            {
                GFX.Grid(batch, AbsXY, _sizeOfGrid.X, _sizeOfGrid.Y, _cellSize.X, _cellSize.Y, Color.Orange * .1f, 3f);
                GFX.Rectangle(batch, AbsRect, _isMouseOver?Color.MonoGameOrange:Color.Gray, 2f);

                if (_isMouseOver)
                {
                    GFX.Rectangle(batch, _rectCursor, Color.GreenYellow, 2f);
                }

                for (int i = 0; i < _mapSize.X; i++)
                {
                    for (int j = 0; j < _mapSize.Y; j++)
                    {
                        float x = _cellSize.X * i + AbsX;
                        float y = _cellSize.Y * j + AbsY;

                        Color color = _colors[_grid.Get(i, j)._type];

                        GFX.Point(batch, new Vector2(x, y) + Vector2.UnitX * (_cellSize.X/2) + Vector2.UnitY * (_cellSize.Y/2), 32, color);

                        if (_grid.Get(i,j)._isSelected)
                            GFX.FillRectangle(batch, new RectangleF(x,y,_cellSize.X, _cellSize.Y), Color.White * .5f);

                        GFX.CenterBorderedStringXY(batch, Game1._fontMain, $"{_grid.Get(i, j)._type}", i * _cellSize.X + AbsX + _cellSize.X / 2, j * _cellSize.Y + AbsY + _cellSize.Y / 2, Color.White, Color.Black);
                    }
                }

                if (_listCells.Count > 0)
                    for (int i = 0; i < _listCells.Count; i++)
                    {
                        Vector2 p1 = new Vector2();
                        Vector2 p2 = new Vector2();

                        p1.X = _listCells[i]._mapPosition.X * _cellSize.X + AbsX + _cellSize.X / 2;
                        p1.Y = _listCells[i]._mapPosition.Y * _cellSize.Y + AbsY + _cellSize.Y / 2;


                        if (i < _listCells.Count-1)
                        {
                            p2.X = _listCells[i+1]._mapPosition.X * _cellSize.X + AbsX + _cellSize.X / 2;
                            p2.Y = _listCells[i+1]._mapPosition.Y * _cellSize.Y + AbsY + _cellSize.Y / 2;
                        }
                        else
                            p2 = p1;

                        GFX.Line(batch, p1, p2, Color.Yellow * .5f, 8f);

                        GFX.LeftTopString(batch, Game1._fontMain, $"{_listCells[i]._type}", 320, i * 40 + 40, Color.OrangeRed);
                    }

            }

            if (indexLayer == (int)Layers.Debug)
            {
                //for (int i = 0; i < _mapSize.X; i++)
                //{
                //    for (int j = 0; j < _mapSize.Y; j++)
                //    {
                //        GFX.CenterStringXY(batch, Game1._fontMain, $"{_grid.Get(i,j)._type}", i * _cellSize.X + AbsX + _cellSize.X/2, j * _cellSize.Y + AbsY + _cellSize.Y/2, Color.Gold * .75f);
                //    }
                //}

                GFX.CenterStringXY(batch, Game1._fontMain, $"{_mapCursor} : {_firstSelected}",AbsRectF.TopCenter + Vector2.UnitY * -20, Color.Gold * .75f);

            }

            return base.Draw(batch, gameTime, indexLayer);
        }

    }
}
