using DrawCP.Models;
using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace DrawCP.Services;

public static class ExportService
{
    public static void ExportToPng(Stream targetStream, List<ShapeModel> shapes, double width, double height)
    {
        // Tạo ảnh với kích thước của Canvas
        var info = new SKImageInfo((int)width, (int)height);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White); // Nền trắng

        foreach (var shape in shapes)
        {
            using var paint = new SKPaint
            {
                IsAntialias = true,
                StrokeWidth = shape.StrokeThickness,
                Color = shape.StrokeColor.ToSKColor()
            };

            var rect = new SKRect(shape.X, shape.Y, shape.X + shape.Width, shape.Y + shape.Height);

            // Vẽ Fill (nếu có)
            if (shape.FillColor != Colors.Transparent)
            {
                paint.Style = SKPaintStyle.Fill;
                paint.Color = shape.FillColor.ToSKColor();
                DrawSkiaShape(canvas, shape, rect, paint);
            }

            // Vẽ Stroke
            paint.Style = SKPaintStyle.Stroke;
            paint.Color = shape.StrokeColor.ToSKColor();
            DrawSkiaShape(canvas, shape, rect, paint);
        }

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        data.SaveTo(targetStream);
    }

    private static void DrawSkiaShape(SKCanvas canvas, ShapeModel shape, SKRect rect, SKPaint paint)
    {
        switch (shape.Type)
        {
            case MyShapeType.Rectangle:
            case MyShapeType.Square:
                canvas.DrawRect(rect, paint);
                break;
            case MyShapeType.Ellipse:
            case MyShapeType.Circle:
                canvas.DrawOval(rect, paint);
                break;
            case MyShapeType.Line:
                canvas.DrawLine(shape.X, shape.Y, shape.X + shape.Width, shape.Y + shape.Height, paint);
                break;
        }
    }
}