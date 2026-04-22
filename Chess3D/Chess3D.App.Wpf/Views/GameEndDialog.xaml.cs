using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Chess3D.App.Wpf.Views;

public partial class GameEndDialog : Window
{
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    public GameEndDialog(string title, string message)
    {
        InitializeComponent();

        SourceInitialized += (_, _) => ApplyDarkTitleBar();

        Title = title;
        TitleTextBlock.Text = title;
        MessageTextBlock.Text = message;
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

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}