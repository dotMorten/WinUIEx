using Microsoft.Maui.LifecycleEvents;

namespace WinUIExMauiSample
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
#if WINDOWS
                 .ConfigureLifecycleEvents(events =>
                 {
                     events.AddWindows(wndLifeCycleBuilder =>
                     {
                         wndLifeCycleBuilder.OnWindowCreated(window =>
                         {
                             var manager = WinUIEx.WindowManager.Get(window);
                             manager.PersistenceId = "MainWindowPersistanceId";
                         });
                     });
                 })
#endif
                 ;

            return builder.Build();
        }
    }
}