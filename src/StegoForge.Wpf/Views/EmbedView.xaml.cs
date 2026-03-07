using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using StegoForge.Wpf.ViewModels;

namespace StegoForge.Wpf.Views;

public partial class EmbedView : UserControl
{
    public EmbedView()
    {
        InitializeComponent();
    }

    private void CarrierPathTextBox_OnPreviewDragOver(object sender, DragEventArgs e)
    {
        e.Handled = SetDropEffects(e);
    }

    private void CarrierPathTextBox_OnDrop(object sender, DragEventArgs e)
    {
        e.Handled = TryApplyDroppedPath(static (vm, path) => vm.TryApplyDroppedCarrierPath(path), e);
    }

    private void PayloadPathTextBox_OnPreviewDragOver(object sender, DragEventArgs e)
    {
        e.Handled = SetDropEffects(e);
    }

    private void PayloadPathTextBox_OnDrop(object sender, DragEventArgs e)
    {
        e.Handled = TryApplyDroppedPath(static (vm, path) => vm.TryApplyDroppedPayloadPath(path), e);
    }

    private void OutputPathTextBox_OnPreviewDragOver(object sender, DragEventArgs e)
    {
        e.Handled = SetDropEffects(e);
    }

    private void OutputPathTextBox_OnDrop(object sender, DragEventArgs e)
    {
        e.Handled = TryApplyDroppedPath(static (vm, path) => vm.TryApplyDroppedOutputPath(path), e);
    }

    private static bool SetDropEffects(DragEventArgs e)
    {
        e.Effects = TryGetSingleDroppedPath(e, out _) ? DragDropEffects.Copy : DragDropEffects.None;
        return true;
    }

    private bool TryApplyDroppedPath(Func<EmbedViewModel, string?, bool> apply, DragEventArgs e)
    {
        if (DataContext is not EmbedViewModel vm)
        {
            return false;
        }

        _ = TryGetSingleDroppedPath(e, out var path);
        return apply(vm, path);
    }

    private static bool TryGetSingleDroppedPath(DragEventArgs e, out string? path)
    {
        path = null;

        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            return false;
        }

        if (e.Data.GetData(DataFormats.FileDrop) is not string[] droppedPaths || droppedPaths.Length != 1)
        {
            return false;
        }

        path = droppedPaths.SingleOrDefault();
        return !string.IsNullOrWhiteSpace(path);
    }
}
