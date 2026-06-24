namespace CopyBuildingModernized.Backend
{
    public static class GridIndexExtensions
    {
        /// <summary>
        /// 将 m*m 格子的序号转换为 n*n 格子的序号，保持共同中心，超出新模板范围的格子返回 -1
        /// </summary>
        public static int ConvertGridIndex(this int oldIndex, int m, int n)
        {
            int oldRow = oldIndex / m;
            int oldCol = oldIndex % m;
            int rowOffset = (n - m) / 2;
            int colOffset = (n - m) / 2;
            int newRow = oldRow + rowOffset;
            int newCol = oldCol + colOffset;
            if (newRow < 0 || newRow >= n || newCol < 0 || newCol >= n)
                return -1;
            return newRow * n + newCol;
        }

        /// <summary>
        /// 将 m*m 格子的序号转换为 n*n 格子的序号，保持共同中心，超出新模板范围的格子返回 -1
        /// </summary>
        public static short ConvertGridIndex(this short oldIndex, short m, short n)
        {
            short oldRow = (short)(oldIndex / m);
            short oldCol = (short)(oldIndex % m);
            short rowOffset = (short)((n - m) / 2);
            short colOffset = (short)((n - m) / 2);
            short newRow = (short)(oldRow + rowOffset);
            short newCol = (short)(oldCol + colOffset);
            if (newRow < 0 || newRow >= n || newCol < 0 || newCol >= n)
                return -1;
            return (short)(newRow * n + newCol);
        }
    }
}
