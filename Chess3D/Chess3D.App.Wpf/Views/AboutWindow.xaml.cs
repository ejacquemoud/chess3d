using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Chess3D.App.Wpf.Views;

public partial class AboutWindow : Window
{
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    public AboutWindow()
    {
        InitializeComponent();

        SourceInitialized += (_, _) => ApplyDarkTitleBar();

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        string versionText = version is null
            ? "Version inconnue"
            : $"Version {version.Major}.{version.Minor}.{version.Build}.{version.Revision} beta";

        VersionTextBlock.Text = versionText;

        Loaded += AboutWindow_Loaded;
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

    private void AboutWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (Icon is ImageSource imageSource)
        {
            AppIconImage.Source = imageSource;
            return;
        }

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Chess.ico");
        if (!File.Exists(iconPath))
            return;

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = new Uri(iconPath, UriKind.Absolute);
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        bitmap.Freeze();

        AppIconImage.Source = bitmap;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}