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
    private List<Button> _toolButtons = new();

    private bool _isMoving = false;
    private PointF _lastMousePos;
    double _currentScale = 1;
    double _startScale = 1;

    public MainPage()
    {
        InitializeComponent();
        CanvasView.Drawable = _painter;

        this.Appearing += (s, e) =>
        {
            CollectToolButtons(this);
            UpdateCanvasSize();
            UpdateToolbarVisuals();
        };

#if WINDOWS
        CanvasView.HandlerChanged += (s, e) =>
        {
            var platformView = CanvasView.Handler?.PlatformView as Microsoft.UI.Xaml.FrameworkElement;
            if (platformView != null) platformView.PointerWheelChanged += PlatformView_PointerWheelChanged;
        };
#endif
    }

    private void CollectToolButtons(Element parent)
    {
        foreach (var item in parent.LogicalChildren)
        {
            if (item is Button btn && btn.CommandParameter != null)
                if (Enum.IsDefined(typeof(MyShapeType), btn.CommandParameter.ToString()))
                    if (!_toolButtons.Contains(btn)) _toolButtons.Add(btn);
            if (item is Element visualElement) CollectToolButtons(visualElement);
        }
    }

    private void UpdateToolbarVisuals()
    {
        foreach (var btn in _toolButtons)
        {
            bool isSelected = btn.CommandParameter?.ToString() == _currentTool.ToString();
            btn.BackgroundColor = isSelected ? Color.FromArgb("#007AFF") : Colors.Transparent;
            btn.TextColor = isSelected ? Colors.White : Color.FromArgb("#333333");
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
        float logicX = (float)(_lastMousePos.X / _currentScale);
        float logicY = (float)(_lastMousePos.Y / _currentScale);

        if (_currentTool == MyShapeType.Select)
        {
            var hit = _painter.Shapes.AsEnumerable().Reverse().FirstOrDefault(s => s.GetBounds().Contains(new PointF(logicX, logicY)));
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
            if (_selectedShape != null) _selectedShape.IsSelected = false;
            _selectedShape = null;

            _painter.CurrentPreviewShape = new ShapeModel
            {
                Type = _currentTool,
                X = logicX,
                Y = logicY,
                StrokeColor = ColorPreview.Color,
                StrokeThickness = (float)ThicknessSlider.Value
            };

            if (_currentTool == MyShapeType.Point)
            {
                _painter.Shapes.Add(_painter.CurrentPreviewShape);
                _painter.CurrentPreviewShape = null;
                SaveState();
            }
        }
        CanvasView.Invalidate();
    }

    private void OnCanvasDrag(object sender, TouchEventArgs e)
    {
        if (e.Touches.Length == 0) return;
        var rawPoint = e.Touches[0];
        float logicX = (float)(rawPoint.X / _currentScale);
        float logicY = (float)(rawPoint.Y / _currentScale);

        if (_currentTool == MyShapeType.Select)
        {
            if (_isMoving && _selectedShape != null)
            {
                _selectedShape.X += (rawPoint.X - _lastMousePos.X) / (float)_currentScale;
                _selectedShape.Y += (rawPoint.Y - _lastMousePos.Y) / (float)_currentScale;
            }
            else if (!_isMoving)
            {
                CanvasContainer.TranslationX += (rawPoint.X - _lastMousePos.X);
                CanvasContainer.TranslationY += (rawPoint.Y - _lastMousePos.Y);
            }
        }
        else if (_painter.CurrentPreviewShape != null)
        {
            _painter.CurrentPreviewShape.Width = logicX - _painter.CurrentPreviewShape.X;
            _painter.CurrentPreviewShape.Height = logicY - _painter.CurrentPreviewShape.Y;
            if (_currentTool == MyShapeType.Square || _currentTool == MyShapeType.Circle)
                _painter.CurrentPreviewShape.Height = _painter.CurrentPreviewShape.Width;
        }
        _lastMousePos = rawPoint;
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
        if (sender is Button btn && Enum.TryParse(btn.CommandParameter?.ToString(), out MyShapeType type))
        {
            _currentTool = type;
            UpdateToolbarVisuals();
            if (type != MyShapeType.Select && _selectedShape != null)
            {
                _selectedShape.IsSelected = false;
                _selectedShape = null;
                CanvasView.Invalidate();
            }
        }
    }

    private void OnThicknessChanged(object sender, ValueChangedEventArgs e)
    {
        if (_selectedShape != null) { _selectedShape.StrokeThickness = (float)e.NewValue; CanvasView.Invalidate(); }
    }

    private async void OnColorPickerTapped(object sender, TappedEventArgs e)
    {
        string res = await DisplayActionSheet("Màu", "Hủy", null, "Black", "Red", "Blue", "Green", "Orange", "Purple");
        if (res != "Hủy" && res != null)
        {
            Color c = res switch { "Red" => Colors.Red, "Blue" => Colors.Blue, "Green" => Colors.Green, "Orange" => Colors.Orange, "Purple" => Colors.Purple, _ => Colors.Black };
            ColorPreview.Color = c;
            if (_selectedShape != null) { _selectedShape.StrokeColor = c; CanvasView.Invalidate(); }
        }
    }

    private void OnFillShape(object sender, EventArgs e)
    {
        if (_selectedShape != null) { SaveState(); _selectedShape.FillColor = ColorPreview.Color; CanvasView.Invalidate(); }
    }

    private void OnUndo(object sender, EventArgs e)
    {
        if (_undoStack.Count > 0) { _painter.Shapes = _undoStack.Last(); _undoStack.RemoveAt(_undoStack.Count - 1); _selectedShape = null; CanvasView.Invalidate(); }
    }

    private void OnDelete(object sender, EventArgs e)
    {
        if (_selectedShape != null) { SaveState(); _painter.Shapes.Remove(_selectedShape); _selectedShape = null; CanvasView.Invalidate(); }
    }

    private void UpdateUIFromSelected()
    {
        if (_selectedShape != null) { ThicknessSlider.Value = _selectedShape.StrokeThickness; ColorPreview.Color = _selectedShape.StrokeColor; }
    }

    private void UpdateCanvasSize()
    {
        double minW = Math.Max(this.Width, 1500);
        double minH = Math.Max(this.Height, 1500);
        CanvasView.WidthRequest = minW; CanvasView.HeightRequest = minH;
        CanvasContainer.WidthRequest = minW; CanvasContainer.HeightRequest = minH;
    }

    private void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
    {
        if (e.Status == GestureStatus.Started) _startScale = _currentScale;
        if (e.Status == GestureStatus.Running) { _currentScale = Math.Clamp(_startScale * e.Scale, 0.1, 10.0); CanvasContainer.Scale = _currentScale; }
    }

#if WINDOWS
    private void PlatformView_PointerWheelChanged(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        int delta = e.GetCurrentPoint(null).Properties.MouseWheelDelta;
        _currentScale = Math.Clamp(_currentScale + (delta > 0 ? 0.1 : -0.1), 0.1, 10.0);
        CanvasContainer.Scale = _currentScale; e.Handled = true;
    }
#endif

    private async void OnSaveBinary(object sender, EventArgs e) { try { string path = Path.Combine(FileSystem.AppDataDirectory, $"draw_{DateTime.Now:HHmmss}.bin"); BinaryService.Save(path, _painter.Shapes); await DisplayAlert("Lưu", "Xong", "OK"); } catch { } }

    private async void OnLoadBinary(object sender, EventArgs e) { var result = await FilePicker.Default.PickAsync(); if (result != null) { _painter.Shapes = BinaryService.Load(result.FullPath); _selectedShape = null; UpdateCanvasSize(); CanvasView.Invalidate(); } }

    private async void OnExportImage(object sender, EventArgs e) { try { string path = Path.Combine(FileSystem.AppDataDirectory, "out.png"); using var s = File.Create(path); ExportService.ExportToPng(s, _painter.Shapes, CanvasView.Width, CanvasView.Height); await DisplayAlert("PNG", "Xong", "OK"); } catch { } }
}