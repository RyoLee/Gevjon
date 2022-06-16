using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Gevjon.Common {
    public class Config {
        private string path;
        private volatile Dictionary<string, string> datas;
        public string get(string k, string d) {
            if (datas.ContainsKey(k)) {
                return datas[k];
            } else {
                set(k, d);
                return d;
            }
        }
        public string get(string k) {
            load();
            return datas[k];
        }
        public void set(string k, string v) {
            datas[k] = v;
            save();
        }
        public Config(string path, object defaultCfg) {
            this.path = path;
            this.datas = new Dictionary<string, string>();
            if (defaultCfg != null) {
                foreach (var prop in defaultCfg.GetType().GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)) {
                    datas[prop.Name] = prop.GetValue(defaultCfg, null).ToString();
                }
            }
            load();
            save();
        }
        private void load() {
            if (File.Exists(path)) {
                using (StreamReader file = File.OpenText(path)) {
                    string jsonStr = file.ReadToEnd();
                    var _datas = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonStr);
                    foreach (var data in _datas) {
                        datas[data.Key] = data.Value;
                    }
                }
            }
        }
        private void save() {
            string jsonStr = FormatOutput(JsonSerializer.Serialize(datas));
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
                        } else if (!char.IsWhiteSpace(character)) {
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
}