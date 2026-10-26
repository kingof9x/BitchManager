using System.Windows;
using BitchManager.Engine;
using BitchManager.UI;

namespace BitchManager;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        var app = new Application { ShutdownMode = ShutdownMode.OnMainWindowClose };
        // The engine grabs Application.Current's dispatcher, so it must be created after the Application.
        var engine = new BitchBoyEngine();
        var window = new MainWindow(engine);
        app.Exit += (_, _) => engine.Dispose();
        app.Run(window);
    }
}
