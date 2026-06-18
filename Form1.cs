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
        private ToolTip toolTipGenerate = null!;
        private Button btnSave = null!;
        private Button btnClear = null!;
        private Label lblStatus = null!;
        private CheckBox chkMultiTurnEditing = null!;
        private string? currentBase64Image = null;
        private byte[]? currentImageBytes = null;
        private ImageGenerationMetadata? currentImageMetadata = null;
        private List<string> selectedImages = new List<string>();
        private Button btnAddImages = null!;
        private const long MaxFileSizeBytes = 20 * 1024 * 1024; // 20 MB

        // 🛡️ Sentinel: Output image size cap (decoded bytes). Complements the client-side guard.
        // Prevents excessive memory allocation from large generated images in the UI layer.
        private const long MaxGeneratedImageBytes = 50 * 1024 * 1024; // 50 MB

        // ⚡ Bolt Optimization: Use a shared HttpClient instance for the lifetime of the application
        // This avoids socket exhaustion (TIME_WAIT state) and eliminates TCP/TLS handshake latency on subsequent requests
        // 🛡️ Sentinel: Add timeout to prevent hanging indefinitely if external API is unresponsive
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        private readonly ImageGeneratorClient _imageClient = new ImageGeneratorClient(_httpClient);

        // Prompt Template System Fields
        private readonly DatabaseHelper _dbHelper = new DatabaseHelper();
        private readonly TemplateRepository _templateRepo;
        private readonly TemplateParser _templateParser;
        private readonly GenerationHistoryRepository _historyRepo;
        private readonly ImageProcessingService _imageProcessingService;
        private readonly HistoryOrchestrator _historyOrchestrator;

        private CheckBox chkEnableTemplates = null!;
        private Button btnManageTemplates = null!;
        private Button btnHistory = null!;
        private ListBox lstAutocomplete = null!;
        private MenuStrip mainMenuStrip = null!;
        private List<string> _templateKeysCache = new List<string>();
        private bool _hasPromptError = false;
        private bool _isGenerating = false;
        private System.Windows.Forms.Timer _validationDebounceTimer = null!;

        public Form1()
        {
            _templateRepo = new TemplateRepository(_dbHelper);
            _templateParser = new TemplateParser(_templateRepo);
            _historyRepo = new GenerationHistoryRepository(_dbHelper);
            _imageProcessingService = new ImageProcessingService();
            _historyOrchestrator = new HistoryOrchestrator(_imageProcessingService, _historyRepo);
            InitializeControls();
        }

        private void InitializeControls()
        {
            _validationDebounceTimer = new System.Windows.Forms.Timer { Interval = 300 };
            _validationDebounceTimer.Tick += (s, ev) =>
            {
                _validationDebounceTimer.Stop();
                _ = UpdateGenerateButtonStateAsync();
            };

            // Create the menu FIRST (before ClientSize / WindowState / any other controls).
            // This gives the docked MenuStrip the best chance to reserve vertical space
            // in the client area before we use absolute Locations. Critical on Maximized forms.
            mainMenuStrip = new MenuStrip();
            var helpMenu = new ToolStripMenuItem("Aide");
            var aboutMenuItem = new ToolStripMenuItem("À propos de Générateur d'image...");
            aboutMenuItem.Click += AboutMenuItem_Click;
            helpMenu.DropDownItems.Add(aboutMenuItem);
            mainMenuStrip.Items.Add(helpMenu);
            this.MainMenuStrip = mainMenuStrip;
            this.Controls.Add(mainMenuStrip);

            // Force the MenuStrip to perform layout immediately so its Height is measured
            // and the client area top is correctly offset before we position the first controls.
            // This prevents the classic MenuStrip-overlapping-absolute-controls bug on Maximized + HighDPI forms.
            this.PerformLayout();
            int menuHeight = mainMenuStrip.Height;
            int contentTop = menuHeight + 6; // small breathing room under the menu

            this.Text = "Générateur d'image Grok Imagine et Nano Banana Pro";
            this.ClientSize = new Size(900, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;

            // UI elements are offset relative to contentTop to avoid MenuStrip overlap on HighDPI/Maximized
            lblKey = new Label
            {
                Text = "Clé API xAI :",
                Location = new Point(20, contentTop),
                AutoSize = true,
                ForeColor = Color.FromArgb(220, 76, 30)
            };
            // ⚡ Bolt Optimization: Enforce MaxLength to prevent UI thread freezing and memory exhaustion from pasting massive strings
            txtApiKey = new TextBox { Location = new Point(190, contentTop - 3), Width = 580, PasswordChar = '•', Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, MaxLength = 1024 };
            txtApiKey.TextChanged += TxtApiKey_TextChanged;

            // Prompt - also protected from menu overlap using the same measured offset
            var lblPrompt = new Label { Text = "Prompt :", Location = new Point(20, contentTop + 38), AutoSize = true };
            txtPrompt = new TextBox { Location = new Point(190, contentTop + 35), Width = 580, Height = 100, Multiline = true, ScrollBars = ScrollBars.Vertical, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, MaxLength = 4000 };
            txtPrompt.KeyDown += TxtPrompt_KeyDown;
            txtPrompt.TextChanged += TxtPrompt_TextChanged;
            txtPrompt.LostFocus += TxtPrompt_LostFocus;

            // Modèle
            var lblModel = new Label { Text = "Modèle :", Location = new Point(20, contentTop + 145), AutoSize = true };
            cmbModel = new ComboBox { Location = new Point(190, contentTop + 142), Width = 230, DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Top | AnchorStyles.Left };
            cmbModel.Items.AddRange(new[] { "grok-imagine-image", "grok-imagine-image-quality", "nano-banana-pro" });
            cmbModel.SelectedIndex = 0;
            cmbModel.SelectedIndexChanged += CmbModel_SelectedIndexChanged;

            // Résolution (haute dispo)
            var lblRes = new Label { Text = "Résolution :", Location = new Point(440, contentTop + 145), AutoSize = true };
            cmbResolution = new ComboBox { Location = new Point(520, contentTop + 142), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Top | AnchorStyles.Left };
            cmbResolution.Items.AddRange(new[] { "1k", "2k" });
            cmbResolution.SelectedIndex = 1; // 2k par défaut (haute résolution)

            // Images
            btnAddImages = new Button { Text = "Ajouter images (0/3)", Location = new Point(690, contentTop + 141), Width = 150, Height = 25, Anchor = AnchorStyles.Top | AnchorStyles.Left };
            btnAddImages.Click += BtnAddImages_Click;

            // Aspect Ratio
            var lblRatio = new Label { Text = "Aspect Ratio :", Location = new Point(20, contentTop + 190), AutoSize = true };
            cmbAspectRatio = new ComboBox { Location = new Point(190, contentTop + 187), Width = 210, DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Top | AnchorStyles.Left };
            cmbAspectRatio.Items.AddRange(new[] { "1:1 (Médias sociaux)", "16:9 (Widescreen)", "9:16 (Stories/Reels)", "4:3 (Standard)", "3:2 (Photographie)", "20:9 (Panoramique cellulaire)" });
            cmbAspectRatio.SelectedIndex = 1; // 16:9 par défaut

            // Multi-turn editing
            chkMultiTurnEditing = new CheckBox
            {
                Text = "Éditer l'image actuelle (Multi-turn)",
                Location = new Point(440, contentTop + 190),
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };

            // Boutons
            btnGenerate = new Button { Text = "Générer l\'image", Location = new Point(190, contentTop + 230), Width = 160, Height = 40, Anchor = AnchorStyles.Top | AnchorStyles.Left };
            btnGenerate.Click += BtnGenerate_Click;
            btnGenerate.MouseEnter += BtnGenerate_MouseEnter;

            toolTipGenerate = new ToolTip
            {
                ToolTipTitle = "Aperçu du prompt résolu",
                UseFading = true,
                UseAnimation = true,
                AutomaticDelay = 500,
                AutoPopDelay = 20000, // 20s visibility to allow reading long prompts
                InitialDelay = 500,
                ReshowDelay = 100
            };

            btnSave = new Button { Text = "📥 Enregistrer l\'image (haute rés.)", Location = new Point(370, contentTop + 230), Width = 250, Height = 40, Enabled = false, Anchor = AnchorStyles.Top | AnchorStyles.Left };
            btnSave.Click += BtnSave_Click;

            btnClear = new Button { Text = "Effacer", Location = new Point(640, contentTop + 230), Width = 100, Height = 40, Anchor = AnchorStyles.Top | AnchorStyles.Left };
            btnClear.Click += (s, e) => ClearForm();

            btnHistory = new Button { Text = "📜 Historique", Location = new Point(750, contentTop + 230), Width = 130, Height = 40, Anchor = AnchorStyles.Top | AnchorStyles.Left };
            btnHistory.Click += BtnHistory_Click;

            // Status
            lblStatus = new Label { Location = new Point(20, contentTop + 280), Width = 750, Height = 30, ForeColor = Color.DarkBlue, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

            // PictureBox
            pictureBox = new PictureBox
            {
                Location = new Point(20, contentTop + 310),
                Size = new Size(840, 320),
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.Black,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            // Prompt Template System UI controls
            btnManageTemplates = new Button
            {
                Text = "Modèles",
                Location = new Point(780, contentTop + 27),
                Width = 100,
                Height = 60,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                UseVisualStyleBackColor = true
            };
            btnManageTemplates.Click += BtnManageTemplates_Click;

            chkEnableTemplates = new CheckBox
            {
                Text = "Activer modèles",
                Location = new Point(780, contentTop + 95),
                Width = 110,
                Height = 30,
                Checked = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                AutoSize = true
            };
            chkEnableTemplates.CheckedChanged += (s, ev) =>
            {
                _validationDebounceTimer.Stop();
                _validationDebounceTimer.Start();
            };

            // Lightweight custom floating ListBox for autocomplete mid-string
            lstAutocomplete = new ListBox
            {
                Visible = false,
                Width = 200,
                Height = 120,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                ForeColor = Color.Black,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular)
            };
            lstAutocomplete.DoubleClick += (s, ev) => InsertSelectedTemplate();

            this.Controls.AddRange(new Control[] { lblKey, txtApiKey, lblPrompt, txtPrompt, lblModel, cmbModel, lblRes, cmbResolution, btnAddImages, lblRatio, cmbAspectRatio, chkMultiTurnEditing,
                btnGenerate, btnSave, btnClear, btnHistory, lblStatus, pictureBox, btnManageTemplates, chkEnableTemplates, lstAutocomplete });

            lstAutocomplete.BringToFront();

            // Apply initial model-dependent state now that all controls are created
            UpdateModelDependentControls();

            this.Paint += Form1_Paint;
            this.Resize += Form1_Resize;

            _ = UpdateGenerateButtonStateAsync();
        }

        private async void BtnManageTemplates_Click(object? sender, EventArgs e)
        {
            using var managerForm = new TemplatesManagerForm(_templateRepo);
            managerForm.ShowDialog(this);
            await RefreshTemplateKeysCacheAsync();
        }

        private void BtnHistory_Click(object? sender, EventArgs e)
        {
            using var historyForm = new HistoryViewerForm(_historyRepo, _imageProcessingService);
            historyForm.ShowDialog(this);

            // Apply "Copie prompt" result if the user activated the button inside the history viewer.
            // The history dialog closes itself on copy, so we are immediately back here to paste.
            // Setting txtPrompt.Text triggers TextChanged (resets red border error state, updates generate button, etc.).
            // Setting cmbModel.SelectedIndex triggers SelectedIndexChanged (updates model-dependent controls + key label).
            if (!string.IsNullOrWhiteSpace(historyForm.PromptToLoad))
            {
                txtPrompt.Text = historyForm.PromptToLoad;

                if (!string.IsNullOrWhiteSpace(historyForm.ModelToLoad))
                {
                    int index = cmbModel.Items.IndexOf(historyForm.ModelToLoad);
                    if (index >= 0)
                    {
                        cmbModel.SelectedIndex = index;
                    }
                    // If the stored model name is not in the current list (e.g. future model), we leave the
                    // current selection unchanged. This is defensive and keeps behavior minimal.
                }
            }
        }

        /// <summary>
        /// Opens the professional About dialog. Called from the Help menu.
        /// </summary>
        private void AboutMenuItem_Click(object? sender, EventArgs e)
        {
            using var aboutForm = new AboutForm();
            aboutForm.ShowDialog(this);
        }

        private async void BtnGenerate_MouseEnter(object? sender, EventArgs e)
        {
            if (txtPrompt == null || _templateParser == null || chkEnableTemplates == null) return;

            string rawPrompt = txtPrompt.Text.Trim();
            if (string.IsNullOrEmpty(rawPrompt))
            {
                toolTipGenerate.SetToolTip(btnGenerate, "Saisissez un prompt pour générer une image.");
                return;
            }

            if (chkEnableTemplates.Checked)
            {
                try
                {
                    // Asynchronously expand and process the prompt (without updating usage count stats!)
                    string resolved = await _templateParser.ProcessPromptAsync(rawPrompt, incrementUsageStats: false);
                    toolTipGenerate.SetToolTip(btnGenerate, $"Prompt résolu :\n{resolved}");

                    if (_hasPromptError)
                    {
                        _hasPromptError = false;
                        this.Invalidate();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
                    // 🛡️ Sentinel: Present a generic error message and avoid leaking raw exceptions.
                    toolTipGenerate.SetToolTip(btnGenerate, "Erreur lors de la résolution du gabarit.");

                    if (!_hasPromptError)
                    {
                        _hasPromptError = true;
                        this.Invalidate();
                    }
                }
            }
            else
            {
                toolTipGenerate.SetToolTip(btnGenerate, $"Prompt brut :\n{rawPrompt}");

                if (_hasPromptError)
                {
                    _hasPromptError = false;
                    this.Invalidate();
                }
            }
        }

        private async void BtnGenerate_Click(object? sender, EventArgs e)
        {
            string apiKey = txtApiKey.Text?.Trim() ?? string.Empty;

            string provider = cmbModel.Text == "nano-banana-pro" ? "Google" : "xAI";
            ApiKeyStorageHelper.SaveApiKey(provider, apiKey);

            string? imageToEditBase64 = null;
            if (chkMultiTurnEditing.Checked && !string.IsNullOrEmpty(currentBase64Image))
            {
                imageToEditBase64 = currentBase64Image;
            }

            Image? previousImage = pictureBox.Image;
            string? previousBase64Image = currentBase64Image;
            byte[]? previousImageBytes = currentImageBytes;

            _isGenerating = true;
            _ = UpdateGenerateButtonStateAsync();
            btnSave.Enabled = false;
            lblStatus.Text = "⏳ Génération en cours...";
            DisposeCurrentImage();
            currentBase64Image = null;
            currentImageBytes = null;

            try
            {
                string selectedRatioText = cmbAspectRatio.SelectedItem?.ToString() ?? "16:9";
                string aspectRatioValue = selectedRatioText.Split(' ')[0];
                string opaqueUserId = await UserIdHelper.GetOpaqueUserIdAsync();

                List<ImageUrlObject> imagesList = await PrepareReferenceImagesAsync(imageToEditBase64);

                string processedPrompt = txtPrompt.Text.Trim();
                if (chkEnableTemplates.Checked)
                {
                    processedPrompt = await _templateParser.ProcessPromptAsync(processedPrompt, incrementUsageStats: true);
                }

                string base64Image = await _imageClient.GenerateImageAsync(
                    apiKey,
                    processedPrompt,
                    cmbModel.Text,
                    cmbResolution.Text,
                    aspectRatioValue,
                    opaqueUserId,
                    imagesList);

                UpdateUIWithGeneratedImage(base64Image, processedPrompt, cmbModel.Text, cmbResolution.Text, aspectRatioValue);
            }
            catch (Exception ex)
            {
                HandleGenerationException(ex);
            }
            finally
            {
                _isGenerating = false;
                _ = UpdateGenerateButtonStateAsync();
                if (currentBase64Image == null && previousBase64Image != null)
                {
                    currentBase64Image = previousBase64Image;
                    currentImageBytes = previousImageBytes;
                    currentImageMetadata = null; // No prior metadata snapshot available for the recovered image
                    DisposeCurrentImage();
                    pictureBox.Image = previousImage; // previousImage lifetime managed by prior assignment site
                    btnSave.Enabled = true;
                }
            }
        }

        private async Task<List<ImageUrlObject>> PrepareReferenceImagesAsync(string? imageToEditBase64)
        {
            var imagesList = new List<ImageUrlObject>();

            if (selectedImages.Count > 0 || !string.IsNullOrEmpty(imageToEditBase64))
            {
                if (!string.IsNullOrEmpty(imageToEditBase64))
                {
                    imagesList.Add(new ImageUrlObject { Type = "image_url", Url = $"data:image/png;base64,{imageToEditBase64}" });
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
                            this.Invoke(() =>
                            {
                                lblStatus.Text = $"❌ Image trop grande : {Path.GetFileName(imgPath)}";
                                MessageBox.Show($"L'image '{Path.GetFileName(imgPath)}' dépasse la limite de 20 Mo.", "Fichier trop volumineux", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            });
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

                    return new ImageUrlObject { Type = "image_url", Url = url };
                }).ToArray();

                var results = await Task.WhenAll(tasks);
                foreach (var res in results)
                {
                    if (res != null) imagesList.Add(res);
                }
            }

            return imagesList;
        }

        private void UpdateUIWithGeneratedImage(string base64Image, string processedPrompt, string model, string resolution, string aspectRatio)
        {
            currentBase64Image = base64Image;

            // Capture immutable generation metadata for embedding on export.
            // This snapshot ensures the prompt/model/etc. match the actual image even if the user
            // later edits the prompt textbox or changes the model combo.
            currentImageMetadata = new ImageGenerationMetadata(
                ImageMetadataEmbedder.GetFriendlyGeneratorName(model),
                processedPrompt,
                model,
                DateTime.UtcNow,
                resolution,
                aspectRatio,
                ImageMetadataEmbedder.AppNameVersion);

            // ⚡ Bolt Optimization: Cache the decoded image bytes to avoid repeated Base64 decoding
            // (which incurs large LOH allocations and CPU overhead) when saving the image later.
            var imageBytes = Convert.FromBase64String(base64Image);
            if (imageBytes.Length > MaxGeneratedImageBytes)
            {
                throw new ImageGeneratorException("L'image générée dépasse la taille maximale autorisée.");
            }
            currentImageBytes = imageBytes;

            DisposeCurrentImage();
            using (var ms = new MemoryStream(imageBytes))
            {
                pictureBox.Image = Image.FromStream(ms);
            }

            lblStatus.Text = $"✅ Image générée avec {model} ({resolution})";
            btnSave.Enabled = true;

            // Log this generation to our SQLite history asynchronously
            _ = Task.Run(async () =>
            {
                try
                {
                    // Construct a rawMetadata JSON string to store in the DB
                    var rawMetadataJson = $"{{\"resolution\":\"{resolution}\",\"aspect_ratio\":\"{aspectRatio}\"}}";

                    await _historyOrchestrator.LogGenerationAsync(
                        imageBytes,
                        processedPrompt,
                        model,
                        modelVersion: null,
                        rawMetadata: rawMetadataJson
                    );
                }
                catch
                {
                    // Fail silently for history logging to never disrupt user generation experience
                }
            });
        }

        private void HandleGenerationException(Exception ex)
        {
            if (ex is KeyNotFoundException)
            {
                _hasPromptError = true;
                this.Invalidate();
                MessageBox.Show("Le gabarit demandé n'est pas reconnu ou introuvable.", "Modèle non reconnu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (ex is FormatException)
            {
                _hasPromptError = true;
                this.Invalidate();
                MessageBox.Show("Une erreur de syntaxe a été détectée dans le gabarit.", "Erreur de modèles", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (ex is InvalidOperationException && ex.Message.Contains("récursion"))
            {
                _hasPromptError = true;
                this.Invalidate();
                MessageBox.Show("Une boucle de récursion infinie a été détectée dans les gabarits.", "Erreur de récursion", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (ex is ArgumentException)
            {
                MessageBox.Show("Une erreur de validation est survenue. Veuillez vérifier vos entrées.", "Erreur de validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else if (ex is ImageGeneratorException generatorEx)
            {
                lblStatus.Text = $"❌ Erreur {generatorEx.StatusCode}";
                MessageBox.Show($"Erreur API :\n{generatorEx.Message}", "Erreur API", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (ex is TaskCanceledException)
            {
                lblStatus.Text = "❌ Délai d'attente dépassé";
                MessageBox.Show("La requête a mis trop de temps à répondre. Veuillez réessayer plus tard.", "Erreur de délai d'attente", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                lblStatus.Text = "❌ Erreur inattendue";
                MessageBox.Show("Une erreur inattendue est survenue lors de la génération.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnSave_Click(object? sender, EventArgs e)
        {
            if (currentBase64Image == null) return;

            // Support both PNG (lossless, recommended for AI art) and JPEG per requirements.
            // Metadata embedding works for both (EXIF + XMP for JPEG; EXIF + XMP + PNG text chunks for PNG).
            using var sfd = new SaveFileDialog
            {
                Filter = "PNG Image|*.png|JPEG Image|*.jpg;*.jpeg",
                Title = "Enregistrer l'image",
                DefaultExt = "png",
                FileName = $"image-{DateTime.Now:yyyyMMdd-HHmmss}.png"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // Offload CPU-bound base64 decoding and metadata embedding to a background thread to prevent UI freezing
                    byte[] bytesToSave = await Task.Run(() =>
                    {
                        var originalBytes = currentImageBytes ?? Convert.FromBase64String(currentBase64Image);
                        if (currentImageMetadata != null)
                        {
                            try
                            {
                                // Automatic metadata embedding (robust: never crash the save on metadata failure)
                                string? targetExt = Path.GetExtension(sfd.FileName);
                                return ImageMetadataEmbedder.Embed(originalBytes, currentImageMetadata, targetExt);
                            }
                            catch
                            {
                                // Fallback: save the raw image without metadata rather than failing the whole operation
                                return originalBytes;
                            }
                        }
                        return originalBytes;
                    });

                    await File.WriteAllBytesAsync(sfd.FileName, bytesToSave);
                    lblStatus.Text = "💾 Image sauvegardée avec métadonnées AI intégrées.";
                    MessageBox.Show("Image enregistrée avec succès !", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error saving image: {ex.Message}");
                    lblStatus.Text = "❌ Erreur de sauvegarde";
                    MessageBox.Show("Une erreur est survenue lors de la sauvegarde de l'image.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ClearForm()
        {
            txtPrompt.Clear();
            DisposeCurrentImage();
            currentBase64Image = null;
            currentImageBytes = null;
            currentImageMetadata = null;
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
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error opening image: {ex.Message}");
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error selecting images: {ex.Message}");
                MessageBox.Show("Une erreur est survenue lors de la sélection des images.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateImageButtonText()
        {
            if (btnAddImages != null)
                btnAddImages.Text = $"Ajouter images ({selectedImages.Count}/3)";
        }

        // 🛡️ Sentinel: Centralized disposal of previous System.Drawing.Image + GDI resources.
        // PictureBox assignment does not auto-dispose the prior image; repeated generations without
        // explicit cleanup can leak handles and memory over a long session.
        private void DisposeCurrentImage()
        {
            var oldImage = pictureBox.Image;
            pictureBox.Image = null;
            oldImage?.Dispose();
        }

        private void Form1_Resize(object? sender, EventArgs e)
        {
            // PictureBox size is automatically adjusted via Anchor
            // If additional custom adjustments are needed, add here
            this.Invalidate();
        }

        // 🛡️ Sentinel: Enforce model-specific UI state per AGENTS.md requirement.
        // Nano Banana Pro does not support image editing/multi-turn; disable related controls
        // and clear any pending edit state to prevent invalid combinations reaching the client.
        private void UpdateModelDependentControls()
        {
            bool isNano = cmbModel.SelectedItem?.ToString() == "nano-banana-pro";
            if (btnAddImages != null)
            {
                btnAddImages.Enabled = !isNano;
            }
            if (chkMultiTurnEditing != null)
            {
                chkMultiTurnEditing.Enabled = !isNano;
            }

            if (isNano)
            {
                if (selectedImages.Count > 0)
                {
                    selectedImages.Clear();
                    UpdateImageButtonText();
                }
                if (chkMultiTurnEditing != null)
                {
                    chkMultiTurnEditing.Checked = false;
                }
            }
        }

        private void CmbModel_SelectedIndexChanged(object? sender, EventArgs e)
        {
            UpdateModelDependentControls();

            if (cmbModel.SelectedItem?.ToString() == "nano-banana-pro")
            {
                lblKey.Text = "Clé Google Cloud :";
                lblKey.ForeColor = Color.FromArgb(26, 115, 232); // Google Blue

                string savedKey = ApiKeyStorageHelper.LoadApiKey("Google");
                if (txtApiKey != null) txtApiKey.Text = savedKey ?? string.Empty;
            }
            else
            {
                lblKey.Text = "Clé API xAI :";
                lblKey.ForeColor = Color.FromArgb(220, 76, 30); // xAI Orange-Red

                string savedKey = ApiKeyStorageHelper.LoadApiKey("xAI");
                if (txtApiKey != null) txtApiKey.Text = savedKey ?? string.Empty;
            }
        }

        //[STAThread]
        //static void Main()
        //{
        //    Application.EnableVisualStyles();
        //    Application.SetCompatibleTextRenderingDefault(false);
        //    Application.Run(new Form1());
        //}

        // --- Prompt Template System Autocomplete Operations ---

        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            string initialProvider = cmbModel?.Text == "nano-banana-pro" ? "Google" : "xAI";
            string savedKey = ApiKeyStorageHelper.LoadApiKey(initialProvider);
            if (!string.IsNullOrEmpty(savedKey) && txtApiKey != null)
            {
                txtApiKey.Text = savedKey;
            }

            await RefreshTemplateKeysCacheAsync();
        }

        private async Task RefreshTemplateKeysCacheAsync()
        {
            try
            {
                var templateKeys = await _templateRepo.GetAllKeysAsync();
                // ⚡ Bolt Optimization: Keys are already returned pre-sorted via database index (GetAllKeysAsync),
                // completely eliminating the O(N log N) in-memory OrderBy allocation on the UI thread.
                _templateKeysCache = templateKeys.ToList();
            }
            catch
            {
                // Silence cache load failures during initialization
            }
        }

        private (int triggerIndex, string query, bool active) GetActiveTrigger()
        {
            int caretIndex = txtPrompt.SelectionStart;
            if (caretIndex == 0) return (-1, string.Empty, false);

            string text = txtPrompt.Text;
            int lastBrace = text.LastIndexOf('{', caretIndex - 1);
            if (lastBrace == -1) return (-1, string.Empty, false);

            // Verify no closing brace in between the trigger and current caret index
            int lastClosing = text.LastIndexOf('}', caretIndex - 1);
            if (lastClosing > lastBrace)
            {
                return (-1, string.Empty, false);
            }

            string query = text.Substring(lastBrace + 1, caretIndex - (lastBrace + 1));

            // Prevent matching if trigger query contains characters that are invalid key syntax (newline, colon)
            if (query.Contains('\r') || query.Contains('\n') || query.Contains(':'))
            {
                return (-1, string.Empty, false);
            }

            return (lastBrace, query, true);
        }

        private void PositionAutocomplete(int triggerIndex)
        {
            // Retrieve position of the '{' trigger relative to prompt textbox
            Point charPos = txtPrompt.GetPositionFromCharIndex(triggerIndex);

            // Convert character index position to Form coordinate space
            Point formPos = txtPrompt.PointToScreen(charPos);
            Point localPos = this.PointToClient(formPos);

            int x = localPos.X;
            int y = localPos.Y + 20; // offset slightly below the caret line

            // Boundary guard: ensure it fits within the horizontal form client space
            if (x + lstAutocomplete.Width > this.ClientSize.Width)
            {
                x = this.ClientSize.Width - lstAutocomplete.Width - 10;
            }

            // Boundary guard: ensure it fits within vertical space (draw above caret if it clips form bottom)
            if (y + lstAutocomplete.Height > this.ClientSize.Height)
            {
                y = localPos.Y - lstAutocomplete.Height - 5;
            }

            lstAutocomplete.Location = new Point(Math.Max(10, x), Math.Max(10, y));
        }

        private void InsertSelectedTemplate()
        {
            if (lstAutocomplete.SelectedItem is not string selectedKey) return;

            var (triggerIndex, query, active) = GetActiveTrigger();
            if (!active) return;

            int caretIndex = txtPrompt.SelectionStart;
            string text = txtPrompt.Text;

            // Replace the template tag segment with fully enclosed brace tag
            string before = text.Substring(0, triggerIndex);
            string after = text.Substring(caretIndex);

            txtPrompt.Text = before + "{" + selectedKey + "}" + after;
            txtPrompt.SelectionStart = triggerIndex + selectedKey.Length + 2; // place caret after '}'

            lstAutocomplete.Visible = false;
            txtPrompt.Focus();
        }

        private void TxtPrompt_KeyDown(object? sender, KeyEventArgs e)
        {
            if (!lstAutocomplete.Visible) return;

            if (e.KeyCode == Keys.Down)
            {
                int next = lstAutocomplete.SelectedIndex + 1;
                if (next < lstAutocomplete.Items.Count)
                {
                    lstAutocomplete.SelectedIndex = next;
                }
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Up)
            {
                int prev = lstAutocomplete.SelectedIndex - 1;
                if (prev >= 0)
                {
                    lstAutocomplete.SelectedIndex = prev;
                }
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Tab)
            {
                InsertSelectedTemplate();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                lstAutocomplete.Visible = false;
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void TxtPrompt_TextChanged(object? sender, EventArgs e)
        {
            if (_hasPromptError)
            {
                _hasPromptError = false;
                this.Invalidate();
            }

            // ⚡ Bolt Optimization: Debounce UI inputs that trigger database queries
            _validationDebounceTimer.Stop();
            _validationDebounceTimer.Start();

            if (!chkEnableTemplates.Checked)
            {
                lstAutocomplete.Visible = false;
                return;
            }

            var (triggerIndex, query, active) = GetActiveTrigger();
            if (active)
            {
                var matched = _templateKeysCache
                    .Where(k => k.Contains(query, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (matched.Count > 0)
                {
                    lstAutocomplete.BeginUpdate();
                    lstAutocomplete.Items.Clear();
                    // ⚡ Bolt Optimization: Batch insert autocomplete items using .AddRange() instead of a foreach loop
                    // This prevents repeated array resizing and layout recalculations, optimizing rendering performance
                    lstAutocomplete.Items.AddRange(matched.Cast<object>().ToArray());
                    lstAutocomplete.SelectedIndex = 0;
                    lstAutocomplete.EndUpdate();

                    PositionAutocomplete(triggerIndex);
                    lstAutocomplete.Visible = true;
                    lstAutocomplete.BringToFront();
                }
                else
                {
                    lstAutocomplete.Visible = false;
                }
            }
            else
            {
                lstAutocomplete.Visible = false;
            }
        }

        private void TxtPrompt_LostFocus(object? sender, EventArgs e)
        {
            _ = ValidatePromptAsync();

            // Give double-click actions some time to resolve before closing the window
            var timer = new System.Windows.Forms.Timer { Interval = 200 };
            timer.Tick += (s, ev) =>
            {
                timer.Stop();
                timer.Dispose();
                if (!lstAutocomplete.Focused && !txtPrompt.Focused)
                {
                    lstAutocomplete.Visible = false;
                }
            };
            timer.Start();
        }

        private void Form1_Paint(object? sender, PaintEventArgs e)
        {
            if (_hasPromptError && txtPrompt != null)
            {
                using (var pen = new Pen(Color.Red, 2))
                {
                    var rect = txtPrompt.Bounds;
                    rect.Inflate(2, 2);
                    e.Graphics.DrawRectangle(pen, rect);
                }
            }
        }

        private void TxtApiKey_TextChanged(object? sender, EventArgs e)
        {
            // ⚡ Bolt Optimization: Debounce UI inputs that trigger database queries
            _validationDebounceTimer.Stop();
            _validationDebounceTimer.Start();
        }

        private async Task ValidatePromptAsync()
        {
            if (txtPrompt == null || _templateParser == null || chkEnableTemplates == null) return;

            bool hasError = false;
            if (chkEnableTemplates.Checked)
            {
                string rawPrompt = txtPrompt.Text.Trim();
                if (!string.IsNullOrEmpty(rawPrompt))
                {
                    try
                    {
                        // Dry-run process without usage count increments
                        await _templateParser.ProcessPromptAsync(rawPrompt, incrementUsageStats: false);
                    }
                    catch
                    {
                        hasError = true;
                    }
                }
            }

            if (_hasPromptError != hasError)
            {
                _hasPromptError = hasError;
                this.Invalidate();
            }
        }

        private async Task UpdateGenerateButtonStateAsync()
        {
            if (txtApiKey == null || txtPrompt == null || btnGenerate == null || _templateParser == null) return;

            if (_isGenerating)
            {
                btnGenerate.Enabled = false;
                return;
            }

            string key = txtApiKey.Text.Trim();
            string prompt = txtPrompt.Text.Trim();

            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(prompt))
            {
                btnGenerate.Enabled = false;
                return;
            }

            // Fast-scan syntax check (sync) to avoid async database queries for basic syntax issues
            if (chkEnableTemplates != null && chkEnableTemplates.Checked)
            {
                try
                {
                    int braceCount = 0;
                    for (int i = 0; i < prompt.Length; i++)
                    {
                        char c = prompt[i];
                        if (c == '{')
                        {
                            braceCount++;
                            if (braceCount > 1) throw new FormatException();
                        }
                        else if (c == '}')
                        {
                            braceCount--;
                            if (braceCount < 0) throw new FormatException();
                        }
                    }
                    if (braceCount != 0) throw new FormatException();
                }
                catch
                {
                    btnGenerate.Enabled = false;
                    return;
                }
            }

            // Database key resolution check (async)
            bool isValid = true;
            if (chkEnableTemplates != null && chkEnableTemplates.Checked)
            {
                try
                {
                    await _templateParser.ProcessPromptAsync(prompt, incrementUsageStats: false);
                }
                catch
                {
                    isValid = false;
                }
            }

            btnGenerate.Enabled = isValid;
        }
    }
}