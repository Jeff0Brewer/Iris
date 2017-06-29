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

        EyeXHost eyeHost;
        public MainWindow()
        {
            InitializeComponent();

            t1 = new GazeTrack(canv);

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
        }

        private class GazeTrack {
            private Ellipse body;
            private Point prev;
            private double radius;
            private double smooth;
            private double opacity;

            public GazeTrack(Canvas canv) {
                smooth = .5;
                radius = 5;
                opacity = 1;
                prev = new Point(0, 0);
                body = new Ellipse();
                body.Width = radius*2;
                body.Height = radius*2;
                body.Fill = new SolidColorBrush(System.Windows.Media.Colors.Black);
                Canvas.SetLeft(body, 0);
                Canvas.SetTop(body, 0);

                canv.Children.Add(body);
            }

            public void setOpacity(double o) {
                opacity = o;
                body.Opacity = opacity;
            }

            public void setSize(double s) {
                radius = s / 2;
                body.Width = s;
                body.Height = s;
            }

            public void setSmooth(double s) {
                smooth = s;
            }

            public void next(Point p) {
                prev.X = prev.X * smooth + p.X * (1 - smooth);
                prev.Y = prev.Y * smooth + p.Y * (1 - smooth);
                Canvas.SetLeft(body, prev.X - radius);
                Canvas.SetTop(body, prev.Y - radius);
            }
        }

        private void SmoothEnter_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Enter)) {
                TextBox input = sender as TextBox;
                try
                {
                    t1.setSmooth(Convert.ToDouble(input.Text));
                }
                catch
                {

                }
            }
        }

        private void SizeEnter_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Enter))
            {
                TextBox input = sender as TextBox;
                try
                {
                    t1.setSize(Convert.ToDouble(input.Text));
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
    }
}
