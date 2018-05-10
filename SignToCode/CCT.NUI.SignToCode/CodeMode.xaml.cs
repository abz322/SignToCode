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
using System.IO;
using System.Windows.Forms;
using System.Windows.Threading;
using System.Timers;
using System.Threading;

using System.Text.RegularExpressions;

namespace CCT.NUI.SignToCode
{
    /// <summary>
    /// Interaction logic for CodeMode.xaml
    /// </summary>
    public partial class CodeMode : Window
    {
        private IDataSourceFactory factory;
        private IHandDataSource handDataSource;
        private IClusterDataSource clusterDataSource;
        static private bool dynamicGestureBlocker = false;
        CssLibrary xml = new CssLibrary();
        System.Timers.Timer gestureTimer = new System.Timers.Timer();
        string gesToTxt;
        static private bool token = false;
        static private bool isSet = false;
        static private bool selectorToken = false;
        static private bool propertyToken = false;
        static private bool valueToken = false;
        static private bool beautifierComplete = false;
        float lHndX;
        float rHndX;
        float lHndY;
        float rHndY;

        //The primary function exeuted when the user loads Code Mode.
        //components are initialised when the functions below are executed.
        public CodeMode()
        {
            InitializeComponent();
            this.Start();

            // Allow the RETURN key to be entered in the TextBox control.
            cssTxtBlock.AcceptsReturn = true;
            // Allow the TAB key to be entered in the TextBox control.
            cssTxtBlock.AcceptsTab = true;
            //Hides the Content Displayer
            ImageSectionHidden();
        }

        //This function starts the communcation between the Candescent NUI library with the Kinect device.
        private void Start()
        {
            //Specifies that the Kinect device is in not in Near Mode
            this.factory = new SDKDataSourceFactory(useNearMode: false);

            //This specifies the minimum and maximum depth threshold that the Kinect will use to find hands.
            this.clusterDataSource = this.factory.CreateClusterDataSource(new Core.Clustering.ClusterDataSourceSettings {MaximumDepthThreshold = 1000 });
            this.handDataSource = new HandDataSource(this.factory.CreateShapeDataSource(this.clusterDataSource, new Core.Shape.ShapeDataSourceSettings()));

            //This will output the display of the video feed in depth mode
            var depthImageSource = this.factory.CreateDepthImageDataSource();
            depthImageSource.NewDataAvailable += new NewDataHandler<ImageSource>(CodeMode_NewDataAvailable);
            depthImageSource.Start();
            handDataSource.Start();
            handDataSource.NewDataAvailable += new NewDataHandler<HandCollection>(handDataSource_NewDataAvailable);

            //This add a layer and paint any detected hands in the colour green with a white dot for detected palms and purple dots for each finger tip detected.
            var layers = new List<IWpfLayer>();
            layers.Add(new WpfHandLayer(this.handDataSource));
            this.videoControl.Layers = layers;
            
        }

        //This function communicates with the Kinect and the CAndescent NUI library to streama live video feed.
        void CodeMode_NewDataAvailable(ImageSource data)
        {
            this.videoControl.Dispatcher.Invoke(new Action(() =>
            {
                this.videoControl.ShowImageSource(data);
            }));
        }

        //Exit application functions
        //All the feeds are stopped and disposed of then the whole application is closed.
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            new Action(() =>
            {
                this.handDataSource.Stop();
                this.factory.Dispose();
                System.Windows.Forms.Application.Exit();
            }).BeginInvoke(null, null);
        }


        //Load file functions
        //The load file dialog is opened for the user to find their CSS file then the system loads the file into the text block and adds two new lines so the user can begin coding without need to use any further mouse clicks
        private void buttonLoad_Click(object sender, RoutedEventArgs e)
        {
            dynamicGestureBlocker = true;
            token = true;
            LoadFile();
        }

        private void LoadFile()
        {
            if (dynamicGestureBlocker == true)
            {
                OpenFileDialog loadFile = new OpenFileDialog();
                if (token == true)
                {
                    token = false;
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        loadFile.ShowDialog();

                        try
                        {
                            cssTxtBlock.Text = File.ReadAllText(loadFile.FileName);
                            cssTxtBlock.Focus();
                            cssTxtBlock.Select(cssTxtBlock.Text.Length, 0);
                            cssTxtBlock.Text += "\n\n";
                            cssTxtBlock.CaretIndex = cssTxtBlock.Text.Length;
                        }
                        catch
                        {
                            //do nothing
                        }
                    }));
                }
                if (token == false)
                {
                    DelayGesture(3000);
                }
            }
        }

        //Save file functions
        //the save file dialog is opened and the user can specify a name and location for thie CSS file.
        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            dynamicGestureBlocker = true;
            token = true;
            SaveFile();
        }

        private void SaveFile()
        {
            if (dynamicGestureBlocker == true)
            {
                SaveFileDialog saveFile = new SaveFileDialog();
                saveFile.Filter = "CSS Files | *.css";
                saveFile.DefaultExt = "css";
                if (token == true)
                {
                    token = false;
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        saveFile.ShowDialog();
                        
                        string fileName = saveFile.FileName;
                        try
                        {
                            File.WriteAllText(fileName, cssTxtBlock.Text);
                        
                        }
                        catch
                        {
                            //do nothing
                        }
                    }));
                }
                
                if (token == false)
                {
                    DelayGesture(3000);
                }
            }

        }

        //The GestureToText function takes in incoming text associated with a performed gesture and adds it to the text block before calling the ImageSectionVisible function and the gesture blocker function
        public void GestureToText(string gesToTxt)
        {
            int wordEndPosition = cssTxtBlock.SelectionLength;
            int currentPosition = wordEndPosition;
            int selStart = cssTxtBlock.SelectionStart;

            if (gesToTxt == "\n")
            {
                this.Dispatcher.Invoke((Action)(() =>
                {
                    cssTxtBlock.Focus();
                    cssTxtBlock.Text = cssTxtBlock.Text.Insert(cssTxtBlock.SelectionStart, ";\n\t");
                    cssTxtBlock.SelectionStart = selStart + 3;
                    ImageSectionVisible(gesToTxt);
                    DelayGesture(3000);
                }));
            }
            else if (gesToTxt != "\b")
            {
                this.Dispatcher.Invoke((Action)(() =>
                {
                    cssTxtBlock.Focus();
                    cssTxtBlock.Text = cssTxtBlock.Text.Insert(cssTxtBlock.SelectionStart, gesToTxt.ToString());
                    cssTxtBlock.SelectionStart = selStart + gesToTxt.Length;
                    ImageSectionVisible(gesToTxt);
                    DelayGesture(3000);
                }));
            }
            else if(gesToTxt == "\b")
            {
                this.Dispatcher.Invoke((Action)(() =>
                {
                    if (cssTxtBlock.Text.Length > 0)
                    {
                        cssTxtBlock.Undo();
                        ImageSectionVisible("Undo");
                        DelayGesture(3000);
                    }
                }));
            }
        }

        //This section activates the Content Displayer 
        //It uses the performed gesture text to display the relevant, text and image.
        public void ImageSectionVisible(string gesToTxt)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                foreach (var i in xml.Selector)
                {
                    if (gesToTxt == i)
                    {
                        if (gesToTxt == "*")
                        {
                            gestureImage.Source = new BitmapImage(new Uri("Images/Selector/all.png", UriKind.Relative));
                            break;
                        }
                        else if (gesToTxt == "#")
                        {
                            gestureImage.Source = new BitmapImage(new Uri("Images/Selector/div.png", UriKind.Relative));
                            break;
                        }
                        else
                        {
                            gestureImage.Source = new BitmapImage(new Uri("Images/Selector/" + i + ".png", UriKind.Relative));
                            break;
                        }
                    }
                }

                foreach (var i in xml.Property)
                {
                    if (gesToTxt == i)
                    {
                        gestureImage.Source = new BitmapImage(new Uri("Images/Property/" + i + ".png", UriKind.Relative));
                        break;
                    }
                }

                foreach (var i in xml.Value)
                {
                    if (gesToTxt == i)
                    {
                        gestureImage.Source = new BitmapImage(new Uri("Images/Value/" + i + ".png", UriKind.Relative));
                        break;
                    }
                }

                for (int i = 0; i <= 9; i++)
                {
                    if (gesToTxt == i.ToString())
                    {
                        gestureImage.Source = new BitmapImage(new Uri("Images/Numbers/" + i.ToString() + ".png", UriKind.Relative));
                        break;
                    }
                }
                
                for (char i = 'a'; i <= 'z'; i++)
                {
                    if (gesToTxt == i.ToString())
                    {
                        gestureImage.Source = new BitmapImage(new Uri("Images/Alphabet/" + i.ToString() + ".png", UriKind.Relative));
                        break;
                    }
                }

                if (gesToTxt == "Undo")
                {
                    gestureImage.Source = new BitmapImage(new Uri("Images/No_Type/undo.png", UriKind.Relative));
                }
                else if (gesToTxt == "Beautifier")
                {
                    gestureImage.Source = new BitmapImage(new Uri("Images/No_Type/beautifier.png", UriKind.Relative));
                }
                else if (gesToTxt == "\n")
                {
                    gestureImage.Source = new BitmapImage(new Uri("Images/No_Type/newline.png", UriKind.Relative));
                }
                gestureImage.Visibility = Visibility.Visible;
                arrowImage.Visibility = Visibility.Visible;

                if (gesToTxt == "\n")
                {
                    gestureTextInfo.Content = "New Line";
                }
                else
                {
                    gestureTextInfo.Content = gesToTxt.ToString();
                }
            }));
        }

        //This function hides the Content Displayer and reinitialises the image and text values.
        public void ImageSectionHidden()
        {
            this.Dispatcher.Invoke((Action)(() =>
                {
                    gestureImage.Source = null;
                    gestureImage.Visibility = Visibility.Hidden;
                    arrowImage.Visibility = Visibility.Hidden;
                    gestureTextInfo.Content = "";
                }));
        }

        //Keyboard key press for the beautifier fuction
        public void KeyListener(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                Beautifier();
            }
        }

        //Beautifier Function
        //This out lays the structure of the text box in a format that follows the CSS structure
        public void Beautifier()
        {
            //variables that store the properties of the text block
            int wordEndPosition = cssTxtBlock.SelectionStart;
            int currentPosition = wordEndPosition;
            int selStart = cssTxtBlock.SelectionStart;

            this.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
            {
                //This loop uses the crrent position of the text block to find the nearest text that is not whitespace
                while (currentPosition > 0 && cssTxtBlock.Text[currentPosition - 1] != ' ')
                {
                    currentPosition--;
                    
                }
                //Checks to see if there is text in the text block
                if (cssTxtBlock.Text != null)
                {
                    
                    try
                    {
                        //A condiotnal statement to check whether a beautifier loop has been exectued already
                        //Checks to see if the text in the text block matches a CSS element or is a token is raised
                        //the text box is then manipulated with the appropriate beautifier layout
                        //the Content Displayer is then activated before executing the Gesture blocker function
                        if (beautifierComplete == false)
                        {
                            
                            foreach (var i in xml.Selector)
                            {
                                
                                if (cssTxtBlock.Text.Substring(currentPosition, wordEndPosition - currentPosition) == i && valueToken == false || selectorToken == true && valueToken == false)
                                {
                                    this.Dispatcher.Invoke((Action)(() =>
                                    {
                                        cssTxtBlock.Text += " {\n\t\n}";
                                        cssTxtBlock.SelectionStart = selStart + 4;
                                        selectorToken = false;
                                        propertyToken = true;
                                    }));
                                    beautifierComplete = true;
                                    break;
                                }
                            }
                        }

                        if (beautifierComplete == false)
                        {
                            foreach (var i in xml.Property)
                            {

                                string propertyTxt = cssTxtBlock.Text.Substring(currentPosition, wordEndPosition - currentPosition);
                                propertyTxt = Regex.Replace(propertyTxt, @"\t|\n|\r|{", "");
                                if (propertyTxt == i || propertyToken == true)
                                {
                                    this.Dispatcher.Invoke((Action)(() =>
                                    {
                                        cssTxtBlock.Text = cssTxtBlock.Text.Insert(cssTxtBlock.SelectionStart, ": ");
                                        cssTxtBlock.SelectionStart = selStart + 2;
                                        valueToken = true;
                                        propertyToken = false;
                                    }));
                                    beautifierComplete = true;
                                    break;
                                }
                            }
                        }
                        if (beautifierComplete == false)
                        {
                            foreach (var i in xml.Value)
                            {
                                if (cssTxtBlock.Text.Substring(currentPosition, wordEndPosition - currentPosition) == ";")
                                {
                                    this.Dispatcher.Invoke((Action)(() =>
                                    {
                                        cssTxtBlock.Text += "\n\n";
                                        cssTxtBlock.CaretIndex = selStart + 4;
                                    }));
                                    beautifierComplete = true;
                                    break;
                                }

                                else if (cssTxtBlock.Text.Substring(currentPosition, wordEndPosition - currentPosition) == i || valueToken == true && propertyToken == false)
                                {
                                    this.Dispatcher.Invoke((Action)(() =>
                                    {
                                        cssTxtBlock.Text = cssTxtBlock.Text.Insert(cssTxtBlock.SelectionStart, ";");
                                        cssTxtBlock.Text += "\n\n";
                                        cssTxtBlock.CaretIndex = selStart + 6;
                                        valueToken = false;
                                        selectorToken = false;
                                        propertyToken = false;
                                    }));
                                    beautifierComplete = true;
                                    break;
                                }
                            }
                        }
                    }
                    catch
                    {
                        //do nothing
                    }
                    ImageSectionVisible("Beautifier");
                    DelayGesture(3000);
                }

            }));
        }

        //The Gesture Blocker function used to stop the live feed from accepting any gestures for a specified lenght of time.
        //After the timer is over, variables are reinitialised
        private void DelayGesture(int time)
        {
            gestureTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            gestureTimer.Interval = time;
            gestureTimer.Enabled = true;
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            isSet = false;
            dynamicGestureBlocker = false;
            beautifierComplete = false;
            ImageSectionHidden();
            gestureTimer.Stop();
        }

        //Function that watches for any hands and fingers available as well as to see if a particular gesture has been performed.
        //Gestures types are indicated by the number of fingers on the left hand 
        //the values of a type is specified by the right hand and in what direction the right hand is moving in.
        public void handDataSource_NewDataAvailable(HandCollection data)
        {

            //when 1 or more hands are found, enter the loop
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


                    //Alpha-Numeric Specific Gestures
                    //
                    //**Numerical Gestures**//
                    if (leftHand.FingerCount == 0)
                    {
                        if (rightHand.FingerCount == 1)
                        {
                            if (isSet == false)
                            {
                                rHndX = rightHand.PalmX;
                                isSet = true;
                            }
                            //0
                            if (rightHand.PalmX < rHndX - 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "0";
                                    GestureToText(gesToTxt);
                                }
                            }
                            //1
                            else if (rightHand.PalmX > rHndX + 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "1";
                                    GestureToText(gesToTxt);
                                }
                            }
                        }

                        else if (rightHand.FingerCount == 2)
                        {
                            if (isSet == false)
                            {
                                rHndX = rightHand.PalmX;
                                isSet = true;
                            }
                            //2
                            if (rightHand.PalmX < rHndX - 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "2";
                                    GestureToText(gesToTxt);
                                }
                            }
                            //3
                            else if (rightHand.PalmX > rHndX + 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "3";
                                    GestureToText(gesToTxt);
                                }
                            }
                        }

                        else if (rightHand.FingerCount == 3)
                        {
                            if (isSet == false)
                            {
                                rHndX = rightHand.PalmX;
                                isSet = true;
                            }
                            //4
                            if (rightHand.PalmX < rHndX - 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "4";
                                    GestureToText(gesToTxt);
                                }
                            }
                            //5
                            else if (rightHand.PalmX > rHndX + 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "5";
                                    GestureToText(gesToTxt);
                                }
                            }
                        }

                        else if (rightHand.FingerCount == 4)
                        {
                            if (isSet == false)
                            {
                                rHndX = rightHand.PalmX;
                                isSet = true;
                            }
                            //6
                            if (rightHand.PalmX < rHndX - 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "6";
                                    GestureToText(gesToTxt);
                                }
                            }
                            //7
                            else if (rightHand.PalmX > rHndX + 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "7";
                                    GestureToText(gesToTxt);
                                }
                            }
                        }

                        else if (rightHand.FingerCount == 5)
                        {
                            if (isSet == false)
                            {
                                rHndX = rightHand.PalmX;
                                isSet = true;
                            }
                            //8
                            if (rightHand.PalmX < rHndX - 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "8";
                                    GestureToText(gesToTxt);
                                }
                            }
                            //9
                            else if (rightHand.PalmX > rHndX + 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "9";
                                    GestureToText(gesToTxt);
                                }
                            }
                        }
                    }
                    //**Numerical End**//

                    //**Alphabetical Gestures**//
                    if (leftHand.FingerCount == 1)
                    {
                        if (rightHand.FingerCount == 0)
                        {
                            if (isSet == false)
                            {
                                lHndX = leftHand.PalmX;
                                rHndX = rightHand.PalmX;
                                isSet = true;
                            }
                            //a
                            if (rightHand.PalmX < rHndX - 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "a";
                                    GestureToText(gesToTxt);
                                }
                            }
                            //b
                            else if (rightHand.PalmX > rHndX + 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "b";
                                    GestureToText(gesToTxt);
                                }
                            }
                            //c
                            else if (leftHand.PalmX < lHndX - 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "c";
                                    GestureToText(gesToTxt);
                                }
                            }
                            //d
                            else if (leftHand.PalmX > lHndX + 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "d";
                                    GestureToText(gesToTxt);
                                }
                            }
                        }

                        if (rightHand.FingerCount == 1)
                        {
                            if (isSet == false)
                            {
                                lHndX = leftHand.PalmX;
                                rHndX = rightHand.PalmX;
                                isSet = true;
                            }
                            //e
                            if (rightHand.PalmX < rHndX - 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "e";
                                    GestureToText(gesToTxt);
                                }
                            }
                            //f
                            else if (rightHand.PalmX > rHndX + 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "f";
                                    GestureToText(gesToTxt);
                                }
                            }
                            //g
                            else if (leftHand.PalmX < lHndX - 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "g";
                                    GestureToText(gesToTxt);
                                }
                            }
                            //h
                            else if (leftHand.PalmX > lHndX + 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "h";
                                    GestureToText(gesToTxt);
                                }
                            }
                        }

                        if (rightHand.FingerCount == 2)
                        {
                            if (isSet == false)
                            {
                                lHndX = leftHand.PalmX;
                                rHndX = rightHand.PalmX;
                                isSet = true;
                            }
                            //i
                            if (rightHand.PalmX < rHndX - 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "i";
                                    GestureToText(gesToTxt);
                                }
                            }
                            //j
                            else if (rightHand.PalmX > rHndX + 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "j";
                                    GestureToText(gesToTxt);
                                }
                            }
                            //k
                            else if (leftHand.PalmX < lHndX - 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "k";
                                    GestureToText(gesToTxt);
                                }
                            }
                            //l
                            else if (leftHand.PalmX > lHndX + 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "l";
                                    GestureToText(gesToTxt);
                                }
                            }
                        }

                        if (rightHand.FingerCount == 3)
                        {
                            if (isSet == false)
                            {
                                lHndX = leftHand.PalmX;
                                rHndX = rightHand.PalmX;
                                isSet = true;
                            }
                            //m
                            if (rightHand.PalmX < rHndX - 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "m";
                                    GestureToText(gesToTxt);
                                }
                            }
                            //n
                            else if (rightHand.PalmX > rHndX + 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "n";
                                    GestureToText(gesToTxt);
                                }
                            }
                            //o
                            else if (leftHand.PalmX < lHndX - 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "o";
                                    GestureToText(gesToTxt);
                                }
                            }
                            //p
                            else if (leftHand.PalmX > lHndX + 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "p";
                                    GestureToText(gesToTxt);
                                }
                            }
                        }

                        if (rightHand.FingerCount == 4)
                        {
                            if (isSet == false)
                            {
                                lHndX = leftHand.PalmX;
                                rHndX = rightHand.PalmX;
                                isSet = true;
                            }
                            //q
                            if (rightHand.PalmX < rHndX - 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "q";
                                    GestureToText(gesToTxt);
                                }
                            }
                            //r
                            else if (rightHand.PalmX > rHndX + 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "r";
                                    GestureToText(gesToTxt);
                                }
                            }
                            //s
                            else if (leftHand.PalmX < lHndX - 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "s";
                                    GestureToText(gesToTxt);
                                }
                            }
                            //t
                            else if (leftHand.PalmX > lHndX + 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "t";
                                    GestureToText(gesToTxt);
                                }
                            }
                        }

                        if (rightHand.FingerCount == 5)
                        {
                            if (isSet == false)
                            {
                                lHndX = leftHand.PalmX;
                                rHndX = rightHand.PalmX;
                                lHndY = leftHand.PalmY;
                                rHndY = rightHand.PalmY;
                                isSet = true;
                            }
                            //u
                            if (rightHand.PalmX < rHndX - 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "u";
                                    GestureToText(gesToTxt);
                                }
                            }
                            //v
                            else if (rightHand.PalmX > rHndX + 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "v";
                                    GestureToText(gesToTxt);
                                }
                            }
                            //w
                            else if (leftHand.PalmX < lHndX - 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "w";
                                    GestureToText(gesToTxt);
                                }
                            }
                            //x
                            else if (leftHand.PalmX > lHndX + 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "x";
                                    GestureToText(gesToTxt);
                                }
                            }
                            //y
                            else if (leftHand.PalmY < lHndY - 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "y";
                                    GestureToText(gesToTxt);
                                }
                            }
                            //z
                            else if (rightHand.PalmY < rHndY - 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "z";
                                    GestureToText(gesToTxt);
                                }
                            }
                        }
                    }
                    //**Alphabetical End**//


                    //CSS Specific Gestures
                    //
                    //**Value Gestures**//
                    if (leftHand.FingerCount == 2)
                    {
                        if (rightHand.FingerCount == 1)
                        {
                            if (isSet == false)
                            {
                                rHndX = rightHand.PalmX;
                                isSet = true;
                            }
                            //absolute
                            if (rightHand.PalmX < rHndX - 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "absolute";
                                    GestureToText(gesToTxt);
                                }
                            }
                            //fixed
                            else if (rightHand.PalmX > rHndX + 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "fixed";
                                    GestureToText(gesToTxt);
                                }
                            }
                        }

                        else if (rightHand.FingerCount == 2)
                        {
                            if (isSet == false)
                            {
                                rHndX = rightHand.PalmX;
                                isSet = true;
                            }
                            //hidden
                            if (rightHand.PalmX < rHndX - 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "hidden";
                                    GestureToText(gesToTxt);
                                }
                            }
                            //italic
                            else if (rightHand.PalmX > rHndX + 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "italic";
                                    GestureToText(gesToTxt);
                                }
                            }
                        }

                        else if (rightHand.FingerCount == 3)
                        {
                            if (isSet == false)
                            {
                                rHndX = rightHand.PalmX;
                                isSet = true;
                            }
                            //left
                            if (rightHand.PalmX < rHndX - 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "left";
                                    GestureToText(gesToTxt);
                                }
                            }
                            //normal
                            else if (rightHand.PalmX > rHndX + 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "normal";
                                    GestureToText(gesToTxt);
                                }
                            }
                        }

                        else if (rightHand.FingerCount == 4)
                        {
                            if (isSet == false)
                            {
                                rHndX = rightHand.PalmX;
                                isSet = true;
                            }
                            //relative
                            if (rightHand.PalmX < rHndX - 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "relative";
                                    GestureToText(gesToTxt);
                                }
                            }
                            //right
                            else if (rightHand.PalmX > rHndX + 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "right";
                                    GestureToText(gesToTxt);
                                }
                            }
                        }

                        else if (rightHand.FingerCount == 5)
                        {
                            if (isSet == false)
                            {
                                rHndX = rightHand.PalmX;
                                isSet = true;
                            }
                            //scroll
                            if (rightHand.PalmX < rHndX - 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "scroll";
                                    GestureToText(gesToTxt);
                                }
                            }
                        }
                    }
                    //**Value End**//

                    //**Property Gestures**//
                    if (leftHand.FingerCount == 3)
                    {
                        if (rightHand.FingerCount == 1)
                        {
                            if (isSet == false)
                            {
                                rHndX = rightHand.PalmX;
                                isSet = true;
                            }
                            //background
                            if (rightHand.PalmX < rHndX - 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "background";
                                    GestureToText(gesToTxt);
                                }
                            }
                            //color
                            else if (rightHand.PalmX > rHndX + 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "color";
                                    GestureToText(gesToTxt);
                                }
                            }
                        }

                        else if (rightHand.FingerCount == 2)
                        {
                            if (isSet == false)
                            {
                                rHndX = rightHand.PalmX;
                                isSet = true;
                            }
                            //font-style
                            if (rightHand.PalmX < rHndX - 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "font-style";
                                    GestureToText(gesToTxt);
                                }
                            }
                            //height
                            else if (rightHand.PalmX > rHndX + 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "height";
                                    GestureToText(gesToTxt);
                                }
                            }
                        }

                        else if (rightHand.FingerCount == 3)
                        {
                            if (isSet == false)
                            {
                                rHndX = rightHand.PalmX;
                                isSet = true;
                            }
                            //overflow
                            if (rightHand.PalmX < rHndX - 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "overflow";
                                    GestureToText(gesToTxt);
                                }
                            }
                            //position
                            else if (rightHand.PalmX > rHndX + 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "position";
                                    GestureToText(gesToTxt);
                                }
                            }
                        }

                        else if (rightHand.FingerCount == 4)
                        {
                            if (isSet == false)
                            {
                                rHndX = rightHand.PalmX;
                                isSet = true;
                            }
                            //text-align
                            if (rightHand.PalmX < rHndX - 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "text-align";
                                    GestureToText(gesToTxt);
                                }
                            }
                            //visibility
                            else if (rightHand.PalmX > rHndX + 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "visibility";
                                    GestureToText(gesToTxt);
                                }
                            }
                        }

                        else if (rightHand.FingerCount == 5)
                        {
                            if (isSet == false)
                            {
                                rHndX = rightHand.PalmX;
                                isSet = true;
                            }
                            //width
                            if (rightHand.PalmX < rHndX - 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "width";
                                    GestureToText(gesToTxt);
                                }
                            }
                        }
                    }
                    //**Property End**//

                    //**Selector Gestures**//
                    if (leftHand.FingerCount == 4)
                    {
                        if (rightHand.FingerCount == 1)
                        {
                            if (isSet == false)
                            {
                                rHndX = rightHand.PalmX;
                                isSet = true;
                            }
                            //.
                            if (rightHand.PalmX < rHndX - 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = ".";
                                    selectorToken = true;
                                    GestureToText(gesToTxt);
                                }
                            }
                            //*
                            else if (rightHand.PalmX > rHndX + 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "*";
                                    if (valueToken == true)
                                    {
                                        selectorToken = false;
                                    }
                                    else
                                    {
                                        selectorToken = true;
                                    }
                                    GestureToText(gesToTxt);
                                }
                            }
                        }

                        else if (rightHand.FingerCount == 2)
                        {
                            if (isSet == false)
                            {
                                rHndX = rightHand.PalmX;
                                isSet = true;
                            }
                            //#
                            if (rightHand.PalmX < rHndX - 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "#";
                                    if (valueToken == true)
                                    {
                                        selectorToken = false;
                                    }
                                    else
                                    {
                                        selectorToken = true;
                                    }
                                    GestureToText(gesToTxt);
                                }
                            }
                            //body
                            else if (rightHand.PalmX > rHndX + 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "body";
                                    GestureToText(gesToTxt);
                                }
                            }
                        }

                        else if (rightHand.FingerCount == 3)
                        {
                            if (isSet == false)
                            {
                                rHndX = rightHand.PalmX;
                                isSet = true;
                            }
                            //head
                            if (rightHand.PalmX < rHndX - 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "head";
                                    GestureToText(gesToTxt);
                                }
                            }
                            //html
                            else if (rightHand.PalmX > rHndX + 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "html";
                                    GestureToText(gesToTxt);
                                }
                            }
                        }

                        else if (rightHand.FingerCount == 4)
                        {
                            if (isSet == false)
                            {
                                rHndX = rightHand.PalmX;
                                isSet = true;
                            }
                            //iframe
                            if (rightHand.PalmX < rHndX - 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "iframe";
                                    GestureToText(gesToTxt);
                                }
                            }
                            //li
                            else if (rightHand.PalmX > rHndX + 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "li";
                                    GestureToText(gesToTxt);
                                }
                            }
                        }

                        else if (rightHand.FingerCount == 5)
                        {
                            if (isSet == false)
                            {
                                rHndX = rightHand.PalmX;
                                isSet = true;
                            }
                            //title
                            if (rightHand.PalmX < rHndX - 100)
                            {
                                if (dynamicGestureBlocker == false)
                                {
                                    dynamicGestureBlocker = true;
                                    gesToTxt = "title";
                                    GestureToText(gesToTxt);
                                }
                            }
                        }
                    }
                    //**Selector End**//


                    //Functional Gestures
                    //
                    //Beautifer Gesture
                    if (leftHand.FingerCount == 5 && rightHand.FingerCount == 0)
                    {
                        if (isSet == false)
                        {
                            rHndX = rightHand.PalmX;
                            isSet = true;
                        }

                        if (rightHand.PalmX > rHndX + 100)
                        {
                            if (dynamicGestureBlocker == false)
                            {
                                dynamicGestureBlocker = true;
                                Beautifier();
                            }
                        }
                    }

                    if (leftHand.FingerCount == 5 && rightHand.FingerCount == 1)
                    {
                        if (isSet == false)
                        {
                            rHndX = rightHand.PalmX;
                            isSet = true;
                        }

                        if (rightHand.PalmX < rHndX - 100)
                        {
                            if (dynamicGestureBlocker == false)
                            {
                                propertyToken = true;
                                dynamicGestureBlocker = true;
                                gesToTxt = "\n";
                                GestureToText(gesToTxt);
                            }
                        }
                    }

                    //Undo Gesture
                    else if (leftHand.FingerCount == 5 && rightHand.FingerCount == 2)
                    {
                        if (isSet == false)
                        {
                            rHndX = rightHand.PalmX;
                            isSet = true;
                        }

                        if (rightHand.PalmX > rHndX + 100)
                        {
                            if (dynamicGestureBlocker == false)
                            {
                                dynamicGestureBlocker = true;
                                gesToTxt = "\b";
                                GestureToText(gesToTxt);
                            }
                        }
                    }

                    //Exit Application
                    else if (leftHand.FingerCount == 5 && rightHand.FingerCount == 5)
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
                                if (dynamicGestureBlocker == false)
                                {
                                    ExitGesture();
                                }
                            }
                        }

                    }
                }
            }
        }
    }
}
