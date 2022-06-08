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

using System.Windows;

namespace Gevjon {
    public partial class Setting : Window {
        private MainWindow mainWindow;
        public Setting(MainWindow window) {
            this.mainWindow = window;
            Top = mainWindow.Top;
            Left = mainWindow.Left;
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            UpdateCheckBox.IsChecked = "1".Equals(mainWindow.config.get("autoUpdate"));
            PipeServerCheckBox.IsChecked = "1".Equals(mainWindow.config.get("pipeServer"));
            OnTopCheckBox.IsChecked = "1".Equals(mainWindow.config.get("onTop"));
            AutoScrollCheckBox.IsChecked = "1".Equals(mainWindow.config.get("autoScroll"));
            AlphaSlider.Value = float.Parse(mainWindow.config.get("alpha")) * 100;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e) {
            mainWindow.config.set("autoUpdate", UpdateCheckBox.IsChecked == false ? "0" : "1");
            mainWindow.config.set("onTop", OnTopCheckBox.IsChecked == false ? "0" : "1");
            mainWindow.config.set("pipeServer", PipeServerCheckBox.IsChecked == false ? "0" : "1");
            mainWindow.config.set("autoScroll", AutoScrollCheckBox.IsChecked == false ? "0" : "1");
            mainWindow.config.set("alpha", (AlphaSlider.Value / 100).ToString());
            e.Handled = true;
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            this.Hide();
            mainWindow.Show();
            mainWindow.reload();
            mainWindow.Focus();
        }
    }
}
