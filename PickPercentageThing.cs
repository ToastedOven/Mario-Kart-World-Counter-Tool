using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Godot;
using OpenCvSharp;

namespace CounterTool;

[GlobalClass]
public partial class PickPercentageThing : Node
{
    static Godot.Collections.Dictionary<string, int> currentTrackCounts = new();
    public static async Task CountTracks()
    {
        await VRAverageCalculator.instance.GetSourceImage("BaseImages/votes.tiff");
        List<Task> tasks = new();
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                string thisBounds = $"{200 * VRAverageCalculator.currentImageWidthRatio}:{150 * VRAverageCalculator.currentImageHeightRatio}:{(28 + x * 237) * VRAverageCalculator.currentImageWidthRatio}:{(y == 0 ? 25 : 905) * VRAverageCalculator.currentImageHeightRatio}";
                tasks.Add(VRAverageCalculator.instance.ProcessImageAsync(thisBounds, $"Votes/pick_{1 + x + (y*12)}.tiff", 0, "BaseImages/votes.tiff"));
            }
        }
        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                string thisBounds = $"{200 * VRAverageCalculator.currentImageWidthRatio}:{150 * VRAverageCalculator.currentImageHeightRatio}:{35 + (x * 1659) * VRAverageCalculator.currentImageWidthRatio}:{(26 + 176 + (y * 176)) * VRAverageCalculator.currentImageHeightRatio}";
                tasks.Add(VRAverageCalculator.instance.ProcessImageAsync(thisBounds, $"Votes/pick_{9 + y + (x*12)}.tiff", 0, "BaseImages/votes.tiff"));
            }
        }
        await Task.WhenAll(tasks.ToArray());
        string[] pngFiles = Directory.GetFiles("IconsForVotes", "*.png");
        foreach (var file in pngFiles)
        {
            string trackName = file.GetBaseName().Split("/")[1];
            if (!currentTrackCounts.ContainsKey(trackName))
            {
                currentTrackCounts.Add(trackName, CountTemplateMatches(file));
            }
            else
            {
                currentTrackCounts[trackName] = CountTemplateMatches(file);
            }
            
            GD.Print($"{trackName}: {currentTrackCounts[trackName]}");
        }
    }
    public static int CountTemplateMatches(string templatePath, double threshold = 0.67)
    {
        if (!File.Exists(templatePath))
        {
            GD.Print($"Template file missing: {templatePath}");
            return 0;
        }

        int totalCount = 0;
        using Mat template = Cv2.ImRead(templatePath, ImreadModes.Color);
        for (int i = 1; i <= 24; i++)
        {
            string cropPath = $"Votes/pick_{i}.tiff";
            if (!File.Exists(cropPath))
                continue;

            using Mat cropImage = Cv2.ImRead(cropPath, ImreadModes.Color);
            if (template.Width > cropImage.Width || template.Height > cropImage.Height)
                continue;

            using Mat matchResult = new Mat();
            Cv2.MatchTemplate(cropImage, template, matchResult, TemplateMatchModes.CCoeffNormed);
            Cv2.MinMaxLoc(matchResult, out _, out double maxVal, out _, out _);

            if (maxVal >= threshold)
            {
                totalCount++;
            }
        }

        return totalCount;
    }

    public static bool startScanning;
    private double scanTimer = 1;
    private bool scanning;
    public async override void _Process(double delta)
    {
        if (startScanning && !scanning)
        {
            scanTimer -= delta;
            if (scanTimer <= 0)
            {
                scanning = true;
                await CountTracks();
                scanning = false;
                scanTimer += 1;
                if (currentTrackCounts.ContainsKey("SelectionHappening") && currentTrackCounts["SelectionHappening"] > 0)
                {
                    GD.Print("grabbed votes, checking one more time");
                    scanning = true;
                    await CountTracks();
                    scanning = false;
                    startScanning = currentTrackCounts["SelectionHappening"] == 0;
                }
            }
        }
    }

    public static string GetCurrentTrackVotesForSaveData(int option1, int option2, int option3)
    {
        var option1Count = currentTrackCounts[HistoryCard.trackNumbersToNames[option1 % 30]];
        var option2Count = currentTrackCounts[HistoryCard.trackNumbersToNames[option2 % 30]];
        var option3Count = currentTrackCounts[HistoryCard.trackNumbersToNames[option3 % 30]];
        var randomCount = currentTrackCounts["Random"];
        return $"Option1Votes>>{option1Count},Option2Votes>>{option2Count},Option3Votes>>{option3Count},RandomVotes>>{randomCount}";
    }

    public static void GiveUp()
    {
        startScanning = false;
        foreach (var key in currentTrackCounts.Keys)
        {
            currentTrackCounts[key] = -1;
        }
    }
}