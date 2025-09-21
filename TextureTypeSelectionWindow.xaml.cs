using System.Windows;

namespace VScreator;

/// <summary>
/// Interaction logic for TextureTypeSelectionWindow.xaml
/// </summary>
public partial class TextureTypeSelectionWindow : Window
{
    public string SelectedTextureType { get; private set; } = "item";

    public TextureTypeSelectionWindow()
    {
        InitializeComponent();
    }

    private void BlockButton_Click(object sender, RoutedEventArgs e)
    {
        SelectedTextureType = "block";
        this.DialogResult = true;
        this.Close();
    }

    private void ItemButton_Click(object sender, RoutedEventArgs e)
    {
        SelectedTextureType = "item";
        this.DialogResult = true;
        this.Close();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        // This is now handled by the individual Block/Item buttons
        this.DialogResult = true;
        this.Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        this.DialogResult = false;
        this.Close();
    }
}