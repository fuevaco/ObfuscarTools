﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;

namespace ObfuscarTools
{
    public partial class FrmObfuscarSettings : Form
    {
        public FrmObfuscarSettings()
        {
            InitializeComponent();
        }

        internal DotNetProject DotNetProject { get; set; }

        private void FrmObfuscarSettings_Load(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var configs = DotNetProject.GetAllConfigurations();
            foreach (var c in configs)
            {
                var index = dgvConfigurations.Rows.Add();
                dgvConfigurations.Rows[index].Cells[0].Value = c.ConfigurationName;
                dgvConfigurations.Rows[index].Cells[1].Value = c.PlatformName;
                dgvConfigurations.Rows[index].Cells[2].Value = DotNetProject.IsObfuscated(c.ConfigurationName, c.PlatformName);
            }

            var cfg = ObfuscarBaseConfig.Read(DotNetProject.ObfuscarBaseConfigFile);
            lblProjectName.Text = DotNetProject.Name;

            chkRegenerateDebugInfo.Checked = cfg.RegenerateDebugInfo;
            chkMarkedOnly.Checked = cfg.MarkedOnly;
            chkRenameProperties.Checked = cfg.RenameProperties;
            chkRenameEvents.Checked = cfg.RenameEvents;
            chkRenameFields.Checked = cfg.RenameFields;
            chkKeepPublicApi.Checked = cfg.KeepPublicApi;
            chkHidePrivateApi.Checked = cfg.HidePrivateApi;
            chkReuseNames.Checked = cfg.ReuseNames;
            chkHideStrings.Checked = cfg.HideStrings;
            chkOptimizeMethods.Checked = cfg.OptimizeMethods;
            chkSuppressIldasm.Checked = cfg.SuppressIldasm;
            chkAnalyzeXaml.Checked = cfg.AnalyzeXaml;
            chkUseUnicodeNames.Checked = cfg.UseUnicodeNames;
            chkUseKoreanNames.Checked = cfg.UseKoreanNames;

            txtNamespaces.Text = string.Join("\r\n", cfg.NamespacesToSkip);
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            var cfg = ObfuscarBaseConfig.Read(DotNetProject.ObfuscarBaseConfigFile);

            cfg.RegenerateDebugInfo = chkRegenerateDebugInfo.Checked;
            cfg.MarkedOnly = chkMarkedOnly.Checked;
            cfg.RenameProperties = chkRenameProperties.Checked;
            cfg.RenameEvents = chkRenameEvents.Checked;
            cfg.RenameFields = chkRenameFields.Checked;
            cfg.KeepPublicApi = chkKeepPublicApi.Checked;
            cfg.HidePrivateApi = chkHidePrivateApi.Checked;
            cfg.ReuseNames = chkReuseNames.Checked;
            cfg.HideStrings = chkHideStrings.Checked;
            cfg.OptimizeMethods = chkOptimizeMethods.Checked;
            cfg.SuppressIldasm = chkSuppressIldasm.Checked;
            cfg.AnalyzeXaml = chkAnalyzeXaml.Checked;
            cfg.UseUnicodeNames = chkUseUnicodeNames.Checked;
            cfg.UseKoreanNames = chkUseKoreanNames.Checked;


            var lstNamespaces = txtNamespaces.Text.Replace("\r\n", "\n").Split('\n');
            cfg.NamespacesToSkip = new List<string>(lstNamespaces);

            cfg.Write(DotNetProject.ObfuscarBaseConfigFile);


            var hasObfuscarEnabled = false;
            foreach (DataGridViewRow row in dgvConfigurations.Rows)
            {
                var name = row.Cells[0].Value.ToString();
                var platform = row.Cells[1].Value.ToString();
                var enabled = row.Cells[2].Value as bool?;
                if (enabled == null) continue;

                hasObfuscarEnabled |= enabled.Value;
                if (enabled.Value)
                {
                    DotNetProject.AddObfuscarCommand(name, platform);
                }
                else
                {
                    DotNetProject.RemoveObfuscarCommand(name, platform);
                }
            }

            if (hasObfuscarEnabled)
            {
                DotNetProject.AddObfuscarBaseConfig();
            }

            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}