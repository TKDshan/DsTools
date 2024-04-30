using UnityEngine;

namespace DsTools.Tile
{
    internal class TileConfig : ScriptableObject
    {
        [Header("默认网格数量")] public int gridCount = 200;
        [Header("默认网格大小")] public int gridSize = 32;
        [Header("滑动范围")] public Vector2 gridScaleRange = new Vector2(0.5f,3f);
        [Header("默认网格颜色")] public Color gridColor = Color.gray;
        [Header("默认选择颜色")] public Color selectionColor = Color.cyan;
        [Header("默认选中模式")] public SelectionMode selectionMode = SelectionMode.Add;

#if UNITY_EDITOR
        [UnityEditor.MenuItem("DsTools/瓦片地图编辑器配置")]
        public static void CreateTileConfig()
        {
            TileEditorUtilities.CreateScriptableObject<TileConfig>(TileEditorUtilities.TileConfigPath);
        }
#endif
    }
    
    public enum SelectionMode
    {
        None,
        [InspectorName("添加")]
        Add,
        [InspectorName("移除")]
        Remove,
        [InspectorName("重选")]
        Replace,
        [InspectorName("矩形")]
        RectAdd
    }
}