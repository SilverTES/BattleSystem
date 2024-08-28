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
        static int _dropZoneIndex = 0;
        public readonly int _index;
        bool _isShow = true;

        public RectangleF _rectDropZone = new();

        private RectangleF _rectNearDropZone = new();
        private Node _curNodeInDropZone = null;

        bool _isNearNode = false;

        int[] _droppableTypes; // Nodes droppables in the zone

        bool _isActive = true;

        public DropZone(Rectangle rect, float nearZoneSize, int[] droppableTypes) 
        {
            _index = _dropZoneIndex++;

            _rectDropZone = rect;
            _droppableTypes = droppableTypes;
            _rectNearDropZone = _rectDropZone.Extend(nearZoneSize);
        }
        public void SetContainerNode(Node node) 
        {
            _curNodeInDropZone = node;
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
            _rectDropZone = rect;
            _rectNearDropZone = _rectDropZone.Extend(nearZoneSize);
        }

        public void Update(GameTime gameTime, List<Node> nodeToCheck)
        {
            _isNearNode = false;

            if (!_isActive)
                return;

            foreach (var item in nodeToCheck)
            {
                if (Collision2D.RectRect(item.AbsRect, _rectNearDropZone))
                {
                    _isNearNode = true;

                    for (int i = 0;  i < _droppableTypes.Length;  i++)
                    {
                        if (item._type == _droppableTypes[i])
                        {
                            var dragAndDrop = item.This<Card>();

                            if (_curNodeInDropZone == null)
                                if (!dragAndDrop.IsDropped)
                                {
                                    dragAndDrop.SetDropZone(this);
                                    dragAndDrop.IsNearDropZone(true);
                                }
                                else
                                {
                                    dragAndDrop.SetDropZone(this);
                                    SetContainerNode(item);
                                }

                        }
                    }

                }

            }

            if (_curNodeInDropZone != null)
            {
                if (_curNodeInDropZone._rect.TopLeft + _curNodeInDropZone._parent.XY != _rectDropZone.TopLeft) // Test if itemXY is left the zoneXY and dropZone contain a Node !
                {
                    _curNodeInDropZone = null;
                    //Console.Write("<Retired contained Node>");
                }
            }
        }

        public void Draw(SpriteBatch batch, float alpha = 1f)
        {
            if (_isShow)
            {
                Color color = Color.Violet;

                if (!_isActive) color = Color.Black;

                GFX.Rectangle(batch, _rectDropZone, color * .75f * alpha, 4f);
                GFX.Rectangle(batch, _rectNearDropZone, color * .25f * alpha, 2f);

                if (_isNearNode)
                    GFX.Rectangle(batch, _rectNearDropZone, color * .5f * alpha, 2f);

                if (_curNodeInDropZone != null)
                {
                    GFX.CenterStringXY(batch, Game1._fontMain, $"{_curNodeInDropZone}{_curNodeInDropZone._index}", _rectDropZone.TopCenter + new Vector2(0, -10), Color.Gold);
                    GFX.Rectangle(batch, _rectDropZone, Color.Black * .75f * alpha, 4f);
                }
                else
                {
                    GFX.CenterStringXY(batch, Game1._fontMain, "_containerNode == null", _rectDropZone.TopCenter + new Vector2(0, -10), Color.Gold);
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
        public void Update(GameTime gameTime, Node nodeContainDroppables, int[] droppableTypes)
        {
            var nodeToCheck = nodeContainDroppables.GroupOf(droppableTypes);

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
