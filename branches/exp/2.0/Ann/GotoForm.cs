using System;
using System.Windows.Forms;
using Debug = System.Diagnostics.Debug;

namespace Sgry.Ann
{
	public partial class GotoForm : Form
	{
		public GotoForm()
		{
			InitializeComponent();
			Font = SystemInformation.MenuFont;
		}

		public int LineNumber
		{
			get{ return Int32.Parse(_LineNumTextBox.Text); }
			set{ _LineNumTextBox.Text = value.ToString(); }
		}

		void _LineNumTextBox_Enter( object sender, EventArgs e )
		{
			_LineNumTextBox.SelectAll();
		}

		void _LineNumTextBox_KeyDown(object sender, KeyEventArgs e)
		{
			int lineNum;

			if( e.KeyCode == Keys.Up || e.KeyCode == Keys.Down )
			{
				if( _LineNumTextBox.Text == "" )
				{
					lineNum = 1;
				}
				else if( e.KeyData == Keys.Up )
				{
					lineNum = Math.Min( Int32.Parse(_LineNumTextBox.Text)+1,
										Int32.MaxValue );
				}
				else if( e.KeyData == (Keys.Up | Keys.Shift) )
				{
					lineNum = Math.Min( Int32.Parse(_LineNumTextBox.Text)+10,
										Int32.MaxValue );
				}
				else if( e.KeyData == Keys.Down )
				{
					lineNum = Math.Max( Int32.Parse(_LineNumTextBox.Text)-1,
										1 );
				}
				else// if( e.KeyData == (Keys.Down | Keys.Shift) )
				{
					Debug.Assert( e.KeyData == (Keys.Down | Keys.Shift) );
					lineNum = Math.Max( Int32.Parse(_LineNumTextBox.Text)-10,
										1 );
				}
				_LineNumTextBox.Text = lineNum.ToString();
				_LineNumTextBox.SelectAll();
				e.Handled = true;
			}
		}

		void _LineNumTextBox_KeyPress( object sender, KeyPressEventArgs e )
		{
			if( (e.KeyChar < '0' || '9' < e.KeyChar)
				&& e.KeyChar != '\b' )
				e.Handled = true;
		}

		void _OkButton_Click( object sender, EventArgs e )
		{
			if( MyValidate() == false )
			{
				MessageBox.Show( "Please enter a valid line number." );
				_LineNumTextBox.SelectAll();
				return;
			}

			DialogResult = DialogResult.OK;
		}

		bool MyValidate()
		{
			int lineNum;
			string lineNumStr = _LineNumTextBox.Text;

			try
			{
				lineNum = Int32.Parse( lineNumStr );
				if( 0 < lineNum )
				{
					return true;
				}
			}
			catch( FormatException )
			{}
			catch( OverflowException )
			{}

			return false;
		}

		private TableLayoutPanel tableLayoutPanel1;
		private TableLayoutPanel tableLayoutPanel2;

		#region Windows Form Designer generated code
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose( bool disposing )
		{
			if( disposing && (components != null) )
			{
				components.Dispose();
			}
			base.Dispose( disposing );
		}

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this._LineNumLabel = new System.Windows.Forms.Label();
			this._LineNumTextBox = new System.Windows.Forms.TextBox();
			this._OkButton = new System.Windows.Forms.Button();
			this._CancelButton = new System.Windows.Forms.Button();
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
			this.tableLayoutPanel1.SuspendLayout();
			this.tableLayoutPanel2.SuspendLayout();
			this.SuspendLayout();
			// 
			// _LineNumLabel
			// 
			this._LineNumLabel.AutoSize = true;
			this._LineNumLabel.Dock = System.Windows.Forms.DockStyle.Left;
			this._LineNumLabel.Location = new System.Drawing.Point( 5, 0 );
			this._LineNumLabel.Margin = new System.Windows.Forms.Padding( 5, 0, 5, 0 );
			this._LineNumLabel.Name = "_LineNumLabel";
			this._LineNumLabel.Size = new System.Drawing.Size( 85, 29 );
			this._LineNumLabel.TabIndex = 0;
			this._LineNumLabel.Text = "Line &number:";
			this._LineNumLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// _LineNumTextBox
			// 
			this._LineNumTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this._LineNumTextBox.Location = new System.Drawing.Point( 98, 3 );
			this._LineNumTextBox.MaxLength = 10;
			this._LineNumTextBox.Name = "_LineNumTextBox";
			this._LineNumTextBox.Size = new System.Drawing.Size( 131, 23 );
			this._LineNumTextBox.TabIndex = 1;
			this._LineNumTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			this._LineNumTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler( this._LineNumTextBox_KeyDown );
			this._LineNumTextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler( this._LineNumTextBox_KeyPress );
			this._LineNumTextBox.Enter += new System.EventHandler( this._LineNumTextBox_Enter );
			// 
			// _OkButton
			// 
			this._OkButton.Location = new System.Drawing.Point( 60, 0 );
			this._OkButton.Margin = new System.Windows.Forms.Padding( 6, 0, 6, 12 );
			this._OkButton.Name = "_OkButton";
			this._OkButton.Size = new System.Drawing.Size( 87, 29 );
			this._OkButton.TabIndex = 0;
			this._OkButton.Text = "OK";
			this._OkButton.Click += new System.EventHandler( this._OkButton_Click );
			// 
			// _CancelButton
			// 
			this._CancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this._CancelButton.Location = new System.Drawing.Point( 159, 0 );
			this._CancelButton.Margin = new System.Windows.Forms.Padding( 6, 0, 12, 12 );
			this._CancelButton.Name = "_CancelButton";
			this._CancelButton.Size = new System.Drawing.Size( 87, 29 );
			this._CancelButton.TabIndex = 1;
			this._CancelButton.Text = "Cancel";
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.AutoSize = true;
			this.tableLayoutPanel1.ColumnCount = 3;
			this.tableLayoutPanel1.ColumnStyles.Add( new System.Windows.Forms.ColumnStyle( System.Windows.Forms.SizeType.Percent, 100F ) );
			this.tableLayoutPanel1.ColumnStyles.Add( new System.Windows.Forms.ColumnStyle() );
			this.tableLayoutPanel1.ColumnStyles.Add( new System.Windows.Forms.ColumnStyle() );
			this.tableLayoutPanel1.Controls.Add( this._OkButton, 1, 0 );
			this.tableLayoutPanel1.Controls.Add( this._CancelButton, 2, 0 );
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.tableLayoutPanel1.Location = new System.Drawing.Point( 0, 53 );
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 1;
			this.tableLayoutPanel1.RowStyles.Add( new System.Windows.Forms.RowStyle( System.Windows.Forms.SizeType.Percent, 100F ) );
			this.tableLayoutPanel1.Size = new System.Drawing.Size( 258, 41 );
			this.tableLayoutPanel1.TabIndex = 1;
			// 
			// tableLayoutPanel2
			// 
			this.tableLayoutPanel2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.tableLayoutPanel2.ColumnCount = 2;
			this.tableLayoutPanel2.ColumnStyles.Add( new System.Windows.Forms.ColumnStyle() );
			this.tableLayoutPanel2.ColumnStyles.Add( new System.Windows.Forms.ColumnStyle() );
			this.tableLayoutPanel2.Controls.Add( this._LineNumLabel, 0, 0 );
			this.tableLayoutPanel2.Controls.Add( this._LineNumTextBox, 1, 0 );
			this.tableLayoutPanel2.Location = new System.Drawing.Point( 12, 12 );
			this.tableLayoutPanel2.Name = "tableLayoutPanel2";
			this.tableLayoutPanel2.RowCount = 2;
			this.tableLayoutPanel2.RowStyles.Add( new System.Windows.Forms.RowStyle() );
			this.tableLayoutPanel2.RowStyles.Add( new System.Windows.Forms.RowStyle() );
			this.tableLayoutPanel2.Size = new System.Drawing.Size( 232, 38 );
			this.tableLayoutPanel2.TabIndex = 0;
			// 
			// GotoForm
			// 
			this.AcceptButton = this._OkButton;
			this.AutoScaleDimensions = new System.Drawing.SizeF( 96F, 96F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.CancelButton = this._CancelButton;
			this.ClientSize = new System.Drawing.Size( 258, 94 );
			this.Controls.Add( this.tableLayoutPanel2 );
			this.Controls.Add( this.tableLayoutPanel1 );
			this.Font = new System.Drawing.Font( "Meiryo UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)) );
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.ImeMode = System.Windows.Forms.ImeMode.Disable;
			this.Margin = new System.Windows.Forms.Padding( 5, 4, 5, 4 );
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "GotoForm";
			this.ShowIcon = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Go to line";
			this.tableLayoutPanel1.ResumeLayout( false );
			this.tableLayoutPanel2.ResumeLayout( false );
			this.tableLayoutPanel2.PerformLayout();
			this.ResumeLayout( false );
			this.PerformLayout();

		}

		private System.Windows.Forms.Label _LineNumLabel;
		private System.Windows.Forms.Button _OkButton;
		private System.Windows.Forms.Button _CancelButton;
		private System.Windows.Forms.TextBox _LineNumTextBox;
		#endregion
	}
}
