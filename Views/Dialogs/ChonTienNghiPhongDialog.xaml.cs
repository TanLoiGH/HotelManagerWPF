using Microsoft.EntityFrameworkCore;
using QuanLyKhachSan_PhamTanLoi.Data;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace QuanLyKhachSan_PhamTanLoi.Views;

public class AmenityPickItem : INotifyPropertyChanged
{
    private bool _isChecked;

    public string MaTienNghi { get; set; } = "";
    public string TenTienNghi { get; set; } = "";
    public string TenNCC { get; set; } = "—";

    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked == value) return;
            _isChecked = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}





public partial class ChonTienNghiPhongDialog : Window
{
    private readonly string _maPhong;
    private List<AmenityPickItem> _all = [];
    private ICollectionView? _view;

    public List<string> SelectedMaTienNghi { get; private set; } = [];

    public ChonTienNghiPhongDialog(string maPhong)
    {
        _maPhong = maPhong;
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }


    private async Task LoadAsync()
    {
        using var db = new QuanLyKhachSanContext();

        var assigned = await db.TienNghiPhongs
            .Where(t => t.MaPhong == _maPhong)
            .Select(t => t.MaTienNghi)
            .ToListAsync();

        _all = await db.TienNghis
            .Include(t => t.MaNccNavigation)
            .Where(t => t.IsActive == true)
            .OrderBy(t => t.TenTienNghi)
            .Select(t => new AmenityPickItem
            {
                MaTienNghi = t.MaTienNghi,
                TenTienNghi = t.TenTienNghi,
                TenNCC = t.MaNccNavigation != null ? t.MaNccNavigation.TenNcc : "—",
                IsChecked = assigned.Contains(t.MaTienNghi),
            })
            .ToListAsync();

        ListTienNghi.ItemsSource = _all;
        _view = CollectionViewSource.GetDefaultView(ListTienNghi.ItemsSource);
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        if (_view == null) return;
        var kw = TxtSearch.Text.Trim().ToLower();
        _view.Filter = obj =>
        {
            if (obj is not AmenityPickItem item) return false;
            if (string.IsNullOrEmpty(kw)) return true;
            return item.TenTienNghi.ToLower().Contains(kw)
                   || item.TenNCC.ToLower().Contains(kw)
                   || item.MaTienNghi.ToLower().Contains(kw);
        };
        _view.Refresh();
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

    private void BtnChonHet_Click(object sender, RoutedEventArgs e)
    {
        foreach (var i in _all) i.IsChecked = true;
    }

    private void BtnBoChon_Click(object sender, RoutedEventArgs e)
    {
        foreach (var i in _all) i.IsChecked = false;
    }

    private void BtnHuy_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void BtnXacNhan_Click(object sender, RoutedEventArgs e)
    {
        SelectedMaTienNghi = _all.Where(i => i.IsChecked).Select(i => i.MaTienNghi).ToList();
        DialogResult = true;
        Close();
    }
}

