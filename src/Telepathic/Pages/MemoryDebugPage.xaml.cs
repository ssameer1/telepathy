namespace Telepathic.Pages;

public partial class MemoryDebugPage : ContentPage
{
    public MemoryDebugPage(MemoryDebugPageModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
