using Avalonia.Controls;
using ControleMateriais.Desktop.ViewModels;

namespace ControleMateriais.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}
