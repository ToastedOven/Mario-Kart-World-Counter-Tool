using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using OpenCvSharp;

namespace CounterTool;

[GlobalClass]
public partial class PickPercentageThing : Node
{
    public static bool scanningForVotes, scanningForCourseSelected, scanningForPickedCourse, activelyScanning, gaveUp, pickedWasRandom;
    private static readonly HashSet<int> RandomSlotIndices = new();

    private bool _needToGrabResult;
    private bool _courseSelected;
    private string _selectedTrackName = "Unknown";
    public static readonly string iconsForVotesPath = ProjectSettings.GlobalizePath("res://IconsForVotes");

    static readonly Dictionary<string, int> CurrentTrackCounts = new();

    private async Task CountTracksAsync()
    {
        var results = await Task.Run(async () =>
        {
            string baseVotesPath = Path.Combine(VRAverageCalculator.baseImagesPath, "votes.tiff");
            List<Mat> loadedLargerImages = await PrepareSlotImagesAsync(baseVotesPath, "pick");

            RandomSlotIndices.Clear();
            string randomPath = "IconsForVotes/Random.png";
            if (File.Exists(randomPath))
            {
                using Mat randomTemplate = Cv2.ImRead(randomPath, ImreadModes.Color);
                for (int i = 0; i < loadedLargerImages.Count; i++)
                {
                    if (loadedLargerImages[i] != null && CheckMatch(0.67, randomTemplate, loadedLargerImages[i]) > 0)
                    {
                        RandomSlotIndices.Add(i);
                    }
                }
            }

            var tempCounts = Directory.GetFiles("IconsForVotes", "*.png")
                .ToDictionary(
                    Path.GetFileNameWithoutExtension,
                    file => CountTemplateMatches(file, loadedLargerImages)
                );

            foreach (var mat in loadedLargerImages) mat?.Dispose();
            return tempCounts;
        });

        if (!VRAverageCalculator.gotCapture)
        {
            activelyScanning = false;
            _needToGrabResult = false;
            return;
        }

        foreach (var kvp in results) CurrentTrackCounts[kvp.Key] = kvp.Value;
        CompleteScan();
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

    private static double GetMatchConfidence(Mat smallerImage, Mat largerImage)
    {
        if (smallerImage == null || largerImage == null || smallerImage.Width > largerImage.Width || smallerImage.Height > largerImage.Height)
            return 0;

        using Mat matchResult = new Mat();
        Cv2.MatchTemplate(largerImage, smallerImage, matchResult, TemplateMatchModes.CCoeffNormed);
        Cv2.MinMaxLoc(matchResult, out _, out double maxVal, out _, out _);
        return maxVal;
    }

    public static int CheckMatch(double threshold, Mat smallerImage, Mat largerImage)
    {
        return GetMatchConfidence(smallerImage, largerImage) >= threshold ? 1 : 0;
    }

    public override void _Process(double delta)
    {
        if (activelyScanning)
            return;
        
        if (scanningForVotes)
        {
            VoteScanning();
        }
        else if (scanningForCourseSelected)
        {
            CourseSelectionScanning();
        }
        else if (scanningForPickedCourse)
        {
            PickScanning();
        }
    }

    private void VoteScanning()
    {
        if (_needToGrabResult)
        {
            _needToGrabResult = false;
            if (CurrentTrackCounts.TryGetValue("SelectionHappening", out int count) && count > 0)
            {
                GD.Print("grabbed votes");
                scanningForVotes = false;
                scanningForCourseSelected = true;
            }
        }
        else
        {
            activelyScanning = true;
            CountTracksAsync();
        }
    }

    private async void PickScanning()
    {
        if (_needToGrabResult)
        {
            _needToGrabResult = false;

            if (_selectedTrackName != "Unknown")
            {
                int trackNum = HistoryCard.trackNames != null ? HistoryCard.trackNames.IndexOf(_selectedTrackName) : -1;
                GD.Print($"selection is: {_selectedTrackName} (Index: {trackNum}, Random Pick: {pickedWasRandom})");

                if (RecentTrackTracker.instance?.randomCheckbox != null)
                {
                    RecentTrackTracker.instance.randomCheckbox.SetPressed(pickedWasRandom);
                }

                if (trackNum >= 0 && RecentTrackTracker.instance != null)
                {
                    foreach (var track in RecentTrackTracker.instance.recentTracks)
                    {
                        int btnIdx = ButtonThing.buttons.IndexOf(track);
                        if (btnIdx == trackNum || btnIdx == trackNum + 30)
                        {
                            RecentTrackTracker.instance.AddTrack(track);
                            break;
                        }
                    }

                    if (RecentTrackTracker.instance.recentTracks.Count != 4)
                    {
                        RecentTrackTracker.instance.AddTrack(trackNum);
                    }
                }

                scanningForPickedCourse = false;

                if (SettingsPage.autoScanVR)
                {
                    try
                    {
                        await ToSignal(GetTree().CreateTimer(RecentTrackTracker.recentTrackTimeToAutoVrScan), SceneTreeTimer.SignalName.Timeout);
                        GD.Print("scanning for VR automatically");
                        VRAverageCalculator.instance?.ProcessVR();   
                    }
                    catch (Exception ex)
                    {
                        GD.PrintErr($"VR AutoScan timer error: {ex.Message}");
                    }
                }
            }
        }
        else
        {
            activelyScanning = true;
            IdentifySelectedTrackAsync();
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
                scanningForPickedCourse = true;
            }
        }
        else
        {
            activelyScanning = true;
            RunCourseSelectedAsync();   
        }
    }

    private async void IdentifySelectedTrackAsync()
    {
        _selectedTrackName = await Task.Run(async () =>
        {
            string baseVotesFinalPath = Path.Combine(VRAverageCalculator.baseImagesPath, "votes_final.tiff");
            List<Mat> slotMats = await PrepareSlotImagesAsync(baseVotesFinalPath, "selected_pick");
            int winningSlotIndex = -1;

            string borderPath = "IconsForVotes/SelectionHappening.png";
            if (File.Exists(borderPath))
            {
                using Mat borderTemplate = Cv2.ImRead(borderPath, ImreadModes.Color);
                winningSlotIndex = slotMats.FindIndex(m => m != null && CheckMatch(0.67, borderTemplate, m) > 0);
            }

            pickedWasRandom = winningSlotIndex != -1 && RandomSlotIndices.Contains(winningSlotIndex);

            string trackResult = "Unknown";

            if (winningSlotIndex != -1)
            {
                Mat winningSlotMat = slotMats[winningSlotIndex];
                double highestScore = 0.67;

                foreach (var file in Directory.GetFiles("IconsForVotes", "*.png"))
                {
                    string trackName = Path.GetFileNameWithoutExtension(file);
                    if (trackName == "SelectionHappening" || trackName == "Random") continue;

                    using Mat trackTemplate = Cv2.ImRead(file, ImreadModes.Color);
                    double score = GetMatchConfidence(trackTemplate, winningSlotMat);

                    if (score > highestScore)
                    {
                        highestScore = score;
                        trackResult = trackName;
                    }
                }
            }

            foreach (var mat in slotMats) mat?.Dispose();
            return trackResult;
        });

        CompleteScan();
    }

    private static async Task<List<Mat>> PrepareSlotImagesAsync(string sourceFile, string prefix)
    {
        await VRAverageCalculator.instance.GetSourceImage(sourceFile);
        List<Task> cropTasks = new();
        CreateVoteImages(cropTasks, sourceFile, prefix);
        await Task.WhenAll(cropTasks);
        return LoadSlotImages(prefix);
    }

    private void CompleteScan()
    {
        activelyScanning = false;
        _needToGrabResult = !gaveUp;
        gaveUp = false;
    }

    private static List<Mat> LoadSlotImages(string prefix)
    {
        List<Mat> mats = new();
        for (int i = 1; i <= 24; i++)
        {
            string path = Path.Combine(VRAverageCalculator.ocrImagesPath, $"{prefix}_{i}.tiff");
            
            Mat loadedMat = null;
            if (File.Exists(path))
            {
                try
                {
                    loadedMat = Cv2.ImRead(path, ImreadModes.Color);
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"Failed to read image {path}: {ex.Message}");
                }
            }
            mats.Add(loadedMat);
        }
        return mats;
    }

    private static void CreateVoteImages(List<Task> cropTasks, string filePath, string prefix)
    {
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                string bounds = $"{200}:{150}:{(28 + x * 237)}:{(y == 0 ? 25 : 905)}";
                string outputPath = Path.Combine(VRAverageCalculator.ocrImagesPath, $"{prefix}_{1 + x + (y * 12)}.tiff");
                cropTasks.Add(VRAverageCalculator.instance.ProcessImageAsync(bounds, outputPath, 0, filePath));
            }
        }

        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                string bounds = $"{200}:{150}:{(35 + x * 1659)}:{(26 + 176 + (y * 176))}";
                string outputPath = Path.Combine(VRAverageCalculator.ocrImagesPath, $"{prefix}_{9 + y + (x * 12)}.tiff");
                cropTasks.Add(VRAverageCalculator.instance.ProcessImageAsync(bounds, outputPath, 0, filePath));
            }
        }
    }

    private async Task RunCourseSelectedAsync()
    {
        _courseSelected = await Task.Run(async () =>
        {
            string checkForSelectedPath = Path.Combine(VRAverageCalculator.baseImagesPath, "CheckForSelected.tiff");
            string selectedBannerPath = Path.Combine(VRAverageCalculator.baseImagesPath, "selectedBanner.tiff");
            string courseSelectedPath = Path.Combine(VRAverageCalculator.baseImagesPath, "CourseSelected.png");

            await VRAverageCalculator.instance.GetSourceImage(checkForSelectedPath);
            await VRAverageCalculator.instance.ProcessImageAsync("1000:80:480:240", selectedBannerPath, 0, checkForSelectedPath);
            
            using Mat smallerImage = File.Exists(courseSelectedPath) ? Cv2.ImRead(courseSelectedPath) : null;
            using Mat largerImage = File.Exists(selectedBannerPath) ? Cv2.ImRead(selectedBannerPath) : null;
            return CheckMatch(.67, smallerImage, largerImage) > 0;
        });

        CompleteScan();
    }

    public static string GetCurrentTrackVotesForSaveData(int option1, int option2, int option3)
    {
        int option1Count = CurrentTrackCounts.GetValueOrDefault(HistoryCard.trackNames[option1 % 30], 0);
        int option2Count = CurrentTrackCounts.GetValueOrDefault(HistoryCard.trackNames[option2 % 30], 0);
        int option3Count = CurrentTrackCounts.GetValueOrDefault(HistoryCard.trackNames[option3 % 30], 0);
        int randomCount = CurrentTrackCounts.GetValueOrDefault("Random", 0);

        return $"Option1Votes>>{option1Count},Option2Votes>>{option2Count},Option3Votes>>{option3Count},RandomVotes>>{randomCount}";
    }

    public static void GiveUp()
    {
        scanningForVotes = false;
        scanningForCourseSelected = false;
        scanningForPickedCourse = false;
        
        if (activelyScanning)
        {
            gaveUp = true;
        }

        pickedWasRandom = false;
        RandomSlotIndices.Clear();

        foreach (var key in CurrentTrackCounts.Keys.ToList())
        {
            CurrentTrackCounts[key] = -1;
        }
    }
}