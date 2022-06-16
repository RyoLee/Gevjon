using Gevjon.PlugIn;
using log4net;
using log4net.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using static Gevjon.Core.YGOdb;

namespace Gevjon.Core {

    public delegate void PlugInEventCallback(object sender = null,PluginMessageEventArgs args = null);
    public class PlugInEventManager {
        static ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private MainWindow mainWindow;
        private Dictionary<PlugInEventType, List<PlugInEventCallback>> eventQueue = new Dictionary<PlugInEventType, List<PlugInEventCallback>>();
        public PlugInEventManager(MainWindow mainWindow) {
            this.mainWindow = mainWindow;
            AddListener(PlugInEventType.SEARCH, new PlugInEventCallback(DoSearch));
            AddListener(PlugInEventType.LOG, new PlugInEventCallback(DoLog));
        }
        public void AddListener(PlugInEventType type, PlugInEventCallback callback) {
            if (!eventQueue.ContainsKey(type)) {
                eventQueue.Add(type, new List<PlugInEventCallback>());
            }
            if (!eventQueue[type].Contains(callback)) {
                eventQueue[type].Add(callback);
            }
        }

        public void RemoveListener(PlugInEventType type, PlugInEventCallback callback) {
            if (eventQueue.ContainsKey(type)) {
                eventQueue[type].Remove(callback);
            }
        }

        public void PostEvent(object sender, PluginMessageEventArgs args) {
            if (eventQueue != null && eventQueue.ContainsKey(args.EventType)) {
                List<PlugInEventCallback> callbacks = eventQueue[args.EventType];
                for (int i = 0; i < callbacks.Count; i++) {
                    callbacks[i](sender,args);
                }
            }
        }

        private void DoSearch(object sender, PluginMessageEventArgs args) {
            try {
                Dictionary<string, object> json = JsonSerializer.Deserialize<Dictionary<string, object>>(args.Datas["data"].ToString());
                string mode = json["mode"].ToString();
                if (Enum.IsDefined(typeof(SEARCH_MODES), mode)) {
                    switch ((SEARCH_MODES)Enum.Parse(typeof(SEARCH_MODES), mode, true)) {
                        case SEARCH_MODES.exact:
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
                        case SEARCH_MODES.fuzzy:
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
                        case SEARCH_MODES.issued:
                            mainWindow.ControlGrid.Dispatcher.Invoke(new Action(() => {
                                if ("1".Equals(mainWindow.config.get("autoScroll"))) {
                                    if ("⇲".Equals(mainWindow.ResizeButton.Content)) {
                                        mainWindow.ResizeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                                    }
                                }
                                string data = json["data"].ToString();
                                Card card = JsonSerializer.Deserialize<Card>(data);
                                List<Card> cards = new List<Card>() { };
                                cards.Add(card);
                                mainWindow.UpdateCardList(cards);
                            }));
                            break;
                        default:
                            break;
                    }
                } else {
                    logger.WarnFormat("Received unknown mode: {0} from: {1} , full:{2}", mode , sender, args.Datas["data"]);
                }
            }
            catch (Exception ex) {
                logger.ErrorFormat("ERROR: {0}", ex.Message);
            }
        }
        private static Level ParseLevel(string level) {
            var loggerRepository = LoggerManager.GetAllRepositories().FirstOrDefault();

            if (loggerRepository == null) {
                throw new Exception("No logging repositories defined");
            }

            var stronglyTypedLevel = loggerRepository.LevelMap[level];

            if (stronglyTypedLevel == null) {
                throw new Exception("Invalid logging level specified");
            }

            return stronglyTypedLevel;
        }
        private void DoLog(object sender, PluginMessageEventArgs args) {
            try {
                Level level = ParseLevel(args.Datas["level"].ToString());
                object log = args.Datas["log"];
                switch (level) {
                    case Level l when l == Level.Debug:
                        logger.DebugFormat ("{0}: {1}",sender,log);
                        break;
                    case Level l when l == Level.Info:
                        logger.InfoFormat("{0}: {1}", sender, log);
                        break;
                    case Level l when l == Level.Warn:
                        logger.WarnFormat("{0}: {1}", sender, log);
                        break;
                    case Level l when l == Level.Fatal:
                        logger.FatalFormat("{0}: {1}", sender, log);
                        break;
                    case Level l when l == Level.Error:
                    default:
                        logger.ErrorFormat("{0}: {1}", sender, log);
                        break;
                }
            }
            catch (Exception ex) {
                logger.ErrorFormat("ERROR: {0}", ex.Message);
            }

        }

    }
}