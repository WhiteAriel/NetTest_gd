namespace NetTest
{
    partial class FTP
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FTP));
            this.panelFTP = new DevExpress.XtraEditors.PanelControl();
            this.splitFTPMain = new DevExpress.XtraEditors.SplitContainerControl();
            this.splitFTPTop = new DevExpress.XtraEditors.SplitContainerControl();
            this.btnDown = new DevExpress.XtraEditors.SimpleButton();
            this.txtLocalPath = new System.Windows.Forms.TextBox();
            this.txtPath = new System.Windows.Forms.TextBox();
            this.btnFTPEnd = new DevExpress.XtraEditors.SimpleButton();
            this.btnFTPStart = new DevExpress.XtraEditors.SimpleButton();
            this.splitCenter = new DevExpress.XtraEditors.SplitContainerControl();
            this.lvLocalFiles = new System.Windows.Forms.ListView();
            this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader3 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader4 = new System.Windows.Forms.ColumnHeader();
            this.lvFiles = new System.Windows.Forms.ListView();
            this.columnHeader6 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader7 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader8 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader9 = new System.Windows.Forms.ColumnHeader();
            this.statusBar1 = new System.Windows.Forms.StatusBar();
            this.statusBarPanel1 = new System.Windows.Forms.StatusBarPanel();
            this.lstMessages = new System.Windows.Forms.ListView();
            this.columnHeader5 = new System.Windows.Forms.ColumnHeader();
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.contextMenuLocal = new System.Windows.Forms.ContextMenu();
            this.menuItem4 = new System.Windows.Forms.MenuItem();
            this.menuItem5 = new System.Windows.Forms.MenuItem();
            this.menuItem6 = new System.Windows.Forms.MenuItem();
            this.contextMenuRemote = new System.Windows.Forms.ContextMenu();
            this.menuItem1 = new System.Windows.Forms.MenuItem();
            this.menuItem2 = new System.Windows.Forms.MenuItem();
            this.menuItem3 = new System.Windows.Forms.MenuItem();
            this.timerDown = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.panelFTP)).BeginInit();
            this.panelFTP.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitFTPMain)).BeginInit();
            this.splitFTPMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitFTPTop)).BeginInit();
            this.splitFTPTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitCenter)).BeginInit();
            this.splitCenter.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.statusBarPanel1)).BeginInit();
            this.SuspendLayout();
            // 
            // panelFTP
            // 
            this.panelFTP.Controls.Add(this.splitFTPMain);
            this.panelFTP.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelFTP.Location = new System.Drawing.Point(0, 0);
            this.panelFTP.Name = "panelFTP";
            this.panelFTP.Size = new System.Drawing.Size(675, 453);
            this.panelFTP.TabIndex = 0;
            // 
            // splitFTPMain
            // 
            this.splitFTPMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitFTPMain.Horizontal = false;
            this.splitFTPMain.Location = new System.Drawing.Point(2, 2);
            this.splitFTPMain.Name = "splitFTPMain";
            this.splitFTPMain.Panel1.Controls.Add(this.splitFTPTop);
            this.splitFTPMain.Panel1.Text = "Panel1";
            this.splitFTPMain.Panel2.Controls.Add(this.statusBar1);
            this.splitFTPMain.Panel2.Controls.Add(this.lstMessages);
            this.splitFTPMain.Panel2.Text = "Panel2";
            this.splitFTPMain.Size = new System.Drawing.Size(671, 449);
            this.splitFTPMain.SplitterPosition = 313;
            this.splitFTPMain.TabIndex = 0;
            this.splitFTPMain.Text = "splitContainerControl1";
            // 
            // splitFTPTop
            // 
            this.splitFTPTop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitFTPTop.Horizontal = false;
            this.splitFTPTop.Location = new System.Drawing.Point(0, 0);
            this.splitFTPTop.Name = "splitFTPTop";
            this.splitFTPTop.Panel1.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.splitFTPTop.Panel1.Controls.Add(this.btnDown);
            this.splitFTPTop.Panel1.Controls.Add(this.txtLocalPath);
            this.splitFTPTop.Panel1.Controls.Add(this.txtPath);
            this.splitFTPTop.Panel1.Controls.Add(this.btnFTPEnd);
            this.splitFTPTop.Panel1.Controls.Add(this.btnFTPStart);
            this.splitFTPTop.Panel1.Text = "Panel1";
            this.splitFTPTop.Panel2.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.splitFTPTop.Panel2.Controls.Add(this.splitCenter);
            this.splitFTPTop.Panel2.Text = "Panel2";
            this.splitFTPTop.Size = new System.Drawing.Size(667, 309);
            this.splitFTPTop.SplitterPosition = 44;
            this.splitFTPTop.TabIndex = 0;
            this.splitFTPTop.Text = "splitContainerControl1";
            // 
            // btnDown
            // 
            this.btnDown.Location = new System.Drawing.Point(115, 15);
            this.btnDown.Name = "btnDown";
            this.btnDown.Size = new System.Drawing.Size(75, 23);
            this.btnDown.TabIndex = 5;
            this.btnDown.Text = "连续下载";
            // 
            // txtLocalPath
            // 
            this.txtLocalPath.Location = new System.Drawing.Point(491, 15);
            this.txtLocalPath.Name = "txtLocalPath";
            this.txtLocalPath.Size = new System.Drawing.Size(70, 21);
            this.txtLocalPath.TabIndex = 4;
            this.txtLocalPath.Visible = false;
            // 
            // txtPath
            // 
            this.txtPath.Location = new System.Drawing.Point(583, 15);
            this.txtPath.Name = "txtPath";
            this.txtPath.Size = new System.Drawing.Size(68, 21);
            this.txtPath.TabIndex = 3;
            this.txtPath.Visible = false;
            // 
            // btnFTPEnd
            // 
            this.btnFTPEnd.Enabled = false;
            this.btnFTPEnd.Location = new System.Drawing.Point(233, 15);
            this.btnFTPEnd.Name = "btnFTPEnd";
            this.btnFTPEnd.Size = new System.Drawing.Size(75, 23);
            this.btnFTPEnd.TabIndex = 1;
            this.btnFTPEnd.Text = "结束测试";
            // 
            // btnFTPStart
            // 
            this.btnFTPStart.Location = new System.Drawing.Point(3, 15);
            this.btnFTPStart.Name = "btnFTPStart";
            this.btnFTPStart.Size = new System.Drawing.Size(75, 23);
            this.btnFTPStart.TabIndex = 0;
            this.btnFTPStart.Text = "连  接";
            // 
            // splitCenter
            // 
            this.splitCenter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitCenter.Location = new System.Drawing.Point(0, 0);
            this.splitCenter.Name = "splitCenter";
            this.splitCenter.Panel1.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.splitCenter.Panel1.Controls.Add(this.lvLocalFiles);
            this.splitCenter.Panel1.Text = "Panel1";
            this.splitCenter.Panel2.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.splitCenter.Panel2.Controls.Add(this.lvFiles);
            this.splitCenter.Panel2.Text = "Panel2";
            this.splitCenter.Size = new System.Drawing.Size(667, 259);
            this.splitCenter.SplitterPosition = 323;
            this.splitCenter.TabIndex = 0;
            this.splitCenter.Text = "splitContainerControl1";
            // 
            // lvLocalFiles
            // 
            this.lvLocalFiles.AllowDrop = true;
            this.lvLocalFiles.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3,
            this.columnHeader4});
            this.lvLocalFiles.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvLocalFiles.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lvLocalFiles.FullRowSelect = true;
            this.lvLocalFiles.GridLines = true;
            this.lvLocalFiles.Location = new System.Drawing.Point(0, 0);
            this.lvLocalFiles.MultiSelect = false;
            this.lvLocalFiles.Name = "lvLocalFiles";
            this.lvLocalFiles.Size = new System.Drawing.Size(323, 259);
            this.lvLocalFiles.TabIndex = 23;
            this.lvLocalFiles.UseCompatibleStateImageBehavior = false;
            this.lvLocalFiles.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "";
            this.columnHeader1.Width = 22;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "名称";
            this.columnHeader2.Width = 150;
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "大小";
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "修改日期";
            this.columnHeader4.Width = 90;
            // 
            // lvFiles
            // 
            this.lvFiles.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader6,
            this.columnHeader7,
            this.columnHeader8,
            this.columnHeader9});
            this.lvFiles.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvFiles.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lvFiles.FullRowSelect = true;
            this.lvFiles.GridLines = true;
            this.lvFiles.Location = new System.Drawing.Point(0, 0);
            this.lvFiles.MultiSelect = false;
            this.lvFiles.Name = "lvFiles";
            this.lvFiles.Size = new System.Drawing.Size(338, 259);
            this.lvFiles.Sorting = System.Windows.Forms.SortOrder.Ascending;
            this.lvFiles.TabIndex = 25;
            this.lvFiles.UseCompatibleStateImageBehavior = false;
            this.lvFiles.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader6
            // 
            this.columnHeader6.Text = "";
            this.columnHeader6.Width = 22;
            // 
            // columnHeader7
            // 
            this.columnHeader7.Text = "名称";
            this.columnHeader7.Width = 150;
            // 
            // columnHeader8
            // 
            this.columnHeader8.Text = "大小";
            // 
            // columnHeader9
            // 
            this.columnHeader9.Text = "修改日期";
            this.columnHeader9.Width = 90;
            // 
            // statusBar1
            // 
            this.statusBar1.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.statusBar1.Location = new System.Drawing.Point(0, 102);
            this.statusBar1.Name = "statusBar1";
            this.statusBar1.Panels.AddRange(new System.Windows.Forms.StatusBarPanel[] {
            this.statusBarPanel1});
            this.statusBar1.ShowPanels = true;
            this.statusBar1.Size = new System.Drawing.Size(667, 24);
            this.statusBar1.TabIndex = 14;
            this.statusBar1.Text = "sdsdssssss";
            // 
            // statusBarPanel1
            // 
            this.statusBarPanel1.AutoSize = System.Windows.Forms.StatusBarPanelAutoSize.Spring;
            this.statusBarPanel1.Name = "statusBarPanel1";
            this.statusBarPanel1.Width = 650;
            // 
            // lstMessages
            // 
            this.lstMessages.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader5});
            this.lstMessages.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstMessages.Font = new System.Drawing.Font("Times New Roman", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lstMessages.ForeColor = System.Drawing.Color.Blue;
            this.lstMessages.FullRowSelect = true;
            this.lstMessages.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.lstMessages.Location = new System.Drawing.Point(0, 0);
            this.lstMessages.MultiSelect = false;
            this.lstMessages.Name = "lstMessages";
            this.lstMessages.Size = new System.Drawing.Size(667, 126);
            this.lstMessages.TabIndex = 10;
            this.lstMessages.UseCompatibleStateImageBehavior = false;
            this.lstMessages.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader5
            // 
            this.columnHeader5.Text = "Messages";
            this.columnHeader5.Width = 700;
            // 
            // imageList1
            // 
            this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList1.Images.SetKeyName(0, "");
            this.imageList1.Images.SetKeyName(1, "");
            this.imageList1.Images.SetKeyName(2, "");
            // 
            // contextMenuLocal
            // 
            this.contextMenuLocal.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItem4,
            this.menuItem5,
            this.menuItem6});
            // 
            // menuItem4
            // 
            this.menuItem4.Index = 0;
            this.menuItem4.Text = "刷新 (F5)";
            // 
            // menuItem5
            // 
            this.menuItem5.Index = 1;
            this.menuItem5.Text = "重命名 (F2)";
            this.menuItem5.Visible = false;
            // 
            // menuItem6
            // 
            this.menuItem6.Index = 2;
            this.menuItem6.Text = "删除 (DEL)";
            // 
            // contextMenuRemote
            // 
            this.contextMenuRemote.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItem1,
            this.menuItem2,
            this.menuItem3});
            // 
            // menuItem1
            // 
            this.menuItem1.Index = 0;
            this.menuItem1.Text = "刷新 (F5)";
            // 
            // menuItem2
            // 
            this.menuItem2.Index = 1;
            this.menuItem2.Text = "重命名 (F2)";
            this.menuItem2.Visible = false;
            // 
            // menuItem3
            // 
            this.menuItem3.Index = 2;
            this.menuItem3.Text = "删除 (DEL)";
            // 
            // timerDown
            // 
            this.timerDown.Interval = 5000;
            // 
            // FTP
            // 
            this.Name = "FTP";
            ((System.ComponentModel.ISupportInitialize)(this.panelFTP)).EndInit();
            this.panelFTP.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitFTPMain)).EndInit();
            this.splitFTPMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitFTPTop)).EndInit();
            this.splitFTPTop.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitCenter)).EndInit();
            this.splitCenter.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.statusBarPanel1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraEditors.PanelControl panelFTP;
        private DevExpress.XtraEditors.SplitContainerControl splitFTPMain;
        private DevExpress.XtraEditors.SplitContainerControl splitFTPTop;
        private DevExpress.XtraEditors.SplitContainerControl splitCenter;
        private DevExpress.XtraEditors.SimpleButton btnFTPStart;
        private DevExpress.XtraEditors.SimpleButton btnFTPEnd;
        private System.Windows.Forms.TextBox txtPath;
        private System.Windows.Forms.ListView lvLocalFiles;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.ListView lvFiles;
        private System.Windows.Forms.ColumnHeader columnHeader6;
        private System.Windows.Forms.ColumnHeader columnHeader7;
        private System.Windows.Forms.ColumnHeader columnHeader8;
        private System.Windows.Forms.ColumnHeader columnHeader9;
        private System.Windows.Forms.ImageList imageList1;
        private System.Windows.Forms.ContextMenu contextMenuLocal;
        private System.Windows.Forms.MenuItem menuItem4;
        private System.Windows.Forms.MenuItem menuItem5;
        private System.Windows.Forms.MenuItem menuItem6;
        private System.Windows.Forms.ContextMenu contextMenuRemote;
        private System.Windows.Forms.MenuItem menuItem1;
        private System.Windows.Forms.MenuItem menuItem2;
        private System.Windows.Forms.MenuItem menuItem3;
        private System.Windows.Forms.ListView lstMessages;
        private System.Windows.Forms.ColumnHeader columnHeader5;
        private System.Windows.Forms.TextBox txtLocalPath;
        private System.Windows.Forms.StatusBar statusBar1;
        private System.Windows.Forms.StatusBarPanel statusBarPanel1;
        private DevExpress.XtraEditors.SimpleButton btnDown;
        private System.Windows.Forms.Timer timerDown;
        
    }
}
