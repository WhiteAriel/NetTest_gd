using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using System.IO;
using Tamir.IPLib;
using Tamir.IPLib.Packets;
using System.Threading;
using System.Globalization;

namespace NetTest
{
    

    public partial class FTP : DevExpress.XtraEditors.XtraUserControl
    {
        

        //private string m_previousfilename;
        //private FTPCom.FTPC ftpc;
        private const string CRLF = "\r\n";
        private BackgroundWorker m_AsyncWorker = new BackgroundWorker();

        private StringBuilder strbFile = new StringBuilder();
        public string strFile = "";
        private string strLogFile;
        private int iDevice;
        private PcapDevice device;
        IniFile inis = new IniFile(Application.StartupPath + "\\settings.ini");
        IniFile indll = new IniFile(Application.StartupPath + "\\net.dll");
        private DateTime dtStart;
        private static int INDEX_TAG_DIR = 0;
        private static int INDEX_TAG_FILE = 1;
        private static int INDEX_TAG_LINK = 2;
        
        private ListViewItem lvDragItem;
        private DragDropEffects CurrentEffect;
        private string startingFrom = "";

        private string sLocalPath = "";
        private string sRemotePath = "";
        FtpClient ftpClient = null;

        //private String binFname;

        //private System.Windows.Forms.ImageList imageList1;

        private ListViewColumnSorter lvwColumnSorter;
        private ListViewColumnSorter lvwLocalColumnSorter;

        private string RemoteDown;
        private string LocalDown;

        protected bool DownFinish = false;
        private int iDownNum=0;
        public FTP()
        {
            InitializeComponent();

            lvFiles.SmallImageList = imageList1;
            lvLocalFiles.SmallImageList = imageList1;

            lvwColumnSorter = new ListViewColumnSorter();
            this.lvFiles.ListViewItemSorter = lvwColumnSorter;

            lvwLocalColumnSorter = new ListViewColumnSorter();
            this.lvLocalFiles.ListViewItemSorter = lvwLocalColumnSorter;

            // initialize list which shows local directory
            this.lvLocalFiles.AllowDrop = true;
            InitLocalListView();

            m_AsyncWorker.WorkerSupportsCancellation = true;
            //m_AsyncWorker.ProgressChanged += new ProgressChangedEventHandler(bwAsync_ProgressChanged);
            m_AsyncWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bwAsync_RunWorkerCompleted);
            m_AsyncWorker.DoWork += new DoWorkEventHandler(bwAsync_DoWork);
            this.lvLocalFiles.ContextMenu = this.contextMenuLocal;
            this.lvFiles.ContextMenu = this.contextMenuRemote;
            
            Control.CheckForIllegalCrossThreadCalls = false;
        }

        private void btnFTPStart_Click(object sender, EventArgs e)
        {
            string dir = inis.IniReadValue("FTP", "DownPath");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            //dir = inis.IniReadValue("FTP", "Path");
            //if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            this.btnFTPStart.Enabled = false;
            this.lstMessages.Items.Clear();
            this.btnFTPEnd.Enabled = true;
            if (strbFile.Length > 0) strbFile.Remove(0, strbFile.Length);

            strFile = inis.IniReadValue("FTP", "Path") + "\\FTP-" +DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString()
            + "-" + DateTime.Now.Day.ToString() + "-" + DateTime.Now.Hour.ToString() + "-" + DateTime.Now.Minute.ToString() + "-" +
            DateTime.Now.Second.ToString() + ".cap";  // inis.IniReadValue("FTP","Host")+
            strLogFile = inis.IniReadValue("FTP", "Path") + "\\FTP-"+ DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString()
                + DateTime.Now.Day.ToString() + "-" + DateTime.Now.Hour.ToString() + "-" + DateTime.Now.Minute.ToString() + "-" +
                DateTime.Now.Second.ToString() + ".txt";
            
            if (!Directory.Exists(inis.IniReadValue("FTP", "Path"))) Directory.CreateDirectory(inis.IniReadValue("FTP", "Path"));
            //if (!m_AsyncWorker.IsBusy)
            //{
            //    m_AsyncWorker.RunWorkerAsync();
            //}
            this.FTPTesting();
        }

        private void FTPTesting()
        {

            iDevice = int.Parse(inis.IniReadValue("FTP","Adapter"));
            
            PcapDeviceList devices = SharpPcap.GetAllDevices();

            device = devices[iDevice];
            string ip = device.PcapIpAddress;
            strbFile.Append("网卡: " + device.PcapDescription + "\r\n");
            strbFile.Append("IP地址: " + device.PcapIpAddress + "\r\n");
            strbFile.Append("目的地址: " + inis.IniReadValue("FTP","Host") + "\r\n");
            strbFile.Append("测试开始时间: " + dtStart.ToString() + "\r\n");

            Thread.Sleep(100);
            
            this.dtStart = DateTime.Now;
            strbFile.Append("测试开始时间: " + dtStart.ToString() + "\r\n");


            //Register our handler function to the 'packet arrival' event
            device.PcapOnPacketArrival +=
                new SharpPcap.PacketArrivalEvent(device_PcapOnPacketArrival);
            device.PcapOpen(true, 100);
            device.PcapSetFilter("(tcp or udp) and host " + ip);
            device.PcapDumpOpen(strFile);
            if (!m_AsyncWorker.IsBusy)
            {
                m_AsyncWorker.RunWorkerAsync();
            }
            try
            {
                connectiondata conndata = new connectiondata();
                try
                {
                    conndata.address = inis.IniReadValue("FTP", "Host");
                    conndata.username = inis.IniReadValue("FTP", "User");
                    conndata.password = indll.IniReadValue("FTP", "Pass");
                    conndata.port = inis.IniReadValue("FTP", "Port");
                    conndata.anonymous = false;
                    Login(conndata.address, conndata.username, conndata.password, conndata.port, conndata.anonymous);
                    this.btnFTPStart.Enabled = false;
                    this.btnFTPEnd.Enabled = true;

                    lvwColumnSorter.Order = SortOrder.Ascending;
                    ChangeDir("/");
                    lvFiles.AllowDrop = true;

                    // set context menu for the remote file list
                    lvFiles.ContextMenu = contextMenuRemote;

                     //reset address changement
                    //txtAddress.TextChanged -= new System.EventHandler(this.txtAddress_TextChanged);
                }
                catch
                {
                    ftpClient.Close();
                    ftpClient = null;
                    btnFTPStart.Enabled = true;
                    btnFTPEnd.Enabled = false;
                    lvFiles.AllowDrop = false;
                }
               
            }
            catch (Exception ex)
            {
                strbFile.Append(ex.Message);
                //return;
            }


        }
        private void bwAsync_DoWork(object sender, DoWorkEventArgs e)
        {
            device.PcapStartCapture();
            
        }

        private void bwAsync_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                this.strbFile.Append("FTP错误");
                //this.label1.Text = "浏览器错误";
                return;
            }

            // Check to see if the background process was cancelled.
            if (e.Cancelled)
            {
                this.strbFile.Append("任务撤销");
                //this.label1.Text = "任务撤销";
                return;
            }


        }

        private static void device_PcapOnPacketArrival(object sender, Packet packet)
        {
            PcapDevice device = (PcapDevice)sender;
            //if device has a dump file opened
            if (device.PcapDumpOpened)
            {
                //dump the packet to the file
                device.PcapDump(packet);
                //this.memoPcap.Text+="Packet dumped to file.\n";
            }
        }

        private void btnFTPEnd_Click(object sender, EventArgs e)
        {
            
            this.btnFTPStart.Enabled = true;
            CloseConnection();
            this.btnFTPEnd.Enabled = false;
            // reset context menu for the remote file list
            lvFiles.ContextMenu = null;
            device.PcapStopCapture();

            //device.PcapClose();
            DateTime dtEnd = DateTime.Now;
            TimeSpan ts = dtEnd - dtStart;
            float ts2 = ts.Seconds + (float)ts.Milliseconds / 1000;


            strbFile.Append("测试结束,耗时 " + ts.Minutes + "分 " + ts2.ToString() + "秒" + "\r\n");
            strbFile.Append("抓包文件: " + strFile + " 创建\r\n");

            if (!File.Exists(this.strLogFile))
            { //File.Create(this.strLogFile); }
                FileStream fs1 = new FileStream(this.strLogFile, FileMode.CreateNew, FileAccess.Write);
                StreamWriter sw = new StreamWriter(fs1, Encoding.Default);
                sw.Write(this.strbFile.ToString());
                sw.Close();
                fs1.Close();
            }
            else
            {
                FileStream fs1 = new FileStream(this.strLogFile, FileMode.Append, FileAccess.Write);
                StreamWriter sw = new StreamWriter(fs1, Encoding.Default);
                sw.Write(this.strbFile.ToString());
                sw.Close();
                fs1.Close();
            }
            //lstMessages.SelectionColor = Color.Blue;
            lstMessages.Items.Add("抓包文件: " + strFile + " 创建\r\n");
            //lstMessages.SelectionColor = Color.Blue;
            lstMessages.Items.Add("日志文件: " + strLogFile + "\r\n");
            lstMessages.EnsureVisible(lstMessages.Items.Count - 1);
            lstMessages.Invalidate();
            lstMessages.Update();
            this.btnFTPEnd.Enabled = false;
        }

        #region FTP commands

        /// <summary>
        /// Create a new Instance of FtpClient
        /// Then Login to ftp server
        /// </summary>
        private void Login(string address,string username,string password,string port,bool anonymous)
        {
            if (ftpClient == null)
            {
                ftpClient = new FtpClient(address, username, password, 10, Convert.ToInt16(port));
                ftpClient.lstMessage = this.lstMessages;// lstMessages;
            }
            try
            {
                ftpClient.Login();
            }
            catch (FtpClient.FtpException ex)
            {
                //Warning("", ex.Message);
                throw new Exception(ex.Message);
            }

        }

        /// <summary>
        /// Close connection to ftp server
        /// </summary>
        private void CloseConnection()
        {
            if (ftpClient != null)
                ftpClient.Close();
            ftpClient = null;
            lvFiles.Items.Clear();
        }

        /// <summary>
        /// Create a new Instance of FtpClient
        /// Then Login to ftp server
        /// </summary>
        private void GetFileList(string path)
        {
            string[] filelist;
            ListViewItem litem;
            string item = "";
            try
            {
                InitListView(lvFiles);

                if (txtPath.Text != "/")
                    addParentDirectory(lvFiles);

                filelist = ftpClient.GetFileList(path);
                for (int ii = 0; (ii < filelist.Length) && (filelist[ii] != ""); ii++)
                {
                    item = filelist[ii];

                    // fill listview according to format
                    // and OS
                    switch (ftpClient.ServerOS)
                    {
                        case 1:
                            // Unix format
                            litem = parseUnixData(filelist[ii]);
                            if (litem != null)
                                lvFiles.Items.Add(litem);
                            break;
                        case 2:
                            // Windows OS
                            switch (filelist[ii][0])
                            {
                                case '-':
                                case 'd':
                                case 'l':
                                    // Unix format
                                    litem = parseUnixData(filelist[ii]);
                                    if (litem != null)
                                        lvFiles.Items.Add(litem);
                                    break;
                                default:
                                    // DOS format
                                    litem = parseDosData(filelist[ii]);
                                    if (litem != null)
                                        lvFiles.Items.Add(litem);
                                    break;
                            }
                            break;

                        default:
                            litem = null;
                            break;
                    }



                }

                // Loop through and size each column header 
                // to fit the column header text.
                foreach (ColumnHeader ch in this.lvFiles.Columns)
                {
                    ch.Width = -2;
                }

                this.lvFiles.Sort();
            }
            catch (FtpClient.FtpException ex)
            {
                //MessageBox.Show(ex.Message);
            }
            catch (Exception ex)
            {
                lstMessages.Items.Add(ex.Message);
            }

        }



        /// <summary>
        /// Change remote dir
        /// </summary>
        /// <param name="dirname"></param>
        private void ChangeDir(string dirname)
        {
            try
            {
                if (ftpClient != null)
                {
                    txtPath.Text = ftpClient.ChangeDir(dirname);
                    GetFileList("");
                }
            }
            catch (FtpClient.FtpException ex)
            {
                //Warning("", ex.Message);
            }

        }

        /// <summary>
        /// Rename a file on the remote FTP server.
        /// </summary>
        /// <param name="oldFileName"></param>
        private void RenameFile(string oldFileName)
        {
            //string newFileName;
            //bool overwrite = false;

            //frmRenameFile frm = new frmRenameFile();
            //if ((newFileName = frm.ShowModal(oldFileName)) != oldFileName)
            //{
            //    try
            //    {
            //        if (ftpClient != null)
            //        {
            //            ftpClient.RenameFile(oldFileName, newFileName, overwrite);
            //        }
            //    }
            //    catch (FtpClient.FtpException ex)
            //    {
            //        Warning("", ex.Message);
            //    }

            //}
        }

        /// <summary>
        /// Delete a file from the remote FTP server.
        /// </summary>
        /// <param name="fileName"></param>
        private void DeleteFile(string fileName)
        {
            DialogResult dlgr = MessageBox.Show("Are you sure to delete '" + fileName + "'?", "Delete File", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dlgr == DialogResult.Yes)
            {
                try
                {
                    if (ftpClient != null)
                    {
                        UpdateStatusBar("Deleting....");
                        ftpClient.DeleteFile(fileName);

                    }

                }
                catch (FtpClient.FtpException ex)
                {
                    //Warning("", ex.Message);
                }
                catch (Exception ex)
                {
                    //Warning("Warning", ex.Message);
                }
                finally
                {
                    UpdateStatusBar("");
                }
            }
        }


        /// <summary>
        /// Upload a directory and its file contents
        /// </summary>
        /// <param name="path"></param>
        /// <param name="recurse">Whether to recurse sub directories</param>
        private void UploadDirectory(string path, bool recurse)
        {
            try
            {
                if (ftpClient != null)
                {
                    UpdateStatusBar("Uploading....");
                    this.Cursor = Cursors.WaitCursor;
                    ftpClient.UploadDirectory(path, recurse);
                }
            }
            catch (FtpClient.FtpException ex)
            {
                //Warning("", ex.Message);
            }
            catch (Exception ex)
            {
                //Warning("Warning", ex.Message);
            }
            finally
            {
                this.Cursor = Cursors.Default;
                UpdateStatusBar("");
            }
        }

        /// <summary>
        /// Upload a file and set the resume flag.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="resume"></param>
        private void UploadFile(string fileName, bool resume)
        {
            try
            {
                if (ftpClient != null)
                {
                    resume = false;
                    UpdateStatusBar("Uploading....");
                    this.Cursor = Cursors.WaitCursor;
                    ftpClient.Upload(fileName, resume);
                }
            }
            catch (FtpClient.FtpException ex)
            {
                //Warning("", ex.Message);
            }
            catch (Exception ex)
            {
                //Warning("Warning", ex.Message);
            }
            finally
            {
                this.Cursor = Cursors.Default;
                UpdateStatusBar("");
            }
        }

        /// <summary>
        /// Download a remote file to a local file name which can include
        /// a path, and set the resume flag. The local file name will be
        /// created or overwritten, but the path must exist.
        /// </summary>
        /// <param name="remFileName"></param>
        /// <param name="locFileName"></param>
        /// <param name="resume"></param>
        public void DownloadFile(string remFileName, string locFileName, Boolean resume)
        {
            try
            {
                if (ftpClient != null)
                {
                    resume = false;
                    UpdateStatusBar("Downloading....");
                    this.Cursor = Cursors.WaitCursor;
                    ftpClient.Download(remFileName, locFileName, resume);
                }
            }
            catch (FtpClient.FtpException ex)
            {
                //Warning("", ex.Message);
            }
            catch (Exception ex)
            {
                //Warning("Warning", ex.Message);
            }
            finally
            {
                this.Cursor = Cursors.Default;
                UpdateStatusBar("");
            }

        }

        /// <summary>
        /// Rename a local file.
        /// </summary>
        /// <param name="oldFileName"></param>
        private void RenameLocalFile(string oldFileName)
        {
            //string newFileName;

            //frmRenameFile frm = new frmRenameFile();
            //if ((newFileName = frm.ShowModal(oldFileName)) != oldFileName)
            //{
            //    try
            //    {
            //        File.Move(sLocalPath + "\\" + oldFileName, sLocalPath + "\\" + newFileName);
            //    }
            //    catch (Exception ex)
            //    {
            //        Warning("", ex.Message);
            //    }

            //}
        }

        /// <summary>
        /// Delete a local file.
        /// </summary>
        /// <param name="fileName"></param>
        private void DeleteLocalFile(string fileName)
        {
            DialogResult dlgr = MessageBox.Show("Are you sure to delete '" + fileName + "'?", "Delete File", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dlgr == DialogResult.Yes)
            {
                try
                {
                    this.Cursor = Cursors.WaitCursor;
                    UpdateStatusBar("Deleting....");
                    File.Delete(sLocalPath + "\\" + fileName);

                }
                catch (Exception ex)
                {
                    //Warning("Warning", ex.Message);
                }
                finally
                {
                    this.Cursor = Cursors.Default;
                    UpdateStatusBar("");
                }
            }
        }



        #endregion
        
        #region lvLocal events
        private void lvLocalFiles_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            // For the moment I use only column 1 (name) to sort list view
            if (e.Column != 1)
                return;

            // Determine if clicked column is already the column that is being sorted.
            if (e.Column == lvwLocalColumnSorter.SortColumn)
            {
                // Reverse the current sort direction for this column.
                if (lvwLocalColumnSorter.Order == SortOrder.Ascending)
                {
                    lvwLocalColumnSorter.Order = SortOrder.Descending;
                }
                else
                {
                    lvwLocalColumnSorter.Order = SortOrder.Ascending;
                }
            }
            else
            {
                // Set the column number that is to be sorted; default to ascending.
                lvwLocalColumnSorter.SortColumn = e.Column;
                lvwLocalColumnSorter.Order = SortOrder.Ascending;
            }

            // Perform the sort with these new sort options.
            this.lvLocalFiles.Sort();
        }

        private void lvLocalFiles_DoubleClick(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection selIndex = lvLocalFiles.SelectedIndices;
            int i = selIndex[0];

            if ((i >= 0) && (lvLocalFiles.Items[i].ImageIndex == INDEX_TAG_DIR))
            {
                ChangeLocalDir(lvLocalFiles.Items[i].SubItems[1].Text);
            }
        }

        private void lvLocalFiles_DragDrop(object sender, DragEventArgs e)
        {
            if ((CurrentEffect == DragDropEffects.Copy) && (((ListView)sender).Name != startingFrom))
            {
                //MessageBox.Show("download: " + lvDragItem.SubItems[1].Text);
                this.LocalDown = sLocalPath + (sLocalPath.EndsWith("\\") ? "" : "\\") + lvDragItem.SubItems[1].Text;
                this.RemoteDown=sRemotePath + (sRemotePath.EndsWith("/") ? "" : "/") + lvDragItem.SubItems[1].Text;
                DownloadFile(sRemotePath + (sRemotePath.EndsWith("/") ? "" : "/") + lvDragItem.SubItems[1].Text, sLocalPath + (sLocalPath.EndsWith("\\") ? "" : "\\") + lvDragItem.SubItems[1].Text, false);
                //PopulateLocalFileList();
            }
        }

        private void lvLocalFiles_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = CurrentEffect;
        }

        private void lvLocalFiles_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = CurrentEffect;
        }

        private void lvLocalFiles_ItemDrag(object sender, ItemDragEventArgs e)
        {
            int i = ((ListView)sender).SelectedIndices[0];
            ListViewItem lvItem = ((ListView)sender).Items[i];
            int imgindex = lvItem.ImageIndex;

            // try to drag a file
            //			if (lvItem.ImageIndex != INDEX_TAG_DIR)
            {
                Bitmap bmp = (Bitmap)imageList1.Images[imgindex];

                lvDragItem = lvItem;
                CurrentEffect = DragDropEffects.Copy;

                startingFrom = ((ListView)sender).Name;
                this.DoDragDrop(bmp, CurrentEffect);
            }
        }

        private void lvLocalFiles_KeyDown(object sender, KeyEventArgs e)
        {
            ListViewItem lvItem;
            ListView lv = (ListView)sender;

            if (e.KeyCode == Keys.F5)
            {
                // refresh list
                PopulateLocalFileList();
            }
            else
            {
                // to delete or rename a file
                // at least one item must be select
                if (lv.SelectedIndices.Count > 0)
                {
                    // listview is not multi select
                    // then there is only 1 item selected.
                    // This is the item selected
                    lvItem = lv.SelectedItems[0];

                    switch (e.KeyCode)
                    {
                        case Keys.Delete:
                            // here the code to delete a file
                            if (lvItem.ImageIndex == INDEX_TAG_FILE)
                            {
                                DeleteLocalFile(lvItem.SubItems[1].Text);
                                PopulateLocalFileList();
                            }
                            // here put code to delete directory
                            break;

                        case Keys.F2:
                            // rename file
                            if (lvItem.ImageIndex == INDEX_TAG_FILE)
                            {
                                RenameLocalFile(lvItem.SubItems[1].Text);
                                PopulateLocalFileList();
                            }
                            // here put code to rename directory
                            break;

                    }

                }
            }
        }

        #endregion

        #region remote view events
        private void lvFiles_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            // For the moment I use only column 1 (name) to sort list view
            if (e.Column != 1)
                return;

            // Determine if clicked column is already the column that is being sorted.
            if (e.Column == lvwColumnSorter.SortColumn)
            {
                // Reverse the current sort direction for this column.
                if (lvwColumnSorter.Order == SortOrder.Ascending)
                {
                    lvwColumnSorter.Order = SortOrder.Descending;
                }
                else
                {
                    lvwColumnSorter.Order = SortOrder.Ascending;
                }
            }
            else
            {
                // Set the column number that is to be sorted; default to ascending.
                lvwColumnSorter.SortColumn = e.Column;
                lvwColumnSorter.Order = SortOrder.Ascending;
            }

            // Perform the sort with these new sort options.
            this.lvFiles.Sort();
        }

        private void lvFiles_DoubleClick(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection selIndex = lvFiles.SelectedIndices;
            int i = selIndex[0];

            if ((i >= 0) && ((lvFiles.Items[i].ImageIndex == INDEX_TAG_DIR) || (lvFiles.Items[i].ImageIndex == INDEX_TAG_LINK)))
            {
                ChangeDir(lvFiles.Items[i].SubItems[1].Text);
            }
        }

        private void lvFiles_DragDrop(object sender, DragEventArgs e)
        {
            if ((CurrentEffect == DragDropEffects.Copy) && (((ListView)sender).Name != startingFrom))
            {
                if (lvDragItem.ImageIndex == INDEX_TAG_FILE)
                    UploadFile(sLocalPath + (sLocalPath.EndsWith("\\") ? "" : "\\") + lvDragItem.SubItems[1].Text, false);
                else if (lvDragItem.ImageIndex == INDEX_TAG_DIR)
                    UploadDirectory(sLocalPath + (sLocalPath.EndsWith("\\") ? "" : "\\") + lvDragItem.SubItems[1].Text, true);
                GetFileList("");
            }
        }

        private void lvFiles_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = CurrentEffect;
        }

        private void lvFiles_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = CurrentEffect;
        }

        private void lvFiles_ItemDrag(object sender, ItemDragEventArgs e)
        {
            int i = ((ListView)sender).SelectedIndices[0];
            ListViewItem lvItem = ((ListView)sender).Items[i];
            int imgindex = lvItem.ImageIndex;

            // try to drag a file
            if (lvItem.ImageIndex != INDEX_TAG_DIR)
            {
                Bitmap bmp = (Bitmap)imageList1.Images[imgindex];

                lvDragItem = lvItem;
                CurrentEffect = DragDropEffects.Copy;
                startingFrom = ((ListView)sender).Name;

                this.DoDragDrop(bmp, CurrentEffect);
            }
        }

        private void lvFiles_KeyDown(object sender, KeyEventArgs e)
        {
            ListViewItem lvItem;
            ListView lv = (ListView)sender;

            if (e.KeyCode == Keys.F5)
            {
                // refresh list
                GetFileList("");
            }
            else
            {
                // to rename or delete a file
                // at least one item must be select
                if (lv.SelectedIndices.Count > 0)
                {
                    // listview is not multi select
                    // then there is only 1 item selected.
                    // This is the item selected
                    lvItem = lv.SelectedItems[0];

                    switch (e.KeyCode)
                    {
                        case Keys.Delete:
                            // here the code to delete a file
                            if (lvItem.ImageIndex == INDEX_TAG_FILE)
                            {
                                DeleteFile(lvItem.SubItems[1].Text);
                                GetFileList("");
                            }
                            // here put code to delete directory
                            break;

                        case Keys.F2:
                            // rename file
                            if (lvItem.ImageIndex == INDEX_TAG_FILE)
                            {
                                RenameFile(lvItem.SubItems[1].Text);
                                GetFileList("");
                            }
                            // here put code to rename directory
                            break;

                    }

                }
            }
        }
        #endregion

        #region local context menu events
        private void menuItem4_Click(object sender, EventArgs e)
        {
            PopulateLocalFileList();
        }

        private void menuItem5_Click(object sender, EventArgs e)
        {
            ListViewItem lvItem;
            ListView lv = (ListView)(contextMenuLocal.SourceControl);

            if (lv.SelectedIndices.Count > 0)
            {
                lvItem = lv.SelectedItems[0];

                RenameLocalFile(lvItem.SubItems[1].Text);
                PopulateLocalFileList();
            }
        }

        private void menuItem6_Click(object sender, EventArgs e)
        {
            ListViewItem lvItem;
            ListView lv = (ListView)(contextMenuLocal.SourceControl);

            if (lv.SelectedIndices.Count > 0)
            {
                lvItem = lv.SelectedItems[0];

                if (lvItem.ImageIndex == INDEX_TAG_FILE)
                {
                    DeleteLocalFile(lvItem.SubItems[1].Text);
                    PopulateLocalFileList();
                }
            }
        }

        private void contextMenuLocal_Popup(object sender, EventArgs e)
        {
            // we can delete only files
            ListViewItem lvItem;
            ListView lv = (ListView)(contextMenuLocal.SourceControl);

            if (lv.SelectedIndices.Count > 0)
            {
                lvItem = lv.SelectedItems[0];

                menuItem5.Enabled = true;
                if (lvItem.ImageIndex != INDEX_TAG_FILE)
                {
                    menuItem6.Enabled = false;
                    if (lvItem.SubItems[1].Text == "..")
                        menuItem5.Enabled = false;
                }
                else
                    menuItem6.Enabled = true;
            }
            else
            {
                menuItem6.Enabled = false;
                menuItem5.Enabled = false;
            }
        }

        #endregion

        #region remote context menu events
        private void menuItem1_Click(object sender, EventArgs e)
        {
            GetFileList("");
        }

        private void menuItem2_Click(object sender, EventArgs e)
        {
            // rename
            ListViewItem lvItem;
            ListView lv = (ListView)(contextMenuRemote.SourceControl);

            if (lv.SelectedIndices.Count > 0)
            {
                lvItem = lv.SelectedItems[0];
                RenameFile(lvItem.SubItems[1].Text);
                GetFileList("");

            }
        }

        private void menuItem3_Click(object sender, EventArgs e)
        {
            // delete
            ListViewItem lvItem;
            ListView lv = (ListView)(contextMenuRemote.SourceControl);

            if (lv.SelectedIndices.Count > 0)
            {
                lvItem = lv.SelectedItems[0];
                if (lvItem.ImageIndex == INDEX_TAG_FILE)
                {
                    DeleteFile(lvItem.SubItems[1].Text);
                    GetFileList("");
                }

            }
        }

        private void contextMenuRemote_Popup(object sender, EventArgs e)
        {
            ListViewItem lvItem;
            ListView lv = (ListView)(contextMenuRemote.SourceControl);

            if (lv.SelectedIndices.Count > 0)
            {
                lvItem = lv.SelectedItems[0];

                menuItem2.Enabled = true;
                if (lvItem.ImageIndex != INDEX_TAG_FILE)
                {
                    menuItem3.Enabled = false;
                    if (lvItem.SubItems[1].Text == "..")
                        menuItem2.Enabled = false;
                }
                else
                    menuItem3.Enabled = true;
            }
            else
            {
                menuItem3.Enabled = false;
                menuItem2.Enabled = false;
            }
        }

        #endregion

        #region Utility

        /// <summary>
        /// write a message in the status bar
        /// </summary>
        /// <param name="message"></param>
        private void UpdateStatusBar(string message)
        {
            statusBar1.Panels[0].Text = message;
        }


        /// <summary>
        /// parse dos file data
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private ListViewItem parseDosData(string text)
        {
            int imageindex = INDEX_TAG_FILE; //default file (1)

            string[] lvData = new string[4];

            try
            {
                //12-29-02  11:41PM <DIR> Apps
                //01-34-67--01-3456
                string month, day, year;
                string[] test = text.Split(Convert.ToChar(" "));
                if (test.Length < 6)
                    throw new ApplicationException();
                string parse = text;
                month = parse.Substring(0, 2);
                day = parse.Substring(3, 2);
                year = parse.Substring(6, 2);
                string hour = parse.Substring(10, 2);
                string min = parse.Substring(13, 2);
                string tod = parse.Substring(15, 2);
                parse = parse.Substring(17);
                long size = 0;

                while (parse.StartsWith(" "))
                    parse = parse.Substring(1);
                if (parse.StartsWith("<DIR>"))
                {
                    imageindex = INDEX_TAG_DIR;
                    size = 0;
                    parse = parse.Substring(5);
                    while (parse.StartsWith(" "))
                        parse = parse.Substring(1);
                }
                else
                {
                    size = long.Parse(parse.Split(char.Parse(" "))[0]);
                    parse = parse.Substring(parse.Split(char.Parse(" "))[0].Length);
                }
                string filename = parse;

                // I use a treshold to set year
                if (Convert.ToInt16(year) < 70)
                    year = "20" + year;
                else
                    year = "19" + year;

                int hr = int.Parse(hour);
                if (tod.ToUpper() == "PM")
                {
                    if (hr != 12)
                        hr += 12;
                    else
                        if (hr == 12)
                            hr = 0;
                }
                DateTime dt = DateTime.Parse(month + " " + day + " " + year, new System.Globalization.CultureInfo("en-US"));
                dt = new DateTime(dt.Year, dt.Month, dt.Day, hr, int.Parse(min), 0);

                lvData[0] = "";
                lvData[1] = filename.Trim();
                lvData[2] = formatSize(size);
                lvData[3] = Convert.ToString(dt);

            }
            catch
            {
                return null;
            }


            //Create actual list item
            ListViewItem lvItem = new ListViewItem(lvData, imageindex);
            return lvItem;

        }


        /// <summary>
        /// parse unix file data
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private ListViewItem parseUnixData(string text)
        {
            int imageindex = INDEX_TAG_FILE; //default file (1)

            string[] lvData = new string[4];

            string originalText = text;

            if (text == null)
                throw new ArgumentNullException("text");

            try
            {
                bool ufnisDir = false;
                bool ufnisLink = false;
                int inode, year;
                string month, day;
                string ufnext = "";

                if ((text.Split(char.Parse(" "))).Length < 9)
                    throw new ApplicationException();

                // remove link reference
                if (text.IndexOf("->") >= 0)
                    text = text.Remove(text.IndexOf("->"), text.Length - text.IndexOf("->"));

                text = text.Trim();

                // remove multiple space char
                {
                    string strtemp = "";
                    int kk;

                    for (kk = 0; kk < text.Length - 1; kk++)
                    {
                        if ((text[kk] != text[kk + 1]) || (text[kk] != ' '))
                            strtemp += text[kk];
                    }
                    strtemp += text[kk];

                    text = strtemp;
                }

                string[] test = text.Split(char.Parse(" "));
                string[] nfo = new string[9];

                if (test.Length < 9)
                {
                    int kk;
                    for (kk = 0; kk < 3; kk++)
                        nfo[kk] = test[kk];
                    nfo[kk] = "";
                    for (; kk < test.Length; kk++)
                        nfo[kk + 1] = test[kk];
                }
                else
                {
                    int i;
                    for (i = 0; i < nfo.Length; i++)
                        nfo[i] = test[i];

                    if (test.Length > 9)
                    {
                        for (i = 9; i < test.Length; i++)
                            nfo[8] += ' ' + test[i];
                    }
                }

                string ufnpermissions = nfo[0];
                if (ufnpermissions.Length != 10)
                    throw new ApplicationException();

                if (ufnpermissions.StartsWith("d"))
                    ufnisDir = true;
                else if (ufnpermissions.StartsWith("l"))
                {
                    ufnisDir = true;
                    ufnisLink = true;
                }
                else
                {
                    ufnisDir = false;
                    ufnisLink = false;
                    if (text.IndexOf(".") >= 0)
                        ufnext = text.Substring(text.IndexOf("."));
                }

                inode = int.Parse(nfo[1]);
                string ufnowner = nfo[2];
                string ufngroup = nfo[3];
                long ufnsize;

                if (ufnisDir)
                {
                    ufnsize = 0;
                    if (ufnisLink)
                        imageindex = INDEX_TAG_LINK;
                    else
                        imageindex = INDEX_TAG_DIR;
                }
                else
                    ufnsize = long.Parse(nfo[4]);

                month = nfo[5];
                day = nfo[6];
                int hour = 0;
                int minute = 0;
                if (nfo[7].IndexOf(":") == -1)
                    year = int.Parse(nfo[7]);
                else //file made in last 6 months
                {
                    year = DateTime.Now.Year;
                    hour = int.Parse(nfo[7].Substring(0, nfo[7].IndexOf(":")));
                    minute = int.Parse(nfo[7].Substring(nfo[7].IndexOf(":") + 1));
                }
                string ufnfilename = nfo[8];


                DateTime ufndt = DateTime.Parse(month + " " + day + " " + year, new System.Globalization.CultureInfo("en-US"));
                if (ufndt.Month > DateTime.Now.Month) --year;
                ufndt = new DateTime(year, ufndt.Month, ufndt.Day, hour, minute, 0);


                lvData[0] = "";
                lvData[1] = ufnfilename.Trim();
                lvData[2] = formatSize(ufnsize);
                lvData[3] = Convert.ToString(ufndt);


            }
            catch
            {
                return null;
            }


            //Create actual list item
            ListViewItem lvItem = new ListViewItem(lvData, imageindex);

            return lvItem;

        }


        /// <summary>
        /// Add an item representing parent directory
        /// </summary>
        /// <param name="lvF"></param>
        private void addParentDirectory(ListView lvF)
        {
            string[] lvData = new string[4];
            int imageindex = 0; //directory

            lvData[0] = "";
            lvData[1] = "..";
            lvData[2] = "";
            lvData[3] = "";
            //Create actual list item
            ListViewItem lvItem = new ListViewItem(lvData, imageindex);
            lvF.Items.Add(lvItem);
        }

        /// <summary>
        /// returning directory name
        /// </summary>
        /// <param name="stringPath"></param>
        protected string GetPathName(string stringPath)
        {
            //Get Name of folder
            string[] stringSplit = stringPath.Split('\\');
            int _maxIndex = stringSplit.Length;
            return stringSplit[_maxIndex - 1];
        }


        /// <summary>
        /// formatting date
        /// </summary>
        /// <param name="dtDate"></param>
        protected string formatDate(DateTime dtDate)
        {
            //Get date and time in short format
            string stringDate = "";

            stringDate = dtDate.ToShortDateString().ToString() + " " + dtDate.ToShortTimeString().ToString();

            return stringDate;
        }

        /// <summary>
        /// formatting size
        /// </summary>
        /// <param name="lSize"></param>
        protected string formatSize(Int64 lSize)
        {
            //Format number to KB
            string stringSize = "";
            NumberFormatInfo myNfi = new NumberFormatInfo();

            Int64 lKBSize = 0;

            if (lSize < 1024)
            {
                if (lSize == 0)
                {
                    //zero byte
                    stringSize = "0";
                }
                else
                {
                    //less than 1K but not zero byte
                    stringSize = "1";
                }
            }
            else
            {
                //convert to KB
                lKBSize = lSize / 1024;
                //format number with default format
                stringSize = lKBSize.ToString("n", myNfi);
                //remove decimal
                stringSize = stringSize.Replace(".00", "");
            }

            return stringSize + " KB";
        }

        /// <summary>
        /// Show warning message box
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="message"></param>
        private void Warning(string caption, string message)
        {
            if (caption == "")
                caption = "Warning";
            MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        #endregion

        protected void InitLocalListView()
        {
            txtLocalPath.Text = Application.StartupPath;
            PopulateLocalFileList();
        }

        protected void ChangeLocalDir(string newdir)
        {
            if (newdir == "..")
            {
                int l = txtLocalPath.Text.LastIndexOf('\\');
                txtLocalPath.Text = txtLocalPath.Text.Substring(0, l);
            }
            else
            {
                if (newdir.Length > 0)
                {
                    if (txtLocalPath.Text[txtLocalPath.Text.Length - 1] != '\\')
                        txtLocalPath.Text += "\\";
                    txtLocalPath.Text += newdir;
                }
            }
            if (txtLocalPath.Text.Length < 3)
                txtLocalPath.Text += "\\";

            PopulateLocalFileList();
        }

        protected void PopulateLocalFileList()
        {
            //Populate listview with files
            string[] lvData = new string[4];
            string sPath = txtLocalPath.Text;

            InitListView(lvLocalFiles);


            if (sPath.Length > 3)
                addParentDirectory(lvLocalFiles);
            else
            {

            }
            try
            {
                string[] stringDir = Directory.GetDirectories(sPath);
                string[] stringFiles = Directory.GetFiles(sPath);

                string stringFileName = "";
                DateTime dtCreateDate, dtModifyDate;
                Int64 lFileSize = 0;

                foreach (string stringFile in stringDir)
                {
                    stringFileName = stringFile;
                    FileInfo objFileSize = new FileInfo(stringFileName);
                    lFileSize = 0;
                    dtCreateDate = objFileSize.CreationTime; //GetCreationTime(stringFileName);
                    dtModifyDate = objFileSize.LastWriteTime; //GetLastWriteTime(stringFileName);

                    //create listview data
                    lvData[0] = "";
                    lvData[1] = GetPathName(stringFileName);
                    lvData[2] = formatSize(lFileSize);
                    lvData[3] = formatDate(dtModifyDate);

                    //Create actual list item
                    ListViewItem lvItem = new ListViewItem(lvData, 0); // 0 = directory
                    lvLocalFiles.Items.Add(lvItem);

                }

                foreach (string stringFile in stringFiles)
                {
                    stringFileName = stringFile;
                    FileInfo objFileSize = new FileInfo(stringFileName);
                    lFileSize = objFileSize.Length;
                    dtCreateDate = objFileSize.CreationTime; //GetCreationTime(stringFileName);
                    dtModifyDate = objFileSize.LastWriteTime; //GetLastWriteTime(stringFileName);

                    //create listview data
                    lvData[0] = "";
                    lvData[1] = GetPathName(stringFileName);
                    lvData[2] = formatSize(lFileSize);
                    lvData[3] = formatDate(dtModifyDate);

                    //Create actual list item
                    ListViewItem lvItem = new ListViewItem(lvData, 1); // 1 = file
                    lvLocalFiles.Items.Add(lvItem);


                }

                // Loop through and size each column header to fit the column header text.
                foreach (ColumnHeader ch in this.lvLocalFiles.Columns)
                {
                    ch.Width = -2;
                }

            }
            catch (IOException)
            {
                MessageBox.Show("Error: Drive not ready or directory does not exist.");
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("Error: Drive or directory access denided.");
            }
            catch (Exception ee)
            {
                MessageBox.Show("Error: " + ee);
            }
            lvLocalFiles.Invalidate();
            lvLocalFiles.Update();

        }


        protected void InitListView(ListView lvFiles)
        {
            //init ListView control
            lvFiles.Clear();		//clear control
            //create column header for ListView
            lvFiles.Columns.Add("", 22, System.Windows.Forms.HorizontalAlignment.Center);
            lvFiles.Columns.Add("名称", 140, System.Windows.Forms.HorizontalAlignment.Left);
            lvFiles.Columns.Add("大小", 60, System.Windows.Forms.HorizontalAlignment.Right);
            lvFiles.Columns.Add("修改日期", 90, System.Windows.Forms.HorizontalAlignment.Left);


        }

        private void txtLocalPath_TextChanged(object sender, EventArgs e)
        {
            sLocalPath = ((TextBox)sender).Text.Trim();
        }

        private void txtPath_TextChanged(object sender, EventArgs e)
        {
            sRemotePath = ((TextBox)sender).Text.Trim();
        }

        private void txtLocalPath_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 0xd)
            {
                ChangeLocalDir("");
            }
        }

        private void txtPath_KeyPress(object sender, KeyPressEventArgs e)
        {
            if ((e.KeyChar == 0xd) && (ftpClient != null))
            {
                ChangeDir(txtPath.Text);
            }
        }

        private void btnDown_Click(object sender, EventArgs e)
        {
            if ((this.RemoteDown != null) && (this.LocalDown != null))
            {

                this.FTPDown();//DownloadFile(RemoteDown, LocalDown, false);
            }
            else
            {
                this.statusBar1.Text = "未选择下载文件...";
                MessageBox.Show("请先手动下载一次");
            }
        }

        private void FTPDown()
        {
            iDownNum++;
            this.btnFTPStart.Enabled = false;
            this.lstMessages.Items.Clear();
            this.btnFTPEnd.Enabled = true;
            if (strbFile.Length > 0) strbFile.Remove(0, strbFile.Length);

            strFile = inis.IniReadValue("FTP", "Path") + "\\FTP-" + DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString()
            + "-" + DateTime.Now.Day.ToString() + "-" + DateTime.Now.Hour.ToString() + "-" + DateTime.Now.Minute.ToString() + "-" +
            DateTime.Now.Second.ToString() + ".cap";  // inis.IniReadValue("FTP","Host")+
            strLogFile = inis.IniReadValue("FTP", "Path") + "\\FTP-" + DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString()
                + DateTime.Now.Day.ToString() + "-" + DateTime.Now.Hour.ToString() + "-" + DateTime.Now.Minute.ToString() + "-" +
                DateTime.Now.Second.ToString() + ".txt";

            if (!Directory.Exists(inis.IniReadValue("FTP", "Path"))) Directory.CreateDirectory(inis.IniReadValue("FTP", "Path"));
            DownFinish = false;
            this.btnDown.Enabled = false;

            this.timerDown.Enabled = true;
            this.timerDown.Start();

            iDevice = int.Parse(inis.IniReadValue("FTP", "Adapter"));

            PcapDeviceList devices = SharpPcap.GetAllDevices();

            device = devices[iDevice];
            string ip = device.PcapIpAddress;
            strbFile.Append("网卡: " + device.PcapDescription + "\r\n");
            strbFile.Append("IP地址: " + device.PcapIpAddress + "\r\n");
            strbFile.Append("目的地址: " + inis.IniReadValue("FTP", "Host") + "\r\n");
            strbFile.Append("测试开始时间: " + dtStart.ToString() + "\r\n");

            Thread.Sleep(100);

            this.dtStart = DateTime.Now;
            strbFile.Append("测试开始时间: " + dtStart.ToString() + "\r\n");


            //Register our handler function to the 'packet arrival' event
            device.PcapOnPacketArrival +=
                new SharpPcap.PacketArrivalEvent(device_PcapOnPacketArrival);
            device.PcapOpen(true, 100);
            device.PcapSetFilter("(tcp or udp) and host " + ip);
            device.PcapDumpOpen(strFile);
            if (!m_AsyncWorker.IsBusy)
            {
                m_AsyncWorker.RunWorkerAsync();
            }
            try
            {
                connectiondata conndata = new connectiondata();
                try
                {
                    conndata.address = inis.IniReadValue("FTP", "Host");
                    conndata.username = inis.IniReadValue("FTP", "User");
                    conndata.password = indll.IniReadValue("FTP", "Pass");
                    conndata.port = inis.IniReadValue("FTP", "Port");
                    conndata.anonymous = false;
                    Login(conndata.address, conndata.username, conndata.password, conndata.port, conndata.anonymous);
                    this.btnFTPStart.Enabled = false;
                    this.btnFTPEnd.Enabled = true;
                    DownloadFile(RemoteDown, LocalDown, false);
                    DownFinish = true;
                    //lvwColumnSorter.Order = SortOrder.Ascending;
                    //ChangeDir("/");
                    //lvFiles.AllowDrop = true;

                    // set context menu for the remote file list
                    lvFiles.ContextMenu = contextMenuRemote;

                    //reset address changement
                    //txtAddress.TextChanged -= new System.EventHandler(this.txtAddress_TextChanged);
                }
                catch
                {
                    ftpClient.Close();
                    ftpClient = null;
                    btnFTPStart.Enabled = true;
                    btnFTPEnd.Enabled = false;
                    lvFiles.AllowDrop = false;
                    DownFinish = true;
                }

            }
            catch (Exception ex)
            {
                strbFile.Append(ex.Message);
                //return;
            }


        }

        private void timerDown_Tick(object sender, EventArgs e)
        {
            if (!this.DownFinish) return;
            else
            {
                this.timerDown.Stop();
                this.timerDown.Enabled = false;
                CloseConnection();
                this.btnFTPEnd.Enabled = false;
                // reset context menu for the remote file list
                lvFiles.ContextMenu = null;
                device.PcapStopCapture();

                //device.PcapClose();
                DateTime dtEnd = DateTime.Now;
                TimeSpan ts = dtEnd - dtStart;
                float ts2 = ts.Seconds + (float)ts.Milliseconds / 1000;


                strbFile.Append("测试结束,耗时 " + ts.Minutes + "分 " + ts2.ToString() + "秒" + "\r\n");
                strbFile.Append("抓包文件: " + strFile + " 创建\r\n");

                if (!File.Exists(this.strLogFile))
                { //File.Create(this.strLogFile); }
                    FileStream fs1 = new FileStream(this.strLogFile, FileMode.CreateNew, FileAccess.Write);
                    StreamWriter sw = new StreamWriter(fs1, Encoding.Default);
                    sw.Write(this.strbFile.ToString());
                    sw.Close();
                    fs1.Close();
                }
                else
                {
                    FileStream fs1 = new FileStream(this.strLogFile, FileMode.Append, FileAccess.Write);
                    StreamWriter sw = new StreamWriter(fs1, Encoding.Default);
                    sw.Write(this.strbFile.ToString());
                    sw.Close();
                    fs1.Close();
                }
                //lstMessages.SelectionColor = Color.Blue;
                lstMessages.Items.Add("抓包文件: " + strFile + " 创建\r\n");
                //lstMessages.SelectionColor = Color.Blue;
                lstMessages.Items.Add("日志文件: " + strLogFile + "\r\n");
                lstMessages.EnsureVisible(lstMessages.Items.Count - 1);
                lstMessages.Invalidate();
                lstMessages.Update();
                if (iDownNum <= int.Parse(inis.IniReadValue("FTP", "DownNum")))
                {
                    Thread.Sleep(2000);
                    //this.DownFinish = false;
                    this.FTPDown();
                    
                }
                else
                {
                    this.iDownNum = 0;
                    this.DownFinish = false;
                    this.btnDown.Enabled = true;
                    this.btnFTPStart.Enabled = true;
                }
                
                
            }
        }
    }
}
