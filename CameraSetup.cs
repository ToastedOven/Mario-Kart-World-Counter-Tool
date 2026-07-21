using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Godot;
using OpenCvSharp;

namespace CounterTool;

[GlobalClass]
public partial class CameraSetup : Node
{
    [Export] Button prevIndex, nextIndex, rotate, flip;
    [Export] private Control parentControl;
    [Export] private TextureRect previewImage;
    [Export] private Label currentCameraLabel;
    [Export] Texture2D fallBackTexture;
    public static int currentCamera;
    public static CameraSetup instance;
    public static int currentRotation = 0;
    public static int currentFlip = 0;

    public override void _Ready()
    {
        instance = this;
        prevIndex.Pressed += () => { currentCamera--; Preview(); };
        nextIndex.Pressed += () => { currentCamera++; Preview(); };
        
        rotate.Pressed += () => { currentRotation = (currentRotation + 1) % 4; Preview(); };
        flip.Pressed += () => { currentFlip = (currentFlip + 1) % 4; Preview(); };
    }

    public void Preview()
    {
        currentCameraLabel.Text = $"Current Index: {currentCamera}";
        var resolutionsToTest = new List<(int width, int height)>
        {
            (3840, 2160), // 4K
            (2560, 1440), // 2K
            (1920, 1080), // 1080p
            (1280, 720),  // 720p
            (640, 480)    // 480p
        };
        
        using var capture = new VideoCapture(currentCamera);
        using var frame = new Mat();
        
        if (capture.IsOpened())
        {
            foreach (var resolution in resolutionsToTest)
            {
                capture.Set(VideoCaptureProperties.FrameWidth, resolution.width);
                capture.Set(VideoCaptureProperties.FrameHeight, resolution.height);
                int actualWidth = (int)capture.Get(VideoCaptureProperties.FrameWidth);
                int actualHeight = (int)capture.Get(VideoCaptureProperties.FrameHeight);
                if (actualWidth == resolution.width && actualHeight == resolution.height)
                {
                    break;
                }
            }
            
            capture.Read(frame);

            if (!frame.Empty())
            {
                // 1. Apply current 90-degree step rotation transformations
                if (currentRotation == 1)
                {
                    Cv2.Rotate(frame, frame, RotateFlags.Rotate90Clockwise);
                }
                else if (currentRotation == 2)
                {
                    Cv2.Rotate(frame, frame, RotateFlags.Rotate180);
                }
                else if (currentRotation == 3)
                {
                    Cv2.Rotate(frame, frame, RotateFlags.Rotate90Counterclockwise);
                }

                // 2. Apply flip transformations
                if (currentFlip == 1)
                {
                    Cv2.Flip(frame, frame, FlipMode.Y); // Horizontal flip (mirror)
                }
                else if (currentFlip == 2)
                {
                    Cv2.Flip(frame, frame, FlipMode.X); // Vertical flip
                }
                else if (currentFlip == 3)
                {
                    Cv2.Flip(frame, frame, FlipMode.XY); // Both horizontal and vertical flip
                }

                Cv2.ImWrite($"{currentCamera} ligma.tiff", frame);

                // Convert standard OpenCV BGR space to Godot-compatible RGB space
                using var rgbFrame = new Mat();
                Cv2.CvtColor(frame, rgbFrame, ColorConversionCodes.BGR2RGB);

                // Safely copy the raw memory bytes from the native C++ pointer to managed space
                int byteCount = (int)(rgbFrame.Total() * rgbFrame.Channels());
                byte[] rawData = new byte[byteCount];
                Marshal.Copy(rgbFrame.Data, rawData, 0, byteCount);

                // Build the Godot Image asset and pass it onto the UI component
                var imageTexture = Godot.Image.CreateFromData(rgbFrame.Width, rgbFrame.Height, false, Godot.Image.Format.Rgb8, rawData);
                previewImage.Texture = ImageTexture.CreateFromImage(imageTexture);
                SettingsPage.Save();
                return;
            }
        } 

        // Fallback option if the capture card fails to respond or frame is empty
        previewImage.Texture = fallBackTexture;
    }
    public static void ConfigureResolution(VideoCapture capture)
    {
        var resolutionsToTest = new List<(int width, int height)>
        {
            (3840, 2160), // 4K
            (2560, 1440), // 2K
            (1920, 1080), // 1080p
            (1280, 720),  // 720p
            (640, 480)    // 480p
        };

        foreach (var resolution in resolutionsToTest)
        {
            capture.Set(VideoCaptureProperties.FrameWidth, resolution.width);
            capture.Set(VideoCaptureProperties.FrameHeight, resolution.height);
            int actualWidth = (int)capture.Get(VideoCaptureProperties.FrameWidth);
            int actualHeight = (int)capture.Get(VideoCaptureProperties.FrameHeight);
            if (actualWidth == resolution.width && actualHeight == resolution.height)
            {
                break;
            }
        }
    }
    public static void ApplyTransforms(Mat frame)
    {
        if (frame == null || frame.Empty()) return;

        // 1. Apply Rotation
        if (currentRotation == 1)
        {
            Cv2.Rotate(frame, frame, RotateFlags.Rotate90Clockwise);
        }
        else if (currentRotation == 2)
        {
            Cv2.Rotate(frame, frame, RotateFlags.Rotate180);
        }
        else if (currentRotation == 3)
        {
            Cv2.Rotate(frame, frame, RotateFlags.Rotate90Counterclockwise);
        }

        // 2. Apply Flip
        if (currentFlip == 1)
        {
            Cv2.Flip(frame, frame, FlipMode.Y);  // Horizontal
        }
        else if (currentFlip == 2)
        {
            Cv2.Flip(frame, frame, FlipMode.X);  // Vertical
        }
        else if (currentFlip == 3)
        {
            Cv2.Flip(frame, frame, FlipMode.XY); // Both
        }
    }
}