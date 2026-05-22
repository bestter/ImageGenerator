// AI Image generator. A program to generate image from AI API.
// Copyright (C) 2026  Martin Labelle
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

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
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.Run(new Form1());
    }
}