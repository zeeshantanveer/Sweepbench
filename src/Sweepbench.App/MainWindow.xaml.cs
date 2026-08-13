using System.Windows;
using Sweepbench.App.ViewModels;

namespace Sweepbench.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new ShellViewModel();
    }
}
