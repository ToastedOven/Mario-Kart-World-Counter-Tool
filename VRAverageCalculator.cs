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

    public static readonly string baseImagesPath = ProjectSettings.GlobalizePath("res://BaseImages");
    public static  readonly string ocrImagesPath = ProjectSettings.GlobalizePath("res://OcrImages");
    
    private int prevAverageVR = -1;

    public override void _Ready()
    {
        instance = this;
        foreach (var button in vrButtons)
        {
            button.Pressed += ButtonOnPressed;
        }
        resetButton.Pressed += Reset;

        Directory.CreateDirectory(baseImagesPath);
        Directory.CreateDirectory(ocrImagesPath);
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

        string sourceFile = Path.Combine(baseImagesPath, "ligmaballs.tiff");

        await GetSourceImage(sourceFile);
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
                
                string ocrFile = Path.Combine(ocrImagesPath, $"{x},{y}.tiff");
                string myScoreFile = Path.Combine(ocrImagesPath, $"myScore_{x},{y}.tiff");

                cropTasks.Add(ProcessImageAsync(bounds, ocrFile, 90, sourceFile));
                cropTasks.Add(ProcessImageAsync(bounds, myScoreFile, 200, sourceFile));
            }
        }

        await Task.WhenAll(cropTasks);

        List<Task<(int score, int x, int y)>> myScoreOcrTasks = new();
        List<Task<(int score, int x, int y)>> ocrTasks = new();

        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 12; y++)
            {
                string ocrFile = Path.Combine(ocrImagesPath, $"{x},{y}.tiff");
                string myScoreFile = Path.Combine(ocrImagesPath, $"myScore_{x},{y}.tiff");

                myScoreOcrTasks.Add(RunOcrForRegion(myScoreFile, x, y));
                ocrTasks.Add(RunOcrForRegion(ocrFile, x, y));
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
    public async Task GetSourceImage(string fullFilePath)
    {
        using var frame = new Mat();
        if (CameraSetup.ReadFrame(frame))
        {
            Cv2.ImWrite(fullFilePath, frame);
            gotCapture = true;
        }
        else
        {
            gotCapture = false;
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

    private async Task<(int score, int x, int y)> RunOcrForRegion(string fullFilePath, int x, int y)
    {
        return await Task.Run(() =>
        {
            string tessDataPath = ProjectSettings.GlobalizePath("res://tessdata/");
            var tesseractInfo = new ProcessStartInfo {
                FileName = "tesseract",
                Arguments = $"\"{fullFilePath}\" stdout --psm 11 -c tessedit_char_whitelist=0123456789 --tessdata-dir \"{tessDataPath}\" -l eng2",
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
    
    
    public static bool IsTesseractInstalled()
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "tesseract",
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.Start();
            
            process.WaitForExit(3000);

            if (process.ExitCode == 0)
            {
                return true;
            }
        }
        catch (Exception e)
        {
            return false;
        }
        return false;
    }
}