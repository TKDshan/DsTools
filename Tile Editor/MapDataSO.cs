using System.Collections.Generic;
using UnityEngine;

namespace DsTools.Tile
{
    public abstract class MapDataSO : ScriptableObject, ISerializationCallbackReceiver
    {
#if UNITY_EDITOR

        [SerializeField, HideInInspector]
        internal SerializedDictionary<(int row, int column), Tile> mapData =
            new SerializedDictionary<(int row, int column), Tile>();

        [SerializeField, HideInInspector] private List<Tile> list = new List<Tile>();

        internal bool isFold = true;

        internal float gridScale = 1;

        internal Vector2 gridOffset;

        internal Vector2 centerScalePoint;
#endif
        public abstract void Parse();

        /// <summary>
        /// 编辑中绘制内容
        /// </summary>
        public virtual void DrawFoldContent()
        {
#if UNITY_EDITOR
            GUILayout.BeginHorizontal();

            GUILayout.Label("网格数量", GUILayout.Width(100));
            GUILayout.Label(mapData.Count.ToString(), GUILayout.Width(60));

            GUILayout.EndHorizontal();
#endif
        }

        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            list.Clear();

            foreach (var tile in mapData)
            {
                list.Add(tile.Value);
            }
#endif
        }

        public void OnAfterDeserialize()
        {
#if UNITY_EDITOR
            mapData.Clear();

            foreach (var tile in list)
            {
                mapData.Add((tile.Row, tile.Column), tile);
            }

            list.Clear();
#endif
        }
    }
}