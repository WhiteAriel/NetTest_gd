namespace NetTest
{
    partial class FlvSetting
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.panelControl1 = new DevExpress.XtraEditors.PanelControl();
            this.xtraTabflv = new DevExpress.XtraTab.XtraTabControl();
            this.xtraTabPage1 = new DevExpress.XtraTab.XtraTabPage();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.cbSelwebtype = new System.Windows.Forms.ComboBox();
            this.rBtnweb = new System.Windows.Forms.RadioButton();
            this.cbSelweb = new System.Windows.Forms.ComboBox();
            this.labelControl1 = new DevExpress.XtraEditors.LabelControl();
            this.cbAdapter = new System.Windows.Forms.ComboBox();
            this.btnSearchAdapter = new DevExpress.XtraEditors.SimpleButton();
            this.labelControl6 = new DevExpress.XtraEditors.LabelControl();
            this.txtContinueNo = new DevExpress.XtraEditors.TextEdit();
            this.btnPlayPcapPath = new DevExpress.XtraEditors.SimpleButton();
            this.txtPlayPcapPath = new DevExpress.XtraEditors.TextEdit();
            this.labelControl5 = new DevExpress.XtraEditors.LabelControl();
            this.labelControl3 = new DevExpress.XtraEditors.LabelControl();
            this.chkContinue = new DevExpress.XtraEditors.CheckEdit();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.btnSetCancel = new System.Windows.Forms.Button();
            this.btnSetOK = new System.Windows.Forms.Button();

            this.rAutoCheck = new System.Windows.Forms.RadioButton();
            this.labelAutoWebSite = new System.Windows.Forms.Label();
            this.cbAutoWebSite = new System.Windows.Forms.ComboBox();
            this.btnAutoWebSite = new System.Windows.Forms.Button();
            this.labelAutoReal = new System.Windows.Forms.Label();
            this.cbAutoReal = new System.Windows.Forms.ComboBox();
            this.btnAutoReal = new System.Windows.Forms.Button();

            this.labelThreshold = new System.Windows.Forms.Label();
            this.textThreshold = new DevExpress.XtraEditors.TextEdit();
            this.labelSeconds = new System.Windows.Forms.Label();


            ((System.ComponentModel.ISupportInitialize)(this.panelControl1)).BeginInit();
            this.panelControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.xtraTabflv)).BeginInit();
            this.xtraTabflv.SuspendLayout();
            this.xtraTabPage1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelControl1
            // 
            this.panelControl1.Controls.Add(this.xtraTabflv);
            this.panelControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelControl1.Location = new System.Drawing.Point(0, 0);
            this.panelControl1.Name = "panelControl1";
            this.panelControl1.Size = new System.Drawing.Size(900, 650);
            this.panelControl1.TabIndex = 0;
            // 
            // xtraTabflv
            // 
            this.xtraTabflv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.xtraTabflv.Location = new System.Drawing.Point(2, 2);
            this.xtraTabflv.Name = "xtraTabflv";
            this.xtraTabflv.SelectedTabPage = this.xtraTabPage1;
            this.xtraTabflv.Size = new System.Drawing.Size(896, 646);
            this.xtraTabflv.TabIndex = 0;
            this.xtraTabflv.TabPages.AddRange(new DevExpress.XtraTab.XtraTabPage[] {
            this.xtraTabPage1});
            // 
            // xtraTabPage1
            // 
            this.xtraTabPage1.Controls.Add(this.groupBox1);
            this.xtraTabPage1.Controls.Add(this.cbAdapter);
            this.xtraTabPage1.Controls.Add(this.btnSearchAdapter);
            //this.xtraTabPage1.Controls.Add(this.labelControl6);
            //this.xtraTabPage1.Controls.Add(this.txtContinueNo);
            this.xtraTabPage1.Controls.Add(this.btnPlayPcapPath);
            this.xtraTabPage1.Controls.Add(this.txtPlayPcapPath);
            this.xtraTabPage1.Controls.Add(this.labelControl5);
            this.xtraTabPage1.Controls.Add(this.labelControl3);
            //this.xtraTabPage1.Controls.Add(this.chkContinue);
            this.xtraTabPage1.Controls.Add(this.btnSetOK);
            this.xtraTabPage1.Controls.Add(this.btnSetCancel);
            this.xtraTabPage1.Enabled = true;
            this.xtraTabPage1.Name = "xtraTabPage1";
            this.xtraTabPage1.Size = new System.Drawing.Size(887, 614);
            this.xtraTabPage1.Text = "流媒体设置";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.cbSelwebtype);
            this.groupBox1.Controls.Add(this.rBtnweb);
            this.groupBox1.Controls.Add(this.cbSelweb);
            this.groupBox1.Controls.Add(this.labelControl1);
            this.groupBox1.Controls.Add(this.labelAutoWebSite);
            this.groupBox1.Controls.Add(this.rAutoCheck);
            this.groupBox1.Controls.Add(this.cbAutoWebSite);
            this.groupBox1.Controls.Add(this.btnAutoWebSite);
            this.groupBox1.Controls.Add(this.labelAutoReal);
            this.groupBox1.Controls.Add(this.cbAutoReal);
            this.groupBox1.Controls.Add(this.btnAutoReal);
            this.groupBox1.Controls.Add(this.labelThreshold);
            this.groupBox1.Controls.Add(this.textThreshold);
            this.groupBox1.Controls.Add(this.labelSeconds);
            this.groupBox1.Location = new System.Drawing.Point(45, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(793, 320);
            this.groupBox1.TabIndex = 23;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "网页流媒体";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(24, 57);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(79, 14);
            this.label1.TabIndex = 27;
            this.label1.Text = "输入链接类型";


            // 
            // btnSetOK
            // 
            //this.btnSetOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnSetOK.Location = new System.Drawing.Point(185, 490);
            this.btnSetOK.Name = "btnSetOK";
            this.btnSetOK.Size = new System.Drawing.Size(75, 25);
            this.btnSetOK.TabIndex = 1;
            this.btnSetOK.Text = "确定";
            this.btnSetOK.UseVisualStyleBackColor = true;
            this.btnSetOK.Click += new System.EventHandler(this.btnSetOK_Click);
            // 
            // btnSetCancel
            // 
            //this.btnSetCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnSetCancel.Location = new System.Drawing.Point(500, 490);
            this.btnSetCancel.Name = "btnSetCancel";
            this.btnSetCancel.Size = new System.Drawing.Size(75, 25);
            this.btnSetCancel.TabIndex = 0;
            this.btnSetCancel.Text = "取消";
            this.btnSetCancel.UseVisualStyleBackColor = true;
            this.btnSetCancel.Click += new System.EventHandler(this.btnSetCancel_Click);
            // 
            // cbSelwebtype
            //
            this.cbSelwebtype.Enabled = false;
            this.cbSelwebtype.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbSelwebtype.FormattingEnabled = true;
            this.cbSelwebtype.Location = new System.Drawing.Point(174, 57);
            this.cbSelwebtype.Name = "cbSelwebtype";
            this.cbSelwebtype.Size = new System.Drawing.Size(121, 20);
            this.cbSelwebtype.TabIndex = 26;
            this.cbSelwebtype.SelectedIndexChanged += new System.EventHandler(this.cbSelwebtype_SelectedIndexChanged);
            // 
            // rBtnweb
            // 
            this.rBtnweb.AutoSize = true;
            this.rBtnweb.Checked = false;
            this.rBtnweb.Location = new System.Drawing.Point(6, 30);
            this.rBtnweb.Name = "rBtnweb";
            this.rBtnweb.Size = new System.Drawing.Size(109, 18);
            this.rBtnweb.TabIndex = 21;
            this.rBtnweb.TabStop = true;
            this.rBtnweb.Text = "流媒体链接手动设置";
            this.rBtnweb.UseVisualStyleBackColor = true;
            this.rBtnweb.Click += new System.EventHandler(this.rBtnweb_Click);
            // 
            // rAutoCheck
            // 
            this.rAutoCheck.AutoSize = true;
            this.rAutoCheck.Checked = true;
            this.rAutoCheck.Location = new System.Drawing.Point(6, 146);
            this.rAutoCheck.Name = "rAutoCheck";
            this.rAutoCheck.Size = new System.Drawing.Size(109, 18);
            //this.rAutoCheck.TabIndex = 21;
            this.rAutoCheck.TabStop = true;
            this.rAutoCheck.Text = "流媒体链接自动设置";
            this.rAutoCheck.UseVisualStyleBackColor = true;
            this.rAutoCheck.Click += new System.EventHandler(this.rAutoCheck_Click);
            // 
            // labelAutoWebSite
            // 
            this.labelAutoWebSite.AutoSize = true;
            this.labelAutoWebSite.Location = new System.Drawing.Point(24, 176);
            this.labelAutoWebSite.Name = "labelAutoWebSite";
            this.labelAutoWebSite.Size = new System.Drawing.Size(79, 14);
            //this.labelAutoWebSite.TabIndex = 27;
            this.labelAutoWebSite.Text = "选择视频网站";
            // 
            // cbAutoWebSite
            // 
            this.cbAutoWebSite.Enabled = true;
            this.cbAutoWebSite.FormattingEnabled = true;
            this.cbAutoWebSite.Location = new System.Drawing.Point(174, 176);
            this.cbAutoWebSite.Name = "cbAutoWebSite";
            this.cbAutoWebSite.Size = new System.Drawing.Size(121, 20);
            this.cbAutoWebSite.SelectedIndexChanged += new System.EventHandler(this.cbAutoWebSite_SelectedIndexChanged);
            //this.cbAutoWebSite.TabIndex = 25;
            //
            //btnAutoWebSite
            //
            this.btnAutoWebSite.Location = new System.Drawing.Point(430, 176);
            this.btnAutoWebSite.Name = "btnAutoWebSite";
            this.btnAutoWebSite.Size = new System.Drawing.Size(120, 22);
            //this.btnAutoWebSite.TabIndex = 1;
            this.btnAutoWebSite.Text = "更新视频网站";
            this.btnAutoWebSite.UseVisualStyleBackColor = true;
            this.btnAutoWebSite.Click += new System.EventHandler(this.btnAutoWebSite_Click);
            // 
            // labelAutoReal
            // 
            this.labelAutoReal.AutoSize = true;
            this.labelAutoReal.Location = new System.Drawing.Point(24, 206);
            this.labelAutoReal.Name = "labelAutoReal";
            this.labelAutoReal.Size = new System.Drawing.Size(79, 14);
            //this.labelAutoWebSite.TabIndex = 27;
            this.labelAutoReal.Text = "选择测试链接";
            // 
            // cbAutoReal
            // 
            this.cbAutoReal.Enabled = true;
            this.cbAutoReal.FormattingEnabled = true;
            this.cbAutoReal.Location = new System.Drawing.Point(18, 230);
            this.cbAutoReal.Name = "cbAutoWebSite";
            this.cbAutoReal.Size = new System.Drawing.Size(600, 20);
            //this.cbAutoReal.TabIndex = 25;
            //
            //btnAutoReal
            //
            this.btnAutoReal.Location = new System.Drawing.Point(660, 228);
            this.btnAutoReal.Name = "btnAutoWebSite";
            this.btnAutoReal.Size = new System.Drawing.Size(120, 25);
            //this.btnAutoWebSite.TabIndex = 1;
            this.btnAutoReal.Text = "更新视频链接";
            this.btnAutoReal.UseVisualStyleBackColor = true;
            this.btnAutoReal.Click += new System.EventHandler(this.btnAutoReal_Click);

            // 
            // labelThreshold
            // 
            this.labelThreshold.AutoSize = true;
            this.labelThreshold.Location = new System.Drawing.Point(9, 280);
            this.labelThreshold.Name = "labelThreshold";
            this.labelThreshold.Size = new System.Drawing.Size(79, 14);
            //this.labelThreshold.TabIndex = 27;
            this.labelThreshold.Text = "超时门限：";
            // 
            // labelSeconds
            // 
            this.labelSeconds.AutoSize = true;
            this.labelSeconds.Location = new System.Drawing.Point(130, 283);
            this.labelSeconds.Name = "labelSeconds";
            this.labelSeconds.Size = new System.Drawing.Size(79, 14);
            //this.labelThreshold.TabIndex = 27;
            this.labelSeconds.Text = "秒（建议4~40秒）";
            //
            //超时门限
            //
            this.textThreshold.EditValue = "4";
            this.textThreshold.Properties.Mask.EditMask= "#0";
            this.textThreshold.Properties.Mask.MaskType = DevExpress.XtraEditors.Mask.MaskType.Numeric;
            this.textThreshold.Properties.Mask.UseMaskAsDisplayFormat = true;
            this.textThreshold.Location = new System.Drawing.Point(90, 279);
            this.textThreshold.Name = "textThreshold";
            this.textThreshold.Size = new System.Drawing.Size(37, 21);
            this.textThreshold.TabIndex = 14;
           // 
            // cbSelweb
            // 
            this.cbSelweb.Enabled = false;
            this.cbSelweb.FormattingEnabled = true;
            this.cbSelweb.Location = new System.Drawing.Point(18, 110);
            this.cbSelweb.Name = "cbSelweb";
            this.cbSelweb.Size = new System.Drawing.Size(590, 20);
            this.cbSelweb.TabIndex = 25;
          
            // 
            // labelControl1
            // 
            this.labelControl1.Location = new System.Drawing.Point(27, 86);
            this.labelControl1.Name = "labelControl1";
            this.labelControl1.Size = new System.Drawing.Size(132, 14);
            this.labelControl1.TabIndex = 3;
            this.labelControl1.Text = "输入流媒体文件网络地址";
            // 
            // cbAdapter
            // 
            this.cbAdapter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbAdapter.FormattingEnabled = true;
            this.cbAdapter.Location = new System.Drawing.Point(186, 400);
            this.cbAdapter.Name = "cbAdapter";
            this.cbAdapter.Size = new System.Drawing.Size(198, 20);
            this.cbAdapter.TabIndex = 19;
            // 
            // btnSearchAdapter
            // 
            this.btnSearchAdapter.Location = new System.Drawing.Point(400, 400);
            this.btnSearchAdapter.Name = "btnSearchAdapter";
            this.btnSearchAdapter.Size = new System.Drawing.Size(90, 23);
            //this.btnSearchAdapter.TabIndex = 24;
            this.btnSearchAdapter.Text = "检测网卡";
            this.btnSearchAdapter.Click += new System.EventHandler(this.btnSearchAdapter_Click);
            // 
            // labelControl6
            // 
            this.labelControl6.Location = new System.Drawing.Point(220, 450);
            this.labelControl6.Name = "labelControl6";
            this.labelControl6.Size = new System.Drawing.Size(24, 14);
            this.labelControl6.TabIndex = 18;
            this.labelControl6.Text = "次）";
            // 
            // txtContinueNo
            // 
            this.txtContinueNo.EditValue = "3";
            this.txtContinueNo.Location = new System.Drawing.Point(187, 450);
            this.txtContinueNo.Name = "txtContinueNo";
            this.txtContinueNo.Size = new System.Drawing.Size(21, 21);
            this.txtContinueNo.TabIndex = 17;
            this.txtContinueNo.EditValueChanged += new System.EventHandler(this.txtContinueNo_EditValueChanged);
            // 
            // btnPlayPcapPath
            // 
            this.btnPlayPcapPath.Location = new System.Drawing.Point(695, 350);
            this.btnPlayPcapPath.Name = "btnPlayPcapPath";
            this.btnPlayPcapPath.Size = new System.Drawing.Size(75, 23);
            this.btnPlayPcapPath.TabIndex = 16;
            this.btnPlayPcapPath.Text = "浏览";
            this.btnPlayPcapPath.Click += new System.EventHandler(this.btnPlayPcapPath_Click);
            // 
            // txtPlayPcapPath
            // 
            this.txtPlayPcapPath.EditValue = "E:\\temp";
            this.txtPlayPcapPath.Location = new System.Drawing.Point(186, 350);
            this.txtPlayPcapPath.Name = "txtPlayPcapPath";
            this.txtPlayPcapPath.Size = new System.Drawing.Size(467, 21);
            this.txtPlayPcapPath.TabIndex = 14;
            // 
            // labelControl5
            // 
            this.labelControl5.Location = new System.Drawing.Point(45, 350);
            this.labelControl5.Name = "labelControl5";
            this.labelControl5.Size = new System.Drawing.Size(132, 14);
            this.labelControl5.TabIndex = 7;
            this.labelControl5.Text = "日志、抓包文件存储路径";
            // 
            // labelControl3
            // 
            this.labelControl3.Location = new System.Drawing.Point(45, 400);
            this.labelControl3.Name = "labelControl3";
            this.labelControl3.Size = new System.Drawing.Size(59, 14);
            this.labelControl3.TabIndex = 5;
            this.labelControl3.Text = "网卡IP选择";
            // 
            // chkContinue
            // 
            this.chkContinue.Location = new System.Drawing.Point(45, 450);
            this.chkContinue.Name = "chkContinue";
            this.chkContinue.Properties.Caption = "连续测试次数";
            this.chkContinue.Size = new System.Drawing.Size(132, 19);
            this.chkContinue.TabIndex = 2;
            this.chkContinue.CheckedChanged += new System.EventHandler(this.chkContinue_CheckedChanged);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // FlvSetting
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.Controls.Add(this.panelControl1);
            this.Name = "FlvSetting";
            this.Size = new System.Drawing.Size(900, 650);
            ((System.ComponentModel.ISupportInitialize)(this.panelControl1)).EndInit();
            this.panelControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.xtraTabflv)).EndInit();
            this.xtraTabflv.ResumeLayout(false);
            this.xtraTabPage1.ResumeLayout(false);
            this.xtraTabPage1.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraEditors.PanelControl panelControl1;
        private DevExpress.XtraTab.XtraTabControl xtraTabflv;
        private DevExpress.XtraTab.XtraTabPage xtraTabPage1;
        //private DevExpress.XtraEditors.SimpleButton btnSetOK;
        //private DevExpress.XtraEditors.SimpleButton btnSetCance;

        private System.Windows.Forms.Button btnSetOK;
        private System.Windows.Forms.Button btnSetCancel;
        private DevExpress.XtraEditors.LabelControl labelControl5;
        private DevExpress.XtraEditors.LabelControl labelControl3;
       
        private DevExpress.XtraEditors.CheckEdit chkContinue;
        private DevExpress.XtraEditors.SimpleButton btnPlayPcapPath;
        private DevExpress.XtraEditors.TextEdit txtPlayPcapPath;
        private System.Windows.Forms.ComboBox cbAdapter;
        private DevExpress.XtraEditors.SimpleButton btnSearchAdapter;
        private DevExpress.XtraEditors.LabelControl labelControl6;
        private DevExpress.XtraEditors.TextEdit txtContinueNo;
        
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ComboBox cbSelweb;
        private System.Windows.Forms.RadioButton rBtnweb;
        private DevExpress.XtraEditors.LabelControl labelControl1;
        private System.Windows.Forms.ComboBox cbSelwebtype;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;

        private System.Windows.Forms.RadioButton rAutoCheck;
        private System.Windows.Forms.Label labelAutoWebSite;
        private System.Windows.Forms.ComboBox cbAutoWebSite;
        private System.Windows.Forms.Button btnAutoWebSite;
        private System.Windows.Forms.Label labelAutoReal;
        private System.Windows.Forms.ComboBox cbAutoReal;
        private System.Windows.Forms.Button btnAutoReal;

        private System.Windows.Forms.Label labelThreshold;
        private DevExpress.XtraEditors.TextEdit textThreshold;
        private System.Windows.Forms.Label labelSeconds;

        //private DevExpress.XtraEditors.SimpleButton sBtnWeb;
        //private DevExpress.XtraEditors.SimpleButton WebRecordDel;



    }
}