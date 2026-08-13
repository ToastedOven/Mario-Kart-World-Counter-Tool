using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Godot;
using OpenCvSharp;

namespace CounterTool.Utils;

public static class Cv2Extensions
{
    extension(Cv2)
    {
        public static (int x, int y, Size size, int angle, float score)[] InvariantMatchTemplate(Mat image,
            Mat template,
            TemplateMatchModes method,
            System.Range rotationDegreesRange,
            int rotationStep,
            int rotationStepThreshold,
            double threshold,
            float templateWidthPadding = 1f,
            Mat? mask = null,
            bool debugWriteImages = false)
        {
            var imageSize = image.Size();
            var imageCenter = new Point2f(imageSize.Width / 2f, imageSize.Height / 2f);
            var imageType = image.Type();
            
            var results = new ConcurrentBag<(int x, int y, Size size, int angle, float score)>();

            rotationDegreesRange.EnumerateChunked(rotationStep)
                .AsParallel()
                // .WithDegreeOfParallelism(8)
                .ForAll(angle =>
                {
                    using var rotatedTemplate = Cv2.RotateImage(template, angle);
                    var templateSize = rotatedTemplate.Size();
                    if (debugWriteImages)
                        Cv2.ImWrite($"Debug-Out/Track-Selection-Arrow-Segment-{angle}.tiff", rotatedTemplate);

                    var length = Mathf.Sqrt(Mathf.Pow(imageSize.Width / 2f, 2) + Mathf.Pow(imageSize.Height / 2f, 2));

                    var angleRadians = Mathf.DegToRad(-angle);
                    var dir = new Vector2(Mathf.Sin(angleRadians), Mathf.Cos(angleRadians)).Normalized();
                    var maskRectCenter = new Vector2(imageCenter.X, imageCenter.Y) - dir * (imageSize.Height / 3f);
                    
                    var imageMaskRect = new RotatedRect(
                        new Point2f(maskRectCenter.X, maskRectCenter.Y),
                        new Size2f(templateSize.Width + templateWidthPadding, length),
                        angle
                    );

                    var maskBounds = imageMaskRect.BoundingRect();
                    var maskMin = new Point(Mathf.Max(maskBounds.Left - 1, 0), Mathf.Max(maskBounds.Top - 1, 0));
                    var maskMax = new Point(Mathf.Min(maskBounds.Right + 1, imageSize.Width), Mathf.Min(maskBounds.Bottom + 1, imageSize.Height));

                    using var imageMask = new Mat(imageSize, MatType.CV_8UC1);
                    // imageMask.SetTo(Scalar.Black);
                    Cv2.FillConvexPoly(imageMask, imageMaskRect.Points().Select(p => p.ToPoint()), Scalar.White, LineTypes.Link4);

                    using var maskedImage = new Mat(imageSize, imageType);
                    Cv2.BitwiseAnd(image, image, maskedImage, imageMask);

                    imageMask.SetTo(Scalar.White);
                    Cv2.FillConvexPoly(imageMask, imageMaskRect.Points().Select(p => p.ToPoint()), Scalar.Black, LineTypes.Link4);
                    maskedImage.SetTo(Scalar.Black, imageMask);
                    
                    using var croppedImage = maskedImage[maskMin.Y..maskMax.Y, maskMin.X..maskMax.X];
                    if (debugWriteImages)
                        Cv2.ImWrite($"Debug-Out/Track-Selection-Cropped-Rotated-{angle}.tiff", croppedImage);
                    
                    using var output = new Mat();
                    if (mask is null)
                    {
                        Cv2.MatchTemplate(croppedImage, rotatedTemplate, output, method);
                    }
                    else
                    {
                        using var rotatedMask = Cv2.RotateImage(mask, angle);
                        // Cv2.ImWrite($"Debug-Out/Track-Selection-Arrow-Segment-Mask-Rotated-{angle}.tiff", rotatedMask);
                        
                        Cv2.MatchTemplate(croppedImage, rotatedTemplate, output, method, rotatedMask);
                    }
                    
                    using var thresholdImage = new Mat();
                    Cv2.Threshold(output, thresholdImage, threshold, 255, ThresholdTypes.Binary);
                    
                    var (width, height) = (thresholdImage.Width, thresholdImage.Height);

                    for (var x = 0; x < width; x++)
                    for (var y = 0; y < height; y++)
                    {
                        var value = thresholdImage.At<float>(y, x);
                        var score = output.At<float>(y, x);
                        
                        switch (method)
                        {
                            case TemplateMatchModes.SqDiff:
                            case TemplateMatchModes.SqDiffNormed:
                                if (value == 0)
                                    results.Add((x + maskMin.X, y + maskMin.Y, templateSize,  angle, 1 - score));
                                break;
                            case TemplateMatchModes.CCorr:
                            case TemplateMatchModes.CCorrNormed:
                            case TemplateMatchModes.CCoeff:
                            case TemplateMatchModes.CCoeffNormed:
                                if (value != 0)
                                    results.Add((x + maskMin.X, y + maskMin.Y, templateSize, angle, score));
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(method), method, null);
                        }
                    }
                    
                    // unsafe
                    // {
                    //     var currentAngle = angle;
                    //     thresholdImage.ForEachAsFloat((value, pos) =>
                    //     {
                    //         var x = pos[1];
                    //         var y = pos[0];
                    //
                    //         switch (method)
                    //         {
                    //             case TemplateMatchModes.SqDiff:
                    //             case TemplateMatchModes.SqDiffNormed:
                    //                 if (*value == 0)
                    //                     results.Add((x, y, currentAngle));
                    //                 break;
                    //             case TemplateMatchModes.CCorr:
                    //             case TemplateMatchModes.CCorrNormed:
                    //             case TemplateMatchModes.CCoeff:
                    //             case TemplateMatchModes.CCoeffNormed:
                    //                 if (*value != 0)
                    //                     results.Add((x, y, currentAngle));
                    //                 break;
                    //             default:
                    //                 throw new ArgumentOutOfRangeException(nameof(method), method, null);
                    //         }
                    //     });
                    // }
                });


            var distinctResults = new List<(Rect bounds, float score)>();
            
            foreach (var (x, y, _, angle, score) in results.Where(res => !float.IsInfinity(res.score) && !float.IsNaN(res.score)))
            {
                using var rotatedTemplate = Cv2.RotateImage(template, angle);
                var templateSize = rotatedTemplate.Size();
                
                var rect = new Rect(x, y, templateSize.Width, templateSize.Height);

                var collisions = distinctResults.Where(item => item.bounds.IntersectsWith(rect)).ToArray();

                var bestItem = collisions.FirstOrDefault();
                foreach (var collision in collisions)
                {
                    if (!(bestItem.score < collision.score))
                        continue;
                    
                    bestItem = collision;
                    distinctResults.Remove(collision);
                }

                if (!(bestItem.score < score))
                    continue;
                
                distinctResults.Remove(bestItem);
                distinctResults.Add((rect, score));
            }

            var realResults = new List<(int x, int y, Size size, int angle, float score)>();

            foreach (var result in distinctResults)
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                var found = results.FirstOrDefault(item => item.x == result.bounds.X && item.y == result.bounds.Y && item.score == result.score);

                if (found != default)
                {
                    realResults.Add(found);
                }
            }

            // var rectSize = template.Size();
            // var filteredResults = new List<(int x, int y, int angle, float score)>();
            //
            // foreach (var result in realResults)
            // {
            //     var rect = new RotatedRect(new Point(result.x, result.y) + new Point(rectSize.Width / 2f, rectSize.Height / 2f), rectSize, result.angle);
            //     var angleToCenterRads = Mathf.Atan2(rect.Center.Y - imageCenter.Y, rect.Center.X - imageCenter.X);
            //     var angleToCenter = Mathf.RadToDeg(angleToCenterRads);
            //     if (angleToCenter < 0)
            //         angleToCenter += 360;
            //
            //     if (Mathf.Abs(angleToCenter - result.angle) < rotationStepThreshold)
            //         filteredResults.Add(result);
            // }
            
            // var anglesDetected = new int[rotationDegreesRange.End.Value];
            // foreach (var result in realResults)
            //     anglesDetected[result.angle]++;
            //
            // var validResults = new List<(int x, int y, int angle, float score)>();
            //
            // foreach (var result in realResults)
            // {
            //     if (anglesDetected[result.angle] <= 1)
            //     {
            //         if (rotationStepThreshold > 1)
            //         {
            //             var hasNeighbors = false;
            //         
            //             for (var i = 1; i < rotationStepThreshold; i++)
            //             {
            //                 var left = result.angle - rotationStep * i;
            //                 if (left < 0)
            //                     left += 360;
            //                 
            //                 var right = result.angle + rotationStep * i;
            //                 if (right >= 360)
            //                     right -= 360;
            //
            //                 if (anglesDetected[left] == 0 && anglesDetected[right] == 0)
            //                     continue;
            //             
            //                 hasNeighbors = true;
            //                 break;
            //             }
            //
            //             if (!hasNeighbors)
            //                 continue;
            //         }
            //         else
            //         {
            //             continue;
            //         }
            //     }
            //     
            //     validResults.Add(result);
            // }
            
            return realResults.ToArray();
        }

        public static Mat RotateImage(Mat image, int angle)
        {
            var size = image.Size();
            var imageCenter = new Point2f(size.Width / 2f, size.Height / 2f);

            var rotatedRect = new RotatedRect(imageCenter, size, angle);
            var bounds = rotatedRect.BoundingRect();

            var maxSize = new Size(Mathf.Max(size.Width, bounds.Width), Mathf.Max(size.Height, bounds.Height));
            using var rotationCanvas = Mat.ZerosMat(maxSize, image.Type());
            
            var rotationCanvasCenter = new Point2f(maxSize.Width / 2f, maxSize.Height / 2f);
            var centerTranslation = (rotationCanvasCenter - new Point2f(size.Width / 2f, size.Height / 2f)).ToPoint();
            image.CopyTo(rotationCanvas[centerTranslation.Y..(centerTranslation.Y + image.Height), centerTranslation.X..(centerTranslation.X + image.Width)]);

            var rotationMatrix = Cv2.GetRotationMatrix2D(new Point2f(rotationCanvasCenter.X, rotationCanvasCenter.Y), -angle, 1.0);
            
            using var result = new Mat();
            Cv2.WarpAffine(rotationCanvas, result, rotationMatrix, maxSize);
            
            var croppedResult = Mat.ZerosMat(bounds.Size, image.Type());
            result[
                    Mathf.FloorToInt(rotationCanvasCenter.Y - bounds.Height / 2f)..Mathf.FloorToInt(rotationCanvasCenter.Y + bounds.Height / 2f),
                    Mathf.FloorToInt(rotationCanvasCenter.X - bounds.Width / 2f)..Mathf.FloorToInt(rotationCanvasCenter.X + bounds.Width / 2f)
                ].CopyTo(croppedResult);
            
            return croppedResult;
        }

        public static Mat ApplyClahe(Mat image, double clipLimit, Size tileGridSize)
        {
            using var clahe = Cv2.CreateCLAHE(clipLimit, tileGridSize);
            
            using var lab = new Mat();
            Cv2.CvtColor(image, lab, ColorConversionCodes.BGR2Lab);
            
            var channels = Cv2.Split(lab);
            
            using var l = Mat.ZerosMat(channels[0].Size(), channels[0].Type());
            clahe.Apply(channels[0], l);
        
            Cv2.Merge([l, channels[1], channels[2]], lab);
            var result = new Mat();
            
            Cv2.CvtColor(lab, result, ColorConversionCodes.Lab2BGR);
            
            return result;
        }
    }
}