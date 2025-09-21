using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace VScreator;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void NewModButton_Click(object sender, RoutedEventArgs e)
    {
        // Open the mod creation window
        var modCreationWindow = new ModCreationWindow();
        modCreationWindow.ShowDialog();
    }

    private void OpenModButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Open Mod functionality will be implemented here.\nThis will open a file dialog to select an existing mod.", "Open Mod", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}