using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VScreator;

/// <summary>
/// Data model for recipe ingredients
/// </summary>
public class RecipeIngredient
{
    public string Type { get; set; } = ""; // "item" or "block"
    public string Code { get; set; } = "";
    public int Quantity { get; set; } = 1;
    public string DisplayName { get; set; } = "";
    public string IconPath { get; set; } = "";
    public BitmapImage? Icon { get; set; }
}

/// <summary>
/// Data model for grid-based recipes
/// </summary>
public class GridRecipe
{
    public string RecipeName { get; set; } = "";
    public string OutputType { get; set; } = "item"; // "item" or "block"
    public string OutputCode { get; set; } = "";
    public int OutputQuantity { get; set; } = 1;
    public List<List<string>> Pattern { get; set; } = new List<List<string>>();
    public Dictionary<string, RecipeIngredient> Ingredients { get; set; } = new Dictionary<string, RecipeIngredient>();
}

/// <summary>
/// Interaction logic for RecipeCreationWindow.xaml
/// </summary>
public partial class RecipeCreationWindow : Window
{
    private readonly string _modId;
    private readonly string _modName;
    private GridRecipe _currentRecipe = new GridRecipe();
    private GridRecipeData? _existingRecipeData = null;
    private string _existingRecipeName = "";
    private Action? _refreshCallback = null;
    private ObservableCollection<RecipeIngredient> _availableItems = new ObservableCollection<RecipeIngredient>();
    private ObservableCollection<RecipeIngredient> _availableBlocks = new ObservableCollection<RecipeIngredient>();
    private ObservableCollection<RecipeIngredient> _filteredIngredients = new ObservableCollection<RecipeIngredient>();
    private RecipeIngredient? _selectedIngredient;
    private RecipeIngredient? _selectedOutput;
    private int _currentGridSize = 3;
    private string _currentTab = "Items";
    private string _searchText = "";

    // Performance optimization fields
    private Dictionary<string, BitmapImage> _imageCache = new Dictionary<string, BitmapImage>();
    private string _itemsPath = "";
    private string _blocksPath = "";
    private const int InitialLoadCount = 500;
    private const int LoadMoreCount = 200;

    public RecipeCreationWindow(string modId, string modName)
    {
        _modId = modId;
        _modName = modName;

        try
        {
            // Initialize UI components first for immediate display
            InitializeComponent();
            InitializeRecipeSystem();
            GenerateCraftingGrid();
            UpdateRecipePreview();

            System.Diagnostics.Debug.WriteLine("RecipeCreationWindow UI initialized, starting background loading...");

            // Load items and blocks in background for better UX
            _ = LoadAvailableItemsAndBlocksAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in RecipeCreationWindow constructor: {ex.Message}");
            MessageBox.Show($"Error initializing RecipeCreationWindow: {ex.Message}", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // Constructor for editing existing recipes
    public RecipeCreationWindow(string modId, string modName, GridRecipeData existingRecipeData, string existingRecipeName, Action refreshCallback)
    {
        _modId = modId;
        _modName = modName;
        _existingRecipeData = existingRecipeData;
        _existingRecipeName = existingRecipeName;
        _refreshCallback = refreshCallback;

        try
        {
            // Initialize UI components first for immediate display
            InitializeComponent();
            InitializeRecipeSystem();
            GenerateCraftingGrid();
            UpdateRecipePreview();

            System.Diagnostics.Debug.WriteLine("RecipeCreationWindow UI initialized, starting background loading...");

            // Load items and blocks in background for better UX
            _ = LoadAvailableItemsAndBlocksAsync();

            // Pre-fill the form with existing recipe data
            PreFillFormWithExistingData();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in RecipeCreationWindow constructor: {ex.Message}");
            MessageBox.Show($"Error initializing RecipeCreationWindow: {ex.Message}", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadAvailableItemsAndBlocksAsync()
    {
        await LoadAvailableItemsAndBlocks();
        System.Diagnostics.Debug.WriteLine("Background loading completed!");

        // If we're editing a recipe, populate the grid with existing ingredients
        if (_existingRecipeData != null)
        {
            PopulateGridWithExistingIngredients();
        }
    }

    private void InitializeRecipeSystem()
    {
        // Set default values
        RecipeNameTextBox.Text = "new_recipe";
        OutputQuantityTextBox.Text = "1";

        // Set up event handlers
        ItemsTabButton.Tag = "Items";
        BlocksTabButton.Tag = "Blocks";
    }

    private void PreFillFormWithExistingData()
    {
        if (_existingRecipeData != null)
        {
            // Pre-fill the form fields with existing data
            RecipeNameTextBox.Text = _existingRecipeName;
            OutputQuantityTextBox.Text = _existingRecipeData.recipe.output.quantity.ToString();

            // Set grid size based on recipe dimensions
            _currentGridSize = _existingRecipeData.recipe.width;

            // Set output ingredient first
            var outputIngredient = new RecipeIngredient
            {
                Type = _existingRecipeData.recipe.output.type,
                Code = _existingRecipeData.recipe.output.code,
                Quantity = _existingRecipeData.recipe.output.quantity,
                DisplayName = _existingRecipeData.recipe.output.code.Replace("game:", "").Replace("-", " ").Replace("_", " ")
            };

            // Find the output ingredient in available items/blocks
            var availableIngredient = _availableItems.Concat(_availableBlocks)
                .FirstOrDefault(i => i.Code == _existingRecipeData.recipe.output.code);

            if (availableIngredient != null)
            {
                outputIngredient.Icon = availableIngredient.Icon;
                outputIngredient.IconPath = availableIngredient.IconPath;
            }

            _selectedOutput = outputIngredient;

            // Initialize the recipe pattern structure
            _currentRecipe.Pattern.Clear();
            for (int i = 0; i < _currentGridSize; i++)
            {
                _currentRecipe.Pattern.Add(new List<string>(new string[_currentGridSize]));
            }

            // Fill the pattern based on ingredientPattern (new comma-separated format)
            string[] patternRows = _existingRecipeData.recipe.ingredientPattern.Split(',');
            for (int row = 0; row < _currentGridSize && row < patternRows.Length; row++)
            {
                string patternRow = patternRows[row];
                for (int col = 0; col < _currentGridSize && col < patternRow.Length; col++)
                {
                    char patternChar = patternRow[col];
                    if (patternChar != '_')
                    {
                        // Find the ingredient for this letter
                        string letterStr = patternChar.ToString();
                        if (_existingRecipeData.recipe.ingredients.TryGetValue(letterStr, out var ingredientData))
                        {
                            _currentRecipe.Pattern[row][col] = ingredientData.code;
                        }
                    }
                }
            }

            // Update the window title to indicate editing mode
            this.Title = $"Edit Recipe - {_modName}";

            // Update the button text to indicate editing mode
            CreateRecipeButton.Content = "Update Recipe";
        }
    }

    private void PopulateGridWithExistingIngredients()
    {
        if (_existingRecipeData == null) return;

        // Wait a bit for UI to be fully initialized, then populate the grid
        Dispatcher.InvokeAsync(() =>
        {
            try
            {
                // Clear existing grid first
                foreach (var child in CraftingGrid.Children.OfType<Border>())
                {
                    child.Child = null;
                }

                // Set grid size if different from current
                if (_currentGridSize != _existingRecipeData.recipe.width)
                {
                    _currentGridSize = _existingRecipeData.recipe.width;
                    GenerateCraftingGrid();
                }

                // Populate grid cells with existing ingredients (new comma-separated format)
                string[] patternRows = _existingRecipeData.recipe.ingredientPattern.Split(',');
                for (int row = 0; row < _currentGridSize && row < patternRows.Length; row++)
                {
                    string patternRow = patternRows[row];
                    for (int col = 0; col < _currentGridSize && col < patternRow.Length; col++)
                    {
                        char patternChar = patternRow[col];
                        if (patternChar != '_')
                        {
                            string ingredientCode = _currentRecipe.Pattern[row][col];
                            if (!string.IsNullOrEmpty(ingredientCode))
                            {
                                // Find the ingredient data using the letter key
                                string letterStr = patternChar.ToString();
                                if (_existingRecipeData.recipe.ingredients.TryGetValue(letterStr, out var ingredientData))
                                {
                                    var ingredient = new RecipeIngredient
                                    {
                                        Type = ingredientData.type,
                                        Code = ingredientData.code,
                                        Quantity = ingredientData.quantity,
                                        DisplayName = ingredientData.code.Replace("game:", "").Replace("-", " ").Replace("_", " ")
                                    };

                                    // Find the ingredient in available items/blocks for icon
                                    var availableIng = _availableItems.Concat(_availableBlocks)
                                        .FirstOrDefault(item => item.Code == ingredientData.code);

                                    if (availableIng != null)
                                    {
                                        ingredient.Icon = availableIng.Icon;
                                        ingredient.IconPath = availableIng.IconPath;
                                    }

                                    // Find the grid cell and add the visual
                                    var cell = CraftingGrid.Children
                                        .OfType<Border>()
                                        .FirstOrDefault(c => c.Tag?.ToString() == $"{row},{col}");

                                    if (cell != null)
                                    {
                                        var image = new Image
                                        {
                                            Source = ingredient.Icon,
                                            Width = 48,
                                            Height = 48,
                                            Stretch = Stretch.Uniform,
                                            Tag = ingredient
                                        };
                                        cell.Child = image;
                                    }
                                }
                            }
                        }
                    }
                }

                // Update output slot after grid is populated
                UpdateOutputSlotForEditing();
                UpdateRecipeIngredients();
                UpdateRecipePreview();

                System.Diagnostics.Debug.WriteLine($"Grid populated with {_existingRecipeData.recipe.ingredients.Count} ingredients");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error populating grid: {ex.Message}");
            }
        }, System.Windows.Threading.DispatcherPriority.Background);
    }

    private async Task LoadAvailableItemsAndBlocks()
    {
        System.Diagnostics.Debug.WriteLine("=== Starting LoadAvailableItemsAndBlocks (async) ===");

        try
        {
            // Load items and blocks asynchronously
            var loadItemsTask = Task.Run(() => LoadItems());
            var loadBlocksTask = Task.Run(() => LoadBlocks());

            // Wait for both to complete
            await Task.WhenAll(loadItemsTask, loadBlocksTask);

            System.Diagnostics.Debug.WriteLine("=== Calling DisplayIngredients ===");
            DisplayIngredients();
            System.Diagnostics.Debug.WriteLine("=== LoadAvailableItemsAndBlocks completed ===");

            // Log loading completion for debugging (non-blocking)
            System.Diagnostics.Debug.WriteLine($"✓ Loaded {_availableItems.Count} items and {_availableBlocks.Count} blocks successfully");
            System.Diagnostics.Debug.WriteLine($"Sample items: {string.Join(", ", _availableItems.Take(3).Select(i => i.DisplayName))}");
            System.Diagnostics.Debug.WriteLine($"Sample blocks: {string.Join(", ", _availableBlocks.Take(3).Select(i => i.DisplayName))}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in LoadAvailableItemsAndBlocks: {ex.Message}");
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
            System.Diagnostics.Debug.WriteLine("No valid items path found!");
            return;
        }

        System.Diagnostics.Debug.WriteLine($"=== Loading items from: {itemsPath} ===");

        if (Directory.Exists(itemsPath))
        {
            var allItemFiles = Directory.GetFiles(itemsPath, "*.png");
            // Limit initial load to improve performance - can load more on demand
            var itemFiles = allItemFiles.Take(500).ToArray(); // Load first 500 items
            System.Diagnostics.Debug.WriteLine($"Found {allItemFiles.Count()} total PNG files, loading {itemFiles.Count()} for performance");

            foreach (var file in itemFiles)
            {
                string fileName = System.IO.Path.GetFileNameWithoutExtension(file);
                var ingredient = new RecipeIngredient
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
                    bitmap.DecodePixelWidth = 64; // Decode to smaller size for performance
                    bitmap.DecodePixelHeight = 64;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    ingredient.Icon = bitmap;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"✗ Error loading item icon {file}: {ex.Message}");
                }

                _availableItems.Add(ingredient);
            }
            System.Diagnostics.Debug.WriteLine($"=== Loaded {_availableItems.Count} items total ===");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"✗ Items directory not found: {itemsPath}");
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
            System.Diagnostics.Debug.WriteLine("No valid blocks path found!");
            return;
        }

        if (Directory.Exists(blocksPath))
        {
            var allBlockFiles = Directory.GetFiles(blocksPath, "*.png");
            // Limit initial load to improve performance - can load more on demand
            var blockFiles = allBlockFiles.Take(500).ToArray(); // Load first 500 blocks
            System.Diagnostics.Debug.WriteLine($"Found {allBlockFiles.Count()} total PNG files, loading {blockFiles.Count()} for performance");

            foreach (var file in blockFiles)
            {
                string fileName = System.IO.Path.GetFileNameWithoutExtension(file);
                var ingredient = new RecipeIngredient
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
                    bitmap.DecodePixelWidth = 64; // Decode to smaller size for performance
                    bitmap.DecodePixelHeight = 64;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    ingredient.Icon = bitmap;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading block icon {file}: {ex.Message}");
                }

                _availableBlocks.Add(ingredient);
            }
        }
    }

    private void DisplayIngredients()
    {
        System.Diagnostics.Debug.WriteLine($"=== DisplayIngredients called ===");
        System.Diagnostics.Debug.WriteLine($"Current tab: {_currentTab}");
        System.Diagnostics.Debug.WriteLine($"Available items count: {_availableItems.Count}");
        System.Diagnostics.Debug.WriteLine($"Available blocks count: {_availableBlocks.Count}");

        var ingredientsToDisplay = _currentTab == "Items" ? _availableItems : _availableBlocks;
        System.Diagnostics.Debug.WriteLine($"Ingredients to display: {ingredientsToDisplay.Count}");

        try
        {
            // Apply search filter if there's search text
            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                ApplySearchFilter(ingredientsToDisplay);
            }
            else
            {
                // No search filter, show all ingredients
                _filteredIngredients.Clear();
                foreach (var ingredient in ingredientsToDisplay)
                {
                    _filteredIngredients.Add(ingredient);
                }
            }

            // Set items source for virtualized display
            if (IngredientsPanel != null)
            {
                IngredientsPanel.ItemsSource = _filteredIngredients;
                System.Diagnostics.Debug.WriteLine($"Set ItemsSource with {_filteredIngredients.Count} filtered ingredients");

                // Clear previous selection
                _selectedIngredient = null;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ERROR: IngredientsPanel is null!");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"Successfully set ItemsSource with {_filteredIngredients.Count} ingredients for virtualized display");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in DisplayIngredients: {ex.Message}");
        }
    }

    private void ApplySearchFilter(IEnumerable<RecipeIngredient> sourceCollection)
    {
        _filteredIngredients.Clear();

        foreach (var ingredient in sourceCollection)
        {
            // Search in display name and code
            if (ingredient.DisplayName != null &&
                (ingredient.DisplayName.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                 ingredient.Code.Contains(_searchText, StringComparison.OrdinalIgnoreCase)))
            {
                _filteredIngredients.Add(ingredient);
            }
        }

        System.Diagnostics.Debug.WriteLine($"Search filter applied: {_searchText} -> {_filteredIngredients.Count} results");
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            _searchText = textBox.Text;
            DisplayIngredients();
        }
    }

    // Drag and Drop functionality for ingredients
    private Point _dragStartPoint;
    private bool _isDragging = false;

    private void IngredientBorder_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(null);
        _isDragging = false;
        System.Diagnostics.Debug.WriteLine("Mouse down on ingredient border");
    }

    private void IngredientBorder_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && !_isDragging)
        {
            Point currentPoint = e.GetPosition(null);
            System.Diagnostics.Debug.WriteLine($"Mouse move: Current({currentPoint.X},{currentPoint.Y}) Start({_dragStartPoint.X},{_dragStartPoint.Y})");

            if (Math.Abs(currentPoint.X - _dragStartPoint.X) > 3 ||
                Math.Abs(currentPoint.Y - _dragStartPoint.Y) > 3)
            {
                _isDragging = true;
                System.Diagnostics.Debug.WriteLine("Drag threshold reached, starting drag operation");

                if (sender is Border border && border.Tag is RecipeIngredient ingredient)
                {
                    System.Diagnostics.Debug.WriteLine($"Starting drag for ingredient: {ingredient.DisplayName}");
                    e.Handled = true; // Prevent event bubbling
                    StartDragOperation(border, ingredient);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Error: Border or ingredient is null/invalid");
                }
            }
        }
    }

    private void IngredientBorder_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
    }

    private void StartDragOperation(Border border, RecipeIngredient ingredient)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"=== STARTING DRAG OPERATION ===");
            System.Diagnostics.Debug.WriteLine($"Ingredient: {ingredient.DisplayName} ({ingredient.Code})");
            System.Diagnostics.Debug.WriteLine($"Ingredient type: {ingredient.Type}");
            System.Diagnostics.Debug.WriteLine($"Border Tag: {border.Tag}");
            System.Diagnostics.Debug.WriteLine($"Border Tag type: {border.Tag?.GetType().Name}");

            var dataObject = new DataObject("RecipeIngredient", ingredient);
            dataObject.SetData("IngredientData", ingredient);

            // Debug the data object
            System.Diagnostics.Debug.WriteLine("DataObject created with formats:");
            foreach (string format in dataObject.GetFormats())
            {
                System.Diagnostics.Debug.WriteLine($"  - {format}");
            }

            System.Diagnostics.Debug.WriteLine("Calling DragDrop.DoDragDrop...");

            var result = DragDrop.DoDragDrop(border, dataObject, DragDropEffects.Copy);
            System.Diagnostics.Debug.WriteLine($"DragDrop result: {result}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in StartDragOperation: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    // Crafting grid drag and drop handlers
    private void GridCell_DragEnter(object sender, DragEventArgs e)
    {
        if (sender is Border cell)
        {
            HighlightCell(cell, true);
        }
        e.Handled = true;
    }

    private void GridCell_DragLeave(object sender, DragEventArgs e)
    {
        if (sender is Border cell)
        {
            HighlightCell(cell, false);
        }
        e.Handled = true;
    }

    private void GridCell_Drop(object sender, DragEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("=== GRIDCELL_DROP TRIGGERED ===");
        Border? cell = null;
        if (sender is Border border)
        {
            cell = border;
            System.Diagnostics.Debug.WriteLine($"Drop target: {cell.Tag}");
            System.Diagnostics.Debug.WriteLine($"Available data formats: {string.Join(", ", e.Data.GetFormats())}");

            if (e.Data.GetDataPresent("RecipeIngredient"))
            {
                var ingredient = e.Data.GetData("RecipeIngredient") as RecipeIngredient;
                if (ingredient != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Dropping ingredient: {ingredient.DisplayName} ({ingredient.Code})");
                    PlaceIngredientInGrid(cell, ingredient);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Ingredient data is null");
                }
            }
            else if (e.Data.GetDataPresent("IngredientData"))
            {
                var ingredient = e.Data.GetData("IngredientData") as RecipeIngredient;
                if (ingredient != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Dropping ingredient (alternative format): {ingredient.DisplayName} ({ingredient.Code})");
                    PlaceIngredientInGrid(cell, ingredient);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("IngredientData is null");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("No RecipeIngredient or IngredientData present");
                System.Diagnostics.Debug.WriteLine($"Available formats: {string.Join(", ", e.Data.GetFormats())}");
            }
        }

        // Reset highlight
        if (cell != null)
        {
            HighlightCell(cell, false);
        }
        e.Handled = true;
        System.Diagnostics.Debug.WriteLine("=== GRIDCELL_DROP COMPLETED ===");
    }

    private void PlaceIngredientInGrid(Border cell, RecipeIngredient ingredient)
    {
        string[] coords = cell.Tag.ToString().Split(',');
        int row = int.Parse(coords[0]);
        int col = int.Parse(coords[1]);

        // Add ingredient to pattern
        _currentRecipe.Pattern[row][col] = ingredient.Code;

        // Add icon to cell
        var image = new Image
        {
            Source = ingredient.Icon,
            Width = 48,
            Height = 48,
            Stretch = Stretch.Uniform,
            Tag = ingredient
        };
        cell.Child = image;

        UpdateRecipeIngredients();
        UpdateRecipePreview();
    }

    private void HighlightIngredient(Border border, bool highlight)
    {
        if (highlight)
        {
            border.BorderBrush = Brushes.LightBlue;
            border.BorderThickness = new Thickness(2);
            border.Background = new SolidColorBrush(Color.FromArgb(50, 135, 206, 250));
        }
        else
        {
            border.BorderBrush = Brushes.Gray;
            border.BorderThickness = new Thickness(1);
            border.Background = Brushes.DarkSlateGray;
        }
    }

    private void IngredientIcon_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Image image && image.Tag is RecipeIngredient ingredient)
        {
            _selectedIngredient = ingredient;

            // Update output preview if this is the first selected ingredient
            if (_selectedOutput == null)
            {
                _selectedOutput = ingredient;
                UpdateOutputPreview();
            }
        }
    }

    private void UpdateOutputPreview()
    {
        if (_selectedOutput != null)
        {
            OutputPreviewText.Text = _selectedOutput.DisplayName;
        }
        else
        {
            OutputPreviewText.Text = "No output selected";
        }
    }

    private void GenerateCraftingGrid()
    {
        if (CraftingGrid == null) return;

        CraftingGrid.Children.Clear();
        CraftingGrid.RowDefinitions.Clear();
        CraftingGrid.ColumnDefinitions.Clear();

        // Set up grid rows and columns
        for (int i = 0; i < _currentGridSize; i++)
        {
            CraftingGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(70) });
            CraftingGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
        }

        // Initialize pattern
        _currentRecipe.Pattern.Clear();
        for (int i = 0; i < _currentGridSize; i++)
        {
            _currentRecipe.Pattern.Add(new List<string>(new string[_currentGridSize]));
        }

        for (int row = 0; row < _currentGridSize; row++)
        {
            for (int col = 0; col < _currentGridSize; col++)
            {
                var cell = new Border
                {
                    Style = (Style)FindResource("GridCellStyle"),
                    Tag = $"{row},{col}",
                    Cursor = Cursors.Hand,
                    AllowDrop = true
                };

                cell.MouseLeftButtonDown += GridCell_Click;
                cell.MouseRightButtonDown += GridCell_RightClick;
                cell.MouseEnter += (s, e) => HighlightCell(cell, true);
                cell.MouseLeave += (s, e) => HighlightCell(cell, false);
                cell.DragEnter += GridCell_DragEnter;
                cell.DragLeave += GridCell_DragLeave;
                cell.Drop += GridCell_Drop;

                Grid.SetRow(cell, row);
                Grid.SetColumn(cell, col);

                CraftingGrid.Children.Add(cell);
            }
        }
    }

    private void HighlightCell(Border cell, bool highlight)
    {
        if (highlight)
        {
            cell.BorderBrush = Brushes.LightGreen;
            cell.BorderThickness = new Thickness(2);
        }
        else
        {
            cell.BorderBrush = Brushes.Gray;
            cell.BorderThickness = new Thickness(1);
        }
    }

    private void GridCell_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border cell && _selectedIngredient != null)
        {
            string[] coords = cell.Tag.ToString().Split(',');
            int row = int.Parse(coords[0]);
            int col = int.Parse(coords[1]);

            // Add/remove ingredient from pattern
            if (_currentRecipe.Pattern[row][col] == null)
            {
                // Add ingredient
                _currentRecipe.Pattern[row][col] = _selectedIngredient.Code;

                // Add icon to cell
                var image = new Image
                {
                    Source = _selectedIngredient.Icon,
                    Width = 48,
                    Height = 48,
                    Stretch = Stretch.Uniform,
                    Tag = _selectedIngredient
                };
                cell.Child = image;
            }
            else
            {
                // Remove ingredient
                _currentRecipe.Pattern[row][col] = null;
                cell.Child = null;
            }

            UpdateRecipeIngredients();
            UpdateRecipePreview();
        }
    }

    private void GridCell_RightClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border cell)
        {
            string[] coords = cell.Tag.ToString().Split(',');
            int row = int.Parse(coords[0]);
            int col = int.Parse(coords[1]);

            // Empty the slot regardless of current state
            _currentRecipe.Pattern[row][col] = null;

            // Remove any visual content from the cell
            cell.Child = null;

            UpdateRecipeIngredients();
            UpdateRecipePreview();

            System.Diagnostics.Debug.WriteLine($"Right-click: Emptied grid cell at position ({row},{col})");
        }

        // Mark event as handled to prevent context menu
        e.Handled = true;
    }

    private void UpdateRecipeIngredients()
    {
        _currentRecipe.Ingredients.Clear();

        foreach (var row in _currentRecipe.Pattern)
        {
            foreach (var cell in row)
            {
                if (!string.IsNullOrEmpty(cell) && !_currentRecipe.Ingredients.ContainsKey(cell))
                {
                    var ingredient = _availableItems.Concat(_availableBlocks)
                        .FirstOrDefault(i => i.Code == cell);
                    if (ingredient != null)
                    {
                        _currentRecipe.Ingredients[cell] = ingredient;
                    }
                }
            }
        }
    }

    private void UpdateRecipePreview()
    {
        UpdateOutputPreview();
    }

    private void GridRecipeButton_Click(object sender, RoutedEventArgs e)
    {
        // Grid recipe type is already selected
        MessageBox.Show("Grid recipe type selected. Use the crafting grid to arrange ingredients.",
                       "Recipe Type", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ItemsTabButton_Click(object sender, RoutedEventArgs e)
    {
        _currentTab = "Items";
        DisplayIngredients();
    }

    private void BlocksTabButton_Click(object sender, RoutedEventArgs e)
    {
        _currentTab = "Blocks";
        DisplayIngredients();
    }

    private void IngredientBorder_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is RecipeIngredient ingredient)
        {
            _selectedIngredient = ingredient;

            // Update output preview if this is the first selected ingredient
            if (_selectedOutput == null)
            {
                _selectedOutput = ingredient;
                UpdateOutputPreview();
            }
        }
    }

    private void GridSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GridSizeComboBox.SelectedItem is ComboBoxItem item)
        {
            _currentGridSize = int.Parse(item.Content.ToString().Split('x')[0]);
            GenerateCraftingGrid();
        }
    }

    private void CreateRecipeButton_Click(object sender, RoutedEventArgs e)
    {
        if (RecipeNameTextBox == null || string.IsNullOrWhiteSpace(RecipeNameTextBox.Text) || _selectedOutput == null)
        {
            MessageBox.Show("Please provide a recipe name and select an output item.",
                           "Missing Information", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            // Set recipe properties
            _currentRecipe.RecipeName = RecipeNameTextBox.Text;
            _currentRecipe.OutputType = _selectedOutput.Type;
            _currentRecipe.OutputCode = _selectedOutput.Code;
            _currentRecipe.OutputQuantity = int.Parse(OutputQuantityTextBox.Text);

            // Generate recipe JSON
            string recipeJson = GenerateRecipeJson();

            // Check if we're editing an existing recipe
            bool isEditing = _existingRecipeData != null;

            if (isEditing)
            {
                // For editing, we need to check if the recipe name changed
                string oldRecipeName = _existingRecipeName;
                string newRecipeName = _currentRecipe.RecipeName;

                if (oldRecipeName != newRecipeName)
                {
                    // Recipe name changed, remove old file
                    string oldRecipeFilePath = Path.Combine(GetModDirectory(), "assets", _modId, "recipes", "grid", $"{oldRecipeName}.json");
                    if (File.Exists(oldRecipeFilePath))
                    {
                        File.Delete(oldRecipeFilePath);
                    }
                }
            }

            SaveRecipeFile(recipeJson);

            string action = isEditing ? "updated" : "created";

            MessageBox.Show($"Recipe '{_currentRecipe.RecipeName}' has been {action} successfully!\n\n" +
                           $"Saved to: mods/{_modId}/assets/{_modId}/recipes/grid/{_currentRecipe.RecipeName}.json\n" +
                           $"Mod: {_modName}\n" +
                           $"Ingredients: {_currentRecipe.Ingredients.Count}\n" +
                           $"Grid Size: {_currentGridSize}x{_currentGridSize}\n" +
                           $"Output: {_selectedOutput.DisplayName}",
                           $"Recipe {action}", MessageBoxButton.OK, MessageBoxImage.Information);

            // Call the refresh callback to update the recipes list in the parent window
            _refreshCallback?.Invoke();

            // Close the window
            this.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error creating recipe: {ex.Message}",
                           "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private string GenerateRecipeJson()
    {
        // Create letter mapping for ingredients (X, A, B, C, etc.)
        var ingredientLetterMap = new Dictionary<string, char>();
        var ingredientsWithLetters = new Dictionary<string, object>();
        char currentLetter = 'X';

        foreach (var ingredient in _currentRecipe.Ingredients)
        {
            ingredientLetterMap[ingredient.Key] = currentLetter;
            ingredientsWithLetters[currentLetter.ToString()] = new
            {
                type = ingredient.Value.Type,
                code = ingredient.Value.Code,
                quantity = ingredient.Value.Quantity
            };
            currentLetter++;
        }

        // Generate ingredientPattern as comma-separated rows
        var patternRows = new List<string>();
        for (int row = 0; row < _currentGridSize; row++)
        {
            var patternRow = new List<string>();
            for (int col = 0; col < _currentGridSize; col++)
            {
                string cellValue = _currentRecipe.Pattern[row][col];
                if (string.IsNullOrEmpty(cellValue))
                {
                    patternRow.Add("_");
                }
                else if (ingredientLetterMap.TryGetValue(cellValue, out char letter))
                {
                    patternRow.Add(letter.ToString());
                }
                else
                {
                    patternRow.Add("_");
                }
            }
            patternRows.Add(string.Join("", patternRow));
        }

        var recipeData = new
        {
            enabled = true,
            ingredientPattern = string.Join(",", patternRows),
            width = _currentGridSize,
            height = _currentGridSize,
            ingredients = ingredientsWithLetters,
            output = new
            {
                type = _currentRecipe.OutputType,
                code = _currentRecipe.OutputCode,
                quantity = _currentRecipe.OutputQuantity
            }
        };

        return JsonSerializer.Serialize(recipeData, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private string GetModDirectory()
    {
        // Use the mod ID passed to the constructor (same approach as ItemCreationWindow)
        string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string modsDirectory = Path.Combine(exeDirectory, "mods");
        return Path.Combine(modsDirectory, _modId);
    }

    private void SaveRecipeFile(string recipeJson)
    {
        try
        {
            // Use the mod ID passed to the constructor (same approach as ItemCreationWindow)
            string modDirectory = GetModDirectory();

            // Create the path structure: mods/<modid>/assets/<modid>/recipes/grid/
            string assetsPath = Path.Combine(modDirectory, "assets", _modId);
            string recipesDir = Path.Combine(assetsPath, "recipes");
            string gridDir = Path.Combine(recipesDir, "grid");

            // Create directories if they don't exist
            Directory.CreateDirectory(gridDir);

            string filePath = Path.Combine(gridDir, $"{_currentRecipe.RecipeName}.json");
            File.WriteAllText(filePath, recipeJson);

            System.Diagnostics.Debug.WriteLine($"Recipe saved to: {filePath}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving recipe file: {ex.Message}");
            MessageBox.Show($"Error saving recipe: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private string GetModId()
    {
        try
        {
            // PRIORITY 1: Try to read from the current workspace modinfo.json first
            string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string workspaceModInfoPath = Path.Combine(exeDirectory, "..", "..", "..", "..", "modinfo.json");

            System.Diagnostics.Debug.WriteLine($"Looking for workspace modinfo.json at: {workspaceModInfoPath}");

            if (File.Exists(workspaceModInfoPath))
            {
                string jsonContent = File.ReadAllText(workspaceModInfoPath);
                System.Diagnostics.Debug.WriteLine($"Reading workspace modinfo.json: {jsonContent}");

                // Try both key formats for workspace modinfo.json
                string[] possibleKeys = { "\"modid\"", "\"ModId\"" };
                foreach (string key in possibleKeys)
                {
                    int keyIndex = jsonContent.IndexOf(key);
                    if (keyIndex >= 0)
                    {
                        int colonIndex = jsonContent.IndexOf(":", keyIndex);
                        if (colonIndex > keyIndex)
                        {
                            int valueStart = jsonContent.IndexOf("\"", colonIndex);
                            if (valueStart > colonIndex)
                            {
                                int valueEnd = jsonContent.IndexOf("\"", valueStart + 1);
                                if (valueEnd > valueStart)
                                {
                                    string modId = jsonContent.Substring(valueStart + 1, valueEnd - valueStart - 1);
                                    if (!string.IsNullOrEmpty(modId))
                                    {
                                        System.Diagnostics.Debug.WriteLine($"Found workspace mod ID '{modId}' from {workspaceModInfoPath}");
                                        return modId;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // PRIORITY 2: Fallback to application directory modinfo.json
            string appModInfoPath = Path.Combine(exeDirectory, "modinfo.json");
            System.Diagnostics.Debug.WriteLine($"Looking for app modinfo.json at: {appModInfoPath}");

            if (File.Exists(appModInfoPath))
            {
                string jsonContent = File.ReadAllText(appModInfoPath);
                System.Diagnostics.Debug.WriteLine($"Reading app modinfo.json: {jsonContent}");

                // Try both key formats
                string[] possibleKeys = { "\"modid\"", "\"ModId\"" };
                foreach (string key in possibleKeys)
                {
                    int keyIndex = jsonContent.IndexOf(key);
                    if (keyIndex >= 0)
                    {
                        int colonIndex = jsonContent.IndexOf(":", keyIndex);
                        if (colonIndex > keyIndex)
                        {
                            int valueStart = jsonContent.IndexOf("\"", colonIndex);
                            if (valueStart > colonIndex)
                            {
                                int valueEnd = jsonContent.IndexOf("\"", valueStart + 1);
                                if (valueEnd > valueStart)
                                {
                                    string modId = jsonContent.Substring(valueStart + 1, valueEnd - valueStart - 1);
                                    if (!string.IsNullOrEmpty(modId))
                                    {
                                        System.Diagnostics.Debug.WriteLine($"Found app mod ID '{modId}' from {appModInfoPath}");
                                        return modId;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // PRIORITY 3: Last resort - search mods directory for a matching mod
            string modsDirectory = Path.Combine(exeDirectory, "mods");
            System.Diagnostics.Debug.WriteLine($"Looking for mods directory at: {modsDirectory}");

            if (Directory.Exists(modsDirectory))
            {
                string[] modDirectories = Directory.GetDirectories(modsDirectory);
                System.Diagnostics.Debug.WriteLine($"Found {modDirectories.Length} mod directories");

                foreach (string modDir in modDirectories)
                {
                    string modInfoPath = Path.Combine(modDir, "modinfo.json");
                    System.Diagnostics.Debug.WriteLine($"Checking mod directory: {modDir}");

                    if (File.Exists(modInfoPath))
                    {
                        string jsonContent = File.ReadAllText(modInfoPath);

                        // Try different possible key formats for modid
                        string[] possibleKeys = { "\"modid\"", "\"ModId\"" };

                        foreach (string key in possibleKeys)
                        {
                            int keyIndex = jsonContent.IndexOf(key);
                            if (keyIndex >= 0)
                            {
                                // Find the colon after the key
                                int colonIndex = jsonContent.IndexOf(":", keyIndex);
                                if (colonIndex > keyIndex)
                                {
                                    // Find the opening quote of the value
                                    int valueStart = jsonContent.IndexOf("\"", colonIndex);
                                    if (valueStart > colonIndex)
                                    {
                                        // Find the closing quote of the value
                                        int valueEnd = jsonContent.IndexOf("\"", valueStart + 1);
                                        if (valueEnd > valueStart)
                                        {
                                            string modId = jsonContent.Substring(valueStart + 1, valueEnd - valueStart - 1);
                                            if (!string.IsNullOrEmpty(modId))
                                            {
                                                System.Diagnostics.Debug.WriteLine($"Found mod ID '{modId}' from {modInfoPath}");
                                                return modId;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error reading mod ID: {ex.Message}");
        }
        return "mymod"; // Default fallback
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        ResetRecipe();
    }

    private void ResetRecipe()
    {
        _currentRecipe = new GridRecipe();
        RecipeNameTextBox.Text = "new_recipe";
        OutputQuantityTextBox.Text = "1";
        _selectedIngredient = null;
        _selectedOutput = null;
        GenerateCraftingGrid();
        UpdateRecipePreview();
    }

    // Output slot drag and drop handlers
    private void OutputSlot_DragEnter(object sender, DragEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("=== OUTPUTSLOT_DRAGENTER TRIGGERED ===");
        System.Diagnostics.Debug.WriteLine($"Sender type: {sender.GetType().Name}");
        System.Diagnostics.Debug.WriteLine($"Available data formats: {string.Join(", ", e.Data.GetFormats())}");

        // Check for valid data formats
        bool hasRecipeIngredient = e.Data.GetDataPresent("RecipeIngredient");
        bool hasIngredientData = e.Data.GetDataPresent("IngredientData");

        System.Diagnostics.Debug.WriteLine($"Has RecipeIngredient: {hasRecipeIngredient}");
        System.Diagnostics.Debug.WriteLine($"Has IngredientData: {hasIngredientData}");

        if (hasRecipeIngredient || hasIngredientData)
        {
            if (sender is Border outputBorder)
            {
                System.Diagnostics.Debug.WriteLine("Setting highlight for output slot");
                HighlightOutputSlot(outputBorder, true);
            }
            e.Effects = DragDropEffects.Copy;
            System.Diagnostics.Debug.WriteLine("DragEnter: Setting effects to Copy");
        }
        else
        {
            e.Effects = DragDropEffects.None;
            System.Diagnostics.Debug.WriteLine("DragEnter: Setting effects to None - no valid data");
        }
        e.Handled = true;
    }

    private void OutputSlot_DragLeave(object sender, DragEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("=== OUTPUTSLOT_DRAGLEAVE TRIGGERED ===");
        if (sender is Border outputBorder)
        {
            System.Diagnostics.Debug.WriteLine("Removing highlight from output slot");
            HighlightOutputSlot(outputBorder, false);
        }
        e.Handled = true;
    }

    private void OutputSlot_Drop(object sender, DragEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("=== OUTPUTSLOT_DROP TRIGGERED ===");
        Border? outputBorder = null;

        if (sender is Border border)
        {
            outputBorder = border;
            System.Diagnostics.Debug.WriteLine($"Drop target: OutputPreviewBorder");
            System.Diagnostics.Debug.WriteLine($"Available data formats: {string.Join(", ", e.Data.GetFormats())}");

            RecipeIngredient? ingredient = null;

            // Try to get ingredient data
            if (e.Data.GetDataPresent("RecipeIngredient"))
            {
                ingredient = e.Data.GetData("RecipeIngredient") as RecipeIngredient;
                System.Diagnostics.Debug.WriteLine("Found RecipeIngredient data format");
            }
            else if (e.Data.GetDataPresent("IngredientData"))
            {
                ingredient = e.Data.GetData("IngredientData") as RecipeIngredient;
                System.Diagnostics.Debug.WriteLine("Found IngredientData data format");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("No RecipeIngredient or IngredientData present");
                System.Diagnostics.Debug.WriteLine($"Available formats: {string.Join(", ", e.Data.GetFormats())}");
            }

            if (ingredient != null)
            {
                System.Diagnostics.Debug.WriteLine($"Setting output ingredient: {ingredient.DisplayName} ({ingredient.Code})");
                System.Diagnostics.Debug.WriteLine($"Ingredient type: {ingredient.Type}");
                SetRecipeOutput(ingredient);

                // Show success message to user
                MessageBox.Show($"Output set to: {ingredient.DisplayName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ERROR: No valid ingredient data found for output slot");
                MessageBox.Show("No valid ingredient data found. Make sure you're dragging an item from the ingredients list.", "Drop Failed", MessageBoxButton.OK, MessageBoxImage.Warning);

                // Try to debug what data is actually available
                foreach (string format in e.Data.GetFormats())
                {
                    try
                    {
                        object data = e.Data.GetData(format);
                        System.Diagnostics.Debug.WriteLine($"Format '{format}' contains: {data?.GetType().Name} - {data}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error reading format '{format}': {ex.Message}");
                    }
                }
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"ERROR: Sender is not a Border, it's {sender?.GetType().Name}");
            MessageBox.Show($"Unexpected drop target type: {sender?.GetType().Name}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        // Reset highlight
        if (outputBorder != null)
        {
            HighlightOutputSlot(outputBorder, false);
        }
        e.Handled = true;
        System.Diagnostics.Debug.WriteLine("=== OUTPUTSLOT_DROP COMPLETED ===");
    }

    private void SetRecipeOutput(RecipeIngredient ingredient)
    {
        _selectedOutput = ingredient;
        UpdateOutputPreview();
        UpdateOutputSlot();
        System.Diagnostics.Debug.WriteLine($"Recipe output set to: {ingredient.DisplayName}");
    }

    private void HighlightOutputSlot(Border border, bool highlight)
    {
        if (highlight)
        {
            border.BorderBrush = Brushes.LightGreen;
            border.BorderThickness = new Thickness(2);
            border.Background = new SolidColorBrush(Color.FromArgb(50, 144, 238, 144));
        }
        else
        {
            border.BorderBrush = Brushes.Gray;
            border.BorderThickness = new Thickness(1);
            border.Background = Brushes.DarkSlateGray;
        }
    }

    private void UpdateOutputSlot()
    {
        if (_selectedOutput != null)
        {
            // Update the output slot display
            if (OutputSlotImage != null)
            {
                OutputSlotImage.Source = _selectedOutput.Icon;
            }
            if (OutputSlotText != null)
            {
                OutputSlotText.Text = _selectedOutput.DisplayName;
                OutputSlotText.Foreground = Brushes.White;
            }
        }
        else
        {
            // Clear the output slot display
            if (OutputSlotImage != null)
            {
                OutputSlotImage.Source = null;
            }
            if (OutputSlotText != null)
            {
                OutputSlotText.Text = "Drop item here";
                OutputSlotText.Foreground = Brushes.Gray;
            }
        }
    }

    private void UpdateOutputSlotForEditing()
    {
        if (_existingRecipeData != null && _selectedOutput != null)
        {
            // For editing mode, ensure the output slot shows the existing recipe's output
            try
            {
                // Find the output ingredient in available items/blocks for proper icon
                var availableIngredient = _availableItems.Concat(_availableBlocks)
                    .FirstOrDefault(i => i.Code == _existingRecipeData.recipe.output.code);

                if (availableIngredient != null && _selectedOutput != null)
                {
                    // Update the selected output with the proper icon if available
                    _selectedOutput.Icon = availableIngredient.Icon;
                    _selectedOutput.IconPath = availableIngredient.IconPath;
                }

                // Update the output slot display
                if (OutputSlotImage != null)
                {
                    OutputSlotImage.Source = _selectedOutput.Icon;
                }
                if (OutputSlotText != null)
                {
                    OutputSlotText.Text = _selectedOutput.DisplayName;
                    OutputSlotText.Foreground = Brushes.White;
                }

                System.Diagnostics.Debug.WriteLine($"Updated output slot for editing: {_selectedOutput.DisplayName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating output slot for editing: {ex.Message}");
            }
        }
        else
        {
            // Fallback to regular update if not in editing mode
            UpdateOutputSlot();
        }
    }
}