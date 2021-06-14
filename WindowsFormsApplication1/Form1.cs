using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        List<FileSystemWatcher> DisksWatchers;
        string folderAbove;
        string pathNow;

        public Form1()
        {
            InitializeAll();
        }

        private void InitializeAll()
        {
            InitializeComponent();

            DisksWatchers = new List<FileSystemWatcher>();

            treeView.ImageList = GetIconsToImageList();
            treeView.BeforeExpand += TreeView_BeforeExpand;
            treeView.AfterCollapse += TreeView_AfterCollapse;
            BuildTreeView();
            InitListView();
            Icon = Icon.ExtractAssociatedIcon(@"folder.ico");
        }

        public Form1(string text)
        {
            InitializeAll();
            ShowDirsAndFiles(text);
        }


        private void InitListView()
        {
            toolStripComboBox1.Items.AddRange(Enum.GetNames(typeof(View)));
            ImageList large = GetIconsToImageList();
            large.ImageSize = new Size(48, 48);
            listView.LargeImageList = large;
            listView.SmallImageList = GetIconsToImageList();


            toolStripComboBox1.SelectedIndex = 0;
        }

        private ImageList GetIconsToImageList()
        {
            string pathIconsFolder = @"..\..\Icons";
            ImageList listImg = new ImageList();
            string[] namesPictures = Directory.GetFiles(pathIconsFolder);
            foreach (string name in namesPictures)
            {
                Image img = Image.FromFile(name);
                listImg.Images.Add(name, img);
            }

            return listImg;
        }

        private void TreeView_AfterCollapse(object sender, TreeViewEventArgs e)
        {
            if (e.Node != null)
            {
                if (IsFolder(e.Node))
                    e.Node.ImageIndex = 3;
            }
        }

        private void TreeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Node != null)
            {
                if (IsFolder(e.Node))
                {
                    e.Node.ImageIndex = 4;
                }
                CreateNodes(e.Node.Name, e.Node, true);
                treeView.SelectedNode = e.Node;
            }
        }

        private bool IsFolder(TreeNode node)
        {
            foreach (string nameDisk in Directory.GetLogicalDrives())
            {
                if (node.Name == nameDisk)
                    return false;
            }
            string name = node.Name;
            if (IsArchive(node))
                return false;
            return true;
        }

        private bool IsArchive(TreeNode node)
        {
            if (Path.GetExtension(node.Name) == ".rar" || Path.GetExtension(node.Name) == ".7z" || Path.GetExtension(node.Name) == ".zip")
                return true;
            else
                return false;
        }

        private void SelectedFolderWatcher_Renamed(object sender, RenamedEventArgs e)
        {
            TreeNode[] node = treeView.Nodes.Find(e.OldFullPath, true);

            string path = e.OldFullPath.Substring(0, e.OldFullPath.LastIndexOf('\\') + 1);
            if (path.Length > 3)
                path = path.Substring(0, path.LastIndexOf('\\'));
            try
            {
                TreeNode n = node[0];
                if (node[0] != null)
                {
                    node[0].Name = e.FullPath;
                    node[0].Text = e.Name.Substring(e.Name.LastIndexOf('\\') + 1);
                }
            }
            catch (IndexOutOfRangeException ex)
            {
                ;
            }
            catch (Exception ex)
            {
                //statusLabel.Text = ex.Message + " (" + ex.InnerException + ")";
            }
            finally
            {
                if (path == pathNow)
                    ShowDirsAndFiles(path);
            }

        }

        private TreeNode FindNode(string s, TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Name == s)
                    return node;

                TreeNode n = FindNode(s, node.Nodes);
                if (n != null)
                    return n;
            }

            return null;
        }

        private void SelectedFolderWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            string path = e.FullPath.Substring(0, e.FullPath.LastIndexOf('\\') + 1);
            if (path.Length > 3)
                path = path.Substring(0, path.LastIndexOf('\\'));
            string name = e.FullPath.Substring(e.FullPath.LastIndexOf('\\') + 1);
            TreeNode node = null;
            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Created:
                    node = FindNode(path, treeView.Nodes);
                    if (node != null)
                    {
                        this.Invoke(new MethodInvoker(delegate ()
                        {
                            bool isExp = node.IsExpanded;
                            node.Nodes.Clear();
                            CreateNodes(path, node, true);
                            if (isExp)
                                node.Expand();
                        }));
                    }
                    break;
                case WatcherChangeTypes.Deleted:

                    node = FindNode(e.FullPath, treeView.Nodes);
                    if (node != null)
                    {
                        this.Invoke(new MethodInvoker(delegate ()
                        {
                            node.Remove();
                        }));
                    }
                    break;
            }
            if (path == pathNow)
            {
                this.Invoke(new MethodInvoker(delegate ()
                {
                    ShowDirsAndFiles(pathNow);
                }));
            }

        }

        private void BuildTreeView()
        {
            ;

            Text = "Обозреватель";
            if(textBoxPath.Text=="")
                textBoxPath.Text = "Мой Компьютер";
            TreeNode myComp = treeView.Nodes.Add(textBoxPath.Text, textBoxPath.Text, 3, 4);

            int i = 0;
            foreach (string diskName in Directory.GetLogicalDrives())
            {
                FileSystemWatcher watcher = new FileSystemWatcher(diskName);
                watcher.Changed += SelectedFolderWatcher_Changed;
                watcher.Created += SelectedFolderWatcher_Changed;
                watcher.Deleted += SelectedFolderWatcher_Changed;
                watcher.Renamed += SelectedFolderWatcher_Renamed;
                watcher.IncludeSubdirectories = true;

                int idx = diskName.LastIndexOf('\\');
                if (idx > diskName.Length)
                    myComp.Nodes.Add(diskName, diskName.Substring(idx), 1, 1);
                else
                    myComp.Nodes.Add(diskName, diskName, 1, 1);


                CreateNodes(diskName, myComp.Nodes[i++], true);

                watcher.Path = diskName;
                watcher.EnableRaisingEvents = true;

                DisksWatchers.Add(watcher);
            }
            myComp.Expand();
        }

        private void CreateNodes(string path, TreeNode tn, bool loadNextFolder = false)
        {
            string[] nameDirectorys;
            string[] archives;
            string name = "";
            int i = 0;
            bool justOneNode = false;
            if (loadNextFolder == false)
                justOneNode = true;
            try
            {
                nameDirectorys = Directory.GetDirectories(path);
                foreach (string fullname in nameDirectorys)
                {
                    name = fullname.Substring(fullname.LastIndexOf('\\') + 1);
                    if (tn.Nodes[fullname] == null)
                        tn.Nodes.Add(fullname, name, 3, 4);
                    if (loadNextFolder)
                    {
                        CreateNodes(fullname, tn.Nodes[i]);
                    }
                    i++;
                    if (justOneNode)
                        break;
                }


                archives = Directory.GetFiles(path);
                foreach (string nameArch in archives)
                {
                    name = nameArch.Substring(nameArch.LastIndexOf('\\') + 1);
                    if (tn.Nodes[nameArch] == null)
                    {
                        if (Path.GetExtension(nameArch) == ".rar" || Path.GetExtension(nameArch) == ".7z" || Path.GetExtension(nameArch) == ".zip")
                            tn.Nodes.Add(nameArch, name, 5, 5);
                    }
                    if (justOneNode)
                        break;
                }
            }
            catch (Exception ex)
            {
                //statusLabel.Text = ex.Message + $" Узел {name} не создан";
            }

        }

        private void treeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (IsFolder(e.Node))
                e.Node.SelectedImageIndex = e.Node.IsExpanded ? 4 : 3;
            try
            {
                int countElements = GetCountElements(e.Node.Name);
                statusLabel.Text = "Элементов: " + countElements;
            }
            catch (Exception ex)
            {
                statusLabel.Text = "Элементов: " + e.Node.Nodes.Count;
            }

            ShowDirsAndFiles(e.Node.Name);
        }


        private void treeView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {

        }

        private int GetCountElements(string path)
        {
            int count = 0;
            count += Directory.GetDirectories(path).Length;
            foreach (string nameFile in Directory.GetFiles(path))
            {
                if (Path.GetExtension(nameFile) == ".rar" || Path.GetExtension(nameFile) == ".7z" || Path.GetExtension(nameFile) == ".zip")
                    count++;
            }
            return count;
        }

        private void ShowDirsAndFiles(string path)
        {
            listView.Items.Clear();
            textBoxPath.Text = path;
            if (path == "Мой Компьютер")
                LoadFromTreeView(path);
            else
                LoadFromDirectory(path);
        }

        private void LoadFromTreeView(string path)
        {
            DriveColumns();
            foreach (DriveInfo driver in DriveInfo.GetDrives())
            {
                ListViewItem item = listView.Items.Add(driver.Name, driver.Name, 1);

                string type = "";
                switch (driver.DriveType)
                {
                    case DriveType.Unknown:
                        type = "Неизвестный тип диска";
                        break;
                    case DriveType.NoRootDirectory:
                        type = "Диск без корневого элемента";
                        break;
                    case DriveType.Removable:
                        type = "Съемный диск";
                        break;
                    case DriveType.Fixed:
                        type = "Жесткий диск";
                        break;
                    case DriveType.Network:
                        type = "Сетевой диск";
                        break;
                    case DriveType.CDRom:
                        type = "Оптическое дисковое устройство";
                        break;
                    case DriveType.Ram:
                        type = "Диск ОЗУ";
                        break;
                    default:
                        type = "Неизвестный тип диска";
                        break;
                }
                item.SubItems.Add(type);
                item.SubItems.Add(Math.Round((Double)driver.TotalSize / 1000000000, 2) + " Гб");
                item.SubItems.Add(Math.Round((Double)(driver.TotalSize - driver.TotalFreeSpace) / 1000000000, 2) + " Гб");
                item.SubItems.Add(Math.Round((Double)driver.TotalFreeSpace / 1000000000, 2) + " Гб");
            }
        }

        private void DriveColumns()
        {
            listView.Columns.Clear();
            listView.Columns.Add("name", "Имя");
            listView.Columns.Add("type", "Тип");
            listView.Columns.Add("size", "Общий размер");
            listView.Columns.Add("freeSpace", "Занято");
            listView.Columns.Add("freeSpace", "Свободно");
        }


        private void LoadFromDirectory(string path)
        {
            FolderColumns();
            string[] nameDirectorys;
            string[] nameFiles;
            string name = "";
            try
            {
                nameDirectorys = Directory.GetDirectories(path);
                foreach (string fullname in nameDirectorys)
                {
                    name = fullname.Substring(fullname.LastIndexOf('\\') + 1);
                    if (listView.Items[fullname] == null)
                        listView.Items.Add(fullname, name, 3);

                    listView.Items[fullname].SubItems.Add("Папка с файлами");
                    listView.Items[fullname].SubItems.Add("");
                    listView.Items[fullname].SubItems.Add(Directory.GetCreationTime(fullname).ToString());
                    listView.Items[fullname].SubItems.Add(Directory.GetLastAccessTime(fullname).ToString());
                    listView.Items[fullname].SubItems.Add(Directory.GetLastWriteTime(fullname).ToString());
                }


                nameFiles = Directory.GetFiles(path);
                foreach (string nameFile in nameFiles)
                {
                    name = nameFile.Substring(nameFile.LastIndexOf('\\') + 1);
                    if (listView.Items[nameFile] == null)
                    {
                        int idxIcon;
                        if (Path.GetExtension(nameFile) == ".exe")
                            idxIcon = 2;
                        else if (Path.GetExtension(nameFile) == ".rar" || Path.GetExtension(nameFile) == ".7z" || Path.GetExtension(nameFile) == ".zip")
                            idxIcon = 5;
                        else if (Path.GetExtension(nameFile) == ".txt" || Path.GetExtension(nameFile) == ".doc" || Path.GetExtension(nameFile) == ".docx")
                            idxIcon = 0;
                        else
                            idxIcon = 6;
                        listView.Items.Add(nameFile, name, idxIcon);
                        listView.Items[nameFile].SubItems.Add("Файл");
                        listView.Items[nameFile].SubItems.Add(new FileInfo(nameFile).Length / 1000 + " Кб");
                        listView.Items[nameFile].SubItems.Add(File.GetCreationTime(nameFile).ToString());
                        listView.Items[nameFile].SubItems.Add(File.GetLastAccessTime(nameFile).ToString());
                        listView.Items[nameFile].SubItems.Add(File.GetLastWriteTime(nameFile).ToString());
                    }
                }
            }
            catch (Exception)
            {
                //statusLabel.Text = ex.Message + $" Узел {name} не создан";
            }
        }

        private void FolderColumns()
        {
            listView.Columns.Clear();
            listView.Columns.Add("name", "Имя");
            listView.Columns.Add("type", "Тип");
            listView.Columns.Add("size", "Размер");
            listView.Columns.Add("dateCreate", "Дата создания");
            listView.Columns.Add("dateAccess", "Дата последнего доступа");
            listView.Columns.Add("dateWrite", "Дата последней записи");
        }

        private void toolStripComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            View v = (View)Enum.Parse(typeof(View), toolStripComboBox1.SelectedItem.ToString());
            listView.View = v;
        }

        private void listView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            listView.Columns[e.Column].Width = -1;
        }

        private void textBoxPath_TextChanged(object sender, EventArgs e)
        {

            if (textBoxPath.Text.IndexOf('\\') != -1)
            {
                buttonHigher.Enabled = true;
                if (isDrive(textBoxPath.Text))
                {
                    folderAbove = "Мой Компьютер";
                }
                else
                {
                    folderAbove = textBoxPath.Text.Substring(0, textBoxPath.Text.LastIndexOf('\\') + 1);
                    if (!isDrive(folderAbove))
                        folderAbove = folderAbove.Substring(0, folderAbove.Length - 1);
                }
            }
            else
                buttonHigher.Enabled = false;
            pathNow = textBoxPath.Text;
            Text = textBoxPath.Text;
        }

        private bool isDrive(string text)
        {
            foreach (string nameDisk in Directory.GetLogicalDrives())
            {
                if (text == nameDisk)
                    return true;
            }
            return false;
        }

        private void buttonHigher_Click(object sender, EventArgs e)
        {
            ShowDirsAndFiles(folderAbove);
        }

        private void listView_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView.SelectedItems.Count > 1)
            {
                statusLabel2.Text = "Выделено элементов: " + listView.SelectedItems.Count;
            }
            else if (listView.SelectedItems.Count == 1)
            {
                Thread t = new Thread(new ThreadStart(ShowSizeOnStatusBar));
                t.IsBackground = true;
                t.Start();
            }
            else
                statusLabel2.Text = "";

        }

        private void ShowSizeOnStatusBar()
        {
            statusLabel2.Text = "";
            try
            {
                ListViewItem selectedItem = listView.SelectedItems[0];
                statusLabel2.Text = "Размер: " + (GetSize(selectedItem) / 1000000) + " Мб";
            }
            catch (Exception) { }
        }

        private long GetSize(ListViewItem listViewItem)
        {
            long size = 0;
            if (Directory.Exists(listViewItem.Name))
                size = DirSize(new DirectoryInfo(listViewItem.Name));

            if (File.Exists(listViewItem.Name))
                size = new FileInfo(listViewItem.Name).Length;

            return size;
        }
        public static long DirSize(DirectoryInfo d)
        {
            long Size = 0;
            try
            {
                foreach (FileInfo fi in d.GetFiles())
                {
                    Size += fi.Length;
                }
                foreach (DirectoryInfo di in d.GetDirectories())
                {
                    Size += DirSize(di);
                }
            }
            catch (Exception) { }
            return (Size);
        }

        private void listView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if(listView.SelectedItems.Count>0)
            {
                if (Directory.Exists(listView.SelectedItems[0].Name))
                    ShowDirsAndFiles(listView.SelectedItems[0].Name);
                else
                    Process.Start(listView.SelectedItems[0].Name);
            }

        }

        private void OpenNewWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form1 newWindow = new Form1(textBoxPath.Text);
            newWindow.Show();
        }

        private void CloseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
