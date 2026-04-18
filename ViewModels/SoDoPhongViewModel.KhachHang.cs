using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using QuanLyKhachSan_PhamTanLoi.Helpers;
using QuanLyKhachSan_PhamTanLoi.Models;

namespace QuanLyKhachSan_PhamTanLoi.ViewModels;

public partial class SoDoPhongViewModel
{
	#region Biến toàn cục & Trạng thái (Fields & States)

	private CancellationTokenSource? _TimKhachBatDongBo;
	private int _phienTimKhach;
	private bool _isAutoFilling = false; // Cờ chặn hiệu ứng Domino xóa Form
	private bool _isKhachExpanded;

	private string _khachHangSearchText = "";
	private KhachHang? _selectedKhach;
	private ObservableCollection<KhachHang> _khachHangResults = new();

	private string _newKhachHoTen = "";
	private string _newKhachSdt = "";
	private string _newKhachCccd = "";
	private string _newKhachDiaChi = "";
	private string _newKhachPassport = "";
	private string _newKhachVisa = "";
	private string _newKhachQuocTich = "Việt Nam";

	#endregion

	#region Logic Tìm kiếm & Chọn Khách Hàng (Search & Selection)

	public ObservableCollection<KhachHang> KhachHangResults => _khachHangResults;

	private bool _isSearchOpen;
	public bool IsSearchOpen { get => _isSearchOpen; set => SetProperty(ref _isSearchOpen, value); }

	public string KhachHangSearchText
	{
		get => _khachHangSearchText;
		set
		{
			if (SetProperty(ref _khachHangSearchText, value))
			{
				if (_isAutoFilling) return;

				if (SelectedKhach != null)
				{
					SelectedKhach = null;
				}

				IsSearchOpen = true; // Mở ListBox ra khi đang gõ
				BatDauTimKhachHang(value);
				NewKhachHoTen = value;
			}
		}
	}

	public KhachHang? SelectedKhach
	{
		get => _selectedKhach;
		set
		{
			// BỨC TƯỜNG THÉP 2.0: Nếu giá trị mới bằng giá trị cũ, bỏ qua luôn.
			if (_selectedKhach == value) return;

			if (SetProperty(ref _selectedKhach, value))
			{
				CapNhatTienTamTinh();
				OnPropertyChanged(nameof(LoaiKhachHienThi));
				OnPropertyChanged(nameof(IsKhachMoi));
				OnPropertyChanged(nameof(CoHangKhachHang));
				OnPropertyChanged(nameof(ThongTinKhachHeader));

				if (value != null)
				{
					_isAutoFilling = true; // Chặn TextBox Search gọi lại hàm tìm kiếm

					// 1. Ẩn ListBox kết quả (Không dùng lệnh Clear() nữa để tránh Bug Domino)
					IsSearchOpen = false;

					// 2. Gán tên vào TextBox (không gọi hàm Set của SearchText)
					_khachHangSearchText = value.TenKhachHang ?? "";
					OnPropertyChanged(nameof(KhachHangSearchText));

					// 3. Bơm data vào Form
					NewKhachHoTen = value.TenKhachHang ?? "";
					NewKhachSdt = value.DienThoai ?? ""; // Nhớ check lại tên trong DB là DienThoai hay SoDienThoai
					NewKhachCccd = value.Cccd ?? "";
					NewKhachDiaChi = value.DiaChi ?? "";
					NewKhachPassport = value.Passport ?? "";
					NewKhachVisa = value.Visa ?? "";
					NewKhachQuocTich = value.QuocTich ?? "Việt Nam";

					// 4. Mở bung Form
					IsKhachExpanded = true;

					_isAutoFilling = false; // Mở lại cho TextBox Search
				}
				else
				{
					// Nếu WPF tự ép null hoặc nhân viên xóa chọn -> Dọn dẹp form rác
					NewKhachSdt = "";
					NewKhachCccd = "";
					NewKhachDiaChi = "";
					NewKhachPassport = "";
					NewKhachVisa = "";
					NewKhachQuocTich = "Việt Nam";

					IsKhachExpanded = !string.IsNullOrWhiteSpace(KhachHangSearchText);
				}
			}
		}
	}

	#endregion

	#region Các Property trạng thái UI (UI State)

	public string LoaiKhachHienThi => SelectedKhach?.MaLoaiKhachNavigation?.TenLoaiKhach ?? "";
	public bool CoHangKhachHang => !string.IsNullOrWhiteSpace(SelectedKhach?.MaLoaiKhachNavigation?.TenLoaiKhach);
	public bool IsKhachMoi => SelectedKhach == null;
	public string ThongTinKhachHeader => IsKhachMoi ? "➕ Thông tin khách mới" : "👤 Chi tiết khách hàng";

	public bool IsKhachExpanded
	{
		get => _isKhachExpanded;
		set => SetProperty(ref _isKhachExpanded, value);
	}

	public bool IsKhachNuocNgoai => !IsQuocTichVietNam(NewKhachQuocTich);
	public bool IsKhachTrongNuoc => !IsKhachNuocNgoai;

	#endregion

	#region Các Property Form nhập liệu (Form Data)

	public string NewKhachHoTen
	{
		get => _newKhachHoTen;
		set
		{
			if (SetProperty(ref _newKhachHoTen, value))
			{
				// Nếu đã chọn khách cũ, bỏ chọn để chuyển sang chế độ thêm mới
				if (SelectedKhach != null)
					SelectedKhach = null;
			}
		}
	}
	public string NewKhachSdt { get => _newKhachSdt; 
		set => SetProperty(ref _newKhachSdt, value); }
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

	#endregion

	#region Logic Xử lý tìm kiếm Bất đồng bộ (Async Logic)

	private void BatDauTimKhachHang(string tuKhoa)
	{
		_TimKhachBatDongBo?.Cancel();
		_TimKhachBatDongBo = new CancellationTokenSource();
		int phien = Interlocked.Increment(ref _phienTimKhach);
		_ = TimKhachHangAsync(tuKhoa, phien, _TimKhachBatDongBo.Token);
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
			Logger.LogError("Lỗi", ex);
			MessageBox.Show($"Lỗi tìm khách hàng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}

	#endregion

	#region Các hàm hỗ trợ (Helpers)

	private static bool IsQuocTichVietNam(string? quocTich)
	{
		if (string.IsNullOrWhiteSpace(quocTich)) return true;
		var s = quocTich.Trim().ToLowerInvariant();
		return s == "vn" || s.Contains("viet") || s.Contains("việt") || s.Contains("vietnam") ||
			   s.Contains("việt nam") || s.Contains("vietnamese");
	}

	#endregion
}