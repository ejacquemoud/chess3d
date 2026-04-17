using System; 
using Chess3D.Core.Enums;

namespace Chess3D.Core.Models
{
    public readonly record struct Square(int File, int Rank)
    {
        public override string ToString() => $"{(char)('a' + File)}{Rank + 1}";

        public static Square FromAlgebraic(string value)
        {
            int file = value[0] - 'a';
            int rank = value[1] - '1';
            return new Square(file, rank);
        }
    }
}
