namespace ImageGeneratorApp;

static class Program
{
    ///// <summary>
    /////  The main entry point for the application.
    ///// </summary>
    //[STAThread]
    //static void Main()
    //{
    //    // To customize application configuration such as set high DPI settings or default font,
    //    // see https://aka.ms/applicationconfiguration.
    //    ApplicationConfiguration.Initialize();
    //    Application.Run(new Form1());
    //}


    [STAThread]
    static void Main()
    {
        // 🛡️ Sentinel: Global exception handling to prevent leaking stack traces
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (sender, args) =>
        {
            // Note: In a production app, log args.Exception securely here (e.g., to a local file or Event Log)
            MessageBox.Show("Une erreur inattendue est survenue dans l'application.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
        };
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            // Note: In a production app, log args.ExceptionObject securely here
            MessageBox.Show("Une erreur critique est survenue. L'application va se fermer.", "Erreur Critique", MessageBoxButtons.OK, MessageBoxIcon.Error);
        };

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new Form1());
    }
}