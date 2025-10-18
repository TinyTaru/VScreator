using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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
    private ObservableCollection<SelectableItem> _filteredItems = new ObservableCollection<SelectableItem>();
    private string _currentTab = "Items";
    private string _searchText = "";
    private SelectableItem? _selectedItem;
    private readonly bool _blocksOnly;

    public SelectableItem? SelectedItem => _selectedItem;

    public ItemBlockSelectorWindow(bool blocksOnly = false)
    {
        _blocksOnly = blocksOnly;
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

            // Wait for both to complete
            await System.Threading.Tasks.Task.WhenAll(loadItemsTask, loadBlocksTask);
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

    private void DisplayItems()
    {
        var itemsToDisplay = _currentTab == "Items" ? _availableItems : _availableBlocks;

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