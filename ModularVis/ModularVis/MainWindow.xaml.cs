using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;

using EyeXFramework.Wpf;
using Tobii.EyeX.Framework;
using EyeXFramework;

namespace ModularVis
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Point curr = new Point(0, 0);

        GazeTrack t1;
        GazeLine t2;
        FixPoints t3;

        EyeXHost eyeHost;
        public MainWindow()
        {
            InitializeComponent();

            t1 = new GazeTrack(canv);
            t2 = new GazeLine(canv);
            t3 = new FixPoints(canv);

            eyeHost = new EyeXHost();
            eyeHost.Start();
            var gaze = eyeHost.CreateGazePointDataStream(GazePointDataMode.LightlyFiltered);
            gaze.Next += gazePoint;

            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.Render);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 10);
            dispatcherTimer.Tick += new EventHandler(update);
            dispatcherTimer.Start();
        }

        private void gazePoint(object sender, EyeXFramework.GazePointEventArgs e)
        {
            curr.X = e.X;
            curr.Y = e.Y;
        }

        private void update(object sender, EventArgs e)
        {
            Point fromScreen = PointFromScreen(curr);
            t1.next(fromScreen);
            t2.next(fromScreen);
            t3.next(fromScreen);
        }

        private class FixPoints {
            private Canvas canv;
            private Ellipse[] dots;
            private Point[] points;
            private Point potentialFix;
            private Point block;
            private Brush br;
            private int len;
            private double currCount;
            private int fixTime;
            private double startOpacity, endOpacity;
            private double startRadius, endRadius;
            private Line[] lines;
            private double lineWidth;
            private double lineOpacity;
            private int lineInd;


            public FixPoints(Canvas c) {
                canv = c;
                len = 0;
                startRadius = 35;
                endRadius = 0;
                currCount = 1;
                fixTime = 50;
                startOpacity = .5;
                endOpacity = 0;
                br = new SolidColorBrush(System.Windows.Media.Colors.Black);
                dots = new Ellipse[len];
                points = new Point[len];
                lines = new Line[len];
                lineWidth = 5;
                lineOpacity = .5;
                lineInd = 1;
                potentialFix = new Point(0, 0);
                block = new Point(0, 0);
                double opacity = startOpacity;
                double opacityInc = (startOpacity - endOpacity) / len;
                double radius = startRadius;
                double radiusInc = (startRadius - endRadius) / len;
                for (int i = 0; i < len; i++) {
                    points[i] = new Point(-100, -100);
                    dots[i] = new Ellipse();
                    dots[i].Fill = br;
                    dots[i].Opacity = opacity;
                    opacity -= opacityInc;
                    dots[i].Width = radius * 2;
                    dots[i].Height = radius * 2;
                    radius -= radiusInc;
                    Canvas.SetLeft(dots[i], -100);
                    Canvas.SetTop(dots[i], -100);
                    canv.Children.Add(dots[i]);
                    lines[i] = new Line();
                    lines[i].Opacity = lineOpacity;
                    lines[i].Stroke = br;
                    lines[i].StrokeThickness = lineWidth;
                    lines[i].X1 = -100;
                    lines[i].Y1 = -100;
                    lines[i].X2 = -100;
                    lines[i].Y2 = -100;
                    canv.Children.Add(lines[i]);
                }
            }

            public void next(Point p) {
                if (len > 0)
                {
                    if (distance(p, potentialFix) < startRadius && (distance(p, block) > startRadius | fixTime < 15))
                    {
                        currCount++;
                    }
                    else
                    {
                        currCount = (currCount - 6 > 1) ? currCount - 6 : 1;
                    }
                    potentialFix.X = potentialFix.X * ((currCount - 1) / currCount) + p.X * (1 / currCount);
                    potentialFix.Y = potentialFix.Y * ((currCount - 1) / currCount) + p.Y * (1 / currCount);

                    if (currCount > fixTime)
                    {
                        for (int i = len - 1; i > 0; i--)
                        {
                            points[i].X = points[i - 1].X;
                            points[i].Y = points[i - 1].Y;
                        }
                        points[0].X = potentialFix.X;
                        points[0].Y = potentialFix.Y;
                        if(len > 1)
                        {
                            lines[lineInd].X1 = points[0].X;
                            lines[lineInd].Y1 = points[0].Y;
                            lines[lineInd].X2 = points[1].X;
                            lines[lineInd].Y2 = points[1].Y;
                            lineInd = (lineInd + 1) % (len - 1);
                        }
                        for (int i = 0; i < len; i++) {
                            Canvas.SetLeft(dots[i], points[i].X - dots[i].Width/2);
                            Canvas.SetTop(dots[i], points[i].Y - dots[i].Height/2);
                        }
                        currCount = 1;
                        block.X = potentialFix.X;
                        block.Y = potentialFix.Y;
                    }
                }
            }

            public void setLineWidth(double w) {
                lineWidth = w;
                for (int i = 0; i < len; i++) {
                    lines[i].StrokeThickness = lineWidth;
                }
            }

            public void setLineOpacity(double o) {
                lineOpacity = o;
                for (int i = 0; i < len; i++) {
                    lines[i].Opacity = lineOpacity;
                }
            }

            public void setLength(int l) {
                for (int i = 0; i < len; i++) {
                    canv.Children.Remove(dots[i]);
                    canv.Children.Remove(lines[i]);
                }
                len = l;
                dots = new Ellipse[len];
                points = new Point[len];
                lines = new Line[len];
                double opacity = startOpacity;
                double opacityInc = (startOpacity - endOpacity) / len;
                double radius = startRadius;
                double radiusInc = (startRadius - endRadius) / len;
                for (int i = 0; i < len; i++)
                {
                    points[i] = new Point(-100, -100);
                    dots[i] = new Ellipse();
                    dots[i].Fill = br;
                    dots[i].Opacity = opacity;
                    opacity -= opacityInc;
                    dots[i].Width = radius * 2;
                    dots[i].Height = radius * 2;
                    radius -= radiusInc;
                    Canvas.SetLeft(dots[i], -100);
                    Canvas.SetTop(dots[i], -100);
                    canv.Children.Add(dots[i]);
                    lines[i] = new Line();
                    lines[i].Opacity = lineOpacity;
                    lines[i].Stroke = br;
                    lines[i].StrokeThickness = lineWidth;
                    lines[i].X1 = -100;
                    lines[i].Y1 = -100;
                    lines[i].X2 = -100;
                    lines[i].Y2 = -100;
                    canv.Children.Add(lines[i]);
                }
            }

            public void setFixTime(int t) {
                fixTime = t;
            }

            public void setStartRadius(double sr) {
                startRadius = sr;
                double radius = startRadius;
                double radiusInc = (startRadius - endRadius) / len;
                for (int i = 0; i < len; i++) {
                    dots[i].Width = radius * 2;
                    dots[i].Height = radius * 2;
                    Canvas.SetLeft(dots[i], points[i].X - dots[i].Width / 2);
                    Canvas.SetTop(dots[i], points[i].Y - dots[i].Height / 2);
                    radius -= radiusInc;
                }
            }

            public void setEndRadius(double er)
            {
                endRadius = er;
                double radius = startRadius;
                double radiusInc = (startRadius - endRadius) / len;
                for (int i = 0; i < len; i++)
                {
                    dots[i].Width = radius * 2;
                    dots[i].Height = radius * 2;
                    Canvas.SetLeft(dots[i], points[i].X - dots[i].Width / 2);
                    Canvas.SetTop(dots[i], points[i].Y - dots[i].Height / 2);
                    radius -= radiusInc;
                }
            }

            public void setStartOpacity(double so) {
                startOpacity = so;
                double opacity = startOpacity;
                double opacityInc = (startOpacity - endOpacity) / len;
                for (int i = 0; i < len; i++) {
                    dots[i].Opacity = opacity;
                    opacity -= opacityInc;
                }
            }

            public void setEndOpacity(double eo)
            {
                endOpacity = eo;
                double opacity = startOpacity;
                double opacityInc = (startOpacity - endOpacity) / len;
                for (int i = 0; i < len; i++)
                {
                    dots[i].Opacity = opacity;
                    opacity -= opacityInc;
                }
            }

            private double distance(Point a, Point b) {
                return Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
            }
        }

        private class GazeLine {
            private Canvas canv;
            private Line[] trail;
            private Point[] echo;
            private Brush br;
            private int len;
            private double startWidth, endWidth;
            private double startOpacity, endOpacity;
            private double smooth;

            public GazeLine(Canvas c) {
                canv = c;
                br = new SolidColorBrush(System.Windows.Media.Colors.Black);
                len = 0;
                startWidth = 10;
                endWidth = 1;
                startOpacity = 1;
                endOpacity = 0;
                smooth = .7;
                trail = new Line[len];
                echo = new Point[len + 1];
                echo[len] = new Point(0, 0);
                double width = startWidth;
                double widthInc = (startWidth - endWidth) / len;
                double opacity = startOpacity;
                double opacityInc = (startOpacity - endOpacity) / len;
                for (int i = 0; i < len; i++) {
                    echo[i] = new Point(0, 0);
                    trail[i] = new Line();
                    trail[i].X1 = 0;
                    trail[i].X2 = 0;
                    trail[i].Y1 = 0;
                    trail[i].Y2 = 0;
                    trail[i].Stroke = br;
                    trail[i].StrokeThickness = width;
                    width -= widthInc;
                    trail[i].Opacity = opacity;
                    opacity -= opacityInc;
                    canv.Children.Add(trail[i]);
                }
            }

            public void next(Point p) {
                for (int i = len; i > 0; i--) {
                    echo[i].X = echo[i - 1].X;
                    echo[i].Y = echo[i - 1].Y;
                }
                echo[0].X = echo[0].X * smooth + p.X * (1 - smooth);
                echo[0].Y = echo[0].Y * smooth + p.Y * (1 - smooth);
                for (int i = 0; i < len; i++) {
                    trail[i].X1 = echo[i].X;
                    trail[i].Y1 = echo[i].Y;
                    trail[i].X2 = echo[i + 1].X;
                    trail[i].Y2 = echo[i + 1].Y;
                }
            }

            public void setLength(int l) {
                for (int i = 0; i < len; i++) {
                    canv.Children.Remove(trail[i]);
                }
                len = l;
                Point curr = new Point(echo[0].X, echo[0].Y);
                trail = new Line[len];
                echo = new Point[len + 1];
                echo[len] = new Point(curr.X, curr.Y);
                double width = startWidth;
                double widthInc = (startWidth - endWidth) / len;
                double opacity = startOpacity;
                double opacityInc = (startOpacity - endOpacity) / len;
                for (int i = 0; i < len; i++)
                {
                    echo[i] = new Point(curr.X, curr.Y);
                    trail[i] = new Line();
                    trail[i].X1 = curr.X;
                    trail[i].X2 = curr.Y;
                    trail[i].Y1 = curr.X;
                    trail[i].Y2 = curr.Y;
                    trail[i].Stroke = br;
                    trail[i].StrokeThickness = width;
                    width -= widthInc;
                    trail[i].Opacity = opacity;
                    opacity -= opacityInc;
                    canv.Children.Add(trail[i]);
                }
            }

            public void setStartOpacity(double so) {
                startOpacity = so;
                double opacity = startOpacity;
                double opacityInc = (startOpacity - endOpacity) / len;
                for (int i = 0; i < len; i++) {
                    trail[i].Opacity = opacity;
                    opacity -= opacityInc;
                }

            }

            public void setEndOpacity(double eo)
            {
                endOpacity = eo;
                double opacity = startOpacity;
                double opacityInc = (startOpacity - endOpacity) / len;
                for (int i = 0; i < len; i++)
                {
                    trail[i].Opacity = opacity;
                    opacity -= opacityInc;
                }

            }

            public void setStartWidth(double sw) {
                startWidth = sw;
                double width = startWidth;
                double widthInc = (startWidth - endWidth) / len;
                for (int i = 0; i < len; i++) {
                    trail[i].StrokeThickness = width;
                    width -= widthInc;
                }
            }

            public void setEndWidth(double ew)
            {
                endWidth = ew;
                double width = startWidth;
                double widthInc = (startWidth - endWidth) / len;
                for (int i = 0; i < len; i++)
                {
                    trail[i].StrokeThickness = width;
                    width -= widthInc;
                }
            }

            public void setSmooth(double s) {
                smooth = s;
            }
        }

        private class GazeTrack {
            private Canvas canv;
            private Ellipse body;
            private Point prev;
            private Brush br;
            private TrackBlur blur;
            private double outerRadius;
            private double innerRadius;
            private double smooth;
            private double opacity;

            public GazeTrack(Canvas c) {
                canv = c;
                smooth = .7;
                outerRadius = 5;
                innerRadius = 0;
                opacity = 1;
                blur = new TrackBlur(canv, outerRadius);
                br = new SolidColorBrush(System.Windows.Media.Colors.Black);
                prev = new Point(0, 0);
                body = new Ellipse();
                body.Width = outerRadius * 2;
                body.Height = outerRadius * 2;
                body.Stroke = br;
                body.StrokeThickness = outerRadius - innerRadius;
                Canvas.SetLeft(body, 0);
                Canvas.SetTop(body, 0);
                canv.Children.Add(body);
            }

            public void next(Point p)
            {
                prev.X = prev.X * smooth + p.X * (1 - smooth);
                prev.Y = prev.Y * smooth + p.Y * (1 - smooth);
                Canvas.SetLeft(body, prev.X - outerRadius);
                Canvas.SetTop(body, prev.Y - outerRadius);
                blur.next(prev);
            }

            public void setBlurLength(int l) {
                blur.setLength(l, prev);
            }

            public void setBlurStartOpacity(double so) {
                blur.setStartOpacity(so);
            }

            public void setBlurEndOpacity(double eo) {
                blur.setEndOpacity(eo);
            }

            public void setOuterRadius(double or) {
                outerRadius = or;
                body.Width = outerRadius * 2;
                body.Height = outerRadius * 2;
                body.StrokeThickness = outerRadius - innerRadius;
                blur.setRadius(or);
            }

            public void setInnerRadius(double ir)
            {
                innerRadius = ir;
                body.StrokeThickness = outerRadius - innerRadius;
            }

            public void setOpacity(double o) {
                opacity = o;
                body.Opacity = opacity;
            }

            public void setSmooth(double s) {
                smooth = s;
            }
        }

        private class TrackBlur
        {
            private Canvas canv;
            private Ellipse[] lens;
            private Point[] echo;
            private Brush br;
            private double radius;
            private double stretch;
            private int len;
            private double startOpacity, endOpacity;

            public TrackBlur(Canvas c, double rad)
            {
                canv = c;
                br = new SolidColorBrush(System.Windows.Media.Colors.Black);
                radius = rad;
                stretch = 3;
                len = 0;
                startOpacity = .2;
                endOpacity = 0;
                lens = new Ellipse[len];
                echo = new Point[len];
                double opacity = startOpacity;
                double opacityInc = (startOpacity - endOpacity) / len;
                for (int i = 0; i < len; i++)
                {
                    echo[i] = new Point(0, 0);
                    lens[i] = new Ellipse();
                    lens[i].Width = radius * 2;
                    lens[i].Height = radius * 2;
                    lens[i].Stroke = br;
                    lens[i].StrokeThickness = stretch;
                    Canvas.SetLeft(lens[i], 0);
                    Canvas.SetTop(lens[i], 0);
                    lens[i].Opacity = opacity;
                    opacity -= opacityInc;
                    canv.Children.Add(lens[i]);
                }
            }

            public void next(Point p)
            {
                if (len > 0)
                {
                    for (int i = len - 1; i > 0; i--)
                    {
                        echo[i].X = echo[i - 1].X;
                        echo[i].Y = echo[i - 1].Y;
                    }
                    echo[0].X = p.X;
                    echo[0].Y = p.Y;

                    Point currPoint = p;
                    int currEchoInd = 0;
                    Point currEcho = new Point(echo[0].X - currPoint.X, echo[0].Y - currPoint.Y);
                    double currEchoDist = 0;
                    bool sub = false;
                    Canvas.SetLeft(lens[0], currPoint.X - radius);
                    Canvas.SetTop(lens[0], currPoint.Y - radius);
                    for (int i = 1; i < lens.Length; i++)
                    {
                        if (currEchoDist < stretch && sub)
                        {
                            sub = false;
                            currEchoInd++;
                            currEcho.X = echo[currEchoInd].X - currPoint.X;
                            currEcho.Y = echo[currEchoInd].Y - currPoint.Y;
                            currEchoDist = Math.Sqrt(Math.Pow(currEcho.X, 2) + Math.Pow(currEcho.Y, 2));
                        }
                        if (currEchoDist < stretch && !sub)
                        {
                            Canvas.SetLeft(lens[i], p.X - radius);
                            Canvas.SetTop(lens[i], p.Y - radius);
                            lens[i].Visibility = Visibility.Hidden;
                            currEchoInd++;
                            currEcho.X = echo[currEchoInd].X - currPoint.X;
                            currEcho.Y = echo[currEchoInd].Y - currPoint.Y;
                            currEchoDist = Math.Sqrt(Math.Pow(currEcho.X, 2) + Math.Pow(currEcho.Y, 2));
                        }
                        else
                        {
                            double shiftX = 3 * currEcho.X / currEchoDist;
                            double shiftY = 3 * currEcho.Y / currEchoDist;
                            currPoint.X += shiftX;
                            currPoint.Y += shiftY;
                            Canvas.SetLeft(lens[i], currPoint.X - radius);
                            Canvas.SetTop(lens[i], currPoint.Y - radius);
                            lens[i].Visibility = Visibility.Visible;
                            currEchoDist -= stretch;
                            currEcho.X -= shiftX;
                            currEcho.Y -= shiftY;
                            sub = true;
                        }
                    }
                }
            }

            public void setStartOpacity(double so) {
                startOpacity = so;
                double opacity = startOpacity;
                double opacityInc = (startOpacity - endOpacity) / len;
                for (int i = 0; i < len; i++)
                {
                    lens[i].Opacity = opacity;
                    opacity -= opacityInc;
                }
            }

            public void setEndOpacity(double eo)
            {
                endOpacity = eo;
                double opacity = startOpacity;
                double opacityInc = (startOpacity - endOpacity) / len;
                for (int i = 0; i < len; i++)
                {
                    lens[i].Opacity = opacity;
                    opacity -= opacityInc;
                }
            }

            public void setLength(int l, Point p) {
                for (int i = 0; i < len; i++) {
                    canv.Children.Remove(lens[i]);
                }
                len = l;
                lens = new Ellipse[len];
                echo = new Point[len];
                double opacity = startOpacity;
                double opacityInc = (startOpacity - endOpacity) / len;
                for (int i = 0; i < len; i++)
                {
                    echo[i] = new Point(p.X, p.Y);
                    lens[i] = new Ellipse();
                    lens[i].Width = radius * 2;
                    lens[i].Height = radius * 2;
                    lens[i].Stroke = br;
                    lens[i].StrokeThickness = stretch;
                    Canvas.SetLeft(lens[i], p.X - radius);
                    Canvas.SetTop(lens[i], p.Y - radius);
                    lens[i].Opacity = opacity;
                    opacity -= opacityInc;
                    canv.Children.Add(lens[i]);
                }
            }

            public void setRadius(double r) {
                radius = r;
                for (int i = 0; i < len; i++) {
                    lens[i].Width = radius * 2;
                    lens[i].Height = radius * 2;
                }
            }
        }

        private void SmoothEnter_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Enter)) {
                TextBox input = sender as TextBox;
                try
                {
                    t1.setSmooth(Convert.ToDouble(input.Text));
                    t2.setSmooth(Convert.ToDouble(input.Text));
                }
                catch
                {

                }
            }
        }

        private void OpacityEnter_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Enter))
            {
                TextBox input = sender as TextBox;
                try
                {
                    t1.setOpacity(Convert.ToDouble(input.Text));
                }
                catch
                {

                }
            }
        }

        private void LineLength_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Enter))
            {
                TextBox input = sender as TextBox;
                try
                {
                    t2.setLength(Convert.ToInt32(input.Text));
                }
                catch
                {

                }
            }
        }

        private void LineStartWidth_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Enter))
            {
                TextBox input = sender as TextBox;
                try
                {
                    t2.setStartWidth(Convert.ToInt32(input.Text));
                }
                catch
                {

                }
            }
        }

        private void LineEndWidth_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Enter))
            {
                TextBox input = sender as TextBox;
                try
                {
                    t2.setEndWidth(Convert.ToInt32(input.Text));
                }
                catch
                {

                }
            }
        }

        private void LineStartOpacity_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Enter))
            {
                TextBox input = sender as TextBox;
                try
                {
                    t2.setStartOpacity(Convert.ToDouble(input.Text));
                }
                catch
                {

                }
            }
        }

        private void LineEndOpacity_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Enter))
            {
                TextBox input = sender as TextBox;
                try
                {
                    t2.setEndOpacity(Convert.ToDouble(input.Text));
                }
                catch
                {

                }
            }
        }

        private void TrackInnerRad_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Enter))
            {
                TextBox input = sender as TextBox;
                try
                {
                    t1.setInnerRadius(Convert.ToDouble(input.Text));
                }
                catch
                {

                }
            }
        }

        private void TrackOuterRad_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Enter))
            {
                TextBox input = sender as TextBox;
                try
                {
                    t1.setOuterRadius(Convert.ToDouble(input.Text));
                }
                catch
                {

                }
            }
        }

        private void ShadowLength_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Enter))
            {
                TextBox input = sender as TextBox;
                try
                {
                    t1.setBlurLength(Convert.ToInt32(input.Text));
                }
                catch
                {

                }
            }
        }

        private void ShadowStartOpacity_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Enter))
            {
                TextBox input = sender as TextBox;
                try
                {
                    t1.setBlurStartOpacity(Convert.ToDouble(input.Text));
                }
                catch
                {

                }
            }
        }

        private void ShadowEndOpacity_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Enter))
            {
                TextBox input = sender as TextBox;
                try
                {
                    t1.setBlurEndOpacity(Convert.ToDouble(input.Text));
                }
                catch
                {

                }
            }
        }

        private void FixLength_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Enter))
            {
                TextBox input = sender as TextBox;
                try
                {
                    t3.setLength(Convert.ToInt32(input.Text));
                }
                catch
                {

                }
            }
        }

        private void FixStartOpacity_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Enter))
            {
                TextBox input = sender as TextBox;
                try
                {
                    t3.setStartOpacity(Convert.ToDouble(input.Text));
                }
                catch
                {

                }
            }
        }

        private void FixEndOpacity_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Enter))
            {
                TextBox input = sender as TextBox;
                try
                {
                    t3.setEndOpacity(Convert.ToDouble(input.Text));
                }
                catch
                {

                }
            }
        }

        private void FixStartRadius_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Enter))
            {
                TextBox input = sender as TextBox;
                try
                {
                    t3.setStartRadius(Convert.ToDouble(input.Text));
                }
                catch
                {

                }
            }
        }

        private void FixEndRadius_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Enter))
            {
                TextBox input = sender as TextBox;
                try
                {
                    t3.setEndRadius(Convert.ToDouble(input.Text));
                }
                catch
                {

                }
            }
        }

        private void FixTime_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Enter))
            {
                TextBox input = sender as TextBox;
                try
                {
                    t3.setFixTime(Convert.ToInt32(input.Text));
                }
                catch
                {

                }
            }
        }

        private void FixLineWidth_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Enter))
            {
                TextBox input = sender as TextBox;
                try
                {
                    t3.setLineWidth(Convert.ToDouble(input.Text));
                }
                catch
                {

                }
            }
        }

        private void FixLineOpacity_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Enter))
            {
                TextBox input = sender as TextBox;
                try
                {
                    t3.setLineOpacity(Convert.ToDouble(input.Text));
                }
                catch
                {

                }
            }
        }
    }
}
