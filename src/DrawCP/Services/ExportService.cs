using DrawCP.Models;
using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace DrawCP.Services;

public static class ExportService
{
    public static void ExportToPng(Stream targetStream, List<ShapeModel> shapes, double width, double height)
    {
        if (shapes == null || shapes.Count == 0) return;

        // 1. Tính toán Bounding Box bao quát toàn bộ shapes
        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;

        foreach (var s in shapes)
        {
            var bounds = s.GetBounds(); // Lấy RectF của shape
            float halfStroke = s.StrokeThickness / 2;

            // Tính toán khung bao có tính đến độ dày viền
            minX = Math.Min(minX, bounds.Left - halfStroke);
            minY = Math.Min(minY, bounds.Top - halfStroke);
            maxX = Math.Max(maxX, bounds.Right + halfStroke);
            maxY = Math.Max(maxY, bounds.Bottom + halfStroke);
        }

        // Thêm một khoảng đệm (padding) nhỏ 5px để không bị sát mép quá
        float padding = 5;
        minX -= padding;
        minY -= padding;
        maxX += padding;
        maxY += padding;

        float finalWidth = maxX - minX;
        float finalHeight = maxY - minY;

        // 2. Tạo ảnh với kích thước vừa khít
        var info = new SKImageInfo((int)finalWidth, (int)finalHeight);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        // 3. QUAN TRỌNG: Dời tọa độ canvas để hình vẽ khớp vào ảnh mới
        // Vì shapes có tọa độ gốc dựa trên paper, ta phải trừ đi minX, minY
        canvas.Translate(-minX, -minY);

        foreach (var shape in shapes)
        {
            using var paint = new SKPaint
            {
                IsAntialias = true,
                StrokeWidth = shape.StrokeThickness,
            };

            var rect = new SKRect(shape.X, shape.Y, shape.X + shape.Width, shape.Y + shape.Height);

            // Vẽ Fill
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
            case MyShapeType.Point:
                // Vẽ điểm dựa trên StrokeThickness
                float radius = 1 + (shape.StrokeThickness / 2);
                canvas.DrawCircle(shape.X, shape.Y, radius, paint);
                break;
        }
    }
}