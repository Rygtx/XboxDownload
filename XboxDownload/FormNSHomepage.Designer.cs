
namespace XboxDownload
{
    partial class FormNSHomepage
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
            this.label1 = new System.Windows.Forms.Label();
            this.tbNSHomepage = new System.Windows.Forms.TextBox();
            this.butSubmit = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 25);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(98, 18);
            this.label1.TabIndex = 0;
            this.label1.Text = "主页地址：";
            // 
            // tbNSHomepage
            // 
            this.tbNSHomepage.Location = new System.Drawing.Point(116, 20);
            this.tbNSHomepage.Name = "tbNSHomepage";
            this.tbNSHomepage.Size = new System.Drawing.Size(391, 28);
            this.tbNSHomepage.TabIndex = 1;
            // 
            // butSubmit
            // 
            this.butSubmit.Location = new System.Drawing.Point(513, 17);
            this.butSubmit.Name = "butSubmit";
            this.butSubmit.Size = new System.Drawing.Size(112, 34);
            this.butSubmit.TabIndex = 2;
            this.butSubmit.Text = "提交";
            this.butSubmit.UseVisualStyleBackColor = true;
            this.butSubmit.Click += new System.EventHandler(this.ButSubmit_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 64);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(539, 72);
            this.label2.TabIndex = 3;
            this.label2.Text = "使用说明：\r\n1、进入设置中的互联网设置，把DNS设置为本机IP。\r\n2、然后选择连接到此网络，随后Switch会显示连接网络需要验证。\r\n3、点击下一步将会弹出" +
    "浏览器并且自动打开设定的主页地址。";
            // 
            // FormNSHomepage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(144F, 144F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(641, 155);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.butSubmit);
            this.Controls.Add(this.tbNSHomepage);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormNSHomepage";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "设置NS浏览器主页";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox tbNSHomepage;
        private System.Windows.Forms.Button butSubmit;
        private System.Windows.Forms.Label label2;
    }
}