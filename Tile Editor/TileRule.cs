using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace DsTools.Tile
{
    /// <summary>
    /// 瓦片规则
    /// </summary>
    internal class TileRule : ScriptableObject
    {
        /// <summary>
        /// 瓦片层字典
        /// </summary>
        public SerializedDictionary<byte, TileLayer> _tileLayerDic = new SerializedDictionary<byte, TileLayer>();

        public string[] layerArray;

#if UNITY_EDITOR
        [MenuItem("DsTools/瓦片规则配置")]
        public static void CreateTileRule()
        {
            TileEditorUtilities.CreateScriptableObject<TileRule>(TileEditorUtilities.TileRulePath);
        }
#endif


        public bool Add(byte layer, TileLayer tileLayer)
        {
            return _tileLayerDic.Add(layer, tileLayer);
        }

        public bool Remove(byte layer)
        {
            if (_tileLayerDic.Contains(layer))
            {
                _tileLayerDic[layer].Destroy();
            }

            return _tileLayerDic.Remove(layer);
        }

        /// <summary>
        /// 寻找TileInfo
        /// </summary>
        /// <param name="intactName">完整的名字</param>
        /// <returns></returns>
        public TileInfo FindTile(string intactName)
        {
            string[] str = intactName.Split(':');

            TileLayer tileLayer = _tileLayerDic[byte.Parse(str[0])];

            if (tileLayer is not null)
            {
                return tileLayer.FindTile(str);
            }

            return null;
        }

        /// <summary>
        /// 寻找对应组的下标
        /// </summary>
        /// <param name="intactName">完整名字</param>
        /// <returns></returns>
        public int IndexOf(string intactName)
        {
            string[] str = intactName.Split(':');

            if (str.Length != 3) return -1;

            return _tileLayerDic[byte.Parse(str[0])].IndexOf(intactName);
        }

        /// <summary>
        /// 瓦片层
        /// </summary>
        [Serializable]
        public class TileLayer
        {

            public string layerName;
            public SerializedDictionary<string, TileGroup> _tileGroup = new SerializedDictionary<string, TileGroup>();
            public string groupName;
            [NonSerialized] public string[] tileNames;

#if UNITY_EDITOR
            public bool Fold
            {
                get => EditorPrefs.GetBool($"Layer_{layerName}", true);
                set => EditorPrefs.SetBool($"Layer_{layerName}", value);
            }
#endif

            public TileLayer(string layerName)
            {
                this.layerName = layerName;
#if UNITY_EDITOR
                EditorPrefs.SetBool($"Layer_{this.layerName}", true);
#endif
            }

            /// <summary>
            /// 获取当前层的所有瓦片名字
            /// </summary>
            /// <returns></returns>
            public string[] GetCurrentLayerAllTileInfoName()
            {
                List<string> names = new List<string>();

                foreach (var tileGroup in _tileGroup)
                {
                    foreach (var tileInfo in tileGroup.Value.tileList)
                    {
                        names.Add(tileInfo.IntactName());
                    }
                }

                tileNames = names.ToArray();
                return tileNames;
            }

            /// <summary>
            /// 根据名字找到数据下标
            /// </summary>
            /// <param name="intactName">完整的名字</param>
            /// <returns></returns>
            public int IndexOf(string intactName)
            {
                if (tileNames is null)
                    GetCurrentLayerAllTileInfoName();

                if (tileNames is not null)
                {
                    for (int i = 0; i < tileNames.Length; i++)
                    {
                        if (intactName.Equals(tileNames[i]))
                        {
                            return i;
                        }
                    }
                }

                return -1;
            }

            /// <summary>
            /// 寻找TileInfo
            /// </summary>
            /// <param name="str">完整的名字分割后的数组</param>
            /// <returns></returns>
            public TileInfo FindTile(string[] str)
            {
                TileGroup tileGroup = _tileGroup[str[1]];

                if (tileGroup is not null)
                {
                    return tileGroup.FindTile(str);
                }

                return null;
            }

            public void Destroy()
            {
#if UNITY_EDITOR
                EditorPrefs.DeleteKey($"Layer_{layerName}");
#endif
            }

            public bool Add(string groupName, TileGroup tileGroup)
            {
                return _tileGroup.Add(groupName, tileGroup);
            }

            public bool Remove(string groupName)
            {
                if (_tileGroup.Contains(groupName))
                {
                    _tileGroup[groupName].Destroy();
                }

                return _tileGroup.Remove(groupName);
            }
        }

        /// <summary>
        /// 瓦片组
        /// </summary>
        [Serializable]
        public class TileGroup
        {
            public string groupName;
            public List<TileInfo> tileList = new List<TileInfo>();

#if UNITY_EDITOR
            public bool Fold
            {
                get => EditorPrefs.GetBool($"Group_{groupName}", true);
                set => EditorPrefs.SetBool($"Group_{groupName}", value);
            }
#endif

            public TileGroup(string groupName)
            {
                this.groupName = groupName;
#if UNITY_EDITOR
                EditorPrefs.SetBool($"Group_{groupName}", true);
#endif
            }

            /// <summary>
            /// 寻找TileInfo
            /// </summary>
            /// <param name="str">完整的名字分割后的数组</param>
            /// <returns></returns>
            public TileInfo FindTile(string[] str)
            {
                foreach (var tile in tileList)
                {
                    if (tile.tileName.Equals(str[2]))
                    {
                        return tile;
                    }
                }

                return null;
            }

            public void Destroy()
            {
#if UNITY_EDITOR
                EditorPrefs.DeleteKey($"Group_{groupName}");
#endif
            }
        }

        /// <summary>
        /// 瓦片信息
        /// </summary>
        [Serializable]
        public class TileInfo : IComparable<TileInfo>
        {
            /// <summary>
            /// 隶属哪个层
            /// </summary>
            public byte layer;

            /// <summary>
            /// 隶属哪个组
            /// </summary>
            public string groupName;

            /// <summary>
            /// 瓦片名字
            /// </summary>
            public string tileName;

            /// <summary>
            /// 瓦片ID对应实体表实体ID
            /// </summary>
            public int tileID;

            [SerializeField]
            private int onlyID;

            /// <summary>
            /// 瓦片纹理
            /// </summary>
            public Texture texture;

            public string IntactName()
            {
                return $"{layer}:{groupName}:{tileName}";
            }

            public int CompareTo(TileInfo other)
            {
                return tileID.CompareTo(other.tileID);
            }

            public TileInfo(byte layer, int tileID, int onlyID, string groupName, string tileName)
            {
                this.tileID = tileID;
                this.layer = layer;
                this.groupName = groupName;
                this.tileName = tileName;
                this.onlyID = onlyID;
            }
        }
    }

#if UNITY_EDITOR

    [CustomEditor(typeof(TileRule))]
    public class TileRuleEditor : Editor
    {
        private string[] _layerArray;
        private int _index;
        private static Dictionary<MapLayer, string> _layerNamesCache = new Dictionary<MapLayer, string>();
        private Queue<TileRule.TileInfo> _deleteQueue = new Queue<TileRule.TileInfo>();

        private void OnEnable()
        {
            _layerNamesCache.Clear();
            List<string> list = new List<string>();

            for (int i = 0; i < (sizeof(MapLayer) * 8); i++)
            {
                MapLayer mapLayer = (MapLayer)(1 << i);

                if (Enum.IsDefined(typeof(MapLayer), mapLayer))
                {
                    list.Add(mapLayer.GetInspectorName());
                    _layerNamesCache.Add(mapLayer, mapLayer.GetInspectorName());
                }
            }

            _layerArray = list.ToArray();
            _deleteQueue.Clear();
        }

        public override void OnInspectorGUI()
        {
            GUILayout.BeginHorizontal();
            TileRule tileRule = (TileRule)target;

            _index = EditorGUILayout.Popup(_index, _layerArray, GUILayout.Width(100));

            if (GUILayout.Button("添加层数据", GUILayout.Width(100)))
            {
                byte layer = (byte)(1 << _index);
                string layerName = _layerNamesCache[(MapLayer)layer];

                if (!tileRule._tileLayerDic.Contains(layer))
                {
                    tileRule.Add(layer, new TileRule.TileLayer(layerName));
                }
                else
                {
                    Debug.Log($"{_layerNamesCache[(MapLayer)layer]} 已经存在了");
                }
            }

            if (GUILayout.Button("移除层数据", GUILayout.Width(100)))
            {
                byte layer = (byte)(1 << _index);
                string layerName = _layerNamesCache[(MapLayer)layer];

                bool isDelete = EditorUtility.DisplayDialog("提示",
                    $"你确定要删除{layerName}的数据吗",
                    "确定",
                    "取消");

                if (isDelete)
                {
                    bool isSuccess = tileRule.Remove(layer);

                    if (!isSuccess)
                    {
                        Debug.Log($"{layerName} 不存在");
                    }
                }
            }

            if (GUILayout.Button("排序", GUILayout.Width(100)))
            {
                tileRule._tileLayerDic.Sort();

                foreach (var tileLayer in tileRule._tileLayerDic)
                {
                    tileLayer.Value._tileGroup.Sort();

                    foreach (var tileGroup in tileLayer.Value._tileGroup)
                    {
                        tileGroup.Value.tileList.Sort();
                    }
                }
            }

            GUILayout.EndHorizontal();

            GUIStyle layerStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontSize = 16,
                normal = { textColor = Color.gray },
                onNormal = { textColor = Color.white },
            };

            GUIStyle groupStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontSize = 14,
                normal = { textColor = Color.gray },
                onNormal = { textColor = Color.white },
                margin = new RectOffset(8, 0, 2, 0),
            };

            foreach (var tileLayer in tileRule._tileLayerDic)
            {
                GUILayout.Space(5);
                string layerName = tileLayer.Value.layerName;
                bool layerFold = tileLayer.Value.Fold;

                GUILayout.BeginHorizontal();
                bool isLayerFold = EditorGUILayout.Foldout(layerFold, layerName, false, layerStyle);
                tileLayer.Value.groupName =
                    EditorGUILayout.TextField(tileLayer.Value.groupName, GUILayout.Width(100));

                if (GUILayout.Button("添加瓦片组", GUILayout.Width(100)))
                {
                    if (!string.IsNullOrEmpty(tileLayer.Value.groupName))
                    {
                        bool isSuccess = tileLayer.Value.Add(tileLayer.Value.groupName,
                            new TileRule.TileGroup(tileLayer.Value.groupName));

                        if (!isSuccess)
                        {
                            Debug.Log($"{tileLayer.Value.groupName} group 已经存在");
                        }
                    }
                }

                if (GUILayout.Button("移除瓦片组", GUILayout.Width(100)))
                {
                    if (!string.IsNullOrEmpty(tileLayer.Value.groupName))
                    {
                        bool isSuccess = tileLayer.Value.Remove(tileLayer.Value.groupName);

                        if (!isSuccess)
                        {
                            Debug.Log($"{tileLayer.Value.groupName} group 不存在 ");
                        }
                    }
                }

                GUILayout.EndHorizontal();

                if (isLayerFold)
                {
                    foreach (var tileGroup in tileLayer.Value._tileGroup)
                    {
                        GUILayout.Space(5);

                        string groupName = tileGroup.Value.groupName;

                        bool groupFold = tileGroup.Value.Fold;

                        GUILayout.BeginHorizontal();

                        if (GUILayout.Button("+", GUILayout.Width(30)))
                        {
                            tileGroup.Value.Fold = true;
                            int onlyID = (int)DateTime.Now.Ticks;
                            tileGroup.Value.tileList.Add(
                                new TileRule.TileInfo(tileLayer.Key, -1, onlyID, tileGroup.Value.groupName, ""));
                        }

                        bool isGroupFold = EditorGUILayout.Foldout(groupFold, groupName, false, groupStyle);

                        GUILayout.EndHorizontal();

                        if (isGroupFold)
                        {
                            foreach (var tileInfo in tileGroup.Value.tileList)
                            {
                                GUILayout.BeginHorizontal();

                                tileInfo.tileID = EditorGUILayout.IntField(tileInfo.tileID, GUILayout.Width(60));

                                tileInfo.tileName = EditorGUILayout.TextField(tileInfo.tileName, GUILayout.Width(100));

                                tileInfo.texture = (Texture)EditorGUILayout.ObjectField(tileInfo.texture,
                                    typeof(Texture), false, GUILayout.Width(200));

                                if (GUILayout.Button("X", GUILayout.Width(40)))
                                {
                                    _deleteQueue.Enqueue(tileInfo);
                                }

                                GUILayout.EndHorizontal();
                            }

                            if (_deleteQueue.Count > 0)
                            {
                                var tileInfo = _deleteQueue.Dequeue();

                                tileGroup.Value.tileList.Remove(tileInfo);
                            }
                        }

                        if (isGroupFold != groupFold)
                        {
                            tileGroup.Value.Fold = isGroupFold;
                        }
                    }

                    GUILayout.Space(5);
                }

                if (isLayerFold != layerFold)
                {
                    tileLayer.Value.Fold = isLayerFold;
                }
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(tileRule);
            }
        }
    }
#endif
}