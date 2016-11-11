namespace NetTest
{
    partial class FlvTest
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
            this.components = new System.ComponentModel.Container();
            this.panelFlv = new DevExpress.XtraEditors.PanelControl();
            this.splitContainerControl2 = new DevExpress.XtraEditors.SplitContainerControl();
            this.btnFlvStop = new DevExpress.XtraEditors.SimpleButton();
            this.btnFlvStart = new DevExpress.XtraEditors.SimpleButton();
            this.splitContainerControl1 = new DevExpress.XtraEditors.SplitContainerControl();
            this.memoPcap = new System.Windows.Forms.ListBox();
            this.timFlv = new System.Windows.Forms.Timer(this.components);
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.panelFlv)).BeginInit();
            this.panelFlv.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerControl2)).BeginInit();
            this.splitContainerControl2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerControl1)).BeginInit();
            this.splitContainerControl1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelFlv
            // 
            this.panelFlv.Controls.Add(this.splitContainerControl2);
            this.panelFlv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelFlv.Location = new System.Drawing.Point(0, 0);
            this.panelFlv.Name = "panelFlv";
            this.panelFlv.Size = new System.Drawing.Size(542, 417);
            this.panelFlv.TabIndex = 3;
            // 
            // splitContainerControl2
            // 
            this.splitContainerControl2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerControl2.Location = new System.Drawing.Point(2, 2);
            this.splitContainerControl2.Name = "splitContainerControl2";
            this.splitContainerControl2.Panel1.Controls.Add(this.btnFlvStop);
            this.splitContainerControl2.Panel1.Controls.Add(this.btnFlvStart);
            this.splitContainerControl2.Panel1.Text = "Panel1";
            this.splitContainerControl2.Panel2.Controls.Add(this.splitContainerControl1);
            this.splitContainerControl2.Panel2.Text = "Panel2";
            this.splitContainerControl2.Size = new System.Drawing.Size(538, 413);
            this.splitContainerControl2.SplitterPosition = 131;
            this.splitContainerControl2.TabIndex = 0;
            this.splitContainerControl2.Text = "splitContainerControl2";
            // 
            // btnFlvStop
            // 
            this.btnFlvStop.Enabled = false;
            this.btnFlvStop.Location = new System.Drawing.Point(12, 138);
            this.btnFlvStop.Name = "btnFlvStop";
            this.btnFlvStop.Size = new System.Drawing.Size(75, 23);
            this.btnFlvStop.TabIndex = 2;
            this.btnFlvStop.Text = "停止测试";
            this.btnFlvStop.Click += new System.EventHandler(this.btnFlvStop_Click);
            // 
            // btnFlvStart
            // 
            this.btnFlvStart.Location = new System.Drawing.Point(12, 41);
            this.btnFlvStart.Name = "btnFlvStart";
            this.btnFlvStart.Size = new System.Drawing.Size(75, 27);
            this.btnFlvStart.TabIndex = 1;
            this.btnFlvStart.Text = "启动测试";
            this.btnFlvStart.Click += new System.EventHandler(this.btnFlvStart_Click);
            // 
            // splitContainerControl1
            // 
            this.splitContainerControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerControl1.Horizontal = false;
            this.splitContainerControl1.Location = new System.Drawing.Point(0, 0);
            this.splitContainerControl1.Name = "splitContainerControl1";
            this.splitContainerControl1.Panel1.Text = "Panel1";
            this.splitContainerControl1.Panel2.Controls.Add(this.memoPcap);
            this.splitContainerControl1.Panel2.Text = "Panel2";
            this.splitContainerControl1.Size = new System.Drawing.Size(397, 409);
            this.splitContainerControl1.SplitterPosition = 372;
            this.splitContainerControl1.TabIndex = 0;
            this.splitContainerControl1.Text = "splitContainerControl1";
            // 
            // memoPcap
            // 
            this.memoPcap.Dock = System.Windows.Forms.DockStyle.Fill;
            this.memoPcap.FormattingEnabled = true;
            this.memoPcap.ItemHeight = 12;
            this.memoPcap.Location = new System.Drawing.Point(0, 0);
            this.memoPcap.Name = "memoPcap";
            this.memoPcap.Size = new System.Drawing.Size(393, 16);
            this.memoPcap.TabIndex = 0;
            // 
            // timFlv
            // 
            this.timFlv.Interval = 500;
            this.timFlv.Tick += new System.EventHandler(this.timFlv_Tick);
            // 
            // timer1
            // 
            this.timer1.Interval = 180000;
            this.timer1.Tick += new System.EventHandler(this.btnFlvStart_Click);
            // 
            // FlvTest
            // 
            this.Controls.Add(this.panelFlv);
            this.Name = "FlvTest";
            this.Size = new System.Drawing.Size(542, 417);
            ((System.ComponentModel.ISupportInitialize)(this.panelFlv)).EndInit();
            this.panelFlv.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerControl2)).EndInit();
            this.splitContainerControl2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerControl1)).EndInit();
            this.splitContainerControl1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraEditors.PanelControl panelFlv;
        private DevExpress.XtraEditors.SplitContainerControl splitContainerControl2;
        private DevExpress.XtraEditors.SimpleButton btnFlvStop;
        private DevExpress.XtraEditors.SimpleButton btnFlvStart;
        private System.Windows.Forms.ListBox memoPcap;
        private System.Windows.Forms.Timer timFlv;
        private System.Windows.Forms.Timer timer1;
        private DevExpress.XtraEditors.SplitContainerControl splitContainerControl1;


    }
}
