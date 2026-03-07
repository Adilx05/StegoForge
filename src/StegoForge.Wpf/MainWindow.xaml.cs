using System.Windows;
using StegoForge.Wpf.ViewModels;

namespace StegoForge.Wpf;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
