using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Mugen.Core;
using Mugen.GFX;
using Mugen.Input;
using Mugen.Physics;
using System.Collections.Generic;
namespace BattleSystem
{

    public class Cell
    {
        public Unit _unit;
        public bool _isFree = true;
        public int _id = 0;
        float _alpha = 0f;
        float _acc = .025f;

        Vector2 _position = new();
        Point _mapPosition;
        Point _size;

        public bool _isMouseOver = false;
        public bool _isSelected = false;
        public Cell(Point mapPosition, Point size) 
        { 
            _mapPosition = mapPosition;
            _size = size;

            _position.X = _mapPosition.X * _size.X;
            _position.Y = _mapPosition.Y * _size.Y;
        }

        public void Update()
        {
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
        public static Node CurrentDragged;

        int _mapW;
        int _mapH;

        Point _mapCursor = new();
        Vector2 _cursor = new();

        int _cellW;
        int _cellH;

        Cell[,] _cells;

        MouseControl _mouseControl;

        Vector2 _mouse;
        bool _isMouseOver = false;

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

            _cellW = cellW;
            _cellH = cellH;

            _cells = new Cell[_cellW, _cellH];

            for (int i = 0; i < _cellW; i++)
            {
                for (int j = 0; j < _cellH; j++)
                {
                    var cell = new Cell(new Point(i,j), new Point(_cellW, _cellH));

                    cell._id = Misc.Rng.Next(1,8);

                    _cells[i, j] = cell;
                }
            }

            SetSize(_mapW * _cellW, _mapH * _cellH);

            _loop = new Addon.Loop(this);
            _loop.SetLoop(0, -1.5f, 1.5f, .25f, Mugen.Animation.Loops.PINGPONG);
            _loop.Start();
            AddAddon(_loop);

            int[] _droppables = new int[] { UID.Get<Unit>(), UID.Get<Card>() };


            _dropZoneManager = new DropZoneManager();
            _dropZoneManager.AddZone(new DropZone(new Rectangle(20, 120, 64, 64), -10, _droppables));
            _dropZoneManager.AddZone(new DropZone(new Rectangle(20, 200, 64, 64), -10, _droppables));
            _dropZoneManager.AddZone(new DropZone(new Rectangle(20, 280, 64, 64), -10, _droppables));
            _dropZoneManager.AddZone(new DropZone(new Rectangle(20, 360, 64, 64), -10, _droppables));

            _dropZoneInGrid = new DropZone(new Rectangle(0, 0, 64, 64), -10, _droppables);
            _dropZoneInGrid.Show(false);
            _dropZoneManager.AddZone(_dropZoneInGrid);
        }
        public void AddUnit(int mapX, int mapY, int sizeW, int sizeH)
        {
            var unit = new Unit(_mouseControl, this, sizeW, sizeH, _cellW, _cellH);
            unit.SetMapPosition(mapX, mapY).AppendTo(this);

            SetCellUnit(mapX, mapY, unit);
        }
        public void SetCell(int mapX, int mapY, Cell cell)
        {
            if (mapX < 0 || mapX > _mapW || mapY < 0 || mapY > _mapH)
                return;

            _cells[mapX, mapY] = cell;
        }
        public void SetCellUnit(int mapX, int mapY, Unit unit)
        {
            if (mapX < 0 || mapX > _mapW || mapY < 0 || mapY > _mapH)
                return;

            _cells[mapX, mapY]._unit = unit;
        }
        public Cell GetCell(int mapX, int mapY)
        {
            if (mapX < 0 || mapX > _mapW || mapY < 0 || mapY > _mapH)
                return null;
            
            return _cells[mapX, mapY];
        }
        public Unit GetCellUnit(int mapX, int mapY)
        {
            if (mapX < 0 || mapX > _mapW || mapY < 0 || mapY > _mapH)
                return null;

            return _cells[mapX, mapY]._unit;
        }
        private void ResetCells()
        {
            for (int i = 0; i < _cellW; i++)
            {
                for (int j = 0; j < _cellH; j++)
                {
                    var cell = _cells[i, j];

                    cell._isMouseOver = false;
                    cell._isSelected = false;

                }
            }
        }
        private void UpdateCells()
        {
            for (int i = 0; i < _cellW; i++)
            {
                for (int j = 0; j < _cellH; j++)
                {
                    var cell = _cells[i, j];

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
                    var cell = _cells[i, j];

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
            CurrentDragged = null;

            _isMouseOver = false;

            _mouse.X = _mouseControl.GetPosition().X - _x;
            _mouse.Y = _mouseControl.GetPosition().Y - _y;

            _mapCursor.X = (int)(_mouse.X / _cellW);
            _mapCursor.Y = (int)(_mouse.Y / _cellH);

            if (_mapCursor.X < 0) _mapCursor.X = 0;
            if (_mapCursor.X > _mapW - 1) _mapCursor.X = _mapW - 1;
            if (_mapCursor.Y < 0) _mapCursor.Y = 0;
            if (_mapCursor.Y > _mapH - 1) _mapCursor.Y = _mapH - 1;

            _cursor.X = _mapCursor.X * _cellW;
            _cursor.Y = _mapCursor.Y * _cellH;

            if (Misc.PointInRect(_mouse.X + _x, _mouse.Y + _y, _rect))
            {
                _isMouseOver = true;
            }



            if (Button.OnePress("RightButton", Mouse.GetState().RightButton == ButtonState.Pressed))
            {
                new Card(_game, _mouseControl).SetPosition(_mouse).AppendTo(this);
            }

            SortZAscending();
            UpdateChildsSort(gameTime);
            
            // Quand drag un Unit , on change la position du pointeur (souris) par le milieu de l'Unit qui est en train d'être dragué
            if (CurrentDragged != null)
            {
                _mouse.X = CurrentDragged._rect.Center.X;
                _mouse.Y = CurrentDragged._rect.Center.Y;

                _mapCursor.X = (int)(_mouse.X / _cellW);
                _mapCursor.Y = (int)(_mouse.Y / _cellH);

                if (_mapCursor.X < 0) _mapCursor.X = 0;
                if (_mapCursor.X > _mapW - 1) _mapCursor.X = _mapW - 1;
                if (_mapCursor.Y < 0) _mapCursor.Y = 0;
                if (_mapCursor.Y > _mapH - 1) _mapCursor.Y = _mapH - 1;

                _cursor.X = _mapCursor.X * _cellW;
                _cursor.Y = _mapCursor.Y * _cellH;
            }

            ResetCells();
            _cells[_mapCursor.X, _mapCursor.Y]._isMouseOver = true;
            UpdateCells();


            _listItems = GroupOf(new int[] { UID.Get<Card>(), UID.Get<Unit>() });
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
                Rectangle rectOriginal = new Rectangle(_cursor.ToPoint() + new Point(AbsX, AbsY), new Point(_cellW, _cellH));

                //if (GetCellUnit(_mapCursor.X, _mapCursor.Y) == null)
                    _dropZoneInGrid.UpdateZone(rectOriginal, -10);
            }

            return base.Update(gameTime);
        }
        public override Node Draw(SpriteBatch batch, GameTime gameTime, int indexLayer)
        {
            SortZDescending();


            if (indexLayer == (int)Layers.Main)
            {
                //GFX.FillRectangle(batch, AbsRect, Color.DarkBlue * .1f);

                GFX.FillRectangle(batch, AbsRect, Color.DarkBlue * .05f);
                GFX.Grid(batch, AbsXY, _rect.Width, _rect.Height, _cellW, _cellH, Color.Gray * .25f, 3);
                GFX.Grid(batch, AbsXY, _rect.Width, _rect.Height, _cellW, _cellH, Color.Black * .5f, 1);
                GFX.Rectangle(batch, AbsRect, Color.WhiteSmoke * .5f);

                _dropZoneManager.Draw(batch);

                DrawChilds(batch, gameTime, indexLayer);
                DrawCells(batch, indexLayer);

                GFX.LeftTopString(batch, Game1._fontMain, $"{_mouse} -- {_mapCursor} -- {CurrentDragged} {CurrentDragged?._index}", AbsXY + new Vector2(10, -20), Color.AntiqueWhite);

            }


            if (indexLayer == (int)Layers.FX)
            {
                DrawChilds(batch, gameTime, indexLayer);
                DrawCells(batch, indexLayer);

                if (_isMouseOver && _mouseControl._isActiveDrag)
                {
                    Rectangle rectOriginal = new Rectangle(_cursor.ToPoint() + new Point(AbsX, AbsY), new Point(_cellW, _cellH));
                    RectangleF rectCursor = ((RectangleF)rectOriginal).Extend(_loop._current);

                    GFX.FillRectangle(batch, rectCursor, Color.LightSteelBlue * .5f);
                    GFX.Rectangle(batch, rectCursor, Color.LightSteelBlue * 1f, 4f);

                    
                    //System.Console.Write("<Mouse in>");
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
                    Vector2 pos = new Vector2(i * _cellW, j * _cellH) + AbsXY + new Vector2(6, 2);

                    if (_cells[i, j]._unit != null)
                        GFX.LeftTopBorderedString(batch, Game1._fontMain, $"{_cells[i, j]._unit._index}", pos, Color.Yellow, Color.Red);
                    else
                        GFX.LeftTopBorderedString(batch, Game1._fontMain, ".", pos, Color.Yellow, Color.Red);
                }
            }

            if (_isMouseOver)
            {
                Rectangle rectOriginal = new Rectangle(_cursor.ToPoint() + new Point(AbsX, AbsY), new Point(_cellW, _cellH));
                //RectangleF rectCursor = ((RectangleF)rectOriginal).Extend(_loop._current);
                var unit = _cells[_mapCursor.X, _mapCursor.Y]._unit;
                GFX.TopCenterString(batch, Game1._fontMain, $"{unit} {unit?._index}", ((RectangleF)rectOriginal).BottomCenter, Color.Red * .75f);
            }
        }

    }
}
