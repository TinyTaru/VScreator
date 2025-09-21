using System.Windows;
using Microsoft.Win32;
using System.IO;

namespace VScreator;

/// <summary>
/// Interaction logic for ModWorkspaceWindow.xaml
/// </summary>
public partial class ModWorkspaceWindow : Window
{
    private readonly string _modId;
    private readonly string _modName;
    private string _currentTab = "Content";

    public ModWorkspaceWindow(string modId, string modName)
    {
        InitializeComponent();
        _modId = modId;
        _modName = modName;

        // Update the window title with mod information
        Title = $"VScreator - {_modName} Workspace";
    }

    private void AddItemButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show($"Add Item functionality will be implemented here.\n\nMod: {_modName}\nID: {_modId}",
                        "Add Item", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void AddBlockButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show($"Add Block functionality will be implemented here.\n\nMod: {_modName}\nID: {_modId}",
                        "Add Block", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void AddRecipeButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show($"Add Recipe functionality will be implemented here.\n\nMod: {_modName}\nID: {_modId}",
                        "Add Recipe", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show($"Mod settings will be implemented here.\n\nMod: {_modName}\nID: {_modId}",
                        "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BackToMenuButton_Click(object sender, RoutedEventArgs e)
    {
        // Close the workspace and return to main menu
        this.Close();
    }

    private void ContentTab_Click(object sender, RoutedEventArgs e)
    {
        SwitchToTab("Content");
    }

    private void ResourcesTab_Click(object sender, RoutedEventArgs e)
    {
        SwitchToTab("Resources");
    }

    private void SwitchToTab(string tabName)
    {
        _currentTab = tabName;

        if (tabName == "Content")
        {
            // Show content tab buttons, hide resources tab buttons
            ContentTabButtons.Visibility = Visibility.Visible;
            ResourcesTabButtons.Visibility = Visibility.Collapsed;

            // Show content tab message, hide resources tab message
            ContentTabMessage.Visibility = Visibility.Visible;
            ResourcesTabMessage.Visibility = Visibility.Collapsed;
        }
        else if (tabName == "Resources")
        {
            // Hide content tab buttons, show resources tab buttons
            ContentTabButtons.Visibility = Visibility.Collapsed;
            ResourcesTabButtons.Visibility = Visibility.Visible;

            // Hide content tab message, show resources tab message
            ContentTabMessage.Visibility = Visibility.Collapsed;
            ResourcesTabMessage.Visibility = Visibility.Visible;
        }
    }

    private void ImportTextureButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Open file dialog to select PNG file
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select Texture File",
                Filter = "PNG Files (*.png)|*.png",
                CheckFileExists = true,
                CheckPathExists = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string selectedFilePath = openFileDialog.FileName;

                // Ask user for texture type using custom dialog
                var textureTypeDialog = new TextureTypeSelectionWindow();
                var dialogResult = textureTypeDialog.ShowDialog();

                if (dialogResult != true)
                {
                    return; // User cancelled
                }

                bool isBlockTexture = (textureTypeDialog.SelectedTextureType == "block");

                // Create folder structure
                string modDirectory = GetModDirectory();
                string textureType = isBlockTexture ? "block" : "item";
                string texturePath = Path.Combine(modDirectory, "assets", _modId, "textures", textureType);

                // Create directories if they don't exist
                Directory.CreateDirectory(texturePath);

                // Copy file to destination
                string fileName = Path.GetFileName(selectedFilePath);
                string destinationPath = Path.Combine(texturePath, fileName);

                File.Copy(selectedFilePath, destinationPath, true);

                // Show success message
                string textureTypeName = isBlockTexture ? "block" : "item";
                MessageBox.Show(
                    $"Texture '{fileName}' has been imported successfully!\n\n" +
                    $"Type: {textureTypeName}\n" +
                    $"Location: {texturePath}\n" +
                    $"Mod: {_modName}",
                    "Import Successful",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"An error occurred while importing the texture:\n\n{ex.Message}",
                "Import Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private string GetModDirectory()
    {
        // Get the directory where the executable is located
        string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string modsDirectory = Path.Combine(exeDirectory, "mods");
        return Path.Combine(modsDirectory, _modId);
    }
}