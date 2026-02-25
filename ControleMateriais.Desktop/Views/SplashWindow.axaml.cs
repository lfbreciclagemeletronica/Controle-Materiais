using Avalonia.Controls;
using Avalonia.Threading;
using System;

namespace ControleMateriais.Desktop.Views;

public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
    }

    public void StartAndShow(Action onFinished)
    {
        Show();
        DispatcherTimer.RunOnce(() =>
        {
            onFinished();
            Close();
        }, TimeSpan.FromSeconds(2.5));
    }
}
