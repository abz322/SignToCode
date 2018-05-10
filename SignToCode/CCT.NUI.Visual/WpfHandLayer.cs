using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows;
using CCT.NUI.Core;
using CCT.NUI.HandTracking;
using System.Globalization;

namespace CCT.NUI.Visual
{

    //This is a Candescent NUI librayr that adds hand layers to the detected hands and fingers. Comments will be made where the developer has made alterations for Sign To Code.
    public class WpfHandLayer : UIElement, IWpfLayer
    {
        private IHandDataSource dataSource;
        private Canvas canvas;

        private Pen redPen = new Pen(Brushes.Red, 3);
        //This is a pen brush that uses the green colour associated with the theme of the application
        private Pen GPen = new Pen(Brushes.Green, 2);
        private Pen greenPen = new Pen(Brushes.Transparent, 1);
        private Pen whitePen = new Pen(Brushes.Transparent, 1);
        private Pen orangePen = new Pen(Brushes.Transparent, 1);

        private Typeface typeFace = new Typeface(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);

        public WpfHandLayer(IHandDataSource dataSource)
        {
            this.dataSource = dataSource;
            this.dataSource.NewDataAvailable += dataSource_NewDataAvailable;
        }

        public void Activate(Canvas canvas)
        {
            this.canvas = canvas;
            this.canvas.Children.Add(this);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            foreach (var hand in this.dataSource.CurrentValue.Hands)
            {
                this.DrawHand(hand, drawingContext);
            }
        }

        private void DrawHand(HandData hand, DrawingContext drawingContext)
        {
            this.PaintCovexHull(hand, drawingContext);
            if (hand.Contour != null)
            {
                this.PaintContour(hand, drawingContext);
            }
            this.DrawFingerPoints(hand, drawingContext);
            this.DrawCenter(hand, drawingContext);
        }

        //Draws the palm of a hand
        protected virtual void DrawCenter(HandData hand, DrawingContext drawingContext)
        {
            drawingContext.DrawEllipse(Brushes.White, null, new System.Windows.Point(hand.Location.X, hand.Location.Y + 10), 5, 5);
        }

        //Draws fingers
        protected virtual void PaintContour(HandData hand, DrawingContext drawingContext)
        {
            if (hand.Contour.Points.Count > 1)
            {
                var points = hand.Contour.Points.Select(p => new System.Windows.Point(p.X, p.Y)).ToArray();
              
                DrawLines(drawingContext, this.GPen, points, true);
            }
        }

        //Draws convex hull
        protected virtual void PaintCovexHull(HandData cluster, DrawingContext drawingContext)
        {
            if (cluster.ConvexHull.Count > 3)
            {
                this.DrawLines(drawingContext, this.whitePen, cluster.ConvexHull.Points.Select(p => new System.Windows.Point(p.X, p.Y)).ToArray(), false);
            }
        }

        protected virtual void DrawFingerPoints(HandData cluster, DrawingContext drawingContext)
        {
            foreach (var point in cluster.FingerPoints)
            {
                PaintFingerPoint(point, drawingContext);
            }
        }

        //Draws dots for the finger tips
        private void PaintFingerPoint(FingerPoint point, DrawingContext drawingContext)
        {
            drawingContext.DrawEllipse(Brushes.BlueViolet, null, new System.Windows.Point(point.X, point.Y), 5.5, 5.5);

            if (!CCT.NUI.Core.Point.IsZero(point.BaseLeft) && !CCT.NUI.Core.Point.IsZero(point.BaseRight))
            {
                var baseCenter = CCT.NUI.Core.Point.Center(point.BaseLeft, point.BaseRight);

                drawingContext.DrawLine(this.orangePen, new System.Windows.Point(baseCenter.X, baseCenter.Y), new System.Windows.Point(point.X + point.DirectionVector.X * 60, point.Y + point.DirectionVector.Y * 60));
            }
        }

        //Function to draw the lines
        private void DrawLines(DrawingContext drawingContext, Pen pen, System.Windows.Point[] points, bool fingers)
        {
            var pathGeometry = new PathGeometry();
            var figure = new PathFigure(points.First(), points.Skip(1).Select(p => new LineSegment(p, true)), true);
            
            pathGeometry.Figures = new PathFigureCollection { figure };
            pathGeometry.FillRule = FillRule.Nonzero;
            if (fingers == true)
            {
                drawingContext.DrawGeometry(Brushes.DarkGreen, pen, pathGeometry);
            }
            else
            {
                drawingContext.DrawGeometry(null, pen, pathGeometry);
            }
        }

        public void Dispose()
        {
            this.dataSource.NewDataAvailable -= new NewDataHandler<HandCollection>(dataSource_NewDataAvailable);
            if (this.canvas != null)
            {
                this.canvas.Children.Remove(this);
            }
        }

        private void dataSource_NewDataAvailable(HandCollection hands)
        {
            this.Dispatcher.Invoke(new Action(() => InvalidateVisual()));
        }
    }
}
