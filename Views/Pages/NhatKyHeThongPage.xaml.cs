using System.Windows.Controls;
using QuanLyKhachSan_PhamTanLoi.ViewModels;

namespace QuanLyKhachSan_PhamTanLoi.Views.Pages
{
    public partial class NhatKyHeThongPage : Page
    {
        private readonly NhatKyHeThongViewModel _viewModel;

        public NhatKyHeThongPage(NhatKyHeThongViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            this.DataContext = _viewModel; // Gán DataContext để Binding hoạt động
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            _viewModel.Filter(txtSearch.Text);
        }
    }
}