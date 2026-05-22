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

using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageGeneratorApp
{
    public partial class Form1 : Form
    {
        private PictureBox pictureBox = null!;
        private Label lblKey = null!;
        private TextBox txtApiKey = null!;
        private TextBox txtPrompt = null!;
        private ComboBox cmbModel = null!;
        private ComboBox cmbResolution = null!;
        private ComboBox cmbAspectRatio = null!;
        private Button btnGenerate = null!;
        private Button btnSave = null!;
        private Button btnClear = null!;
        private Label lblStatus = null!;
        private CheckBox chkMultiTurnEditing = null!;
        private string? currentBase64Image = null;
        private List<string> selectedImages = new List<string>();
        private Button btnAddImages = null!;
        private const long MaxFileSizeBytes = 20 * 1024 * 1024; // 20 MB

        // ⚡ Bolt Optimization: Use a shared HttpClient instance for the lifetime of the application
        // This avoids socket exhaustion (TIME_WAIT state) and eliminates TCP/TLS handshake latency on subsequent requests
        // 🛡️ Sentinel: Add timeout to prevent hanging indefinitely if external API is unresponsive
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        private readonly ImageGeneratorClient _imageClient = new ImageGeneratorClient(_httpClient);

        public Form1()
        {
            InitializeControls();
        }

        private void InitializeControls()
        {
            this.Text = "Générateur d'image Grok Imagine et Nano Banana Pro";
            this.ClientSize = new Size(900, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;

            // API Key
            lblKey = new Label 
            { 
                Text = "Clé API xAI :", 
                Location = new Point(20, 20), 
                AutoSize = true, 
                ForeColor = Color.FromArgb(220, 76, 30) 
            };
            txtApiKey = new TextBox { Location = new Point(190, 17), Width = 580, PasswordChar = '•', Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

            // Prompt
            var lblPrompt = new Label { Text = "Prompt :", Location = new Point(20, 60), AutoSize = true };
            txtPrompt = new TextBox { Location = new Point(190, 57), Width = 580, Height = 100, Multiline = true, ScrollBars = ScrollBars.Vertical, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, MaxLength = 4000 };

            // Modèle
            var lblModel = new Label { Text = "Modèle :", Location = new Point(20, 175), AutoSize = true };
            cmbModel = new ComboBox { Location = new Point(190, 172), Width = 230, DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Top | AnchorStyles.Left };
            cmbModel.Items.AddRange(new[] { "grok-imagine-image", "grok-imagine-image-pro", "nano-banana-pro" });
            cmbModel.SelectedIndex = 0;
            cmbModel.SelectedIndexChanged += CmbModel_SelectedIndexChanged;

            // Résolution (haute dispo)
            var lblRes = new Label { Text = "Résolution :", Location = new Point(440, 175), AutoSize = true };
            cmbResolution = new ComboBox { Location = new Point(520, 172), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Top | AnchorStyles.Left };
            cmbResolution.Items.AddRange(new[] { "1k", "2k" });
            cmbResolution.SelectedIndex = 1; // 2k par défaut (haute résolution)

            // Images
            btnAddImages = new Button { Text = "Ajouter images (0/3)", Location = new Point(690, 171), Width = 150, Height = 25, Anchor = AnchorStyles.Top | AnchorStyles.Left };
            btnAddImages.Click += BtnAddImages_Click;

            // Aspect Ratio
            var lblRatio = new Label { Text = "Aspect Ratio :", Location = new Point(20, 220), AutoSize = true };
            cmbAspectRatio = new ComboBox { Location = new Point(190, 217), Width = 210, DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Top | AnchorStyles.Left };
            cmbAspectRatio.Items.AddRange(new[] { "1:1 (Médias sociaux)", "16:9 (Widescreen)", "9:16 (Stories/Reels)", "4:3 (Standard)", "3:2 (Photographie)", "20:9 (Panoramique cellulaire)" });
            cmbAspectRatio.SelectedIndex = 1; // 16:9 par défaut

            // Multi-turn editing
            chkMultiTurnEditing = new CheckBox
            {
                Text = "Éditer l'image actuelle (Multi-turn)",
                Location = new Point(440, 220),
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };

            // Boutons
            btnGenerate = new Button { Text = "Générer l'image", Location = new Point(190, 260), Width = 160, Height = 40, Anchor = AnchorStyles.Top | AnchorStyles.Left };
            btnGenerate.Click += BtnGenerate_Click;

            btnSave = new Button { Text = "📥 Enregistrer l'image (haute rés.)", Location = new Point(370, 260), Width = 250, Height = 40, Enabled = false, Anchor = AnchorStyles.Top | AnchorStyles.Left };
            btnSave.Click += BtnSave_Click;

            btnClear = new Button { Text = "Effacer", Location = new Point(640, 260), Width = 100, Height = 40, Anchor = AnchorStyles.Top | AnchorStyles.Left };
            btnClear.Click += (s, e) => ClearForm();

            // Status
            lblStatus = new Label { Location = new Point(20, 310), Width = 750, Height = 30, ForeColor = Color.DarkBlue, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

            // PictureBox
            pictureBox = new PictureBox
            {
                Location = new Point(20, 340),
                Size = new Size(840, 320),
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.Black,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            this.Controls.AddRange(new Control[] { lblKey, txtApiKey, lblPrompt, txtPrompt, lblModel, cmbModel, lblRes, cmbResolution, btnAddImages, lblRatio, cmbAspectRatio, chkMultiTurnEditing,
                btnGenerate, btnSave, btnClear, lblStatus, pictureBox });

            this.Resize += Form1_Resize;
        }

        private async void BtnGenerate_Click(object? sender, EventArgs e)
        {
            string apiKey = txtApiKey.Text?.Trim() ?? string.Empty;


            string? imageToEditBase64 = null;
            if (chkMultiTurnEditing.Checked && !string.IsNullOrEmpty(currentBase64Image))
            {
                imageToEditBase64 = currentBase64Image;
            }

            Image? previousImage = pictureBox.Image;
            string? previousBase64Image = currentBase64Image;

            btnGenerate.Enabled = false;
            btnSave.Enabled = false;
            lblStatus.Text = "⏳ Génération en cours...";
            pictureBox.Image = null;
            currentBase64Image = null;

            try
            {
                string selectedRatioText = cmbAspectRatio.SelectedItem?.ToString() ?? "16:9";
                string aspectRatioValue = selectedRatioText.Split(' ')[0];
                string opaqueUserId = UserIdHelper.GetOpaqueUserId();

                var imagesList = new List<object>();

                if (selectedImages.Count > 0 || !string.IsNullOrEmpty(imageToEditBase64))
                {
                    if (!string.IsNullOrEmpty(imageToEditBase64))
                    {
                        imagesList.Add(new { type = "image_url", url = $"data:image/png;base64,{imageToEditBase64}" });
                    }

                    var tasks = selectedImages.Select(async imgPath =>
                    {
                        var ext = Path.GetExtension(imgPath).ToLower().TrimStart('.');
                        if (ext == "jpg") ext = "jpeg";

                        byte[] b64Bytes;
                        using (var fs = new FileStream(imgPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
                        {
                            // 🛡️ Sentinel: Prevent TOCTOU race condition by checking length on the opened handle
                            if (fs.Length > MaxFileSizeBytes)
                            {
                                lblStatus.Text = $"❌ Image trop grande : {Path.GetFileName(imgPath)}";
                                MessageBox.Show($"L'image '{Path.GetFileName(imgPath)}' dépasse la limite de 20 Mo.", "Fichier trop volumineux", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return null;
                            }

                            // ⚡ Bolt Optimization: Pre-allocate and read directly to avoid MemoryStream chunking
                            b64Bytes = new byte[(int)fs.Length];
                            await fs.ReadExactlyAsync(b64Bytes, 0, b64Bytes.Length);
                        }

                        // ⚡ Bolt Optimization: Use string.Create to build the data URI directly into a pre-allocated string.
                        // This eliminates the intermediate base64 string allocation (~26MB chars for a 20MB file),
                        // significantly reducing Large Object Heap (LOH) fragmentation and memory pressure.
                        string prefix = $"data:image/{ext};base64,";
                        int b64Length = ((b64Bytes.Length + 2) / 3) * 4;
                        string url = string.Create(prefix.Length + b64Length, (prefix, b64Bytes), (span, state) =>
                        {
                            state.prefix.AsSpan().CopyTo(span);
                            Convert.TryToBase64Chars(state.b64Bytes, span.Slice(state.prefix.Length), out _);
                        });

                        return (object?)new { type = "image_url", url = url };
                    }).ToArray();

                    var results = await Task.WhenAll(tasks);
                    foreach (var res in results)
                    {
                        if (res != null) imagesList.Add(res);
                    }
                }

                currentBase64Image = await _imageClient.GenerateImageAsync(
                    apiKey,
                    txtPrompt.Text.Trim(),
                    cmbModel.Text,
                    cmbResolution.Text,
                    aspectRatioValue,
                    opaqueUserId,
                    imagesList);

                var b64 = currentBase64Image;

                // Affichage de l'image
                var imageBytes = Convert.FromBase64String(b64);
                var ms = new MemoryStream(imageBytes);
                pictureBox.Image = Image.FromStream(ms);

                lblStatus.Text = $"✅ Image générée avec {cmbModel.Text} ({cmbResolution.Text})";
                btnSave.Enabled = true;
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message, "Erreur de validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (ImageGeneratorException ex)
            {
                lblStatus.Text = $"❌ Erreur {ex.StatusCode}";
                MessageBox.Show($"Erreur API :\n{ex.Message}", "Erreur API", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (TaskCanceledException)
            {
                lblStatus.Text = "❌ Délai d'attente dépassé";
                MessageBox.Show("La requête a mis trop de temps à répondre. Veuillez réessayer plus tard.", "Erreur de délai d'attente", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception)
            {
                lblStatus.Text = "❌ Erreur inattendue";
                MessageBox.Show("Une erreur inattendue est survenue lors de la génération.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnGenerate.Enabled = true;
                if (currentBase64Image == null && previousBase64Image != null)
                {
                    currentBase64Image = previousBase64Image;
                    pictureBox.Image = previousImage;
                    btnSave.Enabled = true;
                }
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (currentBase64Image == null) return;

            using var sfd = new SaveFileDialog
            {
                Filter = "PNG Image|*.png",
                Title = "Enregistrer l'image",
                FileName = $"image-{DateTime.Now:yyyyMMdd-HHmmss}.png"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var imageBytes = Convert.FromBase64String(currentBase64Image);
                    File.WriteAllBytes(sfd.FileName, imageBytes);
                    lblStatus.Text = "💾 Image sauvegardée avec succès.";
                    MessageBox.Show("Image enregistrée avec succès !", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception)
                {
                    lblStatus.Text = "❌ Erreur de sauvegarde";
                    MessageBox.Show("Une erreur est survenue lors de la sauvegarde de l'image.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ClearForm()
        {
            txtPrompt.Clear();
            pictureBox.Image = null;
            currentBase64Image = null;
            btnSave.Enabled = false;
            lblStatus.Text = "";
            selectedImages.Clear();
            UpdateImageButtonText();
        }

        private void BtnAddImages_Click(object? sender, EventArgs e)
        {
            try
            {
                using var ofd = new OpenFileDialog
                {
                    Filter = "Images|*.jpg;*.jpeg;*.png;*.webp",
                    Multiselect = true,
                    Title = "Sélectionner des images (max 3)"
                };

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    foreach (var file in ofd.FileNames)
                    {
                        try
                        {
                            // 🛡️ Sentinel: Prevent TOCTOU race condition by keeping the file handle open during check
                            using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                if (fs.Length > MaxFileSizeBytes)
                                {
                                    MessageBox.Show($"L'image '{Path.GetFileName(file)}' dépasse la limite de 20 Mo et ne sera pas ajoutée.", "Fichier trop volumineux", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    continue;
                                }
                            }
                        }
                        catch (Exception)
                        {
                            MessageBox.Show($"Impossible d'ouvrir l'image '{Path.GetFileName(file)}'.", "Erreur de lecture", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            continue;
                        }

                        if (!selectedImages.Contains(file) && selectedImages.Count < 3)
                        {
                            selectedImages.Add(file);
                        }
                    }
                    UpdateImageButtonText();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Une erreur est survenue lors de la sélection des images.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateImageButtonText()
        {
            if (btnAddImages != null)
                btnAddImages.Text = $"Ajouter images ({selectedImages.Count}/3)";
        }


        private void Form1_Resize(object? sender, EventArgs e)
        {
            // PictureBox size is automatically adjusted via Anchor
            // If additional custom adjustments are needed, add here
        }

        private void CmbModel_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cmbModel.SelectedItem?.ToString() == "nano-banana-pro")
            {
                lblKey.Text = "Clé Google Cloud :";
                lblKey.ForeColor = Color.FromArgb(26, 115, 232); // Google Blue
            }
            else
            {
                lblKey.Text = "Clé API xAI :";
                lblKey.ForeColor = Color.FromArgb(220, 76, 30); // xAI Orange-Red
            }
        }

        //[STAThread]
        //static void Main()
        //{
        //    Application.EnableVisualStyles();
        //    Application.SetCompatibleTextRenderingDefault(false);
        //    Application.Run(new Form1());
        //}
    }
}
