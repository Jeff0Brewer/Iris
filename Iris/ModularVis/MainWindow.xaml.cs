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
        #region global variables
        //UI
        bool freeze = false;
        SwatchControl swc;
        double UIstroke = 1;
        Brush UIfrontBr = new SolidColorBrush(Colors.Black);
        Brush UIbackBr = new SolidColorBrush(Colors.Gray);
        Rectangle[] menuTabs;
        Canvas[] menus;
        Rectangle menuFade;
        Slider su1, su2;

        //Vis
        GazeTrack t1;
        GazeLine t2;
        FixPoints t3;
        
        //Saving
        String savPath = "VisSaves/Visualizations/";
        string illegalChars = new string(System.IO.Path.GetInvalidFileNameChars()) +
                              new string(System.IO.Path.GetInvalidPathChars());
        double smoothness = .7;
        double blur = 0;

        //Recording
        RecordInterface ri;
        String recordPath = "VisSaves/Recordings/rd.txt";
        String tempRecordPath1 = "VisSaves/Recordings/trd1.txt";
        String tempRecordPath2 = "VisSaves/Recordings/trd2.txt";
        TextWriter rw;
        bool recording = false;
        bool playing = false;

        //Background
        String[] backgrounds = {"blankBg.jpg",
                                "txtBg.jpg",
                                "xrayBg.jpg",
                                "mapBg.jpg",
                                "vdashCam.mp4",
                                "vfishTank.mp4",
                                "vgymastics.mp4"};
        int currBgInd = 0;
        MediaElement vid = null;
        bool looped = false;

        //EyeX
        EyeXHost eyeHost;
        Point curr = new Point(0, 0);
        #endregion

        #region init

        public MainWindow()
        {
            InitializeComponent();

            this.PreviewKeyDown += new KeyEventHandler(onKeyDown);

            eyeHost = new EyeXHost();
            eyeHost.Start();
            var gaze = eyeHost.CreateGazePointDataStream(GazePointDataMode.LightlyFiltered);
            gaze.Next += gazePoint;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            bg.Width = canv.ActualWidth;
            bg.Height = canv.ActualHeight;


            //t1 = new GazeTrack(canv, backgrounds[0]);
            t1 = new GazeTrack(canv, "controls.jpg");
            t2 = new GazeLine(canv);
            t3 = new FixPoints(canv);
            TrackControl tC = new TrackControl(new VisElement[3] { t1, t2, t3 });

            menuFade = new Rectangle();
            menuFade.Width = Canvas.GetLeft(util) + util.Width;
            menuFade.Height = util.Height;
            menuFade.Fill = new LinearGradientBrush(Colors.Transparent, Colors.Black, 90);
            menuFade.Opacity = .1;
            menuFade.Visibility = Visibility.Hidden;
            menuFade.IsHitTestVisible = false;
            Panel.SetZIndex(menuFade, 801);
            canv.Children.Add(menuFade);

            menuTabs = new Rectangle[4];
            menuTabs[0] = trDot;
            menuTabs[1] = trLine;
            menuTabs[2] = trFix;
            menuTabs[3] = util;

            menus = new Canvas[4];

            double menuWidth = 220;
            int menuZindex = 800;
            double menuTop = 30;
            Brush menuBg = new SolidColorBrush(Colors.White);

            double last = 0;
            double bottomSpacing = 12.5;
            double toggleSpacing = 7;
            double sliderSpacing = 8;
            double swatchSpacing = 5;
            double recordSpacing = 12;

            #region trackdot UI panel

            menus[0] = new Canvas();
            menus[0].Width = menuWidth;
            menus[0].Height = 70;
            menus[0].Background = menuBg;
            Panel.SetZIndex(menus[0], menuZindex);
            Canvas.SetLeft(menus[0], 0);
            Canvas.SetTop(menus[0], menuTop);
            menus[0].Visibility = Visibility.Hidden;
            canv.Children.Add(menus[0]);

            last = 10;
            Toggle tt1 = new Toggle("visible", 10, last, t1.togVis, t1.getVis, menus[0]);
            Toggle tt2 = new Toggle("env", 10, last += Toggle.Height + toggleSpacing, t1.setEnv, t1.getEnv, menus[0]);
            last += Toggle.Height;

            menus[0].Height = last + bottomSpacing;

            #endregion

            #region trackline UI panel

            menus[1] = new Canvas();
            menus[1].Width = menuWidth;
            menus[1].Height = 40;
            menus[1].Background = menuBg;
            Panel.SetZIndex(menus[1], menuZindex);
            Canvas.SetLeft(menus[1], 0);
            Canvas.SetTop(menus[1], menuTop);
            menus[1].Visibility = Visibility.Hidden;
            canv.Children.Add(menus[1]);

            last = 10;
            Toggle tl1 = new Toggle("visible", 10, last, t2.togVis, t2.getVis, menus[1]);
            last += Toggle.Height;

            menus[1].Height = last + bottomSpacing;

            #endregion

            #region fix UI panel

            menus[2] = new Canvas();
            menus[2].Width = menuWidth;
            menus[2].Height = 115;
            menus[2].Background = menuBg;
            Panel.SetZIndex(menus[2], menuZindex);
            Canvas.SetLeft(menus[2], 0);
            Canvas.SetTop(menus[2], menuTop);
            menus[2].Visibility = Visibility.Hidden;
            canv.Children.Add(menus[2]);

            last = 10;
            Toggle tf1 = new Toggle("visible", 10, last, t3.togVis, t3.getVis, menus[2]);
            Toggle tf2 = new Toggle("line visible", 10, last += Toggle.Height + toggleSpacing, t3.togLine, t3.getLineVis, menus[2]);
            Toggle tf3 = new Toggle("active line", 10, last += Toggle.Height + toggleSpacing, t3.togActiveLine, t3.getActiveLine, menus[2]);
            SliderControl sc1 = new SliderControl(menus[2]);
            Slider sf1 = new Slider("fixation time", 10, last += Toggle.Height + sliderSpacing, 1, 200, t3.setFixTime, t3.getFixTime, sc1, menus[2]);
            last += Slider.Height;

            menus[2].Height = last + bottomSpacing;

            #endregion

            #region utility UI panel

            menus[3] = new Canvas();
            menus[3].Width = menuWidth;
            menus[3].Height = 230;
            menus[3].Background = menuBg;
            Panel.SetZIndex(menus[3], menuZindex);
            Canvas.SetLeft(menus[3], 0);
            Canvas.SetTop(menus[3], menuTop);
            menus[3].Visibility = Visibility.Hidden;
            canv.Children.Add(menus[3]);

            last = 12.5;
            swc = new SwatchControl(new Action<Brush>[3] { t1.setFillColor, t2.setFillColor, t3.setFillColor }, startMouseListen, stopMouseListen, canv);
            Swatch s1 = new Swatch(Colors.Black, 12.5, last, swc, menus[3]);
            Swatch s2 = new Swatch(Colors.LightGray, 37.5, last, swc, menus[3]);
            Swatch s3 = new Swatch(Colors.Red, 62.5, last, swc, menus[3]);
            Swatch s4 = new Swatch(Colors.Green, 87.5, last, swc, menus[3]);
            Swatch s5 = new Swatch(Colors.Blue, 112.5, last, swc, menus[3]);
            Swatch s6 = new Swatch(Colors.Yellow, 137.5, last, swc, menus[3]);
            Swatch s7 = new Swatch(Colors.Orange, 162.5, last, swc, menus[3]);
            Swatch s8 = new Swatch(Colors.Purple, 187.5, last, swc, menus[3]);
            last += Swatch.Height + swatchSpacing;
            Swatch s9 = new Swatch(Colors.Gray, 12.5, last, swc, menus[3]);
            Swatch s10 = new Swatch(Colors.White, 37.5, last, swc, menus[3]);
            Swatch s11 = new Swatch(Colors.DarkRed, 62.5, last, swc, menus[3]);
            Swatch s12 = new Swatch(Colors.LightGreen, 87.5, last, swc, menus[3]);
            Swatch s13 = new Swatch(Colors.DarkBlue, 112.5, last, swc, menus[3]);
            Swatch s14 = new Swatch(Colors.LightGoldenrodYellow, 137.5, last, swc, menus[3]);
            Swatch s15 = new Swatch(Colors.DarkOrange, 162.5, last, swc, menus[3]);
            Swatch s16 = new Swatch(Colors.HotPink, 187.5, last, swc, menus[3]);
            last += Swatch.Height + 15;
            SliderControl sc2 = new SliderControl(menus[3]);
            su1 = new Slider("smoothness", 10, last, 0, .99, setSmoothness, getSmoothness, sc2, menus[3]);
            su2 = new Slider("background blur", 10, last += Slider.Height + sliderSpacing, 0, 10, setBgBlur, getBgBlur, sc2, menus[3]);

            ri = new RecordInterface(10, last += Slider.Height + recordSpacing, recordPath, recordGaze, stopRecord, startPlayback, stopPlayback, menus[3]);

            double loadHeight = loadVisFiles(10, last += RecordInterface.Height + recordSpacing, menus[3]);
            last += loadHeight;

            menus[3].Height = last + bottomSpacing;
            #endregion
            
            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.Render);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 10);
            dispatcherTimer.Tick += new EventHandler(update);
            dispatcherTimer.Start();
        }

        #endregion
        
        #region Vis elements

        public abstract class VisElement
        {
            public abstract void setColor(int id, Brush b);

            public abstract void setFillColor(Brush b);

            public abstract void linkToControl(TrackControl tc);

            protected Brush brushFromHex(String hex)
            {
                Color c = Color.FromArgb(byte.Parse(hex.Substring(1, 2), NumberStyles.HexNumber),
                                         byte.Parse(hex.Substring(3, 2), NumberStyles.HexNumber),
                                         byte.Parse(hex.Substring(5, 2), NumberStyles.HexNumber),
                                         byte.Parse(hex.Substring(7, 2), NumberStyles.HexNumber));
                return new SolidColorBrush(c);
            }

            protected double max(double a, double b) {
                return (a > b) ? a : b;
            }

            protected double min(double a, double b){
                return (a < b) ? a : b;
            }

            protected int max(int a, int b){
                return (a > b) ? a : b;
            }

            protected int min(int a, int b){
                return (a < b) ? a : b;
            }
        }

        private class FixPoints : VisElement
        {
            private Canvas canv;
            private TrackControl tC;
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
            private double smooth;
            private int lineInd;
            //UI
            private Rectangle overlay;
            private bool setLen;
            private bool dotClicked;
            private int clickedInd;
            private int savLen;
            private bool visible;
            private bool lineVisible;
            private bool activeLineVisible;
            public bool colorActive;
            public Brush cBr;


            public FixPoints(Canvas c)
            {
                canv = c;
                tC = null;
                savLen = 3;
                visible = false;
                lineVisible = true;
                activeLineVisible = false;
                len = 0;
                startRadius = 35;
                endRadius = 15;
                currCount = 1;
                fixTime = 20;
                startOpacity = .5;
                endOpacity = .25;
                smooth = .7;
                prev = new Point(0, 0);
                dbr = new SolidColorBrush(System.Windows.Media.Colors.Black);
                lbr = new SolidColorBrush(System.Windows.Media.Colors.Black);
                dots = new Ellipse[len];
                points = new Point[len];
                lines = new Line[len];
                lineWidth = 10;
                lineOpacity = .5;
                lineInd = len - 2;
                potentialFix = new Point(0, 0);
                block = new Point(0, 0);
                double opacity = startOpacity;
                double opacityInc = (startOpacity - endOpacity) / (len - 1);
                double radius = startRadius;
                double radiusInc = (startRadius - endRadius) / (len - 1);
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

                    Panel.SetZIndex(dots[i], 6);
                    Panel.SetZIndex(lines[i], 5);
                    canv.Children.Add(dots[i]);
                    canv.Children.Add(lines[i]);
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
                cBr = new SolidColorBrush(Colors.Black);
                
                if (len > 0 && !activeLineVisible)
                    lines[len - 1].Visibility = Visibility.Hidden;
                else if (len > 0)
                    lines[len - 1].Visibility = Visibility.Visible;
            }

            public override void linkToControl(TrackControl tc) {
                tC = tc;
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
                     + "lV:" + lineVisible.ToString() + " "
                     + "aL:" + activeLineVisible.ToString() + " "
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
                    key = "lV:";
                    currInd = par.IndexOf(key, currInd) + key.Length;
                    endInd = par.IndexOf(" ", currInd);
                    setLineVis(Convert.ToBoolean(par.Substring(currInd, endInd - currInd + 1)));
                    key = "aL:";
                    currInd = par.IndexOf(key, currInd) + key.Length;
                    endInd = par.IndexOf(" ", currInd);
                    setActiveLineVisibility(Convert.ToBoolean(par.Substring(currInd, endInd - currInd + 1)));
                    key = "lB:";
                    currInd = par.IndexOf(key, currInd) + key.Length;
                    endInd = par.IndexOf(" ", currInd);
                    setColor(1, brushFromHex(par.Substring(currInd, endInd - currInd + 1)));

                    visible = (len > 0);
                }
            }

            private void line_color(object sender, MouseButtonEventArgs e)
            {
                if (cBr != null)
                    lbr = cBr;
                for (int i = 0; i < len; i++)
                {
                    lines[i].Stroke = lbr;
                }
            }

            private void dot_color(object sender, MouseButtonEventArgs e)
            {
                if(cBr != null)
                    dbr = cBr;
                for (int i = 0; i < len; i++){
                    dots[i].Fill = dbr;
                }
            }

            public override void setFillColor(Brush b)
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

            public bool togLine() {
                if (lineVisible) {
                    for (int i = 0; i < len - 1; i++) {
                        lines[i].Visibility = Visibility.Hidden;
                    }
                }
                else {
                    for (int i = 0; i < len - 1; i++) {
                        lines[i].Visibility = Visibility.Visible;
                    }
                }
                lineVisible = !lineVisible;
                return lineVisible;
            }

            public bool getLineVis() {
                return lineVisible;
            }

            private void setLineVis(bool vis) {
                lineVisible = vis;
                if (!lineVisible) {
                    for (int i = 0; i < len - 1; i++) {
                        lines[i].Visibility = Visibility.Hidden;
                    }
                }
                else {
                    for (int i = 0; i < len - 1; i++) {
                        lines[i].Visibility = Visibility.Visible;
                    }
                }
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
                lineOpacity = (lineOpacity < .05) ? .05 : lineOpacity;
                setLineOpacity(lineOpacity);
            }

            private void dot_scrolled(object sender, MouseWheelEventArgs e)
            {
                clickedInd = Convert.ToInt32((sender as Ellipse).Name.Substring(1));
                if (clickedInd < (double)len / 2)
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
                if (dotClicked && setLen && ((len == 1 && distance(mouse, points[len - 1]) > startRadius) ||
                                              len > 1 && distance(mouse, points[len - 1]) > 25)){
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
                else if (!dotClicked)
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


                    refreshActiveLine();

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

            public void refreshActiveLine() {
                double dr = tC.getDotRad();
                if (len > 0 && activeLineVisible){
                    lines[len - 1].X2 = points[0].X;
                    lines[len - 1].Y2 = points[0].Y;
                    double dst = distance(prev, points[0]);
                    double rX = (prev.X - points[0].X) / dst;
                    double rY = (prev.Y - points[0].Y) / dst;
                    lines[len - 1].X1 = prev.X - dr * rX;
                    lines[len - 1].Y1 = prev.Y - dr * rY;

                    if (dr > 0)
                        lines[len - 1].StrokeStartLineCap = PenLineCap.Flat;
                    else
                        lines[len - 1].StrokeStartLineCap = PenLineCap.Round;
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

            public void setLineOpacity(double o)
            {
                lineOpacity = o;
                for (int i = 0; i < len; i++)
                {
                    lines[i].Opacity = lineOpacity;
                }
            }

            public void setLength(int l)
            {
                Point savCurr = new Point(0,0);
                if (len > 0)
                    savCurr = new Point(lines[len - 1].X1, lines[len - 1].Y1);
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
                double opacityInc = (startOpacity - endOpacity) / (len - 1);
                double radius = startRadius;
                double radiusInc = (startRadius - endRadius) / (len - 1);
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
                    if (!lineVisible)
                        lines[i].Visibility = Visibility.Hidden;

                    dots[i] = new Ellipse();
                    dots[i].Fill = dbr;
                    dots[i].Opacity = opacity;
                    opacity -= opacityInc;
                    dots[i].Width = radius * 2;
                    dots[i].Height = radius * 2;
                    Canvas.SetLeft(dots[i], points[i].X - radius);
                    Canvas.SetTop(dots[i], points[i].Y - radius);
                    radius -= radiusInc;
                    radius = Math.Abs(radius);
                    dots[i].Name = "d" + i.ToString();
                    dots[i].PreviewMouseRightButtonDown += dot_rightClicked;
                    dots[i].PreviewMouseLeftButtonDown += dot_leftClicked;
                    dots[i].MouseWheel += dot_scrolled;
                    dots[i].PreviewMouseUp += dot_color;

                    Panel.SetZIndex(dots[i], 6);
                    Panel.SetZIndex(lines[i], 5);
                    canv.Children.Add(dots[i]);
                    canv.Children.Add(lines[i]);
                }
                if (len > 0)
                {
                    lines[len - 1].X1 = savCurr.X;
                    lines[len - 1].Y1 = savCurr.Y;
                    lines[len - 1].X2 = points[0].X;
                    lines[len - 1].Y2 = points[0].Y;
                }
                lineInd = len - 2;

                if (len > 0 && !activeLineVisible)
                    lines[len - 1].Visibility = Visibility.Hidden;
                else if (len > 0)
                    lines[len - 1].Visibility = Visibility.Visible;
            }

            public void setLength(Point p)
            {
                Point savCurr = new Point(lines[len - 1].X1, lines[len - 1].Y1);
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
                double opacityInc = (startOpacity - endOpacity) / (len - 1);
                double radius = startRadius;
                double radiusInc = (startRadius - endRadius) / (len - 1);
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
                    if (!lineVisible)
                        lines[i].Visibility = Visibility.Hidden;

                    dots[i] = new Ellipse();
                    dots[i].Fill = dbr;
                    dots[i].Opacity = opacity;
                    opacity -= opacityInc;
                    dots[i].Width = radius * 2;
                    dots[i].Height = radius * 2;
                    Canvas.SetLeft(dots[i], points[i].X - radius);
                    Canvas.SetTop(dots[i], points[i].Y - radius);
                    radius -= radiusInc;
                    radius = Math.Abs(radius);
                    dots[i].Name = "d" + i.ToString();
                    dots[i].PreviewMouseRightButtonDown += dot_rightClicked;
                    dots[i].PreviewMouseLeftButtonDown += dot_leftClicked;
                    dots[i].MouseWheel += dot_scrolled;
                    dots[i].PreviewMouseUp += dot_color;

                    Panel.SetZIndex(dots[i], 6);
                    Panel.SetZIndex(lines[i], 5);
                    canv.Children.Add(dots[i]);
                    canv.Children.Add(lines[i]);
                }
                lines[len - 1].X1 = savCurr.X;
                lines[len - 1].Y1 = savCurr.Y;
                lines[len - 1].X2 = points[0].X;
                lines[len - 1].Y2 = points[0].Y;
                lineInd = len - 2;
                
                if (len > 0 && !activeLineVisible)
                    lines[len - 1].Visibility = Visibility.Hidden;
                else if (len > 0)
                    lines[len - 1].Visibility = Visibility.Visible;
            }

            public void setFixTime(double t)
            {
                fixTime = (int)t;
            }

            public double getFixTime() {
                return fixTime;
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

            public bool togActiveLine() {
                if (len > 0){
                    activeLineVisible = !activeLineVisible;
                    if (activeLineVisible) {
                        lines[len - 1].Visibility = Visibility.Visible;
                        refreshActiveLine();
                    }
                    else
                        lines[len - 1].Visibility = Visibility.Hidden;
                }
                return activeLineVisible;
            }

            public void setActiveLineVisibility(bool v) {
                if (len > 0){
                    activeLineVisible = v;
                    if (activeLineVisible){
                        lines[len - 1].Visibility = Visibility.Visible;
                        refreshActiveLine();
                    }
                    else
                        lines[len - 1].Visibility = Visibility.Hidden;
                }
            }

            public bool getActiveLine() {
                return activeLineVisible;
            }

            private double distance(Point a, Point b)
            {
                return Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
            }
        }

        private class GazeLine : VisElement
        {
            private Canvas canv;
            private TrackControl tC;
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
                tC = null;
                savLen = 25;
                br = new SolidColorBrush(System.Windows.Media.Colors.Black);
                len = 0;
                startWidth = 10;
                endWidth = 1;
                startOpacity = 1;
                endOpacity = 1;
                smooth = .7;
                trail = new Line[len];
                echo = new Point[len + 1];
                echo[len] = new Point(0, 0);
                double width = startWidth;
                double widthInc = (startWidth - endWidth) / len;
                double opacity = startOpacity;
                double opacityInc = (startOpacity - endOpacity) / len;
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
                cBr = new SolidColorBrush(Colors.Black);
            }
            
            public override void linkToControl(TrackControl tc)
            {
                tC = tc;
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
                    setColor(0, brushFromHex(par.Substring(currInd, endInd - currInd + 1)));

                    visible = (len > 0);
                }
            }

            public override void setFillColor(Brush b)
            {
                cBr = b;
            }

            private void trail_color(object sender, MouseButtonEventArgs e)
            {
                if (cBr != null)
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
                if (distance(echo[len], e.GetPosition(canv)) > 5 && setLen)
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

            private double findAngle(Line l1, Line l2) {
                Point a = new Point(l1.X1 - l1.X2, l1.Y1 - l1.Y2),
                      b = new Point(l2.X2 - l2.X1, l2.Y2 - l2.Y1);
                double da = Math.Sqrt(Math.Pow(a.X, 2) + Math.Pow(a.Y, 2)),
                       db = Math.Sqrt(Math.Pow(b.X, 2) + Math.Pow(b.Y, 2));

                return Math.Acos((a.X*b.X + a.Y*b.Y)/(da*db));
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
                    Panel.SetZIndex(trail[i], 7);
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
                Panel.SetZIndex(trail[len - 1], 7);
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
        }

        private class GazeTrack : VisElement
        {
            private Canvas canv;
            private TrackControl tC;
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
            private double offsetX, offsetY;
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
                tC = null;
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
                cBr = new SolidColorBrush(Colors.Black);
                //EnvColoring
                env = false;
                img = imgpath;
                zoom = .9;
                src = new BitmapImage();
                setEnvImg(img);
            }

            public override void linkToControl(TrackControl tc)
            {
                tC = tc;
                blur.linkToControl(tc);
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

            public override void setFillColor(Brush b)
            {
                cBr = b;
                blur.setFillColor(b);
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
                if (!env) {
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
                setInner = dist < innerRadius + (outerRadius - innerRadius) / 4;
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

            private void refreshEnv(){
                if (outerRadius > 0){
                    double inX = prev.X,
                           inY = prev.Y;
                    inX = max(inX, outerRadius * zoom);
                    inX = min(inX, canv.ActualWidth - outerRadius * zoom);
                    inY = max(inY, outerRadius * zoom);
                    inY = min(inY, canv.ActualHeight - outerRadius * zoom);
                    try{
                        ImageBrush temp = new ImageBrush(new CroppedBitmap(src, new Int32Rect((int)((inX - outerRadius * zoom) * ratioX + offsetX),
                                                                                                (int)((inY - outerRadius * zoom) * ratioY + offsetY),
                                                                                                (int)((2 * outerRadius * zoom) * ratioX),
                                                                                                (int)((2 * outerRadius * zoom) * ratioY))));
                        temp.Stretch = Stretch.UniformToFill;
                        body.Stroke = temp;
                    }
                    catch { }
                }
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
                if (ratioX < ratioY) {
                    ratioY = ratioX;
                    offsetX = 0;
                    offsetY = src.PixelWidth * ((src.PixelHeight / (double)src.PixelWidth) - (bg.Height / bg.Width)) / 2;
                }
                else {
                    ratioX = ratioY;
                    offsetY = 0;
                    offsetX = src.PixelHeight * ((src.PixelWidth / (double)src.PixelHeight) - (bg.Width / bg.Height)) / 2;
                }
                if(env)
                    refreshEnv();
            }

            public bool setEnv()
            {
                env = !env;
                if (env)
                {
                    body.Opacity = 1;
                    refreshEnv();
                }
                else
                {
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
                if (env) {
                    body.Opacity = 1;
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
                tC.setDotRad(innerRadius);
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
            private TrackControl tC;
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
                tC = null;
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
                    Panel.SetZIndex(lens[i], 6);
                    canv.Children.Add(lens[i]);
                }
                cBr = null;
            }

            public void linkToControl(TrackControl tc)
            {
                tC = tc;
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
                if (cBr != null)
                    br = cBr;
                for (int i = 0; i < len; i++)
                {
                    lens[i].Stroke = br;
                }
                tC.clearColor();
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
                    Panel.SetZIndex(lens[i], 6);
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
                        Panel.SetZIndex(lens[i], 6);
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

        #endregion

        #region UI elements

        public class SwatchControl
        {
            private Canvas canv;
            private Action<Brush>[] elementFunc;
            private Action startListen, stopListen;
            private Brush curr;
            private Ellipse cursor;
            private Rectangle overlay;

            public SwatchControl(Action<Brush>[] e, Action stL, Action endL, Canvas c)
            {
                canv = c;
                elementFunc = e;
                startListen = stL;
                stopListen = endL;
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

            public void startColoring(Brush br){
                curr = br;
                cursor.Fill = br;
                Canvas.SetTop(overlay, 0);
                for (int i = 0; i < elementFunc.Length; i++)
                {
                    elementFunc[i](curr);
                }
                Canvas.SetLeft(cursor, -100);
                cursor.Visibility = Visibility.Visible;
            }

            public void clearColor()
            {
                for (int i = 0; i < elementFunc.Length; i++)
                {
                    elementFunc[i](null);
                }
            }

            public void addElementFunc(Action<Brush> eF) {
                Action<Brush>[] temp = new Action<Brush>[elementFunc.Length + 1];
                Array.Copy(elementFunc,temp,elementFunc.Length);
                temp[temp.Length - 1] = eF;
                elementFunc = temp;
            }

            public void mouseReleased() {
                clearColor();
                stopListen();
            }

            private void hover(object sender, MouseEventArgs e)
            {
                Canvas.SetLeft(cursor, e.GetPosition(canv).X - cursor.Width / 2);
                Canvas.SetTop(cursor, e.GetPosition(canv).Y - cursor.Height / 2);
            }

            private void paint(object sender, MouseButtonEventArgs e)
            {
                Canvas.SetTop(overlay, 3000);
                cursor.Visibility = Visibility.Hidden;
                startListen();
            }
        }

        public class Swatch
        {
            private Canvas canv;
            private SwatchControl sc;
            private Brush br;
            private Rectangle body;
            private Brush currColor;
            public const double Height = 20;
            
            public Swatch(Color color, double x, double y, SwatchControl swcont, Canvas c)
            {
                canv = c;
                sc = swcont;
                br = new SolidColorBrush(color);
                currColor = null;

                sc.addElementFunc(this.syncColor);

                body = new Rectangle();
                body.Width = Height;
                body.Height = Height;
                body.RadiusX = Height / 8;
                body.RadiusY = Height / 8;
                body.Fill = br;
                Canvas.SetLeft(body, x);
                Canvas.SetTop(body, y);
                body.PreviewMouseDown += body_downclicked;
                body.PreviewMouseUp += body_upclicked;
                canv.Children.Add(body);
            }

            public void syncColor(Brush cC) {
                currColor = cC;
            }

            private void body_downclicked(object sender, MouseButtonEventArgs e) {
                currColor = null;
            }

            private void body_upclicked(object sender, MouseButtonEventArgs e)
            {
                if (br != currColor)
                    sc.startColoring(br);
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

        private class Slider
        {
            private Canvas canv;
            private Rectangle track;
            private Rectangle handle;
            private SliderControl slideControl;
            private Action<double> sendTo;
            private Func<double> checkVal;
            private double startVal, endVal;
            private TextBlock labelBlock;
            public double currVal;
            public const double Height = 28;


            public Slider(String label, double x, double y, double sV, double eV, Action<double> send, Func<double> check, SliderControl sc, Canvas c)
            {
                canv = c;
                slideControl = sc;
                sendTo = send;
                checkVal = check;
                startVal = sV;
                endVal = eV;
                track = new Rectangle();
                handle = new Rectangle();
                track.Fill = new SolidColorBrush(Colors.LightGray);
                handle.Fill = new SolidColorBrush(Colors.Black);
                track.Width = 200;
                track.Height = 16;
                handle.Width = 7;
                handle.Height = 7;
                if (handle.Height < track.Height){
                    Canvas.SetTop(track, y);
                    Canvas.SetLeft(track, x);
                    Canvas.SetTop(handle, y + (track.Height - handle.Height) / 2);
                    Canvas.SetLeft(handle, x + (track.Width - handle.Width) / 2);
                }
                else {
                    Canvas.SetTop(handle, y);
                    Canvas.SetLeft(handle, x);
                    Canvas.SetTop(track, y + (handle.Height - track.Height) / 2);
                    Canvas.SetLeft(track, x);
                }
                track.RadiusX = handle.Width / 4;
                track.RadiusY = handle.Width / 4;
                handle.RadiusX = handle.Width / 4;
                handle.RadiusY = handle.Width / 4;
                track.PreviewMouseDown += trackMouseDown;
                handle.PreviewMouseDown += handleMouseDown;
                handle.IsVisibleChanged += checkState;
                Panel.SetZIndex(track, 900);
                Panel.SetZIndex(handle, 901);
                canv.Children.Add(track);
                canv.Children.Add(handle);

                labelBlock = new TextBlock();
                labelBlock.Text = label;
                labelBlock.FontSize = 12;
                labelBlock.FontFamily = new FontFamily("Arial");
                labelBlock.Width = track.Width;
                labelBlock.TextAlignment = TextAlignment.Left;
                Canvas.SetLeft(labelBlock, x + 5);
                if(handle.Height < track.Height)
                    Canvas.SetTop(labelBlock, y + track.Height);
                else
                    Canvas.SetTop(labelBlock, y + handle.Height);
                Panel.SetZIndex(labelBlock, 900);
                labelBlock.IsHitTestVisible = false;
                canv.Children.Add(labelBlock);
            }

            private void checkState(object sender, DependencyPropertyChangedEventArgs e) {
                double valLen = endVal - startVal;
                double inVal = checkVal() - startVal;
                if (handle.Height < track.Height)
                    setHandleX(0, (inVal / valLen) * (track.Width - (track.Height - handle.Height)) + (track.Height - handle.Height)/2);
                else
                    setHandleX(0, (inVal / valLen) * track.Width);
            }

            public void checkState()
            {
                double valLen = endVal - startVal;
                double inVal = checkVal() - startVal;
                setHandleX(0, (inVal / valLen) * track.Width + Canvas.GetLeft(track));
            }

            public void setHandleX(int ind, double x)
            {
                double trStart, trEnd;
                if (handle.Height < track.Height){
                    trStart = Canvas.GetLeft(track) + (track.Height - handle.Height) / 2;
                    trEnd = Canvas.GetLeft(track) + track.Width - (track.Height - handle.Height);
                }
                else{
                    trStart = Canvas.GetLeft(track);
                    trEnd = trStart + track.Width;
                }
                x = (x < trStart + handle.Width / 2) ? trStart + handle.Width / 2 : x;
                x = (x > trEnd) ? trEnd : x;
                Canvas.SetLeft(handle, x = (x - handle.Width / 2));
                currVal = ((x - trStart) / (trEnd - trStart)) * (endVal - startVal) + startVal;
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

        private class Toggle
        {
            private Canvas canv;
            private Rectangle outer;
            private Rectangle inner;
            private TextBlock label;
            private Func<bool> send;
            private Func<bool> check;
            private bool on;
            public const double Height = 16;

            public Toggle(String l, double x, double y, Func<bool> s, Func<bool> ch, Canvas c)
            {
                canv = c;
                on = false;
                send = s;
                check = ch;

                outer = new Rectangle();
                outer.Width = Height;
                outer.Height = Height;
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
                Canvas.SetLeft(inner, x + (outer.Width - inner.Width) / 2);
                Canvas.SetTop(inner, y + (outer.Height - inner.Height) / 2);
                inner.PreviewMouseDown += clicked;
                inner.Visibility = Visibility.Hidden;
                canv.Children.Add(inner);

                label = new TextBlock();
                label.Text = l;
                label.FontSize = 12;
                label.FontFamily = new FontFamily("Arial");
                Canvas.SetLeft(label, x + outer.Width + 5);
                Canvas.SetTop(label, y + (outer.Height - label.FontSize) / 2);
                canv.Children.Add(label);
            }

            private void checkState(object sender, DependencyPropertyChangedEventArgs e)
            {
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

            private void clicked(object sender, MouseButtonEventArgs e)
            {
                on = send();

                if (on)
                {
                    inner.Visibility = Visibility.Visible;
                }
                else
                {
                    inner.Visibility = Visibility.Hidden;
                }
            }
        }

        private class TextBar
        {
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
            public const double Height = 20;

            public TextBar(double ix, double iy, Func<String, String> sv, Action<String> ld, Action<String> dl, Canvas c)
            {
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
                textBox.PreviewKeyDown += textEnter;
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
                RectangleGeometry clip = new RectangleGeometry(new Rect(new Point(0, 0), new Point(go.Width, go.Height)));
                clip.RadiusX = go.Height / 8;
                clip.RadiusY = go.Height / 8;
                go.Clip = clip;
                Canvas.SetLeft(go, x + textBox.Width + spacing);
                Canvas.SetTop(go, y);
                go.PreviewMouseDown += goClick;
                go.IsHitTestVisible = false;
                canv.Children.Add(go);

                delete = new Button();
            }

            public TextBar(double ix, double iy, Func<String, String> sv, Action<String> ld, Action<String> dl, Canvas c, String name)
            {
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

            public double getTextBarHeight() {
                return textBox.Height;
            }

            private void timer_tick(object sender, EventArgs e)
            {
                double delta = 0;
                if (sendDown)
                {
                    if (nextY - next.y > 1)
                    {
                        delta = next.y - next.setY(next.y * .7 + nextY * .3);
                    }
                    else
                    {
                        delta = next.y - nextY;
                        next.setY(nextY);
                        activate();
                        next.activate();
                        timer.Stop();
                    }
                }
                else
                {
                    if (y - nextY > 1)
                    {
                        delta = y - setY(y * .7 + nextY * .3);
                    }
                    else
                    {
                        delta = y - nextY;
                        setY(nextY);
                        activate();
                        timer.Stop();
                    }
                }
                if ((!sendDown && next == null) || (sendDown && next.next == null))
                    canv.Height -= delta;
            }

            private void convertToSaved()
            {
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

            private void remove(object sender, MouseButtonEventArgs e)
            {
                del(visName);
                canv.Children.Remove(textBox);
                canv.Children.Remove(go);
                canv.Children.Remove(delete);
                deactivate();
                next.moveUp();
            }

            public void moveUp()
            {
                deactivate();
                nextY = y - textBox.Height - 10;
                sendDown = false;
                timer.Start();
                if (next != null)
                    next.moveUp();
            }

            private void goClick(object sender, MouseButtonEventArgs e)
            {
                if (!saved)
                {
                    visName = save(textBox.Text);
                    saved = true;
                    convertToSaved();
                    deactivate();

                    next = new TextBar(x, y, save, load, del, canv);
                    nextY = y + textBox.Height + 10;
                    sendDown = true;
                    timer.Start();
                }
                else
                {
                    load(visName);
                }
            }

            private void textClick(object sender, MouseButtonEventArgs e)
            {
                if (firstClick)
                {
                    firstClick = false;
                    textBox.Foreground = new SolidColorBrush(Colors.Black);
                    textBox.Text = "";
                }
            }

            private void textEnter(object sender, KeyEventArgs e) {
                if(e.Key.Equals(Key.Space))
                    e.Handled = true;
            }

            public double setY(double iy)
            {
                y = iy;
                Canvas.SetTop(textBox, y);
                Canvas.SetTop(go, y);
                Canvas.SetTop(delete, y);
                return y;
            }

            public void deactivate()
            {
                go.IsHitTestVisible = false;
                delete.IsHitTestVisible = false;
            }

            public void activate()
            {
                go.IsHitTestVisible = true;
                delete.IsHitTestVisible = true;
            }
        }

        private class RecordInterface
        {
            private Canvas canv;
            private Button record;
            private Button play;
            private Ellipse recordDot;
            private Rectangle stopSquare;
            private Polygon playTriangle;
            private Rectangle playSquare;
            private Rectangle dispTrack;
            private Rectangle progress;
            private Rectangle mediaProgress;
            private String path;
            private System.Windows.Threading.DispatcherTimer timer;
            private Action<bool> recordGaze;
            private Action<bool, String> stopRecord;
            private Action startPlay, stopPlay;
            private bool recording, playing;
            private String[] recorded;
            private int currInd;
            private MediaElement vid;
            private int vidID;
            private bool firstFrame;
            private double startTime;
            private double totalTime;
            private double maxBar;
            private bool recordedFull;
            private bool freeze;
            public const double Height = 20;

            public RecordInterface(double ix, double iy, String p, Action<bool> start, Action<bool, String> stop, Action pstart, Action pstop, Canvas c)
            {
                canv = c;
                recording = false;
                playing = false;
                path = p;
                recordGaze = start;
                stopRecord = stop;
                startPlay = pstart;
                stopPlay = pstop;
                currInd = 0;
                vid = null;
                vidID = -1;
                firstFrame = true;
                startTime = 0;
                totalTime = 0;
                maxBar = 0;
                recordedFull = false;
                freeze = false;

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

                mediaProgress = new Rectangle();
                mediaProgress.Width = 0;
                mediaProgress.Height = dispTrack.Height;
                mediaProgress.Fill = new SolidColorBrush(Colors.Red);
                Canvas.SetLeft(mediaProgress, Canvas.GetLeft(dispTrack));
                Canvas.SetTop(mediaProgress, Canvas.GetTop(dispTrack));
                canv.Children.Add(mediaProgress);

                timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = new TimeSpan(0, 0, 0, 0, 10);
                timer.Tick += timer_tick;
            }

            public double getRecordInterfaceHeight() {
                return record.Height;
            }

            private void play_click(object sender, MouseButtonEventArgs e)
            {
                progress.Fill = new SolidColorBrush(Colors.Red);
                mediaProgress.Fill = new SolidColorBrush(Colors.Red);
                Canvas.SetLeft(progress, Canvas.GetLeft(dispTrack));
                progress.Width = 0;
                playing = !playing;
                if (playing)
                {
                    if (playing = File.Exists(path))
                    {
                        recorded = File.ReadAllLines(path);
                        if (recorded[recorded.Length - 1].Contains("-full") &&
                           recorded[recorded.Length - 1].Contains("-" + vidID.ToString() + " "))
                            recordedFull = true;
                        timer.Start();
                        startPlay();
                        currInd = 0;
                        playSquare.Visibility = Visibility.Visible;
                        record.IsHitTestVisible = false;
                    }
                }
                else
                {
                    timer.Stop();
                    stopPlay();
                    playSquare.Visibility = Visibility.Hidden;
                    record.IsHitTestVisible = true;
                }
            }

            private void record_click(object sender, MouseButtonEventArgs e)
            {
                progress.Fill = new SolidColorBrush(Colors.PaleVioletRed);
                mediaProgress.Fill = new SolidColorBrush(Colors.PaleVioletRed);
                recordedFull = false;
                Canvas.SetLeft(progress, Canvas.GetLeft(dispTrack));
                progress.Width = 0;
                mediaProgress.Width = 0;
                recording = !recording;
                if (recording)
                {
                    stopSquare.Visibility = Visibility.Visible;
                    timer.Start();
                    recordGaze(vid != null);
                    play.IsHitTestVisible = false;
                    firstFrame = true;
                }
                else
                {
                    stopSquare.Visibility = Visibility.Hidden;
                    timer.Stop();
                    String foot = (recordedFull) ? "-full" : "-clip";
                    foot += "  -" + vidID.ToString() + " ";
                    stopRecord(vid != null, foot);
                    play.IsHitTestVisible = true;
                }
            }

            private void simulate_record_click()
            {
                recordedFull = true;
                Canvas.SetLeft(progress, Canvas.GetLeft(dispTrack));
                progress.Width = 0;
                mediaProgress.Width = 0;
                recording = !recording;
                if (recording)
                {
                    stopSquare.Visibility = Visibility.Visible;
                    timer.Start();
                    recordGaze(vid != null);
                    play.IsHitTestVisible = false;
                    firstFrame = true;
                }
                else
                {
                    stopSquare.Visibility = Visibility.Hidden;
                    timer.Stop();
                    String foot = (recordedFull) ? "-full" : "-clip";
                    foot += "  -" + vidID.ToString() + " ";
                    stopRecord(vid != null, foot);
                    play.IsHitTestVisible = true;
                }
            }

            private void timer_tick(object sender, EventArgs e)
            {
                if (recording && vid == null && !freeze)
                {
                    dispRecording();
                }
                else if (recording && vid != null)
                {
                    dispMediaRecording();
                }
                else if (playing)
                {
                    dispPlaying();
                }
            }

            private void dispMediaRecording()
            {
                if (firstFrame)
                {
                    startTime = vid.Position.TotalSeconds;
                    totalTime = vid.NaturalDuration.TimeSpan.TotalSeconds;
                    double rat = startTime / totalTime;
                    Canvas.SetLeft(progress, Canvas.GetLeft(dispTrack) + dispTrack.Width * rat);
                    maxBar = Canvas.GetLeft(dispTrack) + dispTrack.Width - Canvas.GetLeft(progress);
                    firstFrame = false;
                }
                double st2end = ((vid.Position.TotalSeconds - startTime) / totalTime) * dispTrack.Width;
                progress.Width = (st2end >= 0) ? st2end : maxBar;
                mediaProgress.Width = (st2end >= 0) ? 0 : Math.Abs(dispTrack.Width - maxBar + st2end);
                if (Math.Ceiling(st2end) == 0)
                    simulate_record_click();
            }

            private void dispRecording()
            {
                double speed = 1;
                double maxWidth = 40;
                double pL = Canvas.GetLeft(progress);
                double tL = Canvas.GetLeft(dispTrack);
                double tR = tL + dispTrack.Width;
                if (pL == tL && progress.Width < 40)
                {
                    progress.Width = min(maxWidth, progress.Width + speed);
                }
                else if (pL >= tL && pL + speed < tR)
                {
                    Canvas.SetLeft(progress, pL = pL + speed);
                    progress.Width = min(maxWidth, tR - pL);
                }
                else
                {
                    progress.Width = 0;
                    Canvas.SetLeft(progress, tL);
                }
            }

            private void dispPlaying()
            {
                if (recorded.Length > 0)
                    progress.Width = dispTrack.Width * (currInd + 1) / recorded.Length;
            }

            public Point next()
            {
                if (vid != null && recordedFull)
                    currInd = (int)((vid.Position.TotalSeconds / vid.NaturalDuration.TimeSpan.TotalSeconds) * (recorded.Length - 2));
                else
                    currInd = (currInd + 1) % (recorded.Length - 1);
                currInd = min(currInd, recorded.Length - 2);
                currInd = max(currInd, 0);
                String curr = recorded[currInd];
                return new Point(Convert.ToDouble(curr.Substring(0, curr.IndexOf(":"))),
                                 Convert.ToDouble(curr.Substring(curr.IndexOf(":") + 1, curr.IndexOf(" ") - curr.IndexOf(":"))));
            }

            public void attachMedia(MediaElement me, int id)
            {
                recordedFull = false;
                vid = me;
                vidID = id;
            }

            public void detatchMedia()
            {
                recordedFull = false;
                vid = null;
                vidID = -1;
            }

            public void setFreeze(bool frz) {
                freeze = frz;
            }

            private double max(double a, double b)
            {
                return (a > b) ? a : b;
            }

            private double min(double a, double b)
            {
                return (a < b) ? a : b;
            }

            private int min(int a, int b)
            {
                return (a < b) ? a : b;
            }

            private int max(int a, int b) {
                return (a > b) ? a : b;
            }
        }

        #endregion

        #region utility
        
        public class TrackControl
        {
            private VisElement[] elements;
            private double dotRad;

            public TrackControl(VisElement[] e)
            {
                elements = e;
                dotRad = 0;
                foreach (VisElement element in elements)
                {
                    element.linkToControl(this);
                }
            }

            public void clearColor()
            {
                for (int i = 0; i < elements.Length; i++)
                    elements[i].setFillColor(null);
            }

            public double getDotRad() {
                return dotRad;
            }

            public void setDotRad(double dr) {
                dotRad = dr;
                (elements[2] as FixPoints).refreshActiveLine();
            }
        }

        #region recording
        public void recordGaze(bool media)
        {
            recording = true;
            if (media)
            {
                rw = new StreamWriter(tempRecordPath1);
            }
            else
            {
                rw = new StreamWriter(recordPath);
            }
        }

        public void stopRecord(bool media, String footer)
        {
            recording = false;
            if (media)
            {
                rw.Close();
                rw = new StreamWriter(recordPath);
                String[] tFile;
                if (File.Exists(tempRecordPath2))
                {
                    tFile = File.ReadAllLines(tempRecordPath2);
                    for (int i = 0; i < tFile.Length; i++)
                    {
                        rw.WriteLine(tFile[i]);
                    }
                }
                tFile = File.ReadAllLines(tempRecordPath1);
                for (int i = 0; i < tFile.Length; i++)
                {
                    rw.WriteLine(tFile[i]);
                }
                rw.WriteLine(footer);
                File.Delete(tempRecordPath1);
                File.Delete(tempRecordPath2);
                rw.Close();
            }
            else
            {
                rw.WriteLine(footer);
                rw.Close();
            }
        }

        public void startPlayback()
        {
            playing = true;
        }

        public void stopPlayback()
        {
            playing = false;
        }
        #endregion

        #region save/load
        public String saveCurr(String name){
            if (name.Length > 50) {
                name = name.Substring(0, 50);
            }
            foreach (char c in illegalChars){
                name = name.Replace(c.ToString(), "$");
            }
            int savNum = 0;
            String currPath = savPath + name + ".txt";
            while (File.Exists(currPath))
            {
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

        public void loadVis(String name)
        {
            String path = savPath + name + ".txt";
            if (File.Exists(path))
            {
                String[] loaded = File.ReadAllLines(path);
                loadOddParams(loaded[0]);
                t1.loadFromParams(loaded[1]);
                t2.loadFromParams(loaded[2]);
                t3.loadFromParams(loaded[3]);
                su1.checkState();
                su2.checkState();
            }
        }

        public void deleteSave(String name)
        {
            String p;
            if (File.Exists(p = savPath + name + ".txt"))
                File.Delete(p);
        }

        public double loadVisFiles(double ix, double iy, Canvas c)
        {
            String[] files = Directory.GetFiles(savPath.Substring(0, savPath.Length - 1));
            TextBar prev = null;
            foreach (String file in files)
            {
                if (!(file.Contains("\\ignore.txt") || file.Contains("/ignore.txt")))
                {
                    TextBar tb = new TextBar(ix, iy, saveCurr, loadVis, deleteSave, c, file.Substring(savPath.Length, file.Length - savPath.Length - 4));
                    if (prev != null)
                        prev.next = tb;
                    prev = tb;
                    iy += TextBar.Height + 10;
                }
            }
            if (prev != null)
            {
                prev.next = new TextBar(ix, iy, saveCurr, loadVis, deleteSave, c);
                prev.next.activate();
            }
            else
            {
                prev = new TextBar(ix, iy, saveCurr, loadVis, deleteSave, c);
                prev.activate();
            }
            return (files.Length) * (TextBar.Height + 10) - 10;
        }

        public void loadOddParams(String par)
        {
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
        
        #endregion

        #region freezing
        private void picFreezeSync(bool frz)
        {
            bg.Opacity = (frz) ? .5 : 1;
        }

        private void vidFreezeSync(bool frz)
        {
            if (frz)
            {
                vid.Opacity = .5;
                vid.Pause();
            }
            else
            {
                vid.Opacity = 1;
                vid.Play();
            }
            bg.Opacity = 1;
        }
        #endregion

        #region general setting
        public void setBgBlur(double b)
        {
            blur = b;
            BlurEffect bgb = new BlurEffect();
            bgb.Radius = b;
            bg.Effect = bgb;
        }

        public void setSmoothness(double s)
        {
            smoothness = s;
            t1.setSmooth(s);
            t2.setSmooth(s);
            t3.setSmooth(s);
        }

        public double getBgBlur() {
            return blur;
        }

        public double getSmoothness() {
            return smoothness;
        }
        #endregion

        public void startMouseListen() {
            canv.MouseMove += listenForMouseUp;
        }

        public void stopMouseListen() {
            canv.MouseMove -= listenForMouseUp;
        }

        #endregion

        #region global event handlers

        private void update(object sender, EventArgs e)
        {
            if (recording && !freeze)
            {
                if (looped)
                {
                    rw.Close();
                    rw = new StreamWriter(tempRecordPath2);
                    looped = false;
                }
                rw.WriteLine(curr.X.ToString() + ":" + curr.Y.ToString() + " ");
            }

            Point fromScreen = PointFromScreen(curr);

            if (playing && !freeze)
            {
                fromScreen = PointFromScreen(ri.next());
            }

            if (!freeze)
            {
                t1.next(fromScreen);
                t2.next(fromScreen);
                t3.next(fromScreen);
            }
        }
        
        private void onKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Space))
            {
                freeze = !freeze;
                picFreezeSync(freeze);
                if (vid != null && vid.IsLoaded){
                    vidFreezeSync(freeze);
                }
                ri.setFreeze(freeze);
            }
            else if (e.Key.Equals(Key.Left) || e.Key.Equals(Key.Right))
            {
                currBgInd = (e.Key.Equals(Key.Left)) ? (currBgInd + backgrounds.Length - 1) % backgrounds.Length :
                                                       (currBgInd + 1) % backgrounds.Length;
                String file = backgrounds[currBgInd];
                picFreezeSync(freeze);
                canv.Children.Remove(vid);
                vid = null;
                ri.detatchMedia();
                if (file.Substring(0, 1).Equals("v"))
                {
                    vid = new MediaElement();
                    vid.LoadedBehavior = System.Windows.Controls.MediaState.Manual;
                    vid.UnloadedBehavior = System.Windows.Controls.MediaState.Manual;
                    vid.Source = new Uri(file.Substring(1), UriKind.Relative);
                    vid.Width = canv.ActualWidth;
                    Canvas.SetTop(vid, (canv.ActualHeight - (9.0/16.0)*canv.ActualWidth)/2);
                    Panel.SetZIndex(vid, 0);
                    canv.Children.Add(vid);
                    vid.Play();
                    Thread.Sleep(50);
                    vidFreezeSync(freeze);
                    vid.MediaEnded += vid_ended;
                    ri.attachMedia(vid, currBgInd);
                    file = "blackBg.jpg";
                }
                BitmapImage src = new BitmapImage();
                src.BeginInit();
                src.UriSource = new Uri(file, UriKind.Relative);
                src.CacheOption = BitmapCacheOption.OnLoad;
                src.EndInit();
                ImageBrush imBr = new ImageBrush(src);
                imBr.Stretch = Stretch.UniformToFill;
                bg.Fill = imBr;

                t1.setEnvImg(file);
            }
            else if (e.Key.Equals(Key.Escape))
            {
                this.Close();
            }
        }

        private void vid_ended(object sender, EventArgs e)
        {
            if(vid != null) { 
                looped = true;
                vid.Position = TimeSpan.Zero;
                vid.Play();
            }
        }

        private void gazePoint(object sender, EyeXFramework.GazePointEventArgs e)
        {
            curr.X = e.X;
            curr.Y = e.Y;
        }

        private void selectMenu(object sender, MouseButtonEventArgs e) {
            Rectangle selected = sender as Rectangle;
            int menuInd = -1;
            for (int i = 0; i < menuTabs.Length; i++) {
                if (selected.Name.Equals(menuTabs[i].Name))
                    menuInd = i;
            }
            if (menus[menuInd].Visibility == Visibility.Hidden) {
                for (int i = 0; i < menuTabs.Length; i++){
                    menus[i].Visibility = Visibility.Hidden;
                    menuTabs[i].StrokeThickness = UIstroke;
                    Panel.SetZIndex(menuTabs[i], 800);
                }
                menus[menuInd].Visibility = Visibility.Visible;
                menuTabs[menuInd].StrokeThickness = 0;
                Panel.SetZIndex(menuTabs[menuInd], 802);
                menuFade.Visibility = Visibility.Visible;
            }
            else {
                menus[menuInd].Visibility = Visibility.Hidden;
                menuTabs[menuInd].StrokeThickness = UIstroke;
                Panel.SetZIndex(menuTabs[menuInd], 800);
                menuFade.Visibility = Visibility.Hidden;
            }
        }

        private void listenForMouseUp(object sender, MouseEventArgs e) {
            if(e.LeftButton.Equals(MouseButtonState.Released)) {
                swc.mouseReleased();
            }
        }

        #endregion
        
        #region convenience functions

        private int min(int a, int b) {
            return (a < b) ? a : b;
        }

        private int max(int a, int b){
            return (a > b) ? a : b;
        }

        private double min(double a, double b)
        {
            return (a < b) ? a : b;
        }

        private double max(double a, double b)
        {
            return (a > b) ? a : b;
        }

        #endregion
    }
}