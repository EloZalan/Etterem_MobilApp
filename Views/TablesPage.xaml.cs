using WaiterApp.ViewModels;

namespace WaiterApp.Views;

public partial class TablesPage : ContentPage
{
    private readonly TablesViewModel _viewModel;

    public TablesPage(TablesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }

    private async void OnWalkInReservationClicked(object sender, EventArgs e)
    {
        var input = await DisplayPromptAsync(
            "Walk-in reservation",
            "Enter guest count:",
            accept: "OK",
            cancel: "Cancel",
            keyboard: Keyboard.Numeric);

        if (string.IsNullOrWhiteSpace(input))
            return;

        if (!int.TryParse(input, out var guestCount) || guestCount < 1)
        {
            await DisplayAlert("Invalid value", "Guest count must be a number greater than 0.", "OK");
            return;
        }

        await _viewModel.CreateWalkInReservationAndOpenOrderAsync(guestCount);
    }
}