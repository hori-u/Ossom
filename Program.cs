using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace Ossom
{
    internal class Program
    {
        // エントリポイント
        public static void Main(string[] args)
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        // Avaloniaアプリケーションの設定
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                         .UsePlatformDetect()
                         .LogToTrace();
    }
}
