using Gevjon.Common;
using Gevjon.PlugIn;
using System.ComponentModel.Composition;
using System.IO;
using System.IO.Pipes;
using System.Text.Json;
using System.Windows;

namespace Gevjon.PipeServer {
    [Export(typeof(IPlugIn))]
    public class PipeServer : IPlugIn {
        private event EventHandler handler;
        private Config config;
        private bool stopFlag = true;
        public string Name => "PipeServer";

        public Version Version {
            get {
                return new Version("1.0.0");
            }
        }

        public Guid Id {
            get {
                return new Guid("c870bda9-3cab-4868-9686-d6e44a5369f4");
            }
        }

        public string Description => "命名管道服务";

        public Config Config { set => config = value; }

        public object DefaultConfig => new { interval = "1000" };

        public Window? SettingPanel => new SettingPanel(config);

        event EventHandler IPlugIn.PluginMessageEvent {
            add {
                handler += value;
            }

            remove {
                handler -= value;
            }
        }

        private void StartPipeServer() {
            using (NamedPipeServerStream pipeServer = new NamedPipeServerStream("GevjonCore", PipeDirection.InOut)) {
                pipeServer.WaitForConnection();
                try {
                    using (StreamReader sr = new StreamReader(pipeServer)) {
                        string temp = "";
                        string line;
                        while ((line = sr.ReadLine()) != null) {
                            temp += line;
                        }
                        Dictionary<string, object> message = new Dictionary<string, object>();
                        PluginMessageEventArgs args = new PluginMessageEventArgs { EventType = PlugInEventType.SEARCH, Datas = new Dictionary<string, object> { { "data", temp } } };
                        SendMessage(args);
                    }
                }
                catch (IOException e) {
                    PluginMessageEventArgs args = new PluginMessageEventArgs { EventType = PlugInEventType.LOG, Datas = new Dictionary<string, object> { { "log", e }, { "level", "Info" } } };
                    SendMessage(args);
                }
            }
        }
        public void Load() {
            if (stopFlag) {
                stopFlag = false;
                Task.Run(() => {
                    while (true) {
                        if (stopFlag) {
                            Thread.Sleep(int.Parse(config.get("interval", "1000")));
                        } else {
                            StartPipeServer();
                        }
                    }
                });
            }
        }

        public void Unload() {
            stopFlag = true;
        }
        protected virtual void SendMessage(PluginMessageEventArgs args) {
            handler?.Invoke(this, args);
        }
    }

}