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
using System.Data.SQLite;
using System.Collections.Generic;
using System.Configuration;
using System.IO.Pipes;
using System.Linq;
using System.Web.Script.Serialization;
using System.Threading.Tasks;
using System.ComponentModel;

namespace Gevjon
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private YGOdb db;
        class YGOdb
        {
            static string cnDbFile = "locales\\zh-CN\\cards.cdb";
            static string enDbFile = "locales\\en-US\\cards.cdb";
            static string jpDbFile = "locales\\ja-JP\\cards.cdb";
            static string connectionStringPrefix = "data source = ";
            public int curSrcIndex = 0;
            SQLiteConnection tarConn;
            SQLiteCommand tarCmd;
            SQLiteConnection srcConn;
            SQLiteCommand srcCmd;
            SQLiteConnection cnConn;
            SQLiteCommand cnCmd;
            SQLiteConnection enConn;
            SQLiteCommand enCmd;
            SQLiteConnection jpConn;
            SQLiteCommand jpCmd;
            public YGOdb()
            {
                cnConn = new SQLiteConnection(connectionStringPrefix + cnDbFile);
                cnConn.Open();
                cnCmd = cnConn.CreateCommand();
                enConn = new SQLiteConnection(connectionStringPrefix + enDbFile);
                enConn.Open();
                enCmd = enConn.CreateCommand();
                jpConn = new SQLiteConnection(connectionStringPrefix + jpDbFile);
                jpConn.Open();
                jpCmd = jpConn.CreateCommand();
                tarConn = cnConn;
                tarCmd = cnCmd;
                //use jp by default
                srcConn = jpConn;
                srcCmd = jpCmd;
            }
            public void ResetSrc(int index)
            {
                switch (index)
                {
                    case 0:
                        srcConn = jpConn;
                        srcCmd = jpCmd;
                        break;
                    case 1:
                        srcConn = enConn;
                        srcCmd = enCmd;
                        break;
                    case 2:
                        srcConn = cnConn;
                        srcCmd = cnCmd;
                        break;
                    default:
                        System.Environment.Exit(1);
                        break;
                }

            }
            public List<Card> FindById(string id, string srcName)
            {
                List<Card> cards = new List<Card>();
                if (null == id || "".Equals(id.Trim()))
                {
                    return cards;
                }
                string queryString = "select id,name,desc from texts where id=@cid";
                tarCmd.CommandText = queryString;
                tarCmd.Parameters.Add(new SQLiteParameter("@cid", id));
                SQLiteDataReader dataReader = tarCmd.ExecuteReader();
                while (dataReader.Read())
                {
                    string cid = dataReader["id"].ToString();
                    string cname = dataReader["name"].ToString();
                    string cdesc = dataReader["desc"].ToString();
                    cards.Add(new Card(cid, srcName == null || "".Equals(srcName.Trim()) ? "" : srcName, cname, cdesc));
                }
                dataReader.Close();
                return cards;

            }
            public List<Card> FindByName(string name)
            {
                List<Card> cards = new List<Card>();
                if (null == name || "".Equals(name.Trim()))
                {
                    return cards;
                }
                string queryString = "select id,name from texts where name  like @cname";
                srcCmd.CommandText = queryString;
                srcCmd.Parameters.Add(new SQLiteParameter("@cname", '%' + name + '%'));
                SQLiteDataReader dataReader = srcCmd.ExecuteReader();
                List<Tuple<string, string>> ids = new List<Tuple<string, string>>();
                while (dataReader.Read())
                {
                    string cid = dataReader["id"].ToString();
                    string cname = dataReader["name"].ToString();
                    ids.Add(new Tuple<string, string>(cid, cname));
                }
                dataReader.Close();
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
            Task task = new Task(() => {
                while (true)
                {
                    StartPipeServer();
                }
            });
            task.Start();
        }
        public delegate void DelegateMessage(string Reply);

        enum MODES
        {
            id, name, issued
        };
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
                                        this.FindByIdButton.Dispatcher.Invoke(new Action(() =>
                                        {
                                            CardIdBox.Text = json["id"].ToString();
                                            CardNameBox.Text = "";
                                            FindByIdButton_Click(null, null);
                                        }));
                                        break;
                                    case MODES.name:
                                        this.FindByNameButton.Dispatcher.Invoke(new Action(() =>
                                        {
                                            CardIdBox.Text = "";
                                            CardNameBox.Text = json["name"].ToString();
                                            FindByNameButton_Click(null, null);
                                        }));
                                        break;
                                    case MODES.issued:
                                        this.SourceComboBox.Dispatcher.Invoke(new Action(() =>
                                        {
                                            string id = json["id"].ToString();
                                            string name = json["name"].ToString();
                                            string desc = json["desc"].ToString();
                                            List<Card> cards = new List<Card>() { };
                                            Card card = new Card(id, "", name, desc);
                                            cards.Add(card);
                                            UpdateCardList(cards);
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
        private static string Reverse(string content)
        {
            char[] charArray = content.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SourceComboBox.SelectedIndex = Int32.Parse(ConfigurationManager.AppSettings["srcDbIndex"] ?? "0");
        }

        private void FindByIdButton_Click(object sender, RoutedEventArgs e)
        {
            CardComboBox.IsEnabled = false;
            CardComboBox.ItemsSource = null;
            CardComboBox.Items.Refresh();
            List<Card> cards = db.FindById(CardIdBox.Text, "");
            UpdateCardList(cards);
        }
        private void FindByNameButton_Click(object sender, RoutedEventArgs e)
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
                CardDescBox.Text = "【" + card.name + "】\n\n" + card.description + "\n\n\nID:[" + card.id + "]";
            }
            else
            {
                CardComboBox.IsEnabled = false;
                CardDescBox.Text = "";
            }
        }
        private void SourceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            if (comboBox.SelectedItem != null)
            {
                db.ResetSrc(comboBox.SelectedIndex);
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                config.AppSettings.Settings["srcDbIndex"].Value = comboBox.SelectedIndex.ToString();
                config.Save(ConfigurationSaveMode.Modified);
            }
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
                FindByIdButton_Click(null, null);
            }
        }

        private void CardNameBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {if (e.Key == System.Windows.Input.Key.Enter)
            {
                FindByNameButton_Click(null, null);
            }

        }

        private void TopmostCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Topmost = true;
        }

        private void TopmostCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Topmost = false;
        }
    }
}
