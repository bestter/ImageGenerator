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
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageGeneratorApp
{
    /// <summary>
    /// User interface Form for creating or editing prompt templates.
    /// Programmatically designed to keep layout responsive, clean, and warnings-free.
    /// </summary>
    public class TemplateEditorForm : Form
    {
        private readonly TemplateRepository _repository;
        private readonly TemplateModel? _existingTemplate;
        private readonly bool _isEditMode;

        // UI Controls
        private TextBox txtKey = null!;
        private TextBox txtCategory = null!;
        private TextBox txtTags = null!;
        private TextBox txtValue = null!;
        private Button btnSave = null!;
        private Button btnCancel = null!;

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateEditorForm"/> class in ADD mode.
        /// </summary>
        /// <param name="repository">The template repository.</param>
        public TemplateEditorForm(TemplateRepository repository) : this(repository, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateEditorForm"/> class in EDIT mode (or ADD mode if template is null).
        /// </summary>
        /// <param name="repository">The template repository.</param>
        /// <param name="template">The existing template model to edit, or null to create a new one.</param>
        public TemplateEditorForm(TemplateRepository repository, TemplateModel? template)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _existingTemplate = template;
            _isEditMode = template != null;

            InitializeControls();

            if (_isEditMode && template != null)
            {
                PopulateFields(template);
            }
        }

        /// <summary>
        /// Programmatically initializes the Form's layout and controls.
        /// </summary>
        private void InitializeControls()
        {
            this.Text = _isEditMode ? "Modifier le modèle de prompt" : "Ajouter un modèle de prompt";
            this.Size = new Size(560, 420);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(450, 350);
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Labels
            var lblKey = new Label { Text = "Nom de clé :", Location = new Point(20, 25), Width = 100, Font = new Font(this.Font, FontStyle.Bold) };
            var lblCategory = new Label { Text = "Catégorie :", Location = new Point(20, 65), Width = 100, Font = new Font(this.Font, FontStyle.Bold) };
            var lblTags = new Label { Text = "Tags :", Location = new Point(20, 105), Width = 100, Font = new Font(this.Font, FontStyle.Bold) };
            var lblValue = new Label { Text = "Texte prompt :", Location = new Point(20, 145), Width = 100, Font = new Font(this.Font, FontStyle.Bold) };

            // TextBoxes
            txtKey = new TextBox
            {
                Location = new Point(130, 22),
                Width = 380,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                MaxLength = 100
            };
            // Disable editing the key if we are in edit mode to prevent breaking references,
            // or we can allow it with collision checks. Let's allow it but check duplicate keys.

            txtCategory = new TextBox
            {
                Location = new Point(130, 62),
                Width = 380,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                MaxLength = 100
            };

            txtTags = new TextBox
            {
                Location = new Point(130, 102),
                Width = 380,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                MaxLength = 200,
                PlaceholderText = "ex: style, retro, portrait (séparés par virgules)"
            };

            txtValue = new TextBox
            {
                Location = new Point(130, 142),
                Width = 380,
                Height = 160,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                MaxLength = 4000,
                PlaceholderText = "Saisissez votre prompt. Utilisez {0}, {1} pour les paramètres."
            };

            // Buttons
            btnSave = new Button
            {
                Text = "Enregistrer",
                Location = new Point(290, 325),
                Width = 100,
                Height = 35,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                UseVisualStyleBackColor = true
            };
            btnSave.Click += BtnSave_Click;

            btnCancel = new Button
            {
                Text = "Annuler",
                Location = new Point(410, 325),
                Width = 100,
                Height = 35,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                DialogResult = DialogResult.Cancel,
                UseVisualStyleBackColor = true
            };

            // Assemble controls
            this.Controls.AddRange(new Control[] {
                lblKey, txtKey,
                lblCategory, txtCategory,
                lblTags, txtTags,
                lblValue, txtValue,
                btnSave, btnCancel
            });

            this.AcceptButton = btnSave;
            this.CancelButton = btnCancel;
        }

        /// <summary>
        /// Populates the UI controls with values from the existing template model.
        /// </summary>
        private void PopulateFields(TemplateModel template)
        {
            txtKey.Text = template.Key;
            txtCategory.Text = template.Category ?? string.Empty;
            txtTags.Text = template.Tags ?? string.Empty;
            txtValue.Text = template.Value;
        }

        /// <summary>
        /// Event handler for the Save button click event.
        /// Performs input validation and database saves.
        /// </summary>
        private async void BtnSave_Click(object? sender, EventArgs e)
        {
            var key = txtKey.Text.Trim();
            var value = txtValue.Text.Trim();
            var category = txtCategory.Text.Trim();
            var tags = txtTags.Text.Trim();

            // 1. Basic Validation
            if (string.IsNullOrWhiteSpace(key))
            {
                MessageBox.Show("Le nom de clé est obligatoire.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtKey.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                MessageBox.Show("Le texte du prompt est obligatoire.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtValue.Focus();
                return;
            }

            // Key formatting validation (should not contain curly braces or colons since they are delimiters)
            if (key.Contains('{') || key.Contains('}') || key.Contains(':'))
            {
                MessageBox.Show("Le nom de clé ne peut pas contenir d'accolades { } ni de deux-points (:).", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtKey.Focus();
                return;
            }

            try
            {
                this.UseWaitCursor = true;
                btnSave.Enabled = false;

                // 2. Collision checking
                if (!_isEditMode || !string.Equals(key, _existingTemplate?.Key, StringComparison.OrdinalIgnoreCase))
                {
                    var existing = await _repository.GetByKeyAsync(key);
                    if (existing != null)
                    {
                        MessageBox.Show($"La clé '{key}' existe déjà. Veuillez en choisir une autre.", "Clé en double", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtKey.Focus();
                        return;
                    }
                }

                // 3. Database operation
                if (_isEditMode && _existingTemplate != null)
                {
                    // Update mode
                    _existingTemplate.Key = key;
                    _existingTemplate.Value = value;
                    _existingTemplate.Category = string.IsNullOrWhiteSpace(category) ? null : category;
                    _existingTemplate.Tags = string.IsNullOrWhiteSpace(tags) ? null : tags;

                    bool success = await _repository.UpdateAsync(_existingTemplate);
                    if (!success)
                    {
                        MessageBox.Show("Impossible de mettre à jour le modèle dans la base de données.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                else
                {
                    // Insert mode
                    var newTemplate = new TemplateModel
                    {
                        Key = key,
                        Value = value,
                        Category = string.IsNullOrWhiteSpace(category) ? null : category,
                        Tags = string.IsNullOrWhiteSpace(tags) ? null : tags,
                        UsageCount = 0,
                        LastUsed = null,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    int affected = await _repository.InsertAsync(newTemplate);
                    if (affected <= 0)
                    {
                        MessageBox.Show("Impossible d'insérer le modèle dans la base de données.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                // Success
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception)
            {
                MessageBox.Show("Une erreur inattendue est survenue lors de l'enregistrement.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSave.Enabled = true;
                this.UseWaitCursor = false;
            }
        }
    }
}