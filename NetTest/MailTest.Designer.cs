namespace NetTest
{
    partial class MailTest
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.tabMail = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.proBar = new System.Windows.Forms.ProgressBar();
            this.labelMsg = new DevExpress.XtraEditors.LabelControl();
            this.btnSend = new DevExpress.XtraEditors.SimpleButton();
            this.labelControl4 = new DevExpress.XtraEditors.LabelControl();
            this.textBody = new System.Windows.Forms.RichTextBox();
            this.btnAttach = new DevExpress.XtraEditors.SimpleButton();
            this.txtAttach = new DevExpress.XtraEditors.TextEdit();
            this.labelControl3 = new DevExpress.XtraEditors.LabelControl();
            this.txtCap = new DevExpress.XtraEditors.TextEdit();
            this.labelControl2 = new DevExpress.XtraEditors.LabelControl();
            this.txtSend = new DevExpress.XtraEditors.TextEdit();
            this.labelControl1 = new DevExpress.XtraEditors.LabelControl();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.panelPop3 = new DevExpress.XtraEditors.PanelControl();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.labelShowPop3 = new DevExpress.XtraEditors.LabelControl();
            this.btnStopPop3 = new System.Windows.Forms.Button();
            this.btnConPop3 = new System.Windows.Forms.Button();
            this.changeButton = new System.Windows.Forms.Button();
            this.textTo = new System.Windows.Forms.TextBox();
            this.textFrom = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.subjectText = new System.Windows.Forms.TextBox();
            this.saveButton = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.attachmentName = new System.Windows.Forms.TextBox();
            this.readButton = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.messageNO = new System.Windows.Forms.TextBox();
            this.messageCount = new System.Windows.Forms.TextBox();
            this.txtPanel = new System.Windows.Forms.Panel();
            this.textBox1 = new DevExpress.XtraEditors.LabelControl();
            this.webBrowser1 = new System.Windows.Forms.WebBrowser();
            this.panelControl1 = new DevExpress.XtraEditors.PanelControl();
            this.timSMTP = new System.Windows.Forms.Timer(this.components);
            this.timBar = new System.Windows.Forms.Timer(this.components);
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.tabMail.SuspendLayout();
            this.tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtAttach.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtCap.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtSend.Properties)).BeginInit();
            this.tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.panelPop3)).BeginInit();
            this.panelPop3.SuspendLayout();
            this.txtPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.panelControl1)).BeginInit();
            this.panelControl1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabMail
            // 
            this.tabMail.Controls.Add(this.tabPage1);
            this.tabMail.Controls.Add(this.tabPage2);
            this.tabMail.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabMail.Location = new System.Drawing.Point(2, 2);
            this.tabMail.Name = "tabMail";
            this.tabMail.SelectedIndex = 0;
            this.tabMail.Size = new System.Drawing.Size(574, 457);
            this.tabMail.TabIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.proBar);
            this.tabPage1.Controls.Add(this.labelMsg);
            this.tabPage1.Controls.Add(this.btnSend);
            this.tabPage1.Controls.Add(this.labelControl4);
            this.tabPage1.Controls.Add(this.textBody);
            this.tabPage1.Controls.Add(this.btnAttach);
            this.tabPage1.Controls.Add(this.txtAttach);
            this.tabPage1.Controls.Add(this.labelControl3);
            this.tabPage1.Controls.Add(this.txtCap);
            this.tabPage1.Controls.Add(this.labelControl2);
            this.tabPage1.Controls.Add(this.txtSend);
            this.tabPage1.Controls.Add(this.labelControl1);
            this.tabPage1.Location = new System.Drawing.Point(4, 21);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(566, 432);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "发送邮件";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // proBar
            // 
            this.proBar.Location = new System.Drawing.Point(3, 389);
            this.proBar.Maximum = 120;
            this.proBar.Name = "proBar";
            this.proBar.Size = new System.Drawing.Size(475, 10);
            this.proBar.TabIndex = 11;
            // 
            // labelMsg
            // 
            this.labelMsg.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.labelMsg.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Flat;
            this.labelMsg.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.labelMsg.Location = new System.Drawing.Point(3, 405);
            this.labelMsg.Name = "labelMsg";
            this.labelMsg.Size = new System.Drawing.Size(560, 24);
            this.labelMsg.TabIndex = 10;
            // 
            // btnSend
            // 
            this.btnSend.Location = new System.Drawing.Point(205, 275);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(75, 23);
            this.btnSend.TabIndex = 9;
            this.btnSend.Text = "发送邮件";
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
            // 
            // labelControl4
            // 
            this.labelControl4.Location = new System.Drawing.Point(6, 99);
            this.labelControl4.Name = "labelControl4";
            this.labelControl4.Size = new System.Drawing.Size(36, 14);
            this.labelControl4.TabIndex = 8;
            this.labelControl4.Text = "正　文";
            // 
            // textBody
            // 
            this.textBody.Location = new System.Drawing.Point(6, 119);
            this.textBody.Name = "textBody";
            this.textBody.Size = new System.Drawing.Size(477, 150);
            this.textBody.TabIndex = 7;
            this.textBody.Text = "";
            // 
            // btnAttach
            // 
            this.btnAttach.Location = new System.Drawing.Point(409, 70);
            this.btnAttach.Name = "btnAttach";
            this.btnAttach.Size = new System.Drawing.Size(75, 23);
            this.btnAttach.TabIndex = 6;
            this.btnAttach.Text = "浏  览";
            this.btnAttach.Click += new System.EventHandler(this.btnAttach_Click);
            // 
            // txtAttach
            // 
            this.txtAttach.Location = new System.Drawing.Point(48, 72);
            this.txtAttach.Name = "txtAttach";
            this.txtAttach.Size = new System.Drawing.Size(355, 21);
            this.txtAttach.TabIndex = 5;
            // 
            // labelControl3
            // 
            this.labelControl3.Location = new System.Drawing.Point(6, 75);
            this.labelControl3.Name = "labelControl3";
            this.labelControl3.Size = new System.Drawing.Size(36, 14);
            this.labelControl3.TabIndex = 4;
            this.labelControl3.Text = "附　件";
            // 
            // txtCap
            // 
            this.txtCap.Location = new System.Drawing.Point(48, 43);
            this.txtCap.Name = "txtCap";
            this.txtCap.Size = new System.Drawing.Size(435, 21);
            this.txtCap.TabIndex = 3;
            // 
            // labelControl2
            // 
            this.labelControl2.Location = new System.Drawing.Point(6, 46);
            this.labelControl2.Name = "labelControl2";
            this.labelControl2.Size = new System.Drawing.Size(36, 14);
            this.labelControl2.TabIndex = 2;
            this.labelControl2.Text = "主   题";
            // 
            // txtSend
            // 
            this.txtSend.Location = new System.Drawing.Point(48, 16);
            this.txtSend.Name = "txtSend";
            this.txtSend.Size = new System.Drawing.Size(435, 21);
            this.txtSend.TabIndex = 1;
            // 
            // labelControl1
            // 
            this.labelControl1.Location = new System.Drawing.Point(6, 19);
            this.labelControl1.Name = "labelControl1";
            this.labelControl1.Size = new System.Drawing.Size(36, 14);
            this.labelControl1.TabIndex = 0;
            this.labelControl1.Text = "收件人";
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.panelPop3);
            this.tabPage2.Location = new System.Drawing.Point(4, 21);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(566, 432);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "接收邮件";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // panelPop3
            // 
            this.panelPop3.Controls.Add(this.label7);
            this.panelPop3.Controls.Add(this.label6);
            this.panelPop3.Controls.Add(this.labelShowPop3);
            this.panelPop3.Controls.Add(this.btnStopPop3);
            this.panelPop3.Controls.Add(this.btnConPop3);
            this.panelPop3.Controls.Add(this.changeButton);
            this.panelPop3.Controls.Add(this.textTo);
            this.panelPop3.Controls.Add(this.textFrom);
            this.panelPop3.Controls.Add(this.label5);
            this.panelPop3.Controls.Add(this.label4);
            this.panelPop3.Controls.Add(this.subjectText);
            this.panelPop3.Controls.Add(this.saveButton);
            this.panelPop3.Controls.Add(this.label3);
            this.panelPop3.Controls.Add(this.attachmentName);
            this.panelPop3.Controls.Add(this.readButton);
            this.panelPop3.Controls.Add(this.label2);
            this.panelPop3.Controls.Add(this.label1);
            this.panelPop3.Controls.Add(this.messageNO);
            this.panelPop3.Controls.Add(this.messageCount);
            this.panelPop3.Controls.Add(this.txtPanel);
            this.panelPop3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelPop3.Location = new System.Drawing.Point(3, 3);
            this.panelPop3.Name = "panelPop3";
            this.panelPop3.Size = new System.Drawing.Size(560, 426);
            this.panelPop3.TabIndex = 0;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(251, 67);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(22, 14);
            this.label7.TabIndex = 39;
            this.label7.Text = "To";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(244, 40);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(34, 14);
            this.label6.TabIndex = 38;
            this.label6.Text = "From";
            // 
            // labelShowPop3
            // 
            this.labelShowPop3.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.labelShowPop3.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.labelShowPop3.Location = new System.Drawing.Point(2, 410);
            this.labelShowPop3.Name = "labelShowPop3";
            this.labelShowPop3.Size = new System.Drawing.Size(556, 14);
            this.labelShowPop3.TabIndex = 37;
            // 
            // btnStopPop3
            // 
            this.btnStopPop3.Location = new System.Drawing.Point(5, 178);
            this.btnStopPop3.Name = "btnStopPop3";
            this.btnStopPop3.Size = new System.Drawing.Size(75, 23);
            this.btnStopPop3.TabIndex = 36;
            this.btnStopPop3.Text = "停止测试";
            this.btnStopPop3.UseVisualStyleBackColor = true;
            this.btnStopPop3.Click += new System.EventHandler(this.btnStopPop3_Click);
            // 
            // btnConPop3
            // 
            this.btnConPop3.Location = new System.Drawing.Point(5, 10);
            this.btnConPop3.Name = "btnConPop3";
            this.btnConPop3.Size = new System.Drawing.Size(75, 23);
            this.btnConPop3.TabIndex = 35;
            this.btnConPop3.Text = "收信/开始";
            this.btnConPop3.UseVisualStyleBackColor = true;
            this.btnConPop3.Click += new System.EventHandler(this.btnConPop3_Click);
            // 
            // changeButton
            // 
            this.changeButton.Location = new System.Drawing.Point(188, 145);
            this.changeButton.Name = "changeButton";
            this.changeButton.Size = new System.Drawing.Size(113, 23);
            this.changeButton.TabIndex = 34;
            this.changeButton.Text = "切换";
            this.changeButton.UseVisualStyleBackColor = true;
            this.changeButton.Click += new System.EventHandler(this.changeButton_Click);
            // 
            // textTo
            // 
            this.textTo.Location = new System.Drawing.Point(284, 64);
            this.textTo.Name = "textTo";
            this.textTo.ReadOnly = true;
            this.textTo.Size = new System.Drawing.Size(259, 21);
            this.textTo.TabIndex = 33;
            // 
            // textFrom
            // 
            this.textFrom.Location = new System.Drawing.Point(284, 37);
            this.textFrom.Name = "textFrom";
            this.textFrom.ReadOnly = true;
            this.textFrom.Size = new System.Drawing.Size(259, 21);
            this.textFrom.TabIndex = 32;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(117, 150);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(31, 14);
            this.label5.TabIndex = 30;
            this.label5.Text = "内容";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(117, 123);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(31, 14);
            this.label4.TabIndex = 29;
            this.label4.Text = "主题";
            // 
            // subjectText
            // 
            this.subjectText.Location = new System.Drawing.Point(188, 120);
            this.subjectText.Name = "subjectText";
            this.subjectText.ReadOnly = true;
            this.subjectText.Size = new System.Drawing.Size(355, 21);
            this.subjectText.TabIndex = 28;
            // 
            // saveButton
            // 
            this.saveButton.Location = new System.Drawing.Point(5, 96);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(75, 23);
            this.saveButton.TabIndex = 27;
            this.saveButton.Text = "保存附件";
            this.saveButton.UseVisualStyleBackColor = true;
            this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(117, 96);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(67, 14);
            this.label3.TabIndex = 26;
            this.label3.Text = "附件文件名";
            // 
            // attachmentName
            // 
            this.attachmentName.Location = new System.Drawing.Point(188, 93);
            this.attachmentName.Name = "attachmentName";
            this.attachmentName.ReadOnly = true;
            this.attachmentName.Size = new System.Drawing.Size(355, 21);
            this.attachmentName.TabIndex = 25;
            // 
            // readButton
            // 
            this.readButton.Location = new System.Drawing.Point(5, 39);
            this.readButton.Name = "readButton";
            this.readButton.Size = new System.Drawing.Size(75, 23);
            this.readButton.TabIndex = 24;
            this.readButton.Text = "读取邮件";
            this.readButton.UseVisualStyleBackColor = true;
            this.readButton.Click += new System.EventHandler(this.readButton_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(117, 40);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(67, 14);
            this.label2.TabIndex = 23;
            this.label2.Text = "读取邮件号";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(117, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(43, 14);
            this.label1.TabIndex = 22;
            this.label1.Text = "邮件数";
            // 
            // messageNO
            // 
            this.messageNO.Location = new System.Drawing.Point(188, 37);
            this.messageNO.Name = "messageNO";
            this.messageNO.Size = new System.Drawing.Size(39, 21);
            this.messageNO.TabIndex = 21;
            // 
            // messageCount
            // 
            this.messageCount.Location = new System.Drawing.Point(188, 10);
            this.messageCount.Name = "messageCount";
            this.messageCount.ReadOnly = true;
            this.messageCount.Size = new System.Drawing.Size(39, 21);
            this.messageCount.TabIndex = 20;
            // 
            // txtPanel
            // 
            this.txtPanel.Controls.Add(this.textBox1);
            this.txtPanel.Controls.Add(this.webBrowser1);
            this.txtPanel.Location = new System.Drawing.Point(119, 178);
            this.txtPanel.Name = "txtPanel";
            this.txtPanel.Size = new System.Drawing.Size(424, 217);
            this.txtPanel.TabIndex = 31;
            // 
            // textBox1
            // 
            this.textBox1.AllowHtmlString = true;
            this.textBox1.Appearance.ForeColor = System.Drawing.SystemColors.Desktop;
            this.textBox1.Appearance.Options.UseForeColor = true;
            this.textBox1.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.textBox1.Location = new System.Drawing.Point(62, 68);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(100, 96);
            this.textBox1.TabIndex = 2;
            this.textBox1.Visible = false;
            // 
            // webBrowser1
            // 
            this.webBrowser1.Location = new System.Drawing.Point(212, 90);
            this.webBrowser1.MinimumSize = new System.Drawing.Size(20, 20);
            this.webBrowser1.Name = "webBrowser1";
            this.webBrowser1.Size = new System.Drawing.Size(188, 74);
            this.webBrowser1.TabIndex = 1;
            this.webBrowser1.Visible = false;
            // 
            // panelControl1
            // 
            this.panelControl1.Controls.Add(this.tabMail);
            this.panelControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelControl1.Location = new System.Drawing.Point(0, 0);
            this.panelControl1.Name = "panelControl1";
            this.panelControl1.Size = new System.Drawing.Size(578, 461);
            this.panelControl1.TabIndex = 1;
            // 
            // timSMTP
            // 
            this.timSMTP.Interval = 120000;
            this.timSMTP.Tick += new System.EventHandler(this.timSMTP_Tick);
            // 
            // timBar
            // 
            this.timBar.Interval = 1000;
            this.timBar.Tick += new System.EventHandler(this.timBar_Tick);
            // 
            // timer1
            // 
            this.timer1.Interval = 10000;
            this.timer1.Tick += new System.EventHandler(this.btnSend_Click);
            // 
            // MailTest
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.panelControl1);
            this.Name = "MailTest";
            this.Size = new System.Drawing.Size(578, 461);
            this.tabMail.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtAttach.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtCap.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtSend.Properties)).EndInit();
            this.tabPage2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.panelPop3)).EndInit();
            this.panelPop3.ResumeLayout(false);
            this.panelPop3.PerformLayout();
            this.txtPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.panelControl1)).EndInit();
            this.panelControl1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabMail;
        private System.Windows.Forms.TabPage tabPage1;
        private DevExpress.XtraEditors.LabelControl labelControl3;
        private DevExpress.XtraEditors.TextEdit txtCap;
        private DevExpress.XtraEditors.LabelControl labelControl2;
        private DevExpress.XtraEditors.TextEdit txtSend;
        private DevExpress.XtraEditors.LabelControl labelControl1;
        private DevExpress.XtraEditors.SimpleButton btnAttach;
        private DevExpress.XtraEditors.TextEdit txtAttach;
        private DevExpress.XtraEditors.LabelControl labelMsg;
        private DevExpress.XtraEditors.SimpleButton btnSend;
        private DevExpress.XtraEditors.LabelControl labelControl4;
        private System.Windows.Forms.RichTextBox textBody;
        private DevExpress.XtraEditors.PanelControl panelControl1;
        private System.Windows.Forms.TabPage tabPage2;
        private DevExpress.XtraEditors.PanelControl panelPop3;
        private System.Windows.Forms.Button btnStopPop3;
        private System.Windows.Forms.Button btnConPop3;
        private System.Windows.Forms.Button changeButton;
        private System.Windows.Forms.TextBox textTo;
        private System.Windows.Forms.TextBox textFrom;
        private System.Windows.Forms.Panel txtPanel;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox subjectText;
        private System.Windows.Forms.Button saveButton;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox attachmentName;
        private System.Windows.Forms.Button readButton;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox messageNO;
        private System.Windows.Forms.TextBox messageCount;
        private DevExpress.XtraEditors.LabelControl labelShowPop3;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.WebBrowser webBrowser1;
        private DevExpress.XtraEditors.LabelControl textBox1;
        private System.Windows.Forms.Timer timSMTP;
        private System.Windows.Forms.ProgressBar proBar;
        private System.Windows.Forms.Timer timBar;
        private System.Windows.Forms.Timer timer1;
    }
}
