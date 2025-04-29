using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace VoltAir.UI.Controls
{
    public partial class CircleProgressBar : UserControl
    {
        public CircleProgressBar()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public double Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public static readonly StyledProperty<double> ValueProperty =
            AvaloniaProperty.Register<CircleProgressBar, double>(nameof(Value), defaultValue: 50, coerce: (o, d) => d * 360 / 100);

        public int Height
        {
            get => GetValue(HeightProperty);
            set => SetValue(HeightProperty, value);
        }

        public static readonly StyledProperty<int> HeightProperty =
            AvaloniaProperty.Register<CircleProgressBar, int>(nameof(Height), defaultValue: 150);

        public int Width
        {
            get => GetValue(WidthProperty);
            set => SetValue(WidthProperty, value);
        }

        public static readonly StyledProperty<int> WidthProperty =
            AvaloniaProperty.Register<CircleProgressBar, int>(nameof(Width), defaultValue: 150);

        public int StrokeWidth
        {
            get => GetValue(StrokeWidthProperty);
            set => SetValue(StrokeWidthProperty, value);
        }

        public static readonly StyledProperty<int> StrokeWidthProperty =
            AvaloniaProperty.Register<CircleProgressBar, int>(nameof(StrokeWidth), defaultValue: 10);

        public bool IsIndeterminate
        {
            get => GetValue(IsIndeterminateProperty);
            set => SetValue(IsIndeterminateProperty, value);
        }

        public static readonly StyledProperty<bool> IsIndeterminateProperty =
            AvaloniaProperty.Register<CircleProgressBar, bool>(nameof(IsIndeterminate), false);

        // Adding properties for colors
        public IBrush StrokeColor
        {
            get => GetValue(StrokeColorProperty);
            set => SetValue(StrokeColorProperty, value);
        }

        public static readonly StyledProperty<IBrush> StrokeColorProperty =
            AvaloniaProperty.Register<CircleProgressBar, IBrush>(nameof(StrokeColor), new SolidColorBrush(Colors.DarkRed));

        public IBrush FillColor
        {
            get => GetValue(FillColorProperty);
            set => SetValue(FillColorProperty, value);
        }

        public static readonly StyledProperty<IBrush> FillColorProperty =
            AvaloniaProperty.Register<CircleProgressBar, IBrush>(nameof(FillColor), new SolidColorBrush(Colors.Red));
    }
}