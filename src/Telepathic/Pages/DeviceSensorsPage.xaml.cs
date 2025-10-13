using Telepathic.PageModels;

namespace Telepathic.Pages;

public partial class DeviceSensorsPage : ContentPage
{
    private readonly DeviceSensorsPageModel _viewModel;

    public DeviceSensorsPage(DeviceSensorsPageModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.Dispose();
    }
}
