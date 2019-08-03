using AllProjectsSearch;
using AllProjectsSearch.UC;
using Roslyn;
using sunamo;
using sunamo.Essential;
using sunamo.Values;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

/// <summary>
/// Provide MenuItems() for InsertIntoXlfAndConstantCsUC
/// </summary>
public partial class InsertIntoXlfAndConstantCsUCMenuItems
{
    #region Properties
    const string rlData = "RLData.en[";
    static Type type = typeof(InsertIntoXlfAndConstantCsUCMenuItems);
    XlfEngine xlfEngine =XlfEngine.Instance;
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
    #endregion

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
        MenuItem miAddTranslationWhichIsntInKeys = MenuItemHelper.CreateNew("Add translation which isnt in keys (load from RLData.en)");
        miAddTranslationWhichIsntInKeys.Click += MiAddTranslationWhichIsntInKeys_Click;
        menuItems.Add(miAddTranslationWhichIsntInKeys);

        // Load all consts from cs file and create instead of them properties which refer to RLData
        MenuItem miCreatePropertiesFromConsts = MenuItemHelper.CreateNew("Create properties from consts (file path in clipboard)", BtnCreatePropertiesFromConsts_Click); menuItems.Add(miCreatePropertiesFromConsts);

        // Set to clipboard assigment fields in ctor
        MenuItem miCreateAssigmentFromFields = MenuItemHelper.CreateNew("Create assigment from fields");
        miCreateAssigmentFromFields.Click += MiCreateAssigmentFromFields_Click;
        menuItems.Add(miCreateAssigmentFromFields);

        // Remove all comments from postfix, Create parameters to methods
        MenuItem miCreateParametersToMethod = MenuItemHelper.CreateNew("Create parameter to method");
        miCreateParametersToMethod.Click += MiCreateParametersToMethod_Click;
        menuItems.Add(miCreateParametersToMethod);

        // Split all strings to translate-able and not. Working only with c# files
        MenuItem miReplaceAllStringsInFiles = MenuItemHelper.CreateNew("1. Split all splitable strings in files to translate-able (put path in clipboard)");
        miReplaceAllStringsInFiles.Click += MiSplitAllStringsToTranslateAble_Click;
        menuItems.Add(miReplaceAllStringsInFiles);

        MenuItem miTranlateAllTranslateAbleStringsInFile = MenuItemHelper.CreateNew("2. Translate all translate-able strings in file");
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

    #region GetAllStrings



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
    public string ReplaceBadChars(string cs,  List<string> csLines, bool loading, string cFilePath)
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
        var dxs = GetQuotes( ref sbcs, csLines);

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
    private static void GetBetween(SplitStringsData splitStringsData, string cs,  List<int> indexes, List<string> lines)
    {
        for (int i = 0; i < indexes.Count; i++)
        {
            string between = null;
            if (indexes[i] - rlData.Length > -1)
            {
                between = SH.Substring(cs, indexes[i] - rlData.Length, indexes[i+1]);
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
            // Prvně budu pracovat s prefixem
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

