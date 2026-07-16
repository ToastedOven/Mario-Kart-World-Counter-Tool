using Godot;

namespace CounterTool;

[GlobalClass]
public partial class ControlManager : Node
{
    [Export] public Control pickerPage, historyPage, comboPage, postMatchPage, cameraPage, apiPage;
    [Export] public CheckBox autoUpload;
    [Export] public Button historyButton, cameraSetup, enterApiKey;
    public static ControlManager instance;
    public override void _Ready()
    {
        instance = this;
    }
}