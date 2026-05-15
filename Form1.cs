using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GrokImagineApp
{
    public partial class Form1 : Form
    {
        private PictureBox pictureBox = null!;
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
        private static readonly HttpClient _httpClient = new HttpClient();

        public Form1()
        {
            InitializeControls();
        }

        private void InitializeControls()
        {
            this.Text = "Grok Imagine - Générateur d'images xAI";
            this.ClientSize = new Size(900, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;

            // API Key
            var lblKey = new Label { Text = "Clé API xAI :", Location = new Point(20, 20), AutoSize = true };
            txtApiKey = new TextBox { Location = new Point(120, 17), Width = 650, PasswordChar = '•', Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

            // Prompt
            var lblPrompt = new Label { Text = "Prompt :", Location = new Point(20, 60), AutoSize = true };
            txtPrompt = new TextBox { Location = new Point(120, 57), Width = 650, Height = 100, Multiline = true, ScrollBars = ScrollBars.Vertical, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, MaxLength = 4000 };

            // Modèle
            var lblModel = new Label { Text = "Modèle :", Location = new Point(20, 175), AutoSize = true };
            cmbModel = new ComboBox { Location = new Point(120, 172), Width = 300, DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Top | AnchorStyles.Left };
            cmbModel.Items.AddRange(new[] { "grok-imagine-image", "grok-imagine-image-pro" });
            cmbModel.SelectedIndex = 0;

            // Résolution (haute dispo)
            var lblRes = new Label { Text = "Résolution :", Location = new Point(440, 175), AutoSize = true };
            cmbResolution = new ComboBox { Location = new Point(520, 172), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Top | AnchorStyles.Left };
            cmbResolution.Items.AddRange(new[] { "1k", "2k" });
            cmbResolution.SelectedIndex = 1; // 2k par défaut (haute résolution)

            // Images
            btnAddImages = new Button { Text = "Ajouter images (0/5)", Location = new Point(690, 171), Width = 150, Height = 25, Anchor = AnchorStyles.Top | AnchorStyles.Left };
            btnAddImages.Click += BtnAddImages_Click;

            // Aspect Ratio
            var lblRatio = new Label { Text = "Aspect Ratio :", Location = new Point(20, 220), AutoSize = true };
            cmbAspectRatio = new ComboBox { Location = new Point(120, 217), Width = 250, DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Top | AnchorStyles.Left };
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
            btnGenerate = new Button { Text = "Générer l'image", Location = new Point(120, 260), Width = 200, Height = 40, Anchor = AnchorStyles.Top | AnchorStyles.Left };
            btnGenerate.Click += BtnGenerate_Click;

            btnSave = new Button { Text = "📥 Enregistrer l'image (haute rés.)", Location = new Point(340, 260), Width = 250, Height = 40, Enabled = false, Anchor = AnchorStyles.Top | AnchorStyles.Left };
            btnSave.Click += BtnSave_Click;

            btnClear = new Button { Text = "Effacer", Location = new Point(610, 260), Width = 100, Height = 40, Anchor = AnchorStyles.Top | AnchorStyles.Left };
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
            if (string.IsNullOrWhiteSpace(txtApiKey.Text))
            {
                MessageBox.Show("Entre ta clé API xAI d'abord !", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(txtPrompt.Text))
            {
                MessageBox.Show("Écris un prompt !", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

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
                object requestBody;
                string apiUrl;

                string selectedRatioText = cmbAspectRatio.SelectedItem?.ToString() ?? "16:9";
                string aspectRatioValue = selectedRatioText.Split(' ')[0];

                if (selectedImages.Count > 0 || !string.IsNullOrEmpty(imageToEditBase64))
                {
                    apiUrl = "https://api.x.ai/v1/images/edits";
                    var imagesList = new List<object>();

                    if (!string.IsNullOrEmpty(imageToEditBase64))
                    {
                        imagesList.Add(new { type = "image_url", url = $"data:image/png;base64,{imageToEditBase64}" });
                    }

                    var tasks = selectedImages.Select(async imgPath =>
                    {
                        var ext = Path.GetExtension(imgPath).ToLower().TrimStart('.');
                        if (ext == "jpg") ext = "jpeg";

                        string b64Data;
                        using (var stream = new FileStream(imgPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            if (stream.Length > MaxFileSizeBytes)
                            {
                                lblStatus.Text = $"❌ Image trop grande : {Path.GetFileName(imgPath)}";
                                MessageBox.Show($"L'image '{Path.GetFileName(imgPath)}' dépasse la limite de 20 Mo.", "Fichier trop volumineux", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return null;
                            }

                            var extIn = Path.GetExtension(imgPath).ToLower().TrimStart('.');
                            if (extIn == "jpg") extIn = "jpeg";

                            byte[] b64Bytes;
                            using (var streamIn = new FileStream(imgPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                using (var memoryStream = new MemoryStream())
                                {
                                    byte[] buffer = new byte[81920];
                                    int bytesRead;
                                    long totalRead = 0;
                                    while ((bytesRead = await streamIn.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                    {
                                        totalRead += bytesRead;
                                        if (totalRead > MaxFileSizeBytes)
                                        {
                                            // Update UI safely inside the async Task, though WinForms requires Invoke
                                            // if not on UI thread. This Select is running on UI thread since it's
                                            // awaited inside BtnGenerate_Click.
                                            lblStatus.Text = $"❌ Image trop grande : {Path.GetFileName(imgPath)}";
                                            MessageBox.Show($"L'image '{Path.GetFileName(imgPath)}' dépasse la limite de 20 Mo.", "Fichier trop volumineux", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            return null;
                                        }
                                        memoryStream.Write(buffer, 0, bytesRead);
                                    }
                                    b64Bytes = memoryStream.ToArray();
                                }
                            }
                            b64Data = Convert.ToBase64String(b64Bytes);
                            return (object?)new { type = "image_url", url = $"data:image/{ext};base64,{b64Data}" };
                        }
                    });
                    var completedTasks = await Task.WhenAll(tasks);
                    imagesList.AddRange(completedTasks.Where(t => t != null)!);

                    if (imagesList.Count == 1)
                    {
                        requestBody = new
                        {
                            model = cmbModel.Text,
                            prompt = txtPrompt.Text.Trim(),
                            image = imagesList[0],
                            n = 1,
                            resolution = cmbResolution.Text,
                            user = GetOpaqueUserId(),
                            response_format = "b64_json"
                        };
                    }
                    else
                    {
                        requestBody = new
                        {
                            model = cmbModel.Text,
                            prompt = txtPrompt.Text.Trim(),
                            images = imagesList,
                            n = 1,
                            resolution = cmbResolution.Text,
                            aspect_ratio = aspectRatioValue,
                            user = GetOpaqueUserId(),
                            response_format = "b64_json"
                        };
                    }
                }
                else
                {
                    apiUrl = "https://api.x.ai/v1/images/generations";
                    requestBody = new
                    {
                        model = cmbModel.Text,
                        prompt = txtPrompt.Text.Trim(),
                        n = 1,
                        resolution = cmbResolution.Text,
                        aspect_ratio = aspectRatioValue,
                        user = GetOpaqueUserId(),
                        response_format = "b64_json"
                    };
                }

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // ⚡ Bolt Optimization: Create a per-request message to set headers safely with the shared client
                using var requestMessage = new HttpRequestMessage(HttpMethod.Post, apiUrl);
                requestMessage.Headers.Add("Authorization", $"Bearer {txtApiKey?.Text?.Trim()}");
                requestMessage.Content = content;

                // ⚡ Bolt Optimization: Use HttpCompletionOption.ResponseHeadersRead to stream the response
                var response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);

                // ⚡ Bolt Optimization: Read directly from stream to avoid large string allocation
                using var responseStream = await response.Content.ReadAsStreamAsync();

                if (!response.IsSuccessStatusCode)
                {
                    using var reader = new StreamReader(responseStream);
                    var errorString = await reader.ReadToEndAsync();
                    lblStatus.Text = $"❌ Erreur {response.StatusCode}";
                    // Parse the JSON error message to prevent leaking raw HTML or echoing sensitive input/API internals
                    string safeErrorMessage = "Une erreur est survenue lors de la communication avec l'API.";
                    try
                    {
                        using (JsonDocument doc = JsonDocument.Parse(errorString))
                        {
                            if (doc.RootElement.TryGetProperty("error", out JsonElement errorElement) && errorElement.TryGetProperty("message", out JsonElement messageElement))
                            {
                                safeErrorMessage = messageElement.GetString() ?? safeErrorMessage;
                            }
                        }
                    }
                    catch (JsonException)
                    {
                        // Fallback to generic message if parsing fails
                    }

                    MessageBox.Show($"Erreur API :\n{safeErrorMessage}", "Erreur API", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // ⚡ Bolt Optimization: Parse JSON directly from stream
                using var result = await JsonDocument.ParseAsync(responseStream);
                var b64 = result.RootElement.GetProperty("data")[0].GetProperty("b64_json").GetString();
                if (b64 == null)
                {
                    lblStatus.Text = "❌ Réponse API invalide";
                    MessageBox.Show("La réponse de l'API ne contient pas d'image valide.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                currentBase64Image = b64;

                // Affichage de l'image
                var imageBytes = Convert.FromBase64String(b64);
                using var ms = new MemoryStream(imageBytes);
                pictureBox.Image = Image.FromStream(ms);

                lblStatus.Text = $"✅ Image générée avec {cmbModel.Text} ({cmbResolution.Text})";
                btnSave.Enabled = true;
            }
            catch (Exception)
            {
                lblStatus.Text = "❌ Erreur inattendue";
                MessageBox.Show("Une erreur de communication est survenue. Veuillez vérifier votre connexion ou réessayer plus tard.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                Title = "Enregistrer l'image Grok Imagine",
                FileName = $"grok-imagine-{DateTime.Now:yyyyMMdd-HHmmss}.png"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                var imageBytes = Convert.FromBase64String(currentBase64Image);
                File.WriteAllBytes(sfd.FileName, imageBytes);
                lblStatus.Text = $"💾 Image sauvegardée : {Path.GetFileName(sfd.FileName)}";
                MessageBox.Show("Image enregistrée avec succès !", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            using var ofd = new OpenFileDialog
            {
                Filter = "Images|*.jpg;*.jpeg;*.png;*.webp",
                Multiselect = true,
                Title = "Sélectionner des images (max 5)"
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                foreach (var file in ofd.FileNames)
                {
                    if (new FileInfo(file).Length > MaxFileSizeBytes)
                    {
                        MessageBox.Show($"L'image '{Path.GetFileName(file)}' dépasse la limite de 20 Mo et ne sera pas ajoutée.", "Fichier trop volumineux", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        continue;
                    }

                    if (!selectedImages.Contains(file) && selectedImages.Count < 5)
                    {
                        selectedImages.Add(file);
                    }
                }
                UpdateImageButtonText();
            }
        }

        private void UpdateImageButtonText()
        {
            if (btnAddImages != null)
                btnAddImages.Text = $"Ajouter images ({selectedImages.Count}/5)";
        }

        private string GetOpaqueUserId()
        {
            // Compute a SHA-256 hash of the local username to prevent leaking PII
            // Adding a static salt to prevent rainbow table attacks
            string salt = "GrokImagineApp_Salt_2023";
            string rawData = WindowsIdentity.GetCurrent().Name + salt;

            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private void Form1_Resize(object? sender, EventArgs e)
        {
            // PictureBox size is automatically adjusted via Anchor
            // If additional custom adjustments are needed, add here
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
