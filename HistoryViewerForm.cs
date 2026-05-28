using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageGeneratorApp
{
    /// <summary>
    /// User interface dialog for browsing, searching, and viewing historical image generations.
    /// Uses programmatically designed dark mode styling with smooth scaling, a SplitContainer,
    /// and clean GDI+ paint operations.
    /// </summary>
    public class HistoryViewerForm : Form
    {
        private readonly GenerationHistoryRepository _historyRepository;
        private readonly ImageProcessingService _imageProcessingService;
        private readonly BindingList<GenerationHistoryModel> _historyList = new();

        // UI Controls
        private SplitContainer splitContainer = null!;
        private TextBox txtSearch = null!;
        private DataGridView dataGridViewHistory = null!;
        private PictureBox pictureBoxImage = null!;
        private Label lblImageStatus = null!;
        private TextBox txtPrompt = null!;
        private Label lblModelValue = null!;
        private TextBox txtMetadata = null!;

        // Protection against rapid selection change race conditions
        private int _currentSelectionToken = 0;

        private Button btnCopyPrompt = null!;
        private GenerationHistoryModel? _currentHistoryItem;

        /// <summary>
        /// Gets the prompt text to paste into the main form's prompt field when the user activates the copy button in history.
        /// Set only when the copy action is triggered; consumed by the owner form after ShowDialog returns.
        /// </summary>
        public string? PromptToLoad { get; private set; }

        /// <summary>
        /// Gets the model name (exact match for ComboBox items) to select in the main form when the copy action occurs.
        /// </summary>
        public string? ModelToLoad { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HistoryViewerForm"/> class.
        /// </summary>
        /// <param name="historyRepository">The history repository from Step 1.</param>
        /// <param name="imageProcessingService">The image processing service from Step 2.</param>
        /// <exception cref="ArgumentNullException">Thrown when dependencies are null.</exception>
        public HistoryViewerForm(
            GenerationHistoryRepository historyRepository,
            ImageProcessingService imageProcessingService)
        {
            _historyRepository = historyRepository ?? throw new ArgumentNullException(nameof(historyRepository));
            _imageProcessingService = imageProcessingService ?? throw new ArgumentNullException(nameof(imageProcessingService));

            InitializeControls();
        }

        /// <summary>
        /// Form Load event: fetches history from the database to populate the grid.
        /// </summary>
        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // SÉCURITÉ : Assigne les dimensions et contraintes réelles du splitter après que le formulaire
            // ait été affiché à sa taille de démarrage (1100px). Évite l'exception InvalidOperationException.
            try
            {
                if (splitContainer.Width > 600)
                {
                    splitContainer.SplitterDistance = 420;
                    splitContainer.Panel1MinSize = 300;
                    splitContainer.Panel2MinSize = 350;
                }
            }
            catch
            {
                // Fallback silencieux en cas d'adaptation DPI atypique
            }

            await LoadHistoryAsync();
        }

        /// <summary>
        /// Programmatically initializes the form controls and layouts.
        /// </summary>
        private void InitializeControls()
        {
            // Form properties
            this.Text = "Historique des Générations";
            this.Size = new Size(1100, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(800, 500);
            this.BackColor = Color.FromArgb(20, 20, 20); // Deep background dark mode
            this.ForeColor = Color.White;

            // Custom typography
            var mainFont = new Font("Segoe UI", 9.5F);
            var titleFont = new Font("Segoe UI Semibold", 10F);
            this.Font = mainFont;

            // SplitContainer for left-side (search/list) and right-side (preview/details)
            splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                SplitterWidth = 6,
                BorderStyle = BorderStyle.None,
                Panel1MinSize = 100, // Safe temporary bounds for early layout phase
                Panel2MinSize = 100
            };
            splitContainer.Panel1.Padding = new Padding(15, 15, 5, 15);
            splitContainer.Panel2.Padding = new Padding(5, 15, 15, 15);

            // === LEFT SIDE PANEL (Search & List) ===
            var leftPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(28, 28, 28),
                Padding = new Padding(15)
            };
            leftPanel.Paint += (s, e) => DrawRoundedBorder(leftPanel, e.Graphics, Color.FromArgb(45, 45, 45), 8);

            var lblSearch = new Label
            {
                Text = "Rechercher (Prompt / Modèle) :",
                Dock = DockStyle.Top,
                Height = 22,
                ForeColor = Color.FromArgb(200, 200, 200),
                Font = titleFont
            };

            txtSearch = new TextBox
            {
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Height = 28,
                Font = mainFont,
                MaxLength = 200
            };
            txtSearch.TextChanged += TxtSearch_TextChanged;

            var spacer = new Panel { Dock = DockStyle.Top, Height = 15 };

            dataGridViewHistory = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToOrderColumns = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                GridColor = Color.FromArgb(45, 45, 45),
                EnableHeadersVisualStyles = false,
                RowTemplate = { Height = 40 }
            };
            dataGridViewHistory.SelectionChanged += DataGridViewHistory_SelectionChanged;

            // Grid header style
            dataGridViewHistory.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 9.5F),
                SelectionBackColor = Color.FromArgb(40, 40, 40),
                Alignment = DataGridViewContentAlignment.MiddleLeft
            };
            dataGridViewHistory.ColumnHeadersHeight = 35;
            dataGridViewHistory.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            // Grid row cell styles
            dataGridViewHistory.RowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                SelectionBackColor = Color.FromArgb(0, 120, 215), // Sky blue accent
                SelectionForeColor = Color.White,
                Font = mainFont,
                Alignment = DataGridViewContentAlignment.MiddleLeft
            };

            dataGridViewHistory.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(35, 35, 35),
                ForeColor = Color.White,
                SelectionBackColor = Color.FromArgb(0, 120, 215),
                SelectionForeColor = Color.White,
                Font = mainFont
            };

            // DataGridView Columns
            var colDate = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "CreatedAt",
                HeaderText = "Date",
                Width = 120,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd HH:mm" }
            };

            var colModel = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "ModelName",
                HeaderText = "Modèle",
                Width = 130
            };

            var colPrompt = new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Prompt",
                HeaderText = "Prompt",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            };

            dataGridViewHistory.Columns.AddRange(colDate, colModel, colPrompt);

            leftPanel.Controls.Add(dataGridViewHistory);
            leftPanel.Controls.Add(spacer);
            leftPanel.Controls.Add(txtSearch);
            leftPanel.Controls.Add(lblSearch);
            splitContainer.Panel1.Controls.Add(leftPanel);

            // === RIGHT SIDE PANEL (Details & Preview) ===
            var rightPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(28, 28, 28),
                Padding = new Padding(15)
            };
            rightPanel.Paint += (s, e) => DrawRoundedBorder(rightPanel, e.Graphics, Color.FromArgb(45, 45, 45), 8);

            // PictureBox Card
            var pictureCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(20, 20, 20),
                Padding = new Padding(5)
            };
            pictureCard.Paint += (s, e) => DrawRoundedBorder(pictureCard, e.Graphics, Color.FromArgb(45, 45, 45), 4);

            pictureBoxImage = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(20, 20, 20)
            };
            pictureCard.Controls.Add(pictureBoxImage);

            lblImageStatus = new Label
            {
                Text = "Sélectionnez une génération pour afficher les détails.",
                Dock = DockStyle.Bottom,
                Height = 25,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(150, 150, 150),
                Font = new Font("Segoe UI", 9.0F, FontStyle.Italic)
            };
            pictureCard.Controls.Add(lblImageStatus);

            // Details Container
            var detailsContainer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 280,
                Padding = new Padding(0, 15, 0, 0)
            };

            // Prompt header: label + inline "Copie prompt" button (per task requirement).
            // Button placed immediately to the right of the label using manual positioning inside a fixed-height
            // header panel (mental layout calc: label ~155px wide + 20px gap => X=175 for compact French text).
            // Header docks top so it stacks visually above the multiline prompt textbox.
            // Styling matches the existing dark theme (grays from 28-60 range) and uses Flat for modern look.
            // No Dock on children; they remain left-aligned on resize (acceptable, prompt content below uses full width).
            var promptHeaderPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 26,
                BackColor = Color.FromArgb(28, 28, 28)
            };

            var lblPrompt = new Label
            {
                Text = "Prompt de génération :",
                Location = new Point(0, 4),
                AutoSize = true,
                ForeColor = Color.FromArgb(200, 200, 200),
                Font = titleFont
            };

            btnCopyPrompt = new Button
            {
                Text = "Copie prompt",
                Location = new Point(175, 1),
                Width = 105,
                Height = 23,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5F),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                TabStop = false
            };
            btnCopyPrompt.FlatAppearance.BorderColor = Color.FromArgb(90, 90, 90);
            btnCopyPrompt.FlatAppearance.BorderSize = 1;
            btnCopyPrompt.Click += BtnCopyPrompt_Click;
            btnCopyPrompt.Enabled = false; // enabled only on valid selection

            promptHeaderPanel.Controls.AddRange(new Control[] { lblPrompt, btnCopyPrompt });

            txtPrompt = new TextBox
            {
                Dock = DockStyle.Top,
                Height = 70,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = mainFont
            };

            var detailSpacer1 = new Panel { Dock = DockStyle.Top, Height = 8 };

            var modelInfoPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 28
            };

            var lblModel = new Label
            {
                Text = "Modèle :",
                Location = new Point(0, 4),
                AutoSize = true,
                ForeColor = Color.FromArgb(200, 200, 200),
                Font = new Font("Segoe UI Semibold", 9.5F)
            };

            lblModelValue = new Label
            {
                Location = new Point(65, 4),
                AutoSize = true,
                ForeColor = Color.FromArgb(0, 175, 240), // Premium sky-blue color
                Font = new Font("Segoe UI Semibold", 9.5F)
            };
            modelInfoPanel.Controls.AddRange(new Control[] { lblModel, lblModelValue });

            var detailSpacer2 = new Panel { Dock = DockStyle.Top, Height = 8 };

            var lblMeta = new Label
            {
                Text = "Métadonnées brutes :",
                Dock = DockStyle.Top,
                Height = 20,
                ForeColor = Color.FromArgb(200, 200, 200),
                Font = titleFont
            };

            txtMetadata = new TextBox
            {
                Dock = DockStyle.Top,
                Height = 110,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 9.0F)
            };

            detailsContainer.Controls.AddRange(new Control[] {
                txtMetadata, lblMeta, detailSpacer2, modelInfoPanel, detailSpacer1, txtPrompt, promptHeaderPanel
            });

            rightPanel.Controls.Add(pictureCard);
            rightPanel.Controls.Add(detailsContainer);
            splitContainer.Panel2.Controls.Add(rightPanel);

            this.Controls.Add(splitContainer);
        }

        // Draw an elegant thin anti-aliased border around container panels
        private void DrawRoundedBorder(Panel panel, Graphics g, Color color, int radius)
        {
            int diameter = radius * 2;
            if (panel.Width <= diameter || panel.Height <= diameter)
            {
                return; // Prevent GDI+ AddArc crash when panel is collapsed or not fully sized yet
            }

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var pen = new Pen(color, 1.5F);
            var rect = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);
            using var path = GetRoundedRectPath(rect, radius);
            g.DrawPath(pen, path);
        }

        private System.Drawing.Drawing2D.GraphicsPath GetRoundedRectPath(Rectangle bounds, int radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            int diameter = radius * 2;
            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.X + bounds.Width - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.X + bounds.Width - diameter, bounds.Y + bounds.Height - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Y + bounds.Height - diameter, diameter, diameter, 90, 90);
            path.CloseAllFigures();
            return path;
        }

        /// <summary>
        /// Loads records from the database and binds them to the DataGridView.
        /// </summary>
        private async Task LoadHistoryAsync()
        {
            try
            {
                this.UseWaitCursor = true;
                var records = await _historyRepository.GetAllAsync();

                _historyList.Clear();
                foreach (var record in records)
                {
                    _historyList.Add(record);
                }

                dataGridViewHistory.DataSource = _historyList;
                UpdateSelectionDetails();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement de l'historique :\n{ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.UseWaitCursor = false;
            }
        }

        /// <summary>
        /// Handles filtering as the user types in the search text box.
        /// </summary>
        private async void TxtSearch_TextChanged(object? sender, EventArgs e)
        {
            var searchTerm = txtSearch.Text;
            try
            {
                var filtered = await _historyRepository.SearchAsync(searchTerm);

                _historyList.Clear();
                foreach (var record in filtered)
                {
                    _historyList.Add(record);
                }

                UpdateSelectionDetails();
            }
            catch (Exception ex)
            {
                lblImageStatus.Text = $"Error searching: {ex.Message}";
            }
        }

        /// <summary>
        /// Triggered whenever the row selection in the grid changes.
        /// Loads the respective WebP image asynchronously and populates metadata text fields.
        /// </summary>
        private void DataGridViewHistory_SelectionChanged(object? sender, EventArgs e)
        {
            UpdateSelectionDetails();
        }

        private async void UpdateSelectionDetails()
        {
            if (dataGridViewHistory.SelectedRows.Count == 0)
            {
                ClearDetails();
                return;
            }

            var selectedRow = dataGridViewHistory.SelectedRows[0];
            if (selectedRow.DataBoundItem is not GenerationHistoryModel history)
            {
                ClearDetails();
                return;
            }

            // Track current item for the copy-prompt action (button "Copie prompt")
            _currentHistoryItem = history;
            btnCopyPrompt.Enabled = !string.IsNullOrWhiteSpace(history.Prompt);

            // Increment the concurrency token to prevent fast-selection race conditions
            var token = ++_currentSelectionToken;

            // Immediately display texts
            txtPrompt.Text = history.Prompt;
            lblModelValue.Text = $"{history.ModelName}  {(string.IsNullOrEmpty(history.ModelVersion) ? "" : $"({history.ModelVersion})")}";
            txtMetadata.Text = FormatJson(history.RawMetadata);

            // Clean up the old image before rendering a new one to prevent GDI+ memory leaks
            var oldImage = pictureBoxImage.Image;
            pictureBoxImage.Image = null;
            oldImage?.Dispose();

            if (string.IsNullOrWhiteSpace(history.ImagePath) || !File.Exists(history.ImagePath))
            {
                lblImageStatus.Text = "⚠️ Image absente du disque.";
                lblImageStatus.Visible = true;
                return;
            }

            lblImageStatus.Text = "⏳ Chargement de l'image...";
            lblImageStatus.Visible = true;

            try
            {
                var gdiImage = await _imageProcessingService.LoadWebpForWinFormsAsync(history.ImagePath);

                // Ensure the selection has not changed during the file load and decode task
                if (token == _currentSelectionToken)
                {
                    pictureBoxImage.Image = gdiImage;
                    lblImageStatus.Text = "";
                    lblImageStatus.Visible = false; // Hide text when successfully loaded
                }
                else
                {
                    // Selection changed in the meantime, discard the loaded image resources
                    gdiImage.Dispose();
                }
            }
            catch (Exception ex)
            {
                if (token == _currentSelectionToken)
                {
                    lblImageStatus.Text = $"❌ Erreur de chargement: {ex.Message}";
                    lblImageStatus.Visible = true;
                }
            }
        }

        private void ClearDetails()
        {
            _currentSelectionToken++; // invalidate active loading tasks

            _currentHistoryItem = null;
            if (btnCopyPrompt != null)
            {
                btnCopyPrompt.Enabled = false;
            }

            var oldImage = pictureBoxImage.Image;
            pictureBoxImage.Image = null;
            oldImage?.Dispose();

            txtPrompt.Clear();
            lblModelValue.Text = string.Empty;
            txtMetadata.Clear();
            lblImageStatus.Text = "Sélectionnez une génération pour afficher les détails.";
            lblImageStatus.Visible = true;
        }

        /// <summary>
        /// Handles the "Copie prompt" button click: captures the current history entry's prompt and model
        /// into public properties, then closes the dialog immediately. The owner (Form1) applies the
        /// values to its txtPrompt and cmbModel right after ShowDialog returns, achieving the "immediate paste"
        /// behavior requested.
        /// </summary>
        private void BtnCopyPrompt_Click(object? sender, EventArgs e)
        {
            if (_currentHistoryItem == null || string.IsNullOrWhiteSpace(_currentHistoryItem.Prompt))
            {
                return;
            }

            PromptToLoad = _currentHistoryItem.Prompt;
            ModelToLoad = _currentHistoryItem.ModelName;

            // Close now so caller can transfer values without the user needing to close manually.
            // This is the minimal, non-refactored way to achieve cross-form immediate copy for a modal dialog.
            this.Close();
        }

        /// <summary>
        /// Pretty prints the raw metadata JSON block.
        /// </summary>
        private string FormatJson(string? rawJson)
        {
            if (string.IsNullOrWhiteSpace(rawJson))
            {
                return "Aucune métadonnée disponible.";
            }

            try
            {
                using var jsonDoc = JsonDocument.Parse(rawJson);
                return JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
            }
            catch
            {
                // Fallback to compact JSON if it is not valid JSON
                return rawJson;
            }
        }

        /// <summary>
        /// Ensures standard WinForms disposals are handled clean.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                var oldImage = pictureBoxImage.Image;
                pictureBoxImage.Image = null;
                oldImage?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}