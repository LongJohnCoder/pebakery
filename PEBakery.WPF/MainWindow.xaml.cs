﻿/*
    Copyright (C) 2016-2017 Hajin Jang
    Licensed under GPL 3.0
 
    PEBakery is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using PEBakery.Helper;
using PEBakery.Lib;
using PEBakery.Core;
using MahApps.Metro.IconPacks;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using System.Globalization;
using System.Threading;
using System.Security.Cryptography;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace PEBakery.WPF
{
    #region MainWindow
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<Project> projectList;
        private string baseDir;
        private BackgroundWorker loadWorker = new BackgroundWorker();
        private BackgroundWorker refreshWorker = new BackgroundWorker();
        private double scaleFactor = 1;

        private TreeViewModel currentTree;
        private Logger logger;
        private PluginCache pluginCache;

        const int MaxDpiScale = 4;
        private int allPluginCount = 0;

        private TreeViewModel treeModel;
        public TreeViewModel TreeModel { get => treeModel; }

        public MainWindow()
        {
            InitializeComponent();

            string[] args = App.Args;
            if (int.TryParse(Properties.Resources.IntegerVersion, NumberStyles.Integer, CultureInfo.InvariantCulture, out App.Version) == false)
            {
                Console.WriteLine("Cannot determine version");
                Application.Current.Shutdown();
            }  

            // string argBaseDir = FileHelper.GetProgramAbsolutePath();
            string argBaseDir = Environment.CurrentDirectory;
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], "/basedir", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length)
                    {
                        argBaseDir = System.IO.Path.GetFullPath(args[i + 1]);
                        Environment.CurrentDirectory = argBaseDir;
                    }
                    else
                    {
                        Console.WriteLine("\'/basedir\' must be used with path\r\n");
                    }
                }
                else if (string.Equals(args[i], "/?", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(args[i], "/help", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(args[i], "/h", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Sorry, help message not implemented\r\n");
                }
            }

            this.projectList = new List<Project>();

            this.baseDir = argBaseDir;
            this.treeModel = new TreeViewModel(null);
            this.DataContext = treeModel;

            LoadButtonsImage();

            string logDBFile = System.IO.Path.Combine(baseDir, "PEBakeryLog.db");
            try
            {
                File.Delete(logDBFile); // Temp measure - needed to test DB Log
            }
            catch (IOException) { }
            this.logger = new Logger(logDBFile);

            string cacheFile = System.IO.Path.Combine(baseDir, "PEBakeryCache.db");
            this.pluginCache = new PluginCache(cacheFile);

            StartLoadWorker();
        }

        void LoadButtonsImage()
        {
            // Properties.Resources.
            BuildButton.Content = ImageHelper.GetMaterialIcon(PackIconMaterialKind.Wrench, 5);
            RefreshButton.Content = ImageHelper.GetMaterialIcon(PackIconMaterialKind.Refresh, 5);
            SettingButton.Content = ImageHelper.GetMaterialIcon(PackIconMaterialKind.Settings, 5);
            UpdateButton.Content = ImageHelper.GetMaterialIcon(PackIconMaterialKind.Download, 5);
            AboutButton.Content = ImageHelper.GetMaterialIcon(PackIconMaterialKind.Help, 5);

            PluginRunButton.Content = ImageHelper.GetMaterialIcon(PackIconMaterialKind.Wrench, 5);
            PluginEditButton.Content = ImageHelper.GetMaterialIcon(PackIconMaterialKind.BorderColor, 5);
            PluginRefreshButton.Content = ImageHelper.GetMaterialIcon(PackIconMaterialKind.Refresh, 5);
        }

        private void StartLoadWorker()
        {
            Stopwatch watch = new Stopwatch();

            Image image = new Image()
            {
                UseLayoutRounding = true,
                Stretch = Stretch.Uniform,
                StretchDirection = StretchDirection.DownOnly,
                Source = ImageHelper.ToBitmapImage(Properties.Resources.DonutPng),
            };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);

            PluginLogo.Content = image;
            PluginTitle.Text = "Welcome to PEBakery!";
            PluginDescription.Text = "PEBakery loading...";

            allPluginCount = 0;
            int loadedPluginCount = 0;
            int cachedPluginCount = 0;

            MainProgressRing.IsActive = true;
            LoadProgressBar.Visibility = Visibility.Visible;
            StatusBar.Visibility = Visibility.Collapsed;
            loadWorker = new BackgroundWorker();
            loadWorker.DoWork += (object sender, DoWorkEventArgs e) =>
            {
                string baseDir = (string)e.Argument;
                BackgroundWorker worker = sender as BackgroundWorker;

                watch = Stopwatch.StartNew();
                this.projectList = new List<Project>();

                string[] projArray = Directory.GetDirectories(System.IO.Path.Combine(baseDir, "Projects"));
                List<string> projList = new List<string>();
                foreach (string dir in projArray)
                {
                    if (File.Exists(System.IO.Path.Combine(baseDir, "Projects", dir, "script.project")))
                    {
                        projList.Add(dir);
                        allPluginCount += Project.GetPluginCount(System.IO.Path.Combine(baseDir, "Projects", dir));
                    }
                }

                Dispatcher.Invoke(() =>
                {
                    LoadProgressBar.Maximum = allPluginCount;
                });

                foreach (string dir in projList)
                {
                    Project project = new Project(baseDir, System.IO.Path.GetFileName(dir), pluginCache, worker);
                    project.Load();
                    projectList.Add(project);
                }

                Dispatcher.Invoke(() =>
                {
                    foreach (Project project in this.projectList)
                    {
                        List<Node<Plugin>> plugins = project.VisiblePlugins.Root;
                        RecursivePopulateMainTreeView(plugins, this.treeModel);
                    };
                    MainTreeView.DataContext = treeModel;
                    currentTree = treeModel.Child[0];
                    DrawPlugin(projectList[0].MainPlugin);
                });
            };
            loadWorker.WorkerReportsProgress = true;
            loadWorker.ProgressChanged += (object sender, ProgressChangedEventArgs e) =>
            {
                Interlocked.Increment(ref loadedPluginCount);
                LoadProgressBar.Value = loadedPluginCount;
                if (e.ProgressPercentage == 1)
                { // Cached
                    Interlocked.Increment(ref cachedPluginCount);
                    if (e.UserState == null)
                        PluginDescription.Text = $"PEBakery loading...{Environment.NewLine}({loadedPluginCount} / {allPluginCount}) Cached - Error";
                    else
                        PluginDescription.Text = $"PEBakery loading...{Environment.NewLine}({loadedPluginCount} / {allPluginCount}) Cached - {e.UserState}";
                }
                else
                {
                    if (e.UserState == null)
                        PluginDescription.Text = $"PEBakery loading...{Environment.NewLine}({loadedPluginCount} / {allPluginCount}) Error";
                    else
                        PluginDescription.Text = $"PEBakery loading...{Environment.NewLine}({loadedPluginCount} / {allPluginCount}) {e.UserState}";
                }
            };
            loadWorker.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) =>
            {
                watch.Stop();
                TimeSpan t = watch.Elapsed;
                this.StatusBar.Text = $"{allPluginCount} plugins loaded ({cachedPluginCount} cached), took {t:hh\\:mm\\:ss}";
                LoadProgressBar.Visibility = Visibility.Collapsed;
                StatusBar.Visibility = Visibility.Visible;

                MainProgressRing.IsActive = false;

                DispatcherTimer Timer = new DispatcherTimer();
                Timer.Interval = TimeSpan.FromSeconds(5);
                Timer.Tick += (object tickSender, EventArgs tickArgs) =>
                {
                    StartCacheWorker();
                    (tickSender as DispatcherTimer).Stop();
                };
                Timer.Start();
            };
            loadWorker.RunWorkerAsync(baseDir);
        }

        private void StartCacheWorker()
        {
            Stopwatch watch = new Stopwatch();
            BackgroundWorker cacheWorker = new BackgroundWorker();

            MainProgressRing.IsActive = true;
            int cachedPluginCount = 0;
            cacheWorker.DoWork += (object sender, DoWorkEventArgs e) =>
            {
                BackgroundWorker worker = sender as BackgroundWorker;

                watch = Stopwatch.StartNew();
                foreach (Project project in projectList)
                {
                    var tasks = project.AllPluginList.Select(p =>
                    {
                        return Task.Run(() =>
                        {
                            if (p.Type == PluginType.Link)
                            {
                                worker.ReportProgress(0);
                                return;
                            }

                            // Is cache exist?
                            DateTime lastWriteTime = File.GetLastWriteTimeUtc(p.DirectFullPath);
                            string sPath = p.FullPath.Remove(0, baseDir.Length + 1);
                            DB_PluginCache pCache = pluginCache.Table<DB_PluginCache>()
                                .FirstOrDefault(x => sPath.Equals(x.Path, StringComparison.OrdinalIgnoreCase));
                            if (pCache == null) // Cache not exists
                            {
                                pCache = new DB_PluginCache()
                                {
                                    Path = sPath,
                                    LastWriteTime = lastWriteTime,
                                };

                                BinaryFormatter formatter = new BinaryFormatter();
                                using (MemoryStream mem = new MemoryStream())
                                {
                                    formatter.Serialize(mem, p);
                                    pCache.Serialized = mem.ToArray();
                                }

                                pluginCache.Insert(pCache);
                            }
                            else if (DateTime.Equals(pCache.LastWriteTime, lastWriteTime) == false) // Cache is outdated
                            {
                                pCache.LastWriteTime = lastWriteTime;
                                BinaryFormatter formatter = new BinaryFormatter();
                                using (MemoryStream mem = new MemoryStream())
                                {
                                    formatter.Serialize(mem, p);
                                    pCache.Serialized = mem.ToArray();
                                }

                                pluginCache.Update(pCache);
                            }

                            worker.ReportProgress(0);
                        });
                    }).ToArray();
                    Task.WaitAll(tasks);
                }
            };

            cacheWorker.WorkerReportsProgress = true;
            cacheWorker.ProgressChanged += (object sender, ProgressChangedEventArgs e) =>
            {
                Interlocked.Increment(ref cachedPluginCount);
                StatusBar.Text = $"Updating cache... ({cachedPluginCount}/{allPluginCount})";
            };
            cacheWorker.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) =>
            {
                watch.Stop();
                TimeSpan t = watch.Elapsed;
                StatusBar.Text = $"{allPluginCount} plugins cached, took {t:hh\\:mm\\:ss}";

                MainProgressRing.IsActive = false;
            };
            cacheWorker.RunWorkerAsync();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (loadWorker.IsBusy == false)
            {
                (MainTreeView.DataContext as TreeViewModel).Child.Clear();
                LoadProgressBar.Value = 0;
                LoadProgressBar.Visibility = Visibility.Visible;
                StatusBar.Visibility = Visibility.Collapsed;

                StartLoadWorker();
            }
        }

        private void RecursivePopulateMainTreeView(List<Node<Plugin>> plugins, TreeViewModel treeParent)
        {
            foreach (Node<Plugin> node in plugins)
            {
                Plugin p = node.Data;

                TreeViewModel item = new TreeViewModel(treeParent);
                treeParent.Child.Add(item);
                item.Node = node;

                if (p.Type == PluginType.Directory)
                {
                    item.SetIcon(ImageHelper.GetMaterialIcon(PackIconMaterialKind.Folder, 0));
                }
                else if (p.Type == PluginType.Plugin)
                {
                    if (p.Level == Project.MainLevel)
                        item.SetIcon(ImageHelper.GetMaterialIcon(PackIconMaterialKind.Settings, 0));
                    else if (p.Mandatory)
                        item.SetIcon(ImageHelper.GetMaterialIcon(PackIconMaterialKind.LockOutline, 0));
                    else
                        item.SetIcon(ImageHelper.GetMaterialIcon(PackIconMaterialKind.File, 0));
                }
                else if (p.Type == PluginType.Link)
                {
                    item.SetIcon(ImageHelper.GetMaterialIcon(PackIconMaterialKind.OpenInNew, 0));
                }

                if (0 < node.Child.Count)
                    RecursivePopulateMainTreeView(node.Child, item);
            }
        }

        private void MainTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var tree = sender as TreeView;

            if (tree.SelectedItem is TreeViewModel)
            {
                TreeViewModel item = currentTree = tree.SelectedItem as TreeViewModel;

                Dispatcher.Invoke(() =>
                {
                    Stopwatch watch = new Stopwatch();
                    watch.Start();
                    DrawPlugin(item.Node.Data);
                    watch.Stop();
                    double sec = watch.Elapsed.TotalSeconds;
                    StatusBar.Text = $"{currentTree.Node.Data.ShortPath} rendered. Took {sec:0.000}sec";
                });
            }
            else
            {
                Debug.Assert(false);
            }
        }

        private void DrawPlugin(Plugin p)
        {
            Stopwatch watch = new Stopwatch();
            double size = PluginLogo.ActualWidth * MaxDpiScale;
            if (p.Type == PluginType.Directory)
                PluginLogo.Content = ImageHelper.GetMaterialIcon(PackIconMaterialKind.Folder, 0);
            else
            {
                try
                {
                    MemoryStream mem = EncodedFile.ExtractLogo(p, out ImageType type);
                    if (type == ImageType.Svg)
                    {
                        Image image = new Image()
                        {
                            Source = ImageHelper.SvgToBitmapImage(mem, size, size),
                            Stretch = Stretch.Uniform
                        };
                        PluginLogo.Content = image;
                    }
                    else
                    {
                        Image image = new Image();
                        BitmapImage bitmap = ImageHelper.ImageToBitmapImage(mem);
                        image.StretchDirection = StretchDirection.DownOnly;
                        image.Stretch = Stretch.Uniform;
                        image.UseLayoutRounding = true; // Must to prevent blurry image rendering
                        image.Source = bitmap;

                        Grid grid = new Grid();
                        grid.Children.Add(image);

                        PluginLogo.Content = grid;
                    }
                }
                catch
                { // No logo file - use default
                    if (p.Type == PluginType.Plugin)
                        PluginLogo.Content = ImageHelper.GetMaterialIcon(PackIconMaterialKind.FileDocument, 0);
                    else if (p.Type == PluginType.Link)
                        PluginLogo.Content = ImageHelper.GetMaterialIcon(PackIconMaterialKind.OpenInNew, 0);
                }
            }
            PluginTitle.Text = StringEscaper.Unescape(p.Title);
            PluginDescription.Text = StringEscaper.Unescape(p.Description);
            PluginVersion.Text = $"v{p.Version}";
            PluginAuthor.Text = p.Author;

            MainCanvas.Children.Clear();
            if (p.Type != PluginType.Directory)
            {
                ScaleTransform scale = new ScaleTransform(scaleFactor, scaleFactor);
                UIRenderer render = new UIRenderer(MainCanvas, this, p, logger, scaleFactor);
                MainCanvas.LayoutTransform = scale;
                render.Render();
            }

        }

        private void PluginRefreshButton_Click(object sender, RoutedEventArgs e)
        {
            StartRefreshWorker();
        }

        private void StartRefreshWorker()
        {
            if (currentTree == null)
                return;

            if (refreshWorker.IsBusy)
                return;

            Stopwatch watch = new Stopwatch();

            this.MainProgressRing.IsActive = true;
            refreshWorker = new BackgroundWorker();
            refreshWorker.DoWork += (object sender, DoWorkEventArgs e) =>
            {
                watch.Start();
                Plugin p = currentTree.Node.Data.Project.RefreshPlugin(currentTree.Node.Data);
                if (p != null)
                {
                    currentTree.Node.Data = p;
                    Dispatcher.Invoke(() => 
                    {
                        currentTree.Node.Data = p;
                        DrawPlugin(currentTree.Node.Data);
                    });
                }
            };
            refreshWorker.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) =>
            {
                MainProgressRing.IsActive = false;
                watch.Stop();
                double sec = watch.Elapsed.TotalSeconds;
                StatusBar.Text = $"{currentTree.Node.Data.ShortPath} reloaded. Took {sec:0.000}sec";
            };
            refreshWorker.RunWorkerAsync();
        }

        private void MainTreeView_Loaded(object sender, RoutedEventArgs e)
        {
            Window window = Window.GetWindow(this);
            window.KeyDown += MainTreeView_KeyDown;
        }

        /// <summary>
        /// Used to ensure pressing 'Space' to toggle TreeView's checkbox.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainTreeView_KeyDown(object sender, KeyEventArgs e)
        {
            // Window window = sender as Window;
            base.OnKeyDown(e);

            if (e.Key == Key.Space)
            {
                if (Keyboard.FocusedElement is FrameworkElement focusedElement)
                {
                    if (focusedElement.DataContext is TreeViewModel node)
                    {
                        if (node.Checked == true)
                            node.Checked = false;
                        else if (node.Checked == false)
                            node.Checked = true;
                        e.Handled = true;
                    }
                }
            }
        }

        private void PluginRunButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void BuildButton_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void SettingButton_Click(object sender, RoutedEventArgs e)
        {
            SettingViewModel settingViewModel = new SettingViewModel(scaleFactor * 100);
            SettingWindow dialog = new SettingWindow(settingViewModel);
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                scaleFactor = settingViewModel.ScaleFactor / 100;
                StartRefreshWorker();
            }
        }

        private void PluginEditButton_Click(object sender, RoutedEventArgs e)
        {
            ProcessStartInfo procInfo = new ProcessStartInfo()
            {
                Verb = "open",
                FileName = currentTree.Node.Data.FullPath,
                UseShellExecute = true
            };
            Process.Start(procInfo);
        }
    }
    #endregion

    #region TreeViewModel
    public class TreeViewModel : INotifyPropertyChanged
    {
        public TreeViewModel(TreeViewModel parent)
        {
            this.parent = parent;
        }

        public bool Checked
        {
            get
            {
                switch (node.Data.Selected)
                {
                    case SelectedState.True:
                        return true;
                    default:
                        return false;
                }
            }
            set
            {
                if (node.Data.Mandatory == false && node.Data.Selected != SelectedState.None)
                {
                    if (value)
                        node.Data.Selected = SelectedState.True;
                    else
                        node.Data.Selected = SelectedState.False;

                    if (0 < this.Child.Count)
                    { // Set child plugins, too -> Top-down propagation
                        foreach (TreeViewModel childModel in this.Child)
                        {
                            if (value)
                                childModel.Checked = true;
                            else
                                childModel.Checked = false;
                        }
                    }

                    ParentCheckedPropagation();
                    OnPropertyUpdate("Checked");
                }
            }
        }

        public void ParentCheckedPropagation()
        { // Bottom-up propagation of Checked property
            if (parent == null)
                return;

            bool setParentChecked = false;

            foreach (TreeViewModel sibling in parent.Child)
            { // Siblings
                if (sibling.Checked)
                    setParentChecked = true;
            }

            parent.SetParentChecked(setParentChecked);
        }

        public void SetParentChecked(bool value)
        {
            if (parent == null)
                return;

            if (node.Data.Mandatory == false && node.Data.Selected != SelectedState.None)
            {
                if (value)
                    node.Data.Selected = SelectedState.True;
                else
                    node.Data.Selected = SelectedState.False;
            }

            OnPropertyUpdate("Checked");
            ParentCheckedPropagation();
        }

        public Visibility CheckBoxVisible
        {
            get
            {
                if (node.Data.Selected == SelectedState.None)
                    return Visibility.Hidden;
                else
                    return Visibility.Visible;
            }
        }

        public string Text { get => node.Data.Title; }

        private Node<Plugin> node;
        public Node<Plugin> Node
        {
            get => node;
            set
            {
                node = value;
                OnPropertyUpdate("Node");
            }
        }

        private Control icon;
        public Control Icon
        {
            get => icon;
            set
            {
                icon = value;
                OnPropertyUpdate("Icon");
            }
        }

        private TreeViewModel parent;
        public TreeViewModel Parent { get => parent; }

        private ObservableCollection<TreeViewModel> child = new ObservableCollection<TreeViewModel>();
        public ObservableCollection<TreeViewModel> Child { get => child; }

        public void SetIcon(Control icon)
        {
            this.icon = icon;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyUpdate(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    #endregion

}
