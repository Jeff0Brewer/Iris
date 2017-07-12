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
using System.Windows.Media.Effects;

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
        //UI
        bool freeze = false;

        //Vis
        GazeTrack t1;
        GazeLine t2;
        FixPoints t3;

        //Coloring
        CanColor toColor;
        int id;

        EyeXHost eyeHost;
        Point curr = new Point(0, 0);

        public MainWindow()
        {
            InitializeComponent();

            eyeHost = new EyeXHost();
            eyeHost.Start();
            var gaze = eyeHost.CreateGazePointDataStream(GazePointDataMode.LightlyFiltered);
            gaze.Next += gazePoint;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            bg.Width = canv.ActualWidth;
            bg.Height = canv.ActualHeight;

            t1 = new GazeTrack(canv);
            t2 = new GazeLine(canv);
            t3 = new FixPoints(canv);

            toColor = t1;
            id = 0;

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

            if (!freeze)
            {
                t1.next(fromScreen);
                t2.next(fromScreen);
                t3.next(fromScreen);
            }
        }

        public int max(int a, int b) {
            return (a > b) ? a : b;
        }

        public abstract class CanColor {
            public abstract void setColor(int id, Brush b);
        }

        public class EnvLens {
            private Canvas canv;
            private Ellipse lens;
            private Point prev;
            private int radius;
            private double smooth;
            private string img;
            private double zoom;

            public EnvLens(Canvas c, String i) {
                canv = c;
                img = i;
                prev = new Point(100, 100);
                radius = 70;
                smooth = .7;
                zoom = 1;
                lens = new Ellipse();
                lens.Width = 2 * radius;
                lens.Height = 2 * radius;
                Panel.SetZIndex(lens, 51);
                lens.Fill = new SolidColorBrush(Colors.Black);
                canv.Children.Add(lens);
            }

            public void next(Point p) {
                prev.X = prev.X * smooth + p.X * (1 - smooth);
                prev.Y = prev.Y * smooth + p.Y * (1 - smooth);

                Canvas.SetLeft(lens, prev.X - radius);
                Canvas.SetTop(lens, prev.Y - radius);

                Rectangle bg = canv.FindName("bg") as Rectangle;

                try
                {
                    BitmapImage src = new BitmapImage();
                    src.BeginInit();
                    src.UriSource = new Uri(img, UriKind.Relative);
                    src.CacheOption = BitmapCacheOption.OnLoad;
                    src.EndInit();
                    double ratioX = src.PixelWidth / bg.Width;
                    double ratioY = src.PixelHeight / bg.Height;
                    ImageBrush temp = new ImageBrush(new CroppedBitmap(src, new Int32Rect((int)((prev.X - radius*zoom)*ratioX),
                                                                                          (int)((prev.Y - radius*zoom)*ratioY),
                                                                                          (int)((2*radius*zoom)*ratioX), 
                                                                                          (int)((2*radius*zoom)*ratioY))));
                    temp.Stretch = Stretch.Fill;
                    lens.Fill = temp;
                }
                catch { }
            }
        }

        private class Warp {
            private Canvas canv;
            private Polygon[,] frame;
            private Point[,] points;
            private Point prev;
            private double pull;
            private double smooth;
            private double width, height;
            private int dwidth, dheight;
            private double stepX, stepY;

            public Warp(Canvas c, string img) {
                canv = c;
                prev = new Point(0, 0);
                pull = 10;
                smooth = .7; width = canv.ActualWidth - SystemParameters.WindowNonClientFrameThickness.Left - SystemParameters.WindowNonClientFrameThickness.Right;
                height = canv.ActualHeight - SystemParameters.WindowNonClientFrameThickness.Top - SystemParameters.WindowNonClientFrameThickness.Bottom;
                dwidth = 30;
                dheight = (int)(height * (dwidth / width));
                stepX = width / (dwidth - 1);
                stepY = height / (dheight - 1);
                frame = new Polygon[dheight-1, dwidth-1];
                points = new Point[dheight, dwidth];
                for (int y = 0; y < dheight; y++) {
                    for (int x = 0; x < dwidth; x++) {
                        points[y, x] = new Point(x*stepX, y*stepY);
                    }
                }
                BitmapImage src = new BitmapImage();
                src.BeginInit();
                src.UriSource = new Uri(img, UriKind.Relative);
                src.CacheOption = BitmapCacheOption.OnLoad;
                src.EndInit();

                for (int y = 0; y < dheight - 1; y++) {
                    for (int x = 0; x < dwidth - 1; x++) {
                        frame[y, x] = new Polygon();
                        frame[y, x].Points.Add(points[y, x]);
                        frame[y, x].Points.Add(points[y, x + 1]);
                        frame[y, x].Points.Add(points[y + 1, x + 1]);
                        frame[y, x].Points.Add(points[y + 1, x]);
                        ImageBrush temp = new ImageBrush(new CroppedBitmap(src, new Int32Rect((int)(x * stepX), (int)(y * stepY), (int)(stepX), (int)(stepY))));
                        temp.Stretch = Stretch.Fill;
                        frame[y, x].Fill = temp;
                        canv.Children.Add(frame[y, x]);
                    }
                }
            }

            public void next(Point p) {
                prev.X = prev.X * smooth + p.X * (1 - smooth);
                prev.Y = prev.Y * smooth + p.Y * (1 - smooth);
                
                double thisPull;
                Point thisStart = new Point();
                for (int y = 0; y < dheight; y++){
                    for (int x = 0; x < dwidth; x++){
                        thisStart.X = x * stepX;
                        thisStart.Y = y * stepY;
                        thisPull = pull / distance(prev, thisStart);
                        thisPull = min(thisPull, .9);
                        points[y, x].X = thisStart.X * (1 - thisPull) + prev.X * thisPull;
                        points[y, x].Y = thisStart.Y * (1 - thisPull) + prev.Y * thisPull;
                    }
                }
                for (int y = 0; y < dheight - 1; y++){
                    for (int x = 0; x < dwidth - 1; x++){
                        frame[y, x].Points[0] = points[y, x];
                        frame[y, x].Points[1] = points[y, x + 1];
                        frame[y, x].Points[2] = points[y + 1, x + 1];
                        frame[y, x].Points[3] = points[y + 1, x];
                    }
                }
            }

            private double min(double a, double b)
            {
                return (a < b) ? a : b;
            }

            private double distance(Point a, Point b)
            {
                return Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
            }
        }

        private class EnvGaze : CanColor {
            private Canvas canv;
            private double width, height;
            private Ellipse[,] dots;
            private Brush br;
            private Point prev;
            private double radius;
            private double opacity;
            private double pull;
            private double smooth;
            private int dwidth, dheight;
            private double stepX, stepY;
            
            public EnvGaze(Canvas c) {
                canv = c;
                br = new SolidColorBrush(Colors.Black);
                prev = new Point(0, 0);
                width = canv.ActualWidth - SystemParameters.WindowNonClientFrameThickness.Left - SystemParameters.WindowNonClientFrameThickness.Right;
                height = canv.ActualHeight - SystemParameters.WindowNonClientFrameThickness.Top - SystemParameters.WindowNonClientFrameThickness.Bottom;
                radius = 3;
                opacity = .5;
                pull = 30;
                smooth = .7;
                dwidth = 15;
                dheight = (int)(height * (dwidth / width));
                stepX = width / (dwidth - 1);
                stepY = height / (dheight - 1);
                dots = new Ellipse[dheight, dwidth];
                for (int y = 0; y < dheight; y++) {
                    for (int x = 0; x < dwidth; x++) {
                        dots[y, x] = new Ellipse();
                        dots[y, x].Width = radius * 2;
                        dots[y, x].Height = radius * 2;
                        dots[y, x].Opacity = opacity;
                        dots[y, x].Fill = br;
                        Canvas.SetLeft(dots[y, x], x * stepX);
                        Canvas.SetTop(dots[y, x], y * stepY);
                        canv.Children.Add(dots[y, x]);
                    }
                }
            }

            public void next(Point p)
            {
                prev.X = prev.X * smooth + p.X * (1 - smooth);
                prev.Y = prev.Y * smooth + p.Y * (1 - smooth);

                double thisPull;
                Point thisStart = new Point();
                for (int y = 0; y < dheight; y++){
                    for (int x = 0; x < dwidth; x++){
                        thisStart.X = x * stepX;
                        thisStart.Y = y * stepY;
                        thisPull = pull / distance(prev, thisStart);
                        thisPull = min(thisPull, .9);
                        Canvas.SetLeft(dots[y, x], thisStart.X * (1 - thisPull) + prev.X * thisPull);
                        Canvas.SetTop(dots[y, x], thisStart.Y * (1 - thisPull) + prev.Y * thisPull);
                    }
                }
            }

            public void setDensity(int d) {
                for (int y = 0; y < dheight; y++) {
                    for (int x = 0; x < dwidth; x++) {
                        canv.Children.Remove(dots[y, x]);
                    }
                }
                dwidth = d;
                dheight = (int)(height * (dwidth / width));
                stepX = width / (dwidth - 1);
                stepY = height / (dheight - 1);
                dots = new Ellipse[dheight, dwidth];
                for (int y = 0; y < dheight; y++){
                    for (int x = 0; x < dwidth; x++){
                        dots[y, x] = new Ellipse();
                        dots[y, x].Width = radius * 2;
                        dots[y, x].Height = radius * 2;
                        dots[y, x].Opacity = opacity;
                        dots[y, x].Fill = br;
                        Canvas.SetLeft(dots[y, x], x * stepX);
                        Canvas.SetTop(dots[y, x], y * stepY);
                        canv.Children.Add(dots[y, x]);
                    }
                }
            }

            public override void setColor(int id, Brush b) {

            }

            private double min(double a, double b) {
                return (a < b) ? a : b;
            }

            private double distance(Point a, Point b){
                return Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
            }
        }

        private class FixPoints : CanColor
        {
            private Canvas canv;
            private Ellipse[] dots;
            private Point[] points;
            private Point potentialFix;
            private Point block;
            private Point prev;
            private Brush dbr;
            private Brush lbr;
            private int len;
            private double currCount;
            private int fixTime;
            private double startOpacity, endOpacity;
            private double startRadius, endRadius;
            private Line[] lines;
            private double lineWidth;
            private double lineOpacity;
            private double activeLineOpacity;
            private double smooth;
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
                smooth = .7;
                prev = new Point(0, 0);
                dbr = new SolidColorBrush(System.Windows.Media.Colors.Black);
                lbr = new SolidColorBrush(System.Windows.Media.Colors.Black);
                dots = new Ellipse[len];
                points = new Point[len];
                lines = new Line[len];
                lineWidth = 5;
                lineOpacity = .5;
                activeLineOpacity = 0;
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
                    dots[i].Fill = dbr;
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
                    lines[i].Stroke = lbr;
                    lines[i].StrokeThickness = lineWidth;
                    lines[i].X1 = -100;
                    lines[i].Y1 = -100;
                    lines[i].X2 = -100;
                    lines[i].Y2 = -100;
                    lines[i].StrokeEndLineCap = PenLineCap.Round;
                    lines[i].StrokeStartLineCap = PenLineCap.Round;
                    canv.Children.Add(lines[i]);
                }
            }

            public void next(Point p) {
                prev.X = prev.X * smooth + p.X * (1 - smooth);
                prev.Y = prev.Y * smooth + p.Y * (1 - smooth);
                if (len > 0)
                {
                    if (fixTime < 10 || (distance(prev, potentialFix) < startRadius && distance(prev, block) > startRadius))
                    {
                        currCount++;
                    }
                    else
                    {
                        currCount = (currCount - 6 > 1) ? currCount - 6 : 1;
                    }
                    potentialFix.X = potentialFix.X * ((currCount - 1) / currCount) + prev.X * (1 / currCount);
                    potentialFix.Y = potentialFix.Y * ((currCount - 1) / currCount) + prev.Y * (1 / currCount);


                    lines[len - 1].X2 = points[0].X;
                    lines[len - 1].Y2 = points[0].Y;
                    lines[len - 1].X1 = prev.X;
                    lines[len - 1].Y1 = prev.Y;

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

            public override void setColor(int id, Brush b) {
                if (id == 0)
                {
                    for (int i = 0; i < len; i++)
                    {
                        dbr = b;
                        dots[i].Fill = dbr;
                    }
                }
                else
                {
                    for (int i = 0; i < len; i++)
                    {
                        lbr = b;
                        lines[i].Stroke = lbr;
                    }
                }
            }

            public void setLineWidth(double w) {
                lineWidth = w;
                for (int i = 0; i < len; i++) {
                    lines[i].StrokeThickness = lineWidth;
                }
            }

            public void setActiveLineOpacity(double o) {
                activeLineOpacity = o;
                lines[len-1].Opacity = activeLineOpacity;
            }

            public void setLineOpacity(double o) {
                lineOpacity = o;
                for (int i = 0; i < len - 1; i++) {
                    lines[i].Opacity = lineOpacity;
                }
            }

            public void setLength(int l) {
                lineInd = 0;
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
                    dots[i].Fill = dbr;
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
                    lines[i].Stroke = lbr;
                    lines[i].StrokeThickness = lineWidth;
                    lines[i].X1 = -100;
                    lines[i].Y1 = -100;
                    lines[i].X2 = -100;
                    lines[i].Y2 = -100;
                    lines[i].StrokeEndLineCap = PenLineCap.Round;
                    lines[i].StrokeStartLineCap = PenLineCap.Round;
                    canv.Children.Add(lines[i]);
                }
                lines[len - 1].Opacity = activeLineOpacity;
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

            public void setSmooth(double s) {
                smooth = s;
            }

            private double distance(Point a, Point b) {
                return Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
            }
        }

        private class GazeLine : CanColor {
            private Canvas canv;
            private Line[] trail;
            private Point[] echo;
            private Brush br;
            private int len;
            private double startWidth, endWidth;
            private double startOpacity, endOpacity;
            private double smooth;
            //UI
            private Rectangle overlay;
            private bool setLen;
            private int clickedInd;

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
                    trail[i].StrokeEndLineCap = PenLineCap.Round;
                    trail[i].Name = "l" + i.ToString();
                    trail[i].PreviewMouseRightButtonDown += startLengthAdjust;
                    trail[i].PreviewMouseLeftButtonDown += startWidthAdjust;
                    trail[i].MouseWheel += opacityAdjust;
                    width -= widthInc;
                    trail[i].Opacity = opacity;
                    opacity -= opacityInc;
                    canv.Children.Add(trail[i]);
                }
                //UI
                overlay = new Rectangle();
                overlay.Width = 3000;
                overlay.Height = 3000;
                overlay.Fill = br;
                overlay.Opacity = 0;
                Canvas.SetTop(overlay, 3000);
                Panel.SetZIndex(overlay, 1000);
                overlay.PreviewMouseMove += trail_dragged;
                overlay.PreviewMouseUp += trail_unclicked;
                canv.Children.Add(overlay);
            }

            private void opacityAdjust(object sender, MouseWheelEventArgs e) {
                clickedInd = Convert.ToInt32((sender as Line).Name.Substring(1));
                if (clickedInd > len / 2)
                {
                    endOpacity += (double)e.Delta / 3000;
                    endOpacity = (endOpacity > 1) ? 1 : endOpacity;
                    endOpacity = (endOpacity < 0) ? 0 : endOpacity;
                    setEndOpacity(endOpacity);
                }
                else {
                    startOpacity += (double)e.Delta / 3000;
                    startOpacity = (startOpacity > 1) ? 1 : startOpacity;
                    startOpacity = (startOpacity < 0) ? 0 : startOpacity;
                    setStartOpacity(startOpacity);
                }
            }

            private void startWidthAdjust(object sender, MouseButtonEventArgs e) {
                setLen = false;
                clickedInd = Convert.ToInt32((sender as Line).Name.Substring(1));
                Canvas.SetTop(overlay, 0);
            }

            private void startLengthAdjust(object sender, MouseButtonEventArgs e) {
                setLen = true;
                clickedInd = Convert.ToInt32((sender as Line).Name.Substring(1));
                setLength(clickedInd + 1);
                Canvas.SetTop(overlay, 0);
            }

            private void trail_dragged(object sender, MouseEventArgs e) {
                if (distance(echo[len], e.GetPosition(canv)) > 10 && setLen) {
                    setLength(e.GetPosition(canv));
                }
                if (!setLen) {
                    Point a = e.GetPosition(canv);
                    a.X -= echo[clickedInd].X;
                    a.Y -= echo[clickedInd].Y;
                    Point b = echo[clickedInd + 1];
                    b.X -= echo[clickedInd].X;
                    b.Y -= echo[clickedInd].Y;
                    double w = distance(echo[clickedInd], e.GetPosition(canv)) * Math.Sin(Math.Acos((a.X * b.X + a.Y * b.Y)/(distance(a,new Point(0,0))*distance(b,new Point(0,0)))));
                    if (clickedInd > len / 2)
                    {
                        setEndWidth(2*w);
                    }
                    else {
                        setStartWidth(2*w);
                    }
                }
            }

            private void trail_unclicked(object sender, MouseButtonEventArgs e) {
                Canvas.SetTop(overlay, 3000);
            }

            private double distance(Point a, Point b)
            {
                return Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
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

            public override void setColor(int id, Brush b) {
                br = b;
                for (int i = 0; i < len; i++) {
                    trail[i].Stroke = br;
                }
            }

            public void setLength(int l) {
                for (int i = 0; i < len; i++) {
                    canv.Children.Remove(trail[i]);
                }
                len = l;
                trail = new Line[len];
                Point[] temp = new Point[len + 1];
                int setNum = min(echo.Length, temp.Length);
                Array.Copy(echo, temp, setNum);
                echo = temp;
                double width = startWidth;
                double widthInc = (startWidth - endWidth) / len;
                double opacity = startOpacity;
                double opacityInc = (startOpacity - endOpacity) / len;
                for (int i = 0; i < len; i++)
                {
                    trail[i] = new Line();
                    trail[i].X1 = echo[i].X;
                    trail[i].Y1 = echo[i].Y;
                    trail[i].X2 = echo[i + 1].X;
                    trail[i].Y2 = echo[i + 1].Y;
                    trail[i].Stroke = br;
                    trail[i].StrokeThickness = width;
                    trail[i].StrokeEndLineCap = PenLineCap.Round;
                    trail[i].Name = "l" + i.ToString();
                    trail[i].PreviewMouseRightButtonDown += startLengthAdjust;
                    trail[i].PreviewMouseLeftButtonDown += startWidthAdjust;
                    trail[i].MouseWheel += opacityAdjust;
                    width -= widthInc;
                    trail[i].Opacity = opacity;
                    opacity -= opacityInc;
                    canv.Children.Add(trail[i]);
                }
            }

            public void setLength(Point p)
            {
                len = len + 1;
                Line[] tempLine = new Line[len];
                Array.Copy(trail, tempLine, len - 1);
                trail = tempLine;
                Point[] tempEcho = new Point[len + 1];
                int setNum = min(echo.Length, tempEcho.Length);
                Array.Copy(echo, tempEcho, setNum);
                echo = tempEcho;
                echo[len].X = p.X;
                echo[len].Y = p.Y;
                trail[len - 1] = new Line();
                trail[len - 1].X1 = echo[len - 1].X;
                trail[len - 1].Y1 = echo[len - 1].Y;
                trail[len - 1].X2 = echo[len].X;
                trail[len - 1].Y2 = echo[len].Y;
                trail[len - 1].Stroke = br;
                trail[len - 1].PreviewMouseRightButtonDown += startLengthAdjust;
                trail[len - 1].PreviewMouseLeftButtonDown += startWidthAdjust;
                trail[len - 1].MouseWheel += opacityAdjust;
                canv.Children.Add(trail[len - 1]);
                double width = startWidth;
                double widthInc = (startWidth - endWidth) / len;
                double opacity = startOpacity;
                double opacityInc = (startOpacity - endOpacity) / len;
                for (int i = 0; i < len; i++)
                {
                    trail[i].Stroke = br;
                    trail[i].StrokeThickness = width;
                    trail[i].StrokeEndLineCap = PenLineCap.Round;
                    trail[i].Name = "l" + i.ToString();
                    width -= widthInc;
                    trail[i].Opacity = opacity;
                    opacity -= opacityInc;
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

            private int min(int a, int b) {
                return (a < b) ? a : b;
            }
        }

        private class GazeTrack : CanColor {
            private Canvas canv;
            private Ellipse body;
            private Point prev;
            private Brush br;
            private TrackBlur blur;
            private double outerRadius;
            private double innerRadius;
            private double smooth;
            private double opacity;
            //EnvColoring
            private bool env;
            private string img;
            private double zoom;
            private BitmapImage src;
            private double ratioX, ratioY;
            //UI
            private Rectangle overlay;
            private bool setInner;
            private bool setLen;


            public GazeTrack(Canvas c) {
                canv = c;
                smooth = .7;
                outerRadius = 50;
                innerRadius = 0;
                opacity = 1;
                blur = new TrackBlur(canv, outerRadius);
                br = new SolidColorBrush(System.Windows.Media.Colors.Black);
                prev = new Point(0, 0);
                body = new Ellipse();
                body.Opacity = opacity;
                body.Width = outerRadius * 2;
                body.Height = outerRadius * 2;
                body.Stroke = br;
                body.StrokeThickness = outerRadius - innerRadius;
                Canvas.SetLeft(body, 0);
                Canvas.SetTop(body, 0);
                Panel.SetZIndex(body, 50);
                body.PreviewMouseLeftButtonDown += body_leftclicked;
                body.PreviewMouseRightButtonDown += body_rightclicked;
                body.MouseWheel += body_scrolled;
                canv.Children.Add(body);
                //UI
                overlay = new Rectangle();
                overlay.Width = 3000;
                overlay.Height = 3000;
                overlay.Fill = br;
                overlay.Opacity = 0;
                Canvas.SetTop(overlay, 3000);
                Panel.SetZIndex(overlay, 1000);
                overlay.PreviewMouseMove += body_dragged;
                overlay.PreviewMouseUp += body_unclicked;
                canv.Children.Add(overlay);
                //EnvColoring
                env = false;
                img = "";
                zoom = .95;
                src = new BitmapImage();
            }

            private void body_scrolled(object sender, MouseWheelEventArgs e) {
                opacity += (double)e.Delta / 1500;
                opacity = (opacity > 1) ? 1 : opacity;
                opacity = (opacity < 0) ? 0 : opacity;
                body.Opacity = opacity;
            }

            private void body_rightclicked(object sender, MouseButtonEventArgs e){
                setLen = true;
                blur.remove();
                Canvas.SetTop(overlay, 0);
            }

            private void body_leftclicked(object sender, MouseButtonEventArgs e) {
                setLen = false;
                double dist = Math.Sqrt(Math.Pow(e.GetPosition(canv).X - (Canvas.GetLeft(body) + outerRadius),2) + Math.Pow(e.GetPosition(canv).Y - (Canvas.GetTop(body) + outerRadius), 2));
                setInner = dist < innerRadius + (outerRadius - innerRadius) / 2;
                Canvas.SetTop(overlay, 0);
            }

            private void body_dragged(object sender, MouseEventArgs e) {
                double dist = Math.Sqrt(Math.Pow(e.GetPosition(canv).X - (Canvas.GetLeft(body) + outerRadius), 2) + Math.Pow(e.GetPosition(canv).Y - (Canvas.GetTop(body) + outerRadius), 2));
                if (!setLen)
                {
                    if (setInner)
                    {
                        dist = (dist < 5) ? 0 : dist;
                        dist = (dist > outerRadius - 3) ? outerRadius - 3 : dist;
                        setInnerRadius(dist);
                    }
                    else
                    {
                        dist = (dist < innerRadius + 3) ? innerRadius + 3 : dist;
                        setOuterRadius(dist);
                    }
                }
                else {
                    blur.setLength(e.GetPosition(canv), prev);
                }
            }

            private void body_unclicked(object sender, MouseButtonEventArgs e) {
                Canvas.SetTop(overlay, 3000);
                if (blur.len == 1) {
                    blur.setLength(0, prev);
                }
            }
            
            public void next(Point p)
            {
                prev.X = prev.X * smooth + p.X * (1 - smooth);
                prev.Y = prev.Y * smooth + p.Y * (1 - smooth);
                Canvas.SetLeft(body, prev.X - outerRadius);
                Canvas.SetTop(body, prev.Y - outerRadius);

                if (env) {
                    try
                    {
                        ImageBrush temp = new ImageBrush(new CroppedBitmap(src, new Int32Rect((int)((prev.X - outerRadius * zoom) * ratioX),
                                                                                              (int)((prev.Y - outerRadius * zoom) * ratioY),
                                                                                              (int)((2 * outerRadius * zoom) * ratioX),
                                                                                              (int)((2 * outerRadius * zoom) * ratioY))));
                        temp.Stretch = Stretch.Fill;
                        body.Stroke = temp;
                    }
                    catch { }
                }


                blur.next(prev);
            }

            public override void setColor(int id, Brush b) {
                if (id == 0){
                    br = b;
                    body.Stroke = br;
                }
                else {
                    blur.setColor(b);
                }
                env = false;
            }

            public void setEnv(String path) {
                opacity = 1;
                body.Opacity = opacity;
                img = path;
                env = true;
                src = new BitmapImage();
                src.BeginInit();
                src.UriSource = new Uri(img, UriKind.Relative);
                src.CacheOption = BitmapCacheOption.OnLoad;
                src.EndInit();
                Rectangle bg = canv.FindName("bg") as Rectangle;
                ratioX = src.PixelWidth / bg.Width;
                ratioY = src.PixelHeight / bg.Height;
            }

            public void setEnvZoom(double z) {
                zoom = z;
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
                Canvas.SetLeft(body, prev.X - outerRadius);
                Canvas.SetTop(body, prev.Y - outerRadius);
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
            public int len;
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
                    lens[i].Name = "s" + i.ToString();
                    lens[i].MouseWheel += lens_scrolled;
                    opacity -= opacityInc;
                    canv.Children.Add(lens[i]);
                }
            }

            private void lens_scrolled(object sender, MouseWheelEventArgs e) {
                double fromBody = distance(echo[0], e.GetPosition(canv)) - radius;
                Point endLens = new Point(Canvas.GetLeft(lens[0]) + radius, Canvas.GetTop(lens[0]) + radius);
                double toEnd = distance(echo[0], endLens) + radius;
                if (fromBody < toEnd){
                    startOpacity += (double)e.Delta / 3000;
                    startOpacity = (startOpacity > 1) ? 1 : startOpacity;
                    startOpacity = (startOpacity < 0) ? 0 : startOpacity;
                    setStartOpacity(startOpacity);
                }
                else {
                    endOpacity += (double)e.Delta / 3000;
                    endOpacity = (endOpacity > 1) ? 1 : endOpacity;
                    endOpacity = (endOpacity < 0) ? 0 : endOpacity;
                    setEndOpacity(endOpacity);
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

            public void setColor(Brush b) {
                br = b;
                for (int i = 0; i < len; i++) {
                    lens[i].Stroke = br;
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
                Point[] temp = new Point[len];
                int setNum = min(echo.Length, temp.Length);
                Array.Copy(echo, temp, setNum);
                echo = temp;
                double opacity = startOpacity;
                double opacityInc = (startOpacity - endOpacity) / len;
                for (int i = 0; i < len; i++)
                {
                    lens[i] = new Ellipse();
                    lens[i].Width = radius * 2;
                    lens[i].Height = radius * 2;
                    lens[i].Stroke = br;
                    lens[i].StrokeThickness = stretch;
                    Canvas.SetLeft(lens[i], p.X - radius);
                    Canvas.SetTop(lens[i], p.Y - radius);
                    lens[i].Opacity = opacity;
                    lens[i].Name = "s" + i.ToString();
                    lens[i].MouseWheel += lens_scrolled;
                    opacity -= opacityInc;
                    canv.Children.Add(lens[i]);
                }
            }

            public void setLength(Point p, Point source) {
                Point currEnd;
                if (len == 0)
                    currEnd = source;
                else
                    currEnd = new Point(Canvas.GetLeft(lens[len - 1]) + radius, Canvas.GetTop(lens[len - 1]) + radius);
                double dist = distance(currEnd, p);
                int numAdd = (int)((dist - radius) / stretch);
                if (numAdd > 0)
                {
                    len = len + numAdd;
                    Ellipse[] tempLens = new Ellipse[len];
                    Array.Copy(lens, tempLens, lens.Length);
                    lens = tempLens;
                    Point[] tempEcho = new Point[len];
                    int prevLen = echo.Length;
                    Array.Copy(echo, tempEcho, prevLen);
                    echo = tempEcho;
                    for (int i = prevLen; i < echo.Length; i++)
                    {
                        echo[i] = new Point(p.X, p.Y);
                    }
                    double xRatio = (p.X - currEnd.X) / dist;
                    double yRatio = (p.Y - currEnd.Y) / dist;
                    double currStep = stretch;
                    for (int i = prevLen; i < lens.Length; i++)
                    {
                        lens[i] = new Ellipse();
                        lens[i].Width = radius * 2;
                        lens[i].Height = radius * 2;
                        lens[i].Stroke = br;
                        lens[i].StrokeThickness = stretch;
                        Canvas.SetLeft(lens[i], currEnd.X + currStep * xRatio - radius);
                        Canvas.SetTop(lens[i], currEnd.Y + currStep * yRatio - radius);
                        lens[i].Name = "s" + i.ToString();
                        lens[i].MouseWheel += lens_scrolled;
                        currStep += stretch;
                        canv.Children.Add(lens[i]);
                    }
                    setStartOpacity(startOpacity);
                }
            }

            public void remove() {
                for (int i = 0; i < len; i++) {
                    canv.Children.Remove(lens[i]);
                }
                len = 0;
                echo = new Point[0];
                lens = new Ellipse[0];
            }

            public void setRadius(double r) {
                for (int i = 0; i < len; i++) {
                    Point center = new Point(Canvas.GetLeft(lens[i]) + radius, Canvas.GetTop(lens[i]) + radius);
                    lens[i].Width = r * 2;
                    lens[i].Height = r * 2;
                    Canvas.SetLeft(lens[i], center.X - lens[i].Width / 2);
                    Canvas.SetTop(lens[i], center.Y - lens[i].Height / 2);
                }
                radius = r;
            }

            private int min(int a, int b)
            {
                return (a < b) ? a : b;
            }

            private double distance(Point a, Point b)
            {
                return Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
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
                    t3.setSmooth(Convert.ToDouble(input.Text));
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

        private void FixActiveLineOpacity_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Enter))
            {
                TextBox input = sender as TextBox;
                try
                {
                    t3.setActiveLineOpacity(Convert.ToDouble(input.Text));
                }
                catch
                {

                }
            }
        }

        private void EnvZoom_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Enter))
            {
                TextBox input = sender as TextBox;
                try
                {
                    t1.setEnvZoom(Convert.ToDouble(input.Text));
                }
                catch
                {

                }
            }
        }

        private void Color_Click(object sender, MouseButtonEventArgs e)
        {
            toColor.setColor(id,(sender as Rectangle).Fill);
        }

        private void TrDot_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            toColor = t1;
            id = 0;
        }

        private void TrShad_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            toColor = t1;
            id = 1;
        }

        private void Lin_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            toColor = t2;
            id = 0;
        }

        private void FDot_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            toColor = t3;
            id = 0;
        }

        private void FLin_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            toColor = t3;
            id = 1;
        }

        private void Env_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            t1.setEnv("testbg.jpg");
        }

        private void frz_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            freeze = !freeze;
        }
    }
}
