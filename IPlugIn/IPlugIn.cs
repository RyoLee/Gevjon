using System;
using System.Runtime.CompilerServices;

namespace PlugIn {
    public interface IPlugIn {
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
        // 是否可配置
        bool IsConfigurable();
        // 仅当可配置时需要实现,用于呼出配置页
        void ShowSettingPanel();
        // 插件通信
        event EventHandler PluginMessageEvent;


    }
    public class PluginMessageEventArgs : EventArgs {
        public string Message { get; set; }
    }
}