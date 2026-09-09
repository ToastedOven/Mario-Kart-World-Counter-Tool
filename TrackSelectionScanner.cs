using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CounterTool.Utils;
using Godot;
using OpenCvSharp;

namespace CounterTool;

[GlobalClass]
public partial class TrackSelectionScanner : Node
{
    private const int MaxFrames = 12;
    private const double TimeBetweenCaptures = 1.0 / 12f;
    private double timer;
    private int attempts;
    private int framesCollected;
    private int framesFinished;
    private readonly ConcurrentQueue<Mat> frames = [];
    private readonly ConcurrentBag<TrackSelectionHelper.TrackSelectionResult> trackSelectionResults = [];
    private int framesWithoutTrackSelection;
    public static TrackSelectionScanner instance;

    private bool tryScanning;
    
    public void TestScan()
    {
        // CaptureHelper.GetFrame();
        tryScanning = true;
        RecentTrackTracker.instance.ClearTracks();
    }

    public override void _Ready()
    {
        instance = this;
        TrackSelectionHelper.LoadTemplatesAndMasks();
        
        // Call this in ready to prepare capture
        using var frame = new Mat();
        CameraSetup.ReadFrame(frame);
        TrackSelectionHelper.IsInTrackSelection(frame, 0.67);
    }

    public override void _Process(double delta)
    {
        timer += delta;

        if (timer >= TimeBetweenCaptures)
        {
            timer -= TimeBetweenCaptures;
            
            var frame = new Mat();
            CameraSetup.ReadFrame(frame);

            if (TrackSelectionHelper.IsInTrackSelection(frame, 0.67) && tryScanning && framesCollected < MaxFrames)
            {
                if (framesCollected == 0)
                {
                    attempts = 0;
                    frames.Clear();
                    trackSelectionResults.Clear();
                }
                
                GD.Print("In TrackSelection");
                frames.Enqueue(frame);
                framesCollected++;
            }
            else
            {
                frame.Dispose();
                if (framesCollected <= 0)
                    return;

                framesWithoutTrackSelection++;

                if (framesWithoutTrackSelection >= 4)
                {
                    if (frames.IsEmpty && framesFinished != 0 && framesFinished == framesCollected)
                    {
                        // var process = Callable.From(() =>
                        // {
                        //     
                        // });
                        // process.CallDeferred();
                        
                        ProcessResults(trackSelectionResults.ToArray());
                        trackSelectionResults.Clear();
                        
                        tryScanning = false;
                        framesWithoutTrackSelection = 0;
                        framesCollected = 0;
                        framesFinished = 0;
                    }
                    
                    // var frameQueue = frames.ToArray();
                    // frames.Clear();
                
                    // ThreadPool.QueueUserWorkItem(_ =>
                    // {
                    //     var attempt = 0;
                    //     frameQueue.AsParallel()
                    //         .WithExecutionMode(ParallelExecutionMode.ForceParallelism
                    //         )
                    //         .ForAll(result =>
                    //         {
                    //             var results = TrackSelectionHelper.DetectInFrame(result, trackSelectionResults, Interlocked.Increment(ref attempt));
                    //
                    //             foreach (var trackResult in results)
                    //                 trackSelectionResults.Add(trackResult);
                    //         
                    //             result.Dispose();
                    //         });
                    //     // foreach (var result in frameQueue)
                    //     // {
                    //     //     
                    //     // }
                    //
                    //     // var process = Callable.From(() =>
                    //     // {
                    //     //     ProcessResults(trackSelectionResults.ToArray());
                    //     //     trackSelectionResults.Clear();
                    //     // });
                    //     // process.CallDeferred();
                    // });
                }
            }
        }

        if (frames.TryDequeue(out var result))
        {
            var attempt = attempts++;
            
            ThreadPool.QueueUserWorkItem(_ =>
            {
                var results = TrackSelectionHelper.DetectInFrame(result, trackSelectionResults, attempt);
                
                foreach (var trackResult in results)
                    trackSelectionResults.Add(trackResult);
                            
                result.Dispose();
                Interlocked.Increment(ref framesFinished);
            });
        }
    }

    private void ProcessResults(TrackSelectionHelper.TrackSelectionResult[] results)
    {
        var tracksFound = new Dictionary<string, int>();
        var tracksConnected = new Dictionary<string, bool>();

        foreach (var result in results)
        {
            if (tracksFound.TryGetValue(result.trackName, out var numFound))
            {
                numFound++;
            }
            else
            {
                numFound = 1;
            }
            tracksFound[result.trackName] = numFound;

            if (result.connectedPathway)
                tracksConnected[result.trackName] = true;
        }

        var tracks = tracksFound.ToList();
        tracks.Sort((left, right) => left.Value.CompareTo(right.Value));
        tracks.Reverse();
        foreach (var (track, count) in tracks.Take(3))
        {
            var connected = tracksConnected.GetValueOrDefault(track, false);
            GD.Print($"Found Track: {track} Count: {count} Connected: {connected}");
        }

        if (tracks.Count < 3)
        {
            GD.Print($"Failed to detect tracks! Found: {tracks.Count}");
            return;
        }

        foreach (var (trackFile, _) in tracks.Take(3))
        {
            var track = trackFile.Split("/").Last().Replace(".png", "").Trim();
            var trackIndex = HistoryCard.trackNames.IndexOf(track.Replace("Great Block Ruins", "Great ? Block Ruins"));
            if (trackIndex == -1)
                return;
            if (tracksConnected.GetValueOrDefault(trackFile, false))
                trackIndex += ControlManager.TRACKCOUNT;
            
            RecentTrackTracker.instance.AddTrack(trackIndex, true);
        }
        
    }
}