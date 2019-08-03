using desktop.Controls.ToggleSwitch;
using desktop.Interfaces;
using Roslyn;
using sunamo.Clipboard;
using sunamo.Constants;
using sunamo.Essential;
using sunamo.Interfaces;
using SunamoCode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using UnManaged;

namespace AllProjectsSearch.UC
{
    /// <summary>
    /// Interaction logic for InsertIntoXlfAndConstantCsUC.xaml
    /// </summary>
    public partial class InsertIntoXlfAndConstantCsUC : UserControl, IUserControl, IKeysHandler<KeyEventArgs>, IUserControlWithSettingsManager, IUserControlWithMenuItemsList, IWindowOpener, IUserControlWithSizeChange
    {
        #region Class data
        static InsertIntoXlfAndConstantCsUC instance = null;
        public static InsertIntoXlfAndConstantCsUC Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new InsertIntoXlfAndConstantCsUC();
                    instance.Init();
                }
                return instance;
            }
        }
        static Type type = typeof(InsertIntoXlfAndConstantCsUC);
        public string Title => "Insert into xlf and constant *.cs";
        HorizontalToggleSwitch monitorClipboardHelper;
        ClipboardMonitor clipboardMonitor = null;
        InsertIntoXlfAndConstantCsUCMenuItems insertIntoXlfAndConstantCsUCMenuItems = InsertIntoXlfAndConstantCsUCMenuItems.Instance;
        HotKey declineHotkey = null;
        bool initialized = false;
        public ApplicationDataContainer data => MainWindow.Instance.Data;
        const string sunamoOftenCorruptedWhileGetAllStringsSln = @"d:\Documents\Visual Studio 2017\Projects\sunamoOftenCorruptedWhileGetAllStrings\";
        public WindowWithUserControl windowWithUserControl { get => MainWindow.Instance.windowWithUserControl; set => MainWindow.Instance.windowWithUserControl = value; }
        #endregion

        #region XlfEngine data
        XlfEngine xlfEngine = XlfEngine.Instance;

        #endregion

        #region Init
        private InsertIntoXlfAndConstantCsUC()
        {
            InitializeComponent();

            //insertIntoXlfAndConstantCsUCMenuItems.MiSplitAllStringsToTranslateAble_Click(null, null);
            //MainWindow.Instance.SetCancelClosing( false);
            //MainWindow.Instance.Close();

            instance = this;

            Loaded += InsertIntoXlfAndConstantCsUC_Loaded;
            SizeChanged += InsertIntoXlfAndConstantCsUC_SizeChanged;
        }

        public void Init()
        {
            if (!initialized)
            {
                chblFilesOftenCorruptedDuringTranslating.Init(null);


                data.Add(chblFilesOftenCorruptedDuringTranslating);

                initialized = true;

                AllProjectsSearchHelper.AuthGoogleTranslate();

                #region Tool for monitoring clipboard
                clipboardMonitor = ClipboardMonitor.Instance;

                monitorClipboardWithLabel.monitorClipboard.IsChecked = false;
                monitorClipboardHelper = monitorClipboardWithLabel.monitorClipboard;
                monitorClipboardHelper.Checked += monitorClipboard_Checked;
                monitorClipboardHelper.Unchecked += monitorClipboard_Unchecked;

                clipboardMonitor.ClipboardContentChanged += ClipboardMonitor_OnClipboardContentChanged;
                monitorClipboardHelper.IsChecked = false;
                #endregion

                xlfEngine.acceptHotkey = new HotKey(Key.Enter, KeyModifier.Ctrl, xlfEngine.Accept);
                declineHotkey = new HotKey(Key.Delete, KeyModifier.Ctrl, Decline);
                xlfEngine.InitializeMultilingualResources();
                chblFilesOftenCorruptedDuringTranslating.DefaultButtonsInit();

                XlfEngine.Instance.InitializeMultilingualResources();
            }
        }
        #endregion

        #region InsertIntoXlfAndConstantCsUC handlers
        private void InsertIntoXlfAndConstantCsUC_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //OnSizeChanged(new DesktopSize(e));
        }

        private void InsertIntoXlfAndConstantCsUC_Loaded(object sender, RoutedEventArgs e)
        {
            xlfEngine.txtText = txtText;
            xlfEngine.txtEnglishTranslate = txtEnglishTranslate;
            xlfEngine.rbEn = rbEn;
            xlfEngine.rbCs = rbCs;


            //insertIntoXlfAndConstantCsUCMenuItems.MiTranlateAllTranslateAbleStringsInFile_Click(null, null);
        } 
        #endregion

        #region monitorClipboard handlers
        /// <summary>
        /// Enable clipboard monitoring
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void monitorClipboard_Checked(object sender, RoutedEventArgs e)
        {
            ClipboardMonitor.Instance.pernamentlyBlock = false;
            xlfEngine.requireUserDecision = true;
            xlfEngine.acceptHotkey.Tag = null;
        }

        /// <summary>
        /// Disable clipboard monitoring
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void monitorClipboard_Unchecked(object sender, RoutedEventArgs e)
        {
            ClipboardMonitor.Instance.pernamentlyBlock = true;
            xlfEngine.requireUserDecision = false;
        }
        #endregion

        #region Other handlers
        void Decline(HotKey h)
        {
            if (xlfEngine.waitingForUserDecision)
            {

                xlfEngine.waitingForUserDecision = false;
                ThisApp.SetStatus(TypeOfMessage.Success, txtEnglishTranslate.Text + " declined");
                xlfEngine.ClearTextBoxes(true, true);
            }
            else
            {
#if DEBUG
                ThisApp.SetStatus(TypeOfMessage.Information, "wasn't declined");
#endif
            }
        }


        /// <summary>
        /// Insert text into txtInput
        /// </summary>
        private void ClipboardMonitor_OnClipboardContentChanged()
        {
            if (monitorClipboardHelper.IsChecked)
            {
                if (xlfEngine.waitingForUserDecision)
                {
                    ThisApp.SetStatus(TypeOfMessage.Error, "Actually waiting for user decisiion");
                }
                else
                {
                    string text = ClipboardHelper.GetText();
                    text = text.Trim(AllChars.qm);
                    text = text.TrimEnd(AllChars.colon);

                    if (xlfEngine.IsAlreadyContainedInXlfKeys(text, false))
                    {
                        return;
                    }

                    xlfEngine.Add(true, text);
                }
            }
        }

        private void RbEn_Checked(object sender, RoutedEventArgs e)
        {
            xlfEngine.l = Langs.en;
        }

        private void RbCs_Checked(object sender, RoutedEventArgs e)
        {
            xlfEngine.l = Langs.cs;
        }
        #endregion

        #region Interfaces implements
        public bool HandleKey(KeyEventArgs e)
        {
            return false;
        }

        public List<MenuItem> MenuItems()
        {
            var menuItems = insertIntoXlfAndConstantCsUCMenuItems.MenuItems();

            MenuItem miSetWaitingForUserDecision = MenuItemHelper.CreateNew("Set waiting for user decision", delegate { xlfEngine.waitingForUserDecision = true; });
            menuItems.Add(miSetWaitingForUserDecision);

            ((Panel)miOftenCorruptedWhileGetAllStrings.Parent).Children.Remove(miOftenCorruptedWhileGetAllStrings);
            menuItems.Add(miOftenCorruptedWhileGetAllStrings);

            return menuItems;
        }

        /// <summary>
        /// Must be in this way and called from MainWindow
        /// </summary>
        /// <param name="maxWidth"></param>
        /// <param name="maxHeight"></param>
        public void OnSizeChanged(DesktopSize maxSize)
        {
            chblFilesOftenCorruptedDuringTranslating.OnSizeChanged(new DesktopSize(this.ActualWidth, maxSize.Height - r0.ActualHeight - r1.ActualHeight - r2.ActualHeight));
        } 
        #endregion

        #region MenuItem handlers
        private void MiBackupOftenCorruptedWhileGetAllStrings_Click(object sender, RoutedEventArgs e)
        {
            Backup(DefaultPaths.sunamo, sunamoOftenCorruptedWhileGetAllStringsSln);
        }

        private void MiRestoreOftenCorruptedWhileGetAllStrings_Click(object sender, RoutedEventArgs e)
        {
            Backup(sunamoOftenCorruptedWhileGetAllStringsSln, DefaultPaths.sunamo);
        }
        #endregion

        #region Methods
        void Backup(string from, string to)
        {
            var ds = CA.ToListString(chblFilesOftenCorruptedDuringTranslating.CheckedContent());

            foreach (var item in ds)
            {
                //chblFilesOftenCorruptedDuringTranslating.AddCheckbox(item);

                var files = FS.GetFiles(from, item + ".cs", System.IO.SearchOption.AllDirectories);
                foreach (var item2 in files)
                {
                    var newPath = item2.Replace(from, to);
                    FS.CreateUpfoldersPsysicallyUnlessThere(newPath);
                    FS.CopyFile(item2, newPath);
                }
            }


        } 
        #endregion
    }
}

