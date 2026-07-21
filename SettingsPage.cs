using Godot;
using System;
using System.Text;
using CounterTool;

public partial class SettingsPage : GenericPage
{
    public static bool hiddenTimestamp
    {
        get;
        set
        {
            field = value;
            ControlManager.instance.hideTimestamp1.SetPressed(value);
            ControlManager.instance.hideTimestamp2.SetPressed(value);
            Save();
        }
    } = false;

    public static bool autoUpload
    {
        get;
        set
        {
            field = value;
            ControlManager.instance.autoUpload1.SetPressed(value);
            ControlManager.instance.autoUpload2.SetPressed(value);
            Save();
        }
    } = true;
    public static bool autoScanOptions
    {
        get;
        set
        {
            field = value;
            ControlManager.instance.autoScanForTracks.SetPressed(value);
            Save();
        }
    } = true;
    public static bool autoScanVR
    {
        get;
        set
        {
            field = value;
            ControlManager.instance.autoScanForVr.SetPressed(value);
            Save();
        }
    } = true;

    public static SettingsPage instance;
    public static string dbUrl = "https://mkw-db.nunchuk.xyz/";
    [Export] private Label apiLabel;
    public override void _Ready()
    {
        base._Ready();
        instance = this;
        Load();
        ControlManager.instance.hideTimestamp1.Pressed += TimeStampToggle;
        ControlManager.instance.hideTimestamp2.Pressed += TimeStampToggle;
        ControlManager.instance.autoUpload1.Pressed += AutoUploadToggle;
        ControlManager.instance.autoUpload2.Pressed += AutoUploadToggle;
        ControlManager.instance.autoScanForTracks.Pressed += () => { autoScanOptions = ControlManager.instance.autoScanForTracks.IsPressed(); };
        ControlManager.instance.autoScanForVr.Pressed += () => { autoScanOptions = ControlManager.instance.autoScanForVr.IsPressed(); };
    }

    private void AutoUploadToggle()
    {
        autoUpload = !autoUpload;
    }

    private void TimeStampToggle()
    {
        hiddenTimestamp = !hiddenTimestamp;
    }

    public static void Save()
    {
        StringBuilder saveInfo = new StringBuilder();
        var saveFile = FileAccess.Open("user://CounterTool.settings", FileAccess.ModeFlags.Write);
        saveInfo.Append($"API:::{ApiKeyEntry.apiKey}:::{ApiKeyEntry.apiKeyDate}\n");
        saveInfo.Append($"CAMERA:::{CameraSetup.currentCamera}:::{CameraSetup.currentRotation}:::{CameraSetup.currentFlip}\n");
        saveInfo.Append($"TIMESTAMP:::{hiddenTimestamp}\n");
        saveInfo.Append($"UPLOAD:::{autoUpload}\n");
        saveInfo.Append($"SCANOPTIONS:::{autoScanOptions}\n");
        saveInfo.Append($"SCANVR:::{autoScanVR}\n");
        saveInfo.Append($"DBURL:::{dbUrl}\n");
        saveFile.StoreString(saveInfo.ToString());
        saveFile.Close();
    }

    public void Load()
    {
        var saveFile = FileAccess.Open("user://CounterTool.settings", FileAccess.ModeFlags.Read);
        if (saveFile is not null)
        {
            var fileContents = saveFile.GetAsText().Split("\n");
            foreach (var line in fileContents)
            {
                var lineContents = line.Split(":::");
                switch (lineContents[0])
                {
                    case "API":
                        ApiKeyEntry.apiKey = lineContents[1];
                        ApiKeyEntry.apiKeyDate = lineContents[2];
                        apiLabel.Text = $"API Key Set Date: {ApiKeyEntry.apiKeyDate}";
                        break;
                    case "CAMERA":
                        CameraSetup.currentCamera = int.Parse(lineContents[1]);
                        CameraSetup.currentRotation = int.Parse(lineContents[2]);
                        CameraSetup.currentFlip = int.Parse(lineContents[3]);
                        break;
                    case "TIMESTAMP":
                        hiddenTimestamp = bool.Parse(lineContents[1]);
                        break;
                    case "UPLOAD":
                        autoUpload = bool.Parse(lineContents[1]);
                        break;
                    case "SCANOPTIONS":
                        autoScanOptions = bool.Parse(lineContents[1]);
                        break;
                    case "SCANVR":
                        autoScanVR = bool.Parse(lineContents[1]);
                        break;
                    case "DBURL":
                        dbUrl = lineContents[1];
                        break;
                }
            }
            saveFile.Close();
        }
        else
        {
            instance.Visible = true;
        }
    }
}