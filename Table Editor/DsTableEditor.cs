#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
#endif

namespace DsTools.Table
{
#if UNITY_EDITOR
    internal class DsTableEditor : EditorWindow, ISerializationCallbackReceiver
    {
        [MenuItem("Assets/DsTable/Open Table")]
        public static void OpenTable()
        {
            TextAsset textAsset = Selection.activeObject as TextAsset;
            if (textAsset is not null)
            {
                DsTable dsTable = new DsTable(textAsset);
                dsTable.InitData();
                OpenTableEditor(dsTable);
            }
            else
            {
                Debug.Log("只有TextAsset文件可以打开");
            }
        }

        [MenuItem("Assets/DsTable/Create Table")]
        public static void CreateTable()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);

            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                path = Path.GetDirectoryName(path);
            }
            else if (string.IsNullOrEmpty(path))
            {
                path = "Assets";
            }

            string filePath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(path, "Table_.txt"));

            File.WriteAllText(filePath, DsTableExtensions.TableContent);
            AssetDatabase.Refresh();
            Object obj = AssetDatabase.LoadAssetAtPath<Object>(filePath);
            Selection.activeObject = obj;
            EditorUtility.FocusProjectWindow();
        }

        [SerializeField]
        private DsTable _dsTable;
        private bool _isDirty;
        private Vector2 _scrollPosition;
        private int? _row, _column;
        private GUIStyle _style1;
        private GUIStyle _style2;
        private bool _isOpenKey = true;
        private InputDialog _inputDialog;

        public static void OpenTableEditor(DsTable dsTable)
        {
            if (dsTable == null)
            {
                Debug.Log("表格为空");
                return;
            }

            var window = CreateWindow<DsTableEditor>(dsTable.TableName);
            window._dsTable = dsTable;
            window.minSize = new Vector2(960, 540);
            window.Show();
        }

        private void OnGUI()
        {
            //显示头部内容
            TopLabel();
            //处理按键
            ProcessKeyCommands();
            //显示表格内容
            TableContent();
        }

        private void TopLabel()
        {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();

            GUILayout.Label("表名", GUILayout.Width(30));
            _dsTable.TableName = EditorGUILayout.TextField(_dsTable.TableName, GUILayout.Width(100));
            GUILayout.Space(5);

            GUILayout.Label("路径", GUILayout.Width(30));
            _dsTable.TablePath = EditorGUILayout.TextField(_dsTable.TablePath);
            GUILayout.Space(5);

            GUILayout.Label("分隔符", GUILayout.Width(40));
            _dsTable.TableFormat = (TableFormat)EditorGUILayout.EnumPopup(_dsTable.TableFormat, GUILayout.Width(80));
            GUILayout.Space(5);

            GUILayout.Label("快捷键", GUILayout.Width(40));
            _isOpenKey = EditorGUILayout.Toggle(_isOpenKey, GUILayout.Width(20));
            GUILayout.Space(5);

            if (GUILayout.Button("快捷键说明", GUILayout.Width(75)))
            {
                EditorUtility.DisplayDialog("快捷键说明"
                , DsTableExtensions.ShortcutKeyDescription
                , "了解");
            }

            if (GUILayout.Button("添加", GUILayout.Width(75)))
            {
                Add();
            }

            if (GUILayout.Button("移除", GUILayout.Width(75)))
            {
                Delete();
            }

            if (GUILayout.Button("更换", GUILayout.Width(75)))
            {
                _inputDialog = InputDialog.ShowDialog(ChangeLocation, this);
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            _style1 ??= new GUIStyle()
            {
                alignment = TextAnchor.MiddleCenter,
                normal = new GUIStyleState()
                {
                    textColor = Color.white,
                }
            };

            bool hasValue = _row is not null && _column is not null;

            if (hasValue)
            {
                GUILayout.Label($"{_row.Value}行,{_column.Value}列", _style1, GUILayout.Width(60), GUILayout.Height(20));

                string value = _dsTable.GetValue(_row.Value, _column.Value);
                string newValue = GUILayout.TextField(value, GUILayout.Height(20));

                if (value != newValue)
                {
                    _dsTable.SetValue(_row.Value, _column.Value, newValue);
                    _isDirty = true;
                }
            }
            else
            {
                GUILayout.Label("null", _style1, GUILayout.Width(60), GUILayout.Height(20));
                GUILayout.TextField("无焦点", GUILayout.Height(20));
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void ProcessKeyCommands()
        {
            if (!_isOpenKey) return;

            Event e = Event.current;

            //保存
            if (e.type == EventType.KeyDown &&
                e.control && e.keyCode == KeyCode.S)
            {
                if (_isDirty)
                {
                    _dsTable.SaveTableToFile();
                    _isDirty = false;
                }

                Event.current.Use();
            }

            //获取焦点
            if (e.type == EventType.MouseUp && e.button == 0)
            {
                string currentFocusedName = GUI.GetNameOfFocusedControl();

                if (!string.IsNullOrEmpty(currentFocusedName))
                {
                    string[] currentKey = currentFocusedName.Split('_');
                    int i = int.Parse(currentKey[0]);
                    int j = int.Parse(currentKey[1]);

                    _row = i;
                    _column = j;
                }
                else
                {
                    _row = null;
                    _column = null;
                }
                Event.current.Use();
            }

            //插入一行或一列
            if (e.type == EventType.KeyDown && e.control && e.keyCode == KeyCode.A)
            {
                Add();

                Event.current.Use();
            }

            //删除一行或一列
            if (e.type == EventType.KeyDown && e.control && e.keyCode == KeyCode.D)
            {
                Delete();

                Event.current.Use();
            }

            //更换位置
            if (e.type == EventType.KeyDown && e.control && e.keyCode == KeyCode.E)
            {
                RemoveFocus();
                _inputDialog = InputDialog.ShowDialog(ChangeLocation, this);
                Event.current.Use();
            }

            //移动焦点
            if (e.type == EventType.KeyDown && e.control)
            {
                KeyCode keyCode = Event.current.keyCode;

                string currentFocusedName = GUI.GetNameOfFocusedControl();

                if (!string.IsNullOrEmpty(currentFocusedName))
                {
                    string[] currentKey = currentFocusedName.Split('_');
                    int i = int.Parse(currentKey[0]);
                    int j = int.Parse(currentKey[1]);

                    switch (keyCode)
                    {
                        case KeyCode.UpArrow:
                            if (i > 0) i--;
                            break;
                        case KeyCode.DownArrow:
                            if (i < _dsTable.RowCount - 1) i++;
                            break;
                        case KeyCode.LeftArrow:
                            if (j > 0) j--;
                            break;
                        case KeyCode.RightArrow:
                            if (j < _dsTable.ColumnCount - 1) j++;
                            break;
                    }

                    _row = i;
                    _column = j;
                    GUI.FocusControl($"{i}_{j}");
                    Event.current.Use(); //阻止事件进一步传播
                }
            }
        }

        private void TableContent()
        {
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, true, true);

            int rows = _dsTable.RowCount;
            int cols = _dsTable.ColumnCount;

            GUILayout.BeginVertical("Box");

            for (int i = 0; i < rows; i++)
            {
                GUILayout.BeginHorizontal();

                for (int j = 0; j < cols; j++)
                {
                    string value = _dsTable.GetValue(i, j);
                    GUI.SetNextControlName($"{i}_{j}");
                    _style2 ??= new GUIStyle(GUI.skin.textField)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        normal = new GUIStyleState()
                        {
                            textColor = Color.white,
                        },
                        focused = new GUIStyleState()
                        {
                            textColor = new Color(255 / 255f, 250 / 255f, 205 / 255f),
                        },
                        margin = new RectOffset(1, 1, 1, 1),
                    };
                    string newValue = EditorGUILayout.TextField(value, _style2, GUILayout.Width(100), GUILayout.Height(25));

                    if (newValue != value)
                    {
                        _dsTable.SetValue(i, j, newValue);
                        _isDirty = true;
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        private void Add()
        {
            if (_row is null && _column is null)
            {
                int option = EditorUtility.DisplayDialogComplex(
                "增加行或者列",
                "您想要增加一行还是一列",
                "行",
                "取消",
                "列");

                switch (option)
                {
                    case 0:
                        _dsTable.AddRow(_dsTable.RowCount);
                        break;
                    case 1:
                        break;
                    case 2:
                        _dsTable.AddColumn(_dsTable.ColumnCount);
                        break;
                }
            }
            else
            {
                int option = EditorUtility.DisplayDialogComplex(
                "增加行或者列",
                "您想要在焦点位置增加一行还是一列",
                $"第{_row}行",
                "取消",
                $"第{_column}列");

                switch (option)
                {
                    case 0:
                        _dsTable.AddRow(_row.Value);
                        break;
                    case 1:
                        break;
                    case 2:
                        _dsTable.AddColumn(_column.Value);
                        break;
                }
            }
            _isDirty = true;
            RemoveFocus();
        }

        private void Delete()
        {
            if (_row is null && _column is null)
            {
                int option = EditorUtility.DisplayDialogComplex(
                "删除行或者列",
                "您想要删除一行还是一列",
                "行",
                "取消",
                "列");

                switch (option)
                {
                    case 0:
                        _dsTable.RemoveRow(_dsTable.RowCount - 1);
                        break;
                    case 1:
                        break;
                    case 2:
                        _dsTable.RemoveColumn(_dsTable.ColumnCount - 1);
                        break;
                }
            }
            else
            {
                int option = EditorUtility.DisplayDialogComplex(
                "删除行或者列",
                "您想要删除焦点行还是焦点列",
                $"第{_row}行",
                "取消",
                $"第{_column}列");

                switch (option)
                {
                    case 0:
                        _dsTable.RemoveRow(_row.Value);
                        break;
                    case 1:
                        break;
                    case 2:
                        _dsTable.RemoveColumn(_column.Value);
                        break;
                }
            }
            _isDirty = true;
            RemoveFocus();
        }

        private void ChangeLocation(int n1, int n2, bool type)
        {
            if (type)
                _dsTable.ChangeRow(n1, n2);
            else
                _dsTable.ChangeColumn(n1, n2);

            _inputDialog?.Close();
        }

        private void OnDestroy()
        {
            if (_isDirty)
            {
                bool isSave = EditorUtility.DisplayDialog(
                    "表格已修改",
                    "你想要保存对表格的更改吗？",
                    "保存",
                    "不保存");

                if (isSave)
                {
                    _dsTable.SaveTableToFile();
                }
            }
        }

        private void RemoveFocus()
        {
            _row = null;
            _column = null;
            GUIUtility.hotControl = 0;//取消热控制
            GUI.FocusControl(null);
        }

        public void OnBeforeSerialize()
        {
            _dsTable.SaveTableToFile();
            _isDirty = false;
        }

        public void OnAfterDeserialize()
        {

        }
    }
#endif
}


