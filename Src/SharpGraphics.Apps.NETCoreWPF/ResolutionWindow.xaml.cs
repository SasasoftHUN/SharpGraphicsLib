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
using System.Windows.Shapes;

namespace SharpGraphics.Apps.NETCoreWPF
{
    /// <summary>
    /// Interaction logic for ResolutionWindow.xaml
    /// </summary>
    public partial class ResolutionWindow : Window
    {

        #region Properties

        public int ResolutionWidth => int.TryParse(_xResolutionBox.Text, out int width) ? width : 0;
        public int ResolutionHeight => int.TryParse(_yResolutionBox.Text, out int height) ? height : 0;

        #endregion

        #region Constructors

        public ResolutionWindow()
        {
            InitializeComponent();
        }

        #endregion

        #region Control Event Handlers

        private void _acceptButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = int.TryParse(_xResolutionBox.Text, out _) && int.TryParse(_yResolutionBox.Text, out _);
            Close();
        }
        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        #endregion

    }
}
