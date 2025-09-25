using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace VScreator;

/// <summary>
/// Interaction logic for BlockCreationWindow.xaml
/// </summary>
public partial class BlockCreationWindow : Window
{
    private readonly string _modId;
    private readonly string _modName;
    private BlockData? _existingBlockData = null;
    private string _existingBlockName = "";
    private Action? _refreshCallback = null;

    public BlockCreationWindow(string modId, string modName)
    {
        InitializeComponent();
        _modId = modId;
        _modName = modName;

        // Load available block textures
        LoadBlockTextures();
    }

    // Constructor for creating new blocks with refresh callback
    public BlockCreationWindow(string modId, string modName, Action refreshCallback)
    {
        InitializeComponent();
        _modId = modId;
        _modName = modName;
        _refreshCallback = refreshCallback;

        // Load available block textures
        LoadBlockTextures();
    }

    // Constructor for editing existing blocks
    public BlockCreationWindow(string modId, string modName, BlockData existingBlockData, string existingBlockName)
    {
        InitializeComponent();
        _modId = modId;
        _modName = modName;
        _existingBlockData = existingBlockData;
        _existingBlockName = existingBlockName;

        // Load available block textures
        LoadBlockTextures();

        // Pre-fill the form with existing block data
        PreFillFormWithExistingData();
    }

    // Constructor for editing existing blocks with refresh callback
    public BlockCreationWindow(string modId, string modName, BlockData existingBlockData, string existingBlockName, Action refreshCallback)
    {
        InitializeComponent();
        _modId = modId;
        _modName = modName;
        _existingBlockData = existingBlockData;
        _existingBlockName = existingBlockName;
        _refreshCallback = refreshCallback;

        // Load available block textures
        LoadBlockTextures();

        // Pre-fill the form with existing block data
        PreFillFormWithExistingData();
    }

    private void LoadBlockTextures()
    {
        try
        {
            string modDirectory = GetModDirectory();
            string blockTexturesPath = Path.Combine(modDirectory, "assets", _modId, "textures", "block");

            if (Directory.Exists(blockTexturesPath))
            {
                string[] textureFiles = Directory.GetFiles(blockTexturesPath, "*.png");

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
                // No block textures found
                TextureComboBox.Items.Add("No block textures found");
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

    private void CreateBlockButton_Click(object sender, RoutedEventArgs e)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(BlockNameTextBox.Text) ||
            string.IsNullOrWhiteSpace(BlockIdTextBox.Text) ||
            TextureComboBox.SelectedItem == null ||
            string.IsNullOrWhiteSpace(ResistanceTextBox.Text))
        {
            MessageBox.Show("Please fill in all fields (Name, ID, select a texture, and set resistance).",
                           "Missing Information", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Validate resistance is a valid number
        if (!double.TryParse(ResistanceTextBox.Text, out double resistance))
        {
            MessageBox.Show("Please enter a valid number for resistance.",
                           "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            string modDirectory = GetModDirectory();
            string assetsPath = Path.Combine(modDirectory, "assets", _modId);
            string blockTypesPath = Path.Combine(assetsPath, "blocktypes");

            // Create blocktypes directory if it doesn't exist
            Directory.CreateDirectory(blockTypesPath);

            // Create block JSON
            var blockData = new BlockData
            {
                Code = BlockIdTextBox.Text,
                CreativeInventory = new CreativeInventory
                {
                    General = new[] { "*" }
                },
                DrawType = "Cube",
                Texture = new BlockTexture
                {
                    Base = $"block/{TextureComboBox.SelectedItem.ToString()}"
                },
                BlockMaterial = "Stone",
                Resistance = resistance,
                Sounds = new BlockSounds
                {
                    Place = "game:block/dirt",
                    Walk = "game:walk/stone"
                }
            };

            string jsonContent = JsonSerializer.Serialize(blockData, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            string blockFilePath = Path.Combine(blockTypesPath, $"{BlockIdTextBox.Text}.json");

            // Check if we're editing an existing block
            bool isEditing = _existingBlockData != null;

            if (isEditing)
            {
                // For editing, we need to check if the block ID changed
                string oldBlockCode = _existingBlockData.Code;
                string newBlockCode = BlockIdTextBox.Text;

                if (oldBlockCode != newBlockCode)
                {
                    // Block ID changed, remove old file
                    string oldBlockFilePath = Path.Combine(blockTypesPath, $"{oldBlockCode}.json");
                    if (File.Exists(oldBlockFilePath))
                    {
                        File.Delete(oldBlockFilePath);
                    }

                    // Update en.json to remove old entry and add new one
                    UpdateEnJsonFileForEdit(oldBlockCode, newBlockCode, BlockNameTextBox.Text);
                }
                else
                {
                    // Block ID unchanged, just update en.json
                    UpdateEnJsonFile(newBlockCode, BlockNameTextBox.Text);
                }
            }
            else
            {
                // Creating new block
                UpdateEnJsonFile(BlockIdTextBox.Text, BlockNameTextBox.Text);
            }

            File.WriteAllText(blockFilePath, jsonContent);

            string action = isEditing ? "updated" : "created";
            MessageBox.Show($"Block '{BlockNameTextBox.Text}' has been {action} successfully!\n\n" +
                           $"ID: {BlockIdTextBox.Text}\n" +
                           $"Texture: {TextureComboBox.SelectedItem}\n" +
                           $"Resistance: {resistance}\n" +
                           $"Location: {blockFilePath}",
                           $"Block {action}", MessageBoxButton.OK, MessageBoxImage.Information);

            // Call the refresh callback to update the blocks list in the parent window
            _refreshCallback?.Invoke();

            this.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"An error occurred while creating the block:\n\n{ex.Message}",
                           "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void PreFillFormWithExistingData()
    {
        if (_existingBlockData != null)
        {
            // Pre-fill the form fields with existing data
            BlockIdTextBox.Text = _existingBlockData.Code;
            BlockNameTextBox.Text = _existingBlockName;
            ResistanceTextBox.Text = _existingBlockData.Resistance.ToString();

            // Set the texture selection
            string textureBase = _existingBlockData.Texture.Base;
            if (textureBase.StartsWith("block/"))
            {
                string textureName = textureBase.Substring(6); // Remove "block/" prefix
                TextureComboBox.SelectedItem = textureName;
            }

            // Update the window title to indicate editing mode
            this.Title = $"Edit Block - {_modName}";

            // Update the button text to indicate editing mode
            CreateBlockButton.Content = "Update Block";
        }
    }

    private void UpdateEnJsonFile(string blockCode, string blockName)
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

            // Add or update the block entry
            string blockKey = $"block-{blockCode}";
            langEntries[blockKey] = blockName;

            // Write back to en.json with proper formatting
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string updatedContent = JsonSerializer.Serialize(langEntries, options);
            File.WriteAllText(enJsonPath, updatedContent);

            System.Diagnostics.Debug.WriteLine($"Updated en.json at {enJsonPath} with entry: {blockKey}: {blockName}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating en.json: {ex.Message}");
            // Don't show error to user as the block was created successfully
            // The language file update is a secondary feature
        }
    }

    private void UpdateEnJsonFileForEdit(string oldBlockCode, string newBlockCode, string newBlockName)
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
            string oldBlockKey = $"block-{oldBlockCode}";
            langEntries.Remove(oldBlockKey);

            // Add new entry
            string newBlockKey = $"block-{newBlockCode}";
            langEntries[newBlockKey] = newBlockName;

            // Write back to en.json with proper formatting
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string updatedContent = JsonSerializer.Serialize(langEntries, options);
            File.WriteAllText(enJsonPath, updatedContent);

            System.Diagnostics.Debug.WriteLine($"Updated en.json at {enJsonPath} - removed: {oldBlockKey}, added: {newBlockKey}: {newBlockName}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating en.json for edit: {ex.Message}");
            // Don't show error to user as the block was updated successfully
            // The language file update is a secondary feature
        }
    }
}

public class BlockData
{
    public string Code { get; set; } = "";
    public CreativeInventory CreativeInventory { get; set; } = new();
    public string DrawType { get; set; } = "";
    public BlockTexture Texture { get; set; } = new();
    public string BlockMaterial { get; set; } = "";
    public double Resistance { get; set; }
    public BlockSounds Sounds { get; set; } = new();
}

public class BlockTexture
{
    public string Base { get; set; } = "";
}

public class BlockSounds
{
    public string Place { get; set; } = "";
    public string Walk { get; set; } = "";
}