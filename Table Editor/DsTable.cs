using System;
using System.Collections.Generic;
using UnityEngine;

namespace DsTools.Table
{
    internal enum TableFormat
    {
        [InspectorName("制表符(\\t)")]
        TSV,
        [InspectorName("逗号(,)")]
        CSV,
        [InspectorName("竖线符号(|)")]
        PSV,
    }

    [Serializable]
    internal class DsTable
    {
        private string _tableName;
        private string _tablePath;
        private TableFormat _tableFormat;
        private Dictionary<(int row, int column), DsCell> _dic;
        private TextAsset _textAsset;
        private int row;
        private int column;

        public DsTable(TextAsset textAsset)
        {
            if (textAsset is null)
            {
                Debug.Log("textAsset cannot be null");
                return;
            }
            _textAsset = textAsset;
            _dic = new Dictionary<(int row, int column), DsCell>();
            _tableName = textAsset.name;
#if UNITY_EDITOR
            _tablePath = UnityEditor.AssetDatabase.GetAssetPath(textAsset);
#endif
            //TODO:默认表格分割方式
            _tableFormat = TableFormat.TSV;
        }

        /// <summary>
        /// 初始化数据，解析表格
        /// </summary>
        public void InitData()
        {
            _dic ??= new Dictionary<(int Row, int Column), DsCell>();

            if (_textAsset is null)
            {
                Debug.Log("textAsset cannot be null");
                return;
            }

            this.Parser();
        }

        /// <summary>
        /// 表名
        /// </summary>
        public string TableName
        {
            get => _tableName;
            set => _tableName = value;
        }

        /// <summary>
        /// 表资源路径
        /// </summary>
        public string TablePath
        {
            get => _tablePath;
            set => _tablePath = value;
        }

        /// <summary>
        /// 表格分割方式
        /// </summary>
        public TableFormat TableFormat
        {
            get => _tableFormat;
            set => _tableFormat = value;
        }

        /// <summary>
        /// 文本
        /// </summary>
        public TextAsset TextAsset => _textAsset;

        /// <summary>
        /// 获取字典
        /// </summary>
        private Dictionary<(int row, int column), DsCell> CellDic
        {
            get
            {
                if (_dic is null && _textAsset is not null)
                {
                    _dic = new Dictionary<(int row, int column), DsCell>();
                    this.Parser();
                }
                return _dic;
            }
        }

        /// <summary>
        /// 行数
        /// </summary>
        public int RowCount
        {
            get => row;
            set => row = value;
        }

        /// <summary>
        /// 列数
        /// </summary>
        public int ColumnCount
        {
            get => column;
            set => column = value;
        }

        /// <summary>
        /// 获取对应位置的数据
        /// </summary>
        /// <param name="row">行</param>
        /// <param name="column">列</param>
        /// <returns></returns>
        public string GetValue(int row, int column)
        {
            if (CellDic.TryGetValue((row, column), out DsCell cell))
            {
                return cell.value;
            }

            return "";
        }

        /// <summary>
        /// 设置对应位置的数据
        /// </summary>
        /// <param name="row">行</param>
        /// <param name="column">列</param>
        /// <param name="value">数据</param>
        public void SetValue(int row, int column, string value)
        {
            if (CellDic.TryGetValue((row, column), out DsCell cell))
            {
                cell.value = value;
                CellDic[(row, column)] = cell;
            }
            else
            {
                CellDic.Add((row, column), new DsCell(row, column, value));
            }
        }

        /// <summary>
        /// 添加指定行
        /// </summary>
        public void AddRow(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex > RowCount)
            {
                Debug.LogError("行索引超出范围。");
                return;
            }

            for (int row = RowCount - 1; row >= rowIndex; row--)
            {
                for (int column = 0; column < ColumnCount; column++)
                {
                    string value = GetValue(row, column);
                    SetValue(row + 1, column, value);
                }
            }

            for (int column = 0; column < ColumnCount; column++)
            {
                SetValue(rowIndex, column, "");
            }

            RowCount++;
        }

        /// <summary>
        /// 添加指定列
        /// </summary>
        public void AddColumn(int columnIndex)
        {
            if (columnIndex < 0 || columnIndex > ColumnCount)
            {
                Debug.LogError("列索引超出范围。");
                return;
            }

            for (int column = ColumnCount - 1; column >= columnIndex; column--)
            {
                for (int row = 0; row < RowCount; row++)
                {
                    string value = GetValue(row, column);
                    SetValue(row, column + 1, value);
                }
            }

            for (int row = 0; row < RowCount; row++)
            {
                SetValue(row, columnIndex, "");
            }

            ColumnCount++;
        }

        /// <summary>
        /// 移除指定行
        /// </summary>
        /// <param name="rowIndex">要移除的行索引</param>
        public void RemoveRow(int rowIndex)
        {
            if (rowIndex >= RowCount || rowIndex < 0)
            {
                Debug.LogError("Row index out of range.");
                return;
            }

            for (int row = rowIndex + 1; row < RowCount; row++)
            {
                for (int column = 0; column < ColumnCount; column++)
                {
                    if (CellDic.TryGetValue((row, column), out DsCell cell))
                    {
                        CellDic[(row - 1, column)] = cell;
                    }
                    CellDic.Remove((row, column));
                }
            }

            for (int column = 0; column < ColumnCount; column++)
            {
                CellDic.Remove((RowCount - 1, column));
            }

            RowCount--;
        }

        /// <summary>
        /// 移除指定列
        /// </summary>
        /// <param name="columnIndex">要移除的列索引</param>
        public virtual void RemoveColumn(int columnIndex)
        {
            if (columnIndex >= ColumnCount || columnIndex < 0)
            {
                Debug.LogError("Column index out of range.");
                return;
            }

            for (int column = columnIndex + 1; column < ColumnCount; column++)
            {
                for (int row = 0; row < RowCount; row++)
                {
                    if (CellDic.TryGetValue((row, column), out DsCell cell))
                    {
                        CellDic[(row, column - 1)] = cell;
                    }
                    CellDic.Remove((row, column));
                }
            }

            for (int row = 0; row < RowCount; row++)
            {
                CellDic.Remove((row, ColumnCount - 1));
            }

            ColumnCount--;
        }

        /// <summary>
        /// 更换行的位置
        /// </summary>
        public void ChangeRow(int row1, int row2)
        {
            if (row1 < 0 || row1 >= RowCount || row2 < 0 || row2 >= RowCount)
            {
                Debug.Log("Row index out of range.");
                return;
            }

            List<string> row1List = new List<string>();

            for (int column = 0; column < ColumnCount; column++)
            {
                string value = GetValue(row1, column);
                row1List.Add(value);
            }

            for (int column = 0; column < ColumnCount; column++)
            {
                string value = GetValue(row2, column);
                SetValue(row1, column, value);
            }

            for (int column = 0; column < row1List.Count; column++)
            {
                SetValue(row2, column, row1List[column]);
            }
        }

        /// <summary>
        /// 更换列的位置
        /// </summary>
        public void ChangeColumn(int column1, int column2)
        {
            if (column1 < 0 || column1 >= ColumnCount || column2 < 0 || column2 >= ColumnCount)
            {
                Debug.Log("Column index out of range.");
                return;
            }

            List<string> column1List = new List<string>();

            for (int row = 0; row < RowCount; row++)
            {
                string value = GetValue(row, column1);
                column1List.Add(value);
            }

            for (int row = 0; row < RowCount; row++)
            {
                string value = GetValue(row, column2);
                SetValue(row, column1, value);
            }

            for (int row = 0; row < column1List.Count; row++)
            {
                SetValue(row, column2, column1List[row]);
            }
        }
    }
}


