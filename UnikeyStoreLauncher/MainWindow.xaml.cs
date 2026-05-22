using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Windows.ApplicationModel;
using Windows.Graphics; // Bắt buộc cho StartupTask

namespace UnikeyStoreLauncher
{
    public sealed partial class MainWindow : Window
    {
        private string _targetExecutablePath = string.Empty;

        public MainWindow()
        {
            this.InitializeComponent();

            // Lưu ý: Chúng ta không cần gọi SystemBackdrop = new MicaBackdrop() ở đây nữa
            // vì bạn đã khai báo nó rất chuẩn xác bên trong file XAML rồi.
            this.AppWindow.Resize(new SizeInt32(500, 400));

            // Tự động nhận diện phần cứng ngay khi mở app
            DetectHardwareArchitecture();
        }

        private void DetectHardwareArchitecture()
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            Architecture osArch = RuntimeInformation.OSArchitecture;

            // Ánh xạ kiến trúc phần cứng với thư mục Payload tương ứng
            // Lưu ý: Đảm bảo thư mục ARM của bạn tên là "arm64" (hoặc "arm_64" tùy theo cách bạn chốt ở csproj)
            switch (osArch)
            {
                case Architecture.Arm:
                case Architecture.Arm64:
                    _targetExecutablePath = Path.Combine(basePath, "Payload", "arm_64", "UniKeyNT.exe");
                    ArchitectureStatusText.Text = "ARM64 (Optimized for Snapdragon)";
                    break;
                case Architecture.X64:
                    _targetExecutablePath = Path.Combine(basePath, "Payload", "x64", "UniKeyNT.exe");
                    ArchitectureStatusText.Text = "x64 (Standard 64-bit)";
                    break;
                default:
                    _targetExecutablePath = Path.Combine(basePath, "Payload", "x86", "UniKeyNT.exe");
                    ArchitectureStatusText.Text = "x86 (32-bit Compatibility)";
                    break;
            }
        }

        private async void LaunchButton_Click(object sender, RoutedEventArgs e)
        {
            // 1. Xử lý yêu cầu Khởi động cùng Windows (Startup Task)
            if (StartupCheckBox.IsChecked == true)
            {
                try
                {
                    // Tên TaskId này phải khớp 100% với TaskId khai báo trong Package.appxmanifest
                    StartupTask startupTask = await StartupTask.GetAsync("UnikeyStartupId");

                    if (startupTask != null && startupTask.State == StartupTaskState.Disabled)
                    {
                        await startupTask.RequestEnableAsync();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Lỗi thiết lập Startup Task: {ex.Message}");
                }
            }

            // 2. Chạy file UniKeyNT.exe ngầm
            if (File.Exists(_targetExecutablePath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = _targetExecutablePath,
                        UseShellExecute = true,
                        WorkingDirectory = Path.GetDirectoryName(_targetExecutablePath)
                    });

                    // Đóng Launcher bằng API của WinUI 3
                    Application.Current.Exit();
                }
                catch (Exception ex)
                {
                    ArchitectureStatusText.Text = $"Launch failed: {ex.Message}";
                    ArchitectureStatusText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
                }
            }
            else
            {
                ArchitectureStatusText.Text = $"Lỗi: Không tìm thấy file tại {_targetExecutablePath}";
                ArchitectureStatusText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
            }
        }
    }
}