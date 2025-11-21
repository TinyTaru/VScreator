using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;
using System.Windows.Media.Imaging;

namespace VScreator;

/// <summary>
/// Interaction logic for ModPropertiesWindow.xaml
/// </summary>
public partial class ModPropertiesWindow : Window
{
    private readonly string _modId;
    private readonly string _modName;

    public ModPropertiesWindow(string modId, string modName)
    {
        InitializeComponent();
        _modId = modId;
        _modName = modName;

        // Update window title
        Title = $"Mod Properties - {_modName}";

        // Load current version
        LoadCurrentVersion();

        // Load current mod icon
        LoadModIcon();
    }

    private void LoadCurrentVersion()
    {
        try
        {
            string modDirectory = GetModDirectory();
            string modInfoPath = Path.Combine(modDirectory, "modinfo.json");

            if (File.Exists(modInfoPath))
            {
                string jsonContent = File.ReadAllText(modInfoPath);
                // Simple JSON parsing to extract version
                string versionPattern = "\"version\"\\s*:\\s*\"([^\"]+)\"";
                var match = System.Text.RegularExpressions.Regex.Match(jsonContent, versionPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (match.Success)
                {
                    VersionTextBox.Text = match.Groups[1].Value;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading current version: {ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadModIcon()
    {
        try
        {
            string modDirectory = GetModDirectory();
            string iconPath = Path.Combine(modDirectory, "modicon.png");

            if (File.Exists(iconPath))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bitmap.UriSource = new Uri(iconPath, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();

                ModIconImage.Source = bitmap;
            }
            else
            {
                ModIconImage.Source = null;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading mod icon: {ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string newVersion = VersionTextBox.Text.Trim();

            if (string.IsNullOrEmpty(newVersion))
            {
                MessageBox.Show("Version cannot be empty.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Basic version format validation (should be something like x.y.z)
            if (!System.Text.RegularExpressions.Regex.IsMatch(newVersion, @"^\d+\.\d+\.\d+$"))
            {
                MessageBox.Show("Version should be in format x.y.z (e.g., 1.0.0)", "Invalid Format", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Save the version to modinfo.json
            SaveVersionToModInfo(newVersion);

            MessageBox.Show($"Mod version updated to {newVersion}!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving version: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveVersionToModInfo(string version)
    {
        string modDirectory = GetModDirectory();
        string modInfoPath = Path.Combine(modDirectory, "modinfo.json");

        // Ensure mod directory exists
        Directory.CreateDirectory(modDirectory);

        // Create or update modinfo.json
        var modInfo = new
        {
            type = "content",
            modid = _modId,
            name = _modName,
            version = version,
            description = $"Mod created with VScreator",
            authors = new[] { "VScreator User" },
            website = "",
            side = "Universal"
        };

        string jsonContent = JsonSerializer.Serialize(modInfo, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(modInfoPath, jsonContent);
    }

    private void ModIconBorder_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        try
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select Mod Icon",
                Filter = "PNG Files (*.png)|*.png",
                CheckFileExists = true,
                CheckPathExists = true,
                Multiselect = false
            };

            bool? result = openFileDialog.ShowDialog(this);
            if (result != true)
            {
                return;
            }

            string selectedFilePath = openFileDialog.FileName;
            string modDirectory = GetModDirectory();
            Directory.CreateDirectory(modDirectory);
            string iconPath = Path.Combine(modDirectory, "modicon.png");

            try
            {
                if (File.Exists(iconPath))
                {
                    File.Delete(iconPath);
                }

                File.Copy(selectedFilePath, iconPath, overwrite: false);

                LoadModIcon();

                MessageBox.Show("Mod icon updated successfully.", "Icon Updated", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating mod icon: {ex.Message}", "Icon Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error selecting mod icon: {ex.Message}", "Icon Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private string GetModDirectory()
    {
        // Get the directory where the executable is located
        string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string modsDirectory = Path.Combine(exeDirectory, "mods");
        return Path.Combine(modsDirectory, _modId);
    }
}