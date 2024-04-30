using System;
using System.Collections.Generic;
using UnityEngine;

namespace DsTools.Tile
{
    internal interface ITile
    {
        /// <summary>
        /// 行数
        /// </summary>
        /// <value></value>
        public int Row { get; set; }

        /// <summary>
        /// 列数
        /// </summary>
        /// <value></value>
        public int Column { get; set; }

        /// <summary>
        /// 绘制部分纹理
        /// </summary>
        /// <param name="rect">绘制纹理的矩形区域</param>
        /// <param name="texCoords">纹理绘制区域</param>
        /// <param name="mapLayersToDraw">需要绘制的层</param>
        public void DrawTexture(Rect rect, Rect texCoords, MapLayer mapLayersToDraw);

        /// <summary>
        /// 绘制全部纹理
        /// </summary>
        /// <param name="rect">绘制区域</param>
        /// <param name="mapLayersToDraw">绘制大小</param>
        public void DrawTexture(Rect rect,MapLayer mapLayersToDraw);

        /// <summary>
        /// 绘制属性
        /// </summary>
        public void DrawingProperties(TileRule tileRule);

        public List<TileRule.TileInfo> GetAllTileInfo();
    }

    [Flags]
    public enum MapLayer : byte
    {
        None = 0,
        Everything = Ground | Roads | Entity | Decorations,
        [InspectorName("地面层")] Ground = 1 << 0,
        [InspectorName("道路层")] Roads = 1 << 1,
        [InspectorName("实体层")] Entity = 1 << 2,
        [InspectorName("装饰层")] Decorations = 1 << 3,
    }
}