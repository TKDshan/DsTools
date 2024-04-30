using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace DsTools.Tile
{

    internal static class TileEditorUtilities
    {
        public const string TileConfigPath = "Assets/Resources/DsTools/TileConfig.asset";

        public const string TileRulePath = "Assets/Resources/DsTools/TileRule.asset";

        public static void CreateScriptableObject<T>(string path) where T : ScriptableObject
        {
#if UNITY_EDITOR
            string fullPath =
                Path.GetFullPath(Path.Combine(Application.dataPath, "../", TileEditorUtilities.TileConfigPath));
            string directoryPath = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);

                UnityEditor.AssetDatabase.Refresh(); // 刷新AssetDatabase，以便Unity识别新创建的文件夹
            }

            T t = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);

            if (t is not null)
            {
                UnityEditor.EditorUtility.FocusProjectWindow();
                UnityEditor.Selection.activeObject = t;
                return;
            }

            t = ScriptableObject.CreateInstance<T>();
            UnityEditor.AssetDatabase.CreateAsset(t, path);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
            UnityEditor.EditorUtility.FocusProjectWindow();
            UnityEditor.Selection.activeObject = t;
#endif
        }

        public static string GetInspectorName(this Enum value)
        {
            // 获取枚举值对应的字段信息
            FieldInfo field = value.GetType().GetField(value.ToString());

            // 尝试获取InspectorName属性
            InspectorNameAttribute attribute = field.GetCustomAttribute<InspectorNameAttribute>();

            // 如果找到属性，则返回其值；否则返回枚举的原始名称
            return attribute != null ? attribute.displayName : value.ToString();
        }
    }
}