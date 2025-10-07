using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace VScreator;

/// <summary>
/// Interaction logic for WorldGenCreationWindow.xaml
/// </summary>
public partial class WorldGenCreationWindow : Window
{
    private readonly string _modId;
    private readonly string _modName;
    private WorldGenData? _existingWorldGenData = null;
    private string _existingWorldGenName = "";
    private Action? _refreshCallback = null;

    public WorldGenCreationWindow(string modId, string modName)
    {
        InitializeComponent();
        _modId = modId;
        _modName = modName;

        // Set default selection for placement dropdown
        PlacementComboBox.SelectedIndex = 4; // Underground (matches the example)
    }

    // Constructor for creating new world gen with refresh callback
    public WorldGenCreationWindow(string modId, string modName, Action refreshCallback)
    {
        InitializeComponent();
        _modId = modId;
        _modName = modName;
        _refreshCallback = refreshCallback;

        // Set default selection for placement dropdown
        PlacementComboBox.SelectedIndex = 4; // Underground (matches the example)
    }

    // Constructor for editing existing world gen
    public WorldGenCreationWindow(string modId, string modName, WorldGenData existingWorldGenData, string existingWorldGenName)
    {
        InitializeComponent();
        _modId = modId;
        _modName = modName;
        _existingWorldGenData = existingWorldGenData;
        _existingWorldGenName = existingWorldGenName;

        // Pre-fill the form with existing world gen data
        PreFillFormWithExistingData();
    }

    // Constructor for editing existing world gen with refresh callback
    public WorldGenCreationWindow(string modId, string modName, WorldGenData existingWorldGenData, string existingWorldGenName, Action refreshCallback)
    {
        InitializeComponent();
        _modId = modId;
        _modName = modName;
        _existingWorldGenData = existingWorldGenData;
        _existingWorldGenName = existingWorldGenName;
        _refreshCallback = refreshCallback;

        // Pre-fill the form with existing world gen data
        PreFillFormWithExistingData();
    }

    private string GetModDirectory()
    {
        string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string modsDirectory = Path.Combine(exeDirectory, "mods");
        return Path.Combine(modsDirectory, _modId);
    }

    private void CreateWorldgenButton_Click(object sender, RoutedEventArgs e)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(BlockCodesTextBox.Text) ||
            string.IsNullOrWhiteSpace(ChanceTextBox.Text) ||
            PlacementComboBox.SelectedItem == null ||
            string.IsNullOrWhiteSpace(QuantityAvgTextBox.Text) ||
            string.IsNullOrWhiteSpace(QuantityVarTextBox.Text))
        {
            MessageBox.Show("Please fill in all fields (Block Codes, Placement, Rarity, and Quantity values).",
                            "Missing Information", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Validate chance is a valid number
        if (!int.TryParse(ChanceTextBox.Text, out int chance))
        {
            MessageBox.Show("Please enter a valid number for rarity (chance).",
                            "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Validate quantity values are valid numbers
        if (!double.TryParse(QuantityAvgTextBox.Text, out double quantityAvg))
        {
            MessageBox.Show("Please enter a valid number for quantity average.",
                            "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!double.TryParse(QuantityVarTextBox.Text, out double quantityVar))
        {
            MessageBox.Show("Please enter a valid number for quantity variation.",
                            "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            string modDirectory = GetModDirectory();
            string assetsPath = Path.Combine(modDirectory, "assets", _modId);
            string worldGenPath = Path.Combine(assetsPath, "worldgen");

            // Create worldgen directory if it doesn't exist
            Directory.CreateDirectory(worldGenPath);

            // Parse block codes from comma-separated string
            string[] blockCodeArray = BlockCodesTextBox.Text
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(code => code.Trim())
                .Where(code => !string.IsNullOrEmpty(code))
                .ToArray();

            if (blockCodeArray.Length == 0)
            {
                MessageBox.Show("Please enter at least one valid block code.",
                                "Invalid Block Codes", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Create world gen JSON
            var worldGenData = new WorldGenData
            {
                Comment = CommentTextBox.Text,
                BlockCodes = blockCodeArray,
                Quantity = new WorldGenQuantity
                {
                    Avg = quantityAvg,
                    Var = quantityVar
                },
                Chance = chance,
                Placement = ((ComboBoxItem)PlacementComboBox.SelectedItem).Tag.ToString()
            };

            // Generate filename based on first block code or use a default name
            string fileName = !string.IsNullOrWhiteSpace(BlockCodesTextBox.Text)
                ? blockCodeArray[0].Replace(":", "_").Replace("-", "_") + "_worldgen"
                : "custom_worldgen";

            string jsonContent = JsonSerializer.Serialize(worldGenData, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            string worldGenFilePath = Path.Combine(worldGenPath, $"{fileName}.json");

            // Check if we're editing an existing world gen
            bool isEditing = _existingWorldGenData != null;

            if (isEditing && _existingWorldGenData != null)
            {
                // For editing, we need to check if the filename changed
                string oldFileName = _existingWorldGenName;
                string newFileName = fileName;

                if (oldFileName != newFileName)
                {
                    // File name changed, remove old file
                    string oldFilePath = Path.Combine(worldGenPath, $"{oldFileName}.json");
                    if (File.Exists(oldFilePath))
                    {
                        File.Delete(oldFilePath);
                    }
                }
            }

            File.WriteAllText(worldGenFilePath, jsonContent);

            string action = isEditing ? "updated" : "created";
            MessageBox.Show($"World generation '{fileName}' has been {action} successfully!\n\n" +
                            $"Block Codes: {string.Join(", ", blockCodeArray)}\n" +
                            $"Placement: {worldGenData.Placement}\n" +
                            $"Rarity: {chance}\n" +
                            $"Location: {worldGenFilePath}",
                            $"World Gen {action}", MessageBoxButton.OK, MessageBoxImage.Information);

            // Call the refresh callback to update the world gen list in the parent window
            _refreshCallback?.Invoke();

            this.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"An error occurred while creating the world generation:\n\n{ex.Message}",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void PreFillFormWithExistingData()
    {
        if (_existingWorldGenData != null)
        {
            // Pre-fill the form fields with existing data
            CommentTextBox.Text = _existingWorldGenData.Comment;
            BlockCodesTextBox.Text = string.Join(", ", _existingWorldGenData.BlockCodes);
            ChanceTextBox.Text = _existingWorldGenData.Chance.ToString();
            QuantityAvgTextBox.Text = _existingWorldGenData.Quantity.Avg.ToString();
            QuantityVarTextBox.Text = _existingWorldGenData.Quantity.Var.ToString();

            // Set the placement selection
            string placementValue = _existingWorldGenData.Placement;
            foreach (ComboBoxItem item in PlacementComboBox.Items)
            {
                if (item.Tag.ToString() == placementValue)
                {
                    PlacementComboBox.SelectedItem = item;
                    break;
                }
            }

            // Update the window title to indicate editing mode
            this.Title = $"Edit World Gen - {_modName}";

            // Update the button text to indicate editing mode
            // TODO: Fix UI element reference after XAML compilation
            // CreateWorldgenButton.Content = "Update World Gen";
        }
    }
}

// Data models for world generation JSON structure
public class WorldGenData
{
    public string Comment { get; set; } = "";
    public string[] BlockCodes { get; set; } = Array.Empty<string>();
    public WorldGenQuantity Quantity { get; set; } = new();
    public int Chance { get; set; }
    public string Placement { get; set; } = "";
}

public class WorldGenQuantity
{
    public double Avg { get; set; }
    public double Var { get; set; }
}