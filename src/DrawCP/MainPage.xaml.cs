using DrawCP.Graphics;
using DrawCP.Models;
using DrawCP.Services;

namespace DrawCP;

public partial class MainPage : ContentPage
{
    private SimplePainter _painter = new();
    private MyShapeType _currentTool = MyShapeType.Line;
    private ShapeModel? _selectedShape; // .NET 9 Nullable
    private List<List<ShapeModel>> _undoStack = new();

    private bool _isMoving = false;
    private PointF _lastMousePos;

    // Trạng thái Zoom & Pan
    double _currentScale = 1;
    double _startScale = 1;

    public MainPage()
    {
        InitializeComponent();
        CanvasView.Drawable = _painter;

        this.Appearing += (s, e) => UpdateCanvasSize();

#if WINDOWS
        CanvasView.HandlerChanged += (s, e) =>
        {
            var platformView = CanvasView.Handler?.PlatformView as Microsoft.UI.Xaml.FrameworkElement;
            if (platformView != null)
            {
                platformView.PointerWheelChanged += PlatformView_PointerWheelChanged;
            }
        };
#endif
    }

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
        var hit = _painter.Shapes.AsEnumerable().Reverse().FirstOrDefault(s => s.GetBounds().Contains(_lastMousePos));

        if (hit != null && _currentTool == MyShapeType.Point)
        {
            if (_selectedShape != null) _selectedShape.IsSelected = false;
            _selectedShape = hit;
            _selectedShape.IsSelected = true;
            _isMoving = true;
            ThicknessSlider.Value = _selectedShape.StrokeThickness;
            ColorPreview.Color = _selectedShape.StrokeColor;
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

    // 1. Xóa bỏ logic Panning thủ công trong OnCanvasDrag để tránh xung đột với ScrollView
    private void OnCanvasDrag(object sender, TouchEventArgs e)
    {
        var currentPoint = e.Touches[0];

        if (_isMoving && _selectedShape != null)
        {
            _selectedShape.X += currentPoint.X - _lastMousePos.X;
            _selectedShape.Y += currentPoint.Y - _lastMousePos.Y;
            _lastMousePos = currentPoint;
        }
        // Xóa đoạn TranslationX/Y ở đây vì ScrollView sẽ xử lý việc vuốt để cuộn
        else if (_painter.CurrentPreviewShape != null)
        {
            _painter.CurrentPreviewShape.Width = currentPoint.X - _painter.CurrentPreviewShape.X;
            _painter.CurrentPreviewShape.Height = currentPoint.Y - _painter.CurrentPreviewShape.Y;

            if (_currentTool == MyShapeType.Square || _currentTool == MyShapeType.Circle)
                _painter.CurrentPreviewShape.Height = _painter.CurrentPreviewShape.Width;
        }
        CanvasView.Invalidate();
    }

    private void OnCanvasEnd(object sender, TouchEventArgs e)
    {
        if (_painter.CurrentPreviewShape != null)
        {
            SaveState();
            _painter.Shapes.Add(_painter.CurrentPreviewShape);
            _painter.CurrentPreviewShape = null;

            // Vẽ xong hình mới -> Kiểm tra xem có cần nới rộng tờ giấy không
            UpdateCanvasSize();
        }
        _isMoving = false;
        CanvasView.Invalidate();
    }

    private void OnToolSelected(object sender, EventArgs e) =>
        _currentTool = Enum.Parse<MyShapeType>((sender as Button).CommandParameter.ToString());

    private async void OnColorPickerTapped(object sender, TappedEventArgs e)
    {
        string res = await DisplayActionSheet("Màu sắc", "Hủy", null, "Black", "Red", "Blue", "Green", "Orange", "Purple");
        if (res != "Hủy" && res != null)
        {
            Color selected = res switch { "Red" => Colors.Red, "Blue" => Colors.Blue, "Green" => Colors.Green, "Orange" => Colors.Orange, "Purple" => Colors.Purple, _ => Colors.Black };
            ColorPreview.Color = selected;
            if (_selectedShape != null) { _selectedShape.StrokeColor = selected; CanvasView.Invalidate(); }
        }
    }

    private async void OnSaveBinary(object sender, EventArgs e)
    {
        try
        {
            string fileName = $"drawing_{DateTime.Now:yyyyMMdd_HHmmss}.bin";
            string path = Path.Combine(FileSystem.AppDataDirectory, fileName);

            // Đảm bảo đóng file sau khi ghi
            BinaryService.Save(path, _painter.Shapes);

            await DisplayAlert("Thành công", $"Đã lưu: {fileName}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", ex.Message, "OK");
        }
    }

    private async void OnLoadBinary(object sender, EventArgs e)
    {
        var result = await FilePicker.Default.PickAsync();
        if (result != null)
        {
            _painter.Shapes = BinaryService.Load(result.FullPath);
            _selectedShape = null;

            // Load xong -> Nới rộng tờ giấy theo dữ liệu cũ
            UpdateCanvasSize();
            CanvasView.Invalidate();
        }
    }

    private async void OnExportImage(object sender, EventArgs e)
    {
        try
        {
            // Tạo tên file duy nhất theo thời gian để tránh ghi đè
            string fileName = $"drawing_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            string path = Path.Combine(FileSystem.AppDataDirectory, fileName);

            // Khối using đảm bảo stream sẽ được đóng và giải phóng NGAY LẬP TỨC 
            // sau khi thực hiện xong lệnh bên trong, kể cả khi có lỗi xảy ra.
            using (var stream = File.Create(path))
            {
                ExportService.ExportToPng(stream, _painter.Shapes, CanvasView.Width, CanvasView.Height);

                // Ép stream đẩy hết dữ liệu xuống ổ cứng trước khi đóng
                await stream.FlushAsync();
            }

            await DisplayAlert("Thành công", $"Đã xuất ảnh: {fileName}", "OK");
        }
        catch (IOException ioEx)
        {
            await DisplayAlert("Lỗi truy cập file", "File đang bị mở bởi một ứng dụng khác hoặc chưa kịp đóng.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", ex.Message, "OK");
        }
    }

    private void OnThicknessChanged(object sender, ValueChangedEventArgs e)
    {
        if (_selectedShape != null) { _selectedShape.StrokeThickness = (float)e.NewValue; CanvasView.Invalidate(); }
    }

    private void OnFillShape(object sender, EventArgs e)
    {
        if (_selectedShape != null) { _selectedShape.FillColor = ColorPreview.Color; CanvasView.Invalidate(); }
    }

    private void OnUndo(object sender, EventArgs e)
    {
        if (_undoStack.Count > 0) { _painter.Shapes = _undoStack.Last(); _undoStack.RemoveAt(_undoStack.Count - 1); _selectedShape = null; CanvasView.Invalidate(); }
    }

    private void OnDelete(object sender, EventArgs e)
    {
        if (_selectedShape != null) { SaveState(); _painter.Shapes.Remove(_selectedShape); _selectedShape = null; CanvasView.Invalidate(); }
    }

#if WINDOWS
    private void PlatformView_PointerWheelChanged(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        // Lấy thông tin cuộn
        var prop = e.GetCurrentPoint(null).Properties;
        int delta = prop.MouseWheelDelta;

        // Zoom (Ctrl + Wheel hoặc chỉ cần Wheel tùy bạn)
        if (delta > 0)
            _currentScale += 0.1;
        else
            _currentScale -= 0.1;

        _currentScale = Math.Clamp(_currentScale, 0.5, 5.0);
        CanvasContainer.Scale = _currentScale;

        e.Handled = true; // Chặn sự kiện cuộn trang web/app nếu có
    }
#endif

    // ZOOM CHO MOBILE (PINCH)
    private void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
    {
        if (e.Status == GestureStatus.Started) _startScale = _currentScale;
        if (e.Status == GestureStatus.Running)
        {
            _currentScale = Math.Clamp(_startScale * e.Scale, 0.5, 5.0);
            CanvasContainer.Scale = _currentScale;

            // Khi Zoom, kích thước chiếm dụng trong ScrollView cũng thay đổi
            UpdateCanvasSize();
        }
    }

    private void UpdateCanvasSize()
    {
        // Lấy kích thước vùng hiển thị hiện tại (cửa sổ app)
        double minW = this.Width;
        double minH = this.Height;

        // Nếu App vừa mở, Width/Height có thể chưa kịp render (-1)
        if (minW <= 0) minW = DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;
        if (minH <= 0) minH = DeviceDisplay.MainDisplayInfo.Height / DeviceDisplay.MainDisplayInfo.Density;

        if (_painter.Shapes.Count > 0)
        {
            float maxX = _painter.Shapes.Max(s =>
            {
                var b = s.GetBounds();
                return b.X + b.Width;
            }) + 500;

            float maxY = _painter.Shapes.Max(s =>
            {
                var b = s.GetBounds();
                return b.Y + b.Height;
            }) + 500;

            minW = Math.Max(minW, maxX);
            minH = Math.Max(minH, maxY);
        }

        // Cập nhật kích thước vật lý cho "tờ giấy"
        CanvasView.WidthRequest = minW;
        CanvasView.HeightRequest = minH;

        // Cập nhật kích thước chiếm dụng trong ScrollView (bao gồm cả Zoom)
        CanvasContainer.WidthRequest = minW * _currentScale;
        CanvasContainer.HeightRequest = minH * _currentScale;
    }
}