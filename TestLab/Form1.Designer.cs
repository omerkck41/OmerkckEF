﻿namespace TestLab
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
			((System.ComponentModel.ISupportInitialize)dgrid).BeginInit();
			SuspendLayout();
			// 
			// dgrid
			// 
			dgrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			dgrid.Location = new Point(12, 12);
			dgrid.Name = "dgrid";
			dgrid.RowTemplate.Height = 25;
			dgrid.Size = new Size(240, 150);
			dgrid.TabIndex = 0;
			// 
			// Form1
			// 
			AutoScaleDimensions = new SizeF(7F, 15F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(800, 450);
			Controls.Add(dgrid);
			Name = "Form1";
			Text = "Form1";
			((System.ComponentModel.ISupportInitialize)dgrid).EndInit();
			ResumeLayout(false);
		}

		#endregion

		private DataGridView dgrid;
	}
}