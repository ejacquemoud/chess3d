using Chess3D.Core.Enums;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Chess3D.App.Wpf.Views;

public partial class PromotionWindow : Window
{
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    public PieceType? SelectedPromotion { get; private set; }

    public PromotionWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => ApplyDarkTitleBar();
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int dwAttribute,
        ref int pvAttribute,
        int cbAttribute);

    private void ApplyDarkTitleBar()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero)
            return;

        int enabled = 1;
        _ = DwmSetWindowAttribute(
            hwnd,
            DWMWA_USE_IMMERSIVE_DARK_MODE,
            ref enabled,
            Marshal.SizeOf<int>());
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