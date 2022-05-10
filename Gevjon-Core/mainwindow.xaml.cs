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
using System.IO.Compression;

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
                        using (StreamReader sr = new StreamReader(pipeServer))
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
                                                mainWindow.CardSearchBox.Text = json["id"].ToString();
                                                mainWindow.Find(true);
                                            }));
                                            break;
                                        case MODES.name:
                                            mainWindow.ControlGrid.Dispatcher.Invoke(new Action(() =>
                                            {
                                                mainWindow.CardSearchBox.Text = json["name"].ToString();
                                                mainWindow.Find(false);
                                            }));
                                            break;
                                        case MODES.issued:
                                            mainWindow.ControlGrid.Dispatcher.Invoke(new Action(() =>
                                            {
                                                string data = json["data"].ToString();
                                                Card card = serializer.Deserialize<Card>(data);
                                                List<Card> cards = new List<Card>() { };
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
                    catch (IOException e)
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
            static string dbFile = "cards.json";

            Dictionary<string, Card> datas;
            public void reload() {
                datas = new Dictionary<string, Card>();
                using (StreamReader file = File.OpenText(dbFile))
                {
                    string jsonStr = file.ReadToEnd();
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    serializer.MaxJsonLength = 1024 * 1024 * 16;
                    Dictionary<string, Card> cards = serializer.Deserialize<Dictionary<string, Card>>(jsonStr);
                    foreach (var item in cards)
                    {
                        Card card = item.Value;
                        if (card.en_name != null && !"".Equals(card.en_name) && !datas.ContainsKey(card.en_name))
                        {
                            datas.Add(card.en_name, card);
                        }
                        if (card.cn_name != null && !"".Equals(card.cn_name) && !datas.ContainsKey(card.cn_name))
                        {
                            datas.Add(card.cn_name, card);
                        }
                        if (card.cnocg_n != null && !"".Equals(card.cnocg_n) && !datas.ContainsKey(card.cnocg_n))
                        {
                            datas.Add(card.cnocg_n, card);
                        }
                        if (card.jp_name != null && !"".Equals(card.jp_name) && !datas.ContainsKey(card.jp_name))
                        {
                            datas.Add(card.jp_name, card);
                        }
                        if (card.jp_ruby != null && !"".Equals(card.jp_ruby) && !datas.ContainsKey(card.jp_ruby))
                        {
                            datas.Add(card.jp_ruby, card);
                        }
                        if (card.cid != 0 && !datas.ContainsKey(card.cid.ToString()))
                        {
                            datas.Add(card.cid.ToString(), card);
                        }
                    }
                }
            }
            public YGOdb()
            {
                reload();
            }
            public List<Card> Find(string key,bool exact)
            {
                List<Card> cards = new List<Card>();
                if (null == key || "".Equals(key.Trim()))
                {
                    return cards;
                }
                foreach (var item in datas)
                {
                    if (!cards.Contains(item.Value) )
                    {
                        if (!exact)
                        {
                            if (item.Key.Contains(key))
                            {
                                cards.Add(item.Value);
                            }
                        }
                        else
                        {
                            if (item.Key.Equals(key))
                            {
                                cards.Add(item.Value);
                            }
                        }
                    }
                }
                return cards;
            }
        }

        public class Text
        {
            public string types { get; set; }
            public string pdesc { get; set; }
            public string desc { get; set; }
        }
        public class Card
        {
            public int cid { get; set; }
            public int id { get; set; }
            public string cn_name { get; set; }
            public string cnocg_n { get; set; }
            public string jp_ruby { get; set; }
            public string jp_name { get; set; }
            public string en_name { get; set; }
            public Text text { get; set; }
            public string ItemName
            {
                get { return isEmpty(cn_name) ? isEmpty(cnocg_n) ? isEmpty(jp_name) ? isEmpty(jp_ruby) ? jp_ruby : en_name : jp_name : cnocg_n : cn_name; }
            }
            public override string ToString()
            {
                string res = reformat(en_name) + reformat(jp_name) + reformat(cn_name) + "\n";
                res += text.types + "\n\n\n";
                if (!isEmpty(text.pdesc))
                {
                    res += ("------------------------"
                    + "\n"
                    + text.pdesc
                    + "\n"
                    + "------------------------"
                    + "\n\n\n");
                }
                res += text.desc;
                return res;
            }
            private bool isEmpty(string str)
            {
                return (str == null || "".Equals(str.Trim()));
            }
            private string reformat(string str)
            {
                return isEmpty(str) ? "" : "【" + str + "】" + "\n";
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
            UpdateCheckBox.IsChecked = "1".Equals(GetSetting("autoUpdate", "1"));
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
            CardSearchBox.Background = Background;
            UpdateCheckBox.Background = Background;
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

        private void Find(bool exact)
        {
            CardComboBox.IsEnabled = false;
            CardComboBox.ItemsSource = null;
            CardComboBox.Items.Refresh();
            List<Card> cards = db.Find(CardSearchBox.Text, exact);
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

        private void CardSearchBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                Find(System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Control);
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

        private void UpdateCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            SetSetting("autoUpdate", "1");
            CheckUpdate();
            e.Handled = true;
        }

        private void UpdateCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            SetSetting("autoUpdate", "0");
            e.Handled = true;
        }
        private async Task CheckUpdate()
        {
            if (System.Threading.Monitor.TryEnter(UpdateCheckBox)) {
                try
                {
                    string HITS_URL = "https://hits.dwyl.com/RyoLee/Gevjon.svg";
                    string VER_URL = GetSetting("verURL", "https://cdn.jsdelivr.net/gh/RyoLee/Gevjon@gh-pages/version.txt");
                    string REL_URL = GetSetting("dlURL", "https://cdn.jsdelivr.net/gh/RyoLee/Gevjon@gh-pages/Gevjon.7z");
                    string DATA_VER_URL = GetSetting("dataVerURL", "https://ygocdb.com/api/v0/cards.zip.md5");
                    string DATA_REL_URL = GetSetting("dataDlURL", "https://ygocdb.com/api/v0/cards.zip");
                    string remote_ver_str = await TryGetAsync(VER_URL);
                    string locale_ver_str;
                    using (StreamReader reader = new StreamReader("version.txt"))
                    {
                        locale_ver_str = reader.ReadLine() ?? "";
                    }
                    var remote_ver = new Version(remote_ver_str);
                    var locale_ver = new Version(locale_ver_str);
                    if (remote_ver.CompareTo(locale_ver) == 1)
                    {
                        if (MessageBox.Show("本地:\t" + locale_ver_str + "\n远端:\t" + remote_ver_str + "\n是否更新?", "发现新版本", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                        {
                            await TryGetAsync(HITS_URL);
                            System.Diagnostics.Process.Start(REL_URL);
                        }
                    }
                    string remote_data_ver_str = await TryGetAsync(DATA_VER_URL);
                    string locale_data_ver_str;
                    using (StreamReader reader = new StreamReader("cards.ver"))
                    {
                        locale_data_ver_str = reader.ReadLine() ?? "";
                    }
                    if (!locale_data_ver_str.Equals(remote_data_ver_str))
                    {
                        if (MessageBox.Show("本地卡片数据与服务器不一致\n是否更新?", "发现新数据", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                        {
                            await TryGetAsync(HITS_URL);
                            using (var client = new System.Net.WebClient())
                            {
                                client.DownloadFile(DATA_REL_URL, "cards.zip");
                                using (var zipArchive = ZipFile.OpenRead("cards.zip"))
                                {
                                    foreach (ZipArchiveEntry entry in zipArchive.Entries)
                                    {
                                        entry.ExtractToFile(entry.Name, true);
                                    }
                                }
                                client.DownloadFile(DATA_VER_URL, "cards.ver");
                                db.reload();
                            }
                        }
                    }
                }
                finally {
                    System.Threading.Monitor.Exit(UpdateCheckBox);
                }
            }
        }
        private async Task<String> TryGetAsync(string url)
        {
            using (System.Net.Http.HttpClient client = new System.Net.Http.HttpClient())
            {
                System.Net.Http.HttpResponseMessage response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                return default;
            }
        }
    }
}