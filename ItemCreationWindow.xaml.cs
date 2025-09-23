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

    public ItemCreationWindow(string modId, string modName)
    {
        InitializeComponent();
        _modId = modId;
        _modName = modName;

        // Load available item textures and shapes
        LoadItemTextures();
        LoadItemShapes();
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

            string jsonContent = JsonSerializer.Serialize(itemData, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            string itemFilePath = Path.Combine(itemTypesPath, $"{ItemIdTextBox.Text}.json");
            File.WriteAllText(itemFilePath, jsonContent);

            // Update the en.json file with the new item entry
            UpdateEnJsonFile(ItemIdTextBox.Text, ItemNameTextBox.Text);

            MessageBox.Show($"Item '{ItemNameTextBox.Text}' has been created successfully!\n\n" +
                           $"ID: {ItemIdTextBox.Text}\n" +
                           $"Texture: {TextureComboBox.SelectedItem}\n" +
                           $"Shape: {ShapeComboBox.SelectedItem}\n" +
                           $"Location: {itemFilePath}",
                           "Item Created", MessageBoxButton.OK, MessageBoxImage.Information);

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
}

public class ItemData
{
    public string Code { get; set; } = "";
    public CreativeInventory CreativeInventory { get; set; } = new();
    public ItemTexture Texture { get; set; } = new();
    public ItemShape Shape { get; set; } = new();
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