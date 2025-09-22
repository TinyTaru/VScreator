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

        // Load available item textures
        LoadItemTextures();
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

    private void CreateItemButton_Click(object sender, RoutedEventArgs e)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(ItemNameTextBox.Text) ||
            string.IsNullOrWhiteSpace(ItemIdTextBox.Text) ||
            TextureComboBox.SelectedItem == null)
        {
            MessageBox.Show("Please fill in all fields (Name, ID, and select a texture).",
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
                }
            };

            string jsonContent = JsonSerializer.Serialize(itemData, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            string itemFilePath = Path.Combine(itemTypesPath, $"{ItemIdTextBox.Text}.json");
            File.WriteAllText(itemFilePath, jsonContent);

            MessageBox.Show($"Item '{ItemNameTextBox.Text}' has been created successfully!\n\n" +
                          $"ID: {ItemIdTextBox.Text}\n" +
                          $"Texture: {TextureComboBox.SelectedItem}\n" +
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
}

public class ItemData
{
    public string Code { get; set; } = "";
    public CreativeInventory CreativeInventory { get; set; } = new();
    public ItemTexture Texture { get; set; } = new();
}

public class CreativeInventory
{
    public string[] General { get; set; } = Array.Empty<string>();
}

public class ItemTexture
{
    public string Base { get; set; } = "";
}