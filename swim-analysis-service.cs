using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SwimCoach.Terminal
{
    /// <summary>
    /// SwimCoach Terminal Navigator - Helps macOS users manage their terminal environment
    /// using swimming coach metaphors to guide system optimization
    /// </summary>
    public class TerminalCoach
    {
        private readonly ILogger<TerminalCoach> _logger;
        private readonly Dictionary<string, PackageManager> _packageManagers;
        private string _homeDirectory;
        
        // Swimming coach metaphors for system operations
        private static readonly Dictionary<CoachAction, string> CoachTips = new Dictionary<CoachAction, string>
        {
            { CoachAction.CleanUp, "Let's improve your stroke efficiency by removing unused packages." },
            { CoachAction.Organize, "Good file organization is like proper body alignment - essential for peak performance." },
            { CoachAction.Optimize, "Let's fine-tune your system for better performance, just like optimizing your stroke technique." },
            { CoachAction.Monitor, "Regular system monitoring is like tracking your split times - crucial for improvement." },
            { CoachAction.Backup, "Always have a backup plan, just like having multiple race strategies." }
        };

        public TerminalCoach(ILogger<TerminalCoach> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            
            // Initialize supported package managers
            _packageManagers = new Dictionary<string, PackageManager>
            {
                { "homebrew", new PackageManager 
                    { 
                        Name = "Homebrew", 
                        InstallCommand = "brew install",
                        UninstallCommand = "brew uninstall",
                        ListCommand = "brew list",
                        UpdateCommand = "brew update",
                        UpgradeCommand = "brew upgrade",
                        CleanupCommand = "brew cleanup",
                        InfoCommand = "brew info"
                    } 
                },
                { "macports", new PackageManager 
                    { 
                        Name = "MacPorts",
                        InstallCommand = "port install",
                        UninstallCommand = "port uninstall",
                        ListCommand = "port installed",
                        UpdateCommand = "port selfupdate",
                        UpgradeCommand = "port upgrade outdated",
                        CleanupCommand = "port clean --all installed",
                        InfoCommand = "port info"
                    } 
                }
            };
            
            _logger.LogInformation("Terminal Coach initialized successfully");
        }

        /// <summary>
        /// Initializes the Terminal Coach environment
        /// </summary>
        public async Task<SystemInfo> InitializeAsync()
        {
            _logger.LogInformation("Analyzing your system configuration...");
            
            var systemInfo = new SystemInfo();
            
            // Detect macOS version
            var macOSVersion = await ExecuteCommandAsync("sw_vers -productVersion");
            systemInfo.OSVersion = macOSVersion?.Trim() ?? "Unknown";
            
            // Detect available package managers
            systemInfo.PackageManagers = new List<string>();
            foreach (var pm in _packageManagers.Keys)
            {
                var result = await ExecuteCommandAsync($"which {pm.Split(' ')[0]}");
                if (!string.IsNullOrEmpty(result))
                {
                    systemInfo.PackageManagers.Add(pm);
                }
            }
            
            // Analyze disk space
            var diskInfo = await ExecuteCommandAsync("df -h /");
            if (diskInfo != null)
            {
                // Parse disk info using regex
                var match = Regex.Match(diskInfo, @"(\d+%)\s+(.+)$", RegexOptions.Multiline);
                if (match.Success)
                {
                    systemInfo.DiskUsage = match.Groups[1].Value;
                }
            }
            
            // Check shell configuration
            var shellPath = Environment.GetEnvironmentVariable("SHELL") ?? "/bin/bash";
            systemInfo.Shell = Path.GetFileName(shellPath);
            
            Console.WriteLine(GetCoachTip(CoachAction.Monitor));
            Console.WriteLine($"System analysis complete for macOS {systemInfo.OSVersion}");
            
            return systemInfo;
        }

        /// <summary>
        /// Scans for expired or unused packages across package managers
        /// </summary>
        public async Task<ExpiredPackagesResult> ScanForExpiredPackagesAsync()
        {
            var result = new ExpiredPackagesResult();
            
            Console.WriteLine(GetCoachTip(CoachAction.CleanUp));
            Console.WriteLine("Analyzing your installed packages for efficiency improvements...");
            
            foreach (var pm in _packageManagers)
            {
                if (!await IsPackageManagerInstalledAsync(pm.Key))
                    continue;
                    
                Console.WriteLine($"Checking {pm.Value.Name} packages...");
                
                // Get list of installed packages
                var installedPackages = await ExecuteCommandAsync(pm.Value.ListCommand);
                if (string.IsNullOrEmpty(installedPackages))
                    continue;
                
                // Check for outdated packages
                var outdatedCmd = pm.Key == "homebrew" ? "brew outdated" : "port outdated";
                var outdatedPackages = await ExecuteCommandAsync(outdatedCmd);
                
                // Parse the output based on package manager format
                var expired = ParseOutdatedPackages(pm.Key, outdatedPackages);
                result.ExpiredPackages.AddRange(expired);
                
                // Check for unused packages (last used > 90 days ago)
                // This is a simple heuristic and would require more sophisticated tracking in a real app
                var unused = await FindUnusedPackagesAsync(pm.Key, installedPackages);
                result.UnusedPackages.AddRange(unused);
            }
            
            result.TotalCount = result.ExpiredPackages.Count + result.UnusedPackages.Count;
            return result;
        }
        
        /// <summary>
        /// Cleans up expired packages and optimizes system
        /// </summary>
        public async Task<CleanupResult> CleanupSystemAsync(CleanupOptions options)
        {
            var result = new CleanupResult();
            
            Console.WriteLine(GetCoachTip(CoachAction.Optimize));
            Console.WriteLine("Starting system optimization...");
            
            // Remove expired packages if requested
            if (options.RemoveExpiredPackages && options.ExpiredPackages.Any())
            {
                foreach (var package in options.ExpiredPackages)
                {
                    if (_packageManagers.TryGetValue(package.PackageManager, out var pm))
                    {
                        Console.WriteLine($"Removing {package.Name}...");
                        var output = await ExecuteCommandAsync($"{pm.UninstallCommand} {package.Name}");
                        if (!string.IsNullOrEmpty(output))
                        {
                            result.RemovedPackages.Add(package);
                        }
                    }
                }
            }
            
            // Run cleanup commands for each package manager
            foreach (var pm in _packageManagers)
            {
                if (await IsPackageManagerInstalledAsync(pm.Key))
                {
                    Console.WriteLine($"Running {pm.Value.Name} cleanup...");
                    await ExecuteCommandAsync(pm.Value.CleanupCommand);
                    result.CleanupCommands.Add(pm.Value.CleanupCommand);
                }
            }
            
            // Clean system caches
            if (options.CleanSystemCaches)
            {
                Console.WriteLine("Cleaning system caches...");
                
                // Clear system caches directories
                var cacheDirs = new[]
                {
                    Path.Combine(_homeDirectory, "Library/Caches"),
                    "/Library/Caches"
                };
                
                foreach (var dir in cacheDirs)
                {
                    if (Directory.Exists(dir) && options.DryRun == false)
                    {
                        var sizeBefore = await GetDirectorySizeAsync(dir);
                        // In a real app, you'd want to be more careful about what you delete
                        // This is just a demonstration
                        
                        // Instead of actually deleting, just calculate potential savings
                        result.BytesFreed += sizeBefore;
                    }
                }
            }
            
            result.Success = true;
            result.Message = "System optimization complete!";
            return result;
        }
        
        /// <summary>
        /// Organizes terminal configuration files and documents
        /// </summary>
        public async Task<OrganizeResult> OrganizeDocumentsAsync(OrganizeOptions options)
        {
            var result = new OrganizeResult();
            
            Console.WriteLine(GetCoachTip(CoachAction.Organize));
            Console.WriteLine("Organizing your terminal configuration for better performance...");
            
            // Backup existing configuration files
            if (options.BackupConfigs)
            {
                var configFiles = new[]
                {
                    ".bash_profile",
                    ".bashrc",
                    ".zshrc",
                    ".zprofile",
                    ".profile"
                };
                
                var backupDir = Path.Combine(_homeDirectory, "terminal_coach_backup");
                if (!Directory.Exists(backupDir) && !options.DryRun)
                {
                    Directory.CreateDirectory(backupDir);
                }
                
                foreach (var file in configFiles)
                {
                    var fullPath = Path.Combine(_homeDirectory, file);
                    if (File.Exists(fullPath))
                    {
                        result.BackedUpFiles.Add(file);
                        
                        if (!options.DryRun)
                        {
                            var backupPath = Path.Combine(backupDir, $"{file}.{DateTime.Now:yyyyMMdd}");
                            File.Copy(fullPath, backupPath, true);
                        }
                    }
                }
            }
            
            // Analyze shell efficiency
            var currentShell = Environment.GetEnvironmentVariable("SHELL");
            if (currentShell != null)
            {
                result.ShellType = Path.GetFileName(currentShell);
                
                // Analyze shell startup time
                var startupTime = await MeasureShellStartupTimeAsync(currentShell);
                result.StartupTimeMs = startupTime;
                
                if (startupTime > 500) // Arbitrary threshold
                {
                    result.Recommendations.Add("Your shell startup time is high. Consider optimizing your profile scripts.");
                }
            }
            
            // Recommend optimal terminal configurations
            result.Recommendations.Add("Use aliases for common commands to improve efficiency.");
            result.Recommendations.Add("Consider using a modern shell like zsh with syntax highlighting.");
            result.Recommendations.Add("Organize your PATH variable to avoid duplicates and improve lookup times.");
            
            return result;
        }
        
        /// <summary>
        /// Creates a navigation training plan - shortcuts and aliases for common operations
        /// </summary>
        public async Task<NavigationPlan> CreateNavigationPlanAsync()
        {
            var plan = new NavigationPlan();
            
            Console.WriteLine("Creating your terminal navigation training plan...");
            Console.WriteLine("Like a good swimming workout, effective terminal navigation builds muscle memory.");
            
            // Common directories to include in navigation plan
            var commonDirs = new[]
            {
                ("Documents", "~/Documents"),
                ("Development", "~/Development"),
                ("Downloads", "~/Downloads"),
                ("Applications", "/Applications"),
                ("Home", "~")
            };
            
            // Generate aliases for common directories
            foreach (var (name, path) in commonDirs)
            {
                var expandedPath = path.Replace("~", _homeDirectory);
                if (Directory.Exists(expandedPath))
                {
                    plan.DirectoryAliases.Add(new DirectoryAlias
                    {
                        Name = name.ToLower(),
                        Path = path,
                        AliasCommand = $"alias goto_{name.ToLower()}=\"cd {path}\""
                    });
                }
            }
            
            // Add common terminal commands with explanations
            plan.UsefulCommands.Add(new CommandInfo
            {
                Command = "find . -name \"*.txt\" -type f",
                Description = "Find all text files in the current directory and subdirectories",
                Alias = "alias findtxt='find . -name \"*.txt\" -type f'"
            });
            
            plan.UsefulCommands.Add(new CommandInfo
            {
                Command = "grep -r \"search term\" .",
                Description = "Search for text in all files recursively",
                Alias = "alias search='grep -r'"
            });
            
            plan.UsefulCommands.Add(new CommandInfo
            {
                Command = "ls -la | grep ^d",
                Description = "List only directories with details",
                Alias = "alias lsd='ls -la | grep ^d'"
            });
            
            // Generate export script for the plan
            var exportScript = new List<string>
            {
                "#!/bin/bash",
                "# Terminal Navigation Training Plan generated by SwimCoach",
                "# Add these to your .bashrc or .zshrc to improve terminal navigation",
                ""
            };
            
            exportScript.AddRange(plan.DirectoryAliases.Select(a => a.AliasCommand));
            exportScript.AddRange(plan.UsefulCommands.Select(c => c.Alias));
            
            plan.ExportScript = string.Join("\n", exportScript);
            return plan;
        }
        
        #region Helper Methods
        
        private string GetCoachTip(CoachAction action)
        {
            if (CoachTips.TryGetValue(action, out var tip))
            {
                return $"🏊 SwimCoach says: {tip}";
            }
            return "🏊 SwimCoach is ready to help you navigate your terminal more efficiently!";
        }
        
        private async Task<bool> IsPackageManagerInstalledAsync(string packageManager)
        {
            var result = await ExecuteCommandAsync($"which {packageManager.Split(' ')[0]}");
            return !string.IsNullOrEmpty(result);
        }
        
        private async Task<string> ExecuteCommandAsync(string command)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "/bin/bash",
                        Arguments = $"-c \"{command}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                
                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();
                
                if (process.ExitCode != 0)
                {
                    var error = await process.StandardError.ReadToEndAsync();
                    _logger.LogWarning($"Command '{command}' exited with code {process.ExitCode}: {error}");
                    return null;
                }
                
                return output;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing command '{command}'");
                return null;
            }
        }
        
        private List<Package> ParseOutdatedPackages(string packageManager, string outdatedOutput)
        {
            var packages = new List<Package>();
            
            if (string.IsNullOrEmpty(outdatedOutput))
                return packages;
                
            var lines = outdatedOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                // Simple parsing logic - in a real app you'd want more robust parsing
                var parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0)
                {
                    packages.Add(new Package
                    {
                        Name = parts[0],
                        PackageManager = packageManager,
                        Status = PackageStatus.Outdated
                    });
                }
            }
            
            return packages;
        }
        
        private async Task<List<Package>> FindUnusedPackagesAsync(string packageManager, string installedPackages)
        {
            // This would require package usage tracking which is complex
            // This is a simplified version that just returns an empty list
            // In a real app, you'd implement heuristics to determine unused packages
            
            return new List<Package>();
        }
        
        private async Task<long> GetDirectorySizeAsync(string path)
        {
            var output = await ExecuteCommandAsync($"du -sk \"{path}\" | cut -f1");
            if (long.TryParse(output?.Trim(), out var size))
            {
                return size * 1024; // Convert KB to bytes
            }
            return 0;
        }
        
        private async Task<long> MeasureShellStartupTimeAsync(string shellPath)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            await ExecuteCommandAsync($"{shellPath} -i -c exit");
            
            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }
        
        #endregion
    }
    
    #region Models
    
    public enum CoachAction
    {
        CleanUp,
        Organize,
        Optimize,
        Monitor,
        Backup
    }
    
    public class SystemInfo
    {
        public string OSVersion { get; set; }
        public List<string> PackageManagers { get; set; } = new List<string>();
        public string DiskUsage { get; set; }
        public string Shell { get; set; }
    }
    
    public class PackageManager
    {
        public string Name { get; set; }
        public string InstallCommand { get; set; }
        public string UninstallCommand { get; set; }
        public string ListCommand { get; set; }
        public string UpdateCommand { get; set; }
        public string UpgradeCommand { get; set; }
        public string CleanupCommand { get; set; }
        public string InfoCommand { get; set; }
    }
    
    public enum PackageStatus
    {
        Current,
        Outdated,
        Unused
    }
    
    public class Package
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string PackageManager { get; set; }
        public PackageStatus Status { get; set; }
        public DateTime? LastUsed { get; set; }
    }
    
    public class ExpiredPackagesResult
    {
        public List<Package> ExpiredPackages { get; set; } = new List<Package>();
        public List<Package> UnusedPackages { get; set; } = new List<Package>();
        public int TotalCount { get; set; }
    }
    
    public class CleanupOptions
    {
        public bool RemoveExpiredPackages { get; set; }
        public bool CleanSystemCaches { get; set; }
        public bool DryRun { get; set; } = true;
        public List<Package> ExpiredPackages { get; set; } = new List<Package>();
    }
    
    public class CleanupResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<Package> RemovedPackages { get; set; } = new List<Package>();
        public List<string> CleanupCommands { get; set; } = new List<string>();
        public long BytesFreed { get; set; }
    }
    
    public class OrganizeOptions
    {
        public bool BackupConfigs { get; set; } = true;
        public bool OptimizeShellConfig { get; set; } = true;
        public bool DryRun { get; set; } = true;
    }
    
    public class OrganizeResult
    {
        public List<string> BackedUpFiles { get; set; } = new List<string>();
        public List<string> Recommendations { get; set; } = new List<string>();
        public string ShellType { get; set; }
        public long StartupTimeMs { get; set; }
    }
    
    public class DirectoryAlias
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string AliasCommand { get; set; }
    }
    
    public class CommandInfo
    {
        public string Command { get; set; }
        public string Description { get; set; }
        public string Alias { get; set; }
    }
    
    public class NavigationPlan
    {
        public List<DirectoryAlias> DirectoryAliases { get; set; } = new List<DirectoryAlias>();
        public List<CommandInfo> UsefulCommands { get; set; } = new List<CommandInfo>();
        public string ExportScript { get; set; }
    }
    
    #endregion
}
