using collectiblesManagementSystem.Views;

namespace collectiblesManagementSystem;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        CollectionsList.ItemsSource = App.Collections;
    }

    private void OnAddCollectionClicked(object sender, EventArgs e)
    {
        var name = NewCollectionEntry.Text?.Trim();
        if (!string.IsNullOrWhiteSpace(name))
        {
            var newCollection = new Models.Collection { Name = name };
            App.Collections.Add(newCollection);
            
            App.SaveData();

            NewCollectionEntry.Text = string.Empty;
        }
    }

    private async void OnCollectionSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Models.Collection selectedCollection)
        {
            CollectionsList.SelectedItem = null;
            
            await Navigation.PushAsync(new CollectionPage(selectedCollection));
        }
    }

    private async void OnDeleteCollectionClicked(object sender, EventArgs e)
    {
        if (sender is not Button button || button.CommandParameter is not Models.Collection collection)
            return;

        bool confirm = await DisplayAlert(
            "Usuń kolekcję",
            $"Czy na pewno chcesz usunąć kolekcję '{collection.Name}' wraz z jej zawartością?",
            "Usuń",
            "Anuluj");

        if (!confirm)
            return;

        App.Collections.Remove(collection);
        App.SaveData();
    }
}