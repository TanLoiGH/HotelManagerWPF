using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using QuanLyKhachSan_PhamTanLoi.Models;

namespace QuanLyKhachSan_PhamTanLoi.ViewModels;

public partial class SoDoPhongViewModel
{
	private CancellationTokenSource? _ctsTimKhach;
	private int _phienTimKhach;

	private string _khachHangSearchText = "";
	private KhachHang? _selectedKhach;
	private ObservableCollection<KhachHang> _khachHangResults = new();

	//TÌM KHÁCH H
	public string KhachHangSearchText
	{
		get => _khachHangSearchText;
		set {
			if (SetProperty(ref _khachHangSearchText, value))
			{
				BatDauTimKhachHang(value);
				// Tự động mớm sẵn từ khóa tìm kiếm xuống ô Họ Tên Khách Mới
				NewKhachHoTen = value;
			}
		}
	}

	public KhachHang? SelectedKhach
	{
		get => _selectedKhach;
		set { if (SetProperty(ref _selectedKhach, value)) CapNhatTienTamTinh(); }
	}
	public ObservableCollection<KhachHang> KhachHangResults => _khachHangResults;

	private string _newKhachHoTen = "";
	private string _newKhachSdt = "";
	private string _newKhachCccd = "";
	private string _newKhachDiaChi = "";
	private string _newKhachPassport = "";
	private string _newKhachVisa = "";
	private string _newKhachQuocTich = "";

	public string NewKhachHoTen { get => _newKhachHoTen; set => SetProperty(ref _newKhachHoTen, value); }
	public string NewKhachSdt { get => _newKhachSdt; set => SetProperty(ref _newKhachSdt, value); }
	public string NewKhachCccd { get => _newKhachCccd; set => SetProperty(ref _newKhachCccd, value); }
	public string NewKhachDiaChi { get => _newKhachDiaChi; set => SetProperty(ref _newKhachDiaChi, value); }
	public string NewKhachPassport { get => _newKhachPassport; set => SetProperty(ref _newKhachPassport, value); }
	public string NewKhachVisa { get => _newKhachVisa; set => SetProperty(ref _newKhachVisa, value); }

	public string NewKhachQuocTich
	{
		get => _newKhachQuocTich;
		set
		{
			if (SetProperty(ref _newKhachQuocTich, value))
			{
				OnPropertyChanged(nameof(IsKhachNuocNgoai));
				OnPropertyChanged(nameof(IsKhachTrongNuoc));

				if (!IsKhachNuocNgoai)
				{
					NewKhachPassport = "";
					NewKhachVisa = "";
				}
				else
				{
					NewKhachCccd = "";
				}
			}
		}
	}

	public bool IsKhachNuocNgoai => !IsQuocTichVietNam(NewKhachQuocTich);
	public bool IsKhachTrongNuoc => !IsKhachNuocNgoai;

	private static bool IsQuocTichVietNam(string? quocTich)
	{
		if (string.IsNullOrWhiteSpace(quocTich)) return true;
		var s = quocTich.Trim().ToLowerInvariant();
		return s == "vn" || s.Contains("viet") || s.Contains("việt") || s.Contains("vietnam") || s.Contains("việt nam") || s.Contains("vietnamese");
	}

	private void BatDauTimKhachHang(string tuKhoa)
	{
		_ctsTimKhach?.Cancel();
		_ctsTimKhach = new CancellationTokenSource();
		int phien = Interlocked.Increment(ref _phienTimKhach);
		_ = TimKhachHangAsync(tuKhoa, phien, _ctsTimKhach.Token);
	}

	private async Task TimKhachHangAsync(string tuKhoa, int phien, CancellationToken token)
	{
		if (tuKhoa.Length < 2)
		{
			_khachHangResults.Clear();
			return;
		}
		try
		{
			await Task.Delay(250, token);
			if (token.IsCancellationRequested || phien != _phienTimKhach) return;

			await _khoaKhachHang.WaitAsync(token);
			try
			{
				var ketQua = await _khachHangService.SearchKhachHangAsync(tuKhoa);
				if (token.IsCancellationRequested || phien != _phienTimKhach) return;

				_khachHangResults.Clear();
				foreach (var khach in ketQua) _khachHangResults.Add(khach);
			}
			finally
			{
				_khoaKhachHang.Release();
			}
		}
		catch (OperationCanceledException) { /* ignore */ }
		catch (Exception ex)
		{
			MessageBox.Show($"Lỗi tìm khách hàng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}
}