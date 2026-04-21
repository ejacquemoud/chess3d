using System.Threading;
using System.Threading.Tasks;
using Chess3D.Core.Models;

namespace Chess3D.Core.Interfaces;

public interface IChessEngine
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task NewGameAsync(CancellationToken cancellationToken = default);
    Task<Move> GetBestMoveAsync(string fen, int level, CancellationToken cancellationToken = default);
}