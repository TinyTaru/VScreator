using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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

    // Texture selection tracking
    private string _selectedTexturePath = null;
    private string _selectedTextureType = null; // "item" or "block"
    private Border _selectedTextureBorder = null;

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
        // Open the item creation window
        var itemCreationWindow = new ItemCreationWindow(_modId, _modName);
        itemCreationWindow.ShowDialog();
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

            // Show content tab message, hide resources tab content
            ContentTabMessage.Visibility = Visibility.Visible;
            ResourcesTabContent.Visibility = Visibility.Collapsed;
        }
        else if (tabName == "Resources")
        {
            // Hide content tab buttons, show resources tab buttons
            ContentTabButtons.Visibility = Visibility.Collapsed;
            ResourcesTabButtons.Visibility = Visibility.Visible;

            // Hide content tab message, show resources tab content
            ContentTabMessage.Visibility = Visibility.Collapsed;
            ResourcesTabContent.Visibility = Visibility.Visible;

            // Load textures when switching to Resources tab
            LoadTextures();
        }
    }

    private void DeleteTextureButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Check if a texture is selected
            if (string.IsNullOrEmpty(_selectedTexturePath))
            {
                MessageBox.Show(
                    "Please select a texture first by clicking on it.\n\n" +
                    "Selected textures will be highlighted with a yellow border.",
                    "No Texture Selected",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Get texture information
            string fileName = Path.GetFileName(_selectedTexturePath);
            string textureType = _selectedTextureType == "item" ? "Item" : "Block";

            // Show confirmation dialog
            MessageBoxResult result = MessageBox.Show(
                $"Are you sure you want to delete the {textureType.ToLower()} texture '{fileName}'?\n\n" +
                "This action cannot be undone.",
                "Confirm Deletion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);

            if (result == MessageBoxResult.Yes)
            {
                // Delete the texture file
                File.Delete(_selectedTexturePath);

                // Show success message
                MessageBox.Show(
                    $"Texture '{fileName}' has been deleted successfully!\n\n" +
                    $"Type: {textureType}\n" +
                    $"Mod: {_modName}",
                    "Texture Deleted",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Clear selection
                ClearTextureSelection();

                // Refresh texture gallery
                LoadTextures();

                // Update delete button tooltip
                DeleteTextureButton.ToolTip = "Delete selected texture";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"An error occurred while deleting the texture:\n\n{ex.Message}",
                "Delete Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void ClearTextureSelection()
    {
        // Clear visual selection
        if (_selectedTextureBorder != null)
        {
            _selectedTextureBorder.BorderBrush = new SolidColorBrush(Colors.Transparent);
            _selectedTextureBorder.BorderThickness = new Thickness(0);
            _selectedTextureBorder.Background = new SolidColorBrush(Colors.Transparent);
        }

        // Clear selection data
        _selectedTexturePath = null;
        _selectedTextureType = null;
        _selectedTextureBorder = null;
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

                // Refresh texture gallery if we're currently in Resources tab
                if (_currentTab == "Resources")
                {
                    LoadTextures();
                }
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

    private void LoadTextures()
    {
        try
        {
            // Clear existing textures
            ItemTexturesPanel.Children.Clear();
            BlockTexturesPanel.Children.Clear();

            string modDirectory = GetModDirectory();
            string assetsPath = Path.Combine(modDirectory, "assets", _modId, "textures");

            if (!Directory.Exists(assetsPath))
            {
                return; // No assets folder yet
            }

            // Load item textures
            string itemTexturesPath = Path.Combine(assetsPath, "item");
            if (Directory.Exists(itemTexturesPath))
            {
                LoadTexturesFromFolder(itemTexturesPath, ItemTexturesPanel, "item");
            }

            // Load block textures
            string blockTexturesPath = Path.Combine(assetsPath, "block");
            if (Directory.Exists(blockTexturesPath))
            {
                LoadTexturesFromFolder(blockTexturesPath, BlockTexturesPanel, "block");
            }

            // Update headers visibility based on content
            ItemTexturesHeader.Visibility = ItemTexturesPanel.Children.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            BlockTexturesHeader.Visibility = BlockTexturesPanel.Children.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading textures: {ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadTexturesFromFolder(string folderPath, WrapPanel targetPanel, string textureType)
    {
        string[] textureFiles = Directory.GetFiles(folderPath, "*.png");

        foreach (string textureFile in textureFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(textureFile);

            // Create border for selection visual feedback
            Border textureBorder = new Border
            {
                Margin = new Thickness(10),
                CornerRadius = new CornerRadius(5),
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = textureFile, // Store the full path for deletion
                Background = new SolidColorBrush(Colors.Transparent)
            };

            // Create texture display container
            StackPanel textureContainer = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Background = new SolidColorBrush(Colors.Transparent)
            };

            // Create image control
            Image textureImage = new Image
            {
                Width = 64,
                Height = 64,
                Margin = new Thickness(5),
                Stretch = Stretch.Uniform
            };

            try
            {
                // Load the image
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(textureFile, UriKind.Absolute);
                bitmap.EndInit();

                textureImage.Source = bitmap;
            }
            catch (Exception)
            {
                // If image loading fails, show a placeholder
                textureImage.Source = null;
            }

            // Create label with texture name
            TextBlock textureLabel = new TextBlock
            {
                Text = fileName,
                Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#cccccc")),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 80,
                Margin = new Thickness(5)
            };

            // Add image and label to container
            textureContainer.Children.Add(textureImage);
            textureContainer.Children.Add(textureLabel);

            // Add container to border
            textureBorder.Child = textureContainer;

            // Add click event handler for texture selection
            textureBorder.MouseLeftButtonUp += (sender, e) =>
            {
                SelectTexture(textureFile, textureType, textureBorder);
            };

            // Add container to panel
            targetPanel.Children.Add(textureBorder);
        }
    }

    private void SelectTexture(string texturePath, string textureType, Border clickedBorder)
    {
        // Clear previous selection
        if (_selectedTextureBorder != null)
        {
            _selectedTextureBorder.BorderBrush = new SolidColorBrush(Colors.Transparent);
            _selectedTextureBorder.Background = new SolidColorBrush(Colors.Transparent);
        }

        // Set new selection
        _selectedTexturePath = texturePath;
        _selectedTextureType = textureType;
        _selectedTextureBorder = clickedBorder;

        // Visual feedback for selected texture
        clickedBorder.BorderBrush = new SolidColorBrush(Colors.Yellow);
        clickedBorder.BorderThickness = new Thickness(2);
        clickedBorder.Background = new SolidColorBrush(Color.FromArgb(50, 255, 255, 0)); // Semi-transparent yellow background

        // Update delete button tooltip
        string fileName = Path.GetFileNameWithoutExtension(texturePath);
        DeleteTextureButton.ToolTip = $"Delete texture: {fileName}";
    }
}