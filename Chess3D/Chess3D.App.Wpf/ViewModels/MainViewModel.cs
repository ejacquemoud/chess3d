using Chess3D.Core.Models;
using Chess3D.Rendering.Wpf.ViewModels;

namespace Chess3D.App.Wpf.ViewModels
{
    public sealed class MainViewModel
    {
        public Board3DViewModel Board3D { get; }

        public MainViewModel()
        {
            var boardState = BoardState.CreateInitial();
            Board3D = new Board3DViewModel(boardState);
        }
    }
}