using System.Windows.Forms;

namespace Sgry.Ann
{
	partial class DrawingOptionForm : Form
	{
		void InitializeComponent()
		{
			this._Label_Fonts = new System.Windows.Forms.Label();
			this._Label_FontSize = new System.Windows.Forms.Label();
			this._Check_DrawsSpace = new System.Windows.Forms.CheckBox();
			this._Check_DrawsTab = new System.Windows.Forms.CheckBox();
			this._Check_DrawsFullWidthSpace = new System.Windows.Forms.CheckBox();
			this._Check_DrawsEolCode = new System.Windows.Forms.CheckBox();
			this._Check_HighlightCurrentLine = new System.Windows.Forms.CheckBox();
			this._Check_ShowsLineNumber = new System.Windows.Forms.CheckBox();
			this._Check_ShowsHRuler = new System.Windows.Forms.CheckBox();
			this._Check_ShowsDirtBar = new System.Windows.Forms.CheckBox();
			this._Label_TabWidth = new System.Windows.Forms.Label();
			this._Button_OK = new System.Windows.Forms.Button();
			this._Button_Cancel = new System.Windows.Forms.Button();
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this._Num_TabWidth = new System.Windows.Forms.NumericUpDown();
			this._Combo_Fonts = new System.Windows.Forms.ComboBox();
			this._Num_FontSize = new System.Windows.Forms.NumericUpDown();
			this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
			this.tableLayoutPanel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this._Num_TabWidth)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this._Num_FontSize)).BeginInit();
			this.flowLayoutPanel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// _Label_Fonts
			// 
			this._Label_Fonts.AutoSize = true;
			this._Label_Fonts.Dock = System.Windows.Forms.DockStyle.Left;
			this._Label_Fonts.Location = new System.Drawing.Point(3, 0);
			this._Label_Fonts.Name = "_Label_Fonts";
			this._Label_Fonts.Size = new System.Drawing.Size(75, 29);
			this._Label_Fonts.TabIndex = 0;
			this._Label_Fonts.Text = "Font name:";
			this._Label_Fonts.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// _Label_FontSize
			// 
			this._Label_FontSize.AutoSize = true;
			this._Label_FontSize.Dock = System.Windows.Forms.DockStyle.Left;
			this._Label_FontSize.Location = new System.Drawing.Point(3, 29);
			this._Label_FontSize.Name = "_Label_FontSize";
			this._Label_FontSize.Size = new System.Drawing.Size(36, 29);
			this._Label_FontSize.TabIndex = 2;
			this._Label_FontSize.Text = "Size:";
			this._Label_FontSize.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// _Check_DrawsSpace
			// 
			this._Check_DrawsSpace.AutoSize = true;
			this.tableLayoutPanel1.SetColumnSpan(this._Check_DrawsSpace, 2);
			this._Check_DrawsSpace.Location = new System.Drawing.Point(3, 61);
			this._Check_DrawsSpace.Name = "_Check_DrawsSpace";
			this._Check_DrawsSpace.Size = new System.Drawing.Size(100, 19);
			this._Check_DrawsSpace.TabIndex = 4;
			this._Check_DrawsSpace.Text = "Draws &space";
			// 
			// _Check_DrawsTab
			// 
			this._Check_DrawsTab.AutoSize = true;
			this.tableLayoutPanel1.SetColumnSpan(this._Check_DrawsTab, 2);
			this._Check_DrawsTab.Location = new System.Drawing.Point(3, 86);
			this._Check_DrawsTab.Name = "_Check_DrawsTab";
			this._Check_DrawsTab.Size = new System.Drawing.Size(86, 19);
			this._Check_DrawsTab.TabIndex = 5;
			this._Check_DrawsTab.Text = "Draws &tab";
			// 
			// _Check_DrawsFullWidthSpace
			// 
			this._Check_DrawsFullWidthSpace.AutoSize = true;
			this.tableLayoutPanel1.SetColumnSpan(this._Check_DrawsFullWidthSpace, 2);
			this._Check_DrawsFullWidthSpace.Location = new System.Drawing.Point(3, 111);
			this._Check_DrawsFullWidthSpace.Name = "_Check_DrawsFullWidthSpace";
			this._Check_DrawsFullWidthSpace.Size = new System.Drawing.Size(218, 19);
			this._Check_DrawsFullWidthSpace.TabIndex = 6;
			this._Check_DrawsFullWidthSpace.Text = "Draws &full width space (U+3000)";
			// 
			// _Check_DrawsEolCode
			// 
			this._Check_DrawsEolCode.AutoSize = true;
			this.tableLayoutPanel1.SetColumnSpan(this._Check_DrawsEolCode, 2);
			this._Check_DrawsEolCode.Location = new System.Drawing.Point(3, 136);
			this._Check_DrawsEolCode.Name = "_Check_DrawsEolCode";
			this._Check_DrawsEolCode.Size = new System.Drawing.Size(195, 19);
			this._Check_DrawsEolCode.TabIndex = 7;
			this._Check_DrawsEolCode.Text = "Draws EO&L (end of line) code";
			// 
			// _Check_HighlightCurrentLine
			// 
			this._Check_HighlightCurrentLine.AutoSize = true;
			this.tableLayoutPanel1.SetColumnSpan(this._Check_HighlightCurrentLine, 2);
			this._Check_HighlightCurrentLine.Location = new System.Drawing.Point(3, 161);
			this._Check_HighlightCurrentLine.Name = "_Check_HighlightCurrentLine";
			this._Check_HighlightCurrentLine.Size = new System.Drawing.Size(153, 19);
			this._Check_HighlightCurrentLine.TabIndex = 8;
			this._Check_HighlightCurrentLine.Text = "Highlights &current line";
			// 
			// _Check_ShowsLineNumber
			// 
			this._Check_ShowsLineNumber.AutoSize = true;
			this.tableLayoutPanel1.SetColumnSpan(this._Check_ShowsLineNumber, 2);
			this._Check_ShowsLineNumber.Location = new System.Drawing.Point(3, 186);
			this._Check_ShowsLineNumber.Name = "_Check_ShowsLineNumber";
			this._Check_ShowsLineNumber.Size = new System.Drawing.Size(137, 19);
			this._Check_ShowsLineNumber.TabIndex = 9;
			this._Check_ShowsLineNumber.Text = "Shows line &number";
			// 
			// _Check_ShowsHRuler
			// 
			this._Check_ShowsHRuler.AutoSize = true;
			this.tableLayoutPanel1.SetColumnSpan(this._Check_ShowsHRuler, 2);
			this._Check_ShowsHRuler.Location = new System.Drawing.Point(3, 211);
			this._Check_ShowsHRuler.Name = "_Check_ShowsHRuler";
			this._Check_ShowsHRuler.Size = new System.Drawing.Size(156, 19);
			this._Check_ShowsHRuler.TabIndex = 10;
			this._Check_ShowsHRuler.Text = "Shows &horizontal ruler";
			// 
			// _Check_ShowsDirtBar
			// 
			this._Check_ShowsDirtBar.AutoSize = true;
			this.tableLayoutPanel1.SetColumnSpan(this._Check_ShowsDirtBar, 2);
			this._Check_ShowsDirtBar.Location = new System.Drawing.Point(3, 236);
			this._Check_ShowsDirtBar.Name = "_Check_ShowsDirtBar";
			this._Check_ShowsDirtBar.Size = new System.Drawing.Size(111, 19);
			this._Check_ShowsDirtBar.TabIndex = 11;
			this._Check_ShowsDirtBar.Text = "Shows &dirt bar";
			// 
			// _Label_TabWidth
			// 
			this._Label_TabWidth.AutoSize = true;
			this._Label_TabWidth.Dock = System.Windows.Forms.DockStyle.Left;
			this._Label_TabWidth.Location = new System.Drawing.Point(3, 258);
			this._Label_TabWidth.Name = "_Label_TabWidth";
			this._Label_TabWidth.Size = new System.Drawing.Size(69, 29);
			this._Label_TabWidth.TabIndex = 12;
			this._Label_TabWidth.Text = "Tab &width:";
			this._Label_TabWidth.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// _Button_OK
			// 
			this._Button_OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._Button_OK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this._Button_OK.Location = new System.Drawing.Point(54, 6);
			this._Button_OK.Margin = new System.Windows.Forms.Padding(6);
			this._Button_OK.Name = "_Button_OK";
			this._Button_OK.Size = new System.Drawing.Size(87, 26);
			this._Button_OK.TabIndex = 0;
			this._Button_OK.Text = "OK";
			// 
			// _Button_Cancel
			// 
			this._Button_Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._Button_Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this._Button_Cancel.Location = new System.Drawing.Point(153, 6);
			this._Button_Cancel.Margin = new System.Windows.Forms.Padding(6);
			this._Button_Cancel.Name = "_Button_Cancel";
			this._Button_Cancel.Size = new System.Drawing.Size(87, 26);
			this._Button_Cancel.TabIndex = 1;
			this._Button_Cancel.Text = "Cancel";
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.AutoSize = true;
			this.tableLayoutPanel1.ColumnCount = 2;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Controls.Add(this._Label_Fonts, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this._Num_TabWidth, 1, 10);
			this.tableLayoutPanel1.Controls.Add(this._Label_TabWidth, 0, 10);
			this.tableLayoutPanel1.Controls.Add(this._Check_ShowsDirtBar, 0, 9);
			this.tableLayoutPanel1.Controls.Add(this._Check_ShowsHRuler, 0, 8);
			this.tableLayoutPanel1.Controls.Add(this._Check_ShowsLineNumber, 0, 7);
			this.tableLayoutPanel1.Controls.Add(this._Check_HighlightCurrentLine, 0, 6);
			this.tableLayoutPanel1.Controls.Add(this._Check_DrawsEolCode, 0, 5);
			this.tableLayoutPanel1.Controls.Add(this._Check_DrawsFullWidthSpace, 0, 4);
			this.tableLayoutPanel1.Controls.Add(this._Check_DrawsTab, 0, 3);
			this.tableLayoutPanel1.Controls.Add(this._Check_DrawsSpace, 0, 2);
			this.tableLayoutPanel1.Controls.Add(this._Label_FontSize, 0, 1);
			this.tableLayoutPanel1.Controls.Add(this._Combo_Fonts, 1, 0);
			this.tableLayoutPanel1.Controls.Add(this._Num_FontSize, 1, 1);
			this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 1, 11);
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel1.Location = new System.Drawing.Point(6, 6);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 12;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.tableLayoutPanel1.Size = new System.Drawing.Size(333, 346);
			this.tableLayoutPanel1.TabIndex = 0;
			// 
			// _Num_TabWidth
			// 
			this._Num_TabWidth.Location = new System.Drawing.Point(84, 261);
			this._Num_TabWidth.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this._Num_TabWidth.Name = "_Num_TabWidth";
			this._Num_TabWidth.Size = new System.Drawing.Size(62, 23);
			this._Num_TabWidth.TabIndex = 13;
			this._Num_TabWidth.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			// 
			// _Combo_Fonts
			// 
			this._Combo_Fonts.Dock = System.Windows.Forms.DockStyle.Fill;
			this._Combo_Fonts.Location = new System.Drawing.Point(84, 3);
			this._Combo_Fonts.Name = "_Combo_Fonts";
			this._Combo_Fonts.Size = new System.Drawing.Size(246, 23);
			this._Combo_Fonts.TabIndex = 1;
			// 
			// _Num_FontSize
			// 
			this._Num_FontSize.Location = new System.Drawing.Point(84, 32);
			this._Num_FontSize.Maximum = new decimal(new int[] {
            128,
            0,
            0,
            0});
			this._Num_FontSize.Minimum = new decimal(new int[] {
            6,
            0,
            0,
            0});
			this._Num_FontSize.Name = "_Num_FontSize";
			this._Num_FontSize.Size = new System.Drawing.Size(67, 23);
			this._Num_FontSize.TabIndex = 3;
			this._Num_FontSize.Value = new decimal(new int[] {
            6,
            0,
            0,
            0});
			// 
			// flowLayoutPanel1
			// 
			this.flowLayoutPanel1.Controls.Add(this._Button_Cancel);
			this.flowLayoutPanel1.Controls.Add(this._Button_OK);
			this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
			this.flowLayoutPanel1.Location = new System.Drawing.Point(84, 290);
			this.flowLayoutPanel1.Name = "flowLayoutPanel1";
			this.flowLayoutPanel1.Size = new System.Drawing.Size(246, 53);
			this.flowLayoutPanel1.TabIndex = 14;
			this.flowLayoutPanel1.WrapContents = false;
			// 
			// DrawingOptionForm
			// 
			this.AcceptButton = this._Button_OK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoScroll = true;
			this.AutoSize = true;
			this.CancelButton = this._Button_Cancel;
			this.ClientSize = new System.Drawing.Size(345, 352);
			this.Controls.Add(this.tableLayoutPanel1);
			this.Font = new System.Drawing.Font("Meiryo UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
			this.MinimizeBox = false;
			this.Name = "DrawingOptionForm";
			this.Padding = new System.Windows.Forms.Padding(6, 6, 6, 0);
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Ann - Drawing Options";
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this._Num_TabWidth)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this._Num_FontSize)).EndInit();
			this.flowLayoutPanel1.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#region UI Components
		Label _Label_Fonts;
		ComboBox _Combo_Fonts;
		Label _Label_FontSize;
		NumericUpDown _Num_FontSize;
		CheckBox _Check_DrawsSpace;
		CheckBox _Check_DrawsTab;
		CheckBox _Check_DrawsEolCode;
		CheckBox _Check_DrawsFullWidthSpace;
		CheckBox _Check_HighlightCurrentLine;
		CheckBox _Check_ShowsLineNumber;
		CheckBox _Check_ShowsHRuler;
		CheckBox _Check_ShowsDirtBar;
		Label _Label_TabWidth;
		NumericUpDown _Num_TabWidth;
		Button _Button_OK;
		Button _Button_Cancel;
		TableLayoutPanel tableLayoutPanel1;
		#endregion
		private FlowLayoutPanel flowLayoutPanel1;
	}
}
