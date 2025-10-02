using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace VScreator;

/// <summary>
/// Interaction logic for ItemCreationWindow.xaml
/// </summary>
public partial class ItemCreationWindow : Window
{
    private readonly string _modId;
    private readonly string _modName;
    private ItemData? _existingItemData = null;
    private string _existingItemName = "";
    private Action? _refreshCallback = null;

    public ItemCreationWindow(string modId, string modName)
    {
        InitializeComponent();
        _modId = modId;
        _modName = modName;

        // Load available item textures and shapes
        LoadItemTextures();
        LoadItemShapes();
    }

    // Constructor for creating new items with refresh callback
    public ItemCreationWindow(string modId, string modName, Action refreshCallback)
    {
        InitializeComponent();
        _modId = modId;
        _modName = modName;
        _refreshCallback = refreshCallback;

        // Load available item textures and shapes
        LoadItemTextures();
        LoadItemShapes();
    }

    // Constructor for editing existing items
    public ItemCreationWindow(string modId, string modName, ItemData existingItemData, string existingItemName)
    {
        InitializeComponent();
        _modId = modId;
        _modName = modName;
        _existingItemData = existingItemData;
        _existingItemName = existingItemName;

        // Load available item textures and shapes
        LoadItemTextures();
        LoadItemShapes();

        // Pre-fill the form with existing item data
        PreFillFormWithExistingData();
    }

    // Constructor for editing existing items with refresh callback
    public ItemCreationWindow(string modId, string modName, ItemData existingItemData, string existingItemName, Action refreshCallback)
    {
        InitializeComponent();
        _modId = modId;
        _modName = modName;
        _existingItemData = existingItemData;
        _existingItemName = existingItemName;
        _refreshCallback = refreshCallback;

        // Load available item textures and shapes
        LoadItemTextures();
        LoadItemShapes();

        // Pre-fill the form with existing item data
        PreFillFormWithExistingData();
    }

    private void LoadItemTextures()
    {
        try
        {
            string modDirectory = GetModDirectory();
            string itemTexturesPath = Path.Combine(modDirectory, "assets", _modId, "textures", "item");

            if (Directory.Exists(itemTexturesPath))
            {
                string[] textureFiles = Directory.GetFiles(itemTexturesPath, "*.png");

                foreach (string textureFile in textureFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(textureFile);
                    TextureComboBox.Items.Add(fileName);
                }

                if (TextureComboBox.Items.Count > 0)
                {
                    TextureComboBox.SelectedIndex = 0;
                }
            }
            else
            {
                // No item textures found
                TextureComboBox.Items.Add("No item textures found");
                TextureComboBox.IsEnabled = false;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading textures: {ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadItemShapes()
    {
        try
        {
            string modDirectory = GetModDirectory();
            string itemShapesPath = Path.Combine(modDirectory, "assets", _modId, "shapes", "item");

            if (Directory.Exists(itemShapesPath))
            {
                string[] shapeFiles = Directory.GetFiles(itemShapesPath, "*.json");

                foreach (string shapeFile in shapeFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(shapeFile);
                    ShapeComboBox.Items.Add(fileName);
                }

                if (ShapeComboBox.Items.Count > 0)
                {
                    ShapeComboBox.SelectedIndex = 0;
                }
            }
            else
            {
                // No item shapes found
                ShapeComboBox.Items.Add("No item shapes found");
                ShapeComboBox.IsEnabled = false;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading shapes: {ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private string GetModDirectory()
    {
        string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string modsDirectory = Path.Combine(exeDirectory, "mods");
        return Path.Combine(modsDirectory, _modId);
    }

    private void TextureComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // This method is required for the XAML but we don't need to do anything special here
    }

    private void ShapeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // This method is required for the XAML but we don't need to do anything special here
    }

    private void IsFoodCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        FoodPropsGroupBox.Visibility = Visibility.Visible;
    }

    private void IsFoodCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        FoodPropsGroupBox.Visibility = Visibility.Collapsed;
    }

    private void CreateItemButton_Click(object sender, RoutedEventArgs e)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(ItemNameTextBox.Text) ||
            string.IsNullOrWhiteSpace(ItemIdTextBox.Text) ||
            TextureComboBox.SelectedItem == null ||
            ShapeComboBox.SelectedItem == null)
        {
            MessageBox.Show("Please fill in all fields (Name, ID, and select a texture and shape).",
                           "Missing Information", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Validate food properties if item is marked as food
        if (IsFoodCheckBox.IsChecked == true)
        {
            if (string.IsNullOrWhiteSpace(SatietyTextBox.Text) ||
                string.IsNullOrWhiteSpace(HealthTextBox.Text) ||
                FoodCategoryComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please fill in all food properties (Satiety, Health, and Food Category).",
                               "Missing Food Information", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validate numeric fields
            if (!double.TryParse(SatietyTextBox.Text, out double satiety) ||
                !double.TryParse(HealthTextBox.Text, out double health))
            {
                MessageBox.Show("Please enter valid numbers for Satiety and Health.",
                               "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        try
        {
            string modDirectory = GetModDirectory();
            string assetsPath = Path.Combine(modDirectory, "assets", _modId);
            string itemTypesPath = Path.Combine(assetsPath, "itemtypes");

            // Create itemtypes directory if it doesn't exist
            Directory.CreateDirectory(itemTypesPath);

            // Create item JSON
            var itemData = new ItemData
            {
                Code = ItemIdTextBox.Text,
                CreativeInventory = new CreativeInventory
                {
                    General = new[] { "*" }
                },
                Texture = new ItemTexture
                {
                    Base = $"item/{TextureComboBox.SelectedItem.ToString()}"
                },
                Shape = new ItemShape
                {
                    Base = $"item/{ShapeComboBox.SelectedItem.ToString()}"
                }
            };

            // Add nutrition properties if item is marked as food
            if (IsFoodCheckBox.IsChecked == true)
            {
                // Get the selected ComboBoxItem and extract its Content
                var selectedComboBoxItem = FoodCategoryComboBox.SelectedItem as ComboBoxItem;
                string foodCategory = selectedComboBoxItem?.Content?.ToString() ?? "Vegetable";

                itemData.NutritionProps = new NutritionProps
                {
                    FoodCategory = foodCategory,
                    Satiety = double.Parse(SatietyTextBox.Text),
                    Health = double.Parse(HealthTextBox.Text),
                    Nutrition = new Nutrition
                    {
                        Fruit = double.Parse(FruitNutritionTextBox.Text),
                        Vegetable = double.Parse(VegetableNutritionTextBox.Text),
                        Protein = double.Parse(ProteinNutritionTextBox.Text),
                        Grain = 0.0, // Default values for unused nutrition types
                        Dairy = 0.0
                    }
                };
            }

            string jsonContent = JsonSerializer.Serialize(itemData, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            string itemFilePath = Path.Combine(itemTypesPath, $"{ItemIdTextBox.Text}.json");

            // Check if we're editing an existing item
            bool isEditing = _existingItemData != null;

            if (isEditing)
            {
                // For editing, we need to check if the item ID changed
                string oldItemCode = _existingItemData.Code;
                string newItemCode = ItemIdTextBox.Text;

                if (oldItemCode != newItemCode)
                {
                    // Item ID changed, remove old file
                    string oldItemFilePath = Path.Combine(itemTypesPath, $"{oldItemCode}.json");
                    if (File.Exists(oldItemFilePath))
                    {
                        File.Delete(oldItemFilePath);
                    }

                    // Update en.json to remove old entry and add new one
                    UpdateEnJsonFileForEdit(oldItemCode, newItemCode, ItemNameTextBox.Text);
                }
                else
                {
                    // Item ID unchanged, just update en.json
                    UpdateEnJsonFile(newItemCode, ItemNameTextBox.Text);
                }
            }
            else
            {
                // Creating new item
                UpdateEnJsonFile(ItemIdTextBox.Text, ItemNameTextBox.Text);
            }

            File.WriteAllText(itemFilePath, jsonContent);

            string action = isEditing ? "updated" : "created";
            string foodInfo = "";
            if (IsFoodCheckBox.IsChecked == true)
            {
                // Get the selected ComboBoxItem and extract its Content for display
                var selectedComboBoxItem = FoodCategoryComboBox.SelectedItem as ComboBoxItem;
                string foodCategoryDisplay = selectedComboBoxItem?.Content?.ToString() ?? "Unknown";

                foodInfo = $"\nFood Category: {foodCategoryDisplay}\n" +
                          $"Satiety: {SatietyTextBox.Text}\n" +
                          $"Health: {HealthTextBox.Text}";
            }

            MessageBox.Show($"Item '{ItemNameTextBox.Text}' has been {action} successfully!\n\n" +
                           $"ID: {ItemIdTextBox.Text}\n" +
                           $"Texture: {TextureComboBox.SelectedItem}\n" +
                           $"Shape: {ShapeComboBox.SelectedItem}" +
                           foodInfo + $"\n" +
                           $"Location: {itemFilePath}",
                           $"Item {action}", MessageBoxButton.OK, MessageBoxImage.Information);

            // Call the refresh callback to update the items list in the parent window
            _refreshCallback?.Invoke();

            this.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"An error occurred while creating the item:\n\n{ex.Message}",
                           "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void PreFillFormWithExistingData()
    {
        if (_existingItemData != null)
        {
            // Pre-fill the form fields with existing data
            ItemIdTextBox.Text = _existingItemData.Code;
            ItemNameTextBox.Text = _existingItemName;

            // Set the texture selection
            string textureBase = _existingItemData.Texture.Base;
            if (textureBase.StartsWith("item/"))
            {
                string textureName = textureBase.Substring(5); // Remove "item/" prefix
                TextureComboBox.SelectedItem = textureName;
            }

            // Set the shape selection
            string shapeBase = _existingItemData.Shape.Base;
            if (shapeBase.StartsWith("item/"))
            {
                string shapeName = shapeBase.Substring(5); // Remove "item/" prefix
                ShapeComboBox.SelectedItem = shapeName;
            }

            // Set food properties if they exist
            if (_existingItemData.NutritionProps != null)
            {
                IsFoodCheckBox.IsChecked = true;

                // Find and select the correct ComboBoxItem based on the food category
                foreach (ComboBoxItem item in FoodCategoryComboBox.Items)
                {
                    if (item.Content.ToString() == _existingItemData.NutritionProps.FoodCategory)
                    {
                        FoodCategoryComboBox.SelectedItem = item;
                        break;
                    }
                }

                SatietyTextBox.Text = _existingItemData.NutritionProps.Satiety.ToString();
                HealthTextBox.Text = _existingItemData.NutritionProps.Health.ToString();

                if (_existingItemData.NutritionProps.Nutrition != null)
                {
                    FruitNutritionTextBox.Text = _existingItemData.NutritionProps.Nutrition.Fruit.ToString();
                    VegetableNutritionTextBox.Text = _existingItemData.NutritionProps.Nutrition.Vegetable.ToString();
                    ProteinNutritionTextBox.Text = _existingItemData.NutritionProps.Nutrition.Protein.ToString();
                }
            }

            // Update the window title to indicate editing mode
            this.Title = $"Edit Item - {_modName}";

            // Update the button text to indicate editing mode
            CreateItemButton.Content = "Update Item";
        }
    }

    private void UpdateEnJsonFile(string itemCode, string itemName)
    {
        try
        {
            string modDirectory = GetModDirectory();
            string langDirectory = Path.Combine(modDirectory, "assets", _modId, "lang");

            // Create lang directory if it doesn't exist
            Directory.CreateDirectory(langDirectory);

            string enJsonPath = Path.Combine(langDirectory, "en.json");

            // Read existing en.json or create new dictionary
            Dictionary<string, string> langEntries = new Dictionary<string, string>();

            if (File.Exists(enJsonPath))
            {
                try
                {
                    string existingContent = File.ReadAllText(enJsonPath);
                    if (!string.IsNullOrWhiteSpace(existingContent))
                    {
                        // Parse existing JSON
                        try
                        {
                            langEntries = JsonSerializer.Deserialize<Dictionary<string, string>>(existingContent)
                                       ?? new Dictionary<string, string>();
                        }
                        catch
                        {
                            // If parsing fails, start with empty dictionary
                            langEntries = new Dictionary<string, string>();
                        }
                    }
                }
                catch
                {
                    // If reading fails, start with empty dictionary
                    langEntries = new Dictionary<string, string>();
                }
            }

            // Add or update the item entry
            string itemKey = $"item-{itemCode}";
            langEntries[itemKey] = itemName;

            // Write back to en.json with proper formatting
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string updatedContent = JsonSerializer.Serialize(langEntries, options);
            File.WriteAllText(enJsonPath, updatedContent);

            System.Diagnostics.Debug.WriteLine($"Updated en.json at {enJsonPath} with entry: {itemKey}: {itemName}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating en.json: {ex.Message}");
            // Don't show error to user as the item was created successfully
            // The language file update is a secondary feature
        }
    }

    private void UpdateEnJsonFileForEdit(string oldItemCode, string newItemCode, string newItemName)
    {
        try
        {
            string modDirectory = GetModDirectory();
            string langDirectory = Path.Combine(modDirectory, "assets", _modId, "lang");

            // Create lang directory if it doesn't exist
            Directory.CreateDirectory(langDirectory);

            string enJsonPath = Path.Combine(langDirectory, "en.json");

            // Read existing en.json or create new dictionary
            Dictionary<string, string> langEntries = new Dictionary<string, string>();

            if (File.Exists(enJsonPath))
            {
                try
                {
                    string existingContent = File.ReadAllText(enJsonPath);
                    if (!string.IsNullOrWhiteSpace(existingContent))
                    {
                        // Parse existing JSON
                        try
                        {
                            langEntries = JsonSerializer.Deserialize<Dictionary<string, string>>(existingContent)
                                        ?? new Dictionary<string, string>();
                        }
                        catch
                        {
                            // If parsing fails, start with empty dictionary
                            langEntries = new Dictionary<string, string>();
                        }
                    }
                }
                catch
                {
                    // If reading fails, start with empty dictionary
                    langEntries = new Dictionary<string, string>();
                }
            }

            // Remove old entry
            string oldItemKey = $"item-{oldItemCode}";
            langEntries.Remove(oldItemKey);

            // Add new entry
            string newItemKey = $"item-{newItemCode}";
            langEntries[newItemKey] = newItemName;

            // Write back to en.json with proper formatting
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string updatedContent = JsonSerializer.Serialize(langEntries, options);
            File.WriteAllText(enJsonPath, updatedContent);

            System.Diagnostics.Debug.WriteLine($"Updated en.json at {enJsonPath} - removed: {oldItemKey}, added: {newItemKey}: {newItemName}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating en.json for edit: {ex.Message}");
            // Don't show error to user as the item was updated successfully
            // The language file update is a secondary feature
        }
    }
}

public class ItemData
{
    public string Code { get; set; } = "";
    public CreativeInventory CreativeInventory { get; set; } = new();
    public ItemTexture Texture { get; set; } = new();
    public ItemShape Shape { get; set; } = new();
    public NutritionProps? NutritionProps { get; set; } = null;
}

public class CreativeInventory
{
    public string[] General { get; set; } = Array.Empty<string>();
}

public class ItemTexture
{
    public string Base { get; set; } = "";
}

public class ItemShape
{
    public string Base { get; set; } = "";
}

public class NutritionProps
{
    public string FoodCategory { get; set; } = "";
    public double Satiety { get; set; }
    public double Health { get; set; }
    public Nutrition Nutrition { get; set; } = new();
}

public class Nutrition
{
    public double Fruit { get; set; }
    public double Vegetable { get; set; }
    public double Protein { get; set; }
    public double Grain { get; set; }
    public double Dairy { get; set; }
}