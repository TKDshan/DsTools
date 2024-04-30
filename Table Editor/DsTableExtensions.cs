using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace DsTools.Table
{
    internal static class DsTableExtensions
    {
        /// <summary>
        /// 文本解析器
        /// </summary>
        /// <param name="table"></param>
        /// <param name="textAsset"></param>
        /// <returns></returns>
        public static DsTable Parser(this DsTable table)
        {
            char delimiter = table.TableFormat.GetDelimiter();
            string[] lines = table.TextAsset.text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            int maxColumnIndex = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                string[] items = lines[i].Split(delimiter);

                for (int j = 0; j < items.Length; j++)
                {
                    string item = items[j].Trim();
                    table.SetValue(i, j, item);

                    if (j > maxColumnIndex)
                    {
                        maxColumnIndex = j;
                    }
                }
            }
            table.RowCount = lines.Length;
            table.ColumnCount = maxColumnIndex + 1;

            return table;
        }

        /// <summary>
        /// 保存文件到本地
        /// </summary>
        public static void SaveTableToFile(this DsTable dsTable)
        {
            char delimiter = dsTable.TableFormat.GetDelimiter();

            List<int> emptyRowList = dsTable.FilterEmptyRows();

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < dsTable.RowCount; i++)
            {
                if (emptyRowList.Contains(i))
                    continue;

                for (int j = 0; j < dsTable.ColumnCount; j++)
                {
                    // 获取单元格的值，如果不存在则使用空字符串
                    string value = dsTable.GetValue(i, j);
                    sb.Append(value);

                    // 如果不是最后一列，添加分隔符
                    if (j < dsTable.ColumnCount - 1)
                    {
                        sb.Append(delimiter);
                    }
                }

                sb.AppendLine();
            }

            if (sb.Length > 1 && sb.ToString().EndsWith("\r\n"))
            {
                sb.Remove(sb.Length - 2, 2); // 移除"\r\n"
            }
            else if (sb.Length > 0 && sb.ToString().EndsWith("\n"))
            {
                sb.Remove(sb.Length - 1, 1); // 移除"\n"
            }

            string directory = Path.GetDirectoryName(dsTable.TablePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string filePath = Path.Combine(directory, $"{dsTable.TableName}.txt");
            try
            {
                // 写入文件
                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            }
            catch
            {
                Debug.Log($"文件存储失败 {filePath}");
                throw;
            }

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
        }

        // <summary>
        /// 找出表格中数据都是空的行数
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        private static List<int> FilterEmptyRows(this DsTable dsTable)
        {
            List<int> emptyRowList = new List<int>();

            int rowCount = dsTable.RowCount;
            int columnCount = dsTable.ColumnCount;

            for (int i = 0; i < rowCount; i++)
            {
                bool isRowEmpty = true;
                for (int j = 0; j < columnCount; j++)
                {
                    if (!string.IsNullOrEmpty(dsTable.GetValue(i, j)))
                    {
                        isRowEmpty = false;
                        break;
                    }
                }

                if (isRowEmpty)
                {
                    emptyRowList.Add(i);
                }
            }

            return emptyRowList;
        }

        /// <summary>
        /// 根据表格类型返回分隔符
        /// </summary>
        /// <param name="tableFormat">枚举类型</param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">抛出异常</exception>
        public static char GetDelimiter(this TableFormat tableFormat)
        {
            switch (tableFormat)
            {
                case TableFormat.TSV:
                    return '\t';
                case TableFormat.CSV:
                    return ',';
                case TableFormat.PSV:
                    return '|';
                default:
                    throw new NotSupportedException($"Unsupported table format: {tableFormat}");
            }
        }

        public static string TableContent = "#\t表格\t\t\n#\tID\t\t\n#\tint\t\t\n#\t实体ID\t策划备注\t\t";

        public static string ShortcutKeyDescription = $"# Ctrl+A 焦点位置添加一行或者一列\n(无焦点末尾)\n"
        + "# Ctrl+D 焦点位置删除一行或一列\n(无焦点末尾)\n"
        + "# Ctrl+移动 快速移动单元格焦点\n"
        + "# Ctrl+S 保存内容\n"
        + "# Ctrl+E 打开换行窗口";
    }
}


