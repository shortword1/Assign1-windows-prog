using System.Windows.Controls;

namespace Assign1
{
    public partial class NowPlayingControl : UserControl
    {
        public NowPlayingControl()
        {
            InitializeComponent();
        }

        public string Title
        {
            get => textBlockTitle.Text;
            set => textBlockTitle.Text = value;
        }

        public string Artist
        {
            get => textBlockArtist.Text;
            set => textBlockArtist.Text = value;
        }
    }
}
