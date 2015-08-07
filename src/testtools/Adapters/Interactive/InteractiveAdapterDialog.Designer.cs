// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 

namespace Microsoft.Protocols.TestTools
{
    partial class InteractiveAdapterDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Cleans up any resources being used.
        /// </summary>
        /// <param name="disposing">True if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.label1 = new System.Windows.Forms.Label();
            this.textHelpMessage = new System.Windows.Forms.TextBox();
            this.labelActionParameters = new System.Windows.Forms.Label();
            this.dataGridViewActionPara = new System.Windows.Forms.DataGridView();
            this.ColumnName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColumnValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.labelActionResult = new System.Windows.Forms.Label();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.btnProperties = new System.Windows.Forms.Button();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnAbort = new System.Windows.Forms.Button();
            this.tableActionResult = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewActionPara)).BeginInit();
            this.tableLayoutPanel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.textHelpMessage, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.labelActionParameters, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.dataGridViewActionPara, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.labelActionResult, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 0, 6);
            this.tableLayoutPanel1.Controls.Add(this.tableActionResult, 0, 5);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.Padding = new System.Windows.Forms.Padding(10);
            this.tableLayoutPanel1.RowCount = 7;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(392, 476);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 17);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(75, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Help Message";
            // 
            // textHelpMessage
            // 
            this.textHelpMessage.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textHelpMessage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textHelpMessage.Location = new System.Drawing.Point(13, 33);
            this.textHelpMessage.Multiline = true;
            this.textHelpMessage.Name = "textHelpMessage";
            this.textHelpMessage.ReadOnly = true;
            this.textHelpMessage.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textHelpMessage.Size = new System.Drawing.Size(366, 134);
            this.textHelpMessage.TabIndex = 1;
            // 
            // labelActionParameters
            // 
            this.labelActionParameters.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelActionParameters.AutoSize = true;
            this.labelActionParameters.Location = new System.Drawing.Point(13, 177);
            this.labelActionParameters.Name = "labelActionParameters";
            this.labelActionParameters.Size = new System.Drawing.Size(93, 13);
            this.labelActionParameters.TabIndex = 2;
            this.labelActionParameters.Text = "Action Parameters";
            // 
            // dataGridViewActionPara
            // 
            this.dataGridViewActionPara.AllowUserToAddRows = false;
            this.dataGridViewActionPara.AllowUserToDeleteRows = false;
            this.dataGridViewActionPara.AllowUserToResizeColumns = false;
            this.dataGridViewActionPara.AllowUserToResizeRows = false;
            this.dataGridViewActionPara.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dataGridViewActionPara.BackgroundColor = System.Drawing.SystemColors.Control;
            this.dataGridViewActionPara.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dataGridViewActionPara.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewActionPara.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ColumnName,
            this.ColumnValue});
            this.dataGridViewActionPara.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridViewActionPara.Location = new System.Drawing.Point(13, 193);
            this.dataGridViewActionPara.Name = "dataGridViewActionPara";
            this.dataGridViewActionPara.ReadOnly = true;
            this.dataGridViewActionPara.RowHeadersVisible = false;
            this.dataGridViewActionPara.Size = new System.Drawing.Size(366, 114);
            this.dataGridViewActionPara.TabIndex = 3;
            // 
            // ColumnName
            // 
            this.ColumnName.DataPropertyName = "Name";
            this.ColumnName.HeaderText = "Name";
            this.ColumnName.Name = "ColumnName";
            this.ColumnName.ReadOnly = true;
            // 
            // ColumnValue
            // 
            this.ColumnValue.DataPropertyName = "Value";
            this.ColumnValue.HeaderText = "Value";
            this.ColumnValue.Name = "ColumnValue";
            this.ColumnValue.ReadOnly = true;
            // 
            // labelActionResult
            // 
            this.labelActionResult.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelActionResult.AutoSize = true;
            this.labelActionResult.Location = new System.Drawing.Point(13, 317);
            this.labelActionResult.Name = "labelActionResult";
            this.labelActionResult.Size = new System.Drawing.Size(70, 13);
            this.labelActionResult.TabIndex = 4;
            this.labelActionResult.Text = "Action Result";
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 4;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.tableLayoutPanel2.Controls.Add(this.btnProperties, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.btnOK, 2, 0);
            this.tableLayoutPanel2.Controls.Add(this.btnAbort, 3, 0);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(13, 433);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 1;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(366, 30);
            this.tableLayoutPanel2.TabIndex = 5;
            // 
            // btnProperties
            // 
            this.btnProperties.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnProperties.Location = new System.Drawing.Point(3, 3);
            this.btnProperties.Name = "btnProperties";
            this.btnProperties.Size = new System.Drawing.Size(74, 24);
            this.btnProperties.TabIndex = 0;
            this.btnProperties.Text = "Properties";
            this.btnProperties.UseVisualStyleBackColor = true;
            this.btnProperties.Click += new System.EventHandler(this.btnProperties_Click);
            // 
            // btnOK
            // 
            this.btnOK.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnOK.Location = new System.Drawing.Point(209, 3);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(74, 24);
            this.btnOK.TabIndex = 1;
            this.btnOK.Text = "Continue";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnAbort
            // 
            this.btnAbort.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnAbort.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnAbort.Location = new System.Drawing.Point(289, 3);
            this.btnAbort.Name = "btnAbort";
            this.btnAbort.Size = new System.Drawing.Size(74, 24);
            this.btnAbort.TabIndex = 2;
            this.btnAbort.Text = "Abort";
            this.btnAbort.UseVisualStyleBackColor = true;
            this.btnAbort.Click += new System.EventHandler(this.btnAbort_Click);
            // 
            // tableActionResult
            // 
            this.tableActionResult.AutoScroll = true;
            this.tableActionResult.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Single;
            this.tableActionResult.ColumnCount = 2;
            this.tableActionResult.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableActionResult.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableActionResult.Dock = System.Windows.Forms.DockStyle.Top;
            this.tableActionResult.Location = new System.Drawing.Point(13, 333);
            this.tableActionResult.Name = "tableActionResult";
            this.tableActionResult.RowCount = 1;
            this.tableActionResult.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.tableActionResult.Size = new System.Drawing.Size(366, 94);
            this.tableActionResult.TabIndex = 6;
            // 
            // InteractiveAdapterDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(392, 476);
            this.Controls.Add(this.tableLayoutPanel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.MinimumSize = new System.Drawing.Size(300, 400);
            this.Name = "InteractiveAdapterDialog";
            this.Text = "Interactive Adapter";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.InteractiveAdapterDialog_Load);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewActionPara)).EndInit();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textHelpMessage;
        private System.Windows.Forms.Label labelActionParameters;
        private System.Windows.Forms.DataGridView dataGridViewActionPara;
        private System.Windows.Forms.Label labelActionResult;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Button btnProperties;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnAbort;
        private System.Windows.Forms.TableLayoutPanel tableActionResult;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColumnName;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColumnValue;
    }
}