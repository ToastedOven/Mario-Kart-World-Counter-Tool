using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
    private const int TrackTemplateThreshold = 90;
    private const double TrackTemplateClipLimit = 8.0;
    private static readonly Size TrackTemplateGridSize = new(9, 9);

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
                    using var image = Cv2.ImRead(file);
                    // var template = new Mat();
                    using var contrastedTemplate = Cv2.ApplyClahe(image, TrackTemplateClipLimit, TrackTemplateGridSize);
                    using var grayscale = new Mat();
                    Cv2.CvtColor(contrastedTemplate, grayscale, ColorConversionCodes.BGR2GRAY);
                    using var template = new Mat();
                    Cv2.Threshold(grayscale, template, TrackTemplateThreshold, 255, ThresholdTypes.BinaryInv);
                    var maskedTemplate = new Mat();
                    Cv2.BitwiseAnd(template, _trackTemplateMask, maskedTemplate);
                    
                    Cv2.ImWrite($"Debug-Out/Template-{file.Split("/").Last()}.tiff", maskedTemplate);
                    return new TrackTemplate(file, maskedTemplate);
                })
                .ToArray();
            
            _arrowSegmentTemplate.Dispose();
            _arrowSegmentTemplate = Cv2.ImRead("Templates/Track-Selection-Arrow-Segment.png", ImreadModes.Grayscale);
            
            _arrowSegmentTemplateMask.Dispose();
            _arrowSegmentTemplateMask = Cv2.ImRead("Templates/Track-Selection-Arrow-Segment-Mask.png", ImreadModes.Grayscale);
        }
    }
    
    public static bool IsInTrackSelection(Mat frame, double threshold)
    {
        _randomTrackSelectionTemplate ??= Cv2.ImRead("Templates/Random - Track Selection.png");
        
        // _videoCapture ??= new VideoCapture(CameraSetup.currentCameraIndex);
        // if (!_videoCapture.IsOpened())
        //     return false;
        //
        // CameraSetup.ConfigureResolution(_videoCapture);
        // using var frame = new Mat();
        // _videoCapture.Read(frame);
        // CameraSetup.ApplyTransforms(frame);
        // using var newFrame = new Mat();
        // Cv2.Resize(frame, newFrame, new Size(1920, 1080));

        // using var frame = new Mat();
        // CameraSetup.ReadFrame(frame);

        using var croppedFrame = frame[1008..1047, 1644..1877];
        using var result = new Mat();
        Cv2.MatchTemplate(croppedFrame, _randomTrackSelectionTemplate, result, TemplateMatchModes.CCoeffNormed);
        Cv2.MinMaxLoc(result, out _, out Point max);

        return result.At<float>(max.Y, max.X) >= threshold;
    }

    public static TrackSelectionResult[] DetectInFrame(Mat frame, ConcurrentBag<TrackSelectionResult> trackSelectionResults, int attempt)
    {
        // var stopwatch = new Stopwatch();
        // stopwatch.Start();
        
        // _videoCapture ??= new VideoCapture(CameraSetup.currentCameraIndex);
        // if (!_videoCapture.IsOpened())
        //     return [];
        //
        // CameraSetup.ConfigureResolution(_videoCapture);
        // using var frame = new Mat();
        // _videoCapture.Read(frame);
        // CameraSetup.ApplyTransforms(frame);
        // using var newFrame = new Mat();
        // Cv2.Resize(frame, newFrame, new Size(1920, 1080));

        const int croppedWidth = 1920 / 5;
        const int croppedHeight = 1080 / 3;

        var center = new Point(1920 / 2, 1080 / 2);

        // using var frame = new Mat();
        // CameraSetup.ReadFrame(frame);

        using var centerCrop = frame[(center.Y - croppedHeight / 2)..(center.Y + croppedHeight / 2), (center.X - croppedWidth / 2)..(center.X + croppedWidth / 2)];
        
        // stopwatch.LogAndRestart("Cropped image");

        // stopwatch.LogAndRestart("Load arrow templates");

        using var contrastedCrop = Cv2.ApplyClahe(centerCrop, 4.0, new Size(8.0, 8.0));
        
        // Cv2.ImWrite($"Debug-Out/Track-Selection-Arrow-Contrast-{attempt}.tiff", contrastedCrop);
        
        using var grayCrop = new Mat();
        Cv2.CvtColor(contrastedCrop, grayCrop, ColorConversionCodes.BGR2GRAY);
        using var thresholdCrop = new Mat();
        Cv2.Threshold(grayCrop, thresholdCrop, 180, 255, ThresholdTypes.Binary);
        
        // Cv2.ImWrite($"Debug-Out/Track-Selection-Arrow-Threshold-{attempt}.tiff", thresholdCrop);
        
        var result = Cv2.InvariantMatchTemplate(thresholdCrop, _arrowSegmentTemplate, TemplateMatchModes.CCoeffNormed, ..360, 1, 10, 0.50, 0, mask: _arrowSegmentTemplateMask);
        
        // stopwatch.LogAndRestart("Detect arrow segments");
    
        // using var resultImage = new Mat(newFrame.Width, newFrame.Height, MatType.CV_32FC4);
    
        // var size = _arrowSegmentTemplate.Size();

        // using var font = new FontFace("Consolas");
        //
        // using var debugCenterCrop = Mat.ZerosMat(centerCrop.Size(), centerCrop.Type());
        // centerCrop.CopyTo(debugCenterCrop);
        //     
        // foreach (var (x, y, size, angle, score) in result)
        // {
        //     var min = new Point(x, y);
        //     var max = new Point(x + size.Width, y + size.Height);
        //
        //     var rotatedRect = new RotatedRect(min + new Point(size.Width / 2f, size.Height / 2f), size, angle);
        //     // var rotatedRect = new RotatedRect(min, size, angle);
        //
        //     var points = rotatedRect.Points()
        //         .Select(point => new Point(Mathf.FloorToInt(point.X), Mathf.FloorToInt(point.Y)))
        //         .ToArray();
        //         
        //     Cv2.Line(debugCenterCrop, points[0], points[1], Scalar.Red, 1, LineTypes.Link4);
        //     Cv2.Line(debugCenterCrop, points[1], points[2], Scalar.Red, 1, LineTypes.Link4);
        //     Cv2.Line(debugCenterCrop, points[2], points[3], Scalar.Red, 1, LineTypes.Link4);
        //     Cv2.Line(debugCenterCrop, points[3], points[0], Scalar.Red, 1, LineTypes.Link4);
        //         
        //     var angleRadians = Mathf.DegToRad(-angle);
        //     var dir = -new Vec2f(Mathf.Sin(angleRadians), Mathf.Cos(angleRadians)).Normalized();
        //         
        //     Cv2.ArrowedLine(debugCenterCrop, rotatedRect.Center.ToPoint(), (rotatedRect.Center + (dir * 20).ToPoint()).ToPoint(), Scalar.Red, 2, LineTypes.Link4);
        //     
        //     Cv2.PutText(debugCenterCrop, $"Angle: {angle}, Score: {score}", (max - min) - new Point(0, 10), Scalar.Red, font, 12);
        // }
        //
        // // stopwatch.LogAndRestart("Process arrow results");
        //
        // Cv2.ImWrite($"Debug-Out/Track-Selection-Arrow-Matches-{attempt}.tiff", debugCenterCrop);
        
        // stopwatch.LogAndRestart("Write arrow results");

        var results = new List<TrackSelectionResult>();

        var tracksFoundByCount = trackSelectionResults.ToArray()
            .GroupBy(item => item.trackName)
            .Select(h => (h.Key, Count: h.Count(), Bounds: h.First().bounds))
            .ToArray();

        var existingDetectedTracks = _trackTemplates
            .Select(item => tracksFoundByCount.FirstOrDefault(track => track.Key == item.Name))
            .Where(item => item.Count >= 4)
            .ToArray();
        
        // GD.Print($"Existing tracks with over 4 detections: {}");

        if (existingDetectedTracks.Length >= 3)
        {
            GD.Print("Likely confidant we have all tracks detected!");
        }

        var filter = _trackTemplates
            .Where(item =>
            {
                return (tracksFoundByCount.All(track => track.Key != item.Name) && attempt >= 8) || (tracksFoundByCount.FirstOrDefault(track => track.Key == item.Name).Count >= 4);
            })
            .Select(item => item.Name)
            .ToArray();

        using var contrastedFrame = Cv2.ApplyClahe(frame, TrackTemplateClipLimit, TrackTemplateGridSize);

        (string track, Rect bounds)[] detectedTracks = existingDetectedTracks.Length >= 3
            ? existingDetectedTracks.Select(item => (item.Key, item.Bounds)).ToArray()
            : DetectTracks(contrastedFrame, 0.4, filter, attempt);
        // stopwatch.LogAndRestart("Detect Tracks");
        
        foreach (var (track, bounds) in detectedTracks)
        {
            // Cv2.Rectangle(frame, bounds, Scalar.Red, 1, LineTypes.Link4);
            // Cv2.PutText(frame, $"Track: {track}", bounds.TopLeft - new Point(0, 8), Scalar.Red, font, 16);

            var connectedPathway = false;

            foreach (var (_, _, _, angle, score) in result)
            {
                var angleRadians = Mathf.DegToRad(-angle);
                var dir = -new Vec2f(Mathf.Sin(angleRadians), Mathf.Cos(angleRadians)).Normalized();

                if (!bounds.IntersectsWith(center, dir))
                    continue;

                // var arrowEnd = center + (dir * 150).ToPoint();
                // var arrowLength = arrowEnd.DistanceTo(arrowEnd);
                // Cv2.ArrowedLine(frame, center, arrowEnd, Scalar.Green, 2, LineTypes.Link4);
                // Cv2.PutText(frame, $"Score: {score}", arrowEnd - new Point(arrowLength / 2, 6), Scalar.Red, font, 12);

                connectedPathway = true;
                break;
            }

            results.Add(new TrackSelectionResult(track, connectedPathway, bounds));
                
            // GD.Print($"Detected Track: {track} {(connectedPathway ? "Intermission" : "3-Lap")}");
        }
        
        // stopwatch.LogAndRestart("Process track results");

        // Cv2.ImWrite($"Debug-Out/Track-Selection-Tracks-{attempt}.tiff", frame);
        
        // stopwatch.LogAndRestart("Write track results");

        return results.ToArray();
    }

    private static (string track, Rect bounds)[] DetectTracks(Mat frame, double threshold, string[] tracksToExclude, int attempt)
    {
        using var grayFrame = new Mat();
        Cv2.CvtColor(frame, grayFrame, ColorConversionCodes.BGR2GRAY);
        using var binaryFrame = new Mat();
        Cv2.Threshold(grayFrame, binaryFrame, TrackTemplateThreshold, 255, ThresholdTypes.BinaryInv);
        
        var detectedTracks = new ConcurrentBag<(string track, Rect bounds)>();
        
        _trackTemplates
            .AsParallel()
            .Where(item => tracksToExclude.All(track => track != item.Name))
            .ForAll(item =>
            {
                using var output = new Mat();
                Cv2.MatchTemplate(binaryFrame, item.Template, output, TemplateMatchModes.CCoeffNormed);
                Cv2.MinMaxLoc(output, out _, out Point max);

                if (output.At<float>(max.Y, max.X) > threshold)
                {
                    detectedTracks.Add((item.Name, new Rect(max, item.Template.Size())));
                }
            });

        // Cv2.ImWrite($"Debug-Out/Track-Selection-Threshold-Binary-{attempt}.tiff", binaryFrame);
        
        return detectedTracks.ToArray();
    }

    public struct TrackSelectionResult(string trackName, bool connectedPathway, Rect bounds)
    {
        public readonly string trackName = trackName;
        public readonly bool connectedPathway = connectedPathway;
        public readonly Rect bounds = bounds;
    }

    private record TrackTemplate(string Name, Mat Template);
}