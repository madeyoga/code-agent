using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace DotNuxt.Tools;

public static class AgentTools
{
    // ---- Allowed categories ----

    private static readonly string[] _allowedCategories = ["build", "files", "git", "system", "network", "text"];

    /// <summary>
    /// Each category maps to allowed executable basenames (lower-cased, no extension).
    /// Includes both cross-platform aliases and their platform-native targets.
    /// </summary>
    private static readonly Dictionary<string, HashSet<string>> _allowedExecutables = new(StringComparer.OrdinalIgnoreCase)
    {
        ["build"] = ["dotnet", "npm", "npx", "make", "cargo", "go", "gradle", "mvn"],
        ["files"] = ["ls", "dir", "cat", "type", "mkdir", "md", "cp", "copy", "mv", "move", "rm", "del", "pwd"],
        ["git"]   = ["git"],
        ["system"]= ["ps", "tasklist", "uname", "whoami", "id", "chmod", "chown", "which", "where"],
        ["network"]  = ["curl", "wget", "ping", "nslookup", "dig"],
        ["text"]   = ["grep", "findstr", "wc", "sort", "uniq", "head", "tail"],
    };

    // ---- Cross-platform alias map (Windows → native cmd.exe equivalents) ----

    private static readonly string[] _winAliasPairs =
    [
        "ls|dir",
        "cat|type",
        "pwd|cd",
        "cp|copy",
        "mv|move",
        "rm|del",
        "grep|findstr"
    ];

    // ---- Agent-facing tool methods ----

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

    // -------------------------------------------------------------------------
    // ExecuteShellCommand — allowlist + cross-platform aliases
    // -------------------------------------------------------------------------
    [Description("Execute a shell command in the current directory and return the output. Always returns the platform (Windows/Linux/macOS) so you know what kind of commands to use. Supports common cross-platform aliases: ls/dir, cat/type, cp/copy, mv/move, rm/del, pwd/cd, grep/findstr. Shell syntax (|, &&, ||, >) is allowed when all executables are in the allowlist.")]
    public static string ExecuteShellCommand(
        [Description("The command to execute")] string command,
        [Description("Optional working directory for the command")] string? workdir = null)
    {
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        // Step 1 — resolve cross-platform aliases (only meaningful on Windows where cmd.exe doesn't know ls/cat/grep etc.)
        var resolved = command;
        if (isWindows)
            foreach (var pair in _winAliasPairs)
            {
                var from = pair.Split('|')[0];
                var to   = pair.Split('|')[1];
                // Replace whole-word occurrences only (\b = word boundary, \b ensures we don't replace "ls" inside "filels")
                resolved = Regex.Replace(resolved, $@"\b{Regex.Escape(from)}\b", to, RegexOptions.IgnoreCase);
            }

        // Step 2 — extract executables from all segments (piped/&&/||/; joined) and validate each against the allowlist
        var execNames = ShellExecExtractor.Extract(resolved);
        var denied   = new List<string>();

        foreach (var name in execNames)
        {
            var baseName = Path.GetFileNameWithoutExtension(name).ToLowerInvariant();
            if (!IsAllowed(baseName))
                denied.Add($"{name}");
        }

        if (denied.Any())
        {
            var report = new List<string> { "Command denied:" };
            foreach (var e in denied) report.Add($"  DENIED: {e}");
            report.Add("");
            report.Add("Allowed categories:");
            report.Add("  build     - dotnet, npm, npx, make, cargo, go, gradle, mvn");
            report.Add("  files     - ls/dir, cat/type, mkdir/md, cp/copy, mv/move, rm/del, pwd/cd");
            report.Add("  git       - git");
            report.Add("  system    - ps/tasklist, uname/whoami/id, chmod/chown, which/where");
            report.Add("  network   - curl/wget, ping, nslookup/dig");
            report.Add("  text      - grep/findstr, wc, sort, uniq, head, tail");
            return string.Join("\n", report);
        }

        // Step 3 — execute
        var platformName = isWindows ? "Windows"
            : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "macOS"
            : "Linux";
        var arch = RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant();

        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName   = isWindows ? "cmd.exe" : "/bin/bash",
                Arguments  = isWindows ? $"/c \"{resolved}\"" : $"-c \"{resolved} 2>&1\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute        = false,
                CreateNoWindow         = true,
            };

            if (!string.IsNullOrEmpty(workdir))
                psi.WorkingDirectory = workdir;

            using var p = new System.Diagnostics.Process { StartInfo = psi };
            p.Start();
            var output = p.StandardOutput.ReadToEnd();
            var error  = p.StandardError.ReadToEnd();
            p.WaitForExit();
            if (p.ExitCode != 0)
                return $"Platform: {platformName} ({arch})\nExited with code {p.ExitCode}\n{error.Trim()}";

            var trimmed = output.Trim();
            return $"Platform: {platformName} ({arch})\n{(string.IsNullOrEmpty(trimmed) ? "(Command executed successfully with no output)" : trimmed)}";
        }
        catch (Exception ex)
        {
            return $"Platform: {platformName} ({arch})\nError executing command: {ex.Message}";
        }

        // ---- local helper ----
        bool IsAllowed(string baseName)
        {
            foreach (var cat in _allowedCategories)
                if (_allowedExecutables.TryGetValue(cat, out var set) && set.Contains(baseName))
                    return true;
            return false;
        }
    }

    // -------------------------------------------------------------------------

    [Description("Create a new file or overwrite an existing one with the specified content. Returns success message and path of created/updated file.")]
    public static string CreateFileOrOverwrite(
        [Description("Path where to create/write the file (including filename)")] string filePath,
        [Description("Code string to write into the new or existing file. literal file contents. Do not JSON-escape, C#-escape, or Python-escape the file contents. Pass the exact bytes that should appear in the file.")] string content)
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

// ========================================================================
// ShellExecExtractor — extracts executable basenames from complex shell
// expressions (pipes, &&, ||, semicolons, redirections).
// ========================================================================
public static partial class ShellExecExtractor
{
    public static List<string> Extract(string command)
    {
        // Split on shell operators while preserving them so we can ignore later.
        var parts = MyRegex().Split(command);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            // Skip pure operator segments
            if (trimmed == "|" || trimmed == "&&" || trimmed == "||" || MyRegex1().IsMatch(trimmed))
                continue;

            var firstToken = FirstExecutable(token: trimmed);
            if (!string.IsNullOrEmpty(firstToken))
                names.Add(firstToken);
        }

        return names.ToList();

        string? FirstExecutable(string token)
        {
            // Strip subshell wrappers: $(...) or `...`
            if (token.StartsWith("$(") && token.EndsWith(")") && token.Length > 2)
                token = token.Substring(2, token.Length - 3).Trim();
            if (token.StartsWith("`") && token.EndsWith("`") && token.Length > 1)
                token = token.Substring(1, token.Length - 2).Trim();

            // Handle `>file` / `>>file` redirects: skip if it starts with > or <
            if (token.StartsWith(">", StringComparison.Ordinal) ||
                token.StartsWith("<", StringComparison.Ordinal))
                return null;

            // Split on whitespace to get the first real word
            var words = token.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
                return null;

            var w = words[0].Trim('"', '\'');
            return w.StartsWith("-") ? null : w; // skip flags like -r --force
        }
    }

    [GeneratedRegex(@"(\|\||&&|>>?|\||\s*;\s*)")]
    private static partial Regex MyRegex();
    [GeneratedRegex(@"^>>?$")]
    private static partial Regex MyRegex1();
}
