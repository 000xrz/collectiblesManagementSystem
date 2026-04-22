namespace collectiblesManagementSystem.Models;

public class Collection
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public List<CustomColumnDefinition> CustomColumns { get; set; } = new();
    public List<CollectionItem> Items { get; set; } = new();

    public CustomColumnDefinition? GetCustomColumn(string name) =>
        CustomColumns.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public bool HasCustomColumn(string name) => GetCustomColumn(name) != null;

    public string Serialize()
    {
        var cols = string.Join(";", CustomColumns.Select(SerializeColumn));
        var itemsStr = string.Join("~", Items.Select(i => i.Serialize()));
        return $"{Id}^{Name}^{cols}^{itemsStr}";
    }

    public static Collection Deserialize(string data)
    {
        var parts = data.Split('^');
        var col = new Collection { Id = parts[0], Name = parts[1] };
        
        if (parts.Length > 2 && !string.IsNullOrEmpty(parts[2]))
        {
            foreach (var token in parts[2].Split(';'))
            {
                var def = DeserializeColumn(token);
                if (def != null && !col.HasCustomColumn(def.Name))
                    col.CustomColumns.Add(def);
            }
        }
        
        if (parts.Length > 3 && !string.IsNullOrEmpty(parts[3]))
        {
            foreach (var itemStr in parts[3].Split('~'))
            {
                col.Items.Add(CollectionItem.Deserialize(itemStr));
            }
        }

        return col;
    }

    private static string SerializeColumn(CustomColumnDefinition def)
    {
        var name = Uri.EscapeDataString(def.Name ?? "");
        var type = (int)def.Type;
        var options = def.Type == CustomColumnType.Select
            ? string.Join(",", (def.Options ?? new List<string>()).Select(o => Uri.EscapeDataString(o ?? "")).Where(o => !string.IsNullOrWhiteSpace(o)))
            : "";

        return $"{name},{type},{options}";
    }

    private static CustomColumnDefinition? DeserializeColumn(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var parts = token.Split(',', 3);
        if (parts.Length >= 2 && int.TryParse(parts[1], out var typeInt))
        {
            var name = Uri.UnescapeDataString(parts[0]);
            if (string.IsNullOrWhiteSpace(name))
                return null;

            var type = Enum.IsDefined(typeof(CustomColumnType), typeInt) ? (CustomColumnType)typeInt : CustomColumnType.Text;
            var def = new CustomColumnDefinition { Name = name, Type = type };

            if (type == CustomColumnType.Select && parts.Length == 3 && !string.IsNullOrWhiteSpace(parts[2]))
            {
                foreach (var optEnc in parts[2].Split(','))
                {
                    var opt = Uri.UnescapeDataString(optEnc);
                    if (!string.IsNullOrWhiteSpace(opt) && !def.Options.Any(o => o.Equals(opt, StringComparison.OrdinalIgnoreCase)))
                        def.Options.Add(opt);
                }
            }

            return def;
        }

        var oldName = token;
        try
        {
            oldName = Uri.UnescapeDataString(oldName);
        }
        catch
        {
        }

        if (string.IsNullOrWhiteSpace(oldName))
            return null;

        return new CustomColumnDefinition { Name = oldName, Type = CustomColumnType.Text };
    }
}
