using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessEngine.gameLogic
{
    public struct TableEntry
    {
        public int Depth;
        public int Score;
        public TableFlag Flag;
        public Move? BestMove;
    }
    public enum TableFlag
    {
        Exact,
        LowerBound,
        UpperBound
    }
}
