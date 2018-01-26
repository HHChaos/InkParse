using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

//https://go.microsoft.com/fwlink/?LinkId=234236 上介绍了“用户控件”项模板

namespace InkParse
{
    public sealed partial class ImageStroke : UserControl
    {
        public ImageStroke()
        {
            this.InitializeComponent();
            InkCanvas.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Mouse |
                                                      Windows.UI.Core.CoreInputDeviceTypes.Pen |
                                                      Windows.UI.Core.CoreInputDeviceTypes.Touch;
            InkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(new InkDrawingAttributes
            {
                IgnorePressure = true,
                Color = Colors.Black,
                PenTip = PenTipShape.Circle,
                Size = new Size(8, 8)
            });
            InkCanvas.InkPresenter.StrokesCollected += InkPresenter_StrokesCollected;
            InkCanvas.InkPresenter.StrokesErased += InkPresenter_StrokesErased;
        }

        private void InkPresenter_StrokesErased(InkPresenter sender, InkStrokesErasedEventArgs args)
        {
            foreach (var item in args.Strokes)
            {
                var strokeBuilder = new InkStrokeBuilder();
                strokeBuilder.SetDefaultDrawingAttributes(item.DrawingAttributes);
                var stroke = strokeBuilder.CreateStrokeFromInkPoints(item.GetInkPoints(), item.PointTransform);
                _undoList.Add(stroke);
                if (_redoList.Contains(item))
                    _redoList.Remove(item);
            }
            _undoCommand?.RaiseCanExecuteChanged();
            _redoCommand?.RaiseCanExecuteChanged();
        }

        private readonly List<InkStroke> _redoList = new List<InkStroke>();
        private readonly List<InkStroke> _undoList = new List<InkStroke>();
        private void InkPresenter_StrokesCollected(InkPresenter sender, InkStrokesCollectedEventArgs args)
        {
            foreach (var item in args.Strokes)
            {
                _redoList.Add(item);
            }
            _undoCommand?.RaiseCanExecuteChanged();
            _redoCommand?.RaiseCanExecuteChanged();
        }

        private RelayCommand _undoCommand;

        public RelayCommand UndoCommand
        {
            get
            {
                return _undoCommand ?? (_undoCommand = new RelayCommand(() =>
                {
                    if (_redoList.Count > 0)
                    {
                        var item = _redoList.Last();
                        _redoList.Remove(item);
                        _undoList.Add(item.Clone());
                        item.Selected = true;
                        InkCanvas.InkPresenter.StrokeContainer.DeleteSelected();
                    }
                    _undoCommand?.RaiseCanExecuteChanged();
                    _redoCommand?.RaiseCanExecuteChanged();
                }, () => _redoList.Count > 0));
            }
        }
        private RelayCommand _redoCommand;

        public RelayCommand RedoCommand
        {
            get
            {
                return _redoCommand ?? (_redoCommand = new RelayCommand(() =>
                {
                    if (_undoList.Count > 0)
                    {
                        var item = _undoList.Last();
                        _undoList.Remove(item);
                        InkCanvas.InkPresenter.StrokeContainer.AddStroke(item);
                    }
                    _undoCommand?.RaiseCanExecuteChanged();
                    _redoCommand?.RaiseCanExecuteChanged();
                }, () => _undoList.Count > 0));
            }
        }

        public InkInputProcessingMode InkInputMode
        {
            get { return InkCanvas.InkPresenter.InputProcessingConfiguration.Mode; }
            set { InkCanvas.InkPresenter.InputProcessingConfiguration.Mode = value; }
        }

        public double StrokeSize
        {
            get { return (double)GetValue(StrokeSizeProperty); }
            set { SetValue(StrokeSizeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for StrokeSize.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StrokeSizeProperty =
            DependencyProperty.Register("StrokeSize", typeof(double), typeof(ImageStroke), new PropertyMetadata(8d, StrokeSizePropertyChangedCallback));

        private static void StrokeSizePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as ImageStroke;
            if (control != null)
            {
                var size = (double)e.NewValue;
                if (size > 0)
                {
                    control.InkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(new InkDrawingAttributes
                    {
                        IgnorePressure = true,
                        Color = Colors.Black,
                        PenTip = PenTipShape.Circle,
                        Size = new Size(size, size)
                    });
                }
            }
        }
        public async Task<byte[]> SaveAsSvgAsync()
        {
            var path = ParseInk(InkCanvas.InkPresenter.StrokeContainer.GetStrokes());
            var svgContent = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                             "<svg version=\"1.1\" xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" xml:space=\"preserve\" " +
                             $"viewBox=\"0 0 {CanvasGrid.ActualWidth} {CanvasGrid.ActualHeight}\">\n" +
                             $"{path}" +
                             "</svg>";
            return Encoding.UTF8.GetBytes(svgContent);
        }
        

        private string ParseInk(IEnumerable<InkStroke> strokes)
        {
            var pathBuilder = new StringBuilder();
            foreach (var stroke in strokes)
            {
                var path = new StringBuilder();
                var size = stroke.DrawingAttributes.Size;
                var inkPoints = stroke.GetInkPoints().Select(point => point.Position).ToList();
                foreach (var p in inkPoints)
                {
                    path.Append($" L{p.X},{p.Y}");
                }
                if (path.Length != 0)
                {
                    pathBuilder.Append(
                        $"    <path fill=\"none\" stroke=\"black\" stroke-width=\"{size.Width}\" d=\"M{path.ToString().Substring(2)}\"/>\n");
                }
            }
            return pathBuilder.ToString();
        }

    }
}
