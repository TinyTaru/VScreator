using System.IO;
using System.Text.Json;
using System.Windows;

namespace VScreator;

/// <summary>
/// Interaction logic for ModCreationWindow.xaml
/// </summary>
public partial class ModCreationWindow : Window
{
    public ModCreationWindow()
    {
        InitializeComponent();
    }

    private void CreateModButton_Click(object sender, RoutedEventArgs e)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(ModNameTextBox.Text) ||
            string.IsNullOrWhiteSpace(ModIdTextBox.Text) ||
            string.IsNullOrWhiteSpace(AuthorTextBox.Text))
        {
            MessageBox.Show("Please fill in all required fields (Name, ID, and Author).",
                          "Missing Information", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            // Get the directory where the executable is located
            string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string modsDirectory = Path.Combine(exeDirectory, "mods");

            // Create mods directory if it doesn't exist
            if (!Directory.Exists(modsDirectory))
            {
                Directory.CreateDirectory(modsDirectory);
            }

            // Create mod-specific directory
            string modDirectory = Path.Combine(modsDirectory, ModIdTextBox.Text);
            if (!Directory.Exists(modDirectory))
            {
                Directory.CreateDirectory(modDirectory);
            }

            // Create modinfo.json file
            var modInfo = new ModInfo
            {
                Type = "content", // All VScreator mods are content mods
                ModId = ModIdTextBox.Text,
                Name = ModNameTextBox.Text,
                Authors = new[] { AuthorTextBox.Text },
                Description = DescriptionTextBox.Text,
                Version = VersionTextBox.Text
            };

            string jsonContent = JsonSerializer.Serialize(modInfo, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            string modInfoPath = Path.Combine(modDirectory, "modinfo.json");
            File.WriteAllText(modInfoPath, jsonContent);

            MessageBox.Show($"Mod '{ModNameTextBox.Text}' has been created successfully!\n\n" +
                          $"Location: {modDirectory}\n" +
                          $"Mod ID: {ModIdTextBox.Text}",
                          "Mod Created", MessageBoxButton.OK, MessageBoxImage.Information);

            // Open the mod workspace
            var workspaceWindow = new ModWorkspaceWindow(ModIdTextBox.Text, ModNameTextBox.Text);
            workspaceWindow.Show();

            // Close the creation window
            this.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"An error occurred while creating the mod:\n\n{ex.Message}",
                          "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}

public class ModInfo
{
    public string Type { get; set; } = "content";
    public string ModId { get; set; } = "";
    public string Name { get; set; } = "";
    public string[] Authors { get; set; } = Array.Empty<string>();
    public string Description { get; set; } = "";
    public string Version { get; set; } = "1.0.0";
}