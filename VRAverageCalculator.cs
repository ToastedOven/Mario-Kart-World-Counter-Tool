using System;
using System.Collections.Generic;
using Godot;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CounterTool;
using Godot.Collections;
using ImageMagick;
using OpenCvSharp;
using FileAccess = System.IO.FileAccess;

public partial class VRAverageCalculator : Node
{
    public static List<int> currentMatchVRs = new();
    public static string myCurrentVR;
    [Export] private Array<Button> vrButtons;
    [Export] private Button resetButton;
    [Export] private Label averageVR, myVR, racerCount;
    public static VRAverageCalculator instance;

    public override void _Ready()
    {
        instance = this;
        foreach (var button in vrButtons)
        {
            button.Pressed += ButtonOnPressed;
        }

        resetButton.Pressed += Reset;
    }

    private void ButtonOnPressed()
    {
        ProcessVR();
        RecentTrackTracker.instance.timer = 0;
    }

    public void Reset()
    {
        currentMatchVRs.Clear();
        myCurrentVR = "null";
        checkTimer = 2.5;
        averageVR.Text = $"Average VR: null";
        myVR.Text = $"My VR: null";
        racerCount.Text = $"Real Player Count: null";
    }

    private double checkTimer = 2.5;
    public override void _Process(double delta)
    {
        // checkTimer -= delta;
        if (checkTimer < 0)
        {
            checkTimer = 2.5;
            ProcessVR();
        }
    }

    private string leftBounds = $"{61 * currentImageWidthRatio}:{900 * currentImageHeightRatio}:{448 * currentImageWidthRatio}:{100 * currentImageHeightRatio}";
    private string rightBounds = $"{61 * currentImageWidthRatio}:{900 * currentImageHeightRatio}:{951 * currentImageWidthRatio}:{100 * currentImageHeightRatio}";
    private int prevAverageVR = -1;
    async public void ProcessVR()
    {
        averageVR.Text = $"Average VR: checking...";
        myVR.Text = $"My VR: checking...";
        racerCount.Text = $"Real Player Count: checking...";
        await GetSourceImage("BaseImages/ligmaballs.tiff");
        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 12; y++)
            {
                string thisBounds = $"{85 * currentImageWidthRatio}:{25 * currentImageHeightRatio}:{(x == 0 ? 440 : 940) * currentImageWidthRatio}:{(110 + (y * 76)) * currentImageHeightRatio}";
                await ProcessImageAsync(thisBounds, $"OcrImages/{x},{y}.tiff", 90, "BaseImages/ligmaballs.tiff");
            }
        }
        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 12; y++)
            {
                string thisBounds = $"{85 * currentImageWidthRatio}:{25 * currentImageHeightRatio}:{(x == 0 ? 440 : 940) * currentImageWidthRatio}:{(110 + (y * 76)) * currentImageHeightRatio}";
                await ProcessImageAsync(thisBounds, $"OcrImages/myScore_{x},{y}.tiff", 200, "BaseImages/ligmaballs.tiff");
            }
        }

        List<int> allScores = new();
        int myScore = 0;

        int myX = -1, myY = -1;

        List<Task<(int score, int x, int y)>> myScoreOcrTasks = new();
        List<Task<(int score, int x, int y)>> ocrTasks = new();
        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 12; y++)
            {
                myScoreOcrTasks.Add(RunOcrForRegion($"OcrImages/myScore_{x},{y}.tiff", x, y));
            }
        }
        
        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 12; y++)
            {
                ocrTasks.Add(RunOcrForRegion($"OcrImages/{x},{y}.tiff", x, y));
            }
        }
        await Task.WhenAll(myScoreOcrTasks.ToArray());
        foreach (var task in myScoreOcrTasks)
        {
            if (task.Result.score != 0)
            {
                myX = task.Result.x;
                myY = task.Result.y;
                myScore = task.Result.score;
            }
        }
        await Task.WhenAll(ocrTasks.ToArray());
        foreach (var task in ocrTasks)
        {
            if (task.Result.x == myX && task.Result.y == myY)
            {
                allScores.Add(myScore);
            }
            else if (task.Result.score != 0)
            {
                allScores.Add(task.Result.score);
            }
        }
        
        if (allScores.Count == 0)
        {
            return;
        }
        int totalScore = 0;
        foreach (var score in allScores)
        {
            totalScore += score;
        }
        if (myScore != 0)
        {
            myCurrentVR = $"{myScore}";
        }
        else
        {
            myCurrentVR = $"Couldn't get my VR";
            Reset();
            return;
        }
        int totalVR = 0;
        foreach (var playerVR in allScores)
        {
            if (playerVR > 9999 || playerVR < 1000)
            {
                Reset();
                return;
            }
            totalVR += playerVR;
            // GD.Print($"VR: {playerVR}");
        }
        currentMatchVRs = allScores;
        averageVR.Text = $"Average VR: {totalVR / allScores.Count}";
        myVR.Text = $"My VR: {myCurrentVR}";
        racerCount.Text = $"Real Player Count: {allScores.Count}";
        // GD.Print($"Lobby count: {allScores.Count}, Average VR: {totalVR / allScores.Count}, Current VR: {myCurrentVR}");
        if (totalVR / allScores.Count == prevAverageVR)
        {
            prevAverageVR = -1;
            checkTimer = 60;
        }
        else
        {
            checkTimer = 2.5;
        }
        prevAverageVR = totalVR / allScores.Count;
    }
    public static float currentImageWidthRatio, currentImageHeightRatio;
    public async Task GetSourceImage(string filename)
    {
        using var capture = new VideoCapture(CameraSetup.currentCamera);
        if (capture.IsOpened())
        {
            CameraSetup.ConfigureResolution(capture);
            using var frame = new Mat();
            capture.Read(frame);
            CameraSetup.ApplyTransforms(frame);
            using var newFrame = new Mat();
            Cv2.Resize(frame, newFrame, new Size(1920, 1080));
            Cv2.ImWrite(filename,newFrame);
            var info = new MagickImageInfo(filename);
            currentImageHeightRatio = (int)info.Height / 1080f;
            currentImageWidthRatio = (int)info.Width / 1920f;
        }
    }
    public async Task ProcessImageAsync(string cropCoords, string filename, int threshold, string sourceFile)
    {
        var parts = cropCoords.Split(':');
        if (parts.Length < 4) 
            throw new ArgumentException("Invalid crop coordinates format. Expected 'w:h:x:y'.");

        int width = (int)float.Parse(parts[0]);
        int height = (int)float.Parse(parts[1]);
        int x = (int)float.Parse(parts[2]);
        int y = (int)float.Parse(parts[3]);

        await Task.Run(() =>
        {
            using FileStream fs = new FileStream(sourceFile, FileMode.Open, FileAccess.Read);
            using var image = new MagickImage(fs);
            image.Crop(new MagickGeometry(x, y, (uint)width, (uint)height));
            
            image.ResetPage(); 

            if (threshold > 0)
            {
                image.ColorType = ColorType.Grayscale;

                double thresholdPercentage = ((double)threshold / 255.0) * 100.0;
                image.Threshold(new Percentage(thresholdPercentage));
            }

            image.Write(filename);
        });
    }
    private async Task<(int score, int x, int y)> RunOcrForRegion(string filename, int x, int y)
    {
        return await Task.Run(() =>
        {
            string tessDataPath = ProjectSettings.GlobalizePath("res://tessdata/");
            var tesseractInfo = new ProcessStartInfo {
                FileName = "tesseract",
                Arguments = $"{filename} stdout --psm 11 -c tessedit_char_whitelist=0123456789 --tessdata-dir {tessDataPath} -l eng2",
                RedirectStandardOutput = true,
                UseShellExecute = false, CreateNoWindow = true
            };

            using (var p = Process.Start(tesseractInfo))
            {
                string result = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                result = result.Replace("\n", " ").Trim();
                if (int.TryParse(result, out int score))
                {
                    return (score, x, y);
                }
            }

            return (0, x, y);
        });
    }
}