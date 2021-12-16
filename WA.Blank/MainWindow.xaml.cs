using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WA.Blank
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
#if true // todo only development
            var args = Environment.GetCommandLineArgs();
            // include self dll name
            if (args.Length >= 2)
            {
                if (args[1] == "-k")
                {
                    System.Diagnostics.Process.GetCurrentProcess().Kill(); // force kill
                }
                else if (args[1] == "-e")
                {
                    Application.Current.Shutdown();
                }
            }
#endif
        }
    }
}
