namespace NetTest
{
    partial class WebTest
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
            this.panelWeb = new DevExpress.XtraEditors.PanelControl();
            this.splitContainerControl2 = new DevExpress.XtraEditors.SplitContainerControl();
            this.btnWebStop = new DevExpress.XtraEditors.SimpleButton();
            this.sBTest = new DevExpress.XtraEditors.SimpleButton();
            this.panelExplore = new System.Windows.Forms.Panel();
            this.memoPcap = new System.Windows.Forms.ListBox();
            //this.timWeb = new System.Windows.Forms.Timer(this.components);
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.panelWeb)).BeginInit();
            this.panelWeb.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerControl2)).BeginInit();
            this.splitContainerControl2.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelWeb
            // 
            this.panelWeb.Controls.Add(this.splitContainerControl2);
            this.panelWeb.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelWeb.Location = new System.Drawing.Point(0, 0);
            this.panelWeb.Name = "panelWeb";
            this.panelWeb.Size = new System.Drawing.Size(509, 314);
            this.panelWeb.TabIndex = 2;
            // 
            // splitContainerControl2
            // 
            this.splitContainerControl2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerControl2.Location = new System.Drawing.Point(2, 2);
            this.splitContainerControl2.Name = "splitContainerControl2";
            this.splitContainerControl2.Panel1.Controls.Add(this.btnWebStop);
            this.splitContainerControl2.Panel1.Controls.Add(this.sBTest);
            this.splitContainerControl2.Panel1.Text = "Panel1";
            this.splitContainerControl2.Panel2.Controls.Add(this.panelExplore);
            this.splitContainerControl2.Panel2.Controls.Add(this.memoPcap);
            this.splitContainerControl2.Panel2.Text = "Panel2";
            this.splitContainerControl2.Size = new System.Drawing.Size(505, 310);
            this.splitContainerControl2.SplitterPosition = 127;
            this.splitContainerControl2.TabIndex = 0;
            this.splitContainerControl2.Text = "splitContainerControl2";
            // 
            // btnWebStop
            // 
            this.btnWebStop.Enabled = false;
            this.btnWebStop.Location = new System.Drawing.Point(12, 78);
            this.btnWebStop.Name = "btnWebStop";
            this.btnWebStop.Size = new System.Drawing.Size(75, 23);
            this.btnWebStop.TabIndex = 0;
            this.btnWebStop.Text = "Õ£÷π≤‚ ‘";
            this.btnWebStop.Click += new System.EventHandler(this.btnWebStop_Click);
            // 
            // sBTest
            // 
            this.sBTest.Location = new System.Drawing.Point(12, 24);
            this.sBTest.Name = "sBTest";
            this.sBTest.Size = new System.Drawing.Size(75, 23);
            this.sBTest.TabIndex = 1;
            this.sBTest.Text = "∆Ù∂Ø≤‚ ‘";
            this.sBTest.Click += new System.EventHandler(this.sBTest_Click);
            // 
            // panelExplore
            // 
            this.panelExplore.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.panelExplore.Location = new System.Drawing.Point(0, 1);
            this.panelExplore.Name = "panelExplore";
            this.panelExplore.Size = new System.Drawing.Size(367, 360);
            this.panelExplore.TabIndex = 1;
            // 
            // memoPcap
            // 
            this.memoPcap.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.memoPcap.FormattingEnabled = true;
            this.memoPcap.ItemHeight = 12;
            this.memoPcap.Location = new System.Drawing.Point(0, 40);
            this.memoPcap.Name = "memoPcap";
            this.memoPcap.Size = new System.Drawing.Size(368, 270);
            this.memoPcap.TabIndex = 0;
            // 
            // timWeb
            // 
            //this.timWeb.Interval = 3000;
            //this.timWeb.Tick += new System.EventHandler(this.timWeb_Tick);
            // 
            // timer1
            // 
            this.timer1.Interval = 180000;
            this.timer1.Tick += new System.EventHandler(this.sBTest_Click);
            // 
            // WebTest
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.panelWeb);
            this.Name = "WebTest";
            this.Size = new System.Drawing.Size(509, 314);
            ((System.ComponentModel.ISupportInitialize)(this.panelWeb)).EndInit();
            this.panelWeb.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerControl2)).EndInit();
            this.splitContainerControl2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraEditors.PanelControl panelWeb;
        private DevExpress.XtraEditors.SplitContainerControl splitContainerControl2;
        private DevExpress.XtraEditors.SimpleButton btnWebStop;
        private DevExpress.XtraEditors.SimpleButton sBTest;
        private System.Windows.Forms.ListBox memoPcap;
        //private System.Windows.Forms.Timer timWeb;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Panel panelExplore;
    }
}
