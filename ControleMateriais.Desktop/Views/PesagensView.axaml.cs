using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using ControleMateriais.Desktop.ViewModels;
using System.Collections.Generic;

namespace ControleMateriais.Desktop.Views;

public partial class PesagensView : UserControl
{
    private readonly List<Button> _botooesFiltro = new();

    public PesagensView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => ConectarCallbacks();
    }

    private void ConectarCallbacks()
    {
        if (DataContext is PesagensViewModel vm)
            vm.CarregarPesagens();
    }

    private void PesagemRow_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not PesagensViewModel vm) return;
        if (sender is not Border border) return;
        if (border.DataContext is not PesagemItem item) return;

        vm.AbrirReciboCallback?.Invoke(item);
    }

    private void FiltroButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        if (DataContext is not PesagensViewModel vm) return;

        var tag = btn.Tag as string ?? "todos";
        vm.FiltroStatus = tag;

        foreach (var b in _botooesFiltro)
        {
            b.Classes.Remove("ativo");
        }
        btn.Classes.Add("ativo");
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _botooesFiltro.Clear();
        ColetarBotoesFiltro(this);

        // Marca "Todos" como ativo por padrão
        foreach (var b in _botooesFiltro)
        {
            if ((b.Tag as string) == "todos")
            {
                b.Classes.Add("ativo");
                break;
            }
        }
    }

    private void ColetarBotoesFiltro(Avalonia.Visual visual)
    {
        foreach (var child in visual.GetVisualChildren())
        {
            if (child is Button btn && btn.Classes.Contains("filtro"))
                _botooesFiltro.Add(btn);
            else if (child is Avalonia.Visual v)
                ColetarBotoesFiltro(v);
        }
    }

}
