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
using System.Diagnostics;
using System.Windows;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;

namespace Gevjon {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        public static void InitLogger() {
            Hierarchy hierarchy = (Hierarchy)LogManager.GetRepository();
            hierarchy.Root.RemoveAllAppenders();
            hierarchy.Root.Level = Level.All;
            var patternLayout = new PatternLayout {
                ConversionPattern = "%date [%thread] %level %logger - %message%newline"
            };
            patternLayout.ActivateOptions();

            var roller = new RollingFileAppender {
                File = System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName)+"\\log.txt",
                AppendToFile = true,
                ImmediateFlush=true,
                RollingStyle = RollingFileAppender.RollingMode.Size,
                Encoding = System.Text.Encoding.UTF8,
                MaxSizeRollBackups = 3,
                MaximumFileSize = "1MB",
                StaticLogFileName = true,
                Layout = patternLayout,
                LockingModel = new FileAppender.MinimalLock(),
            };
            roller.ActivateOptions();
            hierarchy.Root.AddAppender(roller);
            BasicConfigurator.Configure(hierarchy);
        }
        static App() {
            InitLogger();
        }
        private void Application_Startup(object sender, StartupEventArgs e) {
            Application currApp = Current;
            currApp.StartupUri = new Uri("MainWindow.xaml", UriKind.RelativeOrAbsolute);
        }
    }

}
