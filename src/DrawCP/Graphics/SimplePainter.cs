using DrawCP.Models;

namespace DrawCP.Graphics;

public class SimplePainter : IDrawable
{
    public List<ShapeModel> Shapes { get; set; } = new();
    // Thêm dấu ? để tương thích .NET 9 Nullable Check
    public ShapeModel? CurrentPreviewShape { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        // Vẽ các hình chính thức
        foreach (var shape in Shapes)
        {
            DrawShape(canvas, shape);
        }

        // Vẽ Highlight cho hình đang được chọn (Vẽ sau cùng để luôn nằm trên)
        var selected = Shapes.LastOrDefault(s => s.IsSelected);
        if (selected != null) DrawSelectionHighlight(canvas, selected);

        // Vẽ hình nháp (Preview)
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
                // Dùng StrokeColor để vẽ điểm và tăng kích thước lên 5 cho dễ thấy
                canvas.FillColor = shape.StrokeColor;
                canvas.FillCircle(shape.X, shape.Y, 1 + (shape.StrokeThickness / 2));
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
        // Mở rộng vùng bao 2 đơn vị để không đè lên hình
        var highlightRect = new RectF(rect.X - 2, rect.Y - 2, rect.Width + 4, rect.Height + 4);
        canvas.DrawRectangle(highlightRect);
    }
}