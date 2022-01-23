//  ---------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// 
//  The MIT License (MIT)
// 
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
// 
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
// 
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
//  ---------------------------------------------------------------------------------

using CaptureSampleCore;
using Composition.WindowsRuntimeHelpers;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Windows.Foundation.Metadata;
using Windows.Graphics.Capture;
using Windows.UI.Composition;
using System.Data.SQLite;
using System.Collections.Generic;

namespace WPFCaptureSample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IntPtr hwnd;
        private Compositor compositor;
        private CompositionTarget target;
        private ContainerVisual root;

        private BasicSampleApplication sample;
        private ObservableCollection<Process> processes;
        private YGOdb db;
        private ObservableCollection<Card> cards;
        class YGOdb
        {
            static string databaseFileName = "cards.cdb";
            static string connectionString = "data source = " + databaseFileName;
            SQLiteConnection conn;
            SQLiteCommand cmd;
            public YGOdb()
            {
                conn = new SQLiteConnection(connectionString);
                conn.Open();
                cmd = conn.CreateCommand();
            }
            public ObservableCollection<Card> findById(string id)
            {
                ObservableCollection<Card> cards = new ObservableCollection<Card>();
                if (null == id || "".Equals(id.Trim()))
                {
                    return cards;
                }
                string queryString = "select id,name,desc from texts where id like @cid";
                cmd.CommandText = queryString;
                cmd.Parameters.Add(new SQLiteParameter("@cid", '%' + id + '%'));
                SQLiteDataReader dataReader = cmd.ExecuteReader();
                while (dataReader.Read())
                {
                    string cid = dataReader["id"].ToString();
                    string cname = dataReader["name"].ToString();
                    string cdesc = dataReader["desc"].ToString();
                    cards.Add(new Card(cid, cname, cdesc));
                }
                dataReader.Close();
                return cards;

            }
            public ObservableCollection<Card> findByName(string name)
            {
                ObservableCollection<Card> cards = new ObservableCollection<Card>();
                if (null == name || "".Equals(name.Trim()))
                {
                    return cards;
                }
                string queryString = "select id,name,desc from texts where name  like @cname";
                cmd.CommandText = queryString;
                cmd.Parameters.Add(new SQLiteParameter("@cname", '%' + name + '%'));
                SQLiteDataReader dataReader = cmd.ExecuteReader();
                while (dataReader.Read())
                {
                    string cid = dataReader["id"].ToString();
                    string cname = dataReader["name"].ToString();
                    string cdesc = dataReader["desc"].ToString();
                    cards.Add(new Card(cid, cname, cdesc));
                }
                dataReader.Close();
                return cards;
            }


        }
        class Card
        {
            public string id;
            public string name;
            public string description;

            public Card(string id, string name, string description)
            {
                this.id = id;
                this.name = name;
                this.description = description;
            }
            public string Name
            {
                get { return id+":"+ name; }
            }
        }

        public MainWindow()
        {
            db = new YGOdb();
            InitializeComponent();

#if DEBUG
            // Force graphicscapture.dll to load.
            var picker = new GraphicsCapturePicker();
#endif
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var interopWindow = new WindowInteropHelper(this);
            hwnd = interopWindow.Handle;

            var presentationSource = PresentationSource.FromVisual(this);
            double dpiX = 1.0;
            double dpiY = 1.0;
            if (presentationSource != null)
            {
                dpiX = presentationSource.CompositionTarget.TransformToDevice.M11;
                dpiY = presentationSource.CompositionTarget.TransformToDevice.M22;
            }
            var controlsWidth = (float)(ControlsGrid.ActualWidth * dpiX);

            InitComposition(controlsWidth);
            InitWindowList();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            StopCapture();
            WindowComboBox.SelectedIndex = -1;
        }
        private void FindByIdButton_Click(object sender, RoutedEventArgs e)
        {
            cards = db.findById(CardIdBox.Text);
            updateCardList();
        }
        private void FindByNameButton_Click(object sender, RoutedEventArgs e)
        {
            cards = db.findByName(CardNameBox.Text);
            updateCardList();
        }
        private void CardComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = (ComboBox)sender;
            var card = (Card)comboBox.SelectedItem;
            if (card != null)
            {
                CardDescBox.Text = card.description;
            }
        }
        private void WindowComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = (ComboBox)sender;
            var process = (Process)comboBox.SelectedItem;

            if (process != null)
            {
                StopCapture();
                var hwnd = process.MainWindowHandle;
                try
                {
                    StartHwndCapture(hwnd);
                }
                catch (Exception)
                {
                    Debug.WriteLine($"Hwnd 0x{hwnd.ToInt32():X8} is not valid for capture!");
                    processes.Remove(process);
                    comboBox.SelectedIndex = -1;
                }
            }
        }

        private void InitComposition(float controlsWidth)
        {
            // Create the compositor.
            compositor = new Compositor();

            // Create a target for the window.
            target = compositor.CreateDesktopWindowTarget(hwnd, true);

            // Attach the root visual.
            root = compositor.CreateContainerVisual();
            root.RelativeSizeAdjustment = Vector2.One;
            root.Size = new Vector2(-controlsWidth, 0);
            root.Offset = new Vector3(controlsWidth, 0, 0);
            target.Root = root;

            // Setup the rest of the sample application.
            sample = new BasicSampleApplication(compositor);
            root.Children.InsertAtTop(sample.Visual);
        }

        private void InitWindowList()
        {
            if (ApiInformation.IsApiContractPresent(typeof(Windows.Foundation.UniversalApiContract).FullName, 8))
            {
                var processesWithWindows = from p in Process.GetProcesses()
                                           where !string.IsNullOrWhiteSpace(p.MainWindowTitle) && WindowEnumerationHelper.IsWindowValidForCapture(p.MainWindowHandle)
                                           select p;
                processes = new ObservableCollection<Process>(processesWithWindows);
                WindowComboBox.ItemsSource = processes;
            }
            else
            {
                WindowComboBox.IsEnabled = false;
            }
        }
        private void updateCardList()
        {
            if (cards != null && cards.Count != 0)
            {
                CardComboBox.IsEnabled = true;
                CardComboBox.ItemsSource = cards;
                CardComboBox.SelectedIndex = 0;
                CardComboBox.Items.Refresh();
            }
            else
            {
                CardComboBox.IsEnabled = false;
            }
        }

        private void StartHwndCapture(IntPtr hwnd)
        {
            GraphicsCaptureItem item = CaptureHelper.CreateItemForWindow(hwnd);
            if (item != null)
            {
                sample.StartCaptureFromItem(item);
            }
        }

        private void StartHmonCapture(IntPtr hmon)
        {
            GraphicsCaptureItem item = CaptureHelper.CreateItemForMonitor(hmon);
            if (item != null)
            {
                sample.StartCaptureFromItem(item);
            }
        }


        private void StopCapture()
        {
            sample.StopCapture();
        }
    }
}
