namespace DrawCP.Models;

public enum MyShapeType
{
    Select, // Thêm Select làm giá trị 0
    Point,
    Line,
    Rectangle,
    Ellipse,
    Square,
    Circle
}

public class ShapeModel
{
    public MyShapeType Type { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public Color StrokeColor { get; set; } = Colors.Black;
    public Color FillColor { get; set; } = Colors.Transparent; // Đảm bảo mặc định là trong suốt
    public float StrokeThickness { get; set; } = 2;
    public bool IsSelected { get; set; }

    // Lấy Rect bao quanh đối tượng để tính toán va chạm
    public RectF GetBounds()
    {
        if (Type == MyShapeType.Line)
        {
            return new RectF(Math.Min(X, X + Width), Math.Min(Y, Y + Height), Math.Abs(Width), Math.Abs(Height));
        }
        // Chuẩn hóa Rect cho trường hợp kéo ngược (Width/Height âm)
        float x = Width > 0 ? X : X + Width;
        float y = Height > 0 ? Y : Y + Height;
        return new RectF(x, y, Math.Abs(Width), Math.Abs(Height));
    }
}