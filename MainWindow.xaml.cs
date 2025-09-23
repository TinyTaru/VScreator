using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using Microsoft.Win32;
using System.Reflection;

namespace VScreator;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void NewModButton_Click(object sender, RoutedEventArgs e)
    {
        // Open the mod creation window
        var modCreationWindow = new ModCreationWindow();
        modCreationWindow.ShowDialog();
    }

    private void TestOpenExistingMod()
    {
        try
        {
            // Test opening the existing mod directly
            string modPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mods", "myawesomemod");
            System.Diagnostics.Debug.WriteLine($"Testing mod path: {modPath}");

            var validationResult = ValidateModFolder(modPath);
            if (validationResult.IsValid)
            {
                var modInfo = GetModInfo(modPath);
                if (modInfo != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Mod info extracted: {modInfo.Item2} (ID: {modInfo.Item1})");
                    var modWorkspace = new ModWorkspaceWindow(modInfo.Item1, modInfo.Item2);
                    modWorkspace.ShowDialog();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Failed to extract mod info");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Validation failed: {validationResult.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Test error: {ex.Message}");
        }
    }

    private void OpenModButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Get the mods directory path
            string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string modsDirectory = System.IO.Path.Combine(exeDirectory, "mods");

            // Test opening the existing mod directly for debugging
            // TestOpenExistingMod();

            // Create mods directory if it doesn't exist
            if (!System.IO.Directory.Exists(modsDirectory))
            {
                try
                {
                    System.IO.Directory.CreateDirectory(modsDirectory);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Could not create mods directory:\n\n{ex.Message}",
                        "Directory Creation Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }
            }

            // Ensure mods directory exists and is accessible
            if (!System.IO.Directory.Exists(modsDirectory))
            {
                MessageBox.Show(
                    "The mods directory could not be created or accessed.\n\n" +
                    "Please check your file system permissions.",
                    "Directory Access Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            // Open folder picker dialog using robust WPF-compatible approach
            var folderDialog = new OpenFolderDialog
            {
                Title = "Select a mod folder to open",
                InitialDirectory = modsDirectory
            };

            if (folderDialog.ShowDialog(this) == true)
            {
                string selectedPath = folderDialog.FolderName;

                if (string.IsNullOrEmpty(selectedPath))
                {
                    MessageBox.Show(
                        "No folder was selected.",
                        "No Selection",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Debug output
                System.Diagnostics.Debug.WriteLine($"Selected folder: {selectedPath}");

                // Validate that this is a valid mod folder
                var validationResult = ValidateModFolder(selectedPath);
                if (!validationResult.IsValid)
                {
                    System.Diagnostics.Debug.WriteLine($"Validation failed: {validationResult.ErrorMessage}");
                    MessageBox.Show(
                        validationResult.ErrorMessage,
                        "Invalid Mod Folder",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Extract mod information
                var modInfo = GetModInfo(selectedPath);
                if (modInfo == null)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to extract mod information");
                    MessageBox.Show(
                        "Could not read mod information from the selected folder.\n\n" +
                        "Make sure the folder contains a valid modinfo.json file with proper modid and name fields.",
                        "Invalid Mod Information",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"Opening mod: {modInfo.Item2} (ID: {modInfo.Item1})");

                try
                {
                    // Open the mod workspace
                    var modWorkspace = new ModWorkspaceWindow(modInfo.Item1, modInfo.Item2);
                    modWorkspace.ShowDialog();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Workspace error: {ex.Message}");
                    MessageBox.Show(
                        $"Failed to open mod workspace:\n\n{ex.Message}",
                        "Workspace Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"An error occurred while opening the mod:\n\n{ex.Message}",
                "Open Mod Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    // Validation result class for better error reporting
    private class ModFolderValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = "";

        public static ModFolderValidationResult Valid()
        {
            return new ModFolderValidationResult { IsValid = true };
        }

        public static ModFolderValidationResult Invalid(string message)
        {
            return new ModFolderValidationResult { IsValid = false, ErrorMessage = message };
        }
    }

    private ModFolderValidationResult ValidateModFolder(string folderPath)
    {
        try
        {
            // Check if path is null or empty
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return ModFolderValidationResult.Invalid("No folder path provided.");
            }

            // Check if directory exists
            if (!System.IO.Directory.Exists(folderPath))
            {
                return ModFolderValidationResult.Invalid("The selected folder does not exist.");
            }

            // Check if we have read access to the directory
            if (!HasDirectoryAccess(folderPath))
            {
                return ModFolderValidationResult.Invalid("Cannot access the selected folder. Check permissions.");
            }

            // Check if the folder contains a modinfo.json file
            string modInfoPath = System.IO.Path.Combine(folderPath, "modinfo.json");
            if (!System.IO.File.Exists(modInfoPath))
            {
                return ModFolderValidationResult.Invalid(
                    "The selected folder does not contain a modinfo.json file.\n\n" +
                    "Mod folders must contain a modinfo.json file to be valid.");
            }

            // Validate the modinfo.json file
            var jsonValidation = ValidateModInfoFile(modInfoPath);
            if (!jsonValidation.IsValid)
            {
                return jsonValidation;
            }

            return ModFolderValidationResult.Valid();
        }
        catch (Exception ex)
        {
            return ModFolderValidationResult.Invalid($"Error validating folder: {ex.Message}");
        }
    }

    private bool HasDirectoryAccess(string folderPath)
    {
        try
        {
            // Try to get directory info to test access
            var directoryInfo = new System.IO.DirectoryInfo(folderPath);
            directoryInfo.GetDirectories(); // This will throw if no access
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (System.IO.IOException)
        {
            return false;
        }
    }

    private ModFolderValidationResult ValidateModInfoFile(string modInfoPath)
    {
        try
        {
            // Check if file exists
            if (!System.IO.File.Exists(modInfoPath))
            {
                return ModFolderValidationResult.Invalid("modinfo.json file does not exist.");
            }

            // Check if we can read the file
            if (!System.IO.File.Exists(modInfoPath))
            {
                return ModFolderValidationResult.Invalid("Cannot read modinfo.json file.");
            }

            // Read and validate JSON content
            string jsonContent;
            try
            {
                jsonContent = System.IO.File.ReadAllText(modInfoPath);
            }
            catch (Exception ex)
            {
                return ModFolderValidationResult.Invalid($"Cannot read modinfo.json: {ex.Message}");
            }

            // Basic JSON validation
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                return ModFolderValidationResult.Invalid("modinfo.json file is empty.");
            }

            // Check for basic JSON structure
            jsonContent = jsonContent.Trim();
            if (!jsonContent.StartsWith("{") || !jsonContent.EndsWith("}"))
            {
                return ModFolderValidationResult.Invalid("modinfo.json does not appear to be valid JSON.");
            }

            return ModFolderValidationResult.Valid();
        }
        catch (Exception ex)
        {
            return ModFolderValidationResult.Invalid($"Error validating modinfo.json: {ex.Message}");
        }
    }

    private Tuple<string, string>? GetModInfo(string folderPath)
    {
        try
        {
            string modInfoPath = System.IO.Path.Combine(folderPath, "modinfo.json");

            if (!System.IO.File.Exists(modInfoPath))
            {
                return null;
            }

            string jsonContent = System.IO.File.ReadAllText(modInfoPath);

            // Extract modid and name using robust parsing
            string modId = ExtractJsonValue(jsonContent, "modid");
            string modName = ExtractJsonValue(jsonContent, "name");

            // Validate extracted values
            if (string.IsNullOrWhiteSpace(modId))
            {
                System.Diagnostics.Debug.WriteLine("Warning: modid not found or empty in modinfo.json");
                return null;
            }

            if (string.IsNullOrWhiteSpace(modName))
            {
                System.Diagnostics.Debug.WriteLine("Warning: name not found or empty in modinfo.json");
                return null;
            }

            return new Tuple<string, string>(modId.Trim(), modName.Trim());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error reading mod info: {ex.Message}");
            return null;
        }
    }

    private string ExtractJsonValue(string json, string key)
    {
        try
        {
            // Handle both quoted and unquoted values with case-insensitive matching
            string pattern1 = $"\"{key}\"\\s*:\\s*\"([^\"]+)\"";  // "key": "value"
            string pattern2 = $"\"{key}\"\\s*:\\s*([^,\\s\\}}]+)"; // "key": value (unquoted)

            // Try pattern 1 first (quoted value) - case insensitive
            var match = System.Text.RegularExpressions.Regex.Match(json, pattern1, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }

            // Try pattern 2 (unquoted value) - case insensitive
            match = System.Text.RegularExpressions.Regex.Match(json, pattern2, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }

            // If the exact key didn't work, try with different capitalizations
            // This handles cases like "ModId" vs "modid", "Name" vs "name"
            string[] variations = { key.ToLower(), key.ToUpper(), char.ToUpper(key[0]) + key.Substring(1).ToLower() };

            foreach (string variation in variations)
            {
                if (variation != key)
                {
                    string varPattern1 = $"\"{variation}\"\\s*:\\s*\"([^\"]+)\"";
                    string varPattern2 = $"\"{variation}\"\\s*:\\s*([^,\\s\\}}]+)";

                    match = System.Text.RegularExpressions.Regex.Match(json, varPattern1, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        return match.Groups[1].Value.Trim();
                    }

                    match = System.Text.RegularExpressions.Regex.Match(json, varPattern2, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        return match.Groups[1].Value.Trim();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error extracting JSON value for key '{key}': {ex.Message}");
        }

        return string.Empty;
    }

    // Robust WPF-compatible folder picker dialog
    public class OpenFolderDialog
    {
        public string Title { get; set; } = "Select Folder";
        public string InitialDirectory { get; set; } = "";
        public string? FolderName { get; private set; }

        public bool? ShowDialog()
        {
            return ShowDialog(null);
        }

        public bool? ShowDialog(Window? owner = null)
        {
            FolderName = null;

            try
            {
                // Method 1: Try using a custom folder browser dialog
                if (TryShowCustomFolderDialog(owner))
                {
                    return true;
                }

                // Method 2: Fallback to SaveFileDialog approach
                if (TryShowSaveFileDialogApproach(owner))
                {
                    return true;
                }

                // Method 3: Final fallback using OpenFileDialog
                if (TryShowOpenFileDialogApproach(owner))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OpenFolderDialog error: {ex.Message}");
            }

            return false;
        }

        private bool TryShowCustomFolderDialog(Window? owner)
        {
            try
            {
                // Try to load Windows Forms assembly
                var assembly = System.Reflection.Assembly.Load("System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
                if (assembly == null)
                {
                    System.Diagnostics.Debug.WriteLine("Could not load System.Windows.Forms assembly");
                    return false;
                }

                var folderBrowserDialogType = assembly.GetType("System.Windows.Forms.FolderBrowserDialog");
                if (folderBrowserDialogType == null)
                {
                    System.Diagnostics.Debug.WriteLine("Could not find FolderBrowserDialog type");
                    return false;
                }

                var dialog = Activator.CreateInstance(folderBrowserDialogType);
                if (dialog == null)
                {
                    System.Diagnostics.Debug.WriteLine("Could not create FolderBrowserDialog instance");
                    return false;
                }

                try
                {
                    // Set properties using reflection
                    var descriptionProperty = folderBrowserDialogType.GetProperty("Description");
                    if (descriptionProperty != null)
                    {
                        descriptionProperty.SetValue(dialog, Title);
                    }

                    var selectedPathProperty = folderBrowserDialogType.GetProperty("SelectedPath");
                    if (selectedPathProperty != null && !string.IsNullOrEmpty(InitialDirectory))
                    {
                        selectedPathProperty.SetValue(dialog, InitialDirectory);
                    }

                    var showNewFolderButtonProperty = folderBrowserDialogType.GetProperty("ShowNewFolderButton");
                    if (showNewFolderButtonProperty != null)
                    {
                        showNewFolderButtonProperty.SetValue(dialog, false);
                    }

                    // Show dialog - this requires a Windows Forms window handle
                    var showDialogMethod = folderBrowserDialogType.GetMethod("ShowDialog");
                    if (showDialogMethod != null)
                    {
                        // For WPF, we need to get the window handle differently
                        var result = showDialogMethod.Invoke(dialog, null);
                        if (result != null)
                        {
                            // Check if the result indicates success
                            var resultString = result.ToString();
                            if (resultString == "OK" || resultString == "1")
                            {
                                var folderPath = selectedPathProperty?.GetValue(dialog)?.ToString();
                                if (!string.IsNullOrEmpty(folderPath) && System.IO.Directory.Exists(folderPath))
                                {
                                    FolderName = folderPath;
                                    return true;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error setting up FolderBrowserDialog: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Custom folder dialog failed: {ex.Message}");
            }

            return false;
        }

        private bool TryShowSaveFileDialogApproach(Window? owner)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Title = Title + " - Navigate to your mod folder and click Save",
                    InitialDirectory = InitialDirectory,
                    Filter = "All Files|*.*",
                    CheckFileExists = false,
                    CheckPathExists = true,
                    FileName = "Select_Folder_Here",
                    OverwritePrompt = false,
                    CreatePrompt = false
                };

                // Owner property not available in SaveFileDialog

                if (dialog.ShowDialog() == true)
                {
                    string selectedPath = dialog.FileName;

                    // If user selected a file, get its directory
                    if (System.IO.File.Exists(selectedPath))
                    {
                        selectedPath = System.IO.Path.GetDirectoryName(selectedPath);
                    }

                    // If the path is valid and exists, use it
                    if (!string.IsNullOrEmpty(selectedPath) && System.IO.Directory.Exists(selectedPath))
                    {
                        FolderName = selectedPath;
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveFileDialog approach failed: {ex.Message}");
            }

            return false;
        }

        private bool TryShowOpenFileDialogApproach(Window? owner)
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = Title + " - Select any file in the target folder",
                    InitialDirectory = InitialDirectory,
                    CheckFileExists = false,
                    CheckPathExists = false,
                    Multiselect = false,
                    Filter = "All Files|*.*",
                    DereferenceLinks = true
                };

                // Owner property not available in OpenFileDialog

                if (dialog.ShowDialog() == true)
                {
                    string selectedFile = dialog.FileName;

                    if (!string.IsNullOrEmpty(selectedFile))
                    {
                        string folderPath = System.IO.Path.GetDirectoryName(selectedFile);
                        if (!string.IsNullOrEmpty(folderPath) && System.IO.Directory.Exists(folderPath))
                        {
                            FolderName = folderPath;
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OpenFileDialog approach failed: {ex.Message}");
            }

            return false;
        }
    }
}