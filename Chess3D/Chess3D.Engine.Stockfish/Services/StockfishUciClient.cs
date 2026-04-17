using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Chess3D.Core.Interfaces;
using Chess3D.Core.Models;

namespace Chess3D.Engine.Stockfish.Services
{
    public sealed class StockfishUciClient : IChessEngine, IAsyncDisposable
    {
        private readonly string _enginePath;
        private Process? _process;
        private StreamWriter? _input;
        private StreamReader? _output;

        public StockfishUciClient(string enginePath)
        {
            _enginePath = enginePath;
        }

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _enginePath,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            _process.Start();

            _input = _process.StandardInput;
            _output = _process.StandardOutput;

            await SendAsync("uci");
            await WaitForAsync("uciok", cancellationToken);

            await SendAsync("isready");
            await WaitForAsync("readyok", cancellationToken);
        }

        public async Task NewGameAsync(CancellationToken cancellationToken = default)
        {
            await SendAsync("ucinewgame");
            await SendAsync("isready");
            await WaitForAsync("readyok", cancellationToken);
        }

        public async Task<Move> GetBestMoveAsync(string fen, int level, CancellationToken cancellationToken = default)
        {
            await SendAsync($"position fen {fen}");
            var moveTimeMs = MapLevel(level);
            await SendAsync($"go movetime {moveTimeMs}");

            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await _output!.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (line.StartsWith("bestmove ", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    return Move.FromUci(parts[1]);
                }
            }

            throw new OperationCanceledException();
        }

        private async Task SendAsync(string command)
        {
            await _input!.WriteLineAsync(command);
            await _input.FlushAsync();
        }

        private async Task WaitForAsync(string token, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await _output!.ReadLineAsync();
                if (line is not null && line.Contains(token, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            throw new OperationCanceledException();
        }

        private static int MapLevel(int level) => level switch
        {
            1 => 75,
            2 => 125,
            3 => 200,
            4 => 350,
            5 => 500,
            6 => 800,
            7 => 1200,
            8 => 1800,
            9 => 2500,
            10 => 3500,
            _ => 500
        };

        public async ValueTask DisposeAsync()
        {
            if (_process is { HasExited: false })
            {
                await SendAsync("quit");
                _process.Kill(entireProcessTree: true);
            }

            _process?.Dispose();
        }
    }
}
