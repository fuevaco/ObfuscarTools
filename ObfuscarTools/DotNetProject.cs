using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using VSLangProj;

namespace ObfuscarTools
{
    internal class DotNetProject
    {
        private ObfuscarToolsPackage package;

        public DotNetProject(ObfuscarToolsPackage pkg, VSProject proj)
        {
            Project = proj;
            package = pkg;
        }

        public VSProject Project { get; }

        public string FileName
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return Project.Project.FullName;
            }
        }

        public string Name
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return Project.Project.Name;
            }
        }

        public string ProjectDir => new FileInfo(FileName).Directory.FullName;
        public string ObfuscarDir => ProjectDir + "\\_Obfuscar";
        public string ObfuscarBaseConfigFile => Path.Combine(ObfuscarDir, "obfuscar.xml");

        public string StartupDir
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return Path.GetDirectoryName(Project.Project.FileName);
            }
        }

        public Configuration ActiveBuildConfig
        {
            get
            {
                try
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    return Project.Project.ConfigurationManager.ActiveConfiguration;
                }
                catch
                {
                    return null;
                }
            }
        }

        public bool IsNetFrameworkProject()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var isFramework = false;
            foreach (Property prop in Project.Project.Properties)
            {
                if (prop.Name == "TargetFrameworkMoniker")
                {
                    var frameworkVersion = prop.Value.ToString();
                    isFramework = frameworkVersion.StartsWith(".NETFramework");
                }
            }

            return isFramework;
        }

        public List<Configuration> GetAllConfigurations()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var rowNames = Project.Project.ConfigurationManager.ConfigurationRowNames as object[];

            var configurations = new List<Configuration>();
            foreach (var row in rowNames)
            {
                var configRow = Project.Project.ConfigurationManager.ConfigurationRow(row.ToString());
                var en = configRow.GetEnumerator();

                while (en.MoveNext())
                {
                    var item = en.Current as Configuration;
                    configurations.Add(item);
                }
            }

            return configurations;
        }

        public Configuration GetConfiguration(string name, string platformName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var rowNames = Project.Project.ConfigurationManager.ConfigurationRowNames as object[];

            foreach (var row in rowNames)
            {
                var configName = row.ToString();
                if (configName != name) continue;

                var configRow = Project.Project.ConfigurationManager.ConfigurationRow(configName);

                var en = configRow.GetEnumerator();

                while (en.MoveNext())
                {
                    var item = en.Current as Configuration;
                    if (item.PlatformName == platformName) return item;
                }
            }

            return null;
        }


        public string GetOutputPath(Configuration configuration)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var objPath = "";
            var binPath = "";

            foreach (Property prop in configuration.Properties)
            {
                if (prop.Name == "IntermediatePath") objPath = prop.Value.ToString();
                if (prop.Name == "OutputPath") binPath = prop.Value.ToString();
            }

            if (IsNetFrameworkProject()) return objPath;
            return binPath;
        }

        public bool IsObfuscated(string buildConfig, string platform)
        {
            var mgr = new AfterCompileManager(Project, IsNetFrameworkProject());
            return mgr.HasAfterCompileTarget(buildConfig, platform);
        }

        public void CopyObfuscar()
        {
            var fileNames = new[] { "Obfuscar.Console.exe" };
            var srcDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\files\\";

            Directory.CreateDirectory(ObfuscarDir);

            foreach (var f in fileNames)
            {
                File.Copy(srcDir + f, ObfuscarDir + "\\" + f, true);
            }
        }

        public List<string> GetProbePaths()
        {
            var ret = new List<string>();
            foreach (Reference r in Project.References)
            {
                var fileName = r.Path;
                if (fileName == null || fileName == "") continue;
                var folderName = new FileInfo(fileName).Directory.FullName;
                if (ret.Contains(folderName) == false) ret.Add(folderName);
            }

            return ret;
        }

        public void WriteBaseConfig(string path)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            ThreadHelper.ThrowIfNotOnUIThread();
            var probePaths = GetProbePaths();
            var solutionDir = new FileInfo(SolutionManager.GetActiveSolution().FullName).Directory.FullName;
            var config = new ObfuscarBaseConfig();
            if (File.Exists(path))
            {
                config = ObfuscarBaseConfig.Read(path);
            }

            var moduleName = Project.Project.Properties.Item("OutputFileName").Value.ToString();
            config.Module = "$(InPath)\\" + moduleName;

            config.AssemblySearchPaths.Clear();
            foreach (var pp in probePaths)
            {
                if (pp.StartsWith(solutionDir))
                {
                    config.AssemblySearchPaths.Add(GetRelativePath(pp, StartupDir));
                }
                else
                {
                    config.AssemblySearchPaths.Add(pp);
                }
            }

            config.Write(path);
        }


        public void WriteConfig(Configuration projectConfig, string path)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var probePaths = GetProbePaths();
            var config = new ObfuscarConfig();
            if (File.Exists(path))
            {
                config = ObfuscarConfig.Read(path);
            }

            var outputPath = GetOutputPath(projectConfig);
            config.InPath = outputPath;
            config.OutPath = outputPath + "\\Out";
            config.BaseConfigPath = GetRelativePath(ObfuscarBaseConfigFile, StartupDir);
            config.Write(path);
        }

        private string GetRelativePath(string filespec, string folder)
        {
            var pathUri = new Uri(filespec);
            // Folders must end in a slash
            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                folder += Path.DirectorySeparatorChar;
            }

            var folderUri = new Uri(folder);
            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }


        public void AddObfuscarBaseConfig()
        {
            CopyObfuscar();
            WriteBaseConfig(ObfuscarBaseConfigFile);
        }

        public void AddObfuscarCommand(string buildConfig, string platform)
        {
            var projectConfig = GetConfiguration(buildConfig, platform);
            if (projectConfig == null) return;

            var configFileName = $"obfuscar_{buildConfig.Replace(" ", "_")}_{platform.Replace(" ", "_")}.xml";
            WriteConfig(projectConfig, Path.Combine(ObfuscarDir, configFileName));

            var mgr = new AfterCompileManager(Project, IsNetFrameworkProject());
            var cmd = new AfterCompileCommand
            {
                PlatformName = platform,
                BuildConfiguration = buildConfig,
                ConfigXmlName = configFileName,
                IntermediatePath = GetOutputPath(projectConfig)
            };

            mgr.AddAfterCompileCommand(cmd);
        }

        public void RemoveObfuscarCommand(string buildConfig, string platform)
        {
            var mgr = new AfterCompileManager(Project, IsNetFrameworkProject());
            mgr.RemoveAfterCompileTarget(buildConfig, platform);
        }
    }
}