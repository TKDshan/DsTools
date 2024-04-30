using System;
using UnityEngine;

namespace DsTools.Tile
{
    /// <summary>
    /// 瓦片
    /// </summary>
    [Serializable]
    internal sealed class Tile : TileBase
    {
        public Tile(int row,int column)
        {
            Row = row;
            Column = column;
        }

        public override void DrawingProperties(TileRule tileRule)
        {

        }
        
        
    }
}

