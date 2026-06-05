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

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace ImageGeneratorApp
{
    /// <summary>
    /// Professional About dialog for the image generator application.
    /// Displays application name, version, copyright, and the official French GPL v3 notice.
    /// Provides a button to open the full LICENSE.txt file located next to the executable.
    /// Designed code-first (no Designer.cs) following project conventions.
    /// </summary>
    public class AboutForm : Form
    {
        private Button btnShowLicense = null!;
        private Button btnOk = null!;

        private const string GplNotice = @"Ce programme est un logiciel libre : vous pouvez le redistribuer ou le modifier
selon les termes de la Licence Publique Générale GNU telle que publiée par
la Free Software Foundation, soit la version 3 de la Licence, soit
(à votre choix) toute version ultérieure.

Ce programme est distribué dans l'espoir qu'il sera utile,
mais SANS AUCUNE GARANTIE ; sans même la garantie implicite de
COMMERCIALISABILITÉ ou d'ADÉQUATION À UN OBJECTIF PARTICULIER. Voir la
Licence Publique Générale GNU pour plus de détails.

Vous devriez avoir reçu une copie de la Licence Publique Générale GNU
avec ce programme (fichier LICENSE.txt). Sinon, consultez :
https://www.gnu.org/licenses/";

        /// <summary>
        /// Initializes a new instance of the <see cref="AboutForm"/> class.
        /// </summary>
        public AboutForm()
        {
            InitializeControls();
        }

        /// <summary>
        /// Programmatically initializes all controls and layout for the fixed About dialog.
        /// Uses manual positioning with consistent margins for a clean, professional appearance.
        /// </summary>
        private void InitializeControls()
        {
            this.Text = "À propos de Générateur d'image";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(520, 400);
            this.Padding = new Padding(0);

            // Retrieve version dynamically from assembly metadata to stay in sync with csproj
            string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.1.0";

            // Application name (prominent)
            var appNameLabel = new Label
            {
                Text = "Générateur d'image",
                Location = new Point(20, 18),
                AutoSize = true,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 30, 30)
            };

            // Version
            var versionLabel = new Label
            {
                Text = $"Version {version}",
                Location = new Point(20, 50),
                AutoSize = true,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular)
            };

            // Copyright
            var copyrightLabel = new Label
            {
                Text = "© 2026 Martin Labelle",
                Location = new Point(20, 74),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(80, 80, 80)
            };

            // License notice title
            var licenseTitleLabel = new Label
            {
                Text = "Avis de licence :",
                Location = new Point(20, 104),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            // Read-only multi-line TextBox containing the exact official French GPL v3 notice
            var licenseTextBox = new TextBox
            {
                Text = GplNotice,
                Location = new Point(20, 126),
                Size = new Size(470, 170),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                BackColor = SystemColors.Window,
                BorderStyle = BorderStyle.FixedSingle,
                WordWrap = true
            };

            // Prominent button to open the full LICENSE.txt from the application folder
            btnShowLicense = new Button
            {
                Text = "Afficher la licence complète (GPL v3)",
                Location = new Point(20, 312),
                Size = new Size(280, 32),
                UseVisualStyleBackColor = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular)
            };
            btnShowLicense.Click += BtnShowLicense_Click;

            // Standard OK button (AcceptButton for Enter key and dialog result)
            btnOk = new Button
            {
                Text = "OK",
                Location = new Point(400, 312),
                Size = new Size(90, 32),
                UseVisualStyleBackColor = true,
                DialogResult = DialogResult.OK
            };

            this.AcceptButton = btnOk;
            this.Controls.AddRange(new Control[]
            {
                appNameLabel,
                versionLabel,
                copyrightLabel,
                licenseTitleLabel,
                licenseTextBox,
                btnShowLicense,
                btnOk
            });
        }

        /// <summary>
        /// Opens the LICENSE.txt file located in the same directory as the running executable.
        /// Uses explicitly notepad.exe to prevent command injection via UseShellExecute.
        /// Gracefully handles the case where the file is missing.
        /// </summary>
        private void BtnShowLicense_Click(object? sender, EventArgs e)
        {
            string licensePath = Path.Combine(AppContext.BaseDirectory, "LICENSE.txt");

            try
            {
                if (!File.Exists(licensePath))
                {
                    MessageBox.Show(
                        $"Le fichier de licence LICENSE.txt est introuvable dans le dossier de l'application.\n\nChemin attendu :\n{licensePath}",
                        "Licence introuvable",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = "notepad.exe",
                    UseShellExecute = false
                };
                startInfo.ArgumentList.Add(licensePath);

                Process.Start(startInfo);
            }
            catch (Exception)
            {
                // 🛡️ Sentinel: Present a generic error message and avoid leaking system paths or raw exceptions.
                MessageBox.Show(
                    "Impossible d'ouvrir le fichier de licence en raison d'une erreur système inattendue.",
                    "Erreur d'ouverture",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
