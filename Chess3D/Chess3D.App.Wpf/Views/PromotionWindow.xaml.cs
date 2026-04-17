using System.Windows;
using Chess3D.Core.Enums;

namespace Chess3D.App.Wpf.Views;

public partial class PromotionWindow : Window
{
    public PieceType? SelectedPromotion { get; private set; }

    public PromotionWindow()
    {
        InitializeComponent();
    }

    private void PromotionButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || element.Tag is not string tag)
            return;

        SelectedPromotion = tag switch
        {
            "Queen" => PieceType.Queen,
            "Rook" => PieceType.Rook,
            "Bishop" => PieceType.Bishop,
            "Knight" => PieceType.Knight,
            _ => null
        };

        if (SelectedPromotion != null)
        {
            DialogResult = true;
            Close();
        }
    }
}