using System.Windows;
using PlusPlusPen.Services;
using PlusPlusPen.ViewModels;
using PlusPlusPen.Views;

namespace PlusPlusPen;

public partial class App : Application
{
    private ServiceContainer? _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _services = ServiceContainer.Create();
        DispatcherUnhandledException += (_, args) =>
        {
            _services.LogService.LogError("Beklenmeyen uygulama hatasi.", args.Exception);
            MessageBox.Show("Beklenmeyen bir hata oluştu. Ayrıntılar log dosyasına yazıldı.", "++PEN", MessageBoxButton.OK, MessageBoxImage.Warning);
            args.Handled = true;
        };

        var toolbar = new ToolbarWindow(_services)
        {
            DataContext = new ToolbarViewModel(_services)
        };
        toolbar.ApplySettings(_services.DrawingSessionService.Settings);

        MainWindow = toolbar;
        toolbar.Show();
    }
}
