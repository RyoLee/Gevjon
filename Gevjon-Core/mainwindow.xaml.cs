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

using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Configuration;
using System.IO.Pipes;
using System.Web.Script.Serialization;
using System.Threading.Tasks;
using System.IO;

namespace Gevjon
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private YGOdb db;
        private PipeServer pipeServer;
        class PipeServer
        {
            private MainWindow mainWindow;
            private bool stopFlag = true;
            public PipeServer(MainWindow mainWindow)
            {
                this.mainWindow = mainWindow;
                Task.Run(() => {
                    while (true)
                    {
                        if (stopFlag)
                        {
                            System.Threading.Thread.Sleep(1000);
                        }
                        else
                        {
                            StartPipeServer();
                        }
                    }
                });
            }
            private void StartPipeServer()
            {
                using (NamedPipeServerStream pipeServer = new NamedPipeServerStream("GevjonCore", PipeDirection.InOut))
                {
                    Console.WriteLine("NamedPipeServer Start.");
                    Console.Write("Waiting for client connection...");
                    pipeServer.WaitForConnection();
                    Console.WriteLine("Client connected.");
                    try
                    {
                        using (System.IO.StreamReader sr = new System.IO.StreamReader(pipeServer))
                        {
                            string temp = "";
                            string line;
                            while ((line = sr.ReadLine()) != null)
                            {
                                temp += line;
                            }
                            Console.WriteLine("Received from client: {0}", temp);
                            try
                            {
                                JavaScriptSerializer serializer = new JavaScriptSerializer();
                                Dictionary<string, object> json = (Dictionary<string, object>)serializer.DeserializeObject(temp);
                                string mode = json["mode"].ToString();
                                if (Enum.IsDefined(typeof(MODES), mode))
                                {
                                    switch ((MODES)Enum.Parse(typeof(MODES), mode, true))
                                    {
                                        case MODES.id:
                                            mainWindow.ControlGrid.Dispatcher.Invoke(new Action(() =>
                                            {
                                                mainWindow.CardIdBox.Text = json["id"].ToString();
                                                mainWindow.CardNameBox.Text = "";
                                                mainWindow.FindById();
                                            }));
                                            break;
                                        case MODES.name:
                                            mainWindow.ControlGrid.Dispatcher.Invoke(new Action(() =>
                                            {
                                                mainWindow.CardIdBox.Text = "";
                                                mainWindow.CardNameBox.Text = json["name"].ToString();
                                                mainWindow.FindByName();
                                            }));
                                            break;
                                        case MODES.issued:
                                            mainWindow.ControlGrid.Dispatcher.Invoke(new Action(() =>
                                            {
                                                string id = json["id"].ToString();
                                                string name = json["name"].ToString();
                                                string desc = json["desc"].ToString();
                                                List<Card> cards = new List<Card>() { };
                                                Card card = new Card(id, name, desc);
                                                card.isIssued = true;
                                                cards.Add(card);
                                                mainWindow.UpdateCardList(cards);
                                            }));
                                            break;
                                        default:
                                            break;

                                    }
                                    /*using (System.IO.StreamWriter sw = new System.IO.StreamWriter(pipeServer))
                                    {
                                        string msg = "{ \"status\":0}";
                                        sw.WriteLine(msg);
                                    }
                                    */
                                }
                                else
                                {
                                    Console.WriteLine("Received unknown mode: {0}", mode);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("ERROR: {0}", ex.Message);
                            }
                        }
                    }
                    catch (System.IO.IOException e)
                    {
                        Console.WriteLine("ERROR: {0}", e.Message);
                    }
                }
            }

            internal void Start()
            {
                stopFlag = false;
            }

            internal void Stop()
            {
                stopFlag = true;
            }
        }
        public class YGOdb
        {
            static string dbFile = "data.json";
            Dictionary<string, JsonDataItem> ids;

            Dictionary<string, string> names;
            public class JsonDataItem
            {
                public string en;
                public string desc;
                public string ja;
                public string zh;
            }
            public YGOdb()
            {
                names = new Dictionary<string, string>();
                using (StreamReader file = File.OpenText(dbFile))
                {
                    string jsonStr = file.ReadToEnd();
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    serializer.MaxJsonLength = 1024 * 1024 * 16;
                    Dictionary<string, JsonDataItem> json = serializer.Deserialize<Dictionary<string, JsonDataItem>>(jsonStr);
                    ids = new Dictionary<string, JsonDataItem>();
                    foreach (var item in json)
                    {
                        //JsonDataItem jsonDataItem= serializer.Deserialize<JsonDataItem>(item.Value.ToString());
                        JsonDataItem jsonDataItem = item.Value;
                        if (jsonDataItem.en != null && !"".Equals(jsonDataItem.en) && !names.ContainsKey(jsonDataItem.en))
                        {
                            names.Add(jsonDataItem.en, item.Key);
                        }
                        if (jsonDataItem.zh != null && !"".Equals(jsonDataItem.zh) && !names.ContainsKey(jsonDataItem.zh))
                        {
                            names.Add(jsonDataItem.zh, item.Key);
                        }
                        if (jsonDataItem.ja != null && !"".Equals(jsonDataItem.ja) && !names.ContainsKey(jsonDataItem.ja))
                        {
                            names.Add(jsonDataItem.ja, item.Key);
                        }
                        ids.Add(item.Key, jsonDataItem);
                    }
                }
            }
            public List<Card> FindById(string id, string srcName)
            {
                List<Card> cards = new List<Card>();
                if (null == id || "".Equals(id.Trim()) || !ids.ContainsKey(id))
                {
                    return cards;
                }
                cards.Add(new Card(id, srcName == null || "".Equals(srcName.Trim()) ? "" : srcName, ids[id]));
                return cards;

            }
            public List<Card> FindByName(string name)
            {
                List<Card> cards = new List<Card>();
                if (null == name || "".Equals(name.Trim()))
                {
                    return cards;
                }
                Dictionary<string, string> tIds = new Dictionary<string, string>(); 
                foreach (var item in names)
                {
                    if (!tIds.ContainsKey(item.Value) && item.Key.Contains(name))
                    {
                        string cid = item.Value;
                        string cname = item.Key;
                        tIds.Add(cid, cname);
                    }
                }
                foreach (var key in tIds.Keys)
                {
                    List<Card> temp = FindById(key, tIds[key]);
                    if (temp.Count != 0)
                    {
                        for (int j = 0; j < temp.Count; j++)
                        {
                            cards.Add(temp[j]);
                        }
                    }
                }
                return cards;
            }
        }
        public class Card
        {
            public string id;
            public string src;
            public string en;
            public string ja;
            public string zh;
            public string desc;
            public bool isIssued = false;
            public Card(string id, string src, string desc)
            {
                this.id = id;
                this.src = src;
                this.desc = desc;
                this.isIssued = true;
            }
            public Card(string id, string src, YGOdb.JsonDataItem data)
            {
                this.id = id;
                this.src = src;
                this.en = data.en;
                this.ja = data.ja;
                this.zh = data.zh;
                this.desc = data.desc;
            }
            public string ItemName
            {
                get { return isEmpty(src) ? isEmpty(zh) ? isEmpty(ja) ? en : ja : zh : src; }
            }
            public override string ToString()
            {
                return isIssued ? desc : isEmpty(en) ? "" : reformat(en) + reformat(ja) + reformat(zh) + "\n" + desc;
            }
            private string reformat(string str)
            {
                return isEmpty(str) ? "" : "【" + str + "】" + "\n";
            }
            private bool isEmpty(string str)
            {
                return (str == null || "".Equals(str.Trim()));
            }
        }

        public MainWindow()
        {

            db = new YGOdb();
            InitializeComponent();
        }
        public delegate void DelegateMessage(string Reply);

        enum MODES
        {
            id, name, issued
        };

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitBackground();
            pipeServer = new PipeServer(this);
            Background.Opacity = float.Parse(GetSetting("alpha", "0.75"));
            Width = int.Parse(GetSetting("width", "300"));
            Height = int.Parse(GetSetting("height", "600"));
            CardDescBox.FontFamily = new System.Windows.Media.FontFamily(GetSetting("currentFontName", "Microsoft YaHei UI"));
            CardDescBox.FontSize = int.Parse(GetSetting("currentFontSize", "14"));
            PipeServerCheckBox.IsChecked = "1".Equals(GetSetting("pipeServer", "0"));
            LightModeCheckBox.IsChecked = "1".Equals(GetSetting("lightMode", "0"));
            OnTopCheckBox.IsChecked = "1".Equals(GetSetting("onTop", "1"));
            if (OnTopCheckBox.IsChecked ?? false)
            {
                Activate();
                Topmost = false;
                Topmost = true;
                Focus();
            }
            e.Handled = true;
        }
        private void InitBackground()
        {
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
            MoveButton.Background = Background;
            OnTopCheckBox.Background = Background;
            PipeServerCheckBox.Background = Background;
            LightModeCheckBox.Background = Background;
            ExitButton.Background = Background;
            CardIdBox.Background = Background;
            CardNameBox.Background = Background;
            SettingButton.Background = Background;
            CardComboBox.Background = Background;
            CardDescBox.Background = Background;
        }
        private string GetSetting(string key, string defaultValue)
        {
            return ConfigurationManager.AppSettings[key] ?? defaultValue;
        }
        private void SetSetting(string key, string value)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings[key].Value = value;
            config.Save(ConfigurationSaveMode.Modified);
        }

        private void FindById()
        {
            CardComboBox.IsEnabled = false;
            CardComboBox.ItemsSource = null;
            CardComboBox.Items.Refresh();
            List<Card> cards = db.FindById(CardIdBox.Text, "");
            UpdateCardList(cards);
        }
        private void FindByName()
        {
            CardComboBox.IsEnabled = false;
            CardComboBox.ItemsSource = null;
            CardComboBox.Items.Refresh();
            List<Card> cards = db.FindByName(CardNameBox.Text);
            UpdateCardList(cards);
        }

        private void CardComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = (ComboBox)sender;
            if (comboBox.SelectedItem != null)
            {
                var card = (Card)comboBox.SelectedItem;
                CardComboBox.IsEnabled = true;
                CardDescBox.Text = card.ToString();
            }
            else
            {
                CardComboBox.IsEnabled = false;
                CardDescBox.Text = "";
            }
            e.Handled = true;
        }

        private void UpdateCardList(List<Card> cards)
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

        private void CardIdBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                FindById();
                e.Handled = true;
            }
        }

        private void CardNameBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                FindByName();
                e.Handled = true;
            }
        }

        private void OnTopCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            SetSetting("onTop", "1");
            Topmost = false;
            Topmost = true;
            e.Handled = true;
        }

        private void OnTopCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            SetSetting("onTop", "0");
            Topmost = false;
            e.Handled = true;
        }

        private void PipeServerCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            pipeServer.Start();
            LightModeCheckBox.IsEnabled = true;
            SetSetting("pipeServer", "1");
            e.Handled = true;
        }

        private void PipeServerCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            pipeServer.Stop();
            LightModeCheckBox.IsChecked = false;
            LightModeCheckBox.IsEnabled = false;
            SetSetting("pipeServer", "0");
            e.Handled = true;
        }

        private void LightModeCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (PipeServerCheckBox.IsChecked ?? false)
            {
                ControlGrid.Visibility = Visibility.Collapsed;
                SetSetting("lightMode", "1");
            }
            e.Handled = true;
        }

        private void LightModeCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            ControlGrid.Visibility = Visibility.Visible;
            SetSetting("lightMode", "0");
            e.Handled = true;
        }

        private void Window_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            GevjonMainWindow.Dispatcher.Invoke(new Action(() => { this.Background.Opacity = 1; }));
            e.Handled = true;

        }

        private void Window_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            GevjonMainWindow.Dispatcher.Invoke(new Action(() => { this.Background.Opacity = float.Parse(GetSetting("alpha", "0.75")); }));
            e.Handled = true;
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
            e.Handled = true;
        }

        private void MoveButton_LeftMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                this.DragMove();
                e.Handled = true;
            }
        }

        private void MoveButton_RightMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Right)
            {
                if (Width != 30)
                {
                    Width = 30;
                    Height = 30;
                }
                else
                {
                    Width = int.Parse(GetSetting("width", "300"));
                    Height = int.Parse(GetSetting("height", "600"));
                }
                e.Handled = true;
            }
        }
    }
}