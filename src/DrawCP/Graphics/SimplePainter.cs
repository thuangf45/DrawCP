using DrawCP.Models;

namespace DrawCP.Graphics;

public class SimplePainter : IDrawable
{
    public List<ShapeModel> Shapes { get; set; } = new();
    public ShapeModel CurrentPreviewShape { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        foreach (var shape in Shapes)
        {
            DrawShape(canvas, shape);
            if (shape.IsSelected) DrawSelectionHighlight(canvas, shape);
        }

        if (CurrentPreviewShape != null)
        {
            canvas.Alpha = 0.5f;
            DrawShape(canvas, CurrentPreviewShape);
        }
    }

    private void DrawShape(ICanvas canvas, ShapeModel shape)
    {
        canvas.StrokeColor = shape.StrokeColor;
        canvas.StrokeSize = shape.StrokeThickness;
        canvas.FillColor = shape.FillColor;

        var rect = shape.GetBounds();

        switch (shape.Type)
        {
            case MyShapeType.Point:
                canvas.FillCircle(shape.X, shape.Y, shape.StrokeThickness);
                break;
            case MyShapeType.Line:
                canvas.DrawLine(shape.X, shape.Y, shape.X + shape.Width, shape.Y + shape.Height);
                break;
            case MyShapeType.Rectangle:
            case MyShapeType.Square:
                canvas.FillRectangle(rect);
                canvas.DrawRectangle(rect);
                break;
            case MyShapeType.Ellipse:
            case MyShapeType.Circle:
                canvas.FillEllipse(rect);
                canvas.DrawEllipse(rect);
                break;
        }
    }

    private void DrawSelectionHighlight(ICanvas canvas, ShapeModel shape)
    {
        canvas.StrokeColor = Colors.DeepSkyBlue;
        canvas.StrokeSize = 2;
        canvas.StrokeDashPattern = new float[] { 4, 4 };

        var rect = shape.GetBounds();
        // Thay thế Inset(-2, -2) bằng cách tạo Rect mới rộng hơn một chút
        var highlightRect = new RectF(rect.X - 2, rect.Y - 2, rect.Width + 4, rect.Height + 4);

        canvas.DrawRectangle(highlightRect);
    }
}