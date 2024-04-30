using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DsTools.Tile
{
#if UNITY_EDITOR
    /// <summary>
    /// 瓦片地图编辑器
    /// </summary>
    internal class TileMapEditor : UnityEditor.EditorWindow
    {
        //所有地图
        private SerializedDictionary<string, MapDataSO> _mapDic = new SerializedDictionary<string, MapDataSO>();

        //正在编辑的地图信息
        private SerializedDictionary<(int row, int column), Tile> _currentMapData = null;
        private Queue<string> _removeQueue = new Queue<string>();

        private string _currentMapName = "";

        //选择列表
        private List<(int row, int column)> _selectList = new List<(int row, int column)>();
        private bool _leftPanelFoldout = true; //左边收纳

        private readonly float _defaultWidth = 200; //表格左边列表默认宽度
        private readonly float _defaultLineWidth = 2; //分割线默认长度
        private readonly Color _defaultLineColor = new Color(0.5f, 0.5f, 0.5f, 1.0f); //默认分割线颜色

        private float _gridSize = 32f; // 网格大小
        private int _gridCount = 200; // 网格数量
        private float _gridScale = 1f; //网格缩放比例
        private Color _gridColor = Color.gray; // 网格颜色
        private Color _selectionColor = Color.red; //选中的颜色
        private Vector2 _gridScaleRange = new Vector2(0.5f, 3f); //缩放的范围
        private Vector2 _scrollPosTop; //左边顶部滑条
        private Vector2 _scrollPosDown; //左边底部滑条
        private Vector2 _gridOffset = Vector2.zero; //初始偏移
        private Vector2 _dragStartPoint; //鼠标点击位置
        private Vector2 _centerScalePoint;//中心的缩放坐标
        private bool _isDragging = false; //是否开始拖拽
        private UnityEditor.GenericMenu _menu = new UnityEditor.GenericMenu(); //生成菜单
        private TileRule _tileRule; //瓦片规则
        private Vector2 _mousePoint; //鼠标坐标
        private Vector2 _startPos; //网格绘制的起点
        private MapLayer _currentDrawLayer = MapLayer.Everything; //当前绘制的层
        private SelectionMode _selectionMode; //选择模式
        private Vector2 _rectStartPos;//矩形添加按下时的初始坐标
        private Vector2 _rectEndPos;//鼠标滑动中的坐标
        private bool isRectDraw = false;
        private Rect _rect;//绘制的空心网格

        [UnityEditor.MenuItem("DsTools/瓦片地图编辑器", false, 40)]
        public static void OpenMapEditor()
        {
            var window = CreateWindow<TileMapEditor>("地图编辑器");
            window.minSize = new Vector2(960, 540);
            // 获取屏幕大小
            var screenSize = new Vector2(Screen.currentResolution.width, Screen.currentResolution.height);
            // 计算窗口打开时的位置，使其位于屏幕正中间
            var windowPosition =
                new Vector2((screenSize.x - window.minSize.x) / 2, (screenSize.y - window.minSize.y) / 2);
            // 设置窗口位置和大小
            window.position = new Rect(windowPosition.x, windowPosition.y, window.minSize.x, window.minSize.y);
            window.Show();
        }

        //初始化瓦片配置和瓦片规则，菜单
        private void OnEnable()
        {
            TileConfig tileConfig =
                UnityEditor.AssetDatabase.LoadAssetAtPath<TileConfig>(TileEditorUtilities.TileConfigPath);

            if (tileConfig != null)
            {
                _gridSize = tileConfig.gridSize;
                _gridCount = tileConfig.gridCount;
                _gridColor = tileConfig.gridColor;
                _gridScaleRange = tileConfig.gridScaleRange;
                _selectionColor = tileConfig.selectionColor;
                _selectionMode = tileConfig.selectionMode;
            }

            _tileRule = UnityEditor.AssetDatabase.LoadAssetAtPath<TileRule>(TileEditorUtilities.TileRulePath);

            if (_tileRule != null)
            {
                InitGenericMenu();
            }

            _removeQueue.Clear();
            _currentMapData = null;
        }

        private void OnGUI()
        {
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal(new GUIStyle()
            {
                margin = new RectOffset(0, 0, 4, 0),
                normal = new GUIStyleState()
                {
                    // background = MakeTex(2, 2, new Color(0f, 0f, 0f)),
                }
            }, GUILayout.Height(20));

            GUILayout.Space(10);
            if (GUILayout.Button(_leftPanelFoldout ? "收起窗口" : "打开窗口", GUILayout.Width(100)))
            {
                _leftPanelFoldout = !_leftPanelFoldout;
            }

            GUILayout.Space(5);

            GUILayout.Label("绘制层级", GUILayout.Width(60));
            _currentDrawLayer =
                (MapLayer)UnityEditor.EditorGUILayout.EnumFlagsField(_currentDrawLayer, GUILayout.Width(100));

            GUILayout.Label("选择模式", GUILayout.Width(60));
            _selectionMode = (SelectionMode)UnityEditor.EditorGUILayout.EnumPopup(_selectionMode, GUILayout.Width(100));

            GUILayout.Label("网格数量", GUILayout.Width(60));
            UnityEditor.EditorGUILayout.LabelField(_gridCount.ToString(), GUILayout.Width(50));

            GUILayout.Label("网格大小", GUILayout.Width(60));
            UnityEditor.EditorGUILayout.LabelField(_gridSize.ToString(), GUILayout.Width(50));

            GUILayout.Label("缩放", GUILayout.Width(30));
            UnityEditor.EditorGUILayout.LabelField(_gridScale.ToString(), GUILayout.Width(50));

            GUILayout.Label("偏移", GUILayout.Width(30));
            UnityEditor.EditorGUILayout.LabelField(_centerScalePoint.ToString(), GUILayout.Width(120));

            GUILayout.EndHorizontal();

            DrawUpLine();

            GUILayout.BeginHorizontal();

            if (_leftPanelFoldout)
            {
                //绘制左边收纳窗口
                DrawLeftWindow();
            }

            DrawLeftLine();
            //绘制网格编辑窗口
            DrawTileMapWindow();
            DrawRightLine();
            GUILayout.EndHorizontal();
            DrawDownLine();
            GUILayout.EndVertical();
        }

        /// <summary>
        /// 左边收纳窗口
        /// </summary>
        private void DrawLeftWindow()
        {
            GUIStyle style = new GUIStyle()
            {
                normal = new GUIStyleState()
                {
                    background = MakeTex(2, 2, new Color(0.2f, 0.2f, 0.2f)),
                },
            };

            GUIStyle label = new GUIStyle()
            {
                alignment = TextAnchor.MiddleCenter,
                normal = new GUIStyleState()
                {
                    textColor = Color.black,
                    background = MakeTex(2, 2, new Color(0.5f, 0.5f, 0.5f))
                }
            };

            UnityEditor.EditorGUILayout.BeginVertical(style, GUILayout.Width(_defaultWidth),
                GUILayout.ExpandHeight(false));
            {
                GUILayout.Label("关卡数据列表", label);
                _scrollPosTop = GUILayout.BeginScrollView(_scrollPosTop);
                foreach (var map in _mapDic)
                {
                    UnityEditor.EditorGUILayout.BeginHorizontal();
                    bool isFold = UnityEditor.EditorGUILayout.Foldout(map.Value.isFold, new GUIContent(map.Key));

                    //打开
                    if (GUILayout.Button("Open", GUILayout.Width(60)))
                    {
                        if (_mapDic.Contains(_currentMapName))
                        {
                            _mapDic[_currentMapName].gridScale = _gridScale;
                            _mapDic[_currentMapName].gridOffset = _gridOffset;
                            _mapDic[_currentMapName].centerScalePoint = _centerScalePoint;

                            _mapDic[_currentMapName].Parse();
                        }

                        _currentMapName = map.Key;
                        _currentMapData = map.Value.mapData;

                        _gridScale = map.Value.gridScale;
                        _gridOffset = map.Value.gridOffset;
                        _centerScalePoint = map.Value.centerScalePoint;
                    }

                    //移除
                    if (GUILayout.Button("移除", GUILayout.Width(60)))
                    {
                        _mapDic[map.Key].gridScale = _gridScale;
                        _mapDic[map.Key].gridOffset = _gridOffset;
                        _mapDic[map.Key].centerScalePoint = _centerScalePoint;

                        _mapDic[map.Key].Parse();
                        _removeQueue.Enqueue(map.Key);
                        if (map.Key == _currentMapName)
                        {
                            _currentMapName = String.Empty;
                            _currentMapData = null;
                        }
                    }

                    UnityEditor.EditorGUILayout.EndHorizontal();

                    if (isFold)
                    {
                        map.Value.DrawFoldContent();
                    }

                    map.Value.isFold = isFold;
                }

                if (_removeQueue.Count > 0)
                {
                    string needRemoveName = _removeQueue.Dequeue();
                    _mapDic.Remove(needRemoveName);
                }

                GUILayout.EndScrollView();

                Rect dropArea =
                    GUILayoutUtility.GetRect(0, 40, GUILayout.Height(position.height * 0.05f),
                        GUILayout.ExpandWidth(true));
                GUI.Box(dropArea, "将表格文本拖入此处");
                HandleDragAndDrop(dropArea);

                DrawShortLine(_defaultLineWidth, _defaultLineColor);

                GUILayout.Label("瓦片数据", label);
                _scrollPosDown = GUILayout.BeginScrollView(_scrollPosDown);

                if (_currentMapData is not null && _tileRule is not null)
                {
                    foreach (var tileLayer in _tileRule._tileLayerDic)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(tileLayer.Value.layerName, GUILayout.Width(40));
                        int index = FindCurrentLayerTileInfo(tileLayer.Key);

                        string[] names = tileLayer.Value.GetCurrentLayerAllTileInfoName();
                        int changeIndex = UnityEditor.EditorGUILayout.Popup(index, names);

                        if (changeIndex != index)
                        {
                            ChangeTileInfo(names[changeIndex]);
                        }

                        GUILayout.EndHorizontal();
                    }
                }

                GUILayout.EndScrollView();
            }

            UnityEditor.EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 找到当前选中的瓦片对应层级对应的下标
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        private int FindCurrentLayerTileInfo(byte layer)
        {
            if (_selectList.Count <= 0) return -1;

            List<SerializedDictionary<byte, TileRule.TileInfo>> tempList =
                new List<SerializedDictionary<byte, TileRule.TileInfo>>();

            foreach (var pos in _selectList)
            {
                if (_currentMapData.Contains(pos))
                {
                    tempList.Add(_currentMapData[pos].tileInfoDic);
                }
            }

            List<string> intactNameList = new List<string>();
            foreach (var dic in tempList)
            {
                if (dic.Contains(layer))
                {
                    intactNameList.Add(dic[layer].IntactName());
                }
            }

            if (intactNameList.Count == 0)
            {
                return -1;
            }
            else if (intactNameList.Count == 1)
            {
                return _tileRule.IndexOf(intactNameList[0]);
            }
            else if (intactNameList.Count > 1)
            {
                return -1;
            }

            return -1;
        }

        /// <summary>
        /// 选择类型改变瓦片
        /// </summary>
        /// <param name="intactName"></param>
        private void ChangeTileInfo(string intactName)
        {
            if (_selectList.Count > 0)
            {
                TileRule.TileInfo tileInfo = _tileRule.FindTile(intactName);

                foreach (var select in _selectList)
                {
                    if (!_currentMapData.Contains(select))
                    {
                        _currentMapData.Add(select, new Tile(select.row, select.column));
                    }

                    _currentMapData[select].tileInfoDic[tileInfo.layer] = tileInfo;
                    _currentMapData[select].tileInfoDic.Sort();
                }
            }
        }

        /// <summary>
        /// 拖拽文件
        /// </summary>
        /// <param name="dropArea"></param>
        private void HandleDragAndDrop(Rect dropArea)
        {
            Event evt = Event.current;

            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dropArea.Contains(evt.mousePosition))
                        return;
                    UnityEditor.DragAndDrop.visualMode = UnityEditor.DragAndDropVisualMode.Copy;
                    if (evt.type == EventType.DragPerform)
                    {
                        UnityEditor.DragAndDrop.AcceptDrag();

                        foreach (var draggedObject in UnityEditor.DragAndDrop.objectReferences)
                        {
                            CheckDragObject(draggedObject);
                        }
                    }

                    break;
            }
        }

        /// <summary>
        /// 检查拖拽的Object
        /// </summary>
        /// <param name="draggedObject"></param>
        private void CheckDragObject(Object draggedObject)
        {
            if (draggedObject is MapDataSO mapDataSo)
            {
                string Name = draggedObject.name;

                bool isSuccess = _mapDic.Add(Name, mapDataSo);

                if (!isSuccess)
                {
                    Debug.Log($"{Name} 已经存在了");
                }
            }
        }

        /// <summary>
        /// 网格编辑窗口
        /// </summary>
        private void DrawTileMapWindow()
        {
            if (_currentMapData is null) return;
            UnityEditor.EditorGUILayout.BeginVertical("Box", GUILayout.ExpandHeight(true),
                GUILayout.ExpandWidth(true));
            UnityEditor.EditorGUILayout.EndVertical();

            //计算网格绘制范围
            _startPos.x = _leftPanelFoldout ? 218 : 10;
            _startPos.y = _leftPanelFoldout ? 41 : 37;
            float width = position.width - _startPos.x - 10;
            float height = position.height - _startPos.y - 14;
            //监听鼠标
            MouseEvents(_startPos.x, _startPos.y, width, height);

            UnityEditor.Handles.BeginGUI();

            UnityEditor.Handles.color = _gridColor;

            // 绘制横线
            for (int i = 0; i <= _gridCount; i++)
            {
                float lineY = _startPos.y + i * _gridSize * _gridScale + _centerScalePoint.y;
                if (lineY >= _startPos.y && lineY <= _startPos.y + height)
                {
                    UnityEditor.Handles.DrawLine(new Vector3(_startPos.x, lineY, 0),
                        new Vector3(_startPos.x + width, lineY, 0));
                }
            }

            // 绘制竖线
            for (int j = 0; j <= _gridCount; j++)
            {
                float lineX = _startPos.x + j * _gridSize * _gridScale + _centerScalePoint.x;
                if (lineX >= _startPos.x && lineX <= _startPos.x + width)
                {
                    UnityEditor.Handles.DrawLine(new Vector3(lineX, _startPos.y, 0),
                        new Vector3(lineX, _startPos.y + height, 0));
                }
            }

            //计算可视范围
            Rect _visibleArea = new Rect(_startPos.x, _startPos.y, width, height);

            //绘制纹理
            foreach (var tile in _currentMapData)
            {
                float startX = _startPos.x + tile.Value.Column * _gridSize * _gridScale + _centerScalePoint.x;
                float startY = _startPos.y + tile.Value.Row * _gridSize * _gridScale + _centerScalePoint.y;
                Rect tileRect = new Rect(startX, startY, _gridSize * _gridScale, _gridSize * _gridScale);

                //计算需要绘制的纹理和可视范围的交集
                Rect intersection = CalculateRectIntersection(tileRect, _visibleArea);

                if (intersection.width > 0 && intersection.height > 0)
                {
                    tile.Value.DrawTexture(tileRect, intersection, _currentDrawLayer);
                }
            }

            //绘制选择区域
            UnityEditor.Handles.color = _selectionColor;
            foreach (var item in _selectList)
            {
                // 原始矩形的位置和尺寸
                float startX = _startPos.x + item.column * _gridSize * _gridScale + _centerScalePoint.x;
                float startY = _startPos.y + item.row * _gridSize * _gridScale + _centerScalePoint.y;
                float sizeX = _gridSize * _gridScale;
                float sizeY = _gridSize * _gridScale;

                // 计算矩形与绘制范围的交集
                float clampedStartX = Mathf.Max(startX, _startPos.x);
                float clampedStartY = Mathf.Max(startY, _startPos.y);
                float clampedEndX = Mathf.Min(startX + sizeX, _startPos.x + width);
                float clampedEndY = Mathf.Min(startY + sizeY, _startPos.y + height);

                // 检查矩形是否完全在绘制范围外
                if (clampedStartX >= clampedEndX || clampedStartY >= clampedEndY)
                {
                    continue;
                }

                // 计算调整后的矩形的四个角的位置
                Vector3 topLeft = new Vector3(clampedStartX, clampedStartY, 0);
                Vector3 topRight = new Vector3(clampedEndX, clampedStartY, 0);
                Vector3 bottomRight = new Vector3(clampedEndX, clampedEndY, 0);
                Vector3 bottomLeft = new Vector3(clampedStartX, clampedEndY, 0);

                // 绘制调整后的空心矩形
                UnityEditor.Handles.DrawAAPolyLine(2,
                    new Vector3[] { topLeft, topRight, bottomRight, bottomLeft, topLeft });
            }

            if (isRectDraw)
                DrawHollowRectangle(_rectStartPos, _rectEndPos);

            UnityEditor.Handles.EndGUI();
        }

        /// <summary>
        /// 绘制空心矩形
        /// </summary>
        /// <param name="startPos"></param>
        /// <param name="endPos"></param>
        private void DrawHollowRectangle(Vector2 startPos, Vector2 endPos)
        {
            // 计算 drawRect 的左上角位置和宽高
            float x = startPos.x;
            float y = startPos.y;
            float width = Mathf.Abs(endPos.x - startPos.x);
            float height = Mathf.Abs(endPos.y - startPos.y);

            // 根据鼠标滑动方向调整左上角位置
            if (endPos.x < startPos.x)
            {
                x = endPos.x;
            }
            if (endPos.y < startPos.y)
            {
                y = endPos.y;
            }

            _rect = new Rect(x, y, width, height);

            // 计算调整后的矩形的四个角的位置
            Vector3 topLeft = new Vector3(_rect.xMin, _rect.yMin, 0);
            Vector3 topRight = new Vector3(_rect.xMax, _rect.yMin, 0);
            Vector3 bottomRight = new Vector3(_rect.xMax, _rect.yMax, 0);
            Vector3 bottomLeft = new Vector3(_rect.xMin, _rect.yMax, 0);

            // 绘制调整后的空心矩形
            UnityEditor.Handles.DrawAAPolyLine(2,
                new Vector3[] { topLeft, topRight, bottomRight, bottomLeft, topLeft });

        }

        /// <summary>
        /// 将选中的矩形添加到list中
        /// </summary>
        private void RectToList(Rect rect)
        {
            Vector2 startRectPos = new Vector2(rect.xMin, rect.yMin);
            Vector2 endRectPos = new Vector2(rect.xMax, rect.yMax);

            Vector2 startGridPos = startRectPos - _startPos - _centerScalePoint;
            startGridPos /= _gridScale;

            Vector2 endGridPos = endRectPos - _startPos - _centerScalePoint;
            endGridPos /= _gridScale;

            int startRow = Mathf.FloorToInt(startGridPos.y / _gridSize);
            int startColumn = Mathf.FloorToInt(startGridPos.x / _gridSize);

            int endRow = Mathf.FloorToInt(endGridPos.y / _gridSize);
            int endColumn = Mathf.FloorToInt(endGridPos.x / _gridSize);

            for (int i = startRow; i <= endRow; i++)
            {
                for (int j = startColumn; j <= endColumn; j++)
                {
                    _selectList.Add((i, j));
                }
            }
        }

        /// <summary>
        /// 计算矩形是否交界
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private Rect CalculateRectIntersection(Rect a, Rect b)
        {
            float x1 = Mathf.Max(a.xMin, b.xMin);
            float x2 = Mathf.Min(a.xMax, b.xMax);
            float y1 = Mathf.Max(a.yMin, b.yMin);
            float y2 = Mathf.Min(a.yMax, b.yMax);

            if (x2 >= x1 && y2 >= y1)
            {
                return new Rect(x1, y1, x2 - x1, y2 - y1);
            }

            return Rect.zero;
        }

        private void MouseEvents(float startX, float startY, float width, float height)
        {
            Event e = Event.current;

            //鼠标可以交互区域
            Rect interactionRect = new Rect(startX, startY, width, height);

            if (interactionRect.Contains(e.mousePosition))
            {
                // 鼠标滚轮 缩放
                if (e.type == EventType.ScrollWheel)
                {
                    _gridScale -= e.delta.y * 0.05f;
                    _gridScale = Mathf.Clamp(_gridScale, _gridScaleRange.x, _gridScaleRange.y);
                    _centerScalePoint = _gridOffset * _gridScale;
                    e.Use();
                }

                // 鼠标中键点击 拖动
                if (e.type == EventType.MouseDown && e.button == 2)
                {
                    _dragStartPoint = e.mousePosition;
                    _isDragging = true;
                    e.Use();
                }
                else if (e.type == EventType.MouseDrag && _isDragging)
                {
                    Vector2 dragDelta = e.mousePosition - _dragStartPoint;
                    Vector2 maxOffset = new Vector2(0, 0);
                    Vector2 minOffset = new Vector2(-_gridCount * _gridSize * _gridScale, -_gridCount * _gridSize * _gridScale);
                    _gridOffset += dragDelta;
                    _gridOffset = new Vector2(Mathf.Clamp(_gridOffset.x, minOffset.x, maxOffset.x), Mathf.Clamp(_gridOffset.y, minOffset.y, maxOffset.y));
                    _centerScalePoint = _gridOffset * _gridScale;
                    _dragStartPoint = e.mousePosition;
                    e.Use();
                }
                else if (e.type == EventType.MouseUp && _isDragging)
                {
                    _isDragging = false;
                    e.Use();
                }

                // 鼠标右键打开 菜单
                if (e.type == EventType.MouseDown && e.button == 1)
                {
                    _mousePoint = Event.current.mousePosition;
                    _menu.ShowAsContext();
                    e.Use();
                }

                // 鼠标左键 选中空网格
                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    if (_selectionMode == SelectionMode.RectAdd && !isRectDraw)
                    {
                        _rectStartPos = e.mousePosition;
                        _rectEndPos = _rectStartPos;
                        _selectList.Clear();
                        isRectDraw = true;
                        e.Use();
                    }
                    else
                    {
                        if (_selectionMode == SelectionMode.Replace)
                        {
                            _selectList.Clear();
                        }

                        AddGridPositionToList();
                        e.Use();
                    }
                }
                else if (e.type == EventType.MouseDrag && e.button == 0)
                {
                    if (_selectionMode == SelectionMode.RectAdd && isRectDraw)
                    {
                        _rectEndPos = e.mousePosition;
                        e.Use();
                    }
                    else
                    {
                        AddGridPositionToList();
                        e.Use();
                    }
                }
                else if (e.type == EventType.MouseUp && e.button == 0)
                {
                    if (_selectionMode == SelectionMode.RectAdd && isRectDraw)
                    {
                        _rectStartPos = Vector2.zero;
                        _rectEndPos = Vector2.zero;
                        isRectDraw = false;
                        RectToList(_rect);
                        e.Use();
                    }
                }
            }

            // 根据网格尺寸和数量计算网格区域总尺寸
            float totalGridWidth = _gridSize * _gridScale * _gridCount;
            float totalGridHeight = _gridSize * _gridScale * _gridCount;

            // 计算滑动范围限制
            float maxXOffset = 0;
            float maxYOffset = 0;
            float minXOffset = Mathf.Min(0, interactionRect.width - totalGridWidth);
            float minYOffset = Mathf.Min(0, interactionRect.height - totalGridHeight);

            // 应用滑动范围限制
            _centerScalePoint.x = Mathf.Clamp(_centerScalePoint.x, minXOffset, maxXOffset);
            _centerScalePoint.y = Mathf.Clamp(_centerScalePoint.y, minYOffset, maxYOffset);
        }

        /// <summary>
        /// 根据网格坐标添加到list中
        /// </summary>
        private void AddGridPositionToList()
        {
            // 转换鼠标位置到网格坐标
            Vector2 gridPosition = Event.current.mousePosition - _startPos - _centerScalePoint;
            gridPosition /= _gridScale;

            // 计算行和列
            int column = Mathf.FloorToInt(gridPosition.x / _gridSize);
            int row = Mathf.FloorToInt(gridPosition.y / _gridSize);

            var pos = (row, column);
            bool isHave = _selectList.Contains(pos);

            if (!isHave && (_selectionMode == SelectionMode.Add || _selectionMode == SelectionMode.Replace))
            {
                _selectList.Add(pos);
            }
            else if (isHave && _selectionMode == SelectionMode.Remove)
            {
                _selectList.Remove(pos);
            }
        }

        /// <summary>
        /// 初始化右键菜单
        /// </summary>
        private void InitGenericMenu()
        {
            foreach (var tileRule in _tileRule._tileLayerDic)
            {
                string layerName = tileRule.Value.layerName;

                foreach (var tileGroup in tileRule.Value._tileGroup)
                {
                    string groupName = tileGroup.Value.groupName;

                    foreach (var tileInfo in tileGroup.Value.tileList)
                    {
                        if (!string.IsNullOrEmpty(tileInfo.tileName) && tileInfo.texture is not null)
                        {
                            _menu.AddItem(new GUIContent($"{layerName}/{groupName}/{tileInfo.tileName}"), false,
                                () => { ItemOnclick(tileInfo.layer, tileInfo); });
                        }
                    }
                }
            }

            _menu.AddItem(new GUIContent("销毁"), false, () => { ItemOnclick(0, null); });
        }

        /// <summary>
        /// 菜单点击回调
        /// </summary>
        /// <param name="layer">层</param>
        /// <param name="tileInfo">瓦片信息</param>
        private void ItemOnclick(byte layer, TileRule.TileInfo tileInfo)
        {
            if (_selectList.Count > 0)
            {
                foreach (var item in _selectList)
                {
                    AddTile(layer, tileInfo, item.row, item.column);
                }
            }
            else
            {
                // 转换鼠标位置到网格坐标
                Vector2 gridPosition = _mousePoint - _startPos - _centerScalePoint;
                gridPosition /= _gridScale;

                // 计算行和列
                int column = Mathf.FloorToInt(gridPosition.x / _gridSize);
                int row = Mathf.FloorToInt(gridPosition.y / _gridSize);

                AddTile(layer, tileInfo, row, column);
            }

            UnityEditor.EditorUtility.SetDirty(_mapDic[_currentMapName]);
        }

        /// <summary>
        /// 添加瓦片
        /// </summary>
        private void AddTile(byte layer, TileRule.TileInfo tileInfo, int row, int column)
        {
            if (tileInfo is null)
            {
                _currentMapData.Remove((row, column));
                return;
            }

            if (_currentMapData.Contains((row, column)))
            {
                if (_currentMapData[(row, column)] is Tile tile)
                {
                    tile.tileInfoDic[layer] = tileInfo;
                    tile.tileInfoDic.Sort();
                }
            }
            else
            {
                Tile tile = new Tile(row, column);
                _currentMapData.Add((row, column), tile);
                tile.tileInfoDic[layer] = tileInfo;
                tile.tileInfoDic.Sort();
            }
        }

        #region 画线

        /// <summary>
        /// 绘制左边的线
        /// </summary>
        private void DrawLeftLine()
        {
            var originalColor = GUI.color;
            GUI.color = _defaultLineColor;

            GUILayout.Box("", GUILayout.Width(_defaultLineWidth), GUILayout.ExpandHeight(true));

            GUI.color = originalColor;
        }

        /// <summary>
        /// 绘制顶部的线
        /// </summary>
        private void DrawUpLine()
        {
            var originalColor = GUI.color;
            GUI.color = _defaultLineColor;

            GUILayout.Box("", GUILayout.Height(_defaultLineWidth), GUILayout.ExpandWidth(true));

            GUI.color = originalColor;
        }

        /// <summary>
        /// 绘制右边的线
        /// </summary>
        private void DrawRightLine()
        {
            var originalColor = GUI.color;
            GUI.color = _defaultLineColor;

            GUILayout.Box("", GUILayout.Width(_defaultLineWidth), GUILayout.ExpandHeight(true));

            GUI.color = originalColor;
        }

        /// <summary>
        /// 绘制底部边的线
        /// </summary>
        private void DrawDownLine()
        {
            var originalColor = GUI.color;
            GUI.color = _defaultLineColor;

            GUILayout.Box("", GUILayout.Height(_defaultLineWidth), GUILayout.ExpandWidth(true));

            GUI.color = originalColor;
        }

        private void DrawShortLine(float height, Color color)
        {
            var originalColor = GUI.color;
            GUI.color = color;

            GUILayout.Box("", GUILayout.Height(height), GUILayout.Width(_defaultWidth));

            GUI.color = originalColor;
        }

        #endregion

        /// <summary>
        /// 创建纹理
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];

            for (int i = 0; i < pix.Length; i++)
            {
                pix[i] = col;
            }

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();

            return result;
        }

        private void OnDestroy()
        {
            if (!string.IsNullOrEmpty(_currentMapName))
            {
                if (_mapDic.Contains(_currentMapName))
                {
                    _mapDic[_currentMapName].Parse();
                }
            }
        }
    }
#endif
}