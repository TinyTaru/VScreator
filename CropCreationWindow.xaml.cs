using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace VScreator;

/// <summary>
/// Interaction logic for CropCreationWindow.xaml
/// </summary>
public partial class CropCreationWindow : Window
{
    private readonly string _modId;
    private readonly string _modName;
    private CropData? _existingCropData = null;
    private string _existingCropName = "";
    private Action? _refreshCallback = null;
    private bool _shapesLoaded = false;
    private bool _texturesLoaded = false;

    public CropCreationWindow(string modId, string modName)
    {
        InitializeComponent();
        _modId = modId;
        _modName = modName;

        // Reset loaded flags
        _shapesLoaded = false;
        _texturesLoaded = false;

        // Load available crop textures and shapes
        LoadCropTextures();
        LoadCropShapes();
    }

    // Constructor for creating new crops with refresh callback
    public CropCreationWindow(string modId, string modName, Action refreshCallback)
    {
        InitializeComponent();
        _modId = modId;
        _modName = modName;
        _refreshCallback = refreshCallback;

        // Reset loaded flags
        _shapesLoaded = false;
        _texturesLoaded = false;

        // Load available crop textures and shapes
        LoadCropTextures();
        LoadCropShapes();
    }

    // Constructor for editing existing crops
    public CropCreationWindow(string modId, string modName, CropData existingCropData, string existingCropName)
    {
        InitializeComponent();
        _modId = modId;
        _modName = modName;
        _existingCropData = existingCropData;
        _existingCropName = existingCropName;

        // Reset loaded flags
        _shapesLoaded = false;
        _texturesLoaded = false;

        // Load available crop textures and shapes
        LoadCropTextures();
        LoadCropShapes();

        // Pre-fill the form with existing crop data
        PreFillFormWithExistingData();
    }

    // Constructor for editing existing crops with refresh callback
    public CropCreationWindow(string modId, string modName, CropData existingCropData, string existingCropName, Action refreshCallback)
    {
        InitializeComponent();
        _modId = modId;
        _modName = modName;
        _existingCropData = existingCropData;
        _existingCropName = existingCropName;
        _refreshCallback = refreshCallback;

        // Reset loaded flags
        _shapesLoaded = false;
        _texturesLoaded = false;

        // Load available crop textures and shapes
        LoadCropTextures();
        LoadCropShapes();

        // Pre-fill the form with existing crop data
        PreFillFormWithExistingData();
    }

    private string GetModDirectory()
    {
        string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string modsDirectory = Path.Combine(exeDirectory, "mods");
        return Path.Combine(modsDirectory, _modId);
    }

    private void CreateCropButton_Click(object sender, RoutedEventArgs e)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(CropNameTextBox.Text) ||
            string.IsNullOrWhiteSpace(CropIdTextBox.Text) ||
            string.IsNullOrWhiteSpace(StatesTextBox.Text))
        {
            MessageBox.Show("Please fill in all required fields (Name, ID, and States).",
                           "Missing Information", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Validate states format
        string[] states = StatesTextBox.Text.Split(',');
        for (int i = 0; i < states.Length; i++)
        {
            states[i] = states[i].Trim();
            if (string.IsNullOrEmpty(states[i]))
            {
                MessageBox.Show("States field cannot contain empty values.",
                               "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        // Validate numeric fields
        if (!int.TryParse(NutrientConsumptionTextBox.Text, out int nutrientConsumption))
        {
            MessageBox.Show("Please enter a valid number for nutrient consumption.",
                           "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!double.TryParse(GrowthMonthsTextBox.Text, out double growthMonths))
        {
            MessageBox.Show("Please enter a valid number for growth months.",
                           "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(ColdDamageTextBox.Text, out int coldDamage))
        {
            MessageBox.Show("Please enter a valid number for cold damage temperature.",
                           "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(HeatDamageTextBox.Text, out int heatDamage))
        {
            MessageBox.Show("Please enter a valid number for heat damage temperature.",
                           "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            string modDirectory = GetModDirectory();
            string assetsPath = Path.Combine(modDirectory, "assets", _modId);
            string blockTypesPath = Path.Combine(assetsPath, "blocktypes", "plant", "crop");

            // Create directory structure if it doesn't exist
            Directory.CreateDirectory(blockTypesPath);

            // Generate states array for variantgroups
            var variantGroups = new[]
            {
                new VariantGroup
                {
                    code = "stage",
                    states = states
                }
            };

            // Generate shapeByType entries
            var shapeByType = new Dictionary<string, ShapeData>();
            for (int i = 0; i < states.Length && i < 5; i++)
            {
                string stateKey = $"*-{states[i]}";
                string shapeKey = $"Shape{(i + 1)}ComboBox";
                ComboBox? shapeComboBox = FindName(shapeKey) as ComboBox;
                string selectedShape = shapeComboBox?.SelectedItem?.ToString() ?? $"stage-{states[i]}";
                string shapePath = "block/plant/crop/" + selectedShape;

                shapeByType[stateKey] = new ShapeData
                {
                    @base = shapePath
                };
            }

            // Generate texturesByType entries
            var texturesByType = new Dictionary<string, Dictionary<string, TextureData>>();
            for (int i = 0; i < states.Length && i < 5; i++)
            {
                string stateKey = $"*-{states[i]}";
                string textureKey = $"Texture{(i + 1)}ComboBox";
                ComboBox? textureComboBox = FindName(textureKey) as ComboBox;
                ComboBoxItem? selectedTextureItem = textureComboBox?.SelectedItem as ComboBoxItem;
                string selectedTexture = selectedTextureItem?.Content?.ToString() ?? $"stage-{states[i]}";
                string texturePath = "block/plant/crop/" + selectedTexture;

                texturesByType[stateKey] = new Dictionary<string, TextureData>
                {
                    ["plant"] = new TextureData
                    {
                        @base = texturePath
                    }
                };
            }

            // Generate dropsByType
            var dropsByType = new Dictionary<string, List<CropDrop>>();

            // Ripe stage drops (last state)
            string ripeState = $"*-{states[states.Length - 1]}";
            var ripeDrops = new List<CropDrop>();

            if (!string.IsNullOrWhiteSpace(SeedsTextBox.Text))
            {
                ripeDrops.Add(new CropDrop
                {
                    type = "item",
                    code = SeedsTextBox.Text,
                    quantity = new QuantityData { avg = 1.2 }
                });
            }

            if (!string.IsNullOrWhiteSpace(ProduceTextBox.Text))
            {
                ripeDrops.Add(new CropDrop
                {
                    type = "item",
                    code = ProduceTextBox.Text,
                    quantity = new QuantityData { avg = 6, var = 2 }
                });
            }

            if (ripeDrops.Count > 0)
            {
                dropsByType[ripeState] = ripeDrops;
            }

            // Default drops for other stages
            var defaultDrops = new List<CropDrop>();
            if (!string.IsNullOrWhiteSpace(SeedsTextBox.Text))
            {
                defaultDrops.Add(new CropDrop
                {
                    type = "item",
                    code = SeedsTextBox.Text,
                    quantity = new QuantityData { avg = 0.7 }
                });
            }

            if (defaultDrops.Count > 0)
            {
                dropsByType["*"] = defaultDrops;
            }

            // Generate crop properties
            var cropProps = new CropProps
            {
                requiredNutrient = RequiredNutrientComboBox.Text,
                nutrientConsumption = nutrientConsumption,
                growthStages = states.Length,
                totalGrowthMonths = growthMonths,
                coldDamageBelow = coldDamage,
                damageGrowthStuntMul = 0.75,
                coldDamageRipeMul = 0.5,
                heatDamageAbove = heatDamage,
                multipleHarvests = MultipleHarvestsCheckBox.IsChecked ?? false
            };

            // Create the crop data
            var cropData = new CropData
            {
                code = $"crop-{CropIdTextBox.Text}",
                variantgroups = variantGroups,
                shapeByType = shapeByType,
                texturesByType = texturesByType,
                dropsByType = dropsByType,
                cropProps = cropProps
            };

            string jsonContent = JsonSerializer.Serialize(cropData, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            string cropFilePath = Path.Combine(blockTypesPath, $"{CropIdTextBox.Text}.json");

            // Check if we're editing an existing crop
            bool isEditing = _existingCropData != null;

            if (isEditing && _existingCropData != null)
            {
                // For editing, we need to check if the crop ID changed
                string oldCropCode = _existingCropData.code.Replace("crop-", "");
                string newCropCode = CropIdTextBox.Text;

                if (oldCropCode != newCropCode)
                {
                    // Crop ID changed, remove old file
                    string oldCropFilePath = Path.Combine(blockTypesPath, $"{oldCropCode}.json");
                    if (File.Exists(oldCropFilePath))
                    {
                        File.Delete(oldCropFilePath);
                    }

                    // Update en.json to remove old entry and add new one
                    UpdateEnJsonFileForEdit(oldCropCode, newCropCode, CropNameTextBox.Text);
                }
                else
                {
                    // Crop ID unchanged, just update en.json
                    UpdateEnJsonFile(newCropCode, CropNameTextBox.Text);
                }
            }
            else
            {
                // Creating new crop
                UpdateEnJsonFile(CropIdTextBox.Text, CropNameTextBox.Text);
            }

            File.WriteAllText(cropFilePath, jsonContent);

            string action = isEditing ? "updated" : "created";
            MessageBox.Show($"Crop '{CropNameTextBox.Text}' has been {action} successfully!\n\n" +
                           $"ID: {CropIdTextBox.Text}\n" +
                           $"States: {string.Join(", ", states)}\n" +
                           $"Location: {cropFilePath}",
                           $"Crop {action}", MessageBoxButton.OK, MessageBoxImage.Information);

            // Call the refresh callback to update the crops list in the parent window
            _refreshCallback?.Invoke();

            this.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"An error occurred while creating the crop:\n\n{ex.Message}",
                           "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadCropTextures()
    {
        if (_texturesLoaded) return;

        try
        {
            // Clear existing items
            Texture1ComboBox.Items.Clear();
            Texture2ComboBox.Items.Clear();
            Texture3ComboBox.Items.Clear();
            Texture4ComboBox.Items.Clear();
            Texture5ComboBox.Items.Clear();

            string modDirectory = GetModDirectory();
            string cropTexturesPath = Path.Combine(modDirectory, "assets", _modId, "textures", "block", "plant", "crop");

            if (Directory.Exists(cropTexturesPath))
            {
                string[] textureFiles = Directory.GetFiles(cropTexturesPath, "*.png");

                foreach (string textureFile in textureFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(textureFile);

                    // Add to all texture ComboBoxes
                    Texture1ComboBox.Items.Add(fileName);
                    Texture2ComboBox.Items.Add(fileName);
                    Texture3ComboBox.Items.Add(fileName);
                    Texture4ComboBox.Items.Add(fileName);
                    Texture5ComboBox.Items.Add(fileName);
                }
            }
            else
            {
                // No crop textures found, add default
                string defaultTexture = $"stage-1";
                Texture1ComboBox.Items.Add(defaultTexture);
                Texture2ComboBox.Items.Add(defaultTexture.Replace("stage-1", "stage-2"));
                Texture3ComboBox.Items.Add(defaultTexture.Replace("stage-1", "stage-3"));
                Texture4ComboBox.Items.Add(defaultTexture.Replace("stage-1", "stage-4"));
                Texture5ComboBox.Items.Add(defaultTexture.Replace("stage-1", "stage-5"));
            }

            _texturesLoaded = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading crop textures: {ex.Message}");
            Console.Out.Flush();
            MessageBox.Show($"Error loading crop textures: {ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadCropShapes()
    {
        if (_shapesLoaded) return;

        try
        {
            // Clear existing items
            Shape1ComboBox.Items.Clear();
            Shape2ComboBox.Items.Clear();
            Shape3ComboBox.Items.Clear();
            Shape4ComboBox.Items.Clear();
            Shape5ComboBox.Items.Clear();

            string modDirectory = GetModDirectory();
            string cropShapesPath = Path.Combine(modDirectory, "assets", _modId, "shapes", "block", "plant", "crop");

            if (Directory.Exists(cropShapesPath))
            {
                string[] shapeFiles = Directory.GetFiles(cropShapesPath, "*.json");

                foreach (string shapeFile in shapeFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(shapeFile);

                    // Add to all shape ComboBoxes
                    Shape1ComboBox.Items.Add(fileName);
                    Shape2ComboBox.Items.Add(fileName);
                    Shape3ComboBox.Items.Add(fileName);
                    Shape4ComboBox.Items.Add(fileName);
                    Shape5ComboBox.Items.Add(fileName);
                }
            }
            else
            {
                // No crop shapes found, add default
                string defaultShape = $"stage-1";
                Shape1ComboBox.Items.Add(defaultShape);
                Shape2ComboBox.Items.Add(defaultShape.Replace("stage-1", "stage-2"));
                Shape3ComboBox.Items.Add(defaultShape.Replace("stage-1", "stage-3"));
                Shape4ComboBox.Items.Add(defaultShape.Replace("stage-1", "stage-4"));
                Shape5ComboBox.Items.Add(defaultShape.Replace("stage-1", "stage-5"));
            }

            _shapesLoaded = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading crop shapes: {ex.Message}");
            Console.Out.Flush();
            MessageBox.Show($"Error loading crop shapes: {ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void TextureComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // This method is required for the XAML but we don't need to do anything special here
    }

    private void ShapeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // This method is required for the XAML but we don't need to do anything special here
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        // Close the window without saving
        this.Close();
    }

    private void PreFillFormWithExistingData()
    {
        if (_existingCropData != null)
        {
            // Pre-fill the form fields with existing data
            CropIdTextBox.Text = _existingCropData.code.Replace("crop-", "");
            CropNameTextBox.Text = _existingCropName;

            // Fill states
            if (_existingCropData.variantgroups.Length > 0)
            {
                StatesTextBox.Text = string.Join(", ", _existingCropData.variantgroups[0].states);
            }

            // Fill shapes
            for (int i = 0; i < _existingCropData.variantgroups[0].states.Length && i < 5; i++)
            {
                string state = _existingCropData.variantgroups[0].states[i];
                string stateKey = $"*-{state}";
                string shapeKey = $"Shape{(i + 1)}ComboBox";

                if (_existingCropData.shapeByType.TryGetValue(stateKey, out ShapeData? shapeData))
                {
                    ComboBox? shapeComboBox = FindName(shapeKey) as ComboBox;
                    string shapeBase = shapeData.@base;
                    if (shapeBase.StartsWith("block/plant/crop/"))
                    {
                        string shapeName = shapeBase.Substring(16); // Remove "block/plant/crop/" prefix
                        shapeComboBox!.SelectedItem = shapeName;
                    }
                }
            }

            // Fill textures
            for (int i = 0; i < _existingCropData.variantgroups[0].states.Length && i < 5; i++)
            {
                string state = _existingCropData.variantgroups[0].states[i];
                string stateKey = $"*-{state}";
                string textureKey = $"Texture{(i + 1)}ComboBox";

                if (_existingCropData.texturesByType.TryGetValue(stateKey, out Dictionary<string, TextureData>? textureData) &&
                    textureData.TryGetValue("plant", out TextureData? plantTexture))
                {
                    ComboBox? textureComboBox = FindName(textureKey) as ComboBox;
                    string textureBase = plantTexture.@base;
                    if (textureBase.StartsWith("block/plant/crop/"))
                    {
                        string textureName = textureBase.Substring(17); // Remove "block/plant/crop/" prefix
                        textureComboBox!.SelectedItem = textureName;
                    }
                }
            }

            // Fill drops
            if (_existingCropData.dropsByType.TryGetValue("*", out List<CropDrop>? defaultDrops) && defaultDrops.Count > 0)
            {
                SeedsTextBox.Text = defaultDrops[0].code;
            }

            string ripeState = $"*-{_existingCropData.variantgroups[0].states.Last()}";
            if (_existingCropData.dropsByType.TryGetValue(ripeState, out List<CropDrop>? ripeDrops) && ripeDrops.Count > 0)
            {
                // Find produce drop (not seeds)
                foreach (var drop in ripeDrops)
                {
                    if (drop.code != SeedsTextBox.Text)
                    {
                        ProduceTextBox.Text = drop.code;
                        break;
                    }
                }
            }

            // Fill crop properties
            if (_existingCropData.cropProps != null)
            {
                RequiredNutrientComboBox.Text = _existingCropData.cropProps.requiredNutrient;
                NutrientConsumptionTextBox.Text = _existingCropData.cropProps.nutrientConsumption.ToString();
                GrowthMonthsTextBox.Text = _existingCropData.cropProps.totalGrowthMonths.ToString();
                ColdDamageTextBox.Text = _existingCropData.cropProps.coldDamageBelow.ToString();
                HeatDamageTextBox.Text = _existingCropData.cropProps.heatDamageAbove.ToString();
                MultipleHarvestsCheckBox.IsChecked = _existingCropData.cropProps.multipleHarvests;
            }

            // Update the window title to indicate editing mode
            this.Title = $"Edit Crop - {_modName}";

            // Update the button text to indicate editing mode
            CreateCropButton.Content = "Update Crop";
        }
    }

    private void UpdateEnJsonFile(string cropCode, string cropName)
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

            // Add or update the crop entry
            string cropKey = $"block-crop-{cropCode}";
            langEntries[cropKey] = cropName;

            // Write back to en.json with proper formatting
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string updatedContent = JsonSerializer.Serialize(langEntries, options);
            File.WriteAllText(enJsonPath, updatedContent);

            Console.WriteLine($"Updated en.json at {enJsonPath} with entry: {cropKey}: {cropName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating en.json: {ex.Message}");
            // Don't show error to user as the crop was created successfully
            // The language file update is a secondary feature
        }
    }

    private void UpdateEnJsonFileForEdit(string oldCropCode, string newCropCode, string newCropName)
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
            string oldCropKey = $"block-crop-{oldCropCode}";
            langEntries.Remove(oldCropKey);

            // Add new entry
            string newCropKey = $"block-crop-{newCropCode}";
            langEntries[newCropKey] = newCropName;

            // Write back to en.json with proper formatting
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string updatedContent = JsonSerializer.Serialize(langEntries, options);
            File.WriteAllText(enJsonPath, updatedContent);

            Console.WriteLine($"Updated en.json at {enJsonPath} - removed: {oldCropKey}, added: {newCropKey}: {newCropName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating en.json for edit: {ex.Message}");
            // Don't show error to user as the crop was updated successfully
            // The language file update is a secondary feature
        }
    }
}

// Data classes for JSON serialization
public class CropData
{
    public string code { get; set; } = "";
    public VariantGroup[] variantgroups { get; set; } = Array.Empty<VariantGroup>();
    public Dictionary<string, ShapeData> shapeByType { get; set; } = new();
    public Dictionary<string, Dictionary<string, TextureData>> texturesByType { get; set; } = new();
    public Dictionary<string, List<CropDrop>> dropsByType { get; set; } = new();
    public CropProps? cropProps { get; set; }
}

public class VariantGroup
{
    public string code { get; set; } = "";
    public string[] states { get; set; } = Array.Empty<string>();
}

public class ShapeData
{
    public string @base { get; set; } = "";
}

public class TextureData
{
    public string @base { get; set; } = "";
}

public class CropDrop
{
    public string type { get; set; } = "";
    public string code { get; set; } = "";
    public QuantityData? quantity { get; set; }
}

public class QuantityData
{
    public double avg { get; set; }
    public double var { get; set; }
}

public class CropProps
{
    public string requiredNutrient { get; set; } = "";
    public int nutrientConsumption { get; set; }
    public int growthStages { get; set; }
    public double totalGrowthMonths { get; set; }
    public int coldDamageBelow { get; set; }
    public double damageGrowthStuntMul { get; set; }
    public double coldDamageRipeMul { get; set; }
    public int heatDamageAbove { get; set; }
    public bool multipleHarvests { get; set; }
}