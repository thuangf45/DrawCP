using DrawCP.Graphics;
using DrawCP.Models;
using DrawCP.Services;

namespace DrawCP;

public partial class MainPage : ContentPage
{
    private SimplePainter _painter = new();
    private MyShapeType _currentTool = MyShapeType.Line;
    private ShapeModel _selectedShape;
    private List<List<ShapeModel>> _undoStack = new();

    private bool _isMoving = false;
    private PointF _lastMousePos;

    private string _path = FileSystem.AppDataDirectory; // Lấy đường dẫn thư mục hiện tại của ứng dụng

    public MainPage()
    {
        InitializeComponent();
        CanvasView.Drawable = _painter;
    }

    // Hàm Clone để làm Undo/Redo - Đúng chất Architect, tránh tham chiếu vùng nhớ
    private void SaveState()
    {
        var snapshot = _painter.Shapes.Select(s => new ShapeModel
        {
            Type = s.Type,
            X = s.X,
            Y = s.Y,
            Width = s.Width,
            Height = s.Height,
            StrokeColor = s.StrokeColor,
            FillColor = s.FillColor,
            StrokeThickness = s.StrokeThickness,
            IsSelected = false
        }).ToList();

        _undoStack.Add(snapshot);
        if (_undoStack.Count > 20) _undoStack.RemoveAt(0);
    }

    private void OnCanvasStart(object sender, TouchEventArgs e)
    {
        _lastMousePos = e.Touches[0];

        // Hit Test: Tìm hình theo thứ tự Z-index ngược
        var hit = _painter.Shapes.AsEnumerable().Reverse().FirstOrDefault(s => s.GetBounds().Contains(_lastMousePos));

        if (hit != null && _currentTool == MyShapeType.Point) // Point đóng vai trò Select Tool
        {
            if (_selectedShape != null) _selectedShape.IsSelected = false;
            _selectedShape = hit;
            _selectedShape.IsSelected = true;
            _isMoving = true;
            UpdateUIFromSelected();
        }
        else
        {
            if (_selectedShape != null) _selectedShape.IsSelected = false;
            _selectedShape = null;

            _painter.CurrentPreviewShape = new ShapeModel
            {
                Type = _currentTool,
                X = _lastMousePos.X,
                Y = _lastMousePos.Y,
                StrokeColor = ColorPreview.Color,
                StrokeThickness = (float)ThicknessSlider.Value
            };
        }
        CanvasView.Invalidate();
    }

    private void OnCanvasDrag(object sender, TouchEventArgs e)
    {
        var currentPoint = e.Touches[0];
        if (_isMoving && _selectedShape != null)
        {
            float dx = currentPoint.X - _lastMousePos.X;
            float dy = currentPoint.Y - _lastMousePos.Y;
            _selectedShape.X += dx; _selectedShape.Y += dy;
            _lastMousePos = currentPoint;
        }
        else if (_painter.CurrentPreviewShape != null)
        {
            _painter.CurrentPreviewShape.Width = currentPoint.X - _painter.CurrentPreviewShape.X;
            _painter.CurrentPreviewShape.Height = currentPoint.Y - _painter.CurrentPreviewShape.Y;

            // Xử lý Square/Circle
            if (_currentTool == MyShapeType.Square || _currentTool == MyShapeType.Circle)
                _painter.CurrentPreviewShape.Height = _painter.CurrentPreviewShape.Width;
        }
        CanvasView.Invalidate();
    }

    private void OnCanvasEnd(object sender, TouchEventArgs e)
    {
        if (_painter.CurrentPreviewShape != null)
        {
            SaveState(); // Lưu trạng thái trước khi thêm mới
            _painter.Shapes.Add(_painter.CurrentPreviewShape);
            _painter.CurrentPreviewShape = null;
        }
        _isMoving = false;
        CanvasView.Invalidate();
    }

    private void OnToolSelected(object sender, EventArgs e)
    {
        if (sender is Button btn)
            _currentTool = Enum.Parse<MyShapeType>(btn.CommandParameter.ToString());
    }

    // Fix lỗi "UpdateUIFromSelected" not found
    private void UpdateUIFromSelected()
    {
        if (_selectedShape == null) return;
        ThicknessSlider.Value = _selectedShape.StrokeThickness;
        ColorPreview.Color = _selectedShape.StrokeColor;
    }

    // Fix lỗi Signature OnColorPickerTapped
    private void OnColorPickerTapped(object sender, TappedEventArgs e)
    {
        // Logic đổi màu đơn giản để test
        ColorPreview.Color = ColorPreview.Color == Colors.Black ? Colors.Red : Colors.Black;

        // Nếu đang chọn hình thì đổi màu luôn cho hình đó
        if (_selectedShape != null)
        {
            _selectedShape.StrokeColor = ColorPreview.Color;
            CanvasView.Invalidate();
        }
    }

    private async void OnSaveBinary(object sender, EventArgs e)
    {
        string path = Path.Combine(_path, "drawing.bin");
        BinaryService.Save(path, _painter.Shapes);
        await DisplayAlert("Thành công", $"Đã lưu binary tại: {path}", "OK");
    }

    private void OnLoadBinary(object sender, EventArgs e)
    {
        string path = Path.Combine(_path, "drawing.bin");
        _painter.Shapes = BinaryService.Load(path);
        CanvasView.Invalidate();
    }

    private async void OnExportImage(object sender, EventArgs e)
    {
        try
        {
            string fileName = $"Drawing_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            string path = Path.Combine(_path, fileName);

            using (var stream = File.OpenWrite(path))
            {
                ExportService.ExportToPng(stream, _painter.Shapes, CanvasView.Width, CanvasView.Height);
            }

            await DisplayAlert("Thành công", $"Đã xuất ảnh PNG tại: {path}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", ex.Message, "OK");
        }
    }

    // Khi trượt Slider, nếu đang chọn hình thì cập nhật độ dày ngay lập tức
    private void OnThicknessChanged(object sender, ValueChangedEventArgs e)
    {
        if (_selectedShape != null)
        {
            _selectedShape.StrokeThickness = (float)e.NewValue;
            CanvasView.Invalidate();
        }
    }

    // Hàm tô màu cho vật thể đang chọn
    private void OnFillShape(object sender, EventArgs e)
    {
        if (_selectedShape != null)
        {
            SaveState(); // Lưu trạng thái trước khi sửa
            _selectedShape.FillColor = ColorPreview.Color;
            CanvasView.Invalidate();
        }
    }

    // Hàm Undo
    private void OnUndo(object sender, EventArgs e)
    {
        if (_undoStack.Count > 0)
        {
            _painter.Shapes = _undoStack.Last();
            _undoStack.RemoveAt(_undoStack.Count - 1);
            _selectedShape = null; // Bỏ chọn để tránh lỗi tham chiếu
            CanvasView.Invalidate();
        }
    }

    // Hàm Xóa hình đang chọn
    private void OnDelete(object sender, EventArgs e)
    {
        if (_selectedShape != null)
        {
            SaveState();
            _painter.Shapes.Remove(_selectedShape);
            _selectedShape = null;
            CanvasView.Invalidate();
        }
    }
}