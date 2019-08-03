using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Point = System.Windows.Point;

namespace HistogramComparer.Desktop.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly HistogramService histogramService;

        public MainWindowViewModel(HistogramService histogramService)
        {
            SelectImg1 = new RelayCommand((_) => true, (_) =>
            {
                OpenFileDialog ofd = new OpenFileDialog();
                if (ofd.ShowDialog() == true)
                {
                    Img1Path = ofd.FileName;
                }
            });

            SelectImg2 = new RelayCommand((_) => true, (_) =>
            {
                OpenFileDialog ofd = new OpenFileDialog();
                if (ofd.ShowDialog() == true)
                {
                    Img2Path = ofd.FileName;
                }
            });
            this.histogramService = histogramService;
        }

        #region Images
        private string img1Path;

        public string Img1Path
        {
            get { return img1Path; }
            set
            {
                img1Path = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Img1Path)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Img1Source)));
                SeriesCollection1 = CreateNormalizedGrayscaleHistogramSeries(img1Path, img2Path);
            }
        }

        public ImageSource Img1Source
        {
            get
            {
                if (string.IsNullOrEmpty(img1Path))
                {
                    return DefaultImageSource;
                }

                var img = new BitmapImage(new Uri(img1Path, UriKind.Absolute));
                return img;
            }
        }

        private string img2Path;

        public string Img2Path
        {
            get { return img2Path; }
            set
            {
                img2Path = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Img2Path)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Img2Source)));
                SeriesCollection2 = CreateNormalizedGrayscaleHistogramSeries(img2Path, img1Path);
            }
        }

        public ImageSource Img2Source
        {
            get
            {
                if (string.IsNullOrEmpty(img2Path))
                {
                    return DefaultImageSource;
                }

                var img = new BitmapImage(new Uri(img2Path, UriKind.Absolute));
                return img;
            }
        }

        private ImageSource DefaultImageSource
        {
            get
            {
                GeometryGroup rectangles = new GeometryGroup();

                rectangles.FillRule = FillRule.Nonzero;

                rectangles.Children.Add(new RectangleGeometry(new System.Windows.Rect(0, 0, 50, 5), 0, 0, new RotateTransform(-45, 25, 2.5)));
                rectangles.Children.Add(new RectangleGeometry(new System.Windows.Rect(0, 0, 50, 5), 0, 0, new RotateTransform(45, 25, 2.5)));

                GeometryDrawing aGeometryDrawing = new GeometryDrawing();
                aGeometryDrawing.Geometry = rectangles;

                // Paint the drawing with a gradient.
                aGeometryDrawing.Brush =
                    new LinearGradientBrush(
                        Colors.Red,
                        Colors.DarkRed,
                        new Point(0, 0),
                        new Point(1, 1));

                // Outline the drawing with a solid color.
                aGeometryDrawing.Pen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.Red, 1);

                //
                // Use a DrawingImage and an Image control
                // to display the drawing.
                //
                DrawingImage geometryImage = new DrawingImage(aGeometryDrawing);

                // Freeze the DrawingImage for performance benefits.
                geometryImage.Freeze();

                return geometryImage;
            }
        }
        #endregion

        public ICommand SelectImg1 { get; private set; }
        public ICommand SelectImg2 { get; private set; }

        public string[] Labels => Enumerable.Range(1, HistogramService.DEFAULT_HISTOGRAM_WIDTH).Select(_ => _.ToString()).ToArray();
        public Func<double, string> YFormatter => value => value.ToString("F");

        private SeriesCollection seriesCollection1;
        private SeriesCollection seriesCollection2;

        public SeriesCollection SeriesCollection1
        {
            get => seriesCollection1;
            set
            {
                seriesCollection1 = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SeriesCollection1)));
            }
        }
        public SeriesCollection SeriesCollection2
        {
            get => seriesCollection2;
            set
            {
                seriesCollection2 = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SeriesCollection2)));
            }
        }

        private double totalDiff;

        public double TotalDiff
        {
            get { return totalDiff; }
            set
            {
                totalDiff = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalDiff)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalDiffText)));
            }
        }

        public string TotalDiffText => $"Total diff: {totalDiff.ToString("F")}";

        private double maxDiff;

        public double MaxDiff
        {
            get { return maxDiff; }
            set
            {
                maxDiff = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MaxDiff)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MaxDiffText)));
            }
        }

        public string MaxDiffText => $"Max diff: {maxDiff.ToString("F")}";

        private SeriesCollection CreateNormalizedGrayscaleHistogramSeries(params string[] images)
        {
            if (images == null || images.Length < 1)
                throw new ArgumentNullException(nameof(images));

            var series = new SeriesCollection();
            double[] prevHistogramData = null;

            foreach (var img in images)
            {
                if (img == null)
                    continue;

                var bitmap = new Bitmap(img);
                var histogramData = histogramService.GetNormalizedGraysacale(bitmap);

                if (prevHistogramData != null)
                {
                    var diff = new double[256];
                    for (int i = 0; i < histogramData.Length; i++)
                    {
                        diff[i] = Math.Abs(histogramData[i] - prevHistogramData[i]);
                    }
                    series.Add(new LineSeries
                    {
                        Title = $"Diff",
                        Values = new ChartValues<double>(diff),
                        PointGeometry = null,
                        Fill = new SolidColorBrush(Colors.Red)
                    });

                    TotalDiff = diff.Sum();
                    MaxDiff = diff.Max();
                }

                prevHistogramData = histogramData;

                series.Add(new LineSeries
                {
                    Title = $"Normalized {img.Substring(img.LastIndexOf(System.IO.Path.DirectorySeparatorChar) + 1)}",
                    Values = new ChartValues<double>(histogramData),
                    PointGeometry = null
                });
            }

            return series;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
