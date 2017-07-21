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
using System.IO;
using System.Globalization;

using EyeXFramework.Wpf;
using Tobii.EyeX.Framework;
using EyeXFramework;

namespace ModularVis
{
    public partial class MainWindow : Window
    {
        //UI
        Canvas trackUI;
        Canvas lineUI;
        Canvas fixUI;
        Canvas utilUI;
        bool freeze = false;
        SwatchControl swc;
        int bgInd;

        //Vis
        GazeTrack t1;
        GazeLine t2;
        FixPoints t3;

        //Coloring
        CanColor toColor;
        int id;

        //Saving
        String savPath = "C:/Users/ResearchSquad/Documents/VisSaves/Visualizations/";
        double smoothness = .7;
        double blur = 0;

        //Recording
        RecordInterface ri;
        String recordPath = "C:/Users/ResearchSquad/Documents/VisSaves/Recordings/rd.txt";
        TextWriter rw;
        bool recording = false;
        bool playing = false;

        EyeXHost eyeHost;
        Point curr = new Point(0, 0);

        public MainWindow()
        {
            InitializeComponent();

            this.PreviewKeyDown += new KeyEventHandler(onKeyDown);

            eyeHost = new EyeXHost();
            eyeHost.Start();
            var gaze = eyeHost.CreateGazePointDataStream(GazePointDataMode.LightlyFiltered);
            gaze.Next += gazePoint;
        }

        private void onKeyDown(object sender, KeyEventArgs e) {
            if (e.Key.Equals(Key.Space))
            {
                freeze = !freeze;
                bg.Opacity = (freeze) ? .5 : 1;
            }
            else if (e.Key.Equals(Key.Right) || e.Key.Equals(Key.Left))
            {
                bgInd = (bgInd + 1) % 2;
                String img = "";
                switch (bgInd)
                {
                    case 0:
                        img = "bg.jpg";
                        break;
                    case 1:
                        img = "testbg.jpg";
                        break;
                }
                BitmapImage src = new BitmapImage();
                src.BeginInit();
                src.UriSource = new Uri(img, UriKind.Relative);
                src.CacheOption = BitmapCacheOption.OnLoad;
                src.EndInit();
                bg.Fill = new ImageBrush(src);
                t1.setEnvImg(img);
            }
            else if (e.Key.Equals(Key.Escape)) {
                this.Close();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            bg.Width = canv.ActualWidth;
            bg.Height = canv.ActualHeight;
            bgInd = 0;

            t1 = new GazeTrack(canv, "bg.jpg");
            t2 = new GazeLine(canv);
            t3 = new FixPoints(canv);

            #region trackdot UI panel

            trackUI = new Canvas();
            trackUI.Width = 220;
            trackUI.Height = 70;
            trackUI.Background = new SolidColorBrush(Colors.White);
            Panel.SetZIndex(trackUI, 800);
            Canvas.SetLeft(trackUI, 0);
            Canvas.SetTop(trackUI, 30);
            trackUI.Visibility = Visibility.Hidden;
            canv.Children.Add(trackUI);

            Toggle tt1 = new Toggle("visible", 10, 10, t1.togVis, t1.getVis, trackUI);
            Toggle tt2 = new Toggle("env", 10, 35, t1.setEnv, t1.getEnv, trackUI);

            #endregion

            #region trackline UI panel

            lineUI = new Canvas();
            lineUI.Width = 220;
            lineUI.Height = 40;
            lineUI.Background = new SolidColorBrush(Colors.White);
            Panel.SetZIndex(lineUI, 800);
            Canvas.SetLeft(lineUI, 0);
            Canvas.SetTop(lineUI, 30);
            lineUI.Visibility = Visibility.Hidden;
            canv.Children.Add(lineUI);

            Toggle tl1 = new Toggle("visible", 10, 10, t2.togVis, t2.getVis, lineUI);

            #endregion

            #region fix UI panel

            fixUI = new Canvas();
            fixUI.Width = 220;
            fixUI.Height = 90;
            fixUI.Background = new SolidColorBrush(Colors.White);
            Panel.SetZIndex(fixUI, 800);
            Canvas.SetLeft(fixUI, 0);
            Canvas.SetTop(fixUI, 30);
            fixUI.Visibility = Visibility.Hidden;
            canv.Children.Add(fixUI);

            Toggle tf1 = new Toggle("visible", 10, 10, t3.togVis, t3.getVis, fixUI);
            SliderControl sc1 = new SliderControl(fixUI);
            Slider sf1 = new Slider("fixation time", 10, 35, 1, 100, t3.setFixTime, sc1, fixUI);

            #endregion

            #region utility UI panel

            utilUI = new Canvas();
            utilUI.Width = 220;
            utilUI.Height = 240;
            utilUI.Background = new SolidColorBrush(Colors.White);
            Panel.SetZIndex(utilUI, 800);
            Canvas.SetLeft(utilUI, 0);
            Canvas.SetTop(utilUI, 30);
            utilUI.Visibility = Visibility.Hidden;
            canv.Children.Add(utilUI);

            SliderControl sc2 = new SliderControl(utilUI);
            Slider su1 = new Slider("smoothness", 10, 10, 0, .99, setSmoothness, sc2, utilUI);
            Slider su2 = new Slider("background blur", 10, 56, 0, 10, setBgBlur, sc2, utilUI);
            swc = new SwatchControl(new Action<Brush>[4] {t1.setFillColor,t1.blur.setFillColor,t2.setFillColor,t3.setFillColor},canv);
            Swatch s1 = new Swatch(Colors.Black, 12.5, 112, 20, swc, utilUI);
            Swatch s2 = new Swatch(Colors.Red, 37.5, 112, 20, swc, utilUI);
            Swatch s3 = new Swatch(Colors.Green, 62.5, 112, 20, swc, utilUI);
            Swatch s4 = new Swatch(Colors.Blue, 87.5, 112, 20, swc, utilUI);
            Swatch s5 = new Swatch(Colors.Yellow, 112.5, 112, 20, swc, utilUI);
            Swatch s6 = new Swatch(Colors.Orange, 137.5, 112, 20, swc, utilUI);
            Swatch s7 = new Swatch(Colors.Purple, 162.5, 112, 20, swc, utilUI);
            Swatch s8 = new Swatch(Colors.Pink, 187.5, 112, 20, swc, utilUI);
            Swatch s9 = new Swatch(Colors.Gray, 12.5, 137, 20, swc, utilUI);
            Swatch s10 = new Swatch(Colors.DarkRed, 37.5, 137, 20, swc, utilUI);
            Swatch s11 = new Swatch(Colors.LightGreen, 62.5, 137, 20, swc, utilUI);
            Swatch s12 = new Swatch(Colors.DarkBlue, 87.5, 137, 20, swc, utilUI);
            Swatch s13 = new Swatch(Colors.LightGoldenrodYellow, 112.5, 137, 20, swc, utilUI);
            Swatch s14 = new Swatch(Colors.DarkOrange, 137.5, 137, 20, swc, utilUI);
            Swatch s15 = new Swatch(Colors.Violet, 162.5, 137, 20, swc, utilUI);
            Swatch s16 = new Swatch(Colors.HotPink, 187.5, 137, 20, swc, utilUI);

            ri = new RecordInterface(10,170,recordPath,recordGaze,stopRecord,startPlayback,stopPlayback,utilUI);

            loadVisFiles(10,200,utilUI);

            #endregion

            toColor = t1;
            id = 0;

            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.Render);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 10);
            dispatcherTimer.Tick += new EventHandler(update);
            dispatcherTimer.Start();
        }

        public String saveCurr(String name) {
            int savNum = 0;
            String currPath = savPath + name + ".txt";
            while (File.Exists(currPath)) {
                savNum++;
                currPath = savPath + name + savNum.ToString() + ".txt";
            }
            name = (savNum != 0) ? name + savNum.ToString() : name;

            TextWriter tw = new StreamWriter(currPath);
            tw.WriteLine("s:" + smoothness.ToString() + " " + "b:" + blur.ToString() + " ");
            tw.WriteLine(t1.getParams());
            tw.WriteLine(t2.getParams());
            tw.WriteLine(t3.getParams());
            tw.Close();

            return name;
        }

        public void loadVis(String name) {
            String path = savPath + name + ".txt";
            if (File.Exists(path)){
                String[] loaded = File.ReadAllLines(path);
                loadOddParams(loaded[0]);
                t1.loadFromParams(loaded[1]);
                t2.loadFromParams(loaded[2]);
                t3.loadFromParams(loaded[3]);
            }
        }

        public void deleteSave(String name) {
            String p;
            if(File.Exists(p = savPath + name + ".txt"))
                File.Delete(p);
        }

        public void loadVisFiles(double ix, double iy, Canvas c) {
            String[] files = Directory.GetFiles(savPath.Substring(0, savPath.Length - 1));
            TextBar prev = null;
            foreach (String file in files) {
                TextBar tb = new TextBar(ix, iy, saveCurr, loadVis, deleteSave, c, file.Substring(savPath.Length, file.Length - savPath.Length - 4));
                if(prev != null)
                    prev.next = tb;
                prev = tb;
                iy += 30;
            }
            if(prev != null)
                prev.next = new TextBar(ix, iy, saveCurr, loadVis, deleteSave, c);
            else
                prev = new TextBar(ix, iy, saveCurr, loadVis, deleteSave, c);
            c.Height += files.Length * 30;
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
            //UI
            private Rectangle overlay;
            private bool setLen;
            private bool dotClicked;
            private int clickedInd;
            private int savLen;
            private bool visible;
            public new bool colorActive;
            public new Brush cBr;


            public FixPoints(Canvas c)
            {
                canv = c;
                savLen = 2;
                visible = false;
                len = 0;
                startRadius = 35;
                endRadius = 10;
                currCount = 1;
                fixTime = 30;
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
                lineInd = len - 2;
                potentialFix = new Point(0, 0);
                block = new Point(0, 0);
                double opacity = startOpacity;
                double opacityInc = (startOpacity - endOpacity) / len;
                double radius = startRadius;
                double radiusInc = (startRadius - endRadius) / len;
                for (int i = 0; i < len; i++)
                {
                    points[i] = new Point(-100, -100);
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
                    lines[i].Name = "f" + i.ToString();
                    lines[i].MouseWheel += line_scrolled;
                    lines[i].PreviewMouseLeftButtonDown += line_clicked;
                    lines[i].PreviewMouseUp += line_color;
                    canv.Children.Add(lines[i]);
                    dots[i] = new Ellipse();
                    dots[i].Fill = dbr;
                    dots[i].Opacity = opacity;
                    opacity -= opacityInc;
                    dots[i].Width = radius * 2;
                    dots[i].Height = radius * 2;
                    radius -= radiusInc;
                    Canvas.SetLeft(dots[i], -100);
                    Canvas.SetTop(dots[i], -100);
                    dots[i].Name = "d" + i.ToString();
                    dots[i].PreviewMouseRightButtonDown += dot_rightClicked;
                    dots[i].PreviewMouseLeftButtonDown += dot_leftClicked;
                    dots[i].MouseWheel += dot_scrolled;
                    dots[i].PreviewMouseUp += dot_color;
                    canv.Children.Add(dots[i]);
                }
                //UI
                overlay = new Rectangle();
                overlay.Width = 3000;
                overlay.Height = 3000;
                overlay.Fill = dbr;
                overlay.Opacity = 0;
                Canvas.SetTop(overlay, 3000);
                Panel.SetZIndex(overlay, 1000);
                overlay.PreviewMouseMove += overlay_dragged;
                overlay.PreviewMouseUp += overlay_unclicked;
                canv.Children.Add(overlay);
                colorActive = false;
                cBr = null;
            }

            public String getParams()
            {
                String par = "";
                par += "--t3: "
                     + "l:" + len.ToString() + " "
                     + "ft:" + fixTime.ToString() + " "
                     + "sO:" + startOpacity.ToString() + " "
                     + "eO:" + endOpacity.ToString() + " "
                     + "sR:" + startRadius.ToString() + " "
                     + "eR:" + endRadius.ToString() + " "
                     + "bB:" + dbr.ToString() + " "
                     + "lW:" + lineWidth.ToString() + " "
                     + "lO:" + lineOpacity.ToString() + " "
                     + "lB:" + lbr.ToString() + " ";
                return par;
            }

            public void loadFromParams(String par) {
                int currInd = 0;
                int endInd = 0;
                String key = "l:";
                if (par.Contains("--t3"))
                {
                    currInd = par.IndexOf(key, currInd) + key.Length;
                    endInd = par.IndexOf(" ", currInd);
                    setLength(Convert.ToInt32(par.Substring(currInd, endInd - currInd + 1)));
                    key = "ft:";
                    currInd = par.IndexOf(key, currInd) + key.Length;
                    endInd = par.IndexOf(" ", currInd);
                    setFixTime(Convert.ToInt32(par.Substring(currInd, endInd - currInd + 1)));
                    key = "sO:";
                    currInd = par.IndexOf(key, currInd) + key.Length;
                    endInd = par.IndexOf(" ", currInd);
                    setStartOpacity(Convert.ToDouble(par.Substring(currInd, endInd - currInd + 1)));
                    key = "eO:";
                    currInd = par.IndexOf(key, currInd) + key.Length;
                    endInd = par.IndexOf(" ", currInd);
                    setEndOpacity(Convert.ToDouble(par.Substring(currInd, endInd - currInd + 1)));
                    key = "sR:";
                    currInd = par.IndexOf(key, currInd) + key.Length;
                    endInd = par.IndexOf(" ", currInd);
                    setStartRadius(Convert.ToDouble(par.Substring(currInd, endInd - currInd + 1)));
                    key = "eR:";
                    currInd = par.IndexOf(key, currInd) + key.Length;
                    endInd = par.IndexOf(" ", currInd);
                    setEndRadius(Convert.ToDouble(par.Substring(currInd, endInd - currInd + 1)));
                    key = "bB:";
                    currInd = par.IndexOf(key, currInd) + key.Length;
                    endInd = par.IndexOf(" ", currInd);
                    setColor(0, brushFromHex(par.Substring(currInd, endInd - currInd + 1)));
                    key = "lW:";
                    currInd = par.IndexOf(key, currInd) + key.Length;
                    endInd = par.IndexOf(" ", currInd);
                    setLineWidth(Convert.ToDouble(par.Substring(currInd, endInd - currInd + 1)));
                    key = "lO:";
                    currInd = par.IndexOf(key, currInd) + key.Length;
                    endInd = par.IndexOf(" ", currInd);
                    setLineOpacity(Convert.ToDouble(par.Substring(currInd, endInd - currInd + 1)));
                    key = "lB:";
                    currInd = par.IndexOf(key, currInd) + key.Length;
                    endInd = par.IndexOf(" ", currInd);
                    setColor(1, brushFromHex(par.Substring(currInd, endInd - currInd + 1)));

                    visible = (len > 0);
                }
            }

            private void line_color(object sender, MouseButtonEventArgs e)
            {
                lbr = cBr;
                for (int i = 0; i < len; i++)
                {
                    lines[i].Stroke = lbr;
                }
            }

            private void dot_color(object sender, MouseButtonEventArgs e)
            {
                dbr = cBr;
                for (int i = 0; i < len; i++)
                {
                    dots[i].Fill = dbr;
                }
            }

            public void setFillColor(Brush b)
            {
                cBr = b;
            }

            public bool togVis()
            {
                if (visible)
                {
                    savLen = len;
                    setLength(0);
                }
                else
                {
                    setLength(savLen);
                }
                visible = !visible;
                return visible;
            }

            public bool getVis() {
                return visible;
            }

            private void line_clicked(object sender, MouseButtonEventArgs e)
            {
                dotClicked = false;
                clickedInd = Convert.ToInt32((sender as Line).Name.Substring(1));
                Canvas.SetTop(overlay, 0);
            }

            private void line_scrolled(object sender, MouseWheelEventArgs e)
            {
                lineOpacity += (double)e.Delta / 3000;
                lineOpacity = (lineOpacity > 1) ? 1 : lineOpacity;
                lineOpacity = (lineOpacity < 0) ? 0 : lineOpacity;
                setLineOpacity(lineOpacity);
            }

            private void dot_scrolled(object sender, MouseWheelEventArgs e)
            {
                clickedInd = Convert.ToInt32((sender as Ellipse).Name.Substring(1));
                if (clickedInd < len / 2)
                {
                    startOpacity += (double)e.Delta / 3000;
                    startOpacity = (startOpacity > 1) ? 1 : startOpacity;
                    startOpacity = (startOpacity < 0) ? 0 : startOpacity;
                    setStartOpacity(startOpacity);
                }
                else
                {
                    endOpacity += (double)e.Delta / 3000;
                    endOpacity = (endOpacity > 1) ? 1 : endOpacity;
                    endOpacity = (endOpacity < 0) ? 0 : endOpacity;
                    setEndOpacity(endOpacity);
                }
            }

            private void dot_rightClicked(object sender, MouseButtonEventArgs e)
            {
                setLen = true;
                dotClicked = true;
                clickedInd = Convert.ToInt32((sender as Ellipse).Name.Substring(1));
                setLength(clickedInd + 1);
                Canvas.SetTop(overlay, 0);
            }

            private void dot_leftClicked(object sender, MouseButtonEventArgs e)
            {
                setLen = false;
                dotClicked = true;
                clickedInd = Convert.ToInt32((sender as Ellipse).Name.Substring(1));
                Canvas.SetTop(overlay, 0);
            }

            private void overlay_dragged(object sender, MouseEventArgs e)
            {
                Point mouse = e.GetPosition(canv);
                if (dotClicked && setLen && distance(mouse, points[len - 1]) > 30)
                {
                    setLength(e.GetPosition(canv));
                }
                else if (dotClicked && !setLen)
                {
                    if (clickedInd < len / 2 || len == 1)
                    {
                        double newRad = distance(points[clickedInd], mouse);
                        startRadius = ((newRad - endRadius) / (len - clickedInd)) * clickedInd + newRad;
                        startRadius = (startRadius < 0) ? 0 : startRadius;
                        setStartRadius(startRadius);
                    }
                    else
                    {
                        double newRad = distance(points[clickedInd], mouse);
                        endRadius = ((newRad - startRadius) / (clickedInd)) * (len - clickedInd - 1) + newRad;
                        endRadius = (endRadius < 0) ? 0 : endRadius;
                        setEndRadius(endRadius);
                    }
                }
                else if (!dotClicked && clickedInd < len - 1)
                {
                    Point root = new Point(lines[clickedInd].X1, lines[clickedInd].Y1);
                    Point a = mouse;
                    a.X -= root.X;
                    a.Y -= root.Y;
                    Point b = new Point(lines[clickedInd].X2, lines[clickedInd].Y2);
                    b.X -= root.X;
                    b.Y -= root.Y;
                    double w = distance(root, e.GetPosition(canv)) * Math.Sin(Math.Acos((a.X * b.X + a.Y * b.Y) / (distance(a, new Point(0, 0)) * distance(b, new Point(0, 0)))));
                    setLineWidth(2 * w);
                }
            }

            private void overlay_unclicked(object sender, MouseButtonEventArgs e)
            {
                Canvas.SetTop(overlay, 3000);
            }

            public void next(Point p)
            {
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
                        if (len > 1)
                        {
                            lines[lineInd].X1 = points[0].X;
                            lines[lineInd].Y1 = points[0].Y;
                            lines[lineInd].X2 = points[1].X;
                            lines[lineInd].Y2 = points[1].Y;
                            lineInd = (lineInd == 0) ? len - 2 : lineInd - 1;
                        }
                        for (int i = 0; i < len; i++)
                        {
                            Canvas.SetLeft(dots[i], points[i].X - dots[i].Width / 2);
                            Canvas.SetTop(dots[i], points[i].Y - dots[i].Height / 2);
                        }
                        currCount = 1;
                        block.X = potentialFix.X;
                        block.Y = potentialFix.Y;
                    }
                }
            }

            public override void setColor(int id, Brush b)
            {
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

            public void setLineWidth(double w)
            {
                lineWidth = w;
                for (int i = 0; i < len; i++)
                {
                    lines[i].StrokeThickness = lineWidth;
                }
            }

            public void setActiveLineOpacity(double o)
            {
                activeLineOpacity = o;
                lines[len - 1].Opacity = activeLineOpacity;
            }

            public void setLineOpacity(double o)
            {
                lineOpacity = o;
                for (int i = 0; i < len - 1; i++)
                {
                    lines[i].Opacity = lineOpacity;
                }
            }

            public void setLength(int l)
            {
                for (int i = 0; i < len; i++)
                {
                    canv.Children.Remove(dots[i]);
                    canv.Children.Remove(lines[i]);
                }
                int prevlen = len;
                len = l;
                dots = new Ellipse[len];
                lines = new Line[len];
                Point[] tempPoints = new Point[len];
                int setNum = min(points.Length, tempPoints.Length);
                Array.Copy(points, tempPoints, setNum);
                points = tempPoints;
                for (int i = prevlen; i < len; i++)
                {
                    points[i] = new Point();
                }
                double opacity = startOpacity;
                double opacityInc = (startOpacity - endOpacity) / len;
                double radius = startRadius;
                double radiusInc = (startRadius - endRadius) / len;
                for (int i = 0; i < len; i++)
                {
                    lines[i] = new Line();
                    lines[i].Opacity = lineOpacity;
                    lines[i].Stroke = lbr;
                    lines[i].StrokeThickness = lineWidth;
                    if (i < len - 1)
                    {
                        lines[i].X1 = points[i].X;
                        lines[i].Y1 = points[i].Y;
                        lines[i].X2 = points[i + 1].X;
                        lines[i].Y2 = points[i + 1].Y;
                    }
                    lines[i].StrokeEndLineCap = PenLineCap.Round;
                    lines[i].StrokeStartLineCap = PenLineCap.Round;
                    lines[i].Name = "f" + i.ToString();
                    lines[i].MouseWheel += line_scrolled;
                    lines[i].PreviewMouseLeftButtonDown += line_clicked;
                    lines[i].PreviewMouseUp += line_color;
                    canv.Children.Add(lines[i]);
                    dots[i] = new Ellipse();
                    dots[i].Fill = dbr;
                    dots[i].Opacity = opacity;
                    opacity -= opacityInc;
                    dots[i].Width = radius * 2;
                    dots[i].Height = radius * 2;
                    Canvas.SetLeft(dots[i], points[i].X - radius);
                    Canvas.SetTop(dots[i], points[i].Y - radius);
                    radius -= radiusInc;
                    dots[i].Name = "d" + i.ToString();
                    dots[i].PreviewMouseRightButtonDown += dot_rightClicked;
                    dots[i].PreviewMouseLeftButtonDown += dot_leftClicked;
                    dots[i].MouseWheel += dot_scrolled;
                    dots[i].PreviewMouseUp += dot_color;
                    canv.Children.Add(dots[i]);
                }
                if (len > 0)
                    lines[len - 1].Opacity = activeLineOpacity;
                lineInd = len - 2;
            }

            public void setLength(Point p)
            {
                for (int i = 0; i < len; i++)
                {
                    canv.Children.Remove(dots[i]);
                    canv.Children.Remove(lines[i]);
                }
                int prevLen = len;
                int numNew = (int)Math.Ceiling((double)len / 10);
                len += numNew;
                Ellipse[] tempDot = new Ellipse[len];
                Array.Copy(dots, tempDot, prevLen);
                dots = tempDot;
                lines = new Line[len];
                Point[] tempPoints = new Point[len];
                int setNum = min(points.Length, tempPoints.Length);
                Array.Copy(points, tempPoints, setNum);
                points = tempPoints;
                Point end = points[prevLen - 1];
                double stepX = (p.X - end.X) / numNew;
                double stepY = (p.Y - end.Y) / numNew;
                int c = 1;
                for (int i = prevLen; i < len; i++)
                {
                    points[i] = new Point(end.X + stepX * c, end.Y + stepY * c);
                    c++;
                }
                double opacity = startOpacity;
                double opacityInc = (startOpacity - endOpacity) / len;
                double radius = startRadius;
                double radiusInc = (startRadius - endRadius) / len;
                for (int i = 0; i < len; i++)
                {
                    lines[i] = new Line();
                    lines[i].Opacity = lineOpacity;
                    lines[i].Stroke = lbr;
                    lines[i].StrokeThickness = lineWidth;
                    if (i < len - 1)
                    {
                        lines[i].X1 = points[i].X;
                        lines[i].Y1 = points[i].Y;
                        lines[i].X2 = points[i + 1].X;
                        lines[i].Y2 = points[i + 1].Y;
                    }
                    lines[i].StrokeEndLineCap = PenLineCap.Round;
                    lines[i].StrokeStartLineCap = PenLineCap.Round;
                    lines[i].Name = "f" + i.ToString();
                    lines[i].MouseWheel += line_scrolled;
                    lines[i].PreviewMouseLeftButtonDown += line_clicked;
                    lines[i].PreviewMouseUp += line_color;
                    canv.Children.Add(lines[i]);
                    dots[i] = new Ellipse();
                    dots[i].Fill = dbr;
                    dots[i].Opacity = opacity;
                    opacity -= opacityInc;
                    dots[i].Width = radius * 2;
                    dots[i].Height = radius * 2;
                    Canvas.SetLeft(dots[i], points[i].X - radius);
                    Canvas.SetTop(dots[i], points[i].Y - radius);
                    radius -= radiusInc;
                    dots[i].Name = "d" + i.ToString();
                    dots[i].PreviewMouseRightButtonDown += dot_rightClicked;
                    dots[i].PreviewMouseLeftButtonDown += dot_leftClicked;
                    dots[i].MouseWheel += dot_scrolled;
                    dots[i].PreviewMouseUp += dot_color;
                    canv.Children.Add(dots[i]);
                }
                lines[len - 1].Opacity = activeLineOpacity;
                lineInd = len - 2;
            }

            public void setFixTime(double t)
            {
                fixTime = (int)t;
            }

            public void setStartRadius(double sr)
            {
                startRadius = sr;
                double radius = startRadius;
                double radiusInc = (startRadius - endRadius) / (len - 1);
                for (int i = 0; i < len; i++)
                {
                    double currD = Math.Abs(radius * 2);
                    dots[i].Width = currD;
                    dots[i].Height = currD;
                    Canvas.SetLeft(dots[i], points[i].X - dots[i].Width / 2);
                    Canvas.SetTop(dots[i], points[i].Y - dots[i].Height / 2);
                    radius -= radiusInc;
                }
            }

            public void setEndRadius(double er)
            {
                endRadius = er;
                double radius = startRadius;
                double radiusInc = (startRadius - endRadius) / (len - 1);
                for (int i = 0; i < len; i++)
                {
                    double currD = Math.Abs(radius * 2);
                    dots[i].Width = currD;
                    dots[i].Height = currD;
                    Canvas.SetLeft(dots[i], points[i].X - dots[i].Width / 2);
                    Canvas.SetTop(dots[i], points[i].Y - dots[i].Height / 2);
                    radius -= radiusInc;
                }
            }

            public void setStartOpacity(double so)
            {
                startOpacity = so;
                double opacity = startOpacity;
                double opacityInc = (startOpacity - endOpacity) / (len - 1);
                for (int i = 0; i < len; i++)
                {
                    dots[i].Opacity = Math.Abs(opacity);
                    opacity -= opacityInc;
                }
            }

            public void setEndOpacity(double eo)
            {
                endOpacity = eo;
                double opacity = startOpacity;
                double opacityInc = (startOpacity - endOpacity) / (len - 1);
                for (int i = 0; i < len; i++)
                {
                    dots[i].Opacity = Math.Abs(opacity);
                    opacity -= opacityInc;
                }
            }

            public void setSmooth(double s)
            {
                smooth = s;
            }

            private double distance(Point a, Point b)
            {
                return Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
            }

            private int min(int a, int b)
            {
                return (a < b) ? a : b;
            }
        }

        private class GazeLine : CanColor
        {
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
            private int savLen;
            private bool visible;
            public new bool colorActive;
            public new Brush cBr;

            public GazeLine(Canvas c)
            {
                canv = c;
                savLen = 15;
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
                for (int i = 0; i < len; i++)
                {
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
                    trail[i].PreviewMouseUp += trail_color;
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
                colorActive = false;
                cBr = null;
            }

            public String getParams()
            {
                String par = "";
                par += "--t2: "
                     + "l:" + len.ToString() + " "
                     + "sW:" + startWidth.ToString() + " "
                     + "eW:" + endWidth.ToString() + " "
                     + "sO:" + startOpacity.ToString() + " "
                     + "eO:" + endOpacity.ToString() + " "
                     + "b:" + br.ToString() + " ";
                return par;
            }

            public void loadFromParams(String par) {
                int currInd = 0;
                int endInd = 0;
                String key = "l:";
                if (par.Contains("--t2"))
                {
                    currInd = par.IndexOf(key, currInd) + key.Length;
                    endInd = par.IndexOf(" ", currInd);
                    setLength(Convert.ToInt32(par.Substring(currInd, endInd - currInd + 1)));
                    key = "sW:";
                    currInd = par.IndexOf(key, currInd) + key.Length;
                    endInd = par.IndexOf(" ", currInd);
                    setStartWidth(Convert.ToDouble(par.Substring(currInd, endInd - currInd + 1)));
                    key = "eW:";
                    currInd = par.IndexOf(key, currInd) + key.Length;
                    endInd = par.IndexOf(" ", currInd);
                    setEndWidth(Convert.ToDouble(par.Substring(currInd, endInd - currInd + 1)));
                    key = "sO:";
                    currInd = par.IndexOf(key, currInd) + key.Length;
                    endInd = par.IndexOf(" ", currInd);
                    setStartOpacity(Convert.ToDouble(par.Substring(currInd, endInd - currInd + 1)));
                    key = "eO:";
                    currInd = par.IndexOf(key, currInd) + key.Length;
                    endInd = par.IndexOf(" ", currInd);
                    setEndOpacity(Convert.ToDouble(par.Substring(currInd, endInd - currInd + 1)));
                    key = "b:";
                    currInd = par.IndexOf(key, currInd) + key.Length;
                    endInd = par.IndexOf(" ", currInd);
                    setColor(0,brushFromHex(par.Substring(currInd, endInd - currInd + 1)));

                    visible = (len > 0);
                }
            }

            public void setFillColor(Brush b)
            {
                cBr = b;
            }

            private void trail_color(object sender, MouseButtonEventArgs e)
            {
                br = cBr;
                for (int i = 0; i < len; i++)
                {
                    trail[i].Stroke = br;
                }
            }

            public bool togVis()
            {
                if (visible)
                {
                    savLen = len;
                    setLength(0);
                }
                else
                {
                    setLength(savLen);
                }
                visible = !visible;
                return visible;
            }

            public bool getVis() {
                return visible;
            }

            private void opacityAdjust(object sender, MouseWheelEventArgs e)
            {
                clickedInd = Convert.ToInt32((sender as Line).Name.Substring(1));
                if (clickedInd > len / 2)
                {
                    endOpacity += (double)e.Delta / 3000;
                    endOpacity = (endOpacity > 1) ? 1 : endOpacity;
                    endOpacity = (endOpacity < 0) ? 0 : endOpacity;
                    setEndOpacity(endOpacity);
                }
                else
                {
                    startOpacity += (double)e.Delta / 3000;
                    startOpacity = (startOpacity > 1) ? 1 : startOpacity;
                    startOpacity = (startOpacity < 0) ? 0 : startOpacity;
                    setStartOpacity(startOpacity);
                }
            }

            private void startWidthAdjust(object sender, MouseButtonEventArgs e)
            {
                setLen = false;
                clickedInd = Convert.ToInt32((sender as Line).Name.Substring(1));
                Canvas.SetTop(overlay, 0);
            }

            private void startLengthAdjust(object sender, MouseButtonEventArgs e)
            {
                setLen = true;
                clickedInd = Convert.ToInt32((sender as Line).Name.Substring(1));
                setLength(clickedInd);
                Canvas.SetTop(overlay, 0);
            }

            private void trail_dragged(object sender, MouseEventArgs e)
            {
                if (distance(echo[len], e.GetPosition(canv)) > 10 && setLen)
                {
                    setLength(e.GetPosition(canv));
                }
                if (!setLen)
                {
                    Point a = e.GetPosition(canv);
                    a.X -= echo[clickedInd].X;
                    a.Y -= echo[clickedInd].Y;
                    Point b = echo[clickedInd + 1];
                    b.X -= echo[clickedInd].X;
                    b.Y -= echo[clickedInd].Y;
                    double w = distance(echo[clickedInd], e.GetPosition(canv)) * Math.Sin(Math.Acos((a.X * b.X + a.Y * b.Y) / (distance(a, new Point(0, 0)) * distance(b, new Point(0, 0)))));
                    if (clickedInd > len / 2)
                    {
                        setEndWidth(2 * w);
                    }
                    else
                    {
                        setStartWidth(2 * w);
                    }
                }
            }

            private void trail_unclicked(object sender, MouseButtonEventArgs e)
            {
                Canvas.SetTop(overlay, 3000);
                if (len == 0)
                    setLength(1);
            }

            private double distance(Point a, Point b)
            {
                return Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
            }

            public void next(Point p)
            {
                for (int i = len; i > 0; i--)
                {
                    echo[i].X = echo[i - 1].X;
                    echo[i].Y = echo[i - 1].Y;
                }
                echo[0].X = echo[0].X * smooth + p.X * (1 - smooth);
                echo[0].Y = echo[0].Y * smooth + p.Y * (1 - smooth);
                for (int i = 0; i < len; i++)
                {
                    trail[i].X1 = echo[i].X;
                    trail[i].Y1 = echo[i].Y;
                    trail[i].X2 = echo[i + 1].X;
                    trail[i].Y2 = echo[i + 1].Y;
                }
            }

            public override void setColor(int id, Brush b)
            {
                br = b;
                for (int i = 0; i < len; i++)
                {
                    trail[i].Stroke = br;
                }
            }

            public void setLength(int l)
            {
                for (int i = 0; i < len; i++)
                {
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
                    trail[i].PreviewMouseUp += trail_color;
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
                trail[len - 1].PreviewMouseUp += trail_color;
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

            public void setStartOpacity(double so)
            {
                startOpacity = so;
                double opacity = startOpacity;
                double opacityInc = (startOpacity - endOpacity) / len;
                for (int i = 0; i < len; i++)
                {
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

            public void setStartWidth(double sw)
            {
                startWidth = sw;
                double width = startWidth;
                double widthInc = (startWidth - endWidth) / len;
                for (int i = 0; i < len; i++)
                {
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

            public void setSmooth(double s)
            {
                smooth = s;
            }

            private int min(int a, int b)
            {
                return (a < b) ? a : b;
            }
        }

        private class GazeTrack : CanColor
        {
            private Canvas canv;
            private Ellipse body;
            private Point prev;
            private Brush br;
            public TrackBlur blur;
            private double outerRadius;
            private double innerRadius;
            public double smooth;
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
            private double savOuter, savInner;
            private int savLen;
            private bool visible;
            public new bool colorActive;
            public new Brush cBr;


            public GazeTrack(Canvas c, string imgpath)
            {
                canv = c;
                smooth = .7;
                outerRadius = 0;
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
                body.PreviewMouseUp += fillColor;
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
                savOuter = 30;
                savInner = 0;
                savLen = 0;
                visible = false;
                colorActive = false;
                cBr = null;
                //EnvColoring
                env = false;
                img = imgpath;
                zoom = .90;
                src = new BitmapImage();
            }

            public String getParams()
            {
                String par = "";
                par += "--t1: " 
                     + "oR:" + outerRadius.ToString() + " "
                     + "iR:" + innerRadius.ToString() + " "
                     + "O:" + opacity.ToString() + " "
                     + "ENV:" + env.ToString() + " "
                     + "eZ:" + zoom.ToString() + " "
                     + "bB:" + br.ToString() + " ";
                par += blur.getParams();
                return par;
            }

            public void loadFromParams(String par) {
                int currInd = 0;
                int endInd = 0;
                String key = "oR:";
                if (par.Contains("--t1")) {
                    currInd = par.IndexOf(key, currInd) + key.Length;
                    endInd = par.IndexOf(" ", currInd);
                    setOuterRadius(Convert.ToDouble(par.Substring(currInd, endInd - currInd + 1)));
                    key = "iR:";
                    currInd = par.IndexOf(key, currInd) + key.Length;
                    endInd = par.IndexOf(" ", currInd);
                    setInnerRadius(Convert.ToDouble(par.Substring(currInd, endInd - currInd + 1)));
                    key = "O:";
                    currInd = par.IndexOf(key, currInd) + key.Length;
                    endInd = par.IndexOf(" ", currInd);
                    setOpacity(Convert.ToDouble(par.Substring(currInd, endInd - currInd + 1)));
                    key = "ENV:";
                    currInd = par.IndexOf(key, currInd) + key.Length;
                    endInd = par.IndexOf(" ", currInd);
                    setEnv(Convert.ToBoolean(par.Substring(currInd, endInd - currInd + 1)));
                    key = "eZ:";
                    currInd = par.IndexOf(key, currInd) + key.Length;
                    endInd = par.IndexOf(" ", currInd);
                    setEnvZoom(Convert.ToDouble(par.Substring(currInd, endInd - currInd + 1)));
                    key = "bB:";
                    currInd = par.IndexOf(key, currInd) + key.Length;
                    endInd = par.IndexOf(" ", currInd);
                    loadStroke(brushFromHex(par.Substring(currInd, endInd - currInd + 1)));
                    key = "sL:";
                    currInd = par.IndexOf(key, currInd) + key.Length;
                    endInd = par.IndexOf(" ", currInd);
                    blur.setLength(Convert.ToInt32(par.Substring(currInd, endInd - currInd + 1)), prev);
                    key = "ssO:";
                    currInd = par.IndexOf(key, currInd) + key.Length;
                    endInd = par.IndexOf(" ", currInd);
                    blur.setStartOpacity(Convert.ToDouble(par.Substring(currInd, endInd - currInd + 1)));
                    key = "seO:";
                    currInd = par.IndexOf(key, currInd) + key.Length;
                    endInd = par.IndexOf(" ", currInd);
                    blur.setEndOpacity(Convert.ToDouble(par.Substring(currInd, endInd - currInd + 1)));
                    key = "sB:";
                    currInd = par.IndexOf(key, currInd) + key.Length;
                    endInd = par.IndexOf(" ", currInd);
                    blur.setColor(brushFromHex(par.Substring(currInd, endInd - currInd + 1)));

                    visible = (outerRadius > 0);
                }
            }

            public void setFillColor(Brush b)
            {
                cBr = b;
            }

            private void fillColor(object sender, MouseButtonEventArgs e)
            {
                if (cBr != null)
                {
                    br = cBr;
                    body.Stroke = br;
                    env = false;
                    body.Opacity = opacity;
                }
            }

            private void loadStroke(Brush b) {
                if (!env){
                    br = b;
                    body.Stroke = br;
                }
            }

            public bool togVis()
            {
                if (visible)
                {
                    savOuter = outerRadius;
                    savInner = innerRadius;
                    savLen = blur.len;
                    setOuterRadius(0);
                    setInnerRadius(0);
                    setBlurLength(0);
                }
                else
                {
                    setOuterRadius(savOuter);
                    setInnerRadius(savInner);
                    setBlurLength(savLen);
                    if (env)
                        refreshEnv();
                    else
                        body.Stroke = br;
                }
                visible = !visible;
                return visible;
            }

            public bool getVis() {
                return visible;
            }

            private void body_scrolled(object sender, MouseWheelEventArgs e)
            {
                if (!env)
                {
                    opacity += (double)e.Delta / 1500;
                    opacity = (opacity > 1) ? 1 : opacity;
                    opacity = (opacity < 0) ? 0 : opacity;
                    body.Opacity = opacity;
                }
                else
                {
                    zoom += (double)e.Delta / 1500;
                    zoom = (zoom > 1.5) ? 1.5 : zoom;
                    zoom = (zoom < .5) ? .5 : zoom;
                    setEnvZoom(zoom);
                }
            }

            private void body_rightclicked(object sender, MouseButtonEventArgs e)
            {
                setLen = true;
                blur.remove();
                Canvas.SetTop(overlay, 0);
            }

            private void body_leftclicked(object sender, MouseButtonEventArgs e)
            {
                setLen = false;
                double dist = Math.Sqrt(Math.Pow(e.GetPosition(canv).X - (Canvas.GetLeft(body) + outerRadius), 2) + Math.Pow(e.GetPosition(canv).Y - (Canvas.GetTop(body) + outerRadius), 2));
                setInner = dist < innerRadius + (outerRadius - innerRadius) / 2;
                Canvas.SetTop(overlay, 0);
            }

            private void body_dragged(object sender, MouseEventArgs e)
            {
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
                        if (env)
                            refreshEnv();
                    }
                }
                else
                {
                    blur.setLength(e.GetPosition(canv), prev);
                }
            }

            private void body_unclicked(object sender, MouseButtonEventArgs e)
            {
                Canvas.SetTop(overlay, 3000);
                if (blur.len == 1)
                {
                    blur.setLength(0, prev);
                }
            }

            public void next(Point p)
            {
                prev.X = prev.X * smooth + p.X * (1 - smooth);
                prev.Y = prev.Y * smooth + p.Y * (1 - smooth);
                Canvas.SetLeft(body, prev.X - outerRadius);
                Canvas.SetTop(body, prev.Y - outerRadius);

                if (env && visible)
                {
                    refreshEnv();
                }

                blur.next(prev);
            }

            private void refreshEnv()
            {
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

            public override void setColor(int id, Brush b)
            {
                if (id == 0)
                {
                    br = b;
                    body.Stroke = br;
                }
                else
                {
                    blur.setColor(b);
                }
                env = false;
            }

            public void setEnvImg(String i)
            {
                img = i;
                src = new BitmapImage();
                src.BeginInit();
                src.UriSource = new Uri(img, UriKind.Relative);
                src.CacheOption = BitmapCacheOption.OnLoad;
                src.EndInit();
                Rectangle bg = canv.FindName("bg") as Rectangle;
                ratioX = src.PixelWidth / bg.Width;
                ratioY = src.PixelHeight / bg.Height;
                refreshEnv();
            }

            public bool setEnv()
            {
                if (!env)
                {
                    body.Opacity = 1;
                    env = true;
                    src = new BitmapImage();
                    src.BeginInit();
                    src.UriSource = new Uri(img, UriKind.Relative);
                    src.CacheOption = BitmapCacheOption.OnLoad;
                    src.EndInit();
                    Rectangle bg = canv.FindName("bg") as Rectangle;
                    ratioX = src.PixelWidth / bg.Width;
                    ratioY = src.PixelHeight / bg.Height;
                    refreshEnv();
                }
                else
                {
                    env = false;
                    body.Opacity = opacity;
                    body.Stroke = br;
                }
                return env;
            }

            public bool getEnv() {
                return env;
            }

            public void setEnv(bool e) {
                env = e;
                if (env){
                    body.Opacity = 1;
                    src = new BitmapImage();
                    src.BeginInit();
                    src.UriSource = new Uri(img, UriKind.Relative);
                    src.CacheOption = BitmapCacheOption.OnLoad;
                    src.EndInit();
                    Rectangle bg = canv.FindName("bg") as Rectangle;
                    ratioX = src.PixelWidth / bg.Width;
                    ratioY = src.PixelHeight / bg.Height;
                    refreshEnv();
                }
                else {
                    body.Opacity = opacity;
                    body.Stroke = br;
                }
            }

            public void setEnvZoom(double z)
            {
                zoom = z;
                refreshEnv();
            }

            public void setBlurLength(int l)
            {
                blur.setLength(l, prev);
            }

            public void setBlurStartOpacity(double so)
            {
                blur.setStartOpacity(so);
            }

            public void setBlurEndOpacity(double eo)
            {
                blur.setEndOpacity(eo);
            }

            public void setOuterRadius(double or)
            {
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

            public void setOpacity(double o)
            {
                opacity = o;
                body.Opacity = opacity;
            }

            public void setSmooth(double s)
            {
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
            private Brush cBr;

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
                    lens[i].PreviewMouseUp += lens_unclicked;
                    opacity -= opacityInc;
                    canv.Children.Add(lens[i]);
                }
                cBr = null;
            }

            public String getParams()
            {
                String par = "";
                par += "sL:" + len.ToString() + " "
                     + "ssO:" + startOpacity.ToString() + " "
                     + "seO:" + endOpacity.ToString() + " "
                     + "sB:" + br.ToString() + " ";
                return par;
            }

            private void lens_unclicked(object sender, MouseButtonEventArgs e)
            {
                br = cBr;
                for (int i = 0; i < len; i++)
                {
                    lens[i].Stroke = br;
                }
            }

            public void setFillColor(Brush b)
            {
                cBr = b;
            }

            private void lens_scrolled(object sender, MouseWheelEventArgs e)
            {
                Point mouse = e.GetPosition(canv);
                Point stLens = new Point(Canvas.GetLeft(lens[0]) + radius, Canvas.GetTop(lens[0]) + radius);
                Point endLens = new Point(Canvas.GetLeft(lens[len - 1]) + radius, Canvas.GetTop(lens[len - 1]) + radius);
                double fromBody = distance(stLens, mouse) - radius;
                double c2c = distance(stLens, endLens);
                Point a = new Point(mouse.X - endLens.X, mouse.Y - endLens.Y);
                Point b = new Point(radius * (endLens.X - stLens.X) / c2c, radius * (endLens.Y - stLens.Y) / c2c);
                double absA = distance(a, new Point(0, 0));
                double absB = distance(b, new Point(0, 0));
                double m2el = distance(mouse, endLens);
                double toEnd = Math.Sqrt(Math.Pow(m2el, 2) + Math.Pow(radius, 2) - 2 * m2el * radius * (a.X * b.X + a.Y * b.Y) / (absA * absB));
                if (fromBody < toEnd)
                {
                    startOpacity += (double)e.Delta / 3000;
                    startOpacity = (startOpacity > 1) ? 1 : startOpacity;
                    startOpacity = (startOpacity < 0) ? 0 : startOpacity;
                    setStartOpacity(startOpacity);
                }
                else
                {
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

            public void setColor(Brush b)
            {
                br = b;
                for (int i = 0; i < len; i++)
                {
                    lens[i].Stroke = br;
                }
            }

            public void setStartOpacity(double so)
            {
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

            public void setLength(int l, Point p)
            {
                for (int i = 0; i < len; i++)
                {
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
                    lens[i].PreviewMouseUp += lens_unclicked;
                    opacity -= opacityInc;
                    canv.Children.Add(lens[i]);
                }
            }

            public void setLength(Point p, Point source)
            {
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
                        lens[i].PreviewMouseUp += lens_unclicked;
                        currStep += stretch;
                        canv.Children.Add(lens[i]);
                    }
                    setStartOpacity(startOpacity);
                }
            }

            public void remove()
            {
                for (int i = 0; i < len; i++)
                {
                    canv.Children.Remove(lens[i]);
                }
                len = 0;
                echo = new Point[0];
                lens = new Ellipse[0];
            }

            public void setRadius(double r)
            {
                for (int i = 0; i < len; i++)
                {
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

        private class TextBar {
            private Canvas canv;
            private TextBox textBox;
            private Button go;
            private Button delete;
            private Func<String, String> save;
            private Action<String> load;
            private Action<String> del;
            public double x, y;
            private String visName;
            private bool firstClick;
            private bool saved;
            public TextBar next;
            private double nextY;
            private System.Windows.Threading.DispatcherTimer timer;
            private bool sendDown;

            public TextBar(double ix, double iy, Func<String,String> sv, Action<String> ld, Action<String> dl, Canvas c) {
                canv = c;
                x = ix;
                y = iy;
                save = sv;
                load = ld;
                del = dl;
                visName = "";
                firstClick = true;
                saved = false;
                next = null;
                sendDown = false;

                timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = new TimeSpan(0, 0, 0, 0, 10);
                timer.Tick += timer_tick;

                textBox = new TextBox();
                textBox.Width = 135;
                textBox.Height = 20;
                textBox.Background = new SolidColorBrush(Colors.WhiteSmoke);
                textBox.BorderThickness = new Thickness(0);
                textBox.Foreground = new SolidColorBrush(Colors.Gray);
                textBox.Text = "Vis name";
                textBox.FontSize = 13;
                textBox.PreviewMouseDown += textClick;
                Canvas.SetTop(textBox, y);
                Canvas.SetLeft(textBox, x);
                canv.Children.Add(textBox);

                double spacing = 10;
                go = new Button();
                go.Height = 20;
                go.Width = 200 - textBox.Width - spacing;
                go.Content = "save";
                go.FontFamily = new FontFamily("Arial");
                go.Focusable = false;
                go.FontSize = 12;
                go.BorderThickness = new Thickness(0);
                go.Background = new SolidColorBrush(Colors.LightGray);
                RectangleGeometry clip = new RectangleGeometry(new Rect(new Point(0,0),new Point(go.Width,go.Height)));
                clip.RadiusX = go.Height / 8;
                clip.RadiusY = go.Height / 8;
                go.Clip = clip;
                Canvas.SetLeft(go, x + textBox.Width + spacing);
                Canvas.SetTop(go, y);
                go.PreviewMouseDown += goClick;
                canv.Children.Add(go);

                delete = new Button();
            }

            public TextBar(double ix, double iy, Func<String, String> sv, Action<String> ld, Action<String> dl, Canvas c, String name) {
                canv = c;
                x = ix;
                y = iy;
                save = sv;
                load = ld;
                del = dl;
                visName = name;
                firstClick = false;
                saved = true;
                next = null;
                sendDown = false;

                timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = new TimeSpan(0, 0, 0, 0, 10);
                timer.Tick += timer_tick;

                textBox = new TextBox();
                textBox.Width = 100;
                textBox.Height = 20;
                textBox.Background = new SolidColorBrush(Colors.LightGray);
                textBox.BorderThickness = new Thickness(0);
                textBox.Foreground = new SolidColorBrush(Colors.Black);
                textBox.Text = visName;
                textBox.FontSize = 13;
                textBox.PreviewMouseDown += textClick;
                textBox.Focusable = false;
                Canvas.SetTop(textBox, y);
                Canvas.SetLeft(textBox, x);
                canv.Children.Add(textBox);

                go = new Button();
                go.Height = 20;
                go.Width = (200 - textBox.Width - 15) / 2;
                go.Content = "load";
                go.FontFamily = new FontFamily("Arial");
                go.FontSize = 12;
                go.BorderThickness = new Thickness(0);
                go.Background = new SolidColorBrush(Colors.LightGray);
                go.Focusable = false;
                RectangleGeometry clip = new RectangleGeometry(new Rect(new Point(0, 0), new Point(go.Width, go.Height)));
                clip.RadiusX = go.Height / 8;
                clip.RadiusY = go.Height / 8;
                go.Clip = clip;
                Canvas.SetLeft(go, x + textBox.Width + 10);
                Canvas.SetTop(go, y);
                go.PreviewMouseDown += goClick;
                canv.Children.Add(go);

                delete = new Button();
                delete.Height = 20;
                delete.Width = go.Width;
                delete.Content = "delete";
                delete.FontFamily = new FontFamily("Arial");
                delete.FontSize = 12;
                delete.BorderThickness = new Thickness(0);
                delete.Background = new SolidColorBrush(Colors.LightGray);
                clip = new RectangleGeometry(new Rect(new Point(0, 0), new Point(delete.Width, delete.Height)));
                clip.RadiusX = delete.Height / 8;
                clip.RadiusY = delete.Height / 8;
                delete.Clip = clip;
                Canvas.SetLeft(delete, x + textBox.Width + go.Width + 15);
                Canvas.SetTop(delete, y);
                delete.PreviewMouseDoubleClick += remove;
                canv.Children.Add(delete);
            }

            private void timer_tick(object sender, EventArgs e) {
                if (sendDown){
                    if (nextY - next.y > 1){
                        next.setY(next.y * .7 + nextY * .3);
                    }
                    else
                        timer.Stop();
                }
                else {
                    if (y - nextY > 1){
                        setY(y * .7 + nextY * .3);
                    }
                    else
                        timer.Stop();
                }
            }

            private void convertToSaved() {
                textBox.Background = new SolidColorBrush(Colors.LightGray);
                textBox.Text = visName;
                textBox.Focusable = false;
                textBox.Foreground = new SolidColorBrush(Colors.Black);
                textBox.Width = 100;
                firstClick = false;

                Canvas.SetLeft(go, x + textBox.Width + 10);
                go.Content = "load";
                go.Width = (200 - textBox.Width - 15) / 2;
                
                delete.Height = 20;
                delete.Width = go.Width;
                delete.Content = "delete";
                delete.FontFamily = new FontFamily("Arial");
                delete.FontSize = 12;
                delete.BorderThickness = new Thickness(0);
                delete.Background = new SolidColorBrush(Colors.LightGray);
                RectangleGeometry clip = new RectangleGeometry(new Rect(new Point(0, 0), new Point(delete.Width, delete.Height)));
                clip.RadiusX = delete.Height / 8;
                clip.RadiusY = delete.Height / 8;
                delete.Clip = clip;
                Canvas.SetLeft(delete, x + textBox.Width + go.Width + 15);
                Canvas.SetTop(delete, y);
                delete.PreviewMouseDoubleClick += remove;
                canv.Children.Add(delete);
            }

            private void remove(object sender, MouseButtonEventArgs e) {
                del(visName);
                canv.Children.Remove(textBox);
                canv.Children.Remove(go);
                canv.Children.Remove(delete);
                next.moveUp();
            }

            public void moveUp() {
                nextY = y - textBox.Height - 10;
                sendDown = false;
                timer.Start();
                if (next != null)
                    next.moveUp();
                else
                    canv.Height = canv.Height - textBox.Height - 10;
            }

            private void goClick(object sender, MouseButtonEventArgs e) {
                if (!saved){
                    visName = save(textBox.Text);
                    saved = true;
                    convertToSaved();

                    next = new TextBar(x,y,save,load,del,canv);
                    nextY = y + textBox.Height + 10;
                    canv.Height += textBox.Height + 10;
                    sendDown = true;
                    timer.Start();
                }
                else {
                    load(visName);
                }
            }

            private void textClick(object sender, MouseButtonEventArgs e) {
                if (firstClick) {
                    firstClick = false;
                    textBox.Foreground = new SolidColorBrush(Colors.Black);
                    textBox.Text = "";
                }
            }

            public void setY(double iy) {
                y = iy;
                Canvas.SetTop(textBox, y);
                Canvas.SetTop(go, y);
                Canvas.SetTop(delete, y);
            }
        }

        private class RecordInterface {
            private Canvas canv;
            private Button record;
            private Button play;
            private Ellipse recordDot;
            private Rectangle stopSquare;
            private Polygon playTriangle;
            private Rectangle playSquare;
            private Rectangle dispTrack;
            private Rectangle progress;
            private String path;
            private System.Windows.Threading.DispatcherTimer timer;
            private Action recordGaze, stopRecord;
            private Action startPlay, stopPlay;
            private bool recording, playing;
            private String[] recorded;
            private int currInd;

            public RecordInterface(double ix, double iy, String p, Action start, Action stop, Action pstart, Action pstop, Canvas c) {
                canv = c;
                recording = false;
                playing = false;
                path = p;
                recordGaze = start;
                stopRecord = stop;
                startPlay = pstart;
                stopPlay = pstop;
                currInd = 0;

                record = new Button();
                record.Width = 20;
                record.Height = 20;
                record.Background = new SolidColorBrush(Colors.LightGray);
                record.BorderThickness = new Thickness(0);
                RectangleGeometry clip = new RectangleGeometry(new Rect(new Point(0, 0), new Point(record.Width, record.Height)));
                clip.RadiusX = record.Height / 8;
                clip.RadiusY = record.Height / 8;
                record.Clip = clip;
                Canvas.SetLeft(record, ix);
                Canvas.SetTop(record, iy);
                record.PreviewMouseDown += record_click;
                canv.Children.Add(record);

                play = new Button();
                play.Width = 20;
                play.Height = 20;
                play.Background = new SolidColorBrush(Colors.LightGray);
                play.BorderThickness = new Thickness(0);
                play.Clip = clip;
                Canvas.SetLeft(play, ix + record.Width + 5);
                Canvas.SetTop(play, iy);
                play.PreviewMouseDown += play_click;
                canv.Children.Add(play);

                recordDot = new Ellipse();
                recordDot.Width = record.Height * .5;
                recordDot.Height = record.Height * .5;
                recordDot.Fill = new SolidColorBrush(Colors.Red);
                recordDot.IsHitTestVisible = false;
                Canvas.SetLeft(recordDot, ix + (record.Width - recordDot.Width) / 2);
                Canvas.SetTop(recordDot, iy + (record.Height - recordDot.Height) / 2);
                canv.Children.Add(recordDot);

                stopSquare = new Rectangle();
                stopSquare.Width = record.Height * .5;
                stopSquare.Height = record.Height * .5;
                stopSquare.Fill = new SolidColorBrush(Colors.Red);
                stopSquare.IsHitTestVisible = false;
                Canvas.SetLeft(stopSquare, ix + (record.Width - stopSquare.Width) / 2);
                Canvas.SetTop(stopSquare, iy + (record.Height - stopSquare.Height) / 2);
                stopSquare.Visibility = Visibility.Hidden;
                canv.Children.Add(stopSquare);

                playTriangle = new Polygon();
                playTriangle.Points.Add(new Point(0, 0));
                playTriangle.Points.Add(new Point(0, play.Height * .5));
                playTriangle.Points.Add(new Point(play.Height * .5, play.Height * .25));
                playTriangle.Points.Add(new Point(0, 0));
                playTriangle.Width = play.Height * .5;
                playTriangle.Height = play.Height * .5;
                playTriangle.Fill = new SolidColorBrush(Colors.Red);
                playTriangle.IsHitTestVisible = false;
                Canvas.SetLeft(playTriangle, ix + record.Width + 5 + (play.Width - playTriangle.Width) / 2);
                Canvas.SetTop(playTriangle, iy + (record.Height - playTriangle.Height) / 2);
                canv.Children.Add(playTriangle);

                playSquare = new Rectangle();
                playSquare.Width = record.Height * .5;
                playSquare.Height = record.Height * .5;
                playSquare.Fill = new SolidColorBrush(Colors.Red);
                playSquare.IsHitTestVisible = false;
                Canvas.SetLeft(playSquare, ix + record.Width + 5 + (play.Width - playSquare.Width) / 2);
                Canvas.SetTop(playSquare, iy + (record.Height - playSquare.Height) / 2);
                playSquare.Visibility = Visibility.Hidden;
                canv.Children.Add(playSquare);

                dispTrack = new Rectangle();
                dispTrack.Width = 200 - record.Width - play.Width - 10;
                dispTrack.Height = 5;
                dispTrack.Fill = new SolidColorBrush(Colors.LightGray);
                Canvas.SetLeft(dispTrack, ix + record.Width + play.Width + 10);
                Canvas.SetTop(dispTrack, iy + (record.Height - dispTrack.Height) / 2);
                canv.Children.Add(dispTrack);

                progress = new Rectangle();
                progress.Width = 0;
                progress.Height = dispTrack.Height;
                progress.Fill = new SolidColorBrush(Colors.Red);
                Canvas.SetLeft(progress, Canvas.GetLeft(dispTrack));
                Canvas.SetTop(progress, Canvas.GetTop(dispTrack));
                canv.Children.Add(progress);

                timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = new TimeSpan(0, 0, 0, 0, 10);
                timer.Tick += timer_tick;
            }

            private void play_click(object sender, MouseButtonEventArgs e) {
                Canvas.SetLeft(progress, Canvas.GetLeft(dispTrack));
                progress.Width = 0;
                playing = !playing;
                if (playing){
                    if (playing = File.Exists(path)){
                        recorded = File.ReadAllLines(path);
                        timer.Start();
                        startPlay();
                        currInd = 0;
                        playSquare.Visibility = Visibility.Visible;
                        record.IsHitTestVisible = false;
                    }
                }
                else {
                    timer.Stop();
                    stopPlay();
                    playSquare.Visibility = Visibility.Hidden;
                    record.IsHitTestVisible = true;
                }
            }

            private void record_click(object sender, MouseButtonEventArgs e) {
                Canvas.SetLeft(progress, Canvas.GetLeft(dispTrack));
                progress.Width = 0;
                recording = !recording;
                if (recording){
                    stopSquare.Visibility = Visibility.Visible;
                    timer.Start();
                    recordGaze();
                    play.IsHitTestVisible = false;
                }
                else {
                    stopSquare.Visibility = Visibility.Hidden;
                    timer.Stop();
                    stopRecord();
                    play.IsHitTestVisible = true;
                }
            }

            private void timer_tick(object sender, EventArgs e) {
                if (recording) {
                    dispRecording();
                }
                else if(playing){
                    dispPlaying();
                }
            }

            private void dispRecording() {
                double speed = 1;
                double maxWidth = 40;
                double pL = Canvas.GetLeft(progress);
                double tL = Canvas.GetLeft(dispTrack);
                double tR = tL + dispTrack.Width;
                if (pL == tL && progress.Width < 40){
                    progress.Width = min(maxWidth, progress.Width + speed);
                }
                else if (pL >= tL && pL + speed < tR){
                    Canvas.SetLeft(progress, pL = pL + speed);
                    progress.Width = min(maxWidth, tR - pL);
                }
                else{
                    progress.Width = 0;
                    Canvas.SetLeft(progress, tL);
                }
            }

            private void dispPlaying() {
                if(recorded.Length > 0)
                    progress.Width = dispTrack.Width * (currInd + 1) / recorded.Length;
            }

            public Point next() {
                currInd = (currInd + 1) % recorded.Length;
                String curr = recorded[currInd];
                return new Point(Convert.ToDouble(curr.Substring(0, curr.IndexOf(":"))),
                                 Convert.ToDouble(curr.Substring(curr.IndexOf(":") + 1,curr.IndexOf(" ") - curr.IndexOf(":"))));
            }

            private double max(double a, double b) {
                return (a > b) ? a : b;
            }

            private double min(double a, double b) {
                return (a < b) ? a : b;
            }
        }

        public void recordGaze() {
            recording = true;
            rw = new StreamWriter(recordPath);
        }

        public void stopRecord() {
            recording = false;
            rw.Close();
        }

        public void startPlayback() {
            playing = true;
        }

        public void stopPlayback() {
            playing = false;
        }

        public void loadOddParams(String par) {
            int currInd = 0;
            int endInd = 0;
            String key = "s:";
            currInd = par.IndexOf(key, currInd) + key.Length;
            endInd = par.IndexOf(" ", currInd);
            setSmoothness(Convert.ToDouble(par.Substring(currInd, endInd - currInd + 1)));
            key = "b:";
            currInd = par.IndexOf(key, currInd) + key.Length;
            endInd = par.IndexOf(" ", currInd);
            setBgBlur(Convert.ToDouble(par.Substring(currInd, endInd - currInd + 1)));
        }

        public void setBgBlur(double b) {
            blur = b;
            BlurEffect bgb = new BlurEffect();
            bgb.Radius = b;
            bg.Effect = bgb;
        }

        public void setSmoothness(double s) {
            smoothness = s;
            t1.setSmooth(s);
            t2.setSmooth(s);
            t3.setSmooth(s);
        }

        private void gazePoint(object sender, EyeXFramework.GazePointEventArgs e)
        {
            curr.X = e.X;
            curr.Y = e.Y;
        }

        private void update(object sender, EventArgs e)
        {
            if (recording) {
                rw.WriteLine(curr.X.ToString() + ":" + curr.Y.ToString() + " ");
            }

            Point fromScreen = PointFromScreen(curr);

            if (playing && !freeze) {
                fromScreen = PointFromScreen(ri.next());
            }

            if (!freeze){
                t1.next(fromScreen);
                t2.next(fromScreen);
                t3.next(fromScreen);
            }
        }

        public int max(int a, int b) {
            return (a > b) ? a : b;
        }

        public class SwatchControl {
            private Canvas canv;
            private Action<Brush>[] elementFunc;
            private Brush curr;
            private Ellipse cursor;
            private Rectangle overlay;

            public SwatchControl(Action<Brush>[] e, Canvas c) {
                canv = c;
                elementFunc = e;
                curr = new SolidColorBrush(Colors.Black);

                cursor = new Ellipse();
                cursor.Width = 10;
                cursor.Height = 10;
                cursor.Fill = curr;
                Canvas.SetLeft(cursor, -100);
                Canvas.SetTop(cursor, 0);
                Panel.SetZIndex(cursor, 850);
                cursor.IsHitTestVisible = false;
                canv.Children.Add(cursor);

                overlay = new Rectangle();
                overlay.Width = 3000;
                overlay.Height = 3000;
                overlay.Fill = curr;
                overlay.Opacity = 0;
                Canvas.SetLeft(overlay, 0);
                Canvas.SetTop(overlay, 3000);
                Panel.SetZIndex(overlay, 1000);
                overlay.PreviewMouseDown += paint;
                overlay.PreviewMouseMove += hover;
                canv.Children.Add(overlay);
            }

            public void startColoring(Brush br) {
                curr = br;
                cursor.Fill = br;
                Canvas.SetTop(overlay, 0);
                for (int i = 0; i < elementFunc.Length; i++) {
                    elementFunc[i](curr);
                }
                Canvas.SetLeft(cursor, -100);
                cursor.Visibility = Visibility.Visible;
            }

            public void clearColor() {
                for (int i = 0; i < elementFunc.Length; i++){
                    elementFunc[i](null);
                }
            }

            private void hover(object sender, MouseEventArgs e){
                Canvas.SetLeft(cursor, e.GetPosition(canv).X - cursor.Width / 2);
                Canvas.SetTop(cursor, e.GetPosition(canv).Y - cursor.Height / 2);
            }

            private void paint(object sender, MouseButtonEventArgs e) {
                Canvas.SetTop(overlay, 3000);
                cursor.Visibility = Visibility.Hidden;
            }
        }

        public class Swatch {
            private Canvas canv;
            private SwatchControl sc;
            private Brush br;
            private Rectangle body;
            private double size;

            public Swatch(Color color, double x, double y, double s, SwatchControl swcont, Canvas c) {
                canv = c;
                sc = swcont;
                br = new SolidColorBrush(color);
                size = s;

                body = new Rectangle();
                body.Width = size;
                body.Height = size;
                body.RadiusX = size / 8;
                body.RadiusY = size / 8;
                body.Fill = br;
                Canvas.SetLeft(body, x);
                Canvas.SetTop(body, y);
                body.PreviewMouseDown += body_clicked;
                canv.Children.Add(body);
            }

            private void body_clicked(object sender, MouseButtonEventArgs e)
            {
                sc.startColoring(br);
            }
        }

        public abstract class CanColor {
            public abstract void setColor(int id, Brush b);
            public bool colorActive;
            public Brush cBr;

            public Brush brushFromHex(String hex) {
                Color c = Color.FromArgb(byte.Parse(hex.Substring(1, 2), NumberStyles.HexNumber),
                                         byte.Parse(hex.Substring(3, 2), NumberStyles.HexNumber),
                                         byte.Parse(hex.Substring(5, 2), NumberStyles.HexNumber),
                                         byte.Parse(hex.Substring(7, 2), NumberStyles.HexNumber));
                return new SolidColorBrush(c);
            }
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

        private class Toggle {
            private Canvas canv;
            private Rectangle outer;
            private Rectangle inner;
            private TextBlock label;
            private Func<bool> send;
            private Func<bool> check;
            private bool on;

            public Toggle(String l, double x, double y, Func<bool> s, Func<bool> ch, Canvas c) {
                canv = c;
                on = false;
                send = s;
                check = ch;

                outer = new Rectangle();
                outer.Width = 16;
                outer.Height = 16;
                outer.Fill = new SolidColorBrush(Colors.LightGray);
                outer.RadiusX = outer.Width / 8;
                outer.RadiusY = outer.Height / 8;
                Canvas.SetLeft(outer, x);
                Canvas.SetTop(outer, y);
                outer.PreviewMouseDown += clicked;
                outer.IsVisibleChanged += checkState;
                canv.Children.Add(outer);

                inner = new Rectangle();
                inner.Width = 7;
                inner.Height = 7;
                inner.Fill = new SolidColorBrush(Colors.Black);
                inner.RadiusX = inner.Width / 4;
                inner.RadiusY = inner.Height / 4;
                Canvas.SetLeft(inner, x + (outer.Width-inner.Width)/2);
                Canvas.SetTop(inner, y + (outer.Height - inner.Height)/2);
                inner.PreviewMouseDown += clicked;
                inner.Visibility = Visibility.Hidden;
                canv.Children.Add(inner);

                label = new TextBlock();
                label.Text = l;
                label.FontSize = 12;
                label.FontFamily = new FontFamily("Arial");
                Canvas.SetLeft(label, x + outer.Width + 5);
                Canvas.SetTop(label, y + (outer.Height - label.FontSize)/2);
                canv.Children.Add(label);
            }

            private void checkState(object sender, DependencyPropertyChangedEventArgs e) {
                on = check();

                if (on)
                {
                    inner.Visibility = Visibility.Visible;
                }
                else
                {
                    inner.Visibility = Visibility.Hidden;
                }
            }

            private void clicked(object sender, MouseButtonEventArgs e) {
                on = send();

                if (on){
                    inner.Visibility = Visibility.Visible;
                }
                else {
                    inner.Visibility = Visibility.Hidden;
                }
            }
        }

        private class Slider
        {
            private Canvas canv;
            private Rectangle track;
            private Rectangle handle;
            private SliderControl slideControl;
            private Action<double> sendTo;
            private double startVal, endVal;
            private String label;
            public double currVal;

            public Slider(String l, double x, double y, double sV, double eV, Action<double> send, SliderControl sc, Canvas c)
            {
                canv = c;
                slideControl = sc;
                sendTo = send;
                label = l;
                startVal = sV;
                endVal = eV;
                track = new Rectangle();
                handle = new Rectangle();
                track.Fill = new SolidColorBrush(Colors.LightGray);
                handle.Fill = new SolidColorBrush(Colors.Black);
                track.Width = 200;
                track.Height = 20;
                handle.Width = 7;
                handle.Height = 12;
                Canvas.SetTop(track, y);
                Canvas.SetLeft(track, x);
                Canvas.SetTop(handle, y + (track.Height - handle.Height)/2);
                Canvas.SetLeft(handle, x + (track.Width - handle.Width) / 2);
                track.RadiusX = handle.Width / 4;
                track.RadiusY = handle.Width / 4;
                handle.RadiusX = handle.Width / 4;
                handle.RadiusY = handle.Width / 4;
                track.PreviewMouseDown += trackMouseDown;
                handle.PreviewMouseDown += handleMouseDown;
                Panel.SetZIndex(track, 900);
                Panel.SetZIndex(handle, 900);
                canv.Children.Add(track);
                canv.Children.Add(handle);

                TextBlock labelBlock = new TextBlock();
                labelBlock.Text = label;
                labelBlock.FontSize = 12;
                labelBlock.FontFamily = new FontFamily("Arial");
                labelBlock.Width = track.Width;
                labelBlock.TextAlignment = TextAlignment.Center;
                Canvas.SetLeft(labelBlock, x);
                Canvas.SetTop(labelBlock, y + track.Height + 5);
                canv.Children.Add(labelBlock);
            }

            public void setHandleX(int ind, double x)
            {
                double trStart = Canvas.GetLeft(track);
                x = (x < trStart + handle.Width / 2) ? trStart + handle.Width / 2 : x;
                x = (x > trStart + track.Width - handle.Width / 2) ? trStart + track.Width - handle.Width / 2 : x;
                Canvas.SetLeft(handle, x = (x - handle.Width / 2));
                currVal = ((x - trStart) / track.Width) * (endVal - startVal) + startVal;
            }

            public void activateControl(int ind)
            {
                sendTo(currVal);
            }

            private void trackMouseDown(object sender, MouseButtonEventArgs e)
            {
                setHandleX(0, e.GetPosition(canv).X);
                slideControl.engage(this, 0);
            }

            private void handleMouseDown(object sender, MouseButtonEventArgs e)
            {
                slideControl.engage(this, 0);
            }
        }

        private class SliderControl
        {
            private Canvas canv;
            private Rectangle hover;
            private Slider slider;
            private int handleInd;

            public SliderControl(Canvas c)
            {
                canv = c;
                hover = new Rectangle();
                Panel.SetZIndex(hover, 1000);
                hover.Width = 6000;
                hover.Height = 3000;
                hover.Fill = new SolidColorBrush(Colors.Black);
                hover.Opacity = 0;
                Canvas.SetTop(hover, 3000);
                Canvas.SetLeft(hover, -3000);
                hover.PreviewMouseMove += onHover;
                hover.PreviewMouseUp += disengage;
                canv.Children.Add(hover);
                slider = null;
                handleInd = 0;
            }

            public void engage(Slider s, int h)
            {
                slider = s;
                handleInd = h;
                Canvas.SetTop(hover, 0);
            }

            public void disengage(object sender, MouseButtonEventArgs e)
            {
                Canvas.SetTop(hover, 3000);
            }

            public void onHover(object sender, MouseEventArgs e)
            {
                slider.setHandleX(handleInd, e.GetPosition(canv).X);
                slider.activateControl(handleInd);
            }
        }

        private void trUI(object sender, MouseButtonEventArgs e)
        {
            if (trackUI.Visibility == Visibility.Hidden)
            {
                trackUI.Visibility = Visibility.Visible;
                lineUI.Visibility = Visibility.Hidden;
                fixUI.Visibility = Visibility.Hidden;
                utilUI.Visibility = Visibility.Hidden;
                trDot.StrokeThickness = 0;
                trLine.StrokeThickness = 2;
                trFix.StrokeThickness = 2;
                util.StrokeThickness = 2;
                Panel.SetZIndex(trDot, 801);
                Panel.SetZIndex(trLine, 800);
                Panel.SetZIndex(trFix, 800);
                Panel.SetZIndex(util, 800);
            }
            else {
                trackUI.Visibility = Visibility.Hidden;
                (sender as Rectangle).StrokeThickness = 2;
                Panel.SetZIndex((sender as Rectangle), 800);
            }
        }

        private void lnUI(object sender, MouseButtonEventArgs e)
        {
            if (lineUI.Visibility == Visibility.Hidden)
            {
                trackUI.Visibility = Visibility.Hidden;
                lineUI.Visibility = Visibility.Visible;
                fixUI.Visibility = Visibility.Hidden;
                utilUI.Visibility = Visibility.Hidden;
                trDot.StrokeThickness = 2;
                trLine.StrokeThickness = 0;
                trFix.StrokeThickness = 2;
                util.StrokeThickness = 2;
                Panel.SetZIndex(trDot, 800);
                Panel.SetZIndex(trLine, 801);
                Panel.SetZIndex(trFix, 800);
                Panel.SetZIndex(util, 800);
            }
            else
            {
                lineUI.Visibility = Visibility.Hidden;
                (sender as Rectangle).StrokeThickness = 2;
                Panel.SetZIndex((sender as Rectangle), 800);
            }
        }

        private void fxUI(object sender, MouseButtonEventArgs e)
        {
            if (fixUI.Visibility == Visibility.Hidden)
            {
                trackUI.Visibility = Visibility.Hidden;
                lineUI.Visibility = Visibility.Hidden;
                fixUI.Visibility = Visibility.Visible;
                utilUI.Visibility = Visibility.Hidden;
                trDot.StrokeThickness = 2;
                trLine.StrokeThickness = 2;
                trFix.StrokeThickness = 0;
                util.StrokeThickness = 2;
                Panel.SetZIndex(trDot, 800);
                Panel.SetZIndex(trLine, 800);
                Panel.SetZIndex(trFix, 801);
                Panel.SetZIndex(util, 800);
            }
            else
            {
                fixUI.Visibility = Visibility.Hidden;
                (sender as Rectangle).StrokeThickness = 2;
                Panel.SetZIndex((sender as Rectangle), 800);
            }
        }

        private void utUI(object sender, MouseButtonEventArgs e)
        {
            if (utilUI.Visibility == Visibility.Hidden)
            {
                trackUI.Visibility = Visibility.Hidden;
                lineUI.Visibility = Visibility.Hidden;
                fixUI.Visibility = Visibility.Hidden;
                utilUI.Visibility = Visibility.Visible;
                trDot.StrokeThickness = 2;
                trLine.StrokeThickness = 2;
                trFix.StrokeThickness = 2;
                util.StrokeThickness = 0;
                Panel.SetZIndex(trDot, 800);
                Panel.SetZIndex(trLine, 800);
                Panel.SetZIndex(trFix, 800);
                Panel.SetZIndex(util, 801);
            }
            else
            {
                utilUI.Visibility = Visibility.Hidden;
                (sender as Rectangle).StrokeThickness = 2;
                Panel.SetZIndex((sender as Rectangle), 800);
            }
        }
    }
}
