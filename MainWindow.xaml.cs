using System.IO;
using System.Windows;
using System.Windows.Input;

namespace AwsCliCredentialUtility;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private const string FileName = "last-profile";

    public MainWindow()
    {
        InitializeComponent();

        if (File.Exists(FileName))
            ProfileTextBox.Text = File.ReadAllText(FileName);
    }

    private void ProfileTextBox_OnKeyDown(object sender, KeyEventArgs e)
    {
        File.WriteAllText(FileName, ProfileTextBox.Text);

        if (e.Key == Key.Enter)
            Button.Command.Execute(ProfileTextBox.Text);
    }
}