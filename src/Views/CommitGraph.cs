using System.Collections.Generic;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace SourceGit.Views
{
    public class CommitGraph : Control
    {
        public static readonly DirectProperty<CommitGraph, Models.CommitGraph> GraphProperty =
            AvaloniaProperty.RegisterDirect<CommitGraph, Models.CommitGraph>(
                nameof(Graph),
                static o => o.Graph,
                static (o, v) => o.Graph = v);

        public Models.CommitGraph Graph
        {
            get => _graph;
            set => SetAndRaise(GraphProperty, ref _graph, value);
        }

        public static readonly DirectProperty<CommitGraph, Models.CommitGraphLayout> LayoutProperty =
            AvaloniaProperty.RegisterDirect<CommitGraph, Models.CommitGraphLayout>(
                nameof(Layout),
                static o => o.Layout,
                static (o, v) => o.Layout = v);

        public Models.CommitGraphLayout Layout
        {
            get => _layout;
            set => SetAndRaise(LayoutProperty, ref _layout, value);
        }

        public static readonly StyledProperty<IBrush> DotBrushProperty =
            AvaloniaProperty.Register<CommitGraph, IBrush>(nameof(DotBrush), Brushes.Transparent);

        public IBrush DotBrush
        {
            get => GetValue(DotBrushProperty);
            set => SetValue(DotBrushProperty, value);
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            if (_graph == null || _layout == null || Models.CommitGraph.Pens.Count == 0)
                return;

            var startY = _layout.StartY;
            var clipWidth = _layout.ClipWidth;
            var clipHeight = Bounds.Height;
            var rowHeight = _layout.RowHeight;
            var endY = startY + clipHeight + 28;

            using (context.PushClip(new Rect(_layout.StartX, 0, clipWidth, clipHeight)))
            using (context.PushTransform(Matrix.CreateTranslation(_layout.StartX, -startY)))
            {
                DrawCurves(context, _graph, startY, endY, rowHeight);
                DrawAnchors(context, _graph, startY, endY, rowHeight);
            }
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == GraphProperty ||
                change.Property == LayoutProperty ||
                change.Property == DotBrushProperty)
                InvalidateVisual();
        }

        private void DrawCurves(DrawingContext context, Models.CommitGraph graph, double top, double bottom, double rowHeight)
        {
            var dimmed = new List<(StreamGeometry, int)>();
            var highlighted = new List<(StreamGeometry, int)>();

            foreach (var link in graph.Links)
            {
                var startY = link.Start.Y * rowHeight;
                var endY = link.End.Y * rowHeight;

                if (endY < top)
                    continue;
                if (startY > bottom)
                    break;

                var geo = new StreamGeometry();
                using (var ctx = geo.Open())
                {
                    ctx.BeginFigure(new Point(link.Start.X, startY), false);
                    ctx.QuadraticBezierTo(new Point(link.Control.X, link.Control.Y * rowHeight), new Point(link.End.X, endY));
                }

                (link.IsHighlighted ? highlighted : dimmed).Add((geo, link.Color));
            }

            foreach (var line in graph.Paths)
            {
                var last = new Point(line.Points[0].X, line.Points[0].Y * rowHeight);
                var size = line.Points.Count;
                var endY = line.Points[size - 1].Y * rowHeight;

                if (endY < top)
                    continue;
                if (last.Y > bottom)
                    break;

                var geo = new StreamGeometry();
                using (var ctx = geo.Open())
                {
                    var started = false;
                    var ended = false;
                    for (int i = 1; i < size; i++)
                    {
                        var cur = new Point(line.Points[i].X, line.Points[i].Y * rowHeight);
                        if (cur.Y < top)
                        {
                            last = cur;
                            continue;
                        }

                        if (!started)
                        {
                            ctx.BeginFigure(last, false);
                            started = true;
                        }

                        if (cur.Y > bottom)
                        {
                            cur = new Point(cur.X, bottom);
                            ended = true;
                        }

                        // Lane changes are drawn as smooth S-curves with vertical tangents at both ends.
                        if (cur.X != last.X)
                        {
                            var midY = (last.Y + cur.Y) / 2;
                            ctx.CubicBezierTo(new Point(last.X, midY), new Point(cur.X, midY), cur);
                        }
                        else
                        {
                            ctx.LineTo(cur);
                        }

                        if (ended)
                            break;
                        last = cur;
                    }
                }

                (line.IsHighlighted ? highlighted : dimmed).Add((geo, line.Color));
            }

            foreach (var (geo, color) in dimmed)
                context.DrawGeometry(null, Models.CommitGraph.DimPens[color], geo);

            foreach (var (geo, color) in highlighted)
                context.DrawGeometry(null, Models.CommitGraph.GlowPens[color], geo);

            foreach (var (geo, color) in highlighted)
                context.DrawGeometry(null, Models.CommitGraph.Pens[color], geo);
        }

        private void DrawAnchors(DrawingContext context, Models.CommitGraph graph, double top, double bottom, double rowHeight)
        {
            var halo = DotBrush;

            foreach (var dot in graph.Dots)
            {
                var center = new Point(dot.Center.X, dot.Center.Y * rowHeight);

                if (center.Y < top)
                    continue;
                if (center.Y > bottom)
                    break;

                var pen = dot.IsHighlighted ? Models.CommitGraph.Pens[dot.Color] : Models.CommitGraph.DimPens[dot.Color];
                var ringPen = new Pen(pen.Brush, 2.5);
                switch (dot.Type)
                {
                    case Models.CommitGraph.DotType.Head:
                        if (dot.IsHighlighted)
                            context.DrawEllipse(Models.CommitGraph.GlowPens[dot.Color].Brush, null, center, 15, 15);
                        context.DrawEllipse(halo, null, center, 10.5, 10.5);
                        context.DrawEllipse(null, ringPen, center, 9, 9);
                        context.DrawEllipse(pen.Brush, null, center, 5, 5);
                        break;
                    case Models.CommitGraph.DotType.Merge:
                        context.DrawEllipse(halo, null, center, 9.5, 9.5);
                        context.DrawEllipse(null, ringPen, center, 6.5, 6.5);
                        context.DrawEllipse(pen.Brush, null, center, 2.5, 2.5);
                        break;
                    default:
                        context.DrawEllipse(halo, null, center, 9.5, 9.5);
                        context.DrawEllipse(pen.Brush, null, center, 7, 7);
                        break;
                }
            }
        }

        private Models.CommitGraph _graph = null;
        private Models.CommitGraphLayout _layout = null;
    }
}
