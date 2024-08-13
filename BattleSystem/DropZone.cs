using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mugen.Core;
using Mugen.GFX;
using Mugen.Physics;
using System.Collections.Generic;

namespace BattleSystem
{
    public class DropZone
    {
        bool _isShow = true;

        public RectangleF _rect = new();

        public RectangleF _rectNear = new();
        public Node _nearNode = null;
        public Node _containedNode = null;

        public bool _isNearNode = false;

        int[] _droppablesType; // Nodes droppables in the zone

        bool _isActive = true;

        public DropZone(Rectangle rect, float nearZoneSize, int[] droppables) 
        { 
            _rect = rect;
            _droppablesType = droppables;
            _rectNear = _rect.Extend(nearZoneSize);
        }

        public void Show(bool isShow)
        { 
            _isShow = isShow;
        }
        public void SetActive(bool isActive)
        {
            _isActive = isActive;
        }
        public void UpdateZone(RectangleF rect, float nearZoneSize) 
        {
            _rect = rect;
            _rectNear = _rect.Extend(nearZoneSize);
        }

        public void Update(GameTime gameTime, List<Node> nodeToCheck)
        {
            _isNearNode = false;

            if (!_isActive)
                return;

            foreach (var item in nodeToCheck)
            {
                if (Collision2D.RectRect(item.AbsRect, _rectNear))
                {
                    _isNearNode = true;

                    for (int i = 0;  i < _droppablesType.Length;  i++)
                    {
                        if (item._type == _droppablesType[i])
                        {
                            if (item._type == UID.Get<Unit>())
                            {
                                var unit = item.This<Unit>();
                                
                                if (!unit._isDropped)
                                {
                                    if (_containedNode == null)
                                    {
                                        unit._dropZone = this;
                                        unit._isDroppable = true;
                                    }

                                }

                            }
                        }
                    }

                }

            }

            if (_containedNode != null)
            {
                if ((_containedNode._rect + _containedNode._parent.XY != _rect)) // Test if item is left the zone and dropZone contain a Node !
                {
                    _containedNode = null;
                    //Console.Write("<Retired contained Node>");
                }
            }

            if (!_isNearNode)
            {
                _nearNode = null;
                _containedNode = null;
            }
        }

        public void Draw(SpriteBatch batch, float alpha = 1f)
        {
            if (_isShow)
            {
                Color color = Color.Violet;

                if (!_isActive) color = Color.Black;

                GFX.Rectangle(batch, _rect, color * .75f * alpha, 4f);
                GFX.Rectangle(batch, _rectNear, color * .25f * alpha, 2f);

                if (_isNearNode)
                    GFX.Rectangle(batch, _rectNear, color * .5f * alpha, 2f);

                if (_containedNode != null)
                {
                    GFX.CenterStringXY(batch, Game1._fontMain, $"{_containedNode}{_containedNode._index}", _rect.TopCenter + new Vector2(0, -10), Color.Gold);
                    GFX.Rectangle(batch, _rect, Color.Black * .75f * alpha, 4f);
                }
                
            }

        }
    }

    public class DropZoneManager
    {
        List<DropZone> _zones = new List<DropZone>();
        public DropZoneManager()
        {

        }
        public void AddZone(DropZone zone) 
        { 
            _zones.Add(zone);
        }
        public void Update(GameTime gameTime, List<Node> nodeToCheck)
        {
            foreach (var item in nodeToCheck)
            {
                if (item._type == UID.Get<Unit>())
                item.This<Unit>()._isDroppable = false;
            }

            for (int i = 0; i < _zones.Count; i++)
            {
                _zones[i].Update(gameTime, nodeToCheck);

            }

        }

        public void Draw(SpriteBatch batch)
        {
            for (int i = 0; i < _zones.Count; i++)
            {
                _zones[i].Draw(batch);
            }
        }
    }

}
