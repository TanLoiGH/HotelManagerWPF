using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace QuanLyKhachSan_PhamTanLoi.ViewModels;

public class TienNghiPhongViewModel : INotifyPropertyChanged
{
    private string _tenTrangThai = "";
    private string _maTrangThai = "TNTT01";
    private bool _canBaoTri;

    public string MaTienNghi { get; set; } = "";
    public string TenTienNghi { get; set; } = "";
    public string? MaDanhMuc { get; set; }
    public string TenDanhMuc { get; set; } = "";
    public DateOnly? HanBaoHanh { get; set; }
    public string? MaNcc { get; set; }
    public string TenNCC { get; set; } = "";

    public string HanBaoHanhText => HanBaoHanh.HasValue ? HanBaoHanh.Value.ToString("dd/MM/yyyy") : "—";


    // Các thuộc tính có thể thay đổi trên UI cần gọi OnPropertyChanged
    public string TenTrangThai
    {
        get => _tenTrangThai;
        set
        {
            _tenTrangThai = value;
            OnPropertyChanged();
        }
    }

    public string MaTrangThai
    {
        get => _maTrangThai;
        set
        {
            _maTrangThai = value;
            OnPropertyChanged();
        }
    }

    public bool CanBaoTri
    {
        get => _canBaoTri;
        set
        {
            _canBaoTri = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null!)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}