namespace TestLab
{
	partial class Form1
	{
		/// <summary>
		///  Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		///  Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
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
		///  Required method for Designer support - do not modify
		///  the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			dgrid = new DataGridView();
			dgrid1 = new DataGridView();
			button1 = new Button();
			((System.ComponentModel.ISupportInitialize)dgrid).BeginInit();
			((System.ComponentModel.ISupportInitialize)dgrid1).BeginInit();
			SuspendLayout();
			// 
			// dgrid
			// 
			dgrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			dgrid.Location = new Point(12, 12);
			dgrid.Name = "dgrid";
			dgrid.RowTemplate.Height = 25;
			dgrid.Size = new Size(624, 150);
			dgrid.TabIndex = 0;
			// 
			// dgrid1
			// 
			dgrid1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			dgrid1.Location = new Point(12, 204);
			dgrid1.Name = "dgrid1";
			dgrid1.RowTemplate.Height = 25;
			dgrid1.Size = new Size(624, 150);
			dgrid1.TabIndex = 0;
			// 
			// button1
			// 
			button1.Location = new Point(561, 403);
			button1.Name = "button1";
			button1.Size = new Size(75, 23);
			button1.TabIndex = 1;
			button1.Text = "button1";
			button1.UseVisualStyleBackColor = true;
			button1.Click += button1_Click;
			// 
			// Form1
			// 
			AutoScaleDimensions = new SizeF(7F, 15F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(648, 450);
			Controls.Add(button1);
			Controls.Add(dgrid1);
			Controls.Add(dgrid);
			Name = "Form1";
			StartPosition = FormStartPosition.CenterScreen;
			Text = "Form1";
			((System.ComponentModel.ISupportInitialize)dgrid).EndInit();
			((System.ComponentModel.ISupportInitialize)dgrid1).EndInit();
			ResumeLayout(false);
		}

		#endregion

		private DataGridView dgrid;
		private DataGridView dgrid1;
		private Button button1;
	}
}