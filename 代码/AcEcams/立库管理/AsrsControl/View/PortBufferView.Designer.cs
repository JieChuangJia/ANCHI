namespace AsrsControl
{
    partial class PortBufferView
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.panel17 = new System.Windows.Forms.Panel();
            this.textBoxPallet = new System.Windows.Forms.TextBox();
            this.listBoxPallet = new System.Windows.Forms.ListBox();
            this.label17 = new System.Windows.Forms.Label();
            this.comboBoxPortin = new System.Windows.Forms.ComboBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnAddPallet = new System.Windows.Forms.Button();
            this.btnDel = new System.Windows.Forms.Button();
            this.btnDispBuf = new System.Windows.Forms.Button();
            this.btnClearBuf = new System.Windows.Forms.Button();
            this.label15 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.panel17.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel17
            // 
            this.panel17.Controls.Add(this.textBoxPallet);
            this.panel17.Controls.Add(this.listBoxPallet);
            this.panel17.Controls.Add(this.label17);
            this.panel17.Controls.Add(this.comboBoxPortin);
            this.panel17.Controls.Add(this.btnSave);
            this.panel17.Controls.Add(this.btnAddPallet);
            this.panel17.Controls.Add(this.btnDel);
            this.panel17.Controls.Add(this.btnDispBuf);
            this.panel17.Controls.Add(this.btnClearBuf);
            this.panel17.Controls.Add(this.label15);
            this.panel17.Controls.Add(this.label1);
            this.panel17.Controls.Add(this.label14);
            this.panel17.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel17.Location = new System.Drawing.Point(0, 0);
            this.panel17.Name = "panel17";
            this.panel17.Size = new System.Drawing.Size(1044, 496);
            this.panel17.TabIndex = 2;
            // 
            // textBoxPallet
            // 
            this.textBoxPallet.Font = new System.Drawing.Font("宋体", 13F);
            this.textBoxPallet.Location = new System.Drawing.Point(203, 42);
            this.textBoxPallet.Name = "textBoxPallet";
            this.textBoxPallet.Size = new System.Drawing.Size(377, 27);
            this.textBoxPallet.TabIndex = 12;
            // 
            // listBoxPallet
            // 
            this.listBoxPallet.Font = new System.Drawing.Font("宋体", 13F);
            this.listBoxPallet.FormattingEnabled = true;
            this.listBoxPallet.ItemHeight = 17;
            this.listBoxPallet.Location = new System.Drawing.Point(203, 114);
            this.listBoxPallet.Name = "listBoxPallet";
            this.listBoxPallet.Size = new System.Drawing.Size(518, 140);
            this.listBoxPallet.TabIndex = 11;
            // 
            // label17
            // 
            this.label17.BackColor = System.Drawing.SystemColors.Info;
            this.label17.Font = new System.Drawing.Font("宋体", 13F);
            this.label17.ForeColor = System.Drawing.Color.Red;
            this.label17.Location = new System.Drawing.Point(63, 304);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(767, 68);
            this.label17.TabIndex = 10;
            this.label17.Text = "说明:系统记录了各入库口等待入库的托盘信息，如实际和当前记录的缓存信息有出入, 则可以\r\n\r\n手动修正或清除缓存信息。";
            // 
            // comboBoxPortin
            // 
            this.comboBoxPortin.Font = new System.Drawing.Font("宋体", 13F);
            this.comboBoxPortin.FormattingEnabled = true;
            this.comboBoxPortin.Location = new System.Drawing.Point(62, 42);
            this.comboBoxPortin.Name = "comboBoxPortin";
            this.comboBoxPortin.Size = new System.Drawing.Size(121, 25);
            this.comboBoxPortin.TabIndex = 9;
            this.comboBoxPortin.SelectedIndexChanged += new System.EventHandler(this.comboBoxPortin_SelectedIndexChanged);
            // 
            // btnSave
            // 
            this.btnSave.Font = new System.Drawing.Font("宋体", 13F);
            this.btnSave.Location = new System.Drawing.Point(63, 221);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(121, 39);
            this.btnSave.TabIndex = 6;
            this.btnSave.Text = "保存";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnAddPallet
            // 
            this.btnAddPallet.Font = new System.Drawing.Font("宋体", 13F);
            this.btnAddPallet.Location = new System.Drawing.Point(600, 40);
            this.btnAddPallet.Name = "btnAddPallet";
            this.btnAddPallet.Size = new System.Drawing.Size(121, 29);
            this.btnAddPallet.TabIndex = 7;
            this.btnAddPallet.Text = "添加";
            this.btnAddPallet.UseVisualStyleBackColor = true;
            this.btnAddPallet.Click += new System.EventHandler(this.btnAddPallet_Click);
            // 
            // btnDel
            // 
            this.btnDel.Font = new System.Drawing.Font("宋体", 13F);
            this.btnDel.Location = new System.Drawing.Point(63, 129);
            this.btnDel.Name = "btnDel";
            this.btnDel.Size = new System.Drawing.Size(121, 42);
            this.btnDel.TabIndex = 7;
            this.btnDel.Text = "移除";
            this.btnDel.UseVisualStyleBackColor = true;
            this.btnDel.Click += new System.EventHandler(this.btnDel_Click);
            // 
            // btnDispBuf
            // 
            this.btnDispBuf.Font = new System.Drawing.Font("宋体", 13F);
            this.btnDispBuf.Location = new System.Drawing.Point(63, 83);
            this.btnDispBuf.Name = "btnDispBuf";
            this.btnDispBuf.Size = new System.Drawing.Size(121, 42);
            this.btnDispBuf.TabIndex = 7;
            this.btnDispBuf.Text = "刷新";
            this.btnDispBuf.UseVisualStyleBackColor = true;
            this.btnDispBuf.Click += new System.EventHandler(this.btnDispBuf_Click);
            // 
            // btnClearBuf
            // 
            this.btnClearBuf.Font = new System.Drawing.Font("宋体", 13F);
            this.btnClearBuf.Location = new System.Drawing.Point(62, 176);
            this.btnClearBuf.Name = "btnClearBuf";
            this.btnClearBuf.Size = new System.Drawing.Size(121, 40);
            this.btnClearBuf.TabIndex = 8;
            this.btnClearBuf.Text = "清空缓存";
            this.btnClearBuf.UseVisualStyleBackColor = true;
            this.btnClearBuf.Click += new System.EventHandler(this.btnClearBuf_Click);
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Font = new System.Drawing.Font("宋体", 13F);
            this.label15.Location = new System.Drawing.Point(200, 93);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(134, 18);
            this.label15.TabIndex = 4;
            this.label15.Text = "缓存的托盘列表";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("宋体", 13F);
            this.label1.Location = new System.Drawing.Point(200, 18);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(98, 18);
            this.label1.TabIndex = 5;
            this.label1.Text = "增加托盘号";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Font = new System.Drawing.Font("宋体", 13F);
            this.label14.Location = new System.Drawing.Point(60, 18);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(98, 18);
            this.label14.TabIndex = 5;
            this.label14.Text = "选择入库口";
            // 
            // PortBufferView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1044, 496);
            this.Controls.Add(this.panel17);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "PortBufferView";
            this.Text = "PortBufferView";
            this.Load += new System.EventHandler(this.PortBufferView_Load);
            this.panel17.ResumeLayout(false);
            this.panel17.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel17;
        private System.Windows.Forms.TextBox textBoxPallet;
        private System.Windows.Forms.ListBox listBoxPallet;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.ComboBox comboBoxPortin;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnAddPallet;
        private System.Windows.Forms.Button btnDel;
        private System.Windows.Forms.Button btnDispBuf;
        private System.Windows.Forms.Button btnClearBuf;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label14;

    }
}