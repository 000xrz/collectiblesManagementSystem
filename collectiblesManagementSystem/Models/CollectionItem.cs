namespace collectiblesManagementSystem.Models;

public class CollectionItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public double Price { get; set; }
    public ItemStatus Status { get; set; } = ItemStatus.New;
    public int Rating { get; set; } = 5;
    public string Comment { get; set; } = "";
    public string ImagePath { get; set; } = "";
    public int Quantity { get; set; } = 1;

    public Dictionary<string, string> CustomData { get; set; } = new();

    public bool HasImage => !string.IsNullOrWhiteSpace(ImagePath);

    public string StatusDisplay => Status switch
    {
        ItemStatus.New => "Nowy",
        ItemStatus.Used => "Używany",
        ItemStatus.ForSale => "Na sprzedaż",
        ItemStatus.Sold => "Sprzedany",
        ItemStatus.Wanted => "Chcę kupić",
        _ => Status.ToString()
    };

    public string ExtendedDetails 
    {
        get
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine($"Cena: {Price:0.##} zł");
            sb.AppendLine($"Status: {StatusDisplay}");
            sb.AppendLine($"Ocena: {Rating}/10");
            sb.AppendLine($"Ilość: {Quantity} szt.");

            if (!string.IsNullOrWhiteSpace(Comment))
                sb.AppendLine($"Komentarz: {Comment}");

            foreach (var kvp in CustomData)
            {
                if (!string.IsNullOrWhiteSpace(kvp.Value))
                    sb.AppendLine($"{kvp.Key}: {kvp.Value}");
            }

            return sb.ToString().TrimEnd();
        }
    }

    public string Serialize()
    {
        var customDataStr = string.Join(";", CustomData.Select(kv => $"{kv.Key}:{kv.Value}"));
        return $"{Id}|{Name}|{Price}|{(int)Status}|{Rating}|{Comment}|{ImagePath}|{customDataStr}|{Quantity}";
    }

    public static CollectionItem Deserialize(string data)
    {
        var parts = data.Split('|');
        if (parts.Length < 7)
            return new CollectionItem();

        var item = new CollectionItem
        {
            Id = parts[0],
            Name = parts[1],
            Price = double.TryParse(parts[2], out var parsedPrice) ? parsedPrice : 0,
            Status = int.TryParse(parts[3], out var parsedStatus) ? (ItemStatus)parsedStatus : ItemStatus.New,
            Rating = int.TryParse(parts[4], out var parsedRating) ? parsedRating : 5,
            Comment = parts[5],
            ImagePath = parts[6]
        };

        if (parts.Length > 7 && !string.IsNullOrEmpty(parts[7]))
        {
            var dictParts = parts[7].Split(';');
            foreach (var dp in dictParts)
            {
                var kv = dp.Split(':');
                if (kv.Length == 2) item.CustomData[kv[0]] = kv[1];
            }
        }
        
        if (parts.Length > 8 && int.TryParse(parts[8], out int quantity))
        {
            item.Quantity = Math.Max(1, quantity);
        }
        
        return item;
    }
}