using System;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace ObfuscarTools
{
    internal static class SolutionManager
    {
        internal static Solution GetActiveSolution()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = Package.GetGlobalService(typeof(SDTE)) as DTE;
            return dte.Solution;
        }


        internal static Project GetActiveProject()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = Package.GetGlobalService(typeof(SDTE)) as DTE;

            Project project = null;
            try
            {
                project = dte.ActiveDocument.ProjectItem.ContainingProject;
            }
            catch
            {
            }

            if (project != null) return project;

            var activeSolutionProjects = dte.ActiveSolutionProjects as Array;
            if (activeSolutionProjects != null && activeSolutionProjects.Length > 0)
            {
                project = activeSolutionProjects.GetValue(0) as Project;
            }

            return project;
        }
    }
}