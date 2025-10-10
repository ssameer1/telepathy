using Telepathic.Models;
using Telepathic.PageModels;

namespace Telepathic.Pages;

public partial class UserProfilePage : ContentPage
{
    public UserProfilePage(UserProfilePageModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private void CalendarCheckBox_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (BindingContext is UserProfilePageModel viewModel)
        {
            var checkBox = (CheckBox)sender;
            var calendar = (CalendarInfo)checkBox.BindingContext;
            viewModel.OnCalendarSelectionChanged(calendar, e.Value);
        }
    }
}
