using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Core {
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
    public class YGOdb {

        string path;

        Dictionary<string, Card> datas;
        public void reload() {
            datas = new Dictionary<string, Card>();
            using (StreamReader file = File.OpenText(path)) {
                string jsonStr = file.ReadToEnd();
                Dictionary<string, Card> cards = JsonSerializer.Deserialize<Dictionary<string, Card>>(jsonStr);
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
}