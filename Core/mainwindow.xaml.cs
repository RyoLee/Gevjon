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

using PlugIn;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Core {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    enum MODES {
        exact, fuzzy, issued
    }
    public partial class MainWindow : Window {
        static string dbFile = "cards.json";
        static string cfgFile = "config.json";
        private YGOdb db;
        public Config config;
        private IPlugIn pipeServer;

        public MainWindow() {
            config = new Config(cfgFile);
            var cfg_ver = new Version(config.get("version"));
            var cur_ver = new Version(AssemblyInfo.VERSION);
            if (cur_ver.CompareTo(cfg_ver) == 1) {
                // update from old version
                config.set("dataVer", "0000");
                config.set("version", AssemblyInfo.VERSION);
            }
            db = new YGOdb(dbFile);
            Left = int.Parse(config.get("left"));
            Top = int.Parse(config.get("top"));
            InitializeComponent();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e) {
            InitBackground();
            pipeServer = new PipeServer();
            pipeServer.PluginMessageEvent += OnPluginMessage;
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
                pipeServer.Load();
            } else {
                pipeServer.Unload();
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
            LockButton.Background = Background;
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
                CardComboBox.DisplayMemberPath = "ItemName";
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
            GevjonMainWindow.Dispatcher.Invoke(new Action(() => {
                this.Background.Opacity = float.Parse(config.get("alpha"));
            }));
            e.Handled = true;
        }
        private void LockButton_Click(object sender, RoutedEventArgs e) {
            if ("L".Equals(LockButton.Content)) {
                GevjonMainWindow.ResizeMode = ResizeMode.CanResizeWithGrip;
                LockButton.Content = "U";
            } else {
                GevjonMainWindow.ResizeMode = ResizeMode.NoResize;
                LockButton.Content = "L";
            }
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
                GevjonMainWindow.ResizeMode = ResizeMode.NoResize;
                LockButton.Content = "L";
                Width = 30;
                Height = 30;
                ResizeButton.Content = "⇲";
            } else {
                Width = int.Parse(config.get("width"));
                Height = int.Parse(config.get("height"));
                GevjonMainWindow.ResizeMode = ResizeMode.NoResize;
                LockButton.Content = "L";
                ResizeButton.Content = "⇱";
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
                        string locale_ver_str = AssemblyInfo.VERSION;

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
            if ("⇲".Equals(ResizeButton.Content)) {
                return;
            }
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
        private void OnPluginMessage(Object sender, EventArgs e) {

            try {
                Dictionary<string, object> json = JsonSerializer.Deserialize<Dictionary<string, object>>(((PluginMessageEventArgs)e).Message);
                string mode = json["mode"].ToString();
                if (Enum.IsDefined(typeof(MODES), mode)) {
                    switch ((MODES)Enum.Parse(typeof(MODES), mode, true)) {
                        case MODES.exact:
                            ControlGrid.Dispatcher.Invoke(new Action(() => {
                                if ("1".Equals(config.get("autoScroll"))) {
                                    if ("⇲".Equals(ResizeButton.Content)) {
                                        ResizeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                                    }
                                }
                                CardSearchBox.Text = json["data"].ToString();
                                Find(true);
                            }));
                            break;
                        case MODES.fuzzy:
                            ControlGrid.Dispatcher.Invoke(new Action(() => {
                                if ("1".Equals(config.get("autoScroll"))) {
                                    if ("⇲".Equals(ResizeButton.Content)) {
                                        ResizeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                                    }
                                }
                                CardSearchBox.Text = json["data"].ToString();
                                Find(false);
                            }));
                            break;
                        case MODES.issued:
                            ControlGrid.Dispatcher.Invoke(new Action(() => {
                                if ("1".Equals(config.get("autoScroll"))) {
                                    if ("⇲".Equals(ResizeButton.Content)) {
                                        ResizeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                                    }
                                }
                                string data = json["data"].ToString();
                                Card card = JsonSerializer.Deserialize<Card>(data);
                                List<Card> cards = new List<Card>() { };
                                cards.Add(card);
                                UpdateCardList(cards);
                            }));
                            break;
                        default:
                            break;
                    }
                } else {
                    Console.WriteLine("Received unknown mode: {0}", mode);
                }
            }
            catch (Exception ex) {
                Console.WriteLine("ERROR: {0}", ex.Message);
            }
        }
    }
}