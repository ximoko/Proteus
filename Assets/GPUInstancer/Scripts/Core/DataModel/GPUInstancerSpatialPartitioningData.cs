using System.Collections.Generic;
using UnityEngine;

namespace GPUInstancer
{
    public class GPUInstancerSpatialPartitioningData<T> where T : GPUInstancerCell
    {   
        public int cellRowAndCollumnCountPerTerrain;
        public List<T> activeCellList;

        private Dictionary<int, T> _cellHashList;
        private List<T> _cellList;
        private List<T> _activeCellsCalculationList;

        public GPUInstancerSpatialPartitioningData()
        {
            activeCellList = new List<T>();
            _cellHashList = new Dictionary<int, T>();
            _cellList = new List<T>();
            _activeCellsCalculationList = new List<T>();
        }

        public void AddCell(T cell)
        {
            _cellHashList.Add(cell.CalculateHash(), cell);
            _cellList.Add(cell);
        }

        public void GetCell(int hash, out T cell)
        {
            _cellHashList.TryGetValue(hash, out cell);
        }

        public List<T> GetCellList()
        {
            return _cellList;
        }

        public void GetCell(T cell)
        {
            _cellHashList.Add(cell.CalculateHash(), cell);
            _cellList.Add(cell);
        }

        public bool IsActiveCellUpdateRequired(Vector3 position)
        {
            return CalculateActiveCells(position);
        }

        public bool CalculateActiveCells(Vector3 position)
        {
            bool result = false;
            _activeCellsCalculationList.Clear();
            foreach (T cell in _cellList)
            {
                if (cell.cellBounds.Contains(position))
                {
                    _activeCellsCalculationList.Add(cell);
                }
            }

            foreach (T cell in _activeCellsCalculationList)
            {
                if (!activeCellList.Contains(cell))
                {
                    result = true;
                    break;
                }
            }
            if (!result)
            {
                foreach (T cell in activeCellList)
                {
                    if (!_activeCellsCalculationList.Contains(cell))
                    {
                        result = true;
                        break;
                    }
                }
            }

            if (result)
            {
                activeCellList.Clear();
                activeCellList.AddRange(_activeCellsCalculationList);
            }

            return result;
        }
    }
}