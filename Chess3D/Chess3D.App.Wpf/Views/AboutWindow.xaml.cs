using System.Reflection;
using System.Windows;

namespace Chess3D.App.Wpf.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        string versionText = version is null
            ? "Version inconnue"
            : $"Version {version.Major}.{version.Minor}.{version.Build}";

        VersionTextBlock.Text = versionText;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}