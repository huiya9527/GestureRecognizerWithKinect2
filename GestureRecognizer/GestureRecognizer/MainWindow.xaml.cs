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
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Media;
using Recognizer.Dollar;
using Microsoft.Kinect;

namespace GestureRecognizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        KinectSensor sensor;
        /// <summary>
        /// Reader for body frames
        /// </summary>
        BodyFrameReader bodyFrameReader;
        /// <summary>
        /// Array for the bodies
        /// </summary>
        private Body[] bodies = null;
        /// <summary>
        /// Screen width and height for determining the exact mouse sensitivity
        /// </summary>
        /// 
        int screenWidth, screenHeight;
        bool primeHand = true;

        private Recognizer.Dollar.Recognizer _rec;
        private bool start = false;
        private ArrayList _points;


        public MainWindow()
        {
            InitializeComponent();
            _rec = new Recognizer.Dollar.Recognizer();

            _points = new ArrayList(256);
            LoadGestureFiles();
            sensor = KinectSensor.GetDefault();
            // open the reader for the body frames
            bodyFrameReader = sensor.BodyFrameSource.OpenReader();
            bodyFrameReader.FrameArrived += bodyFrameReader_FrameArrived;

            // get screen with and height
            screenWidth = (int)SystemParameters.PrimaryScreenWidth;
            screenHeight = (int)SystemParameters.PrimaryScreenHeight;


            // open the sensor
            sensor.Open();
        }

        private void LoadGestureFiles()
        {
            String path = @"GesturesRecord\";

            var files = Directory.GetFiles(path, "*.xml");

            foreach (var file in files)
            {
                Console.WriteLine(file);
                _rec.LoadGesture(file);
            }
        }

        private void mouse_down(float x, float y)
        {
            Console.WriteLine("mouse_left_down");
            _points.Clear();
            m_canvas.Children.Clear();
            draw(x, y);
            _points.Add(new PointR(x, y, Environment.TickCount));
        }

        private void mouse_move(float x, float y)
        {
            Console.WriteLine("move");
            draw(x, y);
            _points.Add(new PointR(x, y, Environment.TickCount));
        }

        private void mouse_up()
        {
            Console.WriteLine("mouse_left_up");
            if (_points.Count >= 5) // require 5 points for a valid gesture
            {
                if (_rec.NumGestures > 0) // not recording, so testing
                {

                    NBestList result = _rec.Recognize(_points); // where all the action is!!
                    select_posture(result.Name);
                    textBlock1.Text = result.Name;
                }
            }
        }

        private void select_posture(String name)
        {
            foreach (var a in GestureCollection.Children)
            {
                if (name.StartsWith(((Grid)a).Name))
                {
                    ((Grid)a).Background = new SolidColorBrush(Colors.LightBlue);
                }
                else
                {
                    ((Grid)a).Background = new SolidColorBrush(Colors.White);
                }
            }
        }

        private void draw(float x, float y)
        {
            Ellipse ellipse = new Ellipse();
            ellipse.Fill = new SolidColorBrush(Colors.Red);
            ellipse.Width = 4;
            ellipse.Height = 4;
            Canvas.SetLeft(ellipse, x * m_canvas.Width);
            Canvas.SetTop(ellipse, y * m_canvas.Height);

            ellipse.Visibility = Visibility.Visible;
            m_canvas.Children.Add(ellipse);
        }
        void bodyFrameReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;
            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (this.bodies == null)
                    {
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }
                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;
                }
            }
            if (!dataReceived)
            {
                return;
            }

            foreach (Body body in this.bodies)
            {

                // get first tracked body only, notice there's a break below.
                if (body.IsTracked)
                {
                    // get various skeletal positions
                    CameraSpacePoint handLeft = body.Joints[JointType.HandLeft].Position;
                    CameraSpacePoint handRight = body.Joints[JointType.HandRight].Position;
                    HandState leftHandState = body.HandLeftState;
                    HandState rightHandState = body.HandRightState;
                    textBlock2.Text = rightHandState.ToString();

                    float x = 0;
                    float y = 0;
                    HandState selectHandState = HandState.Unknown;

                    if (primeHand)
                    {
                        x = handRight.X + 0.1f;
                        y = handRight.Y + 0.4f;
                        selectHandState = rightHandState;
                    }
                    else
                    {
                        x = handLeft.X - 0.1f;
                        y = handLeft.Y + 0.4f;
                        selectHandState = leftHandState;
                    }
                    //reverse y
                    y = 1 - y;

                    if (selectHandState == HandState.Open)
                    {
                        if (start)
                        {
                            mouse_up();
                            start = false;
                        }
                    }
                    else if (selectHandState == HandState.Closed)
                    {
                        if (start)
                        {
                            mouse_move(x, y);
                        }
                        else
                        {
                            mouse_down(x, y);
                            start = true;
                        }
                    }
                    else
                    {
                        if (start)
                        {
                            mouse_move(x, y);
                        }
                    }
                }
            }
        }
    }
}
