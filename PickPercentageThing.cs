using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Godot;
using OpenCvSharp;

namespace CounterTool;

[GlobalClass]
public partial class PickPercentageThing : Node
{
    public static bool startScanning;
    public static bool scanningForCourseSelected;
    public static bool activelyScanning;
    public static bool gaveUp;

    private double _scanTimer = .5;
    private bool _needToGrabResult;
    private bool _courseSelected;

    static readonly Dictionary<string, int> CurrentTrackCounts = new();

    private async Task CountTracksAsync()
    {
        var results = await Task.Run(async () =>
        {
            await VRAverageCalculator.instance.GetSourceImage("BaseImages/votes.tiff");
            
            List<Task> cropTasks = new();

            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    string bounds = $"{200}:{150}:{(28 + x * 237)}:{(y == 0 ? 25 : 905)}";
                    cropTasks.Add(VRAverageCalculator.instance.ProcessImageAsync(bounds, $"Votes/pick_{1 + x + (y * 12)}.tiff", 0, "BaseImages/votes.tiff"));
                }
            }
            
            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    string bounds = $"{200}:{150}:{ (35 + x * 1659) }:{(26 + 176 + (y * 176))}";
                    cropTasks.Add(VRAverageCalculator.instance.ProcessImageAsync(bounds, $"Votes/pick_{9 + y + (x * 12)}.tiff", 0, "BaseImages/votes.tiff"));
                }
            }
            await Task.WhenAll(cropTasks);
            
            List<Mat> loadedLargerImages = new();
            for (int i = 1; i <= 24; i++)
            {
                string path = $"Votes/pick_{i}.tiff";
                loadedLargerImages.Add(File.Exists(path) ? Cv2.ImRead(path, ImreadModes.Color) : null);
            }

            var tempCounts = new Dictionary<string, int>();
            string[] pngFiles = Directory.GetFiles("IconsForVotes", "*.png");
            
            foreach (var file in pngFiles)
            {
                string trackName = Path.GetFileNameWithoutExtension(file);
                tempCounts[trackName] = CountTemplateMatches(file, loadedLargerImages);
            }

            foreach (var mat in loadedLargerImages)
            {
                mat?.Dispose();
            }

            return tempCounts;
        });

        foreach (var kvp in results)
        {
            CurrentTrackCounts[kvp.Key] = kvp.Value;
        }

        _needToGrabResult = !gaveUp;
        gaveUp = false;
        activelyScanning = false;
    }

    public static int CountTemplateMatches(string smallerImagePath, List<Mat> loadedLargerImages, double threshold = 0.67)
    {
        if (!File.Exists(smallerImagePath))
        {
            GD.PrintErr($"Template file missing: {smallerImagePath}");
            return 0;
        }

        int totalCount = 0;
        using Mat smallerImage = Cv2.ImRead(smallerImagePath, ImreadModes.Color);
        
        foreach (Mat largerImage in loadedLargerImages)
        {
            if (largerImage == null) continue;
            totalCount += CheckMatch(threshold, smallerImage, largerImage);
        }

        return totalCount;
    }

    public static int CheckMatch(double threshold, Mat smallerImage, Mat largerImage)
    {
        if (smallerImage.Width > largerImage.Width || smallerImage.Height > largerImage.Height)
            return 0;

        using Mat matchResult = new Mat();
        Cv2.MatchTemplate(largerImage, smallerImage, matchResult, TemplateMatchModes.CCoeffNormed);
        Cv2.MinMaxLoc(matchResult, out _, out double maxVal, out _, out _);

        return maxVal >= threshold ? 1 : 0;
    }

    public override void _Process(double delta)
    {
        if (activelyScanning)
            return;
        
        if (startScanning)
        {
            VoteScanning(delta);
        }
        else if (scanningForCourseSelected)
        {
            CourseSelectionScanning();
        }
    }

    private void VoteScanning(double delta)
    {
        if (_needToGrabResult)
        {
            _needToGrabResult = false;
            _scanTimer = .5;
            if (CurrentTrackCounts.TryGetValue("SelectionHappening", out int count) && count > 0)
            {
                GD.Print("grabbed votes");
                startScanning = false;
                scanningForCourseSelected = true;
            }
        }
        else
        {
            _scanTimer -= delta;
            if (_scanTimer <= 0)
            {
                // GD.Print("scanning for votes");
                activelyScanning = true;
                CountTracksAsync();
            }   
        }
    }

    private async void CourseSelectionScanning()
    {
        if (_needToGrabResult)
        {
            _needToGrabResult = false;
            scanningForCourseSelected = !_courseSelected;
            if (_courseSelected)
            {
                await ToSignal(GetTree().CreateTimer(5.5f), SceneTreeTimer.SignalName.Timeout);
                GD.Print("scanning for VR automatically");
                VRAverageCalculator.instance.ProcessVR();
            }
        }
        else
        {
            activelyScanning = true;
            RunCourseSelectedAsync();   
        }
    }

    private async Task RunCourseSelectedAsync()
    {
        _courseSelected = await Task.Run(async () =>
        {
            // GD.Print("Checking if course has been selected");
            await VRAverageCalculator.instance.GetSourceImage("BaseImages/CheckForSelected.tiff");
            await VRAverageCalculator.instance.ProcessImageAsync("1000:80:480:240", $"BaseImages/selectedBanner.tiff", 0, "BaseImages/CheckForSelected.tiff");
            
            using Mat smallerImage = Cv2.ImRead("BaseImages/CourseSelected.png");
            using Mat largerImage = Cv2.ImRead("BaseImages/selectedBanner.tiff");
            return CheckMatch(.67, smallerImage, largerImage) > 0;
        });

        activelyScanning = false;
        _needToGrabResult = !gaveUp;
        gaveUp = false;
    }

    public static string GetCurrentTrackVotesForSaveData(int option1, int option2, int option3)
    {
        int option1Count = CurrentTrackCounts.GetValueOrDefault(HistoryCard.trackNumbersToNames[option1 % 30], 0);
        int option2Count = CurrentTrackCounts.GetValueOrDefault(HistoryCard.trackNumbersToNames[option2 % 30], 0);
        int option3Count = CurrentTrackCounts.GetValueOrDefault(HistoryCard.trackNumbersToNames[option3 % 30], 0);
        int randomCount = CurrentTrackCounts.GetValueOrDefault("Random", 0);

        return $"Option1Votes>>{option1Count},Option2Votes>>{option2Count},Option3Votes>>{option3Count},RandomVotes>>{randomCount}";
    }

    public static void GiveUp()
    {
        startScanning = false;
        scanningForCourseSelected = false;
        
        if (activelyScanning)
        {
            gaveUp = true;
        }

        string[] keys = new string[CurrentTrackCounts.Keys.Count];
        CurrentTrackCounts.Keys.CopyTo(keys, 0);
        foreach (var key in keys)
        {
            CurrentTrackCounts[key] = -1;
        }
    }
}