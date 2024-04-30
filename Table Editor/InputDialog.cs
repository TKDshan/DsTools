#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
#endif

namespace DsTools
{
#if UNITY_EDITOR
    internal class InputDialog : EditorWindow
    {
        private static Action<int, int, bool> _callback;

        private string _currentRow = "", _currentColumn = "", _targetRow = "", _targetColumn = "";

        public static InputDialog ShowDialog(Action<int, int, bool> callback, EditorWindow parentWindow)
        {
            _callback = callback;
            InputDialog window = GetWindow<InputDialog>("输入框");
            var parentPosition = parentWindow.position;
            var width = 300;
            var height = 100;
            window.position = new Rect(
            parentPosition.x + (parentPosition.width - width) / 2,
            parentPosition.y + (parentPosition.height - height) / 2,
            width,
            height
        );
            window.ShowPopup();
            return window;
        }

        private void OnGUI()
        {
            GUILayout.BeginVertical();

            GUILayout.Label("输入要更换的行或者列");
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Label("当前行");
            _currentRow = GUILayout.TextField(_currentRow, GUILayout.Width(50));
            GUILayout.Space(5);
            GUILayout.Label("目标行");
            _targetRow = GUILayout.TextField(_targetRow, GUILayout.Width(50));
            GUILayout.Space(5);
            if (GUILayout.Button("更换行"))
            {
                int row1 = int.Parse(_currentRow);
                int row2 = int.Parse(_targetRow);

                _callback?.Invoke(row1, row2, true);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("当前列");
            _currentColumn = GUILayout.TextField(_currentColumn, GUILayout.Width(50));
            GUILayout.Space(5);
            GUILayout.Label("目标列");
            _targetColumn = GUILayout.TextField(_targetColumn, GUILayout.Width(50));
            GUILayout.Space(5);
            if (GUILayout.Button("更换列"))
            {
                int col1 = int.Parse(_currentColumn);
                int col2 = int.Parse(_targetColumn);

                _callback?.Invoke(col1, col2, false);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void OnDestroy()
        {
            GUI.FocusControl(null);
            _callback = null;
        }
    }
#endif
}


