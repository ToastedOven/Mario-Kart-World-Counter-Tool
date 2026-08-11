using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CounterTool;
using Godot;
using Godot.Collections;
using ImageMagick;
using OpenCvSharp;

public partial class VRAverageCalculator : Node
{
    public static List<int> currentMatchVRs = new();
    public static string myCurrentVR;
    public static VRAverageCalculator instance;

    [Export] private Array<Button> vrButtons;
    [Export] private Button resetButton;
    [Export] private Label averageVR, myVR, racerCount;
    
    private int prevAverageVR = -1;

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
        averageVR.Text = "Average VR: null";
        myVR.Text = "My VR: null";
        racerCount.Text = "Real Player Count: null";
    }

    public async void ProcessVR()
    {
        averageVR.Text = "Average VR: checking...";
        myVR.Text = "My VR: checking...";
        racerCount.Text = "Real Player Count: checking...";

        await GetSourceImage("BaseImages/ligmaballs.tiff");
        if (!gotCapture)
        {
            Reset();
            return;
        }
        
        List<Task> cropTasks = new();

        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 12; y++)
            {
                string bounds = $"{85}:{25}:{(x == 0 ? 440 : 940)}:{(110 + (y * 76))}";
                cropTasks.Add(ProcessImageAsync(bounds, $"OcrImages/{x},{y}.tiff", 90, "BaseImages/ligmaballs.tiff"));
                cropTasks.Add(ProcessImageAsync(bounds, $"OcrImages/myScore_{x},{y}.tiff", 200, "BaseImages/ligmaballs.tiff"));
            }
        }

        await Task.WhenAll(cropTasks);

        List<Task<(int score, int x, int y)>> myScoreOcrTasks = new();
        List<Task<(int score, int x, int y)>> ocrTasks = new();

        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 12; y++)
            {
                myScoreOcrTasks.Add(RunOcrForRegion($"OcrImages/myScore_{x},{y}.tiff", x, y));
                ocrTasks.Add(RunOcrForRegion($"OcrImages/{x},{y}.tiff", x, y));
            }
        }

        await Task.WhenAll(myScoreOcrTasks);
        await Task.WhenAll(ocrTasks);

        int myX = -1, myY = -1;
        int myScore = 0;

        foreach (var task in myScoreOcrTasks)
        {
            var result = task.Result;
            if (result.score != 0)
            {
                myX = result.x;
                myY = result.y;
                myScore = result.score;
            }
        }

        List<int> allScores = new();
        foreach (var task in ocrTasks)
        {
            var result = task.Result;
            if (result.x == myX && result.y == myY)
            {
                allScores.Add(myScore);
            }
            else if (result.score != 0)
            {
                allScores.Add(result.score);
            }
        }

        if (allScores.Count == 0)
        {
            return;
        }
        RecentTrackTracker.instance.timer = 0;
        if (myScore != 0)
        {
            myCurrentVR = $"{myScore}";
        }
        else
        {
            myCurrentVR = "Couldn't get my VR";
            Reset();
            return;
        }

        if (allScores.Any(score => score > 9999 || score < 1000))
        {
            Reset();
            return;
        }

        int totalVR = allScores.Sum();
        currentMatchVRs = allScores;
        
        int average = totalVR / allScores.Count;
        averageVR.Text = $"Average VR: {average}";
        myVR.Text = $"My VR: {myCurrentVR}";
        racerCount.Text = $"Real Player Count: {allScores.Count}";

        if (average == prevAverageVR)
        {
            prevAverageVR = -1;
        }
        
        prevAverageVR = average;
    }

    public static bool gotCapture;
    public async Task GetSourceImage(string filename)
    {
        gotCapture = false;
        await Task.Run(() =>
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
                Cv2.ImWrite(filename, newFrame);
                gotCapture = true;
            }
        });
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
            using var image = new MagickImage(sourceFile);
            image.Crop(new MagickGeometry(x, y, (uint)width, (uint)height));
            image.ResetPage(); 

            if (threshold > 0)
            {
                image.ColorType = ColorType.Grayscale;
                double thresholdPercentage = (threshold / 255.0) * 100.0;
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
                UseShellExecute = false, 
                CreateNoWindow = true
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