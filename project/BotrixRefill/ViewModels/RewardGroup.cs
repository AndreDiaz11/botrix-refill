using System.Collections.ObjectModel;

namespace BotrixRefill.ViewModels;

public class RewardGroup
{
    public string Label { get; }
    public string IconGlyph { get; }
    public string IconBg { get; }
    public ObservableCollection<RewardCardViewModel> Items { get; } = new();

    public RewardGroup(string label)
    {
        Label = label;
        (IconGlyph, IconBg) = label switch
        {
            "Yape / Plin" => ("▶", "#7C3AED"),
            "Suscripciones" => ("★", "#16A34A"),
            "Recargas" => ("⚡", "#F97316"),
            _ => ("🎁", "#6B7280"),
        };
    }
}
