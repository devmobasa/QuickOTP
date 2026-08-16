using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace QuickOTP.Editor.Controls;

public sealed class PeriodRing : Control
{
    public static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<PeriodRing, double>( nameof( Progress ), 1.0 );

    public static readonly StyledProperty<bool> IsUrgentProperty =
        AvaloniaProperty.Register<PeriodRing, bool>( nameof( IsUrgent ) );

    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<PeriodRing, double>( nameof( StrokeThickness ), 8.0 );

    public static readonly StyledProperty<IBrush> TrackBrushProperty =
        AvaloniaProperty.Register<PeriodRing, IBrush>( nameof( TrackBrush ), new SolidColorBrush( Color.Parse( "#2A4150" ) ) );

    public static readonly StyledProperty<IBrush> ProgressBrushProperty =
        AvaloniaProperty.Register<PeriodRing, IBrush>( nameof( ProgressBrush ), new SolidColorBrush( Color.Parse( "#C7923E" ) ) );

    public static readonly StyledProperty<IBrush> UrgentBrushProperty =
        AvaloniaProperty.Register<PeriodRing, IBrush>( nameof( UrgentBrush ), new SolidColorBrush( Color.Parse( "#D4653A" ) ) );

    static PeriodRing( )
    {
        AffectsRender<PeriodRing>(
            ProgressProperty,
            IsUrgentProperty,
            StrokeThicknessProperty,
            TrackBrushProperty,
            ProgressBrushProperty,
            UrgentBrushProperty );
    }

    public double Progress
    {
        get => GetValue( ProgressProperty );
        set => SetValue( ProgressProperty, value );
    }

    public bool IsUrgent
    {
        get => GetValue( IsUrgentProperty );
        set => SetValue( IsUrgentProperty, value );
    }

    public double StrokeThickness
    {
        get => GetValue( StrokeThicknessProperty );
        set => SetValue( StrokeThicknessProperty, value );
    }

    public IBrush TrackBrush
    {
        get => GetValue( TrackBrushProperty );
        set => SetValue( TrackBrushProperty, value );
    }

    public IBrush ProgressBrush
    {
        get => GetValue( ProgressBrushProperty );
        set => SetValue( ProgressBrushProperty, value );
    }

    public IBrush UrgentBrush
    {
        get => GetValue( UrgentBrushProperty );
        set => SetValue( UrgentBrushProperty, value );
    }

    public override void Render( DrawingContext context )
    {
        var thickness = StrokeThickness;
        var size = Math.Min( Bounds.Width, Bounds.Height );
        var radius = Math.Max( ( size - thickness ) / 2, 1 );
        var center = new Point( Bounds.Width / 2, Bounds.Height / 2 );
        var trackPen = new Pen( TrackBrush, thickness, lineCap: PenLineCap.Round );
        context.DrawEllipse( null, trackPen, center, radius, radius );

        var clamped = Math.Clamp( Progress, 0, 1 );
        if ( clamped <= 0 )
        {
            return;
        }

        var sweep = 360 * clamped;
        var startRadians = -Math.PI / 2;
        var endRadians = startRadians + ( sweep * Math.PI / 180 );
        var startPoint = PointOnCircle( center, radius, startRadians );
        var endPoint = PointOnCircle( center, radius, endRadians );

        var geometry = new StreamGeometry( );
        using ( var geometryContext = geometry.Open( ) )
        {
            geometryContext.BeginFigure( startPoint, false );
            geometryContext.ArcTo(
                endPoint,
                new Size( radius, radius ),
                0,
                sweep > 180,
                SweepDirection.Clockwise );
            geometryContext.EndFigure( false );
        }

        var progressPen = new Pen( IsUrgent ? UrgentBrush : ProgressBrush, thickness, lineCap: PenLineCap.Round );
        context.DrawGeometry( null, progressPen, geometry );
    }

    private static Point PointOnCircle( Point center, double radius, double radians ) =>
        new( center.X + ( radius * Math.Cos( radians ) ), center.Y + ( radius * Math.Sin( radians ) ) );
}
