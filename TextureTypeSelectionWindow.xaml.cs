using System.Windows;
using System.Windows.Controls;

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

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        // Set the selected texture type from the ComboBox
        if (TextureTypeComboBox.SelectedItem is ComboBoxItem selectedItem)
        {
            SelectedTextureType = selectedItem.Content.ToString();
        }

        this.DialogResult = true;
        this.Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        this.DialogResult = false;
        this.Close();
    }
}