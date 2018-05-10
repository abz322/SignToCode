using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using System.Diagnostics;
using CCT.NUI.Core;
using CCT.NUI.Core.OpenNI;
using CCT.NUI.Core.Video;
using CCT.NUI.Visual;
using CCT.NUI.HandTracking;
using CCT.NUI.SignToCode.Properties;
using CCT.NUI.KinectSDK;
using CCT.NUI.Core.Clustering;
using System.Windows.Forms;

namespace CCT.NUI.SignToCode
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IDataSourceFactory factory;
        private IHandDataSource handDataSource;
        private IClusterDataSource clusterDataSource;
        private Boolean listen;
        static private bool gestureBlocker = false;
        static private bool isSet = false;
        float lHndX;
        float rHndX;

        //This is the primary function that is executed when the application has started. Components are initialised and the Start function is executed.
        public MainWindow()
        {
            InitializeComponent();
            this.Start();
            listen = true;
        }

        //This function starts the communcation between the Candescent NUI library with the Kinect device.
        private void Start()
        {
            //Specifies that the Kinect device is in not in Near Mode
            this.factory = new SDKDataSourceFactory(useNearMode: false);

            //This specifies the minimum and maximum depth threshold that the Kinect will use to find hands.
            this.clusterDataSource = this.factory.CreateClusterDataSource(new Core.Clustering.ClusterDataSourceSettings { MaximumDepthThreshold = 1000 });
            this.handDataSource = new HandDataSource(this.factory.CreateShapeDataSource(this.clusterDataSource, new Core.Shape.ShapeDataSourceSettings()));
            
            //This will output the display of the video feed in depth mode
            var depthImageSource = this.factory.CreateDepthImageDataSource();
            depthImageSource.NewDataAvailable += new NewDataHandler<ImageSource>(MainWindow_NewDataAvailable);
            depthImageSource.Start();
            handDataSource.Start();
            handDataSource.NewDataAvailable += new NewDataHandler<HandCollection>(handDataSource_NewDataAvailable);

            //This add a layer and paint any detected hands in the colour green with a white dot for detected palms and purple dots for each finger tip detected.
            var layers = new List<IWpfLayer>();
            layers.Add(new WpfHandLayer(this.handDataSource));
            this.videoControl.Layers = layers;
            
        }

        
        //These are the functions associated with opening the Code Mode UI. The UI is shown, the gesture listener is deactivated and this UI is closed.
        private void buttonCodeMode_Click(object sender, RoutedEventArgs e)
        {
            new CodeMode().Show();
            listen = false;
            this.Close();
        }

        private void CodeModeGesture()
        {
            listen = false;
            if (!gestureBlocker)
            {
                gestureBlocker = true;
            }
            else
            {
                this.Dispatcher.Invoke((Action)(() =>
                {
                    new CodeMode().Show();
                    this.Close();

                }));
            } 
        }

        //This function communicates with the Kinect and the CAndescent NUI library to streama live video feed.
        void MainWindow_NewDataAvailable(ImageSource data)

        {
            this.videoControl.Dispatcher.Invoke(new Action(() =>
            {
                this.videoControl.ShowImageSource(data);
            }));
        }

        //These are the functions associated with closing the application. All feeds a stopped and the application is closed
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            new Action(() =>
            {
                this.handDataSource.Stop();
            }).BeginInvoke(null, null);
        }

        private void buttonExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ExitGesture()
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                this.Close();
            }));
        }

        //This is the active function that consistently looks for hands and dynamic gestures to execute functions.
        void handDataSource_NewDataAvailable(HandCollection data)
        {
            //bool variable to check if the gesture listener is active
            if (listen == true) 
            {
                //when a hand(s) found, enter the loop
                for (int index = 0; index < data.Count; index++)
                {

                    var hand = data.Hands[index];
                    //if there are two hands
                    if (data.Count == 2)
                    {
                        //assign the first hand as left
                        var leftHand = data.Hands.OrderBy(h => h.Location.X).First();
                        //assign the last hand as right
                        var rightHand = data.Hands.OrderBy(h => h.Location.X).Last();

                        //Exit Application Gesture
                        //if there a 5 fingers on both hands and they move away from each other then the exit function is executed.
                        if (leftHand.FingerCount == 5 && rightHand.FingerCount == 5)
                        {
                            if (isSet == false)
                            {
                                lHndX = leftHand.PalmX;
                                rHndX = rightHand.PalmX;
                                isSet = true;
                            }
                            else if (isSet == true)
                            {
                                if (leftHand.PalmX < lHndX - 50 && rightHand.PalmX > rHndX + 50)
                                {
                                    ExitGesture();
                                }
                            }

                        }
                        //Code Mode Gesture
                        //If there are no fingers on the left hand and there is 1 finger on the right hand, and the right hand moves from the right hand side towards the left then execute Code Mode.
                        if (leftHand.FingerCount == 0 && rightHand.FingerCount == 1)
                        {
                            if (isSet == false)
                            {
                                rHndX = rightHand.PalmX;
                                isSet = true;
                            }
                            else if (isSet == true)
                            {
                                if (rightHand.PalmX < rHndX - 50)
                                {
                                    listen = false;
                                    CodeModeGesture();
                                }
                            }
                            
                        }

                    }

                }
            }
            
        }
    }
}
