using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessEngine.gameLogic
{
    public static class Zobrist
    {
        public static ulong[,,] PieceKeys = new ulong[12, 8, 8];
        public static ulong SideToMove;
        static Zobrist()
        {
            Random rng = new Random(12345);
            for(int p = 0; p < 12; p++)
            {
                for(int r=0; r < 8; r++)
                {
                    for(int c=0;c<8; c++)
                    {
                        PieceKeys[p, r, c] = RandomUlong(rng);
                    }
                }
            }
            SideToMove = RandomUlong(rng);
        }
        static ulong RandomUlong(Random rng)
        {
            byte[] bytes = new byte[8];
            rng.NextBytes(bytes);
            return BitConverter.ToUInt64(bytes);
        }
        
    }
}
