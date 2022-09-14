﻿using CreamInstaller.Components;
using CreamInstaller.Resources;
using CreamInstaller.Utility;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using static CreamInstaller.Paradox.ParadoxLauncher;
using static CreamInstaller.Resources.Resources;

namespace CreamInstaller;

internal partial class InstallForm : CustomForm
{
    internal bool Reselecting;
    internal readonly bool Uninstalling;

    internal InstallForm(IWin32Window owner, bool uninstall = false) : base(owner)
    {
        InitializeComponent();
        Text = Program.ApplicationName;
        logTextBox.BackColor = InstallationLog.Background;
        Uninstalling = uninstall;
    }

    private int OperationsCount;
    private int CompleteOperationsCount;

    internal void UpdateProgress(int progress)
    {
        if (!userProgressBar.Disposing && !userProgressBar.IsDisposed)
            userProgressBar.Invoke(() =>
            {
                int value = (int)((float)CompleteOperationsCount / OperationsCount * 100) + progress / OperationsCount;
                if (value < userProgressBar.Value) return;
                userProgressBar.Value = value;
            });
    }

    internal void UpdateUser(string text, Color color, bool info = true, bool log = true)
    {
        if (info) _ = userInfoLabel.Invoke(() => userInfoLabel.Text = text);
        if (log && !logTextBox.Disposing && !logTextBox.IsDisposed)
        {
            logTextBox.Invoke(() =>
            {
                if (logTextBox.Text.Length > 0) logTextBox.AppendText(Environment.NewLine, color);
                logTextBox.AppendText(text, color);
                logTextBox.Invalidate();
            });
        }
    }

    private async Task OperateFor(ProgramSelection selection)
    {
        UpdateProgress(0);
        if (selection.Id == "PL")
        {
            UpdateUser($"Repairing Paradox Launcher . . . ", InstallationLog.Operation);
            _ = await Repair(this, selection);
        }
        IEnumerable<string> invalidDirectories = (await selection.RootDirectory.GetExecutables())
            .Where(d => !selection.ExecutableDirectories.Any(s => d.path.Contains(s.directory)))
            .Select(d => Path.GetDirectoryName(d.path));
        if (!selection.ExecutableDirectories.Any(s => s.directory == selection.RootDirectory))
            invalidDirectories = invalidDirectories.Append(selection.RootDirectory);
        invalidDirectories = invalidDirectories.Distinct();
        foreach (string directory in invalidDirectories)
        {
            directory.GetKoaloaderComponents(out List<string> proxies, out string config);
            if (proxies.Any(proxy => File.Exists(proxy) && proxy.IsResourceFile(ResourceIdentifier.Koaloader))
                || Koaloader.AutoLoadDlls.Any(pair => File.Exists(directory + @"\" + pair.dll))
                || File.Exists(config))
            {
                UpdateUser("Uninstalling Koaloader from " + selection.Name + $" in incorrect directory \"{directory}\" . . . ", InstallationLog.Operation);
                await Koaloader.Uninstall(directory, this);
            }
            Thread.Sleep(0);
        }
        if (Uninstalling || !selection.Koaloader)
        {
            foreach ((string directory, BinaryType binaryType) in selection.ExecutableDirectories)
            {
                directory.GetKoaloaderComponents(out List<string> proxies, out string config);
                if (proxies.Any(proxy => File.Exists(proxy) && proxy.IsResourceFile(ResourceIdentifier.Koaloader))
                    || Koaloader.AutoLoadDlls.Any(pair => File.Exists(directory + @"\" + pair.dll))
                    || File.Exists(config))
                {
                    UpdateUser("Uninstalling Koaloader from " + selection.Name + $" in directory \"{directory}\" . . . ", InstallationLog.Operation);
                    await Koaloader.Uninstall(directory, this);
                }
            }
        }
        bool uninstallProxy = Uninstalling || selection.Koaloader;
        int count = selection.DllDirectories.Count, cur = 0;
        foreach (string directory in selection.DllDirectories)
        {
            if (selection.Platform is Platform.Steam or Platform.Paradox
                && (selection.SelectedDlc.Any(d => d.Value.type is DlcType.Steam or DlcType.SteamHidden)
                    || selection.ExtraSelectedDlc.Any(item => item.dlc.Any(dlc => dlc.Value.type is DlcType.Steam or DlcType.SteamHidden))))
            {
                directory.GetSmokeApiComponents(out string api32, out string api32_o, out string api64, out string api64_o, out string config, out string cache);
                if (!uninstallProxy && (File.Exists(api32) || File.Exists(api64))
                    || uninstallProxy && (File.Exists(api32_o) || File.Exists(api64_o)
                        || !selection.Koaloader && (File.Exists(config) || File.Exists(cache))))
                {
                    UpdateUser($"{(uninstallProxy ? "Uninstalling" : "Installing")} SmokeAPI" +
                        $" {(uninstallProxy ? "from" : "for")} " + selection.Name + $" in directory \"{directory}\" . . . ", InstallationLog.Operation);
                    if (uninstallProxy)
                        await SmokeAPI.Uninstall(directory, this);
                    else
                        await SmokeAPI.Install(directory, selection, this);
                }
            }
            if (selection.Platform is Platform.Epic or Platform.Paradox
                && (selection.SelectedDlc.Any(d => d.Value.type is DlcType.EpicCatalogItem or DlcType.EpicEntitlement)
                    || selection.ExtraSelectedDlc.Any(item => item.dlc.Any(dlc => dlc.Value.type is DlcType.EpicCatalogItem or DlcType.EpicEntitlement))))
            {
                directory.GetScreamApiComponents(out string api32, out string api32_o, out string api64, out string api64_o, out string config);
                if (!uninstallProxy && (File.Exists(api32) || File.Exists(api64))
                    || uninstallProxy && (File.Exists(api32_o) || File.Exists(api64_o)
                        || !selection.Koaloader && File.Exists(config)))
                {
                    UpdateUser($"{(uninstallProxy ? "Uninstalling" : "Installing")} ScreamAPI" +
                        $" {(uninstallProxy ? "from" : "for")} " + selection.Name + $" in directory \"{directory}\" . . . ", InstallationLog.Operation);
                    if (uninstallProxy)
                        await ScreamAPI.Uninstall(directory, this);
                    else
                        await ScreamAPI.Install(directory, selection, this);
                }
            }
            if (selection.Platform is Platform.Ubisoft)
            {
                directory.GetUplayR1Components(out string api32, out string api32_o, out string api64, out string api64_o, out string config);
                if (!uninstallProxy && (File.Exists(api32) || File.Exists(api64))
                    || uninstallProxy && (File.Exists(api32_o) || File.Exists(api64_o)
                        || !selection.Koaloader && File.Exists(config)))
                {
                    UpdateUser($"{(uninstallProxy ? "Uninstalling" : "Installing")} Uplay R1 Unlocker" +
                        $" {(uninstallProxy ? "from" : "for")} " + selection.Name + $" in directory \"{directory}\" . . . ", InstallationLog.Operation);
                    if (uninstallProxy)
                        await UplayR1.Uninstall(directory, this);
                    else
                        await UplayR1.Install(directory, selection, this);
                }
                directory.GetUplayR2Components(out string old_api32, out string old_api64, out api32, out api32_o, out api64, out api64_o, out config);
                if (!uninstallProxy && (File.Exists(old_api32) || File.Exists(old_api64) || File.Exists(api32) || File.Exists(api64))
                    || uninstallProxy && (File.Exists(api32_o) || File.Exists(api64_o)
                        || !selection.Koaloader && File.Exists(config)))
                {
                    UpdateUser($"{(uninstallProxy ? "Uninstalling" : "Installing")} Uplay R2 Unlocker" +
                        $" {(uninstallProxy ? "from" : "for")} " + selection.Name + $" in directory \"{directory}\" . . . ", InstallationLog.Operation);
                    if (uninstallProxy)
                        await UplayR2.Uninstall(directory, this);
                    else
                        await UplayR2.Install(directory, selection, this);
                }
            }
            UpdateProgress(++cur / count * 100);
        }
        if (selection.Koaloader && !Uninstalling)
        {
            foreach ((string directory, BinaryType binaryType) in selection.ExecutableDirectories)
            {
                UpdateUser("Installing Koaloader to " + selection.Name + $" in directory \"{directory}\" . . . ", InstallationLog.Operation);
                await Koaloader.Install(directory, binaryType, selection, this);
            }
        }
        UpdateProgress(100);
    }

    private readonly List<ProgramSelection> DisabledSelections = new();

    private async Task Operate()
    {
        List<ProgramSelection> programSelections = ProgramSelection.AllEnabled;
        OperationsCount = programSelections.Count;
        CompleteOperationsCount = 0;
        foreach (ProgramSelection selection in programSelections)
        {
            if (Program.Canceled || !Program.IsProgramRunningDialog(this, selection)) throw new CustomMessageException("The operation was canceled.");
            try
            {
                await OperateFor(selection);
                UpdateUser($"Operation succeeded for {selection.Name}.", InstallationLog.Success);
                selection.Enabled = false;
                DisabledSelections.Add(selection);
            }
            catch (Exception exception)
            {
                UpdateUser($"Operation failed for {selection.Name}: " + exception, InstallationLog.Error);
            }
            ++CompleteOperationsCount;
        }
        Program.Cleanup();
        List<ProgramSelection> FailedSelections = ProgramSelection.AllEnabled;
        if (FailedSelections.Any())
            if (FailedSelections.Count == 1)
                throw new CustomMessageException($"Operation failed for {FailedSelections.First().Name}.");
            else
                throw new CustomMessageException($"Operation failed for {FailedSelections.Count} programs.");
        foreach (ProgramSelection selection in DisabledSelections)
            selection.Enabled = true;
        DisabledSelections.Clear();
    }

    private readonly int ProgramCount = ProgramSelection.AllEnabled.Count;

    private async void Start()
    {
        Program.Canceled = false;
        acceptButton.Enabled = false;
        retryButton.Enabled = false;
        cancelButton.Enabled = true;
        reselectButton.Enabled = false;
        userProgressBar.Value = userProgressBar.Minimum;
        try
        {
            await Operate();
            UpdateUser($"DLC unlocker(s) successfully {(Uninstalling ? "uninstalled" : "installed and generated")} for " + ProgramCount + " program(s).", InstallationLog.Success);
        }
        catch (Exception exception)
        {
            UpdateUser($"DLC unlocker {(Uninstalling ? "uninstallation" : "installation and/or generation")} failed: " + exception, InstallationLog.Error);
            retryButton.Enabled = true;
        }
        userProgressBar.Value = userProgressBar.Maximum;
        acceptButton.Enabled = true;
        cancelButton.Enabled = false;
        reselectButton.Enabled = true;
    }

    private void OnLoad(object sender, EventArgs _)
    {
        retry:
        try
        {
            userInfoLabel.Text = "Loading . . . ";
            logTextBox.Text = string.Empty;
            Start();
        }
        catch (Exception e)
        {
            if (e.HandleException(form: this)) goto retry;
            Close();
        }
    }

    private void OnAccept(object sender, EventArgs e)
    {
        Program.Cleanup();
        Close();
    }

    private void OnRetry(object sender, EventArgs e)
    {
        Program.Cleanup();
        Start();
    }

    private void OnCancel(object sender, EventArgs e) => Program.Cleanup();

    private void OnReselect(object sender, EventArgs e)
    {
        Program.Cleanup();
        Reselecting = true;
        foreach (ProgramSelection selection in DisabledSelections)
            selection.Enabled = true;
        DisabledSelections.Clear();
        Close();
    }
}
