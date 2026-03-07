using System;
using System.IO;
using Microsoft.Win32;

namespace StegoForge.Wpf.Services;

public sealed class FileDialogService : IFileDialogService
{
    public string? SelectCarrierPath(string? initialPath = null)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select carrier file",
            CheckFileExists = true,
            Multiselect = false,
        };

        ApplyInitialPath(dialog, initialPath);
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? SelectPayloadPath(string? initialPath = null)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select payload file",
            CheckFileExists = true,
            Multiselect = false,
        };

        ApplyInitialPath(dialog, initialPath);
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? SelectEmbedOutputPath(string? initialPath = null)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Select embed output file",
            OverwritePrompt = false,
        };

        ApplyInitialPath(dialog, initialPath);
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? SelectExtractOutputPath(string? initialPath = null)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Select extract output path",
            OverwritePrompt = false,
        };

        ApplyInitialPath(dialog, initialPath);
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    private static void ApplyInitialPath(FileDialog dialog, string? initialPath)
    {
        if (string.IsNullOrWhiteSpace(initialPath))
        {
            return;
        }

        var directory = Path.GetDirectoryName(initialPath);
        if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
        {
            dialog.InitialDirectory = directory;
        }

        var name = Path.GetFileName(initialPath);
        if (!string.IsNullOrWhiteSpace(name))
        {
            dialog.FileName = name;
        }
    }
}
