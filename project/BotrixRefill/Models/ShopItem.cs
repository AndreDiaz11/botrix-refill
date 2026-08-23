using System.Text.Json.Serialization;

namespace BotrixRefill.Models;

public class ShopItem
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("price")]
    public long Price { get; set; }

    [JsonPropertyName("stock")]
    public int Stock { get; set; }

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("disponibilidad")]
    public string? Disponibilidad { get; set; }
}

public class BotrixUser
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("points")]
    public long Points { get; set; }

    [JsonPropertyName("level")]
    public int? Level { get; set; }

    [JsonPropertyName("picture")]
    public string? Picture { get; set; }
}

public class WhoamiResponse
{
    [JsonPropertyName("user")]
    public BotrixUser? User { get; set; }
}
