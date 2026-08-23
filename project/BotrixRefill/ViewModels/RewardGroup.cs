using System.Collections.ObjectModel;

namespace BotrixRefill.ViewModels;

public class RewardGroup
{
    public string Label { get; }
    public ObservableCollection<RewardCardViewModel> Items { get; } = new();

    public RewardGroup(string label)
    {
        Label = label;
    }
}
