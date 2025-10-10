using Telepathic.PageModels;

namespace Telepathic.Pages;

public partial class MyDataPage : ContentPage
{
    public MyDataPage(MyDataPageModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
