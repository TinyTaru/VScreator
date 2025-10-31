using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace VScreator;

/// <summary>
/// Data model for selectable items/blocks
/// </summary>
public class SelectableItem
{
    public string Type { get; set; } = ""; // "item" or "block"
    public string Code { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string IconPath { get; set; } = "";
    public BitmapImage? Icon { get; set; }
}

/// <summary>
/// Interaction logic for ItemBlockSelectorWindow.xaml
/// </summary>
public partial class ItemBlockSelectorWindow : Window
{
    private ObservableCollection<SelectableItem> _availableItems = new ObservableCollection<SelectableItem>();
    private ObservableCollection<SelectableItem> _availableBlocks = new ObservableCollection<SelectableItem>();
    private ObservableCollection<SelectableItem> _customItems = new ObservableCollection<SelectableItem>();
    private ObservableCollection<SelectableItem> _filteredItems = new ObservableCollection<SelectableItem>();
    private string _currentTab = "Items";
    private string _searchText = "";
    private SelectableItem? _selectedItem;
    private readonly bool _blocksOnly;
    private string? _modId;

    public SelectableItem? SelectedItem => _selectedItem;

    public ItemBlockSelectorWindow(bool blocksOnly = false, string? modId = null)
    {
        _blocksOnly = blocksOnly;
        _modId = modId;
        InitializeComponent();

        if (_blocksOnly)
        {
            _currentTab = "Blocks";
            ItemsTabButton.Visibility = Visibility.Collapsed;
        }

        // Load items and blocks in background for better UX
        _ = LoadAvailableItemsAndBlocksAsync();
    }

    private async System.Threading.Tasks.Task LoadAvailableItemsAndBlocksAsync()
    {
        await LoadAvailableItemsAndBlocks();
        DisplayItems();
    }

    private async System.Threading.Tasks.Task LoadAvailableItemsAndBlocks()
    {
        try
        {
            // Load items and blocks asynchronously
            var loadItemsTask = _blocksOnly
                ? System.Threading.Tasks.Task.CompletedTask
                : System.Threading.Tasks.Task.Run(() => LoadItems());
            var loadBlocksTask = System.Threading.Tasks.Task.Run(() => LoadBlocks());
            var loadCustomTask = System.Threading.Tasks.Task.Run(() => LoadCustomItemsAndBlocks());

            // Wait for all to complete
            await System.Threading.Tasks.Task.WhenAll(loadItemsTask, loadBlocksTask, loadCustomTask);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading items and blocks: {ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadItems()
    {
        _availableItems.Clear();

        // Optimized path finding - try most likely paths first
        string[] possiblePaths = new[]
        {
            @"C:\git\Mods\VScreator\VScreator\Resources\vintage_story icons\items",
            System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "vintage_story icons", "items"),
            System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Resources", "vintage_story icons", "items"),
            System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Resources", "vintage_story icons", "items")
        };

        string itemsPath = null;
        foreach (string path in possiblePaths)
        {
            if (Directory.Exists(path))
            {
                itemsPath = path;
                break;
            }
        }

        if (itemsPath == null)
        {
            MessageBox.Show("No valid items path found!", "Path Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (Directory.Exists(itemsPath))
        {
            var allItemFiles = Directory.GetFiles(itemsPath, "*.png");
            var itemFiles = allItemFiles.Take(500).ToArray(); // Load first 500 items

            foreach (var file in itemFiles)
            {
                string fileName = System.IO.Path.GetFileNameWithoutExtension(file);
                var item = new SelectableItem
                {
                    Type = "item",
                    Code = $"game:{fileName}",
                    DisplayName = fileName.Replace("-", " ").Replace("_", " "),
                    IconPath = file
                };

                // Load the icon with optimized settings
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(file, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.DecodePixelWidth = 64;
                    bitmap.DecodePixelHeight = 64;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    item.Icon = bitmap;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading item icon {file}: {ex.Message}");
                }

                _availableItems.Add(item);
            }
        }
    }

    private void LoadBlocks()
    {
        _availableBlocks.Clear();

        // Optimized path finding - try most likely paths first
        string[] possiblePaths = new[]
        {
            @"C:\git\Mods\VScreator\VScreator\Resources\vintage_story icons\blocks",
            System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "vintage_story icons", "blocks"),
            System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Resources", "vintage_story icons", "blocks"),
            System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Resources", "vintage_story icons", "blocks")
        };

        string blocksPath = null;
        foreach (string path in possiblePaths)
        {
            if (Directory.Exists(path))
            {
                blocksPath = path;
                break;
            }
        }

        if (blocksPath == null)
        {
            MessageBox.Show("No valid blocks path found!", "Path Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (Directory.Exists(blocksPath))
        {
            var allBlockFiles = Directory.GetFiles(blocksPath, "*.png");
            var blockFiles = allBlockFiles.Take(500).ToArray(); // Load first 500 blocks

            foreach (var file in blockFiles)
            {
                string fileName = System.IO.Path.GetFileNameWithoutExtension(file);
                var block = new SelectableItem
                {
                    Type = "block",
                    Code = $"game:{fileName}",
                    DisplayName = fileName.Replace("-", " ").Replace("_", " "),
                    IconPath = file
                };

                // Load the icon with optimized settings
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(file, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.DecodePixelWidth = 64;
                    bitmap.DecodePixelHeight = 64;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    block.Icon = bitmap;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading block icon {file}: {ex.Message}");
                }

                _availableBlocks.Add(block);
            }
        }
    }

    private void LoadCustomItemsAndBlocks()
    {
        _customItems.Clear();

        // If no modId was provided, we can't load custom items
        if (string.IsNullOrEmpty(_modId))
        {
            System.Diagnostics.Debug.WriteLine("No mod ID provided - custom items will not be loaded");
            return;
        }

        // Get the mod directory using the standard path structure
        string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string modsDirectory = Path.Combine(exeDirectory, "mods");
        string modDirectory = Path.Combine(modsDirectory, _modId);

        if (!Directory.Exists(modDirectory))
        {
            System.Diagnostics.Debug.WriteLine($"Mod directory not found: {modDirectory}");
            return;
        }

        string assetsPath = Path.Combine(modDirectory, "assets", _modId);

        // Load custom items
        string itemsPath = Path.Combine(assetsPath, "itemtypes");
        System.Diagnostics.Debug.WriteLine($"Looking for custom items in: {itemsPath}");

        if (Directory.Exists(itemsPath))
        {
            var itemFiles = Directory.GetFiles(itemsPath, "*.json");
            System.Diagnostics.Debug.WriteLine($"Found {itemFiles.Length} item files");

            foreach (var file in itemFiles)
            {
                try
                {
                    string json = File.ReadAllText(file);
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var itemData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, options);

                    if (itemData == null)
                    {
                        continue;
                    }

                    // Try to get code field (case-insensitive)
                    JsonElement codeElement;
                    bool hasCode = itemData.TryGetValue("code", out codeElement) ||
                                  itemData.TryGetValue("Code", out codeElement);

                    if (hasCode)
                    {
                        string code = codeElement.GetString() ?? "";
                        string displayName = Path.GetFileNameWithoutExtension(file).Replace("-", " ").Replace("_", " ");

                        // Try to find texture
                        string texturePath = Path.Combine(assetsPath, "textures", "item", $"{Path.GetFileNameWithoutExtension(file)}.png");

                        var item = new SelectableItem
                        {
                            Type = "item",
                            Code = code,
                            DisplayName = displayName,
                            IconPath = texturePath
                        };

                        if (File.Exists(texturePath))
                        {
                            try
                            {
                                var bitmap = new BitmapImage();
                                bitmap.BeginInit();
                                bitmap.UriSource = new Uri(texturePath, UriKind.Absolute);
                                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                bitmap.DecodePixelWidth = 64;
                                bitmap.DecodePixelHeight = 64;
                                bitmap.EndInit();
                                bitmap.Freeze();
                                item.Icon = bitmap;
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error loading custom item texture {texturePath}: {ex.Message}");
                            }
                        }

                        _customItems.Add(item);
                        System.Diagnostics.Debug.WriteLine($"Added custom item: {displayName}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading custom item {file}: {ex.Message}");
                }
            }
        }

        // Load custom blocks
        string blocksPath = Path.Combine(assetsPath, "blocktypes");
        System.Diagnostics.Debug.WriteLine($"Looking for custom blocks in: {blocksPath}");

        if (Directory.Exists(blocksPath))
        {
            var blockFiles = Directory.GetFiles(blocksPath, "*.json");
            System.Diagnostics.Debug.WriteLine($"Found {blockFiles.Length} block files");

            foreach (var file in blockFiles)
            {
                try
                {
                    string json = File.ReadAllText(file);
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var blockData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, options);

                    if (blockData == null)
                    {
                        continue;
                    }

                    // Try to get code field (case-insensitive)
                    JsonElement codeElement;
                    bool hasCode = blockData.TryGetValue("code", out codeElement) ||
                                  blockData.TryGetValue("Code", out codeElement);

                    if (hasCode)
                    {
                        string code = codeElement.GetString() ?? "";
                        string displayName = Path.GetFileNameWithoutExtension(file).Replace("-", " ").Replace("_", " ");

                        // Try to find texture - check multiple possible paths
                        string baseFileName = Path.GetFileNameWithoutExtension(file);
                        string texturesBlockPath = Path.Combine(assetsPath, "textures", "block");

                        string? texturePath = null;

                        // Try exact match first
                        string exactPath = Path.Combine(texturesBlockPath, $"{baseFileName}.png");
                        if (File.Exists(exactPath))
                        {
                            texturePath = exactPath;
                        }
                        else
                        {
                            // If exact match not found, try to find any PNG in the block textures folder
                            if (Directory.Exists(texturesBlockPath))
                            {
                                var pngFiles = Directory.GetFiles(texturesBlockPath, "*.png");
                                if (pngFiles.Length > 0)
                                {
                                    texturePath = pngFiles[0];
                                }
                            }
                        }

                        var block = new SelectableItem
                        {
                            Type = "block",
                            Code = code,
                            DisplayName = displayName,
                            IconPath = texturePath ?? ""
                        };

                        if (texturePath != null && File.Exists(texturePath))
                        {
                            try
                            {
                                var bitmap = new BitmapImage();
                                bitmap.BeginInit();
                                bitmap.UriSource = new Uri(texturePath, UriKind.Absolute);
                                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                bitmap.DecodePixelWidth = 64;
                                bitmap.DecodePixelHeight = 64;
                                bitmap.EndInit();
                                bitmap.Freeze();
                                block.Icon = bitmap;
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error loading custom block texture {texturePath}: {ex.Message}");
                            }
                        }

                        _customItems.Add(block);
                        System.Diagnostics.Debug.WriteLine($"Added custom block: {displayName}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading custom block {file}: {ex.Message}");
                }
            }
        }

        System.Diagnostics.Debug.WriteLine($"Total custom items loaded: {_customItems.Count}");
    }

    private void DisplayItems()
    {
        var itemsToDisplay = _currentTab switch
        {
            "Items" => _availableItems,
            "Blocks" => _availableBlocks,
            "Custom" => _customItems,
            _ => _availableItems
        };

        // Apply search filter if there's search text
        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            ApplySearchFilter(itemsToDisplay);
        }
        else
        {
            // No search filter, show all items
            _filteredItems.Clear();
            foreach (var item in itemsToDisplay)
            {
                _filteredItems.Add(item);
            }
        }

        // Set items source for virtualized display
        if (ItemsPanel != null)
        {
            ItemsPanel.ItemsSource = _filteredItems;
        }
    }

    private void ApplySearchFilter(IEnumerable<SelectableItem> sourceCollection)
    {
        _filteredItems.Clear();

        foreach (var item in sourceCollection)
        {
            // Search in display name and code
            if (item.DisplayName != null &&
                (item.DisplayName.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                 item.Code.Contains(_searchText, StringComparison.OrdinalIgnoreCase)))
            {
                _filteredItems.Add(item);
            }
        }
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            _searchText = textBox.Text;
            DisplayItems();
        }
    }

    private void ItemsTabButton_Click(object sender, RoutedEventArgs e)
    {
        _currentTab = "Items";
        DisplayItems();
    }

    private void BlocksTabButton_Click(object sender, RoutedEventArgs e)
    {
        _currentTab = "Blocks";
        DisplayItems();
    }

    private void CustomTabButton_Click(object sender, RoutedEventArgs e)
    {
        _currentTab = "Custom";
        DisplayItems();
    }

    private void ItemBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is SelectableItem item)
        {
            _selectedItem = item;

            // Close the window and return the selected item
            this.Close();
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _selectedItem = null; // Clear selection
        this.Close();
    }
}