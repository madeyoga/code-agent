using System.ComponentModel;

namespace DotNuxt.Tools;

public static class AgentTools
{
    [Description("Read the content of a file.")]
    public static string ReadFile(
        [Description("Absolute path to the file")] string filePath)
    {
        try
        {
            if (!File.Exists(filePath)) return $"Error: File '{filePath}' not found.";
            return File.ReadAllText(filePath);
        }
        catch (Exception ex)
        {
            return $"Error reading file: {ex.Message}";
        }
    }

    [Description("Search for a pattern in a file using regular expressions.")]
    public static string Grep(
        [Description("Regex pattern to search for")] string pattern,
        [Description("File path to search in")] string filePath)
    {
        try
        {
            if (!File.Exists(filePath)) return $"Error: File '{filePath}' not found.";
            var regex = new System.Text.RegularExpressions.Regex(pattern);
            var lines = File.ReadLines(filePath).ToList();
            var matches = lines
                .Select((text, index) => new { text, index })
                .Where(line => regex.IsMatch(line.text))
                .Select(line => $"{line.index + 1}: {line.text}");

            return matches.Any() ? string.Join("\n", matches) : "No matches found.";
        }
        catch (Exception ex)
        {
            return $"Error during grep: {ex.Message}";
        }
    }

    [Description("Load the content of a .NET skill by its path. List available skills with 'skills' or 'plugins'.")]
    public static string GetSkill(
        [Description("Skill path like 'dotnet/csharp-scripts' or 'dotnet-aspnetcore/dotnet-webapi'")] string path)
    {
        var skillsDir = Path.Combine(AppContext.BaseDirectory, "skills");
        if (!Directory.Exists(skillsDir)) return "No skills installed.";

        var catalog = new List<string>();
        foreach (var pluginDir in Directory.GetDirectories(skillsDir))
        {
            var pluginName = Path.GetFileName(pluginDir);
            var skills = new List<string>();
            foreach (var skillDir in Directory.GetDirectories(pluginDir))
            {
                if (File.Exists(Path.Combine(skillDir, "SKILL.md")))
                    skills.Add(Path.GetFileName(skillDir));
            }
            if (skills.Count > 0)
                catalog.Add($"  [{pluginName}] {string.Join(", ", skills)}");
        }

        return string.Join("\n", catalog);
    }

    [Description("Execute a shell command in the current directory and return the output.")]
    public static string ExecuteShellCommand(
        [Description("The command to execute")] string command,
        [Description("Optional working directory for the command")] string? workdir = null)
    {
        try
        {
            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{command}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            if (!string.IsNullOrEmpty(workdir))
            {
                processInfo.WorkingDirectory = workdir;
            }

            using var process = new System.Diagnostics.Process { StartInfo = processInfo };
            process.Start();

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                return $"Error (Exit Code {process.ExitCode}):\n{error}";
            }

            return string.IsNullOrEmpty(output) ? "(Command executed successfully with no output)" : output;
        }
        catch (Exception ex)
        {
            return $"Error executing command: {ex.Message}";
        }
    }

    [Description("Create a new file or overwrite an existing one with the specified content. Returns success message and path of created/updated file.")]
    public static string CreateFileOrOverwrite(
        [Description("Path where to create/write the file (including filename)")] string filePath,
        [Description("Content/JSON/YAML/text/etc to write into the new or existing file")] string content)
    {
        try
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(filePath, content ?? "");
            return $"Successfully created/overwrote: {filePath}";
        }
        catch (Exception ex)
        {
            return $"Error creating/updating file '{filePath}': {ex.Message}";
        }
    }
}
