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

    public static int currentCameraIndex
    {
        get;
        set => field = Mathf.Max(0, value);
    }
    public static CameraSetup instance;
    public static int currentRotation = 0;
    public static int currentFlip = 0;
    private static VideoCapture? _currentCamera;
    private double msPerFrame = 1 / 60.0;
    private double frameTimer;

    public override void _Ready()
    {
        GrabFrame();
        
        instance = this;
        prevIndex.Pressed += () => { currentCameraIndex--; Preview(); };
        nextIndex.Pressed += () => { currentCameraIndex++; Preview(); };
        
        rotate.Pressed += () => { currentRotation = (currentRotation + 1) % 4; Preview(); };
        flip.Pressed += () => { currentFlip = (currentFlip + 1) % 4; Preview(); };
    }

    private static void UpdateCamera()
    {
        if (_currentCamera is not null && (!_currentCamera.IsOpened() || _currentCamera.IsDisposed))
            _currentCamera = null;
        
        _currentCamera ??= new VideoCapture(currentCameraIndex);

        if (_currentCamera.IsOpened())
        {
            ConfigureResolution(_currentCamera);
        }
    }

    public override void _Process(double delta)
    {
        frameTimer += delta;
        if (frameTimer >= msPerFrame)
        {
            frameTimer -= msPerFrame;

            if (parentControl.Visible)
                UpdatePreviewImage();
        }
        
        GrabFrame();
    }

    public static void ReadFrame(Mat frame)
    {
        if (_currentCamera is null || !_currentCamera.IsOpened())
        {
            DefaultFrame(frame);
            return;
        }
        
        using var read = new Mat();
        _currentCamera!.Retrieve(read);

        if (read.Empty())
        {
            DefaultFrame(frame);
            return;
        }
        
        ApplyTransforms(read);
        Cv2.Resize(read, frame, new Size(1920, 1080));
    }

    private static void DefaultFrame(Mat frame)
    {
        using var temp = new Mat(1080, 1920, MatType.CV_8UC3);
        Cv2.Resize(temp, frame, new Size(1920, 1080));
    }

    private static bool GrabFrame()
    {
        if (_currentCamera is null)
            UpdateCamera();
        
        return _currentCamera!.Grab();
    }

    public void Preview()
    {
        currentCameraLabel.Text = $"Current Index: {currentCameraIndex}";
        // var resolutionsToTest = new List<(int width, int height)>
        // {
        //     (3840, 2160), // 4K
        //     (2560, 1440), // 2K
        //     (1920, 1080), // 1080p
        //     (1280, 720),  // 720p
        //     (640, 480)    // 480p
        // };
        //
        // using var frame = new Mat();
        
        UpdateCamera();
        
        if (_currentCamera!.IsOpened())
        {
            // foreach (var resolution in resolutionsToTest)
            // {
            //     _currentCamera.Set(VideoCaptureProperties.FrameWidth, resolution.width);
            //     _currentCamera.Set(VideoCaptureProperties.FrameHeight, resolution.height);
            //     int actualWidth = (int)_currentCamera.Get(VideoCaptureProperties.FrameWidth);
            //     int actualHeight = (int)_currentCamera.Get(VideoCaptureProperties.FrameHeight);
            //     if (actualWidth == resolution.width && actualHeight == resolution.height)
            //     {
            //         break;
            //     }
            // }
            
            // ReadFrame(frame);

            // if (!frame.Empty())
            // {
                // // 1. Apply current 90-degree step rotation transformations
                // if (currentRotation == 1)
                // {
                //     Cv2.Rotate(frame, frame, RotateFlags.Rotate90Clockwise);
                // }
                // else if (currentRotation == 2)
                // {
                //     Cv2.Rotate(frame, frame, RotateFlags.Rotate180);
                // }
                // else if (currentRotation == 3)
                // {
                //     Cv2.Rotate(frame, frame, RotateFlags.Rotate90Counterclockwise);
                // }
                //
                // // 2. Apply flip transformations
                // if (currentFlip == 1)
                // {
                //     Cv2.Flip(frame, frame, FlipMode.Y); // Horizontal flip (mirror)
                // }
                // else if (currentFlip == 2)
                // {
                //     Cv2.Flip(frame, frame, FlipMode.X); // Vertical flip
                // }
                // else if (currentFlip == 3)
                // {
                //     Cv2.Flip(frame, frame, FlipMode.XY); // Both horizontal and vertical flip
                // }
                
                // ApplyTransforms(frame);
            // }
            
            Save();

            // If the preview image is valid
            if (UpdatePreviewImage())
                return;
        } 

        // Fallback option if the capture card fails to respond or frame is empty
        previewImage.Texture = fallBackTexture;
    }

    private bool UpdatePreviewImage()
    {
        if (_currentCamera is null)
            UpdateCamera();

        if (_currentCamera is null || !_currentCamera.IsOpened())
        {
            previewImage.Texture = fallBackTexture;
            return false;
        }

        using var frame = new Mat();
        ReadFrame(frame);

        if (frame.Empty())
        {
            GD.Print("Frame is empty!");
            previewImage.Texture = fallBackTexture;
            return false;
        }
        
        ApplyTransforms(frame);
        
        // Cv2.ImWrite($"Debug-Out/Camera Preview ({currentCameraIndex}).tiff", frame);
        
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
        return true;
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

    private void Save()
    {
        StringBuilder saveInfo = new StringBuilder();
        var saveFile = FileAccess.Open("user://MkctCamera.settings", FileAccess.ModeFlags.Write);
        saveInfo.Append($"{currentCameraIndex} {currentRotation} {currentFlip}");
        saveFile.StoreString(saveInfo.ToString());
        saveFile.Close();
    }

    private void Load()
    {
        var saveFile = FileAccess.Open("user://MkctCamera.settings", FileAccess.ModeFlags.Read);
        if (saveFile is not null)
        {
            var fileContents = saveFile.GetAsText();
            var numbers = fileContents.Split(" ");
            currentCameraIndex = numbers[0].ToInt();
            currentRotation = numbers[1].ToInt();
            currentFlip = numbers[2].ToInt();
            saveFile.Close();
        }
    }
}