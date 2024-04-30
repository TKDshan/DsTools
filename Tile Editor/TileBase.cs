using System;
using System.Collections.Generic;
using UnityEngine;

namespace DsTools.Tile
{
    /// <summary>
    /// 瓦片基类
    /// </summary>
    [Serializable]
    internal abstract class TileBase : ITile
    {
        [SerializeField] private int _row;

        [SerializeField] private int _column;

        public virtual int Row
        {
            get => _row;
            set => _row = value;
        }

        public virtual int Column
        {
            get => _column;
            set => _column = value;
        }

        public SerializedDictionary<byte, TileRule.TileInfo> tileInfoDic =
            new SerializedDictionary<byte, TileRule.TileInfo>();

        /// <summary>
        /// 获取所有的瓦片信息
        /// </summary>
        /// <returns></returns>
        public List<TileRule.TileInfo> GetAllTileInfo()
        {
            List<TileRule.TileInfo> tileInfoList = new List<TileRule.TileInfo>();

            foreach (var tileInfo in tileInfoDic)
            {
                tileInfoList.Add(tileInfo.Value);
            }

            return tileInfoList;
        }

        public virtual void DrawingProperties(TileRule tileRule)
        {
#if UNITY_EDITOR

#endif
        }

        public virtual void DrawTexture(Rect tileRect, Rect intersection, MapLayer mapLayersToDraw)
        {
#if UNITY_EDITOR
            if (tileInfoDic == null || tileInfoDic.Count == 0)
            {
                return;
            }

            foreach (var tileInfo in tileInfoDic)
            {
                MapLayer mapLayer = (MapLayer)tileInfo.Key;

                bool isDraw = mapLayersToDraw != MapLayer.None && (mapLayersToDraw & mapLayer) == mapLayer;

                if (isDraw && tileInfo.Value.texture is not null)
                {
                    Rect texCoords = new Rect(
                        (intersection.x - tileRect.x) / tileRect.width,
                        (intersection.y - tileRect.y) / tileRect.height,
                        intersection.width / tileRect.width,
                        intersection.height / tileRect.height
                    );

                    GUI.DrawTextureWithTexCoords(intersection, tileInfo.Value.texture, texCoords);
                }
            }
#endif
        }

        public void DrawTexture(Rect rect, MapLayer mapLayersToDraw)
        {
#if UNITY_EDITOR
            if (tileInfoDic == null || tileInfoDic.Count == 0)
            {
                return;
            }

            foreach (var tileInfo in tileInfoDic)
            {
                MapLayer mapLayer = (MapLayer)tileInfo.Key;

                bool isDraw = mapLayersToDraw != MapLayer.None && (mapLayersToDraw & mapLayer) == mapLayer;

                if (isDraw && tileInfo.Value.texture is not null)
                {
                    GUI.DrawTexture(rect, tileInfo.Value.texture, ScaleMode.StretchToFill);
                }
            }
#endif
        }
    }
}