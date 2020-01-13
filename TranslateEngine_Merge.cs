using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using sunamo.Constants;
using sunamo.Essential;
using SunamoCode;
using UnManaged;
using desktop.Controls.ToggleSwitch;
using desktop.Interfaces;
using Roslyn;
using sunamo.Clipboard;
using sunamo.Interfaces;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AllProjectsSearch;
using AllProjectsSearch.UC;
using sunamo;
using sunamo.Values;
using System.Diagnostics;
using System.IO;
using XliffParser;
using System.Xml.Linq;
using System.Xml;
/// <summary>
/// Manage multilanguage strings in *.xlf files 
/// Specific methods for working with Xlf from InsertIntoXlfAndConstantCsUC
/// </summary>
public class XlfEngine
{
    #region Variables
    public Dictionary<Langs, string> filesWithTranslation = new Dictionary<Langs, string>();
    public Langs l = Langs.cs;
    public bool requireUserDecision = false;
    /// <summary>
    /// Path to sunamo project (not solution)
    /// </summary>
    readonly string basePathXlf = null;
    /// <summary>
    /// XlfKeys.cs
    /// </summary>
    public readonly string pathXlfKeys = null;
    public static XlfEngine Instance = new XlfEngine();
    public const string CopyWhileMassAddingNameFolder = "CopyWhileMassAdding";
    public bool waitingForUserDecision = false;
    /// <summary>
    /// Must be global because HotKey has delegate for handling method
    /// </summary>
    public string pascal = null;
    #endregion
    #region Instances which must to be initialized in InsertIntoXlfAndConstantCsUC
    public TextBox txtText = new TextBox();
    public TextBox txtEnglishTranslate = new TextBox();
    public RadioButton rbEn = new RadioButton();
    public RadioButton rbCs = new RadioButton();
    public HotKey acceptHotkey = null;
    #endregion
    #region Init
    private XlfEngine()
    {
        acceptHotkey = new HotKey(Key.Enter, KeyModifier.Ctrl | KeyModifier.Alt | KeyModifier.Shift | KeyModifier.Win, HotKey.DummyMethod);
        pathXlfKeys = FS.Combine(DefaultPaths.sunamo, @"sunamo\Constants\XlfKeys.cs");
        basePathXlf = FS.Combine(DefaultPaths.sunamo, "sunamo");
    }
    /// <summary>
    /// Externally called from many places
    /// </summary>
    public void InitializeMultilingualResources()
    {
        #region Load strings from MultilingualResources file
        var path = FS.Combine(basePathXlf, "MultilingualResources\\");
        foreach (var item in FS.GetFiles(path, "*.xlf", System.IO.SearchOption.TopDirectoryOnly))
        {
            Langs l2 = XmlLocalisationInterchangeFileFormat.GetLangFromFilename(item);
            if (!filesWithTranslation.ContainsKey(l2))
            {
                filesWithTranslation.Add(l2, item);
            }
        }
        #endregion
    }
    #endregion
    #region Add
    /// <summary>
    /// Recognize language and set to txt 
    /// Translate if is needed and put into *.xlf
    /// </summary>
    /// <param name="requireUserDecision"></param>
    /// <param name="text"></param>
    /// <param name="key2"></param>
    public void Add(bool requireUserDecision, string text, string key2 = null)
    {
        #region Recognize language and set to txt 
        // If A2 is path, A1 is text
        if (FS.ExistsFile(text))
        {
            return;
        }
        this.requireUserDecision = requireUserDecision;
        acceptHotkey.Tag = key2;
        if (TextLang.IsCzech(text))
        {
            l = Langs.cs;
            //rbCs.IsChecked = true;
        }
        else
        {
            // not to lb but directly to l. manybe in UC will raise event handler to set l, but in XlfEngine not. 
            l = Langs.en;
            //rbEn.IsChecked = true;
        }
        ClearTextBoxes(false, true);
        txtText.Text = text;
        #endregion
        #region Process text
        if (l == Langs.en)
        {
            // Insert as content of <target>
            // Will use only english so czech don't translate now
            Add();
        }
        else
        {
            string englishTranslate = null;
            englishTranslate = TranslateHelper.Instance.Translate(text, "en", "cs");
            if (char.IsUpper(text[0]))
            {
                englishTranslate = SH.FirstCharUpper(englishTranslate);
            }
            txtEnglishTranslate.Text = englishTranslate;
            ThisApp.SetStatus(TypeOfMessage.Error, "Press enter to add or delete to exit");
            if (requireUserDecision)
            {
                waitingForUserDecision = true;
            }
            else
            {
                Accept(acceptHotkey);
            }
        }
        #endregion
    }
    /// <summary>
    /// Insert into xlf file
    /// </summary>
    /// <param name="txtEnglishTranslate"></param>
    private void Add()
    {
        if (acceptHotkey.Tag == null)
        {
            pascal = ConvertPascalConvention.ToConvention(string.IsNullOrWhiteSpace(txtEnglishTranslate.Text) ? txtText.Text : txtEnglishTranslate.Text);
        }
        else
        {
            pascal = acceptHotkey.Tag.ToString();
        }
        // A2 insertToClipboard
        if (IsAlreadyContainedInXlfKeys(pascal, false))
        {
            return;
        }
        if (!string.IsNullOrWhiteSpace(pascal))
        {
            var fromL = GetFrom();
            var toL = GetTo();
            if (fromL == Langs.cs)
            {
                // Write czech
                XmlLocalisationInterchangeFileFormat.Append(fromL, string.Empty, txtText.Text, pascal, filesWithTranslation[fromL]);
                XmlLocalisationInterchangeFileFormat.Append(toL, string.Empty, txtEnglishTranslate.Text, pascal, filesWithTranslation[toL]);
            }
            else
            {
                XmlLocalisationInterchangeFileFormat.Append(fromL, string.Empty, txtText.Text, pascal, filesWithTranslation[fromL]);
            }
            AddConsts(CA.ToListString(pascal));
        }
    }
    #endregion
    #region Other handlers
    /// <summary>
    /// Externally called as handler
    /// </summary>
    /// <param name="h"></param>
    public void Accept(HotKey h)
    {
        var b1 = waitingForUserDecision;
        var b2 = (!waitingForUserDecision && !requireUserDecision);
        if (b1 || b2)
        {
            Add();
            // cant be, then will create pascal
            //if (h.Tag != null)
            //{
            //    h.Tag = null;
            //}
            waitingForUserDecision = false;
            ThisApp.SetStatus(TypeOfMessage.Success, txtEnglishTranslate.Text + " accepted");
            ClearTextBoxes(true, true);
        }
        else
        {
        #if DEBUG
            string postfix = string.Empty;
            if (!waitingForUserDecision)
            {
                postfix = ", wasn't waited for user desision. Press button";
            }
            ThisApp.SetStatus(TypeOfMessage.Information, "wasn't accepted" + postfix);
            #endif
        }
    }
    #endregion
    #region Work with consts in XlfKeys
    /// <summary>
    /// Add to XlfKeys.cs from xlf
    /// Must manually call XlfResourcesH.SaveResouresToRL(DefaultPaths.sunamoProject) before
    /// called externally from MiAddTranslationWhichIsntInKeys_Click
    /// </summary>
    /// <param name="keysAll"></param>
    public void AddConsts(List<string> keysAll)
    {
        int first = -1;
        List<string> lines = null;
        var keys = GetConsts(out first, out lines);
        var both = CA.CompareList(keys, keysAll);
        CSharpGenerator csg = new CSharpGenerator();
        foreach (var item in keysAll)
        {
            AddConst(csg, item);
        }
        lines.Insert(first, csg.ToString());
        TF.SaveLines(lines, pathXlfKeys);
    }
    /// <summary>
    /// Add c# const code
    /// </summary>
    /// <param name="csg"></param>
    /// <param name="item"></param>
    private static void AddConst(CSharpGenerator csg, string item)
    {
        csg.Field(1, AccessModifiers.Public, true, VariableModifiers.Mapped, "string", item, true, item);
    }
    /// <summary>
    /// Get consts which exists in XlfKeys.cs
    /// </summary>
    /// <param name="first"></param>
    /// <returns></returns>
    List<string> GetConsts(out int first)
    {
        List<string> lines = null;
        return GetConsts(out first, out lines);
    }
    /// <summary>
    /// Get consts which exists in XlfKeys.cs
    /// </summary>
    /// <param name="first"></param>
    /// <param name="lines"></param>
    /// <returns></returns>
    List<string> GetConsts(out int first, out List<string> lines)
    {
        first = -1;
        lines = TF.ReadAllLines(pathXlfKeys);
        var keys = CSharpParser.ParseConsts(lines, out first);
        return keys;
    }
    #endregion
    #region Methods
    /// <summary>
    /// return code for getting from RLData.en
    /// </summary>
    /// <param name="key2"></param>
    /// <returns></returns>
    public string TextFromRLData(string key2)
    {
        return "RLData.en[XlfKeys." + key2 + "]";
    }
    public void ClearTextBoxes(bool textText, bool textTranslate)
    {
        if (textText)
        {
            txtText.Text = string.Empty;
        }
        if (textTranslate)
        {
            txtEnglishTranslate.Text = string.Empty;
        }
    }
    /// <summary>
    /// Return whether A1 is in XlfKeys
    /// if A2, save A1 to clipboard
    /// Externally called from InsertIntoXlfAndConstantCsUC.ClipboardMonitor_OnClipboardContentChanged
    /// </summary>
    /// <param name="pascal"></param>
    /// <param name="insertToClipboard"></param>
    /// <returns></returns>
    public bool IsAlreadyContainedInXlfKeys(string pascal, bool insertToClipboard)
    {
        int first = -1;
        var keys = GetConsts(out first);
        if (keys.Contains(pascal))
        {
            if (insertToClipboard)
            {
                ClipboardHelper.SetText(pascal);
            }
            ThisApp.SetStatus(TypeOfMessage.Information, "Already " + pascal + " contained");
            return true;
        }
        return false;
    }
    Langs GetFrom()
    {
        return l == Langs.cs ? Langs.cs : Langs.en;
    }
    Langs GetTo()
    {
        return l == Langs.cs ? Langs.en : Langs.cs;
    }
    #endregion
    }
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
        instance = this;
        Loaded += InsertIntoXlfAndConstantCsUC_Loaded;
    }
    public void Init()
    {
        if (!initialized)
        {
            chblFilesOftenCorruptedDuringTranslating.Init();
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
    private void InsertIntoXlfAndConstantCsUC_Loaded(object sender, RoutedEventArgs e)
    {
        xlfEngine.txtText = txtText;
        xlfEngine.txtEnglishTranslate = txtEnglishTranslate;
        xlfEngine.rbEn = rbEn;
        xlfEngine.rbCs = rbCs;
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
        MenuItem miSetWaitingForUserDecision = MenuItemHelper.Get("Set waiting for user decision", delegate { xlfEngine.waitingForUserDecision = true; });
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
    /// <summary>
    /// Provide MenuItems() for InsertIntoXlfAndConstantCsUC
    /// </summary>
    public partial class InsertIntoXlfAndConstantCsUCMenuItems
    {
    #region Properties
    const string rlData = "RLData.en[";
    static Type type = typeof(InsertIntoXlfAndConstantCsUCMenuItems);
    XlfEngine xlfEngine = XlfEngine.Instance;
    public static InsertIntoXlfAndConstantCsUCMenuItems Instance = new InsertIntoXlfAndConstantCsUCMenuItems();
    static string pascal
    {
        get
        {
            return XlfEngine.Instance.pascal;
        }
        set
        {
            XlfEngine.Instance.pascal = value;
        }
    }
    const string pathAllStringsInFile = @"d:\Desktop\strings in sunamo\files\";
    const string pathReplaceBadChars = @"d:\Desktop\strings in sunamo\_ReplaceBadChars\";
    public static bool throwExceptions = true;
    /// <summary>
    /// \"
    /// </summary>
    const string from = "\\\"";
    /// <summary>
    /// "\""
    /// </summary>
    const string from2 = "\"\\\"\"";
    /// <summary>
    /// 3x bs, qm
    /// </summary>
    const string to = "\\\\\\\"";
    /// <summary>
    /// 4x bs, qm
    /// </summary>
    const string to2 = "\\\\\\\\\"";
    SplitStringsData splitStringsData = new SplitStringsData();
    #endregion
    #region Init
    private InsertIntoXlfAndConstantCsUCMenuItems()
    {
    }
    /// <summary>
    /// Must be set ThisApp before
    /// </summary>
    public static void InitializeNotTranslateAble()
    {
        SystemWindowsControls.Init();
        AllHtmlTags.Initialize();
        AllHtmlAttrs.Initialize();
        AllHtmlAttrsValues.Init();
        AllExtensionsHelper.Initialize();
        TranslateAbleHelper.isNameOfControl = SunamoCodeHelper.IsNameOfControl;
        SunamoTranslateConsts.InitializeNotTranslateAble();
    }
    #region Translate
    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void MiTranlateAllTranslateAbleStringsInFile_Click(object sender, RoutedEventArgs e)
    {
        var ssd = SavedStringsData.LoadDefault();
        TranlateAllTranslateAbleStringsInFile(ssd);
    }
    /// <summary>
    /// A1 cant be default null, must be loaded before due to avoid loading still the same files while mass process
    /// toTranslate will be obtained itself
    /// A2 are needed due to it's personally select. Can be null, then will be include all strings.
    /// A1 is selected in CompareInCheckBoxListUC
    /// </summary>
    /// <param name="toTranslate"></param>
    /// <param name="notToTranslate"></param>
    /// <param name="path"></param>
    public void TranlateAllTranslateAbleStringsInFile(SavedStringsData ssd, string path = null)
    {
        SplitAllStringsToTranslateAble(path);
        string cs = null;
        List<string> csLines;
        CollectionWithoutDuplicates<string> notToTranslate = null;
        if (splitStringsData != null)
        {
            notToTranslate = splitStringsData.notToTranslate;
        }
        var s = GetAllStringsIn(splitStringsData, ref path, out cs, out csLines).c;
        CA.RemoveWhichExists(s, ssd.manuallyNo);
        CA.RemoveWhichExists(s, ssd.autoNo);
        CA.RemoveWhichExists(notToTranslate.c, ssd.autoYes);
        CA.RemoveWhichExists(notToTranslate.c, ssd.autoNo);
        if (notToTranslate != null)
        {
            for (int i = s.Count - 1; i >= 0; i--)
            {
                if (notToTranslate.c.Contains(s[i]))
                {
                    s.RemoveAt(i);
                }
            }
        }
        foreach (var item in s)
        {
            var qm = SH.WrapWithQm(item);
            // Add to xlf
            xlfEngine.Add(false, item);
            cs = SH.ReplaceAll(cs, xlfEngine.TextFromRLData(XlfEngine.Instance.pascal), qm);
        }
        // commented because is not working inside of method
        //cs = SH.ReplaceAll(cs, " static readonly ", " const ");
        TF.SaveFile(cs, path);
    }
    #endregion
    #region MenuItems handlers
    /// <summary>
    /// Remove all comments from postfix, Create parameters to methods
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void MiCreateParametersToMethod_Click(object sender, RoutedEventArgs e)
    {
        var lines = ClipboardHelper.GetLines();
        CA.RemoveStringsEmpty2(lines);
        StringBuilder sb = new StringBuilder();
        foreach (var item in lines)
        {
            var name = SH.RemoveAfterFirst(item, "//").Trim().TrimEnd(AllChars.comma);
            sb.AppendLine("HtmlGenericControl " + name + ",");
        }
        ClipboardHelper.SetText(sb.ToString());
    }
    /// <summary>
    /// Set to clipboard assigment fields in ctor
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void MiCreateAssigmentFromFields_Click(object sender, RoutedEventArgs e)
    {
        //monitorClipboardHelper.IsChecked = false;
        var content = Clipboard.GetText();
        var t = RoslynHelper.GetSyntaxTree(content, true).GetRoot();
        var dict = RoslynParser.GetVariablesInCsharp(t);
        var onlyB = dict.OnlyBs();
        var ctorInner = CSharpHelper.GetCtorInner(3, onlyB);
        ClipboardHelper.SetText(ctorInner);
        // monitorClipboardHelper.IsChecked = true;
    }
    /// <summary>
    /// Add to XlfKeys.cs from xlf
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void MiAddTranslationWhichIsntInKeys_Click(object sender, RoutedEventArgs e)
    {
        // Before use have to save to xlf and SaveResouresToRL
        var keysAll = RLData.en.Keys.ToList();
        xlfEngine.AddConsts(keysAll);
    }
    #endregion
    #region Button handlers
    /// <summary>
    /// Load all consts from cs file and create instead of them properties which refer to RLData
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void BtnCreatePropertiesFromConsts_Click(object sender, RoutedEventArgs e)
    {
        CreatePropertiesFromConsts();
    }
    #endregion
    #region Other methods
    List<string> LinesFrom(SavedStrings ss)
    {
        return AllProjectsSearchHelper.LinesFrom(SavedStrings.AutoYes);
    }
    public void CreatePropertiesFromConsts()
    {
        string filename = ClipboardHelper.GetText();
        bool ns = false;
        var content = TF.ReadFile(filename);
        var lines = SH.GetLines(content);
        int first = -1;
        CSharpGenerator csg = new CSharpGenerator();
        int tabCount = 1;
        for (int i = lines.Count - 1; i >= 0; i--)
        {
            var text = lines[i].Trim();
            if (ns)
            {
                if (text.StartsWith("namespace"))
                {
                    ns = true;
                    tabCount = 2;
                }
            }
            if (text.Contains(CSharpParser.c))
            {
                first = i;
                lines.RemoveAt(i);
                string key = SH.GetTextBetween(text, CSharpParser.c, CSharpParser.eq);
                // Won't be if string will be multilined
                int first2 = text.IndexOf("\"");
                int last2 = text.LastIndexOf("\"");
                var value = SH.GetTextBetweenTwoChars(text, first2, last2);
                string key2 = key;
                xlfEngine.Add(false, value, key);
                csg.Property(tabCount, AccessModifiers.Public, true, "string", key2, true, false, xlfEngine.TextFromRLData(key2));
            }
        }
        if (first != -1)
        {
            lines.Insert(first, csg.ToString());
            TF.SaveLines(lines, filename);
        }
        // Cant be because will be execute before other - async method will execute after finishing this
        //acceptHotkey.Tag = null;
        // Cant be, is then capture for changes with delay and dont save elements to xlf
        //monitorClipboardWithLabel.monitorClipboard.IsChecked = true;
    }
    private static void WriteFile(string c2FilePath, string tb)
    {
        TF.SaveFile(tb, pathAllStringsInFile + FS.GetFileNameWithoutExtension(c2FilePath).TrimEnd('2') + ".txt");
    }
    public List<MenuItem> MenuItems()
    {
        List<MenuItem> menuItems = new List<MenuItem>();
        // Add to XlfKeys.cs from xlf
        MenuItem miAddTranslationWhichIsntInKeys = MenuItemHelper.Get("Add translation which isnt in keys (load from RLData.en)");
        miAddTranslationWhichIsntInKeys.Click += MiAddTranslationWhichIsntInKeys_Click;
        menuItems.Add(miAddTranslationWhichIsntInKeys);
        // Load all consts from cs file and create instead of them properties which refer to RLData
        MenuItem miCreatePropertiesFromConsts = MenuItemHelper.Get("Create properties from consts (file path in clipboard)", BtnCreatePropertiesFromConsts_Click); menuItems.Add(miCreatePropertiesFromConsts);
        // Set to clipboard assigment fields in ctor
        MenuItem miCreateAssigmentFromFields = MenuItemHelper.Get("Create assigment from fields");
        miCreateAssigmentFromFields.Click += MiCreateAssigmentFromFields_Click;
        menuItems.Add(miCreateAssigmentFromFields);
        // Remove all comments from postfix, Create parameters to methods
        MenuItem miCreateParametersToMethod = MenuItemHelper.Get("Create parameter to method");
        miCreateParametersToMethod.Click += MiCreateParametersToMethod_Click;
        menuItems.Add(miCreateParametersToMethod);
        // Split all strings to translate-able and not. Working only with c# files
        MenuItem miReplaceAllStringsInFiles = MenuItemHelper.Get("1. Split all splitable strings in files to translate-able (put path in clipboard)");
        miReplaceAllStringsInFiles.Click += MiSplitAllStringsToTranslateAble_Click;
        menuItems.Add(miReplaceAllStringsInFiles);
        MenuItem miTranlateAllTranslateAbleStringsInFile = MenuItemHelper.Get("2. Translate all translate-able strings in file");
        miTranlateAllTranslateAbleStringsInFile.Click += MiTranlateAllTranslateAbleStringsInFile_Click;
        menuItems.Add(miTranlateAllTranslateAbleStringsInFile);
        return menuItems;
    }
    /// <summary>
    /// If it is commented, won't return
    /// </summary>
    /// <param name="cs"></param>
    /// <param name="csLines"></param>
    /// <returns></returns>
    private List<int> GetQuotes(ref StringBuilder cs, List<string> csLines)
    {
        var csts = cs.ToString();
        // to2 have priority before to because is longer
        var to2Idx = SH.ReturnOccurencesOfString(csts, to2);
        cs = SH.ReplaceAllSb(cs, string.Empty.PadRight(to2.Length, AllChars.space), to2);
        var ts = cs.ToString();
        var toIdx = SH.ReturnOccurencesOfString(ts, to);
        cs = SH.ReplaceAllSb(cs, string.Empty.PadRight(to.Length, AllChars.space), to);
        ts = cs.ToString();
        csts = cs.ToString();
        var occ = SH.ReturnOccurencesOfString(csts, "\"");
        for (int i = occ.Count - 1; i >= 0; i--)
        {
            var pos = occ[i];
            // "1//"
            var ch = cs[pos];
            var minusOne = cs[pos - 1];
            var minusTwo = cs[pos - 2];
            if (minusOne == AllChars.bs && minusTwo != AllChars.bs)
            {
                if (cs[occ[i - 1] - 1] != '@')
                {
                    occ.RemoveAt(i);
                    continue;
                }
            }
            #region Removing which is in comment
            var line = SH.GetLineIndexFromCharIndex(csts, pos);
            var l = csLines[line];
            var dx = l.IndexOf("//");
            if (dx != -1 && !l.Contains("http") && !l.Contains("ftp"))
            {
                if (dx < pos)
                {
                    occ.RemoveAt(i);
                    continue;
                }
            }
            #endregion
        }
        for (int i = to2Idx.Count - 1; i >= 0; i--)
        {
            cs = cs.Remove(to2Idx[i], to2.Length);
        }
        ts = cs.ToString();
        for (int i = toIdx.Count - 1; i >= 0; i--)
        {
            cs = cs.Remove(toIdx[i], to.Length);
        }
        ts = cs.ToString();
        for (int i = 0; i < toIdx.Count; i++)
        {
            cs = cs.Insert(toIdx[i], to);
        }
        ts = cs.ToString();
        for (int i = 0; i < to2Idx.Count; i++)
        {
            cs = cs.Insert(to2Idx[i], to2);
        }
        return occ;
    }
    #endregion
    /// <summary>
    /// A1.bet is the same as returned value
    /// Returns object only for exception checking
    /// Return null in case of any exception
    /// In get are only translate able strings
    /// </summary>
    /// <returns></returns>
    public CollectionWithoutDuplicates<string> GetAllStringsIn(SplitStringsData splitStringsData, ref string cFilePath, out string cs, out List<string> csLines)
    {
        InitializeNotTranslateAble();
        const string methodName = "MiReplaceAllStringsInFiles_Click";
        //cFilePath = null;
        cs = null;
        csLines = null;
        string c2FilePath = null;
        if (cFilePath == null)
        {
            cFilePath = @"d:\_Test\WpfApp1\WpfApp1\InsertIntoXlfAndConstantCsUCMenuItems\MiReplaceAllStringsInFiles_Click\C.cs";
        }
        if (cFilePath == null)
        {
            cFilePath = ClipboardHelper.GetText();
        }
        if (!FS.ExistsFile(cFilePath))
        {
            cFilePath = DW.SelectOfFile();
        }
        if (!FS.ExistsFile(cFilePath))
        {
            ThisApp.SetStatus(TypeOfMessage.Error, "Selected file doesn't exists");
        }
        else
        {
            c2FilePath = cFilePath;
        }
        if (!cFilePath.EndsWith(".cs"))
        {
            ThisApp.SetStatus(TypeOfMessage.Error, "path dont end with .cs");
            return splitStringsData.bet;
        }
        cs = TF.ReadFile(cFilePath);
        csLines = SH.GetLines(cs);
        TF.SaveFile(DateTime.Now.ToShortTimeString() + Environment.NewLine + cs, pathReplaceBadChars + "original.cs");
        cs = ReplaceBadChars(cs, csLines, true, cFilePath);
        csLines = SH.GetLines(cs);
        StringBuilder sbcs = new StringBuilder(cs);
        // GetQuotes after ReplaceBadChars
        var indexes = GetQuotes(ref sbcs, csLines);
        cs = sbcs.ToString();
        if (indexes.Count % 2 == 1)
        {
            if (throwExceptions)
            {
                AllProjectsSearchSettings.filesNotToTranslate.Add(FS.GetFileNameWithoutExtension(cFilePath));
                //ThrowExceptions.Custom(type, methodName, "Number of quotes is not even, source: " + FS.GetFileName(cFilePath));
                return splitStringsData.bet;
            }
        }
        var lines = SH.GetLines(cs);
        #region Add strings to replacing - exclude aspx, tags, attrs etc.
        GetBetween(splitStringsData, cs, indexes, lines);
        #endregion
        var c = splitStringsData.bet.c;
        bool wrapWithTag = false;
        string startTag = "<b>";
        string endTag = "</b>";
        bool replacedAny = false;
        StringBuilder sb = new StringBuilder();
        foreach (var item2 in c)
        {
            var item = item2;
            wrapWithTag = false;
            var trimmed = item.Trim();
            if (item.StartsWith(startTag) && item.EndsWith(endTag))
            {
                wrapWithTag = true;
            }
            else if (trimmed.StartsWith(startTag) && trimmed.EndsWith(endTag))
            {
                cs = cs.Replace(SH.WrapWithQm(item), SH.WrapWithQm(trimmed));
                item = trimmed;
                wrapWithTag = true;
            }
            if (wrapWithTag)
            {
                var text = item.Substring(startTag.Length, item.Length - endTag.Length - startTag.Length);
                sb.Clear();
                sb.Append(SH.WrapWithQm(startTag));
                sb.Append(AllStrings.plus);
                sb.Append(SH.WrapWithQm(text));
                sb.Append(AllStrings.plus);
                sb.Append(SH.WrapWithQm(endTag));
                cs = SH.ReplaceOnce(cs, item2, sb.ToString());
                replacedAny = true;
            }
        }
        if (replacedAny)
        {
            TF.WriteAllText(cFilePath, cs);
        }
        return splitStringsData.bet;
    }
    #region ReplaceBadChars
    public string ReplaceBadChars(string cs, List<string> csLines, bool loading, string cFilePath)
    {
        TranslateAbleHelper.outsideReplaceBadChars = false;
        var methodName = "ReplaceBadChars";
        CollectionWithoutDuplicates<string> result = new CollectionWithoutDuplicates<string>();
        cs = ReplaceBadCharsWorker(ref cs, loading, result);
        if (loading)
        {
            for (int i = 1; i < 5; i++)
            {
                cs = SH.ReplaceAll(cs, "@" + SH.WrapWithQm(SH.JoinTimes(i, @"\")), SH.WrapWithQm(SH.JoinTimes(i, @"\\")));
            }
        }
        TF.SaveFile(DateTime.Now.ToShortTimeString() + Environment.NewLine + cs, pathReplaceBadChars + "replaced.cs");
        csLines = SH.GetLines(cs);
        StringBuilder sbcs = new StringBuilder(cs);
        // GetQuotes before ReplaceBadChars
        var dxs = GetQuotes(ref sbcs, csLines);
        cs = sbcs.ToString();
        if (dxs.Count % 2 == 1)
        {
            if (throwExceptions)
            {
                //ThrowExceptions.Custom(type, methodName, "Number of quotes is not even, source: " + FS.GetFileName(cFilePath));
                AllProjectsSearchSettings.filesNotToTranslate.Add(FS.GetFileNameWithoutExtension(cFilePath));
                return cs;
            }
        }
        GetBetween(splitStringsData, cs, dxs, csLines);
        TranslateAbleHelper.outsideReplaceBadChars = true;
        return cs;
    }
    #region ReplaceBadCharsWorker
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cs"></param>
    /// <param name="loading"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    private static string ReplaceBadCharsWorker(ref string cs, bool loading, CollectionWithoutDuplicates<string> result)
    {
        if (result == null)
        {
            return cs;
        }
        if (loading)
        {
            foreach (var item in result.c)
            {
                var cs2 = SH.ReplaceAll(item, to2, from2);
                cs2 = SH.ReplaceAll(cs2, to, from);
                cs = SH.ReplaceAll(cs, cs2, item);
            }
        }
        else
        {
            foreach (var item in result.c)
            {
                var cs2 = SH.ReplaceAll(item, from2, to2);
                cs2 = SH.ReplaceAll(cs2, from, to);
                cs = SH.ReplaceAll(cs, cs2, item);
            }
        }
        return cs;
    }
    #endregion
    #endregion
    #region GetBetween
    /// <summary>
    /// In get are only translate able strings
    /// </summary>
    /// <param name="cs"></param>
    /// <param name="result"></param>
    /// <param name="indexes"></param>
    /// <param name="lines"></param>
    /// <param name="notToTranslate"></param>
    private static void GetBetween(SplitStringsData splitStringsData, string cs, List<int> indexes, List<string> lines)
    {
        for (int i = 0; i < indexes.Count; i++)
        {
            string between = null;
            if (indexes[i] - rlData.Length > -1)
            {
                between = SH.Substring(cs, indexes[i] - rlData.Length, indexes[i + 1]);
                if (between.StartsWith("RLData."))
                {
                    i++;
                    continue;
                }
            }
            between = SH.Substring(cs, indexes[i] + 1, indexes[++i]);
            if (between.Trim() == string.Empty)
            {
                continue;
            }
            bool add = TranslateAbleHelper.IsToTranslate(splitStringsData, between, indexes[i], lines);
            if (add)
            {
                splitStringsData.bet.Add(SH.FirstLine(between));
            }
            else
            {
                splitStringsData.notToTranslate.Add(SH.FirstLine(between));
            }
        }
    }
    #endregion
    #region SplitAllStrings
    /// <summary>
    /// Split all strings to translate-able and not. Working only with c# files
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void MiSplitAllStringsToTranslateAble_Click(object sender, RoutedEventArgs e)
    {
        SplitAllStringsToTranslateAble();
    }
    public void SplitAllStringsToTranslateAble(string cFilePath = null)
    {
        string cs = null;
        List<string> csLines;
        var bet = splitStringsData.bet;
        GetAllStringsIn(splitStringsData, ref cFilePath, out cs, out csLines);
        if (bet == null)
        {
            WriteFile(cFilePath, "!null");
            return;
        }
        if (bet.c.Length() == 0)
        {
            WriteFile(cFilePath, "!zero elements");
            return;
        }
        string c2FilePath = cFilePath;
        char firstChar = 'a';
        char lastChar = 'a';
        char firstChar2 = 'a';
        char lastChar2 = 'a';
        bool debug = false;
        #region Find last and first chars
        if (debug)
        {
            Debugger.Break();
        }
        foreach (var item in splitStringsData.v)
        {
            StringPaddingData d = splitStringsData.v[item.Key];
            var key = item.Key;
            d.first = AllProjectsSearchHelper.IsSpecialChar(0, ref key, ref firstChar);
            d.last = AllProjectsSearchHelper.IsSpecialChar(key.Count() - 1, ref key, ref lastChar);
            d.first2 = AllProjectsSearchHelper.IsSpecialChar(1, ref key, ref firstChar2);
            d.last2 = AllProjectsSearchHelper.IsSpecialChar(key.Count() - 2, ref key, ref lastChar2);
            key = key.Trim();
            d.firstChar = firstChar;
            d.lastChar = lastChar;
            d.firstChar2 = firstChar2;
            d.lastChar2 = lastChar2;
        }
        #endregion
        #region Create new string without padding chars and replace
        if (debug)
        {
            Debugger.Break();
        }
        // Make replacing
        foreach (var item in splitStringsData.v)
        {
            var d = item.Value;
            var text = item.Key;
            StringBuilder replaceFor = new StringBuilder();
            #region Remove first chars and append first char
            // First I will working with prefix
            if (d.first || d.first2)
            {
                replaceFor.Append("\"");
            }
            if (d.first || (!d.first && d.first2))
            {
                replaceFor.Append(d.firstChar);
                text = text.Remove(0, 1);
            }
            if (d.first2)
            {
                replaceFor.Append(d.firstChar2);
                text = text.Remove(0, 1);
            }
            if (d.first || d.first2)
            {
                replaceFor.Append("\" + ");
            }
            #endregion
            #region Remove last chars
            // In first line I only removing ...
            if (d.last)
            {
                text = text.Remove(text.Length - 1, 1);
            }
            if (d.last2)
            {
                text = text.Remove(text.Length - 1, 1);
            }
            #endregion
            // Append text
            replaceFor.Append(AllStrings.qm + text + AllStrings.qm);
            #region Append last chars
            // ... because I can add it after I added the main string
            if (d.last || d.last2)
            {
                replaceFor.Append(" + \"");
            }
            if (d.last2 && d.last)
            {
                replaceFor.Append(d.lastChar2);
            }
            if (d.last)
            {
                replaceFor.Append(d.lastChar);
            }
            if (d.last || d.last2)
            {
                replaceFor.Append("\"");
            }
            #endregion
            string replaceFor2 = replaceFor.ToString();
            var replaceFor3 = SH.WrapWithQm(item.Key);
            if (replaceFor3 != replaceFor2)
            {
                cs = SH.ReplaceAll(cs, replaceFor2, replaceFor3);
            }
        }
        #endregion
        #endregion
        cs = ReplaceBadChars(cs, csLines, false, cFilePath);
        TF.SaveFile(cs, c2FilePath);
        TextOutputGenerator tb = new TextOutputGenerator();
        tb.List(TranslateAbleHelper.toTranslate, "To translate");
        tb.List(TranslateAbleHelper.notToTranslate, "Not to translate");
        TranslateAbleHelper.toTranslate.Clear();
        TranslateAbleHelper.notToTranslate.Clear();
        WriteFile(c2FilePath, tb.ToString());
    }
    #endregion
    }
    public class XlfResourcesH
    {
    public static bool initialized = false;
    public static void SaveResouresToRL(string basePath)
    {
        SaveResouresToRL(basePath, "cs");
        SaveResouresToRL(basePath, "en");
    }
    /// <summary>
    /// Private to use SaveResouresToRLSunamo
    /// </summary>
    private static void SaveResouresToRL()
    {
        SaveResouresToRL(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName));
    }
    public static void SaveResouresToRLSunamo()
    {
        SaveResouresToRL(DefaultPaths.sunamoProject);
    }
    /// <summary>
    /// A1 = CS-CZ or CS etc
    /// </summary>
    /// <param name="lang"></param>
    private static void SaveResouresToRL(string basePath, string lang)
    {
        // cant be inicialized - after cs is set initialized to true and skip english
        //initialized = true;
        var path = Path.Combine(basePath, "MultilingualResources");
        var files = FS.GetFiles(path, "*.xlf", SearchOption.TopDirectoryOnly);
        foreach (var file in files)
        {
            var fn = FS.GetFileName(file).ToLower();
            bool isCzech = fn.Contains("cs");
            bool isEnglish = fn.Contains("en");
            var doc = new XlfDocument(file);
            lang = lang.ToLower();
            var xlfFiles = doc.Files.Where(d => d.Original.ToLower().Contains(lang));
            if (xlfFiles.Count() != 0)
            {
                var xlfFile = xlfFiles.First();
                foreach (var u in xlfFile.TransUnits)
                {
                    if (isCzech)
                    {
                        if (!RLData.cs.ContainsKey(u.Id))
                        {
                            RLData.cs.Add(u.Id, u.Target);
                        }
                    }
                    else if (isEnglish)
                    {
                        if (!RLData.en.ContainsKey(u.Id))
                        {
                            RLData.en.Add(u.Id, u.Target);
                        }
                    }
                    else
                    {
                        throw new Exception("Unvalid file" + " " + file + ", " + "please delete it");
                    }
                }
            }
        }
    }
    }
    /// <summary>
    /// Trans-units in *.xlf file and others
    /// </summary>
    public class XlfData
    {
    public XElement group = null;
    public XDocument xd = null;
    public IEnumerable<XElement> trans_units = null;
    }
    /// <summary>
    /// General methods for working with XML
    /// </summary>
    public class XmlLocalisationInterchangeFileFormat
    {
    public static Langs GetLangFromFilename(string s)
    {
        s = FS.GetFileNameWithoutExtension(s);
        var parts = SH.Split(s, AllChars.dot);
        string last = parts[parts.Count - 1].ToLower();
        if (last.StartsWith("cs"))
        {
            return Langs.cs;
        }
        return Langs.en;
    }
    /// <summary>
    /// A1 is possible to obtain with XmlLocalisationInterchangeFileFormat.GetLangFromFilename
    /// </summary>
    /// <param name="enS"></param>
    /// <returns></returns>
    public static void TrimStringResources(Langs toL, string fn)
    {
        var d = GetTransUnits(toL, fn);
        List<XElement> tus = new List<XElement>();
        foreach (XElement item in d.trans_units)
        {
            XElement source = item.Element(XName.Get("source"));
            XElement target = item.Element(XName.Get("target"));
            TrimValueIfNot(source);
            TrimValueIfNot(target);
        }
        d.xd.Save(fn);
    }
    /// <summary>
    /// A1 is possible to obtain with XmlLocalisationInterchangeFileFormat.GetLangFromFilename
    /// </summary>
    /// <param name="fn"></param>
    /// <param name="xd"></param>
    /// <returns></returns>
    public static XlfData GetTransUnits(Langs toL, string fn)
    {
        string enS = File.ReadAllText(fn);
        XlfData d = new XlfData();
        XmlNamespacesHolder h = new XmlNamespacesHolder();
        h.ParseAndRemoveNamespacesXmlDocument(enS);
        d.xd = XHelper.CreateXDocument(fn);
        XHelper.AddXmlNamespaces(h.nsmgr);
        XElement xliff = XHelper.GetElementOfName(d.xd, "xliff");
        var allElements = XHelper.GetElementsOfNameWithAttrContains(xliff, "file", "target-language", toL.ToString(), false);
        var resources = allElements.Where(d2 => XHelper.Attr(d2, "original").Contains("/" + "RESOURCES" + "/"));
        XElement file = resources.First();
        XElement body = XHelper.GetElementOfName(file, "body");
        d.group = XHelper.GetElementOfName(body, "group");
        d.trans_units = XHelper.GetElementsOfName(d.group, TransUnit.tTransUnit);
        return d;
    }
    private static void TrimValueIfNot(XElement source)
    {
        string sourceValue = source.Value;
        if (sourceValue.Length != 0)
        {
            if (char.IsWhiteSpace(sourceValue[sourceValue.Length - 1]) || char.IsWhiteSpace(sourceValue[0]))
            {
                source.Value = sourceValue.Trim();
            }
        }
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="toL"></param>
    /// <param name="originalSource"></param>
    /// <param name="translated"></param>
    /// <param name="pascal"></param>
    /// <param name="fn"></param>
    public static void Append(Langs toL, string originalSource, string translated, string pascal, string fn)
    {
        var d = GetTransUnits(toL, fn);
        var exists = XHelper.GetElementOfNameWithAttr(d.group, TransUnit.tTransUnit, "id", pascal);
        if (exists != null)
        {
            return;
        }
        TransUnit tu = new TransUnit();
        tu.id = pascal;
        tu.source = originalSource;
        tu.translate = true;
        tu.target = translated;
        var xml = tu.ToString();
        XElement xe = XElement.Parse(xml);
        xe = XHelper.MakeAllElementsWithDefaultNs(xe);
        d.group.Add(xe);
        d.xd.Save(fn);
        XHelper.FormatXml(fn);
    }
}