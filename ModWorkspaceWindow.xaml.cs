using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.IO;
using System.IO.Compression;

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
        var recipeCreationWindow = new RecipeCreationWindow(_modId, _modName, RefreshRecipesList);
        recipeCreationWindow.ShowDialog();
    }

    private void AddWorldgenButton_Click(object sender, RoutedEventArgs e)
    {
        // Open the world gen creation window with refresh callback
        var worldgenCreationWindow = new WorldGenCreationWindow(_modId, _modName, RefreshWorldgenList);
        worldgenCreationWindow.ShowDialog();
    }

    private void AddCropButton_Click(object sender, RoutedEventArgs e)
    {
        // Open the crop creation window with refresh callback
        var cropCreationWindow = new CropCreationWindow(_modId, _modName, RefreshCropsList);
        cropCreationWindow.ShowDialog();
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

    private void ExportModButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string modDirectory = GetModDirectory();

            if (!Directory.Exists(modDirectory))
            {
                MessageBox.Show($"Mod folder does not exist yet.\n\nExpected location: {modDirectory}\n\n" +
                              "Create some content (items, blocks, or recipes) first to generate the mod folder structure.",
                              "Cannot Export", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Check if mod has any content
            if (!HasModContent(modDirectory))
            {
                MessageBox.Show("No mod content found to export.\n\n" +
                              "Create some items, blocks, or recipes first before exporting.",
                              "No Content", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Create exports folder if it doesn't exist
            string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string exportsFolder = Path.Combine(exeDirectory, "mods", "exports");
            Directory.CreateDirectory(exportsFolder);

            // Create zip file name with version
            string version = GetModVersion();
            string zipFileName = $"{_modId}_{version}.zip";
            string zipFilePath = Path.Combine(exportsFolder, zipFileName);

            // Create the zip file
            ZipFile.CreateFromDirectory(modDirectory, zipFilePath);

            // Show success popup
            ShowExportSuccessDialog(zipFilePath, exportsFolder, zipFileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error exporting mod:\n\n{ex.Message}",
                          "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private bool HasModContent(string modDirectory)
    {
        try
        {
            // Check for assets folder and its subdirectories
            string assetsPath = Path.Combine(modDirectory, "assets", _modId);

            if (!Directory.Exists(assetsPath))
                return false;

            // Check for any content in itemtypes, blocktypes, recipes, textures, or shapes
            string[] contentPaths = {
                Path.Combine(assetsPath, "itemtypes"),
                Path.Combine(assetsPath, "blocktypes"),
                Path.Combine(assetsPath, "recipes"),
                Path.Combine(assetsPath, "textures"),
                Path.Combine(assetsPath, "shapes")
            };

            foreach (string contentPath in contentPaths)
            {
                if (Directory.Exists(contentPath) && Directory.GetFiles(contentPath, "*.*", SearchOption.AllDirectories).Length > 0)
                    return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private void ShowExportSuccessDialog(string zipFilePath, string exportsFolder, string zipFileName)
    {
        try
        {
            // Create a custom dialog window
            var dialog = new Window
            {
                Title = "Export Successful",
                Width = 400,
                Height = 250,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = new SolidColorBrush(Color.FromRgb(31, 29, 27)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(Color.FromRgb(135, 142, 76)),
                BorderThickness = new Thickness(2),
                ResizeMode = ResizeMode.NoResize,
                SizeToContent = SizeToContent.Height
            };

            // Create main stack panel
            var mainPanel = new StackPanel { Margin = new Thickness(20) };

            // Success message
            var messageText = new TextBlock
            {
                Text = "Mod exported successfully!",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.White),
                Margin = new Thickness(0, 0, 0, 15),
                TextWrapping = TextWrapping.Wrap
            };

            // File info
            var fileInfoText = new TextBlock
            {
                Text = $"Export file: {zipFileName}",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(204, 204, 204)),
                Margin = new Thickness(0, 0, 0, 15),
                TextWrapping = TextWrapping.Wrap
            };

            // Question text
            var questionText = new TextBlock
            {
                Text = "Open exports folder?",
                FontSize = 14,
                Foreground = new SolidColorBrush(Colors.White),
                Margin = new Thickness(0, 0, 0, 20),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Buttons panel
            var buttonsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Yes button (Open exports folder)
            var yesButton = new Button
            {
                Content = "Yes",
                Background = new SolidColorBrush(Color.FromRgb(135, 142, 76)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(Color.FromRgb(135, 142, 76)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(20, 8, 20, 8),
                Margin = new Thickness(0, 0, 10, 0),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 14,
                FontWeight = FontWeights.Bold
            };
            yesButton.Click += (s, e) =>
            {
                try
                {
                    System.Diagnostics.Process.Start("explorer.exe", exportsFolder);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening exports folder:\n\n{ex.Message}",
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                dialog.Close();
            };

            // No button (Just close)
            var noButton = new Button
            {
                Content = "No",
                Background = new SolidColorBrush(Color.FromRgb(64, 64, 64)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(Color.FromRgb(64, 64, 64)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(20, 8, 20, 8),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 14,
                FontWeight = FontWeights.Bold
            };
            noButton.Click += (s, e) => dialog.Close();

            // Add elements to panels
            buttonsPanel.Children.Add(yesButton);
            buttonsPanel.Children.Add(noButton);

            mainPanel.Children.Add(messageText);
            mainPanel.Children.Add(fileInfoText);
            mainPanel.Children.Add(questionText);
            mainPanel.Children.Add(buttonsPanel);

            dialog.Content = mainPanel;
            dialog.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error showing export success dialog:\n\n{ex.Message}",
                          "Dialog Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

            // Load items, blocks, recipes, world gen, and crops when switching to Content tab
            LoadItems();
            LoadBlocks();
            LoadRecipes();
            LoadWorldgen();
            LoadCrops();
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
                itemTypeName = selectedType == "item" ? "Item" : (selectedType == "crop" ? "Crop" : "Block");
            }
            else
            {
                // Handle shape deletion
                selectedPath = _selectedModelPath;
                selectedType = _selectedModelType;
                itemType = "shape";
                itemTypeName = selectedType == "item" ? "Item" : (selectedType == "crop" ? "Crop Shape" : "Block");
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
            // Ask user for texture type using custom dialog first
            var textureTypeDialog = new TextureTypeSelectionWindow();
            var dialogResult = textureTypeDialog.ShowDialog();

            if (dialogResult != true)
            {
                return; // User cancelled
            }

            string selectedTextureType = textureTypeDialog.SelectedTextureType;

            // Open file dialog to select PNG files (multiple selection)
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select Texture Files",
                Filter = "PNG Files (*.png)|*.png",
                CheckFileExists = true,
                CheckPathExists = true,
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string[] selectedFilePaths = openFileDialog.FileNames;

                // Create folder structure
                string modDirectory = GetModDirectory();
                string texturePath;

                if (selectedTextureType == "crop")
                {
                    // For crop textures, use the special subdirectory
                    texturePath = Path.Combine(modDirectory, "assets", _modId, "textures", "block", "plant", "crop");
                }
                else
                {
                    // For regular block or item textures
                    texturePath = Path.Combine(modDirectory, "assets", _modId, "textures", selectedTextureType);
                }

                // Create directories if they don't exist
                Directory.CreateDirectory(texturePath);

                int importedCount = 0;
                List<string> importedFiles = new List<string>();
                List<string> sanitizedFiles = new List<string>();

                foreach (string selectedFilePath in selectedFilePaths)
                {
                    try
                    {
                        // Sanitize filename: remove spaces and convert to lowercase
                        string originalFileName = Path.GetFileName(selectedFilePath);
                        string fileExtension = Path.GetExtension(originalFileName);
                        string fileNameWithoutExt = Path.GetFileNameWithoutExtension(originalFileName);

                        // Remove spaces and convert to lowercase
                        string sanitizedFileName = fileNameWithoutExt.Replace(" ", "").ToLower() + fileExtension.ToLower();

                        string destinationPath = Path.Combine(texturePath, sanitizedFileName);

                        File.Copy(selectedFilePath, destinationPath, true);

                        importedFiles.Add(originalFileName);
                        sanitizedFiles.Add(sanitizedFileName);
                        importedCount++;
                    }
                    catch (Exception fileEx)
                    {
                        MessageBox.Show(
                            $"Error importing file '{Path.GetFileName(selectedFilePath)}':\n\n{fileEx.Message}",
                            "Import Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }

                if (importedCount > 0)
                {
                    // Show success message
                    string textureTypeName = selectedTextureType;
                    string message = $"{importedCount} texture(s) imported successfully!\n\n" +
                                    $"Type: {textureTypeName}\n" +
                                    $"Location: {texturePath}\n" +
                                    $"Mod: {_modName}";

                    if (importedCount == 1)
                    {
                        message += $"\n\nOriginal: {importedFiles[0]}\nSaved as: {sanitizedFiles[0]}";
                        if (importedFiles[0] != sanitizedFiles[0])
                        {
                            message += "\n\n(Filename was sanitized: spaces removed and converted to lowercase)";
                        }
                    }
                    else
                    {
                        message += "\n\nImported files:\n" + string.Join("\n", importedFiles);
                        bool hasSanitized = importedFiles.Zip(sanitizedFiles, (orig, san) => orig != san).Any(x => x);
                        if (hasSanitized)
                        {
                            message += "\n\n(Some filenames were sanitized: spaces removed and converted to lowercase)";
                        }
                    }

                    MessageBox.Show(message, "Import Successful", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Refresh texture gallery if we're currently in Resources tab
                    if (_currentTab == "Resources")
                    {
                        LoadTextures();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"An error occurred while importing textures:\n\n{ex.Message}",
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

                // Create folder structure
                string modDirectory = GetModDirectory();
                string modelType;
                if (textureTypeDialog.SelectedTextureType == "crop")
                {
                    modelType = "block/plant/crop";
                }
                else if (textureTypeDialog.SelectedTextureType == "block")
                {
                    modelType = "block";
                }
                else
                {
                    modelType = "item";
                }
                string modelPath = Path.Combine(modDirectory, "assets", _modId, "shapes", modelType);

                // Create directories if they don't exist
                Directory.CreateDirectory(modelPath);

                // Sanitize filename: remove spaces and convert to lowercase
                string originalFileName = Path.GetFileName(selectedFilePath);
                string fileExtension = Path.GetExtension(originalFileName);
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(originalFileName);
                
                // Remove spaces and convert to lowercase
                string sanitizedFileName = fileNameWithoutExt.Replace(" ", "").ToLower() + fileExtension.ToLower();
                
                string destinationPath = Path.Combine(modelPath, sanitizedFileName);

                File.Copy(selectedFilePath, destinationPath, true);

                // Show success message
                string shapeTypeName = textureTypeDialog.SelectedTextureType;
                string message = $"Shape imported successfully!\n\n" +
                                $"Original: {originalFileName}\n" +
                                $"Saved as: {sanitizedFileName}\n\n" +
                                $"Type: {shapeTypeName}\n" +
                                $"Location: {modelPath}\n" +
                                $"Mod: {_modName}";
                
                if (originalFileName != sanitizedFileName)
                {
                    message += "\n\n(Filename was sanitized: spaces removed and converted to lowercase)";
                }
                
                MessageBox.Show(message, "Import Successful", MessageBoxButton.OK, MessageBoxImage.Information);

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

    private string GetModVersion()
    {
        try
        {
            string modDirectory = GetModDirectory();
            string modInfoPath = Path.Combine(modDirectory, "modinfo.json");

            if (File.Exists(modInfoPath))
            {
                string jsonContent = File.ReadAllText(modInfoPath);
                // Simple JSON parsing to extract version
                string versionPattern = "\"version\"\\s*:\\s*\"([^\"]+)\"";
                var match = System.Text.RegularExpressions.Regex.Match(jsonContent, versionPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error reading mod version: {ex.Message}");
        }

        // Fallback to default version if not found
        return "1.0.0";
    }

    private void LoadTextures()
    {
        try
        {
            // Clear existing textures
            ItemTexturesPanel.Children.Clear();
            BlockTexturesPanel.Children.Clear();
            CropTexturesPanel.Children.Clear();

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

            // Load crop textures (from the special subdirectory)
            string cropTexturesPath = Path.Combine(assetsPath, "block", "plant", "crop");
            if (Directory.Exists(cropTexturesPath))
            {
                LoadTexturesFromFolder(cropTexturesPath, CropTexturesPanel, "crop");
            }

            // Update headers visibility based on content
            ItemTexturesHeader.Visibility = ItemTexturesPanel.Children.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            BlockTexturesHeader.Visibility = BlockTexturesPanel.Children.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            CropTexturesHeader.Visibility = CropTexturesPanel.Children.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
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

            RenderOptions.SetBitmapScalingMode(textureImage, BitmapScalingMode.NearestNeighbor);
            textureImage.SnapsToDevicePixels = true;

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

    private void LoadWorldgen()
    {
        try
        {
            // Clear existing world gen
            WorldgenListView.Items.Clear();

            string modDirectory = GetModDirectory();
            string worldgenPath = Path.Combine(modDirectory, "assets", _modId, "worldgen", "blockpatches");

            if (!Directory.Exists(worldgenPath))
            {
                // No world gen found yet
                WorldgenHeader.Text = "World Gen: (No world gen created yet)";
                return;
            }

            string[] worldgenFiles = Directory.GetFiles(worldgenPath, "*.json");

            foreach (string worldgenFile in worldgenFiles)
            {
                try
                {
                    string worldgenName = Path.GetFileNameWithoutExtension(worldgenFile);
                    string jsonContent = File.ReadAllText(worldgenFile);

                    // Parse the world gen data
                    var worldgenData = System.Text.Json.JsonSerializer.Deserialize<WorldGenData>(jsonContent);
                    if (worldgenData != null)
                    {
                        var worldgenViewModel = new WorldgenViewModel
                        {
                            Name = worldgenName,
                            Comment = worldgenData.Comment,
                            BlockCodes = string.Join(", ", worldgenData.BlockCodes),
                            Placement = worldgenData.Placement,
                            Chance = worldgenData.Chance,
                            QuantityAvg = worldgenData.Quantity.Avg,
                            QuantityVar = worldgenData.Quantity.Var
                        };

                        WorldgenListView.Items.Add(worldgenViewModel);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading world gen {worldgenFile}: {ex.Message}");
                }
            }

            // Update header based on world gen count
            WorldgenHeader.Text = $"World Gen: ({WorldgenListView.Items.Count})";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading world gen: {ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadCrops()
    {
        try
        {
            // Clear existing crops
            CropsListView.Items.Clear();

            string modDirectory = GetModDirectory();
            string cropsPath = Path.Combine(modDirectory, "assets", _modId, "blocktypes", "plant", "crop");

            if (!Directory.Exists(cropsPath))
            {
                // No crops found yet
                CropsHeader.Text = "Crops: (No crops created yet)";
                return;
            }

            string[] cropFiles = Directory.GetFiles(cropsPath, "*.json");

            foreach (string cropFile in cropFiles)
            {
                try
                {
                    string cropCode = Path.GetFileNameWithoutExtension(cropFile);
                    string jsonContent = File.ReadAllText(cropFile);

                    // Parse the crop data
                    var cropData = System.Text.Json.JsonSerializer.Deserialize<CropData>(jsonContent);
                    if (cropData != null)
                    {
                        // Get the crop name from en.json if available
                        string cropName = GetCropNameFromEnJson(cropCode);

                        var cropViewModel = new CropViewModel
                        {
                            Code = cropCode,
                            Name = cropName,
                            Type = "Crop",
                            GrowthStages = cropData.variantgroups.Length > 0 ? cropData.variantgroups[0].states.Length : 0,
                            RequiredNutrient = cropData.cropProps?.requiredNutrient ?? "N"
                        };

                        CropsListView.Items.Add(cropViewModel);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading crop {cropFile}: {ex.Message}");
                }
            }

            // Update header based on crop count
            CropsHeader.Text = $"Crops: ({CropsListView.Items.Count})";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading crops: {ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

    private string GetCropNameFromEnJson(string cropCode)
    {
        try
        {
            string modDirectory = GetModDirectory();
            string langDirectory = Path.Combine(modDirectory, "assets", _modId, "lang");
            string enJsonPath = Path.Combine(langDirectory, "en.json");

            if (File.Exists(enJsonPath))
            {
                string jsonContent = File.ReadAllText(enJsonPath);
                string cropKey = $"block-crop-{cropCode}";

                // Simple extraction for the crop name
                string pattern = $"\"{cropKey}\"\\s*:\\s*\"([^\"]+)\"";
                var match = System.Text.RegularExpressions.Regex.Match(jsonContent, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error reading crop name from en.json: {ex.Message}");
        }

        // Fallback to crop code if name not found
        return cropCode;
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

    private void RefreshWorldgenList()
    {
        // Refresh the world gen list by reloading from disk
        LoadWorldgen();
    }

    private void RefreshCropsList()
    {
        // Refresh the crops list by reloading from disk
        LoadCrops();
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

    private void EditWorldgenButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var button = sender as Button;
            if (button != null)
            {
                string worldgenName = button.Tag as string;
                if (!string.IsNullOrEmpty(worldgenName))
                {
                    EditWorldgen(worldgenName);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error editing world gen: {ex.Message}", "Edit Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void EditWorldgen(string worldgenName)
    {
        try
        {
            string modDirectory = GetModDirectory();
            string worldgenPath = Path.Combine(modDirectory, "assets", _modId, "worldgen", "blockpatches");
            string worldgenFilePath = Path.Combine(worldgenPath, $"{worldgenName}.json");

            if (!File.Exists(worldgenFilePath))
            {
                MessageBox.Show($"World gen file not found: {worldgenFilePath}", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string jsonContent = File.ReadAllText(worldgenFilePath);
            var worldgenData = System.Text.Json.JsonSerializer.Deserialize<WorldGenData>(jsonContent);

            if (worldgenData != null)
            {
                // Open WorldGenCreationWindow with pre-filled data and refresh callback
                var worldgenCreationWindow = new WorldGenCreationWindow(_modId, _modName, worldgenData, worldgenName, RefreshWorldgenList);
                worldgenCreationWindow.ShowDialog();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading world gen for editing: {ex.Message}", "Edit Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void EditCropButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var button = sender as Button;
            if (button != null)
            {
                string cropCode = button.Tag as string;
                if (!string.IsNullOrEmpty(cropCode))
                {
                    EditCrop(cropCode);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error editing crop: {ex.Message}", "Edit Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void EditCrop(string cropCode)
    {
        try
        {
            string modDirectory = GetModDirectory();
            string cropsPath = Path.Combine(modDirectory, "assets", _modId, "blocktypes", "plant", "crop");
            string cropFilePath = Path.Combine(cropsPath, $"{cropCode}.json");

            if (!File.Exists(cropFilePath))
            {
                MessageBox.Show($"Crop file not found: {cropFilePath}", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string jsonContent = File.ReadAllText(cropFilePath);
            var cropData = System.Text.Json.JsonSerializer.Deserialize<CropData>(jsonContent);

            if (cropData != null)
            {
                // Get crop name from en.json
                string cropName = GetCropNameFromEnJson(cropCode);

                // Open CropCreationWindow with pre-filled data and refresh callback
                var cropCreationWindow = new CropCreationWindow(_modId, _modName, cropData, cropName, RefreshCropsList);
                cropCreationWindow.ShowDialog();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading crop for editing: {ex.Message}", "Edit Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

// View model for world gen list
public class WorldgenViewModel
{
    public string Name { get; set; } = "";
    public string Comment { get; set; } = "";
    public string BlockCodes { get; set; } = "";
    public string Placement { get; set; } = "";
    public int Chance { get; set; }
    public double QuantityAvg { get; set; }
    public double QuantityVar { get; set; }
}

// View model for crops list
public class CropViewModel
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public int GrowthStages { get; set; }
    public string RequiredNutrient { get; set; } = "";
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