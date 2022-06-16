using System.IO.Pipes;

namespace PlugIn {
    public class PipeServer : IPlugIn {
        private event EventHandler handler;
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

        event EventHandler IPlugIn.PluginMessageEvent {
            add {
                handler+=value ;
            }

            remove {
                handler -= value ;
            }
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
                        SendMessage(temp);
                    }
                }
                catch (IOException e) {
                    Console.WriteLine("ERROR: {0}", e.Message);
                }
            }
        }

        public void Load() {
            if (stopFlag) {
                stopFlag = false;
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
        }

        public void Unload() {
            stopFlag = true;
        }

        public bool IsConfigurable() {
            return false;
        }

        public void ShowSettingPanel() {
            throw new NotImplementedException();
        }
        protected virtual void SendMessage(string message) {
            handler?.Invoke(this, new PluginMessageEventArgs() { Message = message });
        }
    }

}