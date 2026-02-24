using Avalonia.Controls;
using ControleMateriais.Desktop.ViewModels;
using System;

namespace ControleMateriais.Desktop.Views;

public partial class PriceTableWindow : Window
{

    private PriceTableViewModel _vm;

    public PriceTableWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);


        // Desassina do VM antigo (se houver)
        if (_vm != null)
            _vm.CloseRequested -= OnCloseRequested;

        // Reassina no VM atual
        _vm = DataContext as PriceTableViewModel;
        if (_vm != null)
            _vm.CloseRequested += OnCloseRequested;

    }
    private void OnCloseRequested(object? sender, EventArgs e)
    {
        Close();
    }



}