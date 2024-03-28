using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace SharpGraphics.Apps.NETAvalonia
{
    public partial class ResolutionWindow : Window
    {

        #region Fields

        private TextBox? _xResolutionBox;
        private TextBox? _yResolutionBox;

        #endregion

        #region Properties

        public bool IsAccepted { get; private set; }
        public int ResolutionWidth => int.TryParse(_xResolutionBox?.Text, out int width) ? width : 0;
        public int ResolutionHeight => int.TryParse(_yResolutionBox?.Text, out int height) ? height : 0;

        #endregion

        #region Constructors

        public ResolutionWindow()
        {
            InitializeComponent();
        }

        #endregion

        #region Private Methods

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            _xResolutionBox = this.FindControl<TextBox>("_xResolutionBox");
            _yResolutionBox = this.FindControl<TextBox>("_yResolutionBox");
        }

        #endregion

        #region Control Event Handlers

        private void _acceptButton_Click(object sender, RoutedEventArgs e)
        {
            IsAccepted = int.TryParse(_xResolutionBox?.Text, out _) && int.TryParse(_yResolutionBox?.Text, out _);
            Close();
        }
        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            IsAccepted = false;
            Close();
        }

        #endregion

    }
}
