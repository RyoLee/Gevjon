using Gevjon.Common;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Gevjon.PlugIn {
    public enum PlugInEventType {
        SEARCH, LOG
    }
    public interface IPlugIn {
        // 初始化配置
        Config Config { set; }
        object DefaultConfig { get; }
        // 加载插件
        [MethodImpl(MethodImplOptions.Synchronized)]
        void Load();
        // 卸载插件
        [MethodImpl(MethodImplOptions.Synchronized)]
        void Unload();
        // 插件名
        string Name { get; }
        // 版本信息
        Version Version { get; }
        // 唯一标识
        Guid Id { get; }
        // 描述信息
        string Description { get; }
        // 仅当可配置时需要实现,用于呼出配置页
        Window SettingPanel { get; }
        // 插件通信
        event EventHandler PluginMessageEvent;


    }
    public class PluginMessageEventArgs : EventArgs {
        public PlugInEventType EventType {
            get; set;
        }
        public Dictionary<string,object>? Datas { get; set; }
    }
}