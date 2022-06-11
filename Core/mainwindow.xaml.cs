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

namespace Gevjon {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public class Config {
        private string path;
        private volatile Dictionary<string, string> datas;
        public string get(string k) {
            load();
            return datas[k];
        }
        public void set(string k, string v) {
            datas[k] = v;
            save();
        }
        public Config(String path) {
            this.path = path;
            this.datas = init();
            load();
            save();
        }
        private void load() {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            serializer.MaxJsonLength = 1024 * 1024 * 16;
            if (File.Exists(path)) {
                using (StreamReader file = File.OpenText(path)) {
                    string jsonStr = file.ReadToEnd();
                    var _datas = serializer.Deserialize<Dictionary<string, string>>(jsonStr);
                    foreach (var data in _datas) {
                        datas[data.Key] = data.Value;
                    }
                }
            }
        }
        private Dictionary<string, string> init() {
            Dictionary<string, string> res = new Dictionary<string, string>();
            var defaultCfg = new {
                autoUpdate = "1",
                alpha = "0.5",
                left = "0",
                top = "0",
                width = "380",
                height = "400",
                title = "masterduel",
                onTop = "1",
                pipeServer = "1",
                lightMode = "0",
                currentFontName = "Microsoft YaHei UI",
                currentFontSize = "16",
                verURL = "https://ghproxy.com/https://raw.githubusercontent.com/RyoLee/Gevjon/gh-pages/version.txt",
                dlURL = "https://github.com/RyoLee/Gevjon/releases/latest",
                dataVerURL = "https://ygocdb.com/api/v0/cards.zip.md5",
                dataDlURL = "https://ygocdb.com/api/v0/cards.zip",
                dataVer = "0000",
                autoScroll = "1"
            };
            foreach (var prop in defaultCfg.GetType().GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)) {
                res[prop.Name] = prop.GetValue(defaultCfg, null).ToString();
            }
            return res;
        }
        private void save() {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            serializer.MaxJsonLength = 1024 * 1024 * 16;
            string jsonStr = FormatOutput(serializer.Serialize(datas));
            using (FileStream fs = new FileStream(path, FileMode.Create)) {
                using (StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8)) {
                    sw.WriteLine(jsonStr);
                }
            }
        }
        private static string FormatOutput(string jsonString) {
            var stringBuilder = new System.Text.StringBuilder();

            bool escaping = false;
            bool inQuotes = false;
            int indentation = 0;

            foreach (char character in jsonString) {
                if (escaping) {
                    escaping = false;
                    stringBuilder.Append(character);
                } else {
                    if (character == '\\') {
                        escaping = true;
                        stringBuilder.Append(character);
                    } else if (character == '\"') {
                        inQuotes = !inQuotes;
                        stringBuilder.Append(character);
                    } else if (!inQuotes) {
                        if (character == ',') {
                            stringBuilder.Append(character);
                            stringBuilder.Append("\r\n");
                            stringBuilder.Append('\t', indentation);
                        } else if (character == '[' || character == '{') {
                            stringBuilder.Append(character);
                            stringBuilder.Append("\r\n");
                            stringBuilder.Append('\t', ++indentation);
                        } else if (character == ']' || character == '}') {
                            stringBuilder.Append("\r\n");
                            stringBuilder.Append('\t', --indentation);
                            stringBuilder.Append(character);
                        } else if (character == ':') {
                            stringBuilder.Append(character);
                            stringBuilder.Append(' ');
                        } else if (!Char.IsWhiteSpace(character)) {
                            stringBuilder.Append(character);
                        }
                    } else {
                        stringBuilder.Append(character);
                    }
                }
            }
            return stringBuilder.ToString();
        }
    }
    public partial class MainWindow : Window {
        static string dbFile = "cards.json";
        static string cfgFile = "config.json";
        private YGOdb db;
        public Config config;
        private PipeServer pipeServer;
        class PipeServer {
            private MainWindow mainWindow;
            private bool stopFlag = true;
            public PipeServer(MainWindow mainWindow) {
                this.mainWindow = mainWindow;
                Task.Run(() => {
                    while (true) {
                        if (stopFlag) {
                            System.Threading.Thread.Sleep(1000);
                        } else {
                            StartPipeServer();
                        }
                    }
                });
            }
            private void StartPipeServer() {
                using (NamedPipeServerStream pipeServer = new NamedPipeServerStream("GevjonCore", PipeDirection.InOut)) {
                    Console.WriteLine("NamedPipeServer Start.");
                    Console.Write("Waiting for client connection...");
                    pipeServer.WaitForConnection();
                    Console.WriteLine("Client connected.");
                    try {
                        using (StreamReader sr = new StreamReader(pipeServer)) {
                            string temp = "";
                            string line;
                            while ((line = sr.ReadLine()) != null) {
                                temp += line;
                            }
                            Console.WriteLine("Received from client: {0}", temp);
                            try {
                                JavaScriptSerializer serializer = new JavaScriptSerializer();
                                Dictionary<string, object> json = (Dictionary<string, object>)serializer.DeserializeObject(temp);
                                string mode = json["mode"].ToString();
                                if (Enum.IsDefined(typeof(MODES), mode)) {
                                    switch ((MODES)Enum.Parse(typeof(MODES), mode, true)) {
                                        case MODES.exact:
                                            mainWindow.ControlGrid.Dispatcher.Invoke(new Action(() => {
                                                if ("1".Equals(mainWindow.config.get("autoScroll"))) {
                                                    if ("⇲".Equals(mainWindow.ResizeButton.Content)) {
                                                        mainWindow.ResizeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                                                    }
                                                }
                                                mainWindow.CardSearchBox.Text = json["data"].ToString();
                                                mainWindow.Find(true);
                                            }));
                                            break;
                                        case MODES.fuzzy:
                                            mainWindow.ControlGrid.Dispatcher.Invoke(new Action(() => {
                                                if ("1".Equals(mainWindow.config.get("autoScroll"))) {
                                                    if ("⇲".Equals(mainWindow.ResizeButton.Content)) {
                                                        mainWindow.ResizeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                                                    }
                                                }
                                                mainWindow.CardSearchBox.Text = json["data"].ToString();
                                                mainWindow.Find(false);
                                            }));
                                            break;
                                        case MODES.issued:
                                            mainWindow.ControlGrid.Dispatcher.Invoke(new Action(() => {
                                                if ("1".Equals(mainWindow.config.get("autoScroll"))) {
                                                    if ("⇲".Equals(mainWindow.ResizeButton.Content)) {
                                                        mainWindow.ResizeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                                                    }
                                                }
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
                                } else {
                                    Console.WriteLine("Received unknown mode: {0}", mode);
                                }
                            }
                            catch (Exception ex) {
                                Console.WriteLine("ERROR: {0}", ex.Message);
                            }
                        }
                    }
                    catch (IOException e) {
                        Console.WriteLine("ERROR: {0}", e.Message);
                    }
                }
            }

            internal void Start() {
                stopFlag = false;
            }

            internal void Stop() {
                stopFlag = true;
            }
        }
        public class YGOdb {
            string path;

            Dictionary<string, Card> datas;
            public void reload() {
                datas = new Dictionary<string, Card>();
                using (StreamReader file = File.OpenText(path)) {
                    string jsonStr = file.ReadToEnd();
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    serializer.MaxJsonLength = 1024 * 1024 * 16;
                    Dictionary<string, Card> cards = serializer.Deserialize<Dictionary<string, Card>>(jsonStr);
                    foreach (var item in cards) {
                        Card card = item.Value;
                        if (card.en_name != null && !"".Equals(card.en_name) && !datas.ContainsKey(card.en_name)) {
                            datas.Add(card.en_name, card);
                        }
                        if (card.cn_name != null && !"".Equals(card.cn_name) && !datas.ContainsKey(card.cn_name)) {
                            datas.Add(card.cn_name, card);
                        }
                        if (card.cnocg_n != null && !"".Equals(card.cnocg_n) && !datas.ContainsKey(card.cnocg_n)) {
                            datas.Add(card.cnocg_n, card);
                        }
                        if (card.jp_name != null && !"".Equals(card.jp_name) && !datas.ContainsKey(card.jp_name)) {
                            datas.Add(card.jp_name, card);
                        }
                        if (card.jp_ruby != null && !"".Equals(card.jp_ruby) && !datas.ContainsKey(card.jp_ruby)) {
                            datas.Add(card.jp_ruby, card);
                        }
                        if (card.cid != 0 && !datas.ContainsKey(card.cid.ToString())) {
                            datas.Add(card.cid.ToString(), card);
                        }
                    }
                }
            }
            public YGOdb(string path) {
                this.path = path;
                reload();
            }
            public List<Card> Find(string key, bool exact) {
                List<Card> cards = new List<Card>();
                if (key == null || "".Equals(key.Trim())) {
                    return cards;
                }
                foreach (var item in datas) {
                    if (!cards.Contains(item.Value)) {
                        if (!exact) {
                            if (item.Key.Contains(key)) {
                                cards.Add(item.Value);
                            }
                        } else {
                            if (item.Key.Equals(key)) {
                                cards.Add(item.Value);
                            }
                        }
                    }
                }
                return cards;
            }
        }

        public class Text {
            public string types { get; set; }
            public string pdesc { get; set; }
            public string desc { get; set; }
        }
        public class Card {
            public int cid { get; set; }
            public int id { get; set; }
            public string cn_name { get; set; }
            public string cnocg_n { get; set; }
            public string jp_ruby { get; set; }
            public string jp_name { get; set; }
            public string en_name { get; set; }
            public Text text { get; set; }
            public string ItemName {
                get { return isEmpty(cn_name) ? isEmpty(cnocg_n) ? isEmpty(jp_name) ? isEmpty(jp_ruby) ? jp_ruby : en_name : jp_name : cnocg_n : cn_name; }
            }
            public override string ToString() {
                string res = reformat(en_name) + reformat(jp_name) + reformat(cn_name) + "\n";
                res += text.types + "\n\n\n";
                if (!isEmpty(text.pdesc)) {
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
            private bool isEmpty(string str) {
                return (str == null || "".Equals(str.Trim()));
            }
            private string reformat(string str) {
                return isEmpty(str) ? "" : "【" + str + "】" + "\n";
            }
        }

        public MainWindow() {
            config = new Config(cfgFile);
            db = new YGOdb(dbFile);
            Left = int.Parse(config.get("left"));
            Top = int.Parse(config.get("top"));
            InitializeComponent();
        }
        public delegate void DelegateMessage(string Reply);

        enum MODES {
            exact, fuzzy, issued
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            InitBackground();
            pipeServer = new PipeServer(this);
            Background.Opacity = float.Parse(config.get("alpha"));
            Width = int.Parse(config.get("width"));
            Height = int.Parse(config.get("height"));
            MoveButton.Content = Title + "-" + AssemblyInfo.VERSION;
            CardDescBox.FontFamily = new System.Windows.Media.FontFamily(config.get("currentFontName"));
            CardDescBox.FontSize = int.Parse(config.get("currentFontSize"));
            reload();
            e.Handled = true;
        }
        public void reload() {
            if ("1".Equals(config.get("lightMode"))) {
                ControlGrid.Visibility = Visibility.Collapsed;
            } else {
                ControlGrid.Visibility = Visibility.Visible;
            }
            if ("1".Equals(config.get("onTop"))) {
                Topmost = false;
                Topmost = true;
            } else {
                Topmost = false;
            }
            if ("1".Equals(config.get("autoUpdate"))) {
                CheckUpdate();
            }
            if ("1".Equals(config.get("pipeServer"))) { //重复start不影响
                pipeServer.Start();
            } else {
                pipeServer.Stop();
            }
        }
        private void InitBackground() {
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
            SettingButton.Background = Background;
            MoveButton.Background = Background;
            ResizeButton.Background = Background;
            LightModeButton.Background = Background;
            ExitButton.Background = Background;
            CardSearchBox.Background = Background;
            CardComboBox.Background = Background;
            CardDescBox.Background = Background;
        }

        private void Find(bool exact) {
            CardComboBox.IsEnabled = false;
            CardComboBox.ItemsSource = null;
            CardComboBox.Items.Refresh();
            List<Card> cards = db.Find(CardSearchBox.Text, exact);
            UpdateCardList(cards);
        }

        private void CardComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var comboBox = (ComboBox)sender;
            if (comboBox.SelectedItem != null) {
                var card = (Card)comboBox.SelectedItem;
                CardComboBox.IsEnabled = true;
                CardDescBox.Text = card.ToString();
            } else {
                CardComboBox.IsEnabled = false;
                CardDescBox.Text = "";
            }
            e.Handled = true;
        }

        private void UpdateCardList(List<Card> cards) {
            if (cards != null && cards.Count != 0) {
                CardComboBox.IsEnabled = true;
                CardComboBox.ItemsSource = cards;
                CardComboBox.SelectedIndex = 0;
                CardComboBox.Items.Refresh();
            } else {
                CardComboBox.IsEnabled = false;
            }
        }

        private void CardSearchBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
            if (e.Key == System.Windows.Input.Key.Enter) {
                Find(System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Control);
                e.Handled = true;
            }
        }


        private void Window_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e) {
            GevjonMainWindow.Dispatcher.Invoke(new Action(() => { this.Background.Opacity = 1; }));
            e.Handled = true;
        }

        private void Window_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e) {
            GevjonMainWindow.Dispatcher.Invoke(new Action(() => { this.Background.Opacity = float.Parse(config.get("alpha")); }));
            e.Handled = true;
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e) {
            Application.Current.Shutdown();
            e.Handled = true;
        }
        private void ResizeButton_Click(object sender, RoutedEventArgs e) {
            if ("⇱".Equals(ResizeButton.Content)) {
                config.set("width", GevjonMainWindow.RestoreBounds.Width.ToString());
                config.set("height", GevjonMainWindow.RestoreBounds.Height.ToString());
                ResizeButton.Content = "⇲";
                GevjonMainWindow.ResizeMode = ResizeMode.NoResize;
                Width = 30;
                Height = 30;
            } else {
                ResizeButton.Content = "⇱";
                GevjonMainWindow.ResizeMode = ResizeMode.CanResizeWithGrip;
                Width = int.Parse(config.get("width"));
                Height = int.Parse(config.get("height"));
            }
            e.Handled = true;
        }

        private void MoveButton_LeftMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left) {
                this.DragMove();
                e.Handled = true;
            }
        }
        private void LightModeButton_Click(object sender, RoutedEventArgs e) {
            if ("1".Equals(config.get("lightMode"))) {
                config.set("lightMode", "0");
            } else {
                config.set("lightMode", "1");
            }
            reload();
            e.Handled = true;
        }
        private async Task CheckUpdate() {
            var tak = Task.Run(() => {
                if (System.Threading.Monitor.TryEnter(this)) {
                    try {
                        string VER_URL = config.get("verURL");
                        string REL_URL = config.get("dlURL");
                        string DATA_VER_URL = config.get("dataVerURL");
                        string DATA_REL_URL = config.get("dataDlURL");
                        string remote_ver_str = HttpGet(VER_URL);
                        string locale_ver_str = System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).ProductVersion;

                        var remote_ver = new Version(remote_ver_str);
                        var locale_ver = new Version(locale_ver_str);
                        if (remote_ver.CompareTo(locale_ver) == 1) {
                            if (MessageBox.Show("本地:\t" + locale_ver_str + "\n远端:\t" + remote_ver_str + "\n是否更新?", "发现新版本", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes) {
                                System.Diagnostics.Process.Start(REL_URL);
                            }
                        }
                        string remote_data_ver_str = HttpGet(DATA_VER_URL).Replace("\"", "");
                        string locale_data_ver_str = config.get("dataVer");
                        if (!locale_data_ver_str.Equals(remote_data_ver_str)) {
                            if (MessageBox.Show("本地卡片数据与服务器不一致\n是否更新?", "发现新数据", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes) {
                                using (var client = new System.Net.WebClient()) {
                                    client.DownloadFile(DATA_REL_URL, "cards.zip");
                                    using (var zipArchive = ZipFile.OpenRead("cards.zip")) {
                                        foreach (ZipArchiveEntry entry in zipArchive.Entries) {
                                            entry.ExtractToFile(entry.Name, true);
                                        }
                                        config.set("dataVer", remote_data_ver_str);
                                    }
                                    File.Delete("cards.zip");
                                    db.reload();
                                }
                            }
                        }
                    }
                    finally {
                        System.Threading.Monitor.Exit(this);
                    }
                }
            });
        }
        private string HttpGet(string url) {
            using (System.Net.Http.HttpClient client = new System.Net.Http.HttpClient()) {
                var response = client.GetAsync(url).Result;
                if (response.IsSuccessStatusCode) {
                    return response.Content.ReadAsStringAsync().Result;
                }
                return "";
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            Rect rect = this.RestoreBounds;
            config.set("left", rect.Left.ToString());
            config.set("top", rect.Top.ToString());
            config.set("width", rect.Width.ToString());
            config.set("height", rect.Height.ToString());
        }

        private void SettingButton_Click(object sender, RoutedEventArgs e) {
            var form = new Setting(this);
            this.Hide();
            form.Show();
            form.Focus();
        }
    }
}