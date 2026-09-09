using Godot;
using Godot.Collections;

namespace CounterTool;

[GlobalClass]
public partial class ControlManager : Node
{
    [Export] public Control pickerPage, historyPage, comboPage, postMatchPage, cameraPage, apiPage, settingsPage, urlPage, trackSelectionPage;
    [Export] public CheckBox autoUpload1, autoUpload2, hideTimestamp1, hideTimestamp2, autoScanForTracks, autoScanForVr, mirrorMode, autoSetPlayerCount;
    [Export] public Button historyButton, cameraSetup, enterApiKey, settings, enterCustomUrl, dcButton, pullVersionButton, rescanForCamera;
    [Export] public LineEdit versionLine;
    [Export] private PackedScene popupScene;
    public static ControlManager instance;
    [Export] public Label postMatchInstructions;
    public const int TRACKCOUNT = 40;
    [Export] public Array<Texture2D> trackTextures;
    public override void _Ready()
    {
        instance = this;
        comboPage.Visible = true;
        historyButton.Pressed += () => { historyPage.Visible = true; };
        cameraSetup.Pressed += CameraSetupOnPressed;
        enterApiKey.Pressed += () => { apiPage.Visible = true; };
        enterCustomUrl.Pressed += () => { urlPage.Visible = true; };
        settings.Pressed += () =>  { settingsPage.Visible = true; };
        LoadAfterFrame();
    }

    private void CameraSetupOnPressed()
    {
        cameraPage.Visible = true;
        CameraSetup.instance.Preview();
    }

    public async void CreatePopup(string text)
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        var popup = popupScene.Instantiate<global::Popup>();
        GetParent().AddChild(popup);
        popup.text = text;
    }
    async void LoadAfterFrame()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        SaveManager.Load();
    }
}