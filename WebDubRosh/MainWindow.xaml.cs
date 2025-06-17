using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace WebDubRosh;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private string _externalUrl;
    private readonly DispatcherTimer _logFetchTimer;

    public MainWindow()
    {
        InitializeComponent();
        
        // Регистрируем обработчик для получения уведомлений от App 
        if (Application.Current is App app)
        {
            app.ServerStatusChanged += OnServerStatusChanged;
        }
        
        // Создаем таймер для регулярного обновления логов
        _logFetchTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _logFetchTimer.Tick += LogFetchTimer_Tick;
        _logFetchTimer.Start();
        
        AddLogMessage("Запуск сервера...");
    }
    
    private void OnServerStatusChanged(object sender, ServerStatusEventArgs e)
    {
        // Обрабатываем в UI потоке
        Dispatcher.BeginInvoke(() =>
        {
            // Обновляем статус
            ServerStatusText.Text = e.IsRunning ? "Запущен" : "Остановлен";
            StatusIndicator.Fill = e.IsRunning ? Brushes.Green : Brushes.Red;
            
            // Обновляем информацию о внешнем URL
            if (!string.IsNullOrEmpty(e.ExternalUrl))
            {
                _externalUrl = e.ExternalUrl;
                ExternalUrlText.Text = _externalUrl;
                OpenExternalUrlButton.IsEnabled = true;
                CopyExternalUrlButton.IsEnabled = true;
            }
            
            // Добавляем запись в лог
            AddLogMessage($"{DateTime.Now:HH:mm:ss} - {e.Message}");
        });
    }
    
    private void LogFetchTimer_Tick(object sender, EventArgs e)
    {
        if (Application.Current is App app)
        {
            app.GetLatestLogs().ForEach(AddLogMessage);
        }
    }
    
    private void AddLogMessage(string message)
    {
        if (string.IsNullOrEmpty(message)) return;
        
        LogTextBox.AppendText($"{message}\n");
        LogTextBox.ScrollToEnd();
    }
    
    private void OpenLocalUrl_Click(object sender, RoutedEventArgs e)
    {
        OpenUrl("http://localhost:8080");
    }
    
    private void OpenExternalUrl_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_externalUrl))
        {
            OpenUrl(_externalUrl);
        }
    }
    
    private void CopyExternalUrl_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_externalUrl))
        {
            Clipboard.SetText(_externalUrl);
            MessageBox.Show("URL скопирован в буфер обмена", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
    
    private void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url)
            {
                UseShellExecute = true
            });
            AddLogMessage($"Открываю URL в браузере: {url}");
        }
        catch (Exception ex)
        {
            AddLogMessage($"Ошибка при открытии URL: {ex.Message}");
        }
    }
    
    protected override void OnClosed(EventArgs e)
    {
        // Отписываемся от событий
        if (Application.Current is App app)
        {
            app.ServerStatusChanged -= OnServerStatusChanged;
        }
        
        _logFetchTimer.Stop();
        
        base.OnClosed(e);
    }
}