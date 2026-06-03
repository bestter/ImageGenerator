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
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageGeneratorApp
{
    /// <summary>
    /// User interface Form for managing prompt templates.
    /// Programmatically designed to keep UI responsive, modern, and easily maintainable.
    /// </summary>
    public class TemplatesManagerForm : Form
    {
        private readonly TemplateRepository _repository;
        private List<TemplateModel> _allTemplates = new();
        private readonly BindingList<TemplateModel> _filteredTemplates = new();

        // UI Controls
        private TextBox txtSearch = null!;
        private ComboBox cmbCategory = null!;
        private DataGridView dataGridViewTemplates = null!;
        private Button btnAdd = null!;
        private Button btnEdit = null!;
        private Button btnDuplicate = null!;
        private Button btnDelete = null!;

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplatesManagerForm"/> class.
        /// </summary>
        /// <param name="repository">The template repository containing stored prompt data.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="repository"/> is null.</exception>
        public TemplatesManagerForm(TemplateRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            InitializeControls();
        }

        /// <summary>
        /// Asynchronously loads templates from the database and initializes the grid bindings.
        /// </summary>
        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            await LoadTemplatesAsync();
        }

        /// <summary>
        /// Programmatically initializes the Form's controls and layouts.
        /// </summary>
        private void InitializeControls()
        {
            this.Text = "Gestionnaire de gabarits de prompt";
            this.Size = new Size(900, 550);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(700, 400);

            // 1. Top Panel (Filters & Search)
            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = SystemColors.Control,
                Padding = new Padding(15, 12, 15, 12)
            };

            var lblSearch = new Label
            {
                Text = "Rechercher (Nom/Tag) :",
                Location = new Point(15, 20),
                AutoSize = true,
                Font = new Font(this.Font, FontStyle.Bold)
            };

            txtSearch = new TextBox
            {
                Location = new Point(135, 17),
                Width = 200,
                Anchor = AnchorStyles.Left | AnchorStyles.Top,
                MaxLength = 200
            };
            txtSearch.TextChanged += TxtSearch_TextChanged;

            var lblCategory = new Label
            {
                Text = "Catégorie :",
                Location = new Point(365, 20),
                AutoSize = true,
                Font = new Font(this.Font, FontStyle.Bold)
            };

            cmbCategory = new ComboBox
            {
                Location = new Point(445, 17),
                Width = 180,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Anchor = AnchorStyles.Left | AnchorStyles.Top
            };
            cmbCategory.SelectedIndexChanged += CmbCategory_SelectedIndexChanged;

            topPanel.Controls.AddRange(new Control[] { lblSearch, txtSearch, lblCategory, cmbCategory });

            // 2. Right Sidebar Panel (Actions)
            var rightPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 145,
                BackColor = SystemColors.Control,
                Padding = new Padding(10, 10, 10, 10)
            };

            btnAdd = new Button
            {
                Text = "Ajouter",
                Location = new Point(10, 15),
                Width = 125,
                Height = 35,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                UseVisualStyleBackColor = true
            };
            btnAdd.Click += BtnAdd_Click;

            btnEdit = new Button
            {
                Text = "Modifier",
                Location = new Point(10, 60),
                Width = 125,
                Height = 35,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                UseVisualStyleBackColor = true
            };
            btnEdit.Click += BtnEdit_Click;

            btnDuplicate = new Button
            {
                Text = "Dupliquer",
                Location = new Point(10, 105),
                Width = 125,
                Height = 35,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                UseVisualStyleBackColor = true
            };
            btnDuplicate.Click += BtnDuplicate_Click;

            btnDelete = new Button
            {
                Text = "Supprimer",
                Location = new Point(10, 150),
                Width = 125,
                Height = 35,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                UseVisualStyleBackColor = true,
                BackColor = Color.FromArgb(244, 67, 54),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnDelete.Click += BtnDelete_Click;

            rightPanel.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDuplicate, btnDelete });

            // 3. Center Area (DataGridView)
            dataGridViewTemplates = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToOrderColumns = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                GridColor = Color.LightGray,
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(245, 245, 245) }
            };

            // Bind to BindingList
            dataGridViewTemplates.DataSource = _filteredTemplates;

            // Assemble components onto the Form
            this.Controls.Add(dataGridViewTemplates);
            this.Controls.Add(rightPanel);
            this.Controls.Add(topPanel);
        }

        /// <summary>
        /// Loads the master template list from SQLite and updates the filters.
        /// </summary>
        private async Task LoadTemplatesAsync()
        {
            try
            {
                this.UseWaitCursor = true;
                _allTemplates = (await _repository.GetAllAsync()).ToList();

                PopulateCategoryFilter();
                ApplyFilters();
            }
            catch (Exception)
            {
                MessageBox.Show("Une erreur inattendue est survenue lors du chargement des gabarits depuis la base de données.", "Erreur de base de données", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.UseWaitCursor = false;
            }
        }

        /// <summary>
        /// Dynamically builds the distinct categories filter list.
        /// </summary>
        private void PopulateCategoryFilter()
        {
            cmbCategory.SelectedIndexChanged -= CmbCategory_SelectedIndexChanged;

            string? previousSelection = cmbCategory.SelectedItem?.ToString();
            cmbCategory.Items.Clear();
            cmbCategory.Items.Add("Toutes les catégories");

            var categories = _allTemplates
                .Select(t => t.Category)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c)
                .Cast<object>()
                .ToArray();

            cmbCategory.Items.AddRange(categories);

            if (previousSelection != null && cmbCategory.Items.Contains(previousSelection))
            {
                cmbCategory.SelectedItem = previousSelection;
            }
            else
            {
                cmbCategory.SelectedIndex = 0;
            }

            cmbCategory.SelectedIndexChanged += CmbCategory_SelectedIndexChanged;
        }

        /// <summary>
        /// Synchronizes the DataGridView's columns, formatting and hiding system attributes.
        /// </summary>
        private void ConfigureGridColumns()
        {
            // Hide primary keys and timestamp columns
            if (dataGridViewTemplates.Columns["Id"] is { } colId) colId.Visible = false;
            if (dataGridViewTemplates.Columns["CreatedAt"] is { } colCreated) colCreated.Visible = false;
            if (dataGridViewTemplates.Columns["UpdatedAt"] is { } colUpdated) colUpdated.Visible = false;

            // Style headers and adjust widths
            if (dataGridViewTemplates.Columns["Key"] is { } colKey)
            {
                colKey.HeaderText = "Nom de clé";
                colKey.Width = 160;
            }

            if (dataGridViewTemplates.Columns["Value"] is { } colValue)
            {
                colValue.HeaderText = "Gabarit de prompt";
                colValue.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }

            if (dataGridViewTemplates.Columns["Category"] is { } colCategory)
            {
                colCategory.HeaderText = "Catégorie";
                colCategory.Width = 120;
            }

            if (dataGridViewTemplates.Columns["Tags"] is { } colTags)
            {
                colTags.HeaderText = "Tags";
                colTags.Width = 140;
            }

            if (dataGridViewTemplates.Columns["UsageCount"] is { } colUsageCount)
            {
                colUsageCount.HeaderText = "Utilisé";
                colUsageCount.Width = 65;
                colUsageCount.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            if (dataGridViewTemplates.Columns["LastUsed"] is { } colLastUsed)
            {
                colLastUsed.HeaderText = "Dernière utilisation";
                colLastUsed.Width = 130;
            }
        }

        /// <summary>
        /// Filters the displayed templates based on the search query and selected category.
        /// </summary>
        private void ApplyFilters()
        {
            var searchText = txtSearch.Text.Trim();
            var selectedCategory = cmbCategory.SelectedItem?.ToString();

            var filtered = _allTemplates.AsEnumerable();

            // Text search (case-insensitive key and tags matching)
            if (!string.IsNullOrEmpty(searchText))
            {
                filtered = filtered.Where(t =>
                    t.Key.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    (t.Tags != null && t.Tags.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                );
            }

            // Category filter
            if (!string.IsNullOrEmpty(selectedCategory) && selectedCategory != "Toutes les catégories")
            {
                filtered = filtered.Where(t => string.Equals(t.Category, selectedCategory, StringComparison.OrdinalIgnoreCase));
            }

            // Temporarily suspend events to prevent excessive DataGridView repaints
            _filteredTemplates.RaiseListChangedEvents = false;
            _filteredTemplates.Clear();

            foreach (var template in filtered)
            {
                _filteredTemplates.Add(template);
            }

            _filteredTemplates.RaiseListChangedEvents = true;
            _filteredTemplates.ResetBindings();

            ConfigureGridColumns();
        }

        /// <summary>
        /// Safely retrieves the currently selected TemplateModel from the grid.
        /// </summary>
        /// <returns>The selected TemplateModel, or null if no row is selected.</returns>
        private TemplateModel? GetSelectedTemplate()
        {
            if (dataGridViewTemplates.SelectedRows.Count > 0)
            {
                return dataGridViewTemplates.SelectedRows[0].DataBoundItem as TemplateModel;
            }
            return null;
        }

        // --- Event Handlers ---

        private void TxtSearch_TextChanged(object? sender, EventArgs e)
        {
            ApplyFilters();
        }

        private void CmbCategory_SelectedIndexChanged(object? sender, EventArgs e)
        {
            ApplyFilters();
        }

        private async void BtnAdd_Click(object? sender, EventArgs e)
        {
            using var editor = new TemplateEditorForm(_repository);
            if (editor.ShowDialog(this) == DialogResult.OK)
            {
                await LoadTemplatesAsync();
            }
        }

        private async void BtnEdit_Click(object? sender, EventArgs e)
        {
            var selected = GetSelectedTemplate();
            if (selected == null)
            {
                MessageBox.Show("Veuillez sélectionner un modèle dans la liste à modifier.", "Sélection requise", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var editor = new TemplateEditorForm(_repository, selected);
            if (editor.ShowDialog(this) == DialogResult.OK)
            {
                await LoadTemplatesAsync();
            }
        }

        private async void BtnDuplicate_Click(object? sender, EventArgs e)
        {
            var selected = GetSelectedTemplate();
            if (selected == null)
            {
                MessageBox.Show("Veuillez sélectionner un gabarit dans la liste à dupliquer.", "Sélection requise", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                this.UseWaitCursor = true;

                // Generate a unique, non-colliding key (e.g. key_copy1, key_copy2, etc.)
                var baseKey = selected.Key;
                string newKey;
                int suffix = 1;

                do
                {
                    newKey = $"{baseKey}_copy{suffix}";
                    suffix++;
                } while (_allTemplates.Any(t => string.Equals(t.Key, newKey, StringComparison.OrdinalIgnoreCase)));

                var duplicate = new TemplateModel
                {
                    Key = newKey,
                    Value = selected.Value,
                    Category = selected.Category,
                    Tags = selected.Tags,
                    UsageCount = 0,
                    LastUsed = null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _repository.InsertAsync(duplicate);

                // Add duplicate to top of the master cache list
                _allTemplates.Insert(0, duplicate);

                PopulateCategoryFilter();
                ApplyFilters();

                // Scroll to and select the newly duplicated item in the grid
                foreach (DataGridViewRow row in dataGridViewTemplates.Rows)
                {
                    if (row.DataBoundItem is TemplateModel tm && tm.Id == duplicate.Id)
                    {
                        row.Selected = true;
                        dataGridViewTemplates.FirstDisplayedScrollingRowIndex = row.Index;
                        break;
                    }
                }

                MessageBox.Show($"Gabarit dupliqué avec succès sous le nom '{newKey}' !", "Duplication réussie", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception)
            {
                MessageBox.Show("Une erreur inattendue est survenue lors de la duplication du gabarit.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.UseWaitCursor = false;
            }
        }

        private async void BtnDelete_Click(object? sender, EventArgs e)
        {
            var selected = GetSelectedTemplate();
            if (selected == null)
            {
                MessageBox.Show("Veuillez sélectionner un gabarit dans la liste à supprimer.", "Sélection requise", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirmResult = MessageBox.Show(
                $"Êtes-vous sûr de vouloir supprimer définitivement le gabarit '{selected.Key}' ?",
                "Confirmer la suppression",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (confirmResult == DialogResult.Yes)
            {
                try
                {
                    this.UseWaitCursor = true;
                    bool deleted = await _repository.DeleteAsync(selected.Key);

                    if (deleted)
                    {
                        _allTemplates.Remove(selected);
                        PopulateCategoryFilter();
                        ApplyFilters();
                    }
                    else
                    {
                        MessageBox.Show("Impossible de trouver le gabarit dans la base de données pour le supprimer.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("Une erreur inattendue est survenue lors de la suppression du gabarit.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    this.UseWaitCursor = false;
                }
            }
        }
    }
}