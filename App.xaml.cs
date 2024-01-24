using System.Windows;

namespace AwsCliCredentialUtility;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        Current.DispatcherUnhandledException += (_, args) => MessageBox.Show(args.Exception.Message);
    }
}