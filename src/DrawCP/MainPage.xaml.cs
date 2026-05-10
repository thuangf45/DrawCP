using DrawCP.Graphics;
using DrawCP.Models;
using DrawCP.Services;

namespace DrawCP;

public partial class MainPage : ContentPage
{
    private SimplePainter _painter = new();
    private MyShapeType _currentTool = MyShapeType.Line;
    private ShapeModel? _selectedShape;
    private List<List<ShapeModel>> _undoStack = new();

    private bool _isMoving = false;
    private PointF _lastMousePos;

    // Trạng thái Zoom & Pan
    double _currentScale = 1;
    double _startScale = 1;

    // Danh sách quản lý các nút công cụ để Highlight
    private List<Button> _toolButtons = new();

    public MainPage()
    {
        InitializeComponent();
        CanvasView.Drawable = _painter;

        // Đăng ký sự kiện khi trang xuất hiện
        this.Appearing += (s, e) =>
        {
            CollectToolButtons(this); // Tự động tìm tất cả nút công cụ
            UpdateCanvasSize();
            UpdateToolbarVisuals(); // Highlight nút mặc định ban đầu
        };

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

    /// <summary>
    /// Đệ quy tìm tất cả Button có CommandParameter là MyShapeType trong Layout
    /// </summary>
    private void CollectToolButtons(Element parent)
    {
        // Duyệt qua danh sách logic children
        foreach (var item in parent.LogicalChildren)
        {
            if (item is Button btn && btn.CommandParameter != null)
            {
                // Kiểm tra xem nút này có phải là nút chọn công cụ vẽ không
                if (Enum.IsDefined(typeof(MyShapeType), btn.CommandParameter.ToString()))
                {
                    if (!_toolButtons.Contains(btn))
                        _toolButtons.Add(btn);
                }
            }

            // Nếu item là một layout hoặc view container, tiếp tục đào sâu xuống
            if (item is Element visualElement)
            {
                CollectToolButtons(visualElement);
            }
        }
    }

    /// <summary>
    /// Cập nhật màu sắc cho các nút: Xanh cho nút đang chọn, Trong suốt cho còn lại
    /// </summary>
    private void UpdateToolbarVisuals()
    {
        foreach (var btn in _toolButtons)
        {
            if (btn.CommandParameter?.ToString() == _currentTool.ToString())
            {
                btn.BackgroundColor = Color.FromArgb("#007AFF"); // AccentBlue
                btn.TextColor = Colors.White;
            }
            else
            {
                btn.BackgroundColor = Colors.Transparent;
                btn.TextColor = Color.FromArgb("#333333");
            }
        }
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

        if (_currentTool == MyShapeType.Select)
        {
            var hit = _painter.Shapes.AsEnumerable().Reverse().FirstOrDefault(s => s.GetBounds().Contains(_lastMousePos));

            if (hit != null)
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
            }
        }
        else
        {
            // Logic vẽ hình mới
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

            // Nếu là Point thì vẽ ngay lập tức không cần Drag
            if (_currentTool == MyShapeType.Point)
            {
                _painter.Shapes.Add(_painter.CurrentPreviewShape);
                _painter.CurrentPreviewShape = null;
                SaveState();
            }
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
            UpdateCanvasSize();
        }
        _isMoving = false;
        CanvasView.Invalidate();
    }

    private void OnToolSelected(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter != null)
        {
            if (Enum.TryParse(btn.CommandParameter.ToString(), out MyShapeType selectedType))
            {
                _currentTool = selectedType;
                UpdateToolbarVisuals();
            }
        }
    }

    private void UpdateUIFromSelected()
    {
        if (_selectedShape == null) return;

        ThicknessSlider.Value = _selectedShape.StrokeThickness;
        ColorPreview.Color = _selectedShape.StrokeColor;
    }

    private async void OnColorPickerTapped(object sender, TappedEventArgs e)
    {
        string res = await DisplayActionSheet("Chọn màu sắc", "Hủy", null, "Black", "Red", "Blue", "Green", "Orange", "Purple");
        if (res != "Hủy" && res != null)
        {
            Color selected = res switch
            {
                "Red" => Colors.Red,
                "Blue" => Colors.Blue,
                "Green" => Colors.Green,
                "Orange" => Colors.Orange,
                "Purple" => Colors.Purple,
                _ => Colors.Black
            };

            ColorPreview.Color = selected;

            if (_selectedShape != null)
            {
                SaveState();
                _selectedShape.StrokeColor = selected;
                CanvasView.Invalidate();
            }
        }
    }

    private async void OnSaveBinary(object sender, EventArgs e)
    {
        try
        {
            string fileName = $"drawing_{DateTime.Now:yyyyMMdd_HHmmss}.bin";
            string path = Path.Combine(FileSystem.AppDataDirectory, fileName);
            BinaryService.Save(path, _painter.Shapes);
            await DisplayAlert("Thành công", $"Đã lưu dữ liệu: {fileName}", "OK");
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
            UpdateCanvasSize();
            CanvasView.Invalidate();
        }
    }

    private async void OnExportImage(object sender, EventArgs e)
    {
        try
        {
            string fileName = $"drawing_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            string path = Path.Combine(FileSystem.AppDataDirectory, fileName);

            using (var stream = File.Create(path))
            {
                ExportService.ExportToPng(stream, _painter.Shapes, CanvasView.Width, CanvasView.Height);
                await stream.FlushAsync();
            }

            await DisplayAlert("Thành công", $"Đã xuất ảnh: {fileName}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", ex.Message, "OK");
        }
    }

    private void OnThicknessChanged(object sender, ValueChangedEventArgs e)
    {
        if (_selectedShape != null)
        {
            _selectedShape.StrokeThickness = (float)e.NewValue;
            CanvasView.Invalidate();
        }
    }

    private void OnFillShape(object sender, EventArgs e)
    {
        if (_selectedShape != null)
        {
            SaveState();
            _selectedShape.FillColor = ColorPreview.Color;
            CanvasView.Invalidate();
        }
        else
        {
            DisplayAlert("Gợi ý", "Chọn vật thể bằng Select tool trước khi tô màu.", "OK");
        }
    }

    private void OnUndo(object sender, EventArgs e)
    {
        if (_undoStack.Count > 0)
        {
            _painter.Shapes = _undoStack.Last();
            _undoStack.RemoveAt(_undoStack.Count - 1);
            _selectedShape = null;
            CanvasView.Invalidate();
        }
    }

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

#if WINDOWS
    private void PlatformView_PointerWheelChanged(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var prop = e.GetCurrentPoint(null).Properties;
        int delta = prop.MouseWheelDelta;

        if (delta > 0)
            _currentScale += 0.1;
        else
            _currentScale -= 0.1;

        _currentScale = Math.Clamp(_currentScale, 0.5, 5.0);
        CanvasContainer.Scale = _currentScale;
        UpdateCanvasSize();

        e.Handled = true;
    }
#endif

    private void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
    {
        if (e.Status == GestureStatus.Started) _startScale = _currentScale;
        if (e.Status == GestureStatus.Running)
        {
            _currentScale = Math.Clamp(_startScale * e.Scale, 0.5, 5.0);
            CanvasContainer.Scale = _currentScale;
            UpdateCanvasSize();
        }
    }

    private void UpdateCanvasSize()
    {
        // Lấy kích thước của Grid chứa ScrollView (vùng trống thực tế trên màn hình)
        // Nếu chưa render xong thì lấy size mặc định của thiết bị
        double containerW = this.Width > 0 ? this.Width : DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;
        double containerH = this.Height > 0 ? this.Height : DeviceDisplay.MainDisplayInfo.Height / DeviceDisplay.MainDisplayInfo.Density;

        // Trừ đi lề (Padding/Margin) đã set trong XAML (khoảng 40-60 đơn vị)
        double minW = containerW - 40;
        double minH = containerH - 150; // Trừ đi cả chiều cao toolbar

        if (_painter.Shapes.Count > 0)
        {
            float maxX = _painter.Shapes.Max(s => s.GetBounds().Right) + 100;
            float maxY = _painter.Shapes.Max(s => s.GetBounds().Bottom) + 100;

            minW = Math.Max(minW, maxX);
            minH = Math.Max(minH, maxY);
        }

        // Cập nhật kích thước
        CanvasView.WidthRequest = minW;
        CanvasView.HeightRequest = minH;

        CanvasContainer.WidthRequest = minW * _currentScale;
        CanvasContainer.HeightRequest = minH * _currentScale;
    }

    // Hàm kiểm tra và nới rộng nhanh (Performance optimized)
    private void EnsureCanvasSize(float targetX, float targetY)
    {
        bool needUpdate = false;
        double currentW = CanvasView.WidthRequest;
        double currentH = CanvasView.HeightRequest;

        // Nếu tọa độ đang kéo vượt quá kích thước hiện tại + một khoảng đệm
        if (targetX > currentW - 100)
        {
            currentW = targetX + 1000; // Nới rộng thêm 1000px để tránh gọi lại liên tục
            needUpdate = true;
        }
        if (targetY > currentH - 100)
        {
            currentH = targetY + 1000;
            needUpdate = true;
        }

        if (needUpdate)
        {
            CanvasView.WidthRequest = currentW;
            CanvasView.HeightRequest = currentH;

            // Cập nhật Container theo Scale để ScrollView hiện thanh cuộn đúng
            CanvasContainer.WidthRequest = currentW * _currentScale;
            CanvasContainer.HeightRequest = currentH * _currentScale;
        }
    }

    private void OnCanvasDrag(object sender, TouchEventArgs e)
    {
        var currentPoint = e.Touches[0];

        if (_isMoving && _selectedShape != null)
        {
            _selectedShape.X += currentPoint.X - _lastMousePos.X;
            _selectedShape.Y += currentPoint.Y - _lastMousePos.Y;
            _lastMousePos = currentPoint;

            // Kiểm tra nới rộng khi di chuyển vật thể
            EnsureCanvasSize(_selectedShape.X + Math.Abs(_selectedShape.Width),
                             _selectedShape.Y + Math.Abs(_selectedShape.Height));
        }
        else if (_painter.CurrentPreviewShape != null)
        {
            _painter.CurrentPreviewShape.Width = currentPoint.X - _painter.CurrentPreviewShape.X;
            _painter.CurrentPreviewShape.Height = currentPoint.Y - _painter.CurrentPreviewShape.Y;

            if (_currentTool == MyShapeType.Square || _currentTool == MyShapeType.Circle)
                _painter.CurrentPreviewShape.Height = _painter.CurrentPreviewShape.Width;

            // Kiểm tra nới rộng khi đang vẽ hình mới
            EnsureCanvasSize(currentPoint.X, currentPoint.Y);
        }
        CanvasView.Invalidate();
    }
}