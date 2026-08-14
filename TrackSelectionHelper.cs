using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CounterTool.Utils;
using Godot;
using OpenCvSharp;

namespace CounterTool;

public static class TrackSelectionHelper
{
    private static Mat? _randomTrackSelectionTemplate;
    private static TrackTemplate[] _trackTemplates = [];
    private static Mat _trackTemplateMask = new();
    private static Mat _arrowSegmentTemplate = new();
    private static Mat _arrowSegmentTemplateMask = new();

    public static void LoadTemplatesAndMasks()
    {
        if (_trackTemplates.Length == 0)
        {
            _trackTemplateMask.Dispose();
            _trackTemplateMask = Cv2.ImRead("Templates/Tracks/Mask.png", ImreadModes.Grayscale);
            
            _trackTemplates = Directory.GetFiles("Templates/Tracks/", "*.png")
                .AsParallel()
                .Where(file => !file.EndsWith("Mask.png"))
                .Select(file =>
                {
                    var template = Cv2.ImRead(file, ImreadModes.Color);
                    
                    return new TrackTemplate(file, template);
                })
                .ToArray();
            
            _arrowSegmentTemplate.Dispose();
            _arrowSegmentTemplate = Cv2.ImRead("Templates/Track-Selection-Arrow-Segment.png", ImreadModes.Grayscale);
            
            _arrowSegmentTemplateMask.Dispose();
            _arrowSegmentTemplateMask = Cv2.ImRead("Templates/Track-Selection-Arrow-Segment-Mask.png", ImreadModes.Grayscale);
        }
        InitializeTemplate();
    }
    
    public static bool IsInTrackSelection(Mat frame, double threshold)
    {
        _randomTrackSelectionTemplate ??= Cv2.ImRead("Templates/Random.png");

        using var croppedFrame = frame[1008..1047, 1644..1877];
        using var result = new Mat();
        Cv2.MatchTemplate(croppedFrame, _randomTrackSelectionTemplate, result, TemplateMatchModes.CCoeffNormed);
        Cv2.MinMaxLoc(result, out _, out Point max);

        return result.At<float>(max.Y, max.X) >= threshold;
    }

    public static TrackSelectionResult[] DetectInFrame(Mat frame, ConcurrentBag<TrackSelectionResult> trackSelectionResults, int attempt)
    {
        const int croppedWidth = 1920 / 5;
        const int croppedHeight = 1080 / 3;

        var center = new Point(1920 / 2, 1080 / 2);

        using var centerCrop = frame[(center.Y - croppedHeight / 2)..(center.Y + croppedHeight / 2), (center.X - croppedWidth / 2)..(center.X + croppedWidth / 2)];

        using var contrastedCrop = Cv2.ApplyClahe(centerCrop, 4.0, new Size(8.0, 8.0));
        
        using var grayCrop = new Mat();
        Cv2.CvtColor(contrastedCrop, grayCrop, ColorConversionCodes.BGR2GRAY);
        using var thresholdCrop = new Mat();
        Cv2.Threshold(grayCrop, thresholdCrop, 180, 255, ThresholdTypes.Binary);
        
        var result = Cv2.InvariantMatchTemplate(thresholdCrop, _arrowSegmentTemplate, TemplateMatchModes.CCoeffNormed, ..360, 1, 10, 0.50, 0, mask: _arrowSegmentTemplateMask);

        var results = new List<TrackSelectionResult>();

        var tracksFoundByCount = trackSelectionResults.ToArray()
            .GroupBy(item => item.trackName)
            .Select(h => (h.Key, Count: h.Count(), Bounds: h.First().bounds))
            .ToArray();

        var existingDetectedTracks = _trackTemplates
            .Select(item => tracksFoundByCount.FirstOrDefault(track => track.Key == item.Name))
            .Where(item => item.Count >= 4)
            .ToArray();

        if (existingDetectedTracks.Length >= 3)
        {
            GD.Print($"Likely confident we have all tracks detected! {attempt}");
        }

        var filter = _trackTemplates
            .Where(item =>
            {
                return (tracksFoundByCount.All(track => track.Key != item.Name) && attempt >= 8) || (tracksFoundByCount.FirstOrDefault(track => track.Key == item.Name).Count >= 4);
            })
            .Select(item => item.Name)
            .ToArray();

        (string track, Rect bounds)[] detectedTracks = existingDetectedTracks.Length >= 3
            ? existingDetectedTracks.Select(item => (item.Key, item.Bounds)).ToArray()
            : DetectTracks(frame, 0.67, filter, attempt);
        
        foreach (var (track, bounds) in detectedTracks)
        {
            var connectedPathway = false;

            foreach (var (_, _, _, angle, score) in result)
            {
                var angleRadians = Mathf.DegToRad(-angle);
                var dir = -new Vec2f(Mathf.Sin(angleRadians), Mathf.Cos(angleRadians)).Normalized();

                if (!bounds.IntersectsWith(center, dir))
                    continue;

                var arrowEnd = center + (dir * 150).ToPoint();
                var arrowLength = arrowEnd.DistanceTo(arrowEnd);
                Cv2.ArrowedLine(frame, center, arrowEnd, Scalar.Green, 2, LineTypes.Link4);

                connectedPathway = true;
                break;
            }
            Cv2.Rectangle(frame, bounds.TopLeft, bounds.BottomRight, Scalar.Red, 2, LineTypes.Link4);
            Cv2.PutText(frame, track, bounds.TopLeft, HersheyFonts.HersheyComplex, 1, Scalar.Red);

            results.Add(new TrackSelectionResult(track, connectedPathway, bounds));
        }

        Cv2.ImWrite($"Debug-Out/Track-Selection-Tracks-{attempt}.tiff", frame);
        
        return results.ToArray();
    }

    private static (string track, Rect bounds)[] DetectTracks(Mat frame, double threshold, string[] tracksToExclude, int attempt)
    {
        var trackCards = DetectTrackCards(frame);
        var detectedTracks = new ConcurrentBag<(string track, Rect bounds)>();

        Parallel.ForEach(trackCards, trackCard =>
        {
            string? bestMatchName = null;
            double highestScore = threshold;
            Point bestLoc = default;
            Size bestTemplateSize = default;

            foreach (var item in _trackTemplates)
            {
                if (tracksToExclude.Contains(item.Name)) 
                    continue;

                if (trackCard.TextRegionCrop.Width < item.Template.Width || 
                    trackCard.TextRegionCrop.Height < item.Template.Height)
                    continue;

                using var output = new Mat();
                Cv2.MatchTemplate(trackCard.TextRegionCrop, item.Template, output, TemplateMatchModes.CCoeffNormed);
                Cv2.MinMaxLoc(output, out _, out double maxVal, out _, out Point maxLoc);

                if (maxVal > highestScore)
                {
                    highestScore = maxVal;
                    bestMatchName = item.Name;
                    bestLoc = maxLoc;
                    bestTemplateSize = item.Template.Size();
                }
            }

            if (bestMatchName != null)
            {
                detectedTracks.Add((bestMatchName, new Rect(bestLoc + trackCard.FullCardRect.TopLeft, bestTemplateSize)));
            }
        });

        return detectedTracks.ToArray();
    }

    public struct TrackSelectionResult(string trackName, bool connectedPathway, Rect bounds)
    {
        public readonly string trackName = trackName;
        public readonly bool connectedPathway = connectedPathway;
        public readonly Rect bounds = bounds;
    }

    private record TrackTemplate(string Name, Mat Template);
    
    public static List<DetectedCard> DetectTrackCards(Mat inputFrame)
    {
        var matchedCards = new List<DetectedCard>();

        string debugFolder = Path.Combine(Directory.GetCurrentDirectory(), "Debug-Out");
        Directory.CreateDirectory(debugFolder);

        using var debugFrame = inputFrame.Clone();
    
        using var gray = new Mat();
        Cv2.CvtColor(inputFrame, gray, ColorConversionCodes.BGR2GRAY);
    
        using var blurred = new Mat();
        Cv2.GaussianBlur(gray, blurred, new Size(3, 3), 0);

        using var edges = new Mat();
        Cv2.Canny(blurred, edges, 50, 150);

        using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3));
        using var dilatedEdges = new Mat();
        Cv2.Dilate(edges, dilatedEdges, kernel);
        
        Cv2.FindContours(
            dilatedEdges, 
            out Point[][] candidateContours, 
            out _, 
        RetrievalModes.List, 
        ContourApproximationModes.ApproxSimple
        );

        var candidateRects = new List<Rect>();

        foreach (var candidate in candidateContours)
        {
            Rect rect = Cv2.BoundingRect(candidate);

            if (rect.Width < 220 || rect.Width > 500) continue;
            if (rect.Height < 140 || rect.Height > 350) continue;

            double aspectRatio = (double)rect.Width / rect.Height;
            if (aspectRatio < 1.1 || aspectRatio > 1.8) continue;

            candidateRects.Add(rect);
        }

        var deduplicatedRects = GroupOverlappingRects(candidateRects, 0.5);

        foreach (var rect in deduplicatedRects)
        {
            Rect safeRect = rect.Intersect(new Rect(0, 0, inputFrame.Width, inputFrame.Height));
            Mat croppedCard = new Mat(inputFrame, safeRect).Clone();

            matchedCards.Add(new DetectedCard
            {
                FullCardRect = safeRect,
                TextRegionCrop = croppedCard
            });

            // // Draw green bounding boxes on debug frame
            // Cv2.Rectangle(debugFrame, safeRect, new Scalar(0, 255, 0), 2);
            // Cv2.PutText(
            //     debugFrame, 
            //     $"{safeRect.Width}x{safeRect.Height}", 
            //     new Point(safeRect.X, Math.Max(25, safeRect.Y - 8)), 
            //     HersheyFonts.HersheyComplex, 
            //     0.6, 
            //     new Scalar(0, 255, 0), 
            //     2
            // );
        }

        // Save annotated debug output
        // Cv2.ImWrite(Path.Combine(debugFolder, "CardDetector_ContoursDebug.png"), debugFrame);

        return matchedCards;
    }

    private static List<Rect> GroupOverlappingRects(List<Rect> rects, double overlapThreshold)
    {
        var result = new List<Rect>();
        var ordered = rects.OrderByDescending(r => r.Width * r.Height).ToList();

        while (ordered.Count > 0)
        {
            var current = ordered[0];
            result.Add(current);
            ordered.RemoveAt(0);

            ordered.RemoveAll(r =>
            {
                var intersect = current.Intersect(r);
                double intersectionArea = intersect.Width * intersect.Height;
                double minArea = Math.Min(current.Width * current.Height, r.Width * r.Height);
                return (intersectionArea / minArea) > overlapThreshold;
            });
        }

        return result;
    }
    
    public struct DetectedCard
    {
        public Rect FullCardRect;
        public Mat TextRegionCrop;
    }

    private static Point[]? borderTemplateContour;
    
    public static void InitializeTemplate(string borderImagePath = "res://Templates/TrackBorder.png")
    {
        string absolutePath = ProjectSettings.GlobalizePath(borderImagePath);

        if (!File.Exists(absolutePath))
        {
            GD.PrintErr($"[TrackDetector] Border template missing at: {absolutePath}");
            return;
        }

        using var templateMat = Cv2.ImRead(absolutePath, ImreadModes.Grayscale);
        using var binary = new Mat();
        Cv2.Threshold(templateMat, binary, 100, 255, ThresholdTypes.Binary);

        Cv2.FindContours(
            binary, 
            out Point[][] contours, 
            out _, 
            RetrievalModes.External, 
            ContourApproximationModes.ApproxSimple
        );

        if (contours.Length > 0)
        {
            borderTemplateContour = contours[0];
            GD.Print("[TrackDetector] Border template successfully initialized!");
        }
        else
        {
            GD.PrintErr("[TrackDetector] Failed to extract contours from border template image.");
        }
    }
}