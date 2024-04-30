namespace DsTools.Table
{
    internal struct DsCell
    {
        public int row;

        public int column;

        public string value;

        public DsCell(int row, int column, string value)
        {
            this.row = row;
            this.column = column;
            this.value = value;
        }
    }
}

