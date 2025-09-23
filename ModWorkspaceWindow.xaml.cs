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
    private string? _selectedTexturePath = null;
    private string? _selectedTextureType = null; // "item" or "block"
    private Border? _selectedTextureBorder = null;

    // Model selection tracking
    private string? _selectedModelPath = null;
    private string? _selectedModelType = null; // "item" or "block"
    private Border? _selectedModelBorder = null;

    public ModWorkspaceWindow(string modId, string modName)
    {
        InitializeComponent();
        _modId = modId;
        _modName = modName;

        // Update the window title with mod information
        Title = $"VScreator - {_modName} Workspace";
        SwitchToTab("Content");
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
            DeleteTextureButton.Visibility = Visibility.Collapsed;
        }
        else if (tabName == "Resources")
        {
            // Hide content tab buttons, show resources tab buttons
            ContentTabButtons.Visibility = Visibility.Collapsed;
            ResourcesTabButtons.Visibility = Visibility.Visible;

            // Hide content tab message, show resources tab content
            ContentTabMessage.Visibility = Visibility.Collapsed;
            ResourcesTabContent.Visibility = Visibility.Visible;
            DeleteTextureButton.Visibility = Visibility.Visible;

            // Load textures and models when switching to Resources tab
            LoadTextures();
            LoadModels();
        }
    }

    private void DeleteTextureButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Check if a texture or model is selected
            if (string.IsNullOrEmpty(_selectedTexturePath) && string.IsNullOrEmpty(_selectedModelPath))
            {
                MessageBox.Show(
                    "Please select a texture or model first by clicking on it.\n\n" +
                    "Selected items will be highlighted with a yellow border.",
                    "No Item Selected",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            string? selectedPath, selectedType, itemType, itemTypeName;

            if (!string.IsNullOrEmpty(_selectedTexturePath))
            {
                // Handle texture deletion
                selectedPath = _selectedTexturePath;
                selectedType = _selectedTextureType;
                itemType = "texture";
                itemTypeName = selectedType == "item" ? "Item" : "Block";
            }
            else
            {
                // Handle shape deletion
                selectedPath = _selectedModelPath;
                selectedType = _selectedModelType;
                itemType = "shape";
                itemTypeName = selectedType == "item" ? "Item" : "Block";
            }

            // Get item information
            string fileName = Path.GetFileNameWithoutExtension(selectedPath);

            // Show confirmation dialog
            MessageBoxResult result = MessageBox.Show(
                $"Are you sure you want to delete the {itemTypeName.ToLower()} {itemType} '{fileName}'?\n\n" +
                "This action cannot be undone.",
                "Confirm Deletion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);

            if (result == MessageBoxResult.Yes)
            {
                // Force garbage collection to release file handles
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // Small delay to ensure file handles are released
                System.Threading.Thread.Sleep(100);

                // Delete the file
                File.Delete(selectedPath);

                // Show success message
                MessageBox.Show(
                    $"{itemType} '{fileName}' has been deleted successfully!\n\n" +
                    $"Type: {itemTypeName}\n" +
                    $"Mod: {_modName}",
                    $"{itemType} Deleted",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Clear selections
                ClearTextureSelection();
                ClearModelSelection();

                // Refresh galleries
                LoadTextures();
                LoadModels();

                // Update delete button tooltip
                DeleteTextureButton.ToolTip = "Delete selected texture or model";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"An error occurred while deleting the item:\n\n{ex.Message}",
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

    private void ClearModelSelection()
    {
        // Clear visual selection
        if (_selectedModelBorder != null)
        {
            _selectedModelBorder.BorderBrush = new SolidColorBrush(Colors.Transparent);
            _selectedModelBorder.BorderThickness = new Thickness(0);
            _selectedModelBorder.Background = new SolidColorBrush(Colors.Transparent);
        }

        // Clear selection data
        _selectedModelPath = null;
        _selectedModelType = null;
        _selectedModelBorder = null;
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

    private void ImportModelButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Open file dialog to select JSON model file
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select Model File",
                Filter = "JSON Files (*.json)|*.json",
                CheckFileExists = true,
                CheckPathExists = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string selectedFilePath = openFileDialog.FileName;

                // Ask user for shape type using custom dialog
                var textureTypeDialog = new TextureTypeSelectionWindow();
                textureTypeDialog.Title = "Select Shape Type";
                var dialogResult = textureTypeDialog.ShowDialog();

                if (dialogResult != true)
                {
                    return; // User cancelled
                }

                bool isBlockModel = (textureTypeDialog.SelectedTextureType == "block");

                // Create folder structure
                string modDirectory = GetModDirectory();
                string modelType = isBlockModel ? "block" : "item";
                string modelPath = Path.Combine(modDirectory, "assets", _modId, "shapes", modelType);

                // Create directories if they don't exist
                Directory.CreateDirectory(modelPath);

                // Copy file to destination
                string fileName = Path.GetFileName(selectedFilePath);
                string destinationPath = Path.Combine(modelPath, fileName);

                File.Copy(selectedFilePath, destinationPath, true);

                // Show success message
                string shapeTypeName = isBlockModel ? "block" : "item";
                MessageBox.Show(
                    $"Shape '{fileName}' has been imported successfully!\n\n" +
                    $"Type: {shapeTypeName}\n" +
                    $"Location: {modelPath}\n" +
                    $"Mod: {_modName}",
                    "Import Successful",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Refresh model gallery if we're currently in Resources tab
                if (_currentTab == "Resources")
                {
                    LoadModels();
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"An error occurred while importing the model:\n\n{ex.Message}",
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

    private void LoadModels()
    {
        try
        {
            // Clear existing shapes
            ItemModelsPanel.Children.Clear();
            BlockModelsPanel.Children.Clear();

            string modDirectory = GetModDirectory();
            string assetsPath = Path.Combine(modDirectory, "assets", _modId, "shapes");

            if (!Directory.Exists(assetsPath))
            {
                return; // No assets folder yet
            }

            // Load item shapes
            string itemShapesPath = Path.Combine(assetsPath, "item");
            if (Directory.Exists(itemShapesPath))
            {
                LoadModelsFromFolder(itemShapesPath, ItemModelsPanel, "item");
            }

            // Load block shapes
            string blockShapesPath = Path.Combine(assetsPath, "block");
            if (Directory.Exists(blockShapesPath))
            {
                LoadModelsFromFolder(blockShapesPath, BlockModelsPanel, "block");
            }

            // Update headers visibility based on content
            ItemModelsHeader.Visibility = ItemModelsPanel.Children.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            BlockModelsHeader.Visibility = BlockModelsPanel.Children.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading shapes: {ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadModelsFromFolder(string folderPath, WrapPanel targetPanel, string shapeType)
    {
        string[] shapeFiles = Directory.GetFiles(folderPath, "*.json");

        foreach (string shapeFile in shapeFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(shapeFile);

            // Create border for selection visual feedback
            Border shapeBorder = new Border
            {
                Margin = new Thickness(10),
                CornerRadius = new CornerRadius(5),
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = shapeFile, // Store the full path for deletion
                Background = new SolidColorBrush(Colors.Transparent)
            };

            // Create shape display container
            StackPanel shapeContainer = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Background = new SolidColorBrush(Colors.Transparent)
            };

            // Create a simple 3D cube representation for the shape preview
            Border shapePreview = new Border
            {
                Width = 64,
                Height = 64,
                Margin = new Thickness(5),
                Background = new SolidColorBrush(Color.FromRgb(139, 142, 76)), // Vintage Story green color
                BorderBrush = new SolidColorBrush(Color.FromRgb(90, 95, 58)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3)
            };

            // Add a simple 3D effect
            shapePreview.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Colors.Black,
                Direction = 315,
                ShadowDepth = 2,
                Opacity = 0.3
            };

            // Create label with shape name
            TextBlock shapeLabel = new TextBlock
            {
                Text = fileName,
                Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#cccccc")),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 80,
                Margin = new Thickness(5)
            };

            // Add preview and label to container
            shapeContainer.Children.Add(shapePreview);
            shapeContainer.Children.Add(shapeLabel);

            // Add container to border
            shapeBorder.Child = shapeContainer;

            // Add click event handler for shape selection
            shapeBorder.MouseLeftButtonUp += (sender, e) =>
            {
                SelectModel(shapeFile, shapeType, shapeBorder);
            };

            // Add double-click event handler for shape preview
            shapeBorder.MouseLeftButtonDown += (sender, e) =>
            {
                if (e.ClickCount == 2)
                {
                    PreviewModel(shapeFile);
                }
            };

            // Add container to panel
            targetPanel.Children.Add(shapeBorder);
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
                // Load the image with proper caching settings
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(textureFile, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad; // Cache the image in memory
                bitmap.EndInit();
                bitmap.Freeze(); // Make it immutable to help with cleanup

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

    private void SelectModel(string shapePath, string shapeType, Border clickedBorder)
    {
        // Clear previous selection
        if (_selectedModelBorder != null)
        {
            _selectedModelBorder.BorderBrush = new SolidColorBrush(Colors.Transparent);
            _selectedModelBorder.Background = new SolidColorBrush(Colors.Transparent);
        }

        // Set new selection
        _selectedModelPath = shapePath;
        _selectedModelType = shapeType;
        _selectedModelBorder = clickedBorder;

        // Visual feedback for selected shape
        clickedBorder.BorderBrush = new SolidColorBrush(Colors.Yellow);
        clickedBorder.BorderThickness = new Thickness(2);
        clickedBorder.Background = new SolidColorBrush(Color.FromArgb(50, 255, 255, 0)); // Semi-transparent yellow background

        // Update delete button tooltip
        string fileName = Path.GetFileNameWithoutExtension(shapePath);
        DeleteTextureButton.ToolTip = $"Delete shape: {fileName}";
    }

    private void PreviewModel(string shapePath)
    {
        try
        {
            // Show shape information for now (ModelViewerWindow will be implemented later)
            string fileName = Path.GetFileNameWithoutExtension(shapePath);
            MessageBox.Show(
                $"Shape Preview:\n\n" +
                $"File: {fileName}\n" +
                $"Path: {shapePath}\n\n" +
                $"Shape viewer functionality will be implemented in a future update.",
                "Shape Preview",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"An error occurred while previewing the shape:\n\n{ex.Message}",
                "Preview Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}