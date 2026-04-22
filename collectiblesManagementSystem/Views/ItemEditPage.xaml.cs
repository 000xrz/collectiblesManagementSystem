using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using collectiblesManagementSystem.Models;

namespace collectiblesManagementSystem.Views;

public partial class ItemEditPage : ContentPage
{
    private readonly Collection _parentCollection;
    private readonly CollectionItem _currentItem;
    private readonly bool _isNew;
    public event EventHandler? ItemSaved;
    
    public ItemEditPage(Collection parentCollection, CollectionItem itemToEdit = null)
    {
        InitializeComponent();
        
        _parentCollection = parentCollection;
        _isNew = itemToEdit == null;
        _currentItem = itemToEdit ?? new CollectionItem();

        if (!_isNew)
        {
            NameEntry.Text = _currentItem.Name;
            PriceEntry.Text = _currentItem.Price.ToString();
            StatusPicker.SelectedIndex = (int)_currentItem.Status;
            RatingSlider.Value = _currentItem.Rating;
            CommentEditor.Text = _currentItem.Comment;
            QuantityEntry.Text = _currentItem.Quantity.ToString();
            
            if (!string.IsNullOrEmpty(_currentItem.ImagePath))
                SetImagePreview(_currentItem.ImagePath);
            
            DeleteButton.IsVisible = true;
            Title = "Edytuj przedmiot";
        }
        else
        {
            Title = "Dodaj nowy przedmiot";
            QuantityEntry.Text = "1";
        }

        RatingLabel.Text = $"Ocena: {(int)RatingSlider.Value}/10";
        UpdateNoImageState();
        
        GenerateCustomColumns();
    }

    private readonly Dictionary<string, View> _customEditors = new();

    private void GenerateCustomColumns()
    {
        CustomColumnsContainer.Children.Clear();
        _customEditors.Clear();

        foreach (var col in _parentCollection.CustomColumns)
        {
            var colName = col.Name;
            CustomColumnsContainer.Children.Add(new Label { Text = colName + ":", FontAttributes = FontAttributes.Bold });

            _currentItem.CustomData.TryGetValue(colName, out var currentValue);

            View editor;
            switch (col.Type)
            {
                case CustomColumnType.Number:
                    editor = new Entry
                    {
                        Placeholder = "Wpisz liczbę...",
                        Keyboard = Keyboard.Numeric,
                        Text = currentValue
                    };
                    break;
                case CustomColumnType.Select:
                    var picker = new Picker { Title = "Wybierz..." };
                    foreach (var opt in col.Options)
                        picker.Items.Add(opt);

                    if (!string.IsNullOrWhiteSpace(currentValue))
                    {
                        var idx = picker.Items.IndexOf(currentValue);
                        if (idx >= 0) picker.SelectedIndex = idx;
                    }

                    editor = picker;
                    break;
                case CustomColumnType.Text:
                default:
                    editor = new Entry
                    {
                        Placeholder = "Wpisz wartość...",
                        Text = currentValue
                    };
                    break;
            }

            _customEditors[colName] = editor;
            CustomColumnsContainer.Children.Add(editor);
        }
    }

    private void OnRatingChanged(object sender, ValueChangedEventArgs e)
    {
        RatingLabel.Text = $"Ocena: {(int)e.NewValue}/10";
    }

    private void UpdateNoImageState()
    {
        NoImageLabel.IsVisible = string.IsNullOrWhiteSpace(_currentItem.ImagePath);
    }

    private void SetImagePreview(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            _currentItem.ImagePath = string.Empty;
            ItemImage.Source = null;
            UpdateNoImageState();
            return;
        }

        _currentItem.ImagePath = path;
        ItemImage.Source = ImageSource.FromFile(path);
        UpdateNoImageState();
    }

    private async void OnSelectImageClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await MediaPicker.Default.PickPhotoAsync();
            if (result != null)
            {
                string? savedImagePath = await SavePickedImageToAppDataAsync(result);
                if (!string.IsNullOrWhiteSpace(savedImagePath))
                    SetImagePreview(savedImagePath);
            }
        }
        catch (Exception)
        {
            await DisplayAlert("Błąd", "Nie udało się wczytać zdjęcia.", "OK");
        }
    }

    private static async Task<string?> SavePickedImageToAppDataAsync(FileResult pickedFile)
    {
        if (pickedFile == null)
            return null;

        var imagesDir = Path.Combine(FileSystem.AppDataDirectory, "item_images");
        Directory.CreateDirectory(imagesDir);

        string extension = Path.GetExtension(pickedFile.FileName);
        if (string.IsNullOrWhiteSpace(extension))
            extension = ".jpg";

        string destinationPath = Path.Combine(imagesDir, $"{Guid.NewGuid()}{extension}");

        await using var sourceStream = await pickedFile.OpenReadAsync();
        await using var destinationStream = File.Create(destinationPath);
        await sourceStream.CopyToAsync(destinationStream);

        return destinationPath;
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var name = NameEntry.Text?.Trim() ?? "";
        
        if (string.IsNullOrWhiteSpace(name))
        {
            await DisplayAlert("Błąd", "Nazwa przedmiotu nie może być pusta.", "OK");
            return;
        }

        if (_isNew && _parentCollection.Items.Any(i => i.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            bool proceed = await DisplayAlert("Znaleziono duplikat",
                "Masz już przedmiot o takiej nazwie w tej kolekcji. Czy na pewno chcesz podać 2 raz taki sam element?",
                "Tak", "Anuluj");

            if (!proceed)
                return;
        }

        if (!double.TryParse(PriceEntry.Text, out double price) || price < 0)
        {
            await DisplayAlert("Błąd", "Podaj poprawną cenę (0 lub więcej).", "OK");
            return;
        }

        if (!int.TryParse(QuantityEntry.Text, out int quantity) || quantity < 1)
        {
            await DisplayAlert("Błąd", "Podaj poprawną ilość (minimum 1).", "OK");
            return;
        }
        
        _currentItem.Name = name;
        _currentItem.Price = price;
        
        _currentItem.Status = (ItemStatus)StatusPicker.SelectedIndex;
        _currentItem.Rating = (int)RatingSlider.Value;
        _currentItem.Comment = CommentEditor.Text ?? "";
        _currentItem.Quantity = quantity;
        
        foreach (var col in _parentCollection.CustomColumns)
        {
            var colName = col.Name;
            if (!_customEditors.TryGetValue(colName, out var editor))
                continue;

            string value = "";
            if (editor is Entry entry)
            {
                value = entry.Text?.Trim() ?? "";
            }
            else if (editor is Picker picker)
            {
                value = picker.SelectedItem?.ToString()?.Trim() ?? "";
            }

            if (col.Type == CustomColumnType.Number && !string.IsNullOrWhiteSpace(value))
            {
                if (!TryParseFlexibleDouble(value, out _))
                {
                    await DisplayAlert("Błąd", $"Kolumna '{colName}' wymaga liczby.", "OK");
                    return;
                }
            }

            if (col.Type == CustomColumnType.Select && !string.IsNullOrWhiteSpace(value))
            {
                if (col.Options.Count > 0 && !col.Options.Any(o => o.Equals(value, StringComparison.OrdinalIgnoreCase)))
                {
                    await DisplayAlert("Błąd", $"Kolumna '{colName}' wymaga wyboru z listy.", "OK");
                    return;
                }
            }

            _currentItem.CustomData[colName] = value;
        }
        
        if (_isNew)
            _parentCollection.Items.Add(_currentItem);
        
        App.SaveData();
        ItemSaved?.Invoke(this, EventArgs.Empty);
        
        await Navigation.PopAsync();
    }

    private static bool TryParseFlexibleDouble(string text, out double value)
    {
        return double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value)
               || double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        bool answer = await DisplayAlert("Potwierdź", "Usunąć bezpowrotnie ten element?", "Tak", "Nie");
        if (answer)
        {
            _parentCollection.Items.Remove(_currentItem);
            App.SaveData();
            ItemSaved?.Invoke(this, EventArgs.Empty);
            await Navigation.PopAsync();
        }
    }
}
