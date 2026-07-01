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
            : $"Version {version.Major}.{version.Minor}.{version.Build}";

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
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Chess.ico");
        if (File.Exists(iconPath))
        {
            BitmapFrame? bestFrame;
            using (var stream = File.OpenRead(iconPath))
            {
                var decoder = BitmapDecoder.Create(
                    stream,
                    BitmapCreateOptions.PreservePixelFormat,
                    BitmapCacheOption.OnLoad);

                // Largest frame → WPF downscales cleanly, no upscaling blur
                bestFrame = decoder.Frames
                    .OrderByDescending(f => f.PixelWidth)
                    .FirstOrDefault();
            }

            if (bestFrame != null)
            {
                bestFrame.Freeze();
                AppIconImage.Source = bestFrame;
                return;
            }
        }

        if (Icon is ImageSource imageSource)
            AppIconImage.Source = imageSource;
    }

    private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = e.Uri.AbsoluteUri,
            UseShellExecute = true
        });

        e.Handled = true;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}