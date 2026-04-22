using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using collectiblesManagementSystem.Models;

namespace collectiblesManagementSystem.Views;

public partial class CollectionPage : ContentPage
{
    private Models.Collection _collection;
    private readonly ObservableCollection<CollectionItem> _itemsView = new();
    
    public CollectionPage(Models.Collection collection)
    {
        InitializeComponent();
        _collection = collection;
        Title = _collection.Name;
        ItemsList.ItemsSource = _itemsView;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshData();
    }

    private void RefreshData()
    {
        var sortedItems = _collection.Items
            .OrderBy(i => i.Status == ItemStatus.Sold ? 1 : 0)
            .ThenBy(i => i.Name)
            .ToList();
        
        _itemsView.Clear();
        foreach (var it in sortedItems)
            _itemsView.Add(it);
        
        int total = _collection.Items.Count;
        int sold = _collection.Items.Count(i => i.Status == ItemStatus.Sold);
        int toSell = _collection.Items.Count(i => i.Status == ItemStatus.ForSale);
        
        SummaryLabel.Text = $"Posiadane: {total} | Na sprzedaż: {toSell} | Sprzedane: {sold}";
    }

    private async void OnAddItemClicked(object sender, EventArgs e)
    {
        var page = new ItemEditPage(_collection);
        page.ItemSaved += (_, _) => RefreshData();
        await Navigation.PushAsync(page);
    }

    private async void OnAddColumnClicked(object sender, EventArgs e)
    {
        string columnName = (await DisplayPromptAsync("Nowa kolumna", "Wpisz nazwę nowej kolumny danych:"))?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(columnName))
            return;

        if (_collection.HasCustomColumn(columnName))
        {
            await DisplayAlert("Informacja", $"Kolumna '{columnName}' już istnieje.", "OK");
            return;
        }

        string typeAction = await DisplayActionSheet("Typ kolumny", "Anuluj", null, "Tekst", "Liczba", "Wybór z listy");
        if (typeAction == "Anuluj" || string.IsNullOrWhiteSpace(typeAction))
            return;

        var def = new CustomColumnDefinition { Name = columnName };

        if (typeAction == "Tekst")
        {
            def.Type = CustomColumnType.Text;
        }
        else if (typeAction == "Liczba")
        {
            def.Type = CustomColumnType.Number;
        }
        else if (typeAction == "Wybór z listy")
        {
            def.Type = CustomColumnType.Select;

            var optionsRaw = (await DisplayPromptAsync(
                "Opcje (select)",
                "Podaj opcje rozdzielone przecinkiem, np. A, B, C:",
                accept: "OK",
                cancel: "Anuluj"))?.Trim();

            if (string.IsNullOrWhiteSpace(optionsRaw))
                return;

            var options = optionsRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (options.Count == 0)
            {
                await DisplayAlert("Błąd", "Dla typu „Wybór z listy” musisz podać przynajmniej jedną opcję.", "OK");
                return;
            }

            def.Options.AddRange(options);
        }
        else
        {
            return;
        }

        _collection.CustomColumns.Add(def);
        App.SaveData();
        await DisplayAlert("Sukces", $"Dodano kolumnę '{columnName}'.", "OK");
    }

    private async void OnSyncClicked(object sender, EventArgs e)
    {
        string action = await DisplayActionSheet("Eksport / Import", "Anuluj", null, "Eksportuj", "Importuj");
        
        if (action == "Eksportuj")
        {
            try
            {
                string data = _collection.Serialize();
                string fileName = $"{_collection.Name.Replace(" ", "_")}_export.txt";
                string directoryPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                Directory.CreateDirectory(directoryPath);
                string fullPath = Path.Combine(directoryPath, fileName);
                File.WriteAllText(fullPath, data, Encoding.UTF8);

                await DisplayAlert("Sukces", $"Plik zapisany:\n{fullPath}", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Błąd", $"Eksport nie powiódł się: {ex.Message}", "OK");
            }
        }
        else if (action == "Importuj")
        {
            try
            {
                var result = await FilePicker.PickAsync(new PickOptions { PickerTitle = "Wybierz plik txt" });
                if (result != null)
                {
                    string content = File.ReadAllText(result.FullPath);
                    var imported = Models.Collection.Deserialize(content);

                    MergeImportedColumns(imported);
                    int affectedItems = MergeImportedItems(imported.Items);
                    
                    App.SaveData();
                    RefreshData();
                    await DisplayAlert("Sukces", $"Import zakończony. Zaktualizowano / dodano pozycji: {affectedItems}.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Błąd", $"Import nie powiódł się: {ex.Message}", "OK");
            }
        }
    }

    private void MergeImportedColumns(Models.Collection imported)
    {
        foreach (var importedCol in imported.CustomColumns)
        {
            var existing = _collection.GetCustomColumn(importedCol.Name);
            if (existing == null)
            {
                _collection.CustomColumns.Add(importedCol);
                continue;
            }

            if (existing.Type == CustomColumnType.Select && importedCol.Type == CustomColumnType.Select)
            {
                foreach (var opt in importedCol.Options)
                {
                    if (!existing.Options.Any(o => o.Equals(opt, StringComparison.OrdinalIgnoreCase)))
                        existing.Options.Add(opt);
                }
            }
        }
    }

    private async void OnItemSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is CollectionItem selectedItem)
        {
            ItemsList.SelectedItem = null;
            var page = new ItemEditPage(_collection, selectedItem);
            page.ItemSaved += (_, _) => RefreshData();
            await Navigation.PushAsync(page);
        }
    }

    private int MergeImportedItems(IEnumerable<CollectionItem> importedItems)
    {
        int affected = 0;

        foreach (var importedItem in importedItems)
        {
            importedItem.Id = Guid.NewGuid().ToString();
            importedItem.Quantity = Math.Max(1, importedItem.Quantity);

            foreach (var key in importedItem.CustomData.Keys.ToList())
            {
                if (!_collection.HasCustomColumn(key))
                {
                    _collection.CustomColumns.Add(new CustomColumnDefinition
                    {
                        Name = key,
                        Type = CustomColumnType.Text
                    });
                }
            }

            var sameItem = _collection.Items.FirstOrDefault(existing =>
                existing.Name.Equals(importedItem.Name, StringComparison.OrdinalIgnoreCase)
                && existing.Status == importedItem.Status
                && string.Equals(existing.Comment?.Trim(), importedItem.Comment?.Trim(), StringComparison.OrdinalIgnoreCase)
                && existing.Price == importedItem.Price);

            if (sameItem != null)
            {
                sameItem.Quantity += importedItem.Quantity;

                foreach (var kv in importedItem.CustomData)
                {
                    if (!sameItem.CustomData.ContainsKey(kv.Key) || string.IsNullOrWhiteSpace(sameItem.CustomData[kv.Key]))
                        sameItem.CustomData[kv.Key] = kv.Value;
                }
            }
            else
            {
                _collection.Items.Add(importedItem);
            }

            affected++;
        }

        return affected;
    }
}
