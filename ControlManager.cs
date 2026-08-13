using Godot;

namespace CounterTool;

[GlobalClass]
public partial class ControlManager : Node
{
    [Export] public Control pickerPage, historyPage, comboPage, postMatchPage, cameraPage, apiPage, settingsPage, urlPage, trackSelectionPage;
    [Export] public CheckBox autoUpload1, autoUpload2, hideTimestamp1, hideTimestamp2, autoScanForTracks, autoScanForVr, mirrorMode, autoSetPlayerCount;
    [Export] public Button historyButton, cameraSetup, enterApiKey, settings, enterCustomUrl, dcButton, pullVersionButton;
    [Export] public LineEdit versionLine;
    [Export] private PackedScene popupScene;
    public static ControlManager instance;
    [Export] public Label postMatchInstructions;
    public override void _Ready()
    {
        instance = this;
        comboPage.Visible = true;
        historyButton.Pressed += () => { historyPage.Visible = true; };
        cameraSetup.Pressed += CameraSetupOnPressed;
        enterApiKey.Pressed += () => { apiPage.Visible = true; };
        enterCustomUrl.Pressed += () => { urlPage.Visible = true; };
        settings.Pressed += () =>  { settingsPage.Visible = true; };
    }

    private void CameraSetupOnPressed()
    {
        cameraPage.Visible = true;
        CameraSetup.instance.Preview();
    }

    public void CreatePopup(string text)
    {
        var popup = popupScene.Instantiate<global::Popup>();
        GetParent().AddChild(popup);
        popup.text = text;
    }
}