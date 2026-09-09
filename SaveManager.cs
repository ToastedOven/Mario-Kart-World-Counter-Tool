using System.Text;
using Godot;

namespace CounterTool;

public class SaveManager
{
    public static void Save()
    {
        StringBuilder saveInfo = new StringBuilder();
        saveInfo.Append("\n");
        foreach (var historyCard in HistoryHandler.cards)
        {
            saveInfo.Append($"{historyCard.GetDataForSaving()}|");
        }
        using var saveFile = FileAccess.Open("user://marioKartCourseTracker.save", FileAccess.ModeFlags.Write);
        saveFile.StoreString(saveInfo.ToString());
        saveFile.Close();
    }

    public static void Load()
    {
        using var saveFile = FileAccess.Open("user://marioKartCourseTracker.save", FileAccess.ModeFlags.Read);
        if (saveFile is not null)
        {
            var fileContents = saveFile.GetAsText().Replace("\r\n", "\n");
            var historyInfo = fileContents.Split("\n")[1];
            HistoryHandler.instance.LoadAllCards(historyInfo);
            saveFile.Close();
        }
    }
}