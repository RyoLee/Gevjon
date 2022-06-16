using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Core {
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
        public Config(string path) {
            this.path = path;
            this.datas = init();
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
        private Dictionary<string, string> init() {
            Dictionary<string, string> res = new Dictionary<string, string>();
            var defaultCfg = new {
                version = "1.0.0",
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