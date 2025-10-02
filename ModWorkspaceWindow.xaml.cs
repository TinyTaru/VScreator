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
        // Open the item creation window with refresh callback
        var itemCreationWindow = new ItemCreationWindow(_modId, _modName, RefreshItemsList);
        itemCreationWindow.ShowDialog();
    }

    private void AddBlockButton_Click(object sender, RoutedEventArgs e)
    {
        // Open the block creation window with refresh callback
        var blockCreationWindow = new BlockCreationWindow(_modId, _modName, RefreshBlocksList);
        blockCreationWindow.ShowDialog();
    }

    private void AddRecipeButton_Click(object sender, RoutedEventArgs e)
    {
        // Open the recipe creation window with mod information
        var recipeCreationWindow = new RecipeCreationWindow(_modId, _modName);
        recipeCreationWindow.ShowDialog();
    }

    private void EditRecipeButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var button = sender as Button;
            if (button != null)
            {
                string recipeName = button.Tag as string;
                if (!string.IsNullOrEmpty(recipeName))
                {
                    EditRecipe(recipeName);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error editing recipe: {ex.Message}", "Edit Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void EditRecipe(string recipeName)
    {
        try
        {
            string modDirectory = GetModDirectory();
            string recipesPath = Path.Combine(modDirectory, "assets", _modId, "recipes", "grid");
            string recipeFilePath = Path.Combine(recipesPath, $"{recipeName}.json");

            if (!File.Exists(recipeFilePath))
            {
                MessageBox.Show($"Recipe file not found: {recipeFilePath}", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string jsonContent = File.ReadAllText(recipeFilePath);
            var recipeData = System.Text.Json.JsonSerializer.Deserialize<GridRecipeData>(jsonContent);

            if (recipeData != null)
            {
                // Open RecipeCreationWindow with pre-filled data and refresh callback
                var recipeCreationWindow = new RecipeCreationWindow(_modId, _modName, recipeData, recipeName, RefreshRecipesList);
                recipeCreationWindow.ShowDialog();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading recipe for editing: {ex.Message}", "Edit Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RefreshRecipesList()
    {
        // Refresh the recipes list by reloading from disk
        LoadRecipes();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show($"Mod settings will be implemented here.\n\nMod: {_modName}\nID: {_modId}",
                        "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OpenModFolderButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string modDirectory = GetModDirectory();

            if (Directory.Exists(modDirectory))
            {
                // Open the mod folder in Windows Explorer
                System.Diagnostics.Process.Start("explorer.exe", modDirectory);

                System.Diagnostics.Debug.WriteLine($"Opened mod folder: {modDirectory}");
            }
            else
            {
                MessageBox.Show($"Mod folder does not exist yet.\n\nExpected location: {modDirectory}\n\n" +
                              "Create some content (items, blocks, or recipes) first to generate the mod folder structure.",
                              "Folder Not Found", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error opening mod folder:\n\n{ex.Message}",
                          "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
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

            // Show content tab content, hide resources tab content
            ContentTabContent.Visibility = Visibility.Visible;
            ResourcesTabContent.Visibility = Visibility.Collapsed;
            DeleteTextureButton.Visibility = Visibility.Collapsed;

            // Load items, blocks, and recipes when switching to Content tab
            LoadItems();
            LoadBlocks();
            LoadRecipes();
        }
        else if (tabName == "Resources")
        {
            // Hide content tab buttons, show resources tab buttons
            ContentTabButtons.Visibility = Visibility.Collapsed;
            ResourcesTabButtons.Visibility = Visibility.Visible;

            // Hide content tab content, show resources tab content
            ContentTabContent.Visibility = Visibility.Collapsed;
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

    private void LoadItems()
    {
        try
        {
            // Clear existing items
            ItemsListView.Items.Clear();

            string modDirectory = GetModDirectory();
            string itemTypesPath = Path.Combine(modDirectory, "assets", _modId, "itemtypes");

            if (!Directory.Exists(itemTypesPath))
            {
                // No items found yet
                ItemsHeader.Text = "Items: (No items created yet)";
                return;
            }

            string[] itemFiles = Directory.GetFiles(itemTypesPath, "*.json");

            foreach (string itemFile in itemFiles)
            {
                try
                {
                    string itemCode = Path.GetFileNameWithoutExtension(itemFile);
                    string jsonContent = File.ReadAllText(itemFile);

                    // Parse the item data
                    var itemData = System.Text.Json.JsonSerializer.Deserialize<ItemData>(jsonContent);
                    if (itemData != null)
                    {
                        // Get the item name from en.json if available
                        string itemName = GetItemNameFromEnJson(itemCode);

                        var itemViewModel = new ItemViewModel
                        {
                            Code = itemCode,
                            Name = itemName,
                            Type = "Item"
                        };

                        ItemsListView.Items.Add(itemViewModel);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading item {itemFile}: {ex.Message}");
                }
            }

            // Update header based on item count
            ItemsHeader.Text = $"Items: ({ItemsListView.Items.Count})";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading items: {ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadBlocks()
    {
        try
        {
            // Clear existing blocks
            BlocksListView.Items.Clear();

            string modDirectory = GetModDirectory();
            string blockTypesPath = Path.Combine(modDirectory, "assets", _modId, "blocktypes");

            if (!Directory.Exists(blockTypesPath))
            {
                // No blocks found yet
                BlocksHeader.Text = "Blocks: (No blocks created yet)";
                return;
            }

            string[] blockFiles = Directory.GetFiles(blockTypesPath, "*.json");

            foreach (string blockFile in blockFiles)
            {
                try
                {
                    string blockCode = Path.GetFileNameWithoutExtension(blockFile);
                    string jsonContent = File.ReadAllText(blockFile);

                    // Parse the block data
                    var blockData = System.Text.Json.JsonSerializer.Deserialize<BlockData>(jsonContent);
                    if (blockData != null)
                    {
                        // Get the block name from en.json if available
                        string blockName = GetBlockNameFromEnJson(blockCode);

                        var blockViewModel = new BlockViewModel
                        {
                            Code = blockCode,
                            Name = blockName,
                            Type = "Block",
                            Resistance = blockData.Resistance
                        };

                        BlocksListView.Items.Add(blockViewModel);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading block {blockFile}: {ex.Message}");
                }
            }

            // Update header based on block count
            BlocksHeader.Text = $"Blocks: ({BlocksListView.Items.Count})";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading blocks: {ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadRecipes()
    {
        try
        {
            // Clear existing recipes
            RecipesListView.Items.Clear();

            string modDirectory = GetModDirectory();
            string recipesPath = Path.Combine(modDirectory, "assets", _modId, "recipes", "grid");

            if (!Directory.Exists(recipesPath))
            {
                // No recipes found yet
                RecipesHeader.Text = "Recipes: (No recipes created yet)";
                return;
            }

            string[] recipeFiles = Directory.GetFiles(recipesPath, "*.json");

            foreach (string recipeFile in recipeFiles)
            {
                try
                {
                    string recipeName = Path.GetFileNameWithoutExtension(recipeFile);
                    string jsonContent = File.ReadAllText(recipeFile);

                    // Parse the recipe data
                    var recipeData = System.Text.Json.JsonSerializer.Deserialize<GridRecipeData>(jsonContent);
                    if (recipeData != null)
                    {
                        var recipeViewModel = new RecipeViewModel
                        {
                            Name = recipeName,
                            OutputType = recipeData.recipe.output.type,
                            OutputCode = recipeData.recipe.output.code,
                            OutputQuantity = recipeData.recipe.output.quantity,
                            IngredientCount = recipeData.recipe.ingredients.Count,
                            GridSize = $"{recipeData.recipe.width}x{recipeData.recipe.height}"
                        };

                        RecipesListView.Items.Add(recipeViewModel);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading recipe {recipeFile}: {ex.Message}");
                }
            }

            // Update header based on recipe count
            RecipesHeader.Text = $"Recipes: ({RecipesListView.Items.Count})";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading recipes: {ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private string GetItemNameFromEnJson(string itemCode)
    {
        try
        {
            string modDirectory = GetModDirectory();
            string langDirectory = Path.Combine(modDirectory, "assets", _modId, "lang");
            string enJsonPath = Path.Combine(langDirectory, "en.json");

            if (File.Exists(enJsonPath))
            {
                string jsonContent = File.ReadAllText(enJsonPath);
                string itemKey = $"item-{itemCode}";

                // Simple extraction for the item name
                string pattern = $"\"{itemKey}\"\\s*:\\s*\"([^\"]+)\"";
                var match = System.Text.RegularExpressions.Regex.Match(jsonContent, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error reading item name from en.json: {ex.Message}");
        }

        // Fallback to item code if name not found
        return itemCode;
    }

    private string GetBlockNameFromEnJson(string blockCode)
    {
        try
        {
            string modDirectory = GetModDirectory();
            string langDirectory = Path.Combine(modDirectory, "assets", _modId, "lang");
            string enJsonPath = Path.Combine(langDirectory, "en.json");

            if (File.Exists(enJsonPath))
            {
                string jsonContent = File.ReadAllText(enJsonPath);
                string blockKey = $"block-{blockCode}";

                // Simple extraction for the block name
                string pattern = $"\"{blockKey}\"\\s*:\\s*\"([^\"]+)\"";
                var match = System.Text.RegularExpressions.Regex.Match(jsonContent, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error reading block name from en.json: {ex.Message}");
        }

        // Fallback to block code if name not found
        return blockCode;
    }

    private void ItemsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Handle selection change if needed
    }

    private void BlocksListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Handle selection change if needed
    }

    private void EditBlockButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var button = sender as Button;
            if (button != null)
            {
                string blockCode = button.Tag as string;
                if (!string.IsNullOrEmpty(blockCode))
                {
                    EditBlock(blockCode);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error editing block: {ex.Message}", "Edit Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void EditBlock(string blockCode)
    {
        try
        {
            string modDirectory = GetModDirectory();
            string blockTypesPath = Path.Combine(modDirectory, "assets", _modId, "blocktypes");
            string blockFilePath = Path.Combine(blockTypesPath, $"{blockCode}.json");

            if (!File.Exists(blockFilePath))
            {
                MessageBox.Show($"Block file not found: {blockFilePath}", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string jsonContent = File.ReadAllText(blockFilePath);
            var blockData = System.Text.Json.JsonSerializer.Deserialize<BlockData>(jsonContent);

            if (blockData != null)
            {
                // Get block name from en.json
                string blockName = GetBlockNameFromEnJson(blockCode);

                // Open BlockCreationWindow with pre-filled data
                var blockCreationWindow = new BlockCreationWindow(_modId, _modName, blockData, blockName, RefreshBlocksList);
                blockCreationWindow.ShowDialog();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading block for editing: {ex.Message}", "Edit Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void EditItemButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var button = sender as Button;
            if (button != null)
            {
                string itemCode = button.Tag as string;
                if (!string.IsNullOrEmpty(itemCode))
                {
                    EditItem(itemCode);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error editing item: {ex.Message}", "Edit Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RefreshItemsList()
    {
        // Refresh the items list by reloading from disk
        LoadItems();
    }

    private void RefreshBlocksList()
    {
        // Refresh the blocks list by reloading from disk
        LoadBlocks();
    }

    private void EditItem(string itemCode)
    {
        try
        {
            string modDirectory = GetModDirectory();
            string itemTypesPath = Path.Combine(modDirectory, "assets", _modId, "itemtypes");
            string itemFilePath = Path.Combine(itemTypesPath, $"{itemCode}.json");

            if (!File.Exists(itemFilePath))
            {
                MessageBox.Show($"Item file not found: {itemFilePath}", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string jsonContent = File.ReadAllText(itemFilePath);
            var itemData = System.Text.Json.JsonSerializer.Deserialize<ItemData>(jsonContent);

            if (itemData != null)
            {
                // Get item name from en.json
                string itemName = GetItemNameFromEnJson(itemCode);

                // Open ItemCreationWindow with pre-filled data and refresh callback
                var itemCreationWindow = new ItemCreationWindow(_modId, _modName, itemData, itemName, RefreshItemsList);
                itemCreationWindow.ShowDialog();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading item for editing: {ex.Message}", "Edit Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

// View model for items list
public class ItemViewModel
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
}

// View model for blocks list
public class BlockViewModel
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public double Resistance { get; set; }
}

// View model for recipes list
public class RecipeViewModel
{
    public string Name { get; set; } = "";
    public string OutputType { get; set; } = "";
    public string OutputCode { get; set; } = "";
    public int OutputQuantity { get; set; }
    public int IngredientCount { get; set; }
    public string GridSize { get; set; } = "";
}

// Data model for grid-based recipes (for JSON deserialization)
public class GridRecipeData
{
    public bool enabled { get; set; }
    public GridRecipeContent recipe { get; set; } = new GridRecipeContent();
}

public class GridRecipeContent
{
    public string ingredientPattern { get; set; } = "";
    public Dictionary<string, RecipeIngredientData> ingredients { get; set; } = new Dictionary<string, RecipeIngredientData>();
    public int width { get; set; }
    public int height { get; set; }
    public RecipeOutputData output { get; set; } = new RecipeOutputData();
}

public class RecipeIngredientData
{
    public string type { get; set; } = "";
    public string code { get; set; } = "";
    public int quantity { get; set; }
}

public class RecipeOutputData
{
    public string type { get; set; } = "";
    public string code { get; set; } = "";
    public int quantity { get; set; }
}