using System;
using System.Windows;
using System.Windows.Controls;
using QuanLyKhachSan_PhamTanLoi.Data;
using QuanLyKhachSan_PhamTanLoi.Services;
using QuanLyKhachSan_PhamTanLoi.ViewModels;

namespace QuanLyKhachSan_PhamTanLoi.Views;

public partial class BaoCaoPage : Page
{
    public BaoCaoPage(BaoCaoPageViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
    }
}