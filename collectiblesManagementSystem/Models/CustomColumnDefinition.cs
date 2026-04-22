namespace collectiblesManagementSystem.Models;

public class CustomColumnDefinition
{
    public string Name { get; set; } = "";
    public CustomColumnType Type { get; set; } = CustomColumnType.Text;
    public List<string> Options { get; set; } = new();
}
