using System.Windows;
using PlusPlusPen.Updater.Models;
using PlusPlusPen.Updater.Services;

namespace PlusPlusPen.Updater;

public partial class MainWindow : Window
{
    private readonly UpdaterLogService _logService = new();
    private readonly UpdaterService _updaterService;

    public MainWindow()
    {
        InitializeComponent();
        _updaterService = new UpdaterService(_logService);
        Loaded += HandleLoaded;
    }

    private async void HandleLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var options = UpdaterOptions.Parse(Environment.GetCommandLineArgs().Skip(1).ToArray());
            await _updaterService.RunAsync(options, UpdateStatus);
            await Task.Delay(900);
            Close();
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            _logService.Error("Güncelleştirme Merkezi kapanmadan önce hata verdi.", ex);
            UpdateStatus("Güncelleme başarısız oldu.");
            DetailTextBlock.Text = ex.Message;
            ProgressBar.IsIndeterminate = false;
            ProgressBar.Value = 100;
            MessageBox.Show(
                $"Güncelleme tamamlanamadı.\n\n{ex.Message}\n\nAyrıntılar: {_logService.LogPath}",
                "++PEN Güncelleştirme Merkezi",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void UpdateStatus(string status)
    {
        Dispatcher.Invoke(() =>
        {
            StatusTextBlock.Text = status;
            DetailTextBlock.Text = $"Log: {_logService.LogPath}";
        });
    }
}
