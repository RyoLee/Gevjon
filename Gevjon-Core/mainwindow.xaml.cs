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
                                            mainWindow.SourceComboBox.Dispatcher.Invoke(new Action(() =>
                                            {
                                                mainWindow.LightModeCheckBox.IsChecked = true;
                                                string id = json["id"].ToString();
                                                string name = json["name"].ToString();
                                                string desc = json["desc"].ToString();
                                                List<Card> cards = new List<Card>() { };
                                                Card card = new Card(id, "", name, desc);
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
        class YGOdb
        {
            static string dbFile = "data.json";
            Dictionary<string, string> cnMapper;
            Dictionary<string, string> enMapper;
            Dictionary<string, string> jpMapper;
            Dictionary<string, JsonDataItem> idMapper;

            Dictionary<string, string> srcMapper;
            public class JsonDataItem
            {
                public string en;
                public string desc;
                public string ja;
                public string zh;
            }
            public YGOdb()
            {
                cnMapper = new Dictionary<string, string>();
                enMapper = new Dictionary<string, string>();
                jpMapper = new Dictionary<string, string>();
                using (StreamReader file = File.OpenText(dbFile))
                {
                    string jsonStr = file.ReadToEnd();
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    serializer.MaxJsonLength = 1024 * 1024 * 16;
                    Dictionary<string, JsonDataItem> json = serializer.Deserialize<Dictionary<string, JsonDataItem>>(jsonStr);
                    idMapper = new Dictionary<string, JsonDataItem>();
                    foreach (var item in json)
                    {
                        //JsonDataItem jsonDataItem= serializer.Deserialize<JsonDataItem>(item.Value.ToString());
                        JsonDataItem jsonDataItem = item.Value;
                        if (jsonDataItem.en != null && !"".Equals(jsonDataItem.en) && !enMapper.ContainsKey(jsonDataItem.en))
                        {
                            enMapper.Add(jsonDataItem.en, item.Key);
                        }
                        if (jsonDataItem.zh != null && !"".Equals(jsonDataItem.zh) && !cnMapper.ContainsKey(jsonDataItem.zh))
                        {
                            cnMapper.Add(jsonDataItem.zh, item.Key);
                        }
                        if (jsonDataItem.ja != null && !"".Equals(jsonDataItem.ja) && !jpMapper.ContainsKey(jsonDataItem.ja))
                        {
                            jpMapper.Add(jsonDataItem.ja, item.Key);
                        }
                        idMapper.Add(item.Key, jsonDataItem);
                    }
                }
            }
            public void ResetSrc(int index)
            {
                switch (index)
                {
                    case 0:
                        srcMapper = jpMapper;
                        break;
                    case 1:
                        srcMapper = enMapper;
                        break;
                    case 2:
                        srcMapper = cnMapper;
                        break;
                    default:
                        System.Environment.Exit(1);
                        break;
                }

            }
            public List<Card> FindById(string id, string srcName)
            {
                List<Card> cards = new List<Card>();
                if (null == id || "".Equals(id.Trim()) || !idMapper.ContainsKey(id))
                {
                    return cards;
                }
                string cid = id;
                string cname = idMapper[id].zh;
                string cdesc = idMapper[id].desc;
                cards.Add(new Card(cid, srcName == null || "".Equals(srcName.Trim()) ? "" : srcName, cname, cdesc));
                return cards;

            }
            public List<Card> FindByName(string name)
            {
                List<Card> cards = new List<Card>();
                if (null == name || "".Equals(name.Trim()))
                {
                    return cards;
                }
                List<Tuple<string, string>> ids = new List<Tuple<string, string>>();
                foreach (var item in srcMapper)
                {
                    if (item.Key.Contains(name))
                    {
                        string cid = item.Value;
                        string cname = item.Key;
                        ids.Add(new Tuple<string, string>(cid, cname));
                    }
                }
                for (int i = 0; i < ids.Count; i++)
                {
                    List<Card> temp = FindById(ids[i].Item1, ids[i].Item2);
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
        class Card
        {
            public string id;
            public string srcName;
            public string name;
            public string description;
            public bool isIssued = false;

            public Card(string id, string srcName, string name, string description)
            {
                this.id = id;
                this.srcName = srcName;
                this.name = name;
                this.description = description;
            }
            public string ItemName
            {
                get { return srcName == null || "".Equals(srcName.Trim()) ? name : srcName; }
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
            pipeServer = new PipeServer(this);
            this.Opacity = float.Parse(GetSetting("alpha", "0.75"));
            this.Width = int.Parse(GetSetting("width", "300"));
            this.Height = int.Parse(GetSetting("height", "600"));
            CardDescBox.FontFamily = new System.Windows.Media.FontFamily(GetSetting("currentFontName", "Microsoft YaHei UI"));
            CardDescBox.FontSize = int.Parse(GetSetting("currentFontSize", "14"));
            SourceComboBox.SelectedIndex = Int32.Parse(GetSetting("srcDbIndex", "0"));
            FullInfoCheckBox.IsChecked = "1".Equals(GetSetting("fullInfo", "1"));
            PipeServerCheckBox.IsChecked = "1".Equals(GetSetting("pipeServer", "0"));
            LightModeCheckBox.IsChecked = "1".Equals(GetSetting("lightMode", "0"));
            OnTopCheckBox.IsChecked = "1".Equals(GetSetting("onTop", "1"));
            e.Handled = true;
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
                if ((FullInfoCheckBox.IsChecked ?? false) && !card.isIssued)
                {
                    CardDescBox.Text = "【" + card.name + "】\n\n" + card.description + "\n\n\nID:[" + card.id + "]";
                }
                else
                {
                    CardDescBox.Text = card.description;
                }
            }
            else
            {
                CardComboBox.IsEnabled = false;
                CardDescBox.Text = "";
            }
            e.Handled = true;
        }
        private void SourceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            if (comboBox.SelectedItem != null)
            {
                db.ResetSrc(comboBox.SelectedIndex);
                SetSetting("srcDbIndex", comboBox.SelectedIndex.ToString());
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
            }
            e.Handled = true;
        }

        private void CardNameBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                FindByName();
            }
            e.Handled = true;
        }

        private void OnTopCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            SetSetting("onTop", "1");
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

        private void FullInfoCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            SetSetting("fullInfo", "1");
            e.Handled = true;
        }

        private void FullInfoCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            SetSetting("fullInfo", "0");
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
            GevjonMainWindow.Dispatcher.Invoke(new Action(() => { this.Opacity = 1; }));
            e.Handled = true;

        }

        private void Window_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            GevjonMainWindow.Dispatcher.Invoke(new Action(() => { this.Opacity = float.Parse(GetSetting("alpha", "0.75")); }));
            e.Handled = true;
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
            e.Handled = true;
        }

        private void MoveButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                this.DragMove();
            }
            e.Handled = true;
        }
    }
}