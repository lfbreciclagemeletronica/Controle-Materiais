using Avalonia.Controls;
using Avalonia.Threading;
using ControleMateriais.Desktop.ViewModels;
using System;
using System.Threading.Tasks;

namespace ControleMateriais.Desktop.Views;

public partial class SplashWindow : Window
{
    private readonly SplashViewModel _vm;
    private Action? _onFinished;

    public SplashWindow()
    {
        _vm = new SplashViewModel();
        DataContext = _vm;
        InitializeComponent();
    }

    public void StartAndShow(Action onFinished)
    {
        _onFinished = onFinished;

        // Wire up Continuar button
        var btnContinuar = this.FindControl<Button>("BtnContinuar");
        if (btnContinuar is not null)
            btnContinuar.Click += (_, _) => ContinuarPara();

        // Wire up Configurar button (shown when no credentials)
        var btnConfigurar = this.FindControl<Button>("BtnConfigurar");
        if (btnConfigurar is not null)
            btnConfigurar.Click += async (_, _) =>
            {
                var dialog = new GitHubConfigDialog();
                await dialog.ShowDialog(this);
                // After config, restart sync
                _ = Task.Run(async () =>
                {
                    await Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        await _vm.IniciarSincronizacaoAsync();
                    });
                });
            };

        Show();

        // Run git operations on a background thread — VM dispatches property updates to UI thread
        _ = Task.Run(() => _vm.IniciarSincronizacaoAsync());
    }

    private void ContinuarPara()
    {
        _onFinished?.Invoke();
        Close();
    }
}
