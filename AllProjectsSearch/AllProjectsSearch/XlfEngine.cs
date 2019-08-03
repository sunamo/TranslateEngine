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
            //englishTranslate = result.MergedTranslation; 
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
            // originalSource(always english - same in all),translated,pascal

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

