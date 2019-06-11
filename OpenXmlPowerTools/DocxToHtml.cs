﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using DocumentFormat.OpenXml.Packaging;

namespace OpenXmlPowerTools
{
    public partial class WmlDocument
    {
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public XElement ConvertToHtml(DocxToHtmlSettings htmlConverterSettings)
        {
            return DocxToHtml.ConvertToHtml(this, htmlConverterSettings);
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public XElement ConvertToHtml(HtmlConverterSettings htmlConverterSettings)
        {
            DocxToHtmlSettings settings = new DocxToHtmlSettings(htmlConverterSettings);
            return DocxToHtml.ConvertToHtml(this, settings);
        }
    }

    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
    public class DocxToHtmlSettings
    {
        public string PageTitle;
        public bool DisplayRevisionTracking = true;
        public bool DisplayComments = true;
        public bool DisplayCommentsAsBalloon = false; // if not balloon, then they will display at end of document, after an HR

        public string CssClassPrefix;
        public bool FabricateCssClasses;
        public string GeneralCss;
        public string AdditionalCss;
        public string OpenXmlPowerToolsSpecificCss;
        public bool RestrictToSupportedLanguages;
        public bool RestrictToSupportedNumberingFormats;
        public Dictionary<string, Func<string, int, string, string>> ListItemImplementations;
        public Func<DocxToHtmlImageInfo, XElement> ImageHandler;

        private static string OxPtSpecificCss = @"
.OxPt-ToolTip {
    position: relative;
    display: inline-block;
    /* border-bottom: 1px dotted #ccc; */
    zcursor: help;
    color: #006080;
}
.OxPt-ToolTip .OxPt-RevTrk {
    visibility: hidden;
    position: absolute;
    width: 300px;
    border-color: black;
    border-width: 1px;
    border-style: solid;
    box-shadow: 3px 3px #cccccc;
    background-color: rgb(214,235,254);
    color: black;
    text-align: left;
    padding: 5px 5px 5px 15px;
    border-radius: 6px;
    z-index: 1;
    opacity: 0;
    transition: opacity .6s;
}
.OxPt-ToolTip:hover .OxPt-RevTrk {
    visibility: visible;
    opacity: 1;
}

.OxPt-ToolTip:hover  {
    visibility: visible;
}
.OxPt-ToolTip-Right {
  top: -7px;
 /* left: 125%;  */
}

.OxPt-ToolTip-Right::after {
    content: """";
    position: absolute;
    top: 50%;
    right: 100%;
    margin-top: -7px;
    border-width: 7px;
    border-style: solid;
    border-color: transparent black transparent transparent;
}

.OxPt-Comment-Initials {
    color: red;
    border-bottom: 1px dotted #ccc;
}
";
#if false
        // first version
        private static string OxPtSpecificCss = @"
.OxPt-ToolTip {
    position: relative;
    display: inline-block;
    /* border-bottom: 1px dotted #ccc; */
    zcursor: help;
    color: #006080;
}
.OxPt-ToolTip .OxPt-RevTrk {
    visibility: hidden;
    position: absolute;
    width: 300px;
    background-color: #555;
    color: #fff;
    text-align: left;
    padding: 5px 5px 5px 15px;
    border-radius: 6px;
    z-index: 1;
    opacity: 0;
    transition: opacity .6s;
}
.OxPt-ToolTip:hover .OxPt-RevTrk {
    visibility: visible;
    opacity: 1;
}

.OxPt-ToolTip:hover  {
    visibility: visible;
}
.OxPt-ToolTip-Right {
  top: -5px;
  left: 125%;  
}

.OxPt-ToolTip-Right::after {
    content: """";
    position: absolute;
    top: 50%;
    right: 100%;
    margin-top: -5px;
    border-width: 5px;
    border-style: solid;
    border-color: transparent #555 transparent transparent;
}
";
#endif

        public DocxToHtmlSettings()
        {
            PageTitle = "";
            CssClassPrefix = "pt-";
            FabricateCssClasses = true;
            GeneralCss = "span { white-space: pre-wrap; }";
            AdditionalCss = "";
            OpenXmlPowerToolsSpecificCss = OxPtSpecificCss;
            RestrictToSupportedLanguages = false;
            RestrictToSupportedNumberingFormats = false;
            ListItemImplementations = ListItemRetrieverSettings.DefaultListItemTextImplementations;
        }

        public DocxToHtmlSettings(HtmlConverterSettings htmlConverterSettings)
        {
            PageTitle = htmlConverterSettings.PageTitle;
            CssClassPrefix = htmlConverterSettings.CssClassPrefix;
            FabricateCssClasses = htmlConverterSettings.FabricateCssClasses;
            GeneralCss = htmlConverterSettings.GeneralCss;
            AdditionalCss = htmlConverterSettings.AdditionalCss;
            OpenXmlPowerToolsSpecificCss = OxPtSpecificCss;
            RestrictToSupportedLanguages = htmlConverterSettings.RestrictToSupportedLanguages;
            RestrictToSupportedNumberingFormats = htmlConverterSettings.RestrictToSupportedNumberingFormats;
            ListItemImplementations = htmlConverterSettings.ListItemImplementations;
            ImageHandler = htmlConverterSettings.ImageHandler;
        }
    }

    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
    public class HtmlConverterSettings
    {
        public string PageTitle;
        public string CssClassPrefix;
        public bool FabricateCssClasses;
        public string GeneralCss;
        public string AdditionalCss;
        public bool RestrictToSupportedLanguages;
        public bool RestrictToSupportedNumberingFormats;
        public Dictionary<string, Func<string, int, string, string>> ListItemImplementations;
        public Func<DocxToHtmlImageInfo, XElement> ImageHandler;

        public HtmlConverterSettings()
        {
            PageTitle = "";
            CssClassPrefix = "pt-";
            FabricateCssClasses = true;
            GeneralCss = "span { white-space: pre-wrap; }";
            AdditionalCss = "";
            RestrictToSupportedLanguages = false;
            RestrictToSupportedNumberingFormats = false;
            ListItemImplementations = ListItemRetrieverSettings.DefaultListItemTextImplementations;
        }
    }

    public static class HtmlConverter
    {
        public static XElement ConvertToHtml(WmlDocument wmlDoc, HtmlConverterSettings htmlConverterSettings)
        {
            DocxToHtmlSettings settings = new DocxToHtmlSettings(htmlConverterSettings);
            return DocxToHtml.ConvertToHtml(wmlDoc, settings);
        }

        public static XElement ConvertToHtml(WordprocessingDocument wDoc, HtmlConverterSettings htmlConverterSettings)
        {
            DocxToHtmlSettings settings = new DocxToHtmlSettings(htmlConverterSettings);
            return DocxToHtml.ConvertToHtml(wDoc, settings);
        }
    }

    [SuppressMessage("ReSharper", "NotAccessedField.Global")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class DocxToHtmlImageInfo
    {
        public Bitmap Bitmap;
        public XAttribute ImgStyleAttribute;
        public string ContentType;
        public XElement DrawingElement;
        public string AltText;

        public const int EmusPerInch = 914400;
        public const int EmusPerCm = 360000;
    }

    internal enum RevTrackingMode
    {
        None,
        Delete,
        Insert,
        MoveFrom,
        MoveTo,
    }

    internal class DocxToHtmlTransformMode
    {
        public RevTrackingMode RevTrackingMode;
        public string AdditionalClass;
        public string ToolTipText;
        public string ToolTipTextClass;
    }

    internal class DocxToHtmlTransformState
    {
        public List<object> CommentContent = new List<object>();
        public List<object> FootNotes = new List<object>();
        public List<object> EndNotes = new List<object>();
        public int NextCommentNumber = 1;
        public Graphics Graphics;
    }

    public static class DocxToHtml
    {
        public static XElement ConvertToHtml(WmlDocument doc, DocxToHtmlSettings htmlConverterSettings)
        {
            using (OpenXmlMemoryStreamDocument streamDoc = new OpenXmlMemoryStreamDocument(doc))
            {
                using (WordprocessingDocument document = streamDoc.GetWordprocessingDocument())
                {
                    return ConvertToHtml(document, htmlConverterSettings);
                }
            }
        }

        public static XElement ConvertToHtml(WordprocessingDocument wordDoc, DocxToHtmlSettings docxToHtmlSettings)
        {
            if (!docxToHtmlSettings.DisplayRevisionTracking)
            {
                RevisionAccepter.AcceptRevisions(wordDoc);
            }

            SimplifyMarkupSettings simplifyMarkupSettings = new SimplifyMarkupSettings
            {
                RemoveComments = !docxToHtmlSettings.DisplayComments,
                RemoveContentControls = true,
                RemoveEndAndFootNotes = false,
                RemoveFieldCodes = false,
                RemoveLastRenderedPageBreak = true,
                RemovePermissions = true,
                RemoveProof = true,
                RemoveRsidInfo = true,
                RemoveSmartTags = true,
                RemoveSoftHyphens = true,
                RemoveGoBackBookmark = true,
                ReplaceTabsWithSpaces = false,
            };
            MarkupSimplifier.SimplifyMarkup(wordDoc, simplifyMarkupSettings);

            FormattingAssemblerSettings formattingAssemblerSettings = new FormattingAssemblerSettings
            {
                RemoveStyleNamesFromParagraphAndRunProperties = false,
                ClearStyles = false,
                RestrictToSupportedLanguages = docxToHtmlSettings.RestrictToSupportedLanguages,
                RestrictToSupportedNumberingFormats = docxToHtmlSettings.RestrictToSupportedNumberingFormats,
                CreateHtmlConverterAnnotationAttributes = true,
                OrderElementsPerStandard = false,
                ListItemRetrieverSettings =
                    docxToHtmlSettings.ListItemImplementations == null ?
                    new ListItemRetrieverSettings()
                    {
                        ListItemTextImplementations = ListItemRetrieverSettings.DefaultListItemTextImplementations,
                    } :
                    new ListItemRetrieverSettings()
                    {
                        ListItemTextImplementations = docxToHtmlSettings.ListItemImplementations,
                    },
            };

            FormattingAssembler.AssembleFormatting(wordDoc, formattingAssemblerSettings);

            DocxToHtmlTransformMode txMode = new DocxToHtmlTransformMode()
            {
                RevTrackingMode = RevTrackingMode.None,
            };
            DocxToHtmlTransformState txState = new DocxToHtmlTransformState()
            {
                CommentContent = new List<object>()
            };

            InsertAppropriateNonbreakingSpaces(wordDoc);
            CalculateSpanWidthForTabs(wordDoc, txState);
            ReverseTableBordersForRtlTables(wordDoc);
            AdjustTableBorders(wordDoc);
            XElement rootElement = wordDoc.MainDocumentPart.GetXDocument().Root;
            FieldRetriever.AnnotateWithFieldInfo(wordDoc.MainDocumentPart);
            AnnotateForSections(wordDoc);

            XElement xhtml = (XElement)ConvertToHtmlTransform(wordDoc, docxToHtmlSettings,
                rootElement, false, 0m, txMode, txState);

            var body = xhtml.Descendants(Xhtml.body).FirstOrDefault();
            if (txState.CommentContent.Any())
            {
                body.Add(new XElement(Xhtml.div,
                    new XElement(Xhtml.hr),
                    txState.CommentContent,
                    new XElement(Xhtml.br)));
            }
            if (txState.FootNotes.Any())
            {
                body.Add(new XElement(Xhtml.div,
                    new XElement(Xhtml.hr),
                    txState.FootNotes,
                    new XElement(Xhtml.br)));
            }
            if (txState.EndNotes.Any())
            {
                body.Add(new XElement(Xhtml.div,
                    new XElement(Xhtml.hr),
                    txState.EndNotes,
                    new XElement(Xhtml.br)));
            }

            ReifyStylesAndClasses(docxToHtmlSettings, xhtml);

            // Note: the xhtml returned by ConvertToHtmlTransform contains objects of type
            // XEntity.  PtOpenXmlUtil.cs define the XEntity class.  See
            // http://blogs.msdn.com/ericwhite/archive/2010/01/21/writing-entity-references-using-linq-to-xml.aspx
            // for detailed explanation.
            //
            // If you further transform the XML tree returned by ConvertToHtmlTransform, you
            // must do it correctly, or entities will not be serialized properly.

            return xhtml;
        }

        private static void ReverseTableBordersForRtlTables(WordprocessingDocument wordDoc)
        {
            XDocument xd = wordDoc.MainDocumentPart.GetXDocument();
            foreach (var tbl in xd.Descendants(W.tbl))
            {
                var bidiVisual = tbl.Elements(W.tblPr).Elements(W.bidiVisual).FirstOrDefault();
                if (bidiVisual == null)
                    continue;

                var tblBorders = tbl.Elements(W.tblPr).Elements(W.tblBorders).FirstOrDefault();
                if (tblBorders != null)
                {
                    var left = tblBorders.Element(W.left);
                    if (left != null)
                        left = new XElement(W.right, left.Attributes());

                    var right = tblBorders.Element(W.right);
                    if (right != null)
                        right = new XElement(W.left, right.Attributes());

                    var newTblBorders = new XElement(W.tblBorders,
                        tblBorders.Element(W.top),
                        left,
                        tblBorders.Element(W.bottom),
                        right);
                    tblBorders.ReplaceWith(newTblBorders);
                }

                foreach (var tc in tbl.Elements(W.tr).Elements(W.tc))
                {
                    var tcBorders = tc.Elements(W.tcPr).Elements(W.tcBorders).FirstOrDefault();
                    if (tcBorders != null)
                    {
                        var left = tcBorders.Element(W.left);
                        if (left != null)
                            left = new XElement(W.right, left.Attributes());

                        var right = tcBorders.Element(W.right);
                        if (right != null)
                            right = new XElement(W.left, right.Attributes());

                        var newTcBorders = new XElement(W.tcBorders,
                            tcBorders.Element(W.top),
                            left,
                            tcBorders.Element(W.bottom),
                            right);
                        tcBorders.ReplaceWith(newTcBorders);
                    }
                }
            }
        }

        private static void ReifyStylesAndClasses(DocxToHtmlSettings docxToHtmlSettings, XElement xhtml)
        {
            if (docxToHtmlSettings.FabricateCssClasses)
            {
                var usedCssClassNames = new HashSet<string>();
                var elementsThatNeedClasses = xhtml
                    .DescendantsAndSelf()
                    .Select(d => new
                    {
                        Element = d,
                        Styles = d.Annotation<Dictionary<string, string>>(),
                    })
                    .Where(z => z.Styles != null);
                var augmented = elementsThatNeedClasses
                    .Select(p => new
                    {
                        p.Element,
                        p.Styles,
                        StylesString = p.Element.Name.LocalName + "|" + p.Styles.OrderBy(k => k.Key).Select(s => string.Format("{0}: {1};", s.Key, s.Value)).StringConcatenate(),
                    })
                    .GroupBy(p => p.StylesString)
                    .ToList();
                int classCounter = 1000000;
                var sb = new StringBuilder();
                sb.Append(Environment.NewLine);
                foreach (var grp in augmented)
                {
                    string classNameToUse;
                    var firstOne = grp.First();
                    var styles = firstOne.Styles;
                    if (styles.ContainsKey("PtStyleName"))
                    {
                        classNameToUse = docxToHtmlSettings.CssClassPrefix + styles["PtStyleName"];
                        if (usedCssClassNames.Contains(classNameToUse))
                        {
                            classNameToUse = docxToHtmlSettings.CssClassPrefix +
                                styles["PtStyleName"] + "-" +
                                classCounter.ToString().Substring(1);
                            classCounter++;
                        }
                    }
                    else
                    {
                        classNameToUse = docxToHtmlSettings.CssClassPrefix +
                            classCounter.ToString().Substring(1);
                        classCounter++;
                    }
                    usedCssClassNames.Add(classNameToUse);
                    sb.Append(firstOne.Element.Name.LocalName + "." + classNameToUse + " {" + Environment.NewLine);
                    foreach (var st in firstOne.Styles.Where(s => s.Key != "PtStyleName"))
                    {
                        var s = "    " + st.Key + ": " + st.Value + ";" + Environment.NewLine;
                        sb.Append(s);
                    }
                    sb.Append("}" + Environment.NewLine);
                    var classAtt = new XAttribute("class", classNameToUse);
                    foreach (var gc in grp)
                        gc.Element.Add(classAtt);
                }
                var styleValue = docxToHtmlSettings.GeneralCss + sb + docxToHtmlSettings.AdditionalCss + docxToHtmlSettings.OpenXmlPowerToolsSpecificCss;

                SetStyleElementValue(xhtml, styleValue);
            }
            else
            {
                // Previously, the h:style element was not added at this point. However,
                // at least the General CSS will contain important settings.
                SetStyleElementValue(xhtml, docxToHtmlSettings.GeneralCss + docxToHtmlSettings.AdditionalCss + docxToHtmlSettings.OpenXmlPowerToolsSpecificCss);

                foreach (var d in xhtml.DescendantsAndSelf())
                {
                    var style = d.Annotation<Dictionary<string, string>>();
                    if (style == null)
                        continue;
                    var styleValue =
                        style
                        .Where(p => p.Key != "PtStyleName")
                        .OrderBy(p => p.Key)
                        .Select(e => string.Format("{0}: {1};", e.Key, e.Value))
                        .StringConcatenate();
                    XAttribute st = new XAttribute("style", styleValue);
                    if (d.Attribute("style") != null)
                        d.Attribute("style").Value += styleValue;
                    else
                        d.Add(st);
                }
            }

            var elementsThatNeedClassesAdded = xhtml
                .DescendantsAndSelf()
                .Select(d => new
                {
                    Element = d,
                    Class = d.Annotation<CssClassAnnotation>(),
                })
                .Where(z => z.Class != null);

            foreach (var item in elementsThatNeedClassesAdded)
            {
                var element = item.Element;
                var existingClass = (string)element.Attribute("class");
                if (existingClass == null)
                    element.Add(new XAttribute("class", item.Class.Class));
                else
                    element.Attribute("class").Value = existingClass + " " + item.Class.Class;
            }

        }

        private static void SetStyleElementValue(XElement xhtml, string styleValue)
        {
            var styleElement = xhtml
                .Descendants(Xhtml.style)
                .FirstOrDefault();
            if (styleElement != null)
                styleElement.Value = styleValue;
            else
            {
                styleElement = new XElement(Xhtml.style, styleValue);
                var head = xhtml.Element(Xhtml.head);
                if (head != null)
                    head.Add(styleElement);
            }
        }

        private static object ConvertToHtmlTransform(WordprocessingDocument wordDoc,
            DocxToHtmlSettings settings, XNode node,
            bool suppressTrailingWhiteSpace,
            decimal currentMarginLeft,
            DocxToHtmlTransformMode txMode,
            DocxToHtmlTransformState txState)
        {
            var element = node as XElement;
            if (element == null) return null;

            // Transform the w:document element to the XHTML h:html element.
            // The h:head element is laid out based on the W3C's recommended layout, i.e.,
            // the charset (using the HTML5-compliant form), the title (which is always
            // there but possibly empty), and other meta tags.
            if (element.Name == W.document)
            {
                return new XElement(Xhtml.html,
                    new XElement(Xhtml.head,
                        new XElement(Xhtml.meta, new XAttribute("charset", "UTF-8")),
                        settings.PageTitle != null
                            ? new XElement(Xhtml.title, new XText(settings.PageTitle))
                            : new XElement(Xhtml.title, new XText(string.Empty)),
                        new XElement(Xhtml.meta,
                            new XAttribute("name", "Generator"),
                            new XAttribute("content", "PowerTools for Open XML"))),
                    element.Elements()
                        .Select(e => ConvertToHtmlTransform(wordDoc, settings, e, false, currentMarginLeft, txMode, txState)));
            }

            // Transform the w:body element to the XHTML h:body element.
            if (element.Name == W.body)
            {
                return new XElement(Xhtml.body, CreateSectionDivs(wordDoc, settings, element, txMode, txState));
            }

            // Transform the w:p element to the XHTML h:h1-h6 or h:p element (if the previous paragraph does not
            // have a style separator).
            if (element.Name == W.p)
            {
                return ProcessParagraph(wordDoc, settings, element, suppressTrailingWhiteSpace, currentMarginLeft, txMode, txState);
            }

            // Transform hyperlinks to the XHTML h:a element.
            if (element.Name == W.hyperlink && element.Attribute(R.id) != null)
            {
                try
                {
                    var hyperlinkRelationship = wordDoc.MainDocumentPart
                                .HyperlinkRelationships
                                .FirstOrDefault(x => x.Id == (string)element.Attribute(R.id));

                    if (hyperlinkRelationship == null)
                    {
                        var a2 = new XElement(Xhtml.a,
                            element.Elements(W.r).Select(run => ConvertRun(wordDoc, settings, run, txMode, txState)));
                        if (!a2.Nodes().Any())
                            a2.Add(new XText(""));
                        return a2;
                    }

                    var a = new XElement(Xhtml.a,
                        new XAttribute("href",
                            hyperlinkRelationship.Uri),
                        element.Elements(W.r).Select(run => ConvertRun(wordDoc, settings, run, txMode, txState)));
                    if (!a.Nodes().Any())
                        a.Add(new XText(""));
                    return a;
                }
                catch (UriFormatException)
                {
                    return element.Elements().Select(e => ConvertToHtmlTransform(wordDoc, settings, e, false, currentMarginLeft, txMode, txState));
                }
            }

            // Transform hyperlinks to bookmarks to the XHTML h:a element.
            if (element.Name == W.hyperlink && element.Attribute(W.anchor) != null)
            {
                return ProcessHyperlinkToBookmark(wordDoc, settings, element, txMode, txState);
            }

#if false
<w:del w:id="0"
       w:author="Eric White"
       w:date="2018-03-07T00:32:00Z">
  <w:r w:rsidDel="008970E6">
    <w:delText xml:space="preserve">deleted </w:delText>
  </w:r>
</w:del>
#endif
            // w:del that is a parent of a run, and child of a paragraph.
            if ((element.Name == W.del || element.Name == W.ins || element.Name == W.moveFrom || element.Name == W.moveTo) &&
                element.Element(W.r) != null)
            {
                return CreateContentWithToolTipForRevTracking(wordDoc, settings, element, txState);
            }

            if (element.Name == W.commentReference)
            {
                return CreateContentForComments(wordDoc, settings, element, suppressTrailingWhiteSpace, currentMarginLeft, txMode, txState);
            }

            if (element.Name == W.footnoteReference || element.Name == W.endnoteReference)
            {
                return CreateContentForFootAndEndNotes(wordDoc, settings, element, suppressTrailingWhiteSpace, currentMarginLeft, txMode, txState);
            }

            // Transform contents of runs.
            if (element.Name == W.r)
            {
                return ConvertRun(wordDoc, settings, element, txMode, txState);
            }

            // Transform w:bookmarkStart into anchor
            if (element.Name == W.bookmarkStart)
            {
                return ProcessBookmarkStart(element);
            }

            // Transform every w:t element to a text node.
            if (element.Name == W.t)
            {
                // We don't need to convert characters to entities in a UTF-8 document.
                // Further, we don't need &nbsp; entities for significant whitespace
                // because we are wrapping the text nodes in <span> elements within
                // which all whitespace is significant.
                return new XText(element.Value);
            }

            // Transform every w:delText element to a text node.
            if (element.Name == W.delText)
            {
                return new XText(element.Value);
            }

            // Transform symbols to spans
            if (element.Name == W.sym)
            {
                var cs = (string)element.Attribute(W._char);
                var c = Convert.ToInt32(cs, 16);
                return new XElement(Xhtml.span, new XEntity(string.Format("#{0}", c)));
            }

            // Transform tabs that have the pt:TabWidth attribute set
            if (element.Name == W.tab)
            {
                return ProcessTab(element, txState);
            }

            // Transform w:br to h:br.
            if (element.Name == W.br || element.Name == W.cr)
            {
                return ProcessBreak(element);
            }

            // Transform w:noBreakHyphen to '-'
            if (element.Name == W.noBreakHyphen)
            {
                return new XText("-");
            }

            // Transform w:tbl to h:tbl.
            if (element.Name == W.tbl)
            {
                return ProcessTable(wordDoc, settings, element, currentMarginLeft, txMode, txState);
            }

            // Transform w:tr to h:tr.
            if (element.Name == W.tr)
            {
                return ProcessTableRow(wordDoc, settings, element, currentMarginLeft, txMode, txState);
            }

            // Transform w:tc to h:td.
            if (element.Name == W.tc)
            {
                return ProcessTableCell(wordDoc, settings, element, txMode, txState);
            }

            // Transform images
            if (element.Name == W.drawing || element.Name == W.pict || element.Name == W._object)
            {
                return ProcessImage(wordDoc, element, settings.ImageHandler);
            }

            // Transform content controls.
            if (element.Name == W.sdt)
            {
                return ProcessContentControl(wordDoc, settings, element, currentMarginLeft, txMode, txState);
            }

            // Transform smart tags and simple fields.
            if (element.Name == W.smartTag || element.Name == W.fldSimple)
            {
                return CreateBorderDivs(wordDoc, settings, element.Elements(), txMode, txState);
            }

            // Ignore element.
            return null;
        }

        private static object CreateContentForFootAndEndNotes(WordprocessingDocument wordDoc, DocxToHtmlSettings settings, XElement element,
            bool suppressTrailingWhiteSpace, decimal currentMarginLeft, DocxToHtmlTransformMode txMode, DocxToHtmlTransformState txState)
        {
            var id = (string)element.Attribute(W.id);
            if (id != null)
            {
                OpenXmlPart thePart = null;
                if (element.Name == W.footnoteReference)
                    thePart = wordDoc.MainDocumentPart.FootnotesPart;
                else
                    thePart = wordDoc.MainDocumentPart.EndnotesPart;
                if (thePart != null)
                {
                    var xDoc = thePart.GetXDocument();
                    XName xNameToFind = null;
                    if (element.Name == W.footnoteReference)
                        xNameToFind = W.footnote;
                    else
                        xNameToFind = W.endnote;
                    var note = xDoc.Root.Elements(xNameToFind).FirstOrDefault(c => (string)c.Attribute(W.id) == id);
                    if (note != null)
                    {
                        var noteId = (string)note.Attribute(W.id);
                        if (noteId != null)
                        {
                            string topAnchor;
                            string bottomAnchor;
                            if (element.Name == W.footnoteReference)
                            {
                                topAnchor = "_OxPt_FootNote_Link_" + noteId;
                                bottomAnchor = "_OxPt_FootNote_" + noteId;
                            }
                            else
                            {
                                topAnchor = "_OxPt_EndNote_Link_" + noteId;
                                bottomAnchor = "_OxPt_EndNote_" + noteId;
                            }

                            var noteHyperlink = new XElement(Xhtml.a,
                                new XAttribute("class", "OxPt-Comment-Initials"),
                                new XAttribute("href", "#" + bottomAnchor),
                                new XAttribute("name", topAnchor),
                                new XText("[" + noteId + "]"));

                            var hyperlinkBack = new XElement(Xhtml.a,
                                new XAttribute("class", "OxPt-Comment-Initials"),
                                new XAttribute("href", "#" + topAnchor),
                                new XAttribute("name", bottomAnchor),
                                new XText("[" + noteId + "]"));

                            var noteContent = (new[] { hyperlinkBack }).Concat(
                                note
                                .Elements()
                                .Select(p => ConvertToHtmlTransform(wordDoc,
                                    settings, p,
                                    suppressTrailingWhiteSpace,
                                    currentMarginLeft,
                                    txMode,
                                    txState)));

                            if (element.Name == W.footnoteReference)
                            {
                                txState.FootNotes.Add(noteContent);
                            }
                            else
                            {
                                txState.EndNotes.Add(noteContent);
                            }

                            return noteHyperlink;
                        }
                    }
                }
            }

            return null;
        }

        private static object CreateContentForComments(WordprocessingDocument wordDoc, DocxToHtmlSettings settings, XElement element,
            bool suppressTrailingWhiteSpace, decimal currentMarginLeft, DocxToHtmlTransformMode txMode, DocxToHtmlTransformState txState)
        {
            var id = (string)element.Attribute(W.id);
            if (id != null)
            {
                var commentsPart = wordDoc.MainDocumentPart.WordprocessingCommentsPart;

                if (commentsPart != null)
                {
                    var xDoc = commentsPart.GetXDocument();
                    var comment = xDoc.Root.Elements(W.comment).FirstOrDefault(c => (string)c.Attribute(W.id) == id);
                    if (comment != null)
                    {
                        var author = (string)comment.Attribute(W.author);
                        var dateTimeRawString = (string)comment.Attribute(W.date);
                        var initials = (string)comment.Attribute(W.initials);
                        if (initials != null)
                        {
                            var commentInitials = initials;
                            commentInitials += txState.NextCommentNumber.ToString();
                            txState.NextCommentNumber++;

                            DateTime dateTime = DateTime.Parse(dateTimeRawString);
                            var dateTimeString = dateTime.ToString();
                            var toolTipText = author + " - " + dateTimeString;

                            if (settings.DisplayCommentsAsBalloon)
                            {
                                var commentText = comment
                                    .Elements(W.p)
                                    .Select(p => new object[] { new XElement("br"), p.Descendants(W.t).Select(t => (string)t) });

                                var dummy = new XElement("dummy", toolTipText, commentText);
                                var dummyStr = dummy.Value;

                                XElement toolTipSpan = new XElement(Xhtml.span,
                                    new XAttribute("class", "OxPt-RevTrk OxPt-ToolTip-Right"),
                                    GetNoSpaceAttribute(dummyStr),
                                    toolTipText,
                                    commentText);

                                return new XElement(Xhtml.span,
                                    new XAttribute("class", "OxPt-Comment-Initials OxPt-ToolTip"),
                                    GetNoSpaceAttribute(dummyStr),
                                    new XText("[" + commentInitials + "]"),
                                    toolTipSpan);
                            }
                            else
                            {
                                var topAnchor = "_OxPt_Comment_Link_" + txState.NextCommentNumber.ToString();
                                var bottomAnchor = "_OxPt_Comment_" + txState.NextCommentNumber.ToString();
                                txState.NextCommentNumber++;

                                var commentHyperlink = new XElement(Xhtml.a,
                                    new XAttribute("class", "OxPt-Comment-Initials"),
                                    new XAttribute("href", "#" + bottomAnchor),
                                    new XAttribute("name", topAnchor),
                                    new XText("[" + commentInitials + "]"));

                                var hyperlinkBack = new XElement(Xhtml.a,
                                    new XAttribute("class", "OxPt-Comment-Initials"),
                                    new XAttribute("href", "#" + topAnchor),
                                    new XAttribute("name", bottomAnchor),
                                    new XText("[" + commentInitials + "]"));

                                var commentContent = (new[] { hyperlinkBack }).Concat(
                                    comment
                                    .Elements()
                                    .Select(p => ConvertToHtmlTransform(wordDoc,
                                        settings, p,
                                        suppressTrailingWhiteSpace,
                                        currentMarginLeft,
                                        txMode,
                                        txState)));

                                txState.CommentContent.Add(commentContent);

                                return commentHyperlink;
                            }
                        }
                    }
                }
            }

            return null;
        }

        private static XAttribute GetNoSpaceAttribute(string p)
        {
            if (p.Length > 1 && (char.IsWhiteSpace(p[0]) || char.IsWhiteSpace(p[p.Length - 1])))
                return new XAttribute(XNamespace.Xml + "space", "preserve");
            return null;
        }

        private static object CreateContentWithToolTipForRevTracking(WordprocessingDocument wordDoc, DocxToHtmlSettings settings, XElement element, DocxToHtmlTransformState txState)
        {
            DocxToHtmlTransformMode newTxMode = null;

            var author = (string)element.Attribute(W.author);
            if (author == null)
            {
                if (element.Name == W.ins)
                    newTxMode = new DocxToHtmlTransformMode() { RevTrackingMode = RevTrackingMode.Insert };
                else if (element.Name == W.del)
                    newTxMode = new DocxToHtmlTransformMode() { RevTrackingMode = RevTrackingMode.Delete };
                else if (element.Name == W.moveFrom)
                    newTxMode = new DocxToHtmlTransformMode() { RevTrackingMode = RevTrackingMode.MoveFrom };
                else if (element.Name == W.moveTo)
                    newTxMode = new DocxToHtmlTransformMode() { RevTrackingMode = RevTrackingMode.MoveTo };
            }
            else
            {
                var dateTimeRawString = (string)element.Attribute(W.date);
                string dateTimeString;
                if (dateTimeRawString != null)
                {
                    DateTime dateTime = DateTime.Parse(dateTimeRawString);
                    dateTimeString = dateTime.ToString();
                }
                else
                    dateTimeString = "";
                string toolTipText = "";
                if (element.Name == W.ins)
                {
                    toolTipText = author + " - " + dateTimeString + " - Inserted";
                    newTxMode = new DocxToHtmlTransformMode()
                    {
                        RevTrackingMode = RevTrackingMode.Insert,
                        ToolTipText = toolTipText,
                        ToolTipTextClass = "OxPt-RevTrk OxPt-ToolTip-Right",
                        AdditionalClass = "OxPt-ToolTip",
                    };
                }
                else if (element.Name == W.del)
                {
                    toolTipText = author + " - " + dateTimeString + " - Deleted";
                    newTxMode = new DocxToHtmlTransformMode()
                    {
                        RevTrackingMode = RevTrackingMode.Delete,
                        ToolTipText = toolTipText,
                        ToolTipTextClass = "OxPt-RevTrk OxPt-ToolTip-Right",
                        AdditionalClass = "OxPt-ToolTip",
                    };
                }
                else if (element.Name == W.moveFrom)
                {
                    toolTipText = author + " - " + dateTimeString + " - Move from";
                    newTxMode = new DocxToHtmlTransformMode()
                    {
                        RevTrackingMode = RevTrackingMode.MoveFrom,
                        ToolTipText = toolTipText,
                        ToolTipTextClass = "OxPt-RevTrk OxPt-ToolTip-Right",
                        AdditionalClass = "OxPt-ToolTip",
                    };
                }
                else if (element.Name == W.moveTo)
                {
                    toolTipText = author + " - " + dateTimeString + " - Move to";
                    newTxMode = new DocxToHtmlTransformMode()
                    {
                        RevTrackingMode = RevTrackingMode.MoveTo,
                        ToolTipText = toolTipText,
                        ToolTipTextClass = "OxPt-RevTrk OxPt-ToolTip-Right",
                        AdditionalClass = "OxPt-ToolTip",
                    };
                }

            }

            var contents = element
                .Elements()
                .Select(e => ConvertRun(wordDoc, settings, e, newTxMode, txState));
            return contents;
        }

        private static object ProcessHyperlinkToBookmark(WordprocessingDocument wordDoc, DocxToHtmlSettings settings, XElement element,
            DocxToHtmlTransformMode txMode, DocxToHtmlTransformState txState)
        {
            var style = new Dictionary<string, string>();
            var a = new XElement(Xhtml.a,
                new XAttribute("href", "#" + (string)element.Attribute(W.anchor)),
                element.Elements(W.r).Select(run => ConvertRun(wordDoc, settings, run, txMode, txState)));
            if (!a.Nodes().Any())
                a.Add(new XText(""));
            style.Add("text-decoration", "none");
            a.AddAnnotation(style);
            return a;
        }

        private static object ProcessBookmarkStart(XElement element)
        {
            var name = (string)element.Attribute(W.name);
            if (name == null) return null;

            var style = new Dictionary<string, string>();
            var a = new XElement(Xhtml.a,
                new XAttribute("id", name),
                new XText(""));
            if (!a.Nodes().Any())
                a.Add(new XText(""));
            style.Add("text-decoration", "none");
            a.AddAnnotation(style);
            return a;
        }

        private static object ProcessTab(XElement element, DocxToHtmlTransformState txState)
        {
            var tabWidthAtt = element.Attribute(PtOpenXml.TabWidth);
            if (tabWidthAtt == null) return null;

            var leader = (string)element.Attribute(PtOpenXml.Leader);
            var tabWidth = (decimal)tabWidthAtt;
            var style = new Dictionary<string, string>();
            XElement span;
            if (leader != null)
            {
                var leaderChar = ".";
                if (leader == "hyphen")
                    leaderChar = "-";
                else if (leader == "dot")
                    leaderChar = ".";
                else if (leader == "underscore")
                    leaderChar = "_";

                var runContainingTabToReplace = element.Ancestors(W.r).First();
                var fontNameAtt = runContainingTabToReplace.Attribute(PtOpenXml.pt + "FontName") ??
                                  runContainingTabToReplace.Ancestors(W.p).First().Attribute(PtOpenXml.pt + "FontName");

                var dummyRun = new XElement(W.r,
                    fontNameAtt,
                    runContainingTabToReplace.Elements(W.rPr),
                    new XElement(W.t, leaderChar));

                InitializeStateGraphics(txState);
                var widthOfLeaderChar = FontMetrics.CalcWidthOfRunInTwips(dummyRun, txState.Graphics);

                bool forceArial = false;
                if (widthOfLeaderChar == 0)
                {
                    dummyRun = new XElement(W.r,
                        new XAttribute(PtOpenXml.FontName, "Arial"),
                        runContainingTabToReplace.Elements(W.rPr),
                        new XElement(W.t, leaderChar));
                    InitializeStateGraphics(txState);
                    widthOfLeaderChar = FontMetrics.CalcWidthOfRunInTwips(dummyRun, txState.Graphics);
                    forceArial = true;
                }

                if (widthOfLeaderChar != 0)
                {
                    var numberOfLeaderChars = (int)(Math.Floor((tabWidth * 1440) / widthOfLeaderChar));
                    if (numberOfLeaderChars < 0)
                        numberOfLeaderChars = 0;
                    span = new XElement(Xhtml.span,
                        new XAttribute(XNamespace.Xml + "space", "preserve"),
                        " " + "".PadRight(numberOfLeaderChars, leaderChar[0]) + " ");
                    style.Add("margin", "0 0 0 0");
                    style.Add("padding", "0 0 0 0");
                    style.Add("width", string.Format(NumberFormatInfo.InvariantInfo, "{0:0.00}in", tabWidth));
                    style.Add("text-align", "center");
                    if (forceArial)
                        style.Add("font-family", "Arial");
                }
                else
                {
                    span = new XElement(Xhtml.span, new XAttribute(XNamespace.Xml + "space", "preserve"), " ");
                    style.Add("margin", "0 0 0 0");
                    style.Add("padding", "0 0 0 0");
                    style.Add("width", string.Format(NumberFormatInfo.InvariantInfo, "{0:0.00}in", tabWidth));
                    style.Add("text-align", "center");
                    if (leader == "underscore")
                    {
                        style.Add("text-decoration", "underline");
                    }
                }
            }
            else
            {
#if false
                            var bidi = element
                                .Ancestors(W.p)
                                .Take(1)
                                .Elements(W.pPr)
                                .Elements(W.bidi)
                                .Where(b => b.Attribute(W.val) == null || b.Attribute(W.val).ToBoolean() == true)
                                .FirstOrDefault();
                            var isBidi = bidi != null;
                            if (isBidi)
                                span = new XElement(Xhtml.span, new XEntity("#x200f")); // RLM
                            else
                                span = new XElement(Xhtml.span, new XEntity("#x200e")); // LRM
#else
                span = new XElement(Xhtml.span, new XEntity("#x00a0"));
#endif
                style.Add("margin", string.Format(NumberFormatInfo.InvariantInfo, "0 0 0 {0:0.00}in", tabWidth));
                style.Add("padding", "0 0 0 0");
            }
            span.AddAnnotation(style);
            return span;
        }

        private static void InitializeStateGraphics(DocxToHtmlTransformState txState)
        {
            if (txState.Graphics != null)
                return;
            Bitmap bmp = new Bitmap(100, 100);
            txState.Graphics = Graphics.FromImage(bmp);
        }


        private static object ProcessBreak(XElement element)
        {
            XElement span = null;
            var tabWidth = (decimal?)element.Attribute(PtOpenXml.TabWidth);
            if (tabWidth != null)
            {
                span = new XElement(Xhtml.span);
                span.AddAnnotation(new Dictionary<string, string>
                {
                    { "margin", string.Format(NumberFormatInfo.InvariantInfo, "0 0 0 {0:0.00}in", tabWidth) },
                    { "padding", "0 0 0 0" }
                });
            }

            var paragraph = element.Ancestors(W.p).FirstOrDefault();
            var isBidi = paragraph != null &&
                         paragraph.Elements(W.pPr).Elements(W.bidi).Any(b => b.Attribute(W.val) == null ||
                                                                             b.Attribute(W.val).ToBoolean() == true);
            var zeroWidthChar = isBidi ? new XEntity("#x200f") : new XEntity("#x200e");

            return new object[]
            {
                new XElement(Xhtml.br),
                zeroWidthChar,
                span,
            };
        }

        private static object ProcessContentControl(WordprocessingDocument wordDoc, DocxToHtmlSettings settings,
            XElement element, decimal currentMarginLeft, DocxToHtmlTransformMode txMode, DocxToHtmlTransformState txState)
        {
            var relevantAncestors = element.Ancestors().TakeWhile(a => a.Name != W.txbxContent);
            var isRunLevelContentControl = relevantAncestors.Any(a => a.Name == W.p);
            if (isRunLevelContentControl)
            {
                return element.Elements(W.sdtContent).Elements()
                    .Select(e => ConvertToHtmlTransform(wordDoc, settings, e, false, currentMarginLeft, txMode, txState))
                    .ToList();
            }
            return CreateBorderDivs(wordDoc, settings, element.Elements(W.sdtContent).Elements(), txMode, txState);
        }

        // Transform the w:p element, including the following sibling w:p element(s)
        // in case the w:p element has a style separator. The sibling(s) will be
        // transformed to h:span elements rather than h:p elements and added to
        // the element (e.g., h:h2) created from the w:p element having the (first)
        // style separator (i.e., a w:specVanish element).
        private static object ProcessParagraph(WordprocessingDocument wordDoc, DocxToHtmlSettings settings,
            XElement element, bool suppressTrailingWhiteSpace, decimal currentMarginLeft, DocxToHtmlTransformMode txMode, DocxToHtmlTransformState txState)
        {
            // Ignore this paragraph if the previous paragraph has a style separator.
            // We have already transformed this one together with the previous one.
            var previousParagraph = element.ElementsBeforeSelf(W.p).LastOrDefault();
            if (HasStyleSeparator(previousParagraph)) return null;

            var elementName = GetParagraphElementName(element, wordDoc);
            var isBidi = IsBidi(element);
            var paragraph = (XElement)ConvertParagraph(wordDoc, settings, element, elementName,
                suppressTrailingWhiteSpace, currentMarginLeft, isBidi, txMode, txState);

            // The paragraph conversion might have created empty spans.
            // These can and should be removed because empty spans are
            // invalid in HTML5.
            paragraph.Elements(Xhtml.span).Where(e => e.IsEmpty).Remove();

            foreach (var span in paragraph.Elements(Xhtml.span).ToList())
            {
                var v = span.Value;
                if (v.Length > 0 && (char.IsWhiteSpace(v[0]) || char.IsWhiteSpace(v[v.Length - 1])) && span.Attribute(XNamespace.Xml + "space") == null)
                    span.Add(new XAttribute(XNamespace.Xml + "space", "preserve"));
            }

            while (HasStyleSeparator(element))
            {
                element = element.ElementsAfterSelf(W.p).FirstOrDefault();
                if (element == null) break;

                elementName = Xhtml.span;
                isBidi = IsBidi(element);
                var span = (XElement)ConvertParagraph(wordDoc, settings, element, elementName,
                    suppressTrailingWhiteSpace, currentMarginLeft, isBidi, txMode, txState);
                var v = span.Value;
                if (v.Length > 0 && (char.IsWhiteSpace(v[0]) || char.IsWhiteSpace(v[v.Length - 1])) && span.Attribute(XNamespace.Xml + "space") == null)
                    span.Add(new XAttribute(XNamespace.Xml + "space", "preserve"));
                paragraph.Add(span);
            }

            return paragraph;
        }

        private static object ProcessTable(WordprocessingDocument wordDoc, DocxToHtmlSettings settings, XElement element, decimal currentMarginLeft,
            DocxToHtmlTransformMode txMode, DocxToHtmlTransformState txState)
        {
            var style = new Dictionary<string, string>();
            style.AddIfMissing("border-collapse", "collapse");
            style.AddIfMissing("border", "none");
            var bidiVisual = element.Elements(W.tblPr).Elements(W.bidiVisual).FirstOrDefault();
            var tblW = element.Elements(W.tblPr).Elements(W.tblW).FirstOrDefault();
            if (tblW != null)
            {
                var type = (string)tblW.Attribute(W.type);
                if (type != null && type == "pct")
                {
                    var w = (int)tblW.Attribute(W._w);
                    style.AddIfMissing("width", (w / 50) + "%");
                }
            }
            var tblInd = element.Elements(W.tblPr).Elements(W.tblInd).FirstOrDefault();
            if (tblInd != null)
            {
                var tblIndType = (string)tblInd.Attribute(W.type);
                if (tblIndType != null)
                {
                    if (tblIndType == "dxa")
                    {
                        var width = (decimal?)tblInd.Attribute(W._w);
                        if (width != null)
                        {
                            style.AddIfMissing("margin-left",
                                width > 0m
                                    ? string.Format(NumberFormatInfo.InvariantInfo, "{0}pt", width / 20m)
                                    : "0");
                        }
                    }
                }
            }
            var tableDirection = bidiVisual != null ? new XAttribute("dir", "rtl") : new XAttribute("dir", "ltr");
            style.AddIfMissing("margin-bottom", ".001pt");
            var table = new XElement(Xhtml.table,
                // TODO: Revisit and make sure the omission is covered by appropriate CSS.
                // new XAttribute("border", "1"),
                // new XAttribute("cellspacing", 0),
                // new XAttribute("cellpadding", 0),
                tableDirection,
                element.Elements().Select(e => ConvertToHtmlTransform(wordDoc, settings, e, false, currentMarginLeft, txMode, txState)));
            table.AddAnnotation(style);
            var jc = (string)element.Elements(W.tblPr).Elements(W.jc).Attributes(W.val).FirstOrDefault() ?? "left";
            XAttribute dir = null;
            XAttribute jcToUse = null;
            if (bidiVisual != null)
            {
                dir = new XAttribute("dir", "rtl");
                if (jc == "left")
                    jcToUse = new XAttribute("align", "right");
                else if (jc == "right")
                    jcToUse = new XAttribute("align", "left");
                else if (jc == "center")
                    jcToUse = new XAttribute("align", "center");
            }
            else
            {
                jcToUse = new XAttribute("align", jc);
            }
            var tableDiv = new XElement(Xhtml.div,
                dir,
                jcToUse,
                table);
            return tableDiv;
        }

        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        private static object ProcessTableCell(WordprocessingDocument wordDoc, DocxToHtmlSettings settings, XElement element, DocxToHtmlTransformMode txMode, DocxToHtmlTransformState txState)
        {
            var style = new Dictionary<string, string>();
            XAttribute colSpan = null;
            XAttribute rowSpan = null;

            var tcPr = element.Element(W.tcPr);
            if (tcPr != null)
            {
                if ((string)tcPr.Elements(W.vMerge).Attributes(W.val).FirstOrDefault() == "restart")
                {
                    var currentRow = element.Parent.ElementsBeforeSelf(W.tr).Count();
                    var currentCell = element.ElementsBeforeSelf(W.tc).Count();
                    var tbl = element.Parent.Parent;
                    int rowSpanCount = 1;
                    currentRow += 1;
                    while (true)
                    {
                        var row = tbl.Elements(W.tr).Skip(currentRow).FirstOrDefault();
                        if (row == null)
                            break;
                        var cell2 = row.Elements(W.tc).Skip(currentCell).FirstOrDefault();
                        if (cell2 == null)
                            break;
                        if (cell2.Elements(W.tcPr).Elements(W.vMerge).FirstOrDefault() == null)
                            break;
                        if ((string)cell2.Elements(W.tcPr).Elements(W.vMerge).Attributes(W.val).FirstOrDefault() == "restart")
                            break;
                        currentRow += 1;
                        rowSpanCount += 1;
                    }
                    rowSpan = new XAttribute("rowspan", rowSpanCount);
                }

                if (tcPr.Element(W.vMerge) != null &&
                    (string)tcPr.Elements(W.vMerge).Attributes(W.val).FirstOrDefault() != "restart")
                    return null;

                if (tcPr.Element(W.vAlign) != null)
                {
                    var vAlignVal = (string)tcPr.Elements(W.vAlign).Attributes(W.val).FirstOrDefault();
                    if (vAlignVal == "top")
                        style.AddIfMissing("vertical-align", "top");
                    else if (vAlignVal == "center")
                        style.AddIfMissing("vertical-align", "middle");
                    else if (vAlignVal == "bottom")
                        style.AddIfMissing("vertical-align", "bottom");
                    else
                        style.AddIfMissing("vertical-align", "middle");
                }
                style.AddIfMissing("vertical-align", "top");

                if ((string)tcPr.Elements(W.tcW).Attributes(W.type).FirstOrDefault() == "dxa")
                {
                    decimal width = (int)tcPr.Elements(W.tcW).Attributes(W._w).FirstOrDefault();
                    style.AddIfMissing("width", string.Format(NumberFormatInfo.InvariantInfo, "{0}pt", width / 20m));
                }
                if ((string)tcPr.Elements(W.tcW).Attributes(W.type).FirstOrDefault() == "pct")
                {
                    decimal width = (int)tcPr.Elements(W.tcW).Attributes(W._w).FirstOrDefault();
                    style.AddIfMissing("width", string.Format(NumberFormatInfo.InvariantInfo, "{0:0.0}%", width / 50m));
                }

                var tcBorders = tcPr.Element(W.tcBorders);
                GenerateBorderStyle(tcBorders, W.top, style, BorderType.Cell);
                GenerateBorderStyle(tcBorders, W.right, style, BorderType.Cell);
                GenerateBorderStyle(tcBorders, W.bottom, style, BorderType.Cell);
                GenerateBorderStyle(tcBorders, W.left, style, BorderType.Cell);

                CreateStyleFromShd(style, tcPr.Element(W.shd));

                var gridSpan = tcPr.Elements(W.gridSpan).Attributes(W.val).Select(a => (int?)a).FirstOrDefault();
                if (gridSpan != null)
                    colSpan = new XAttribute("colspan", (int)gridSpan);
            }
            style.AddIfMissing("padding-top", "0");
            style.AddIfMissing("padding-bottom", "0");

            var cell = new XElement(Xhtml.td,
                rowSpan,
                colSpan,
                CreateBorderDivs(wordDoc, settings, element.Elements(), txMode, txState));
            cell.AddAnnotation(style);
            return cell;
        }

        private static object ProcessTableRow(WordprocessingDocument wordDoc, DocxToHtmlSettings settings, XElement element,
            decimal currentMarginLeft, DocxToHtmlTransformMode txMode, DocxToHtmlTransformState txState)
        {
            var style = new Dictionary<string, string>();
            int? trHeight = (int?)element.Elements(W.trPr).Elements(W.trHeight).Attributes(W.val).FirstOrDefault();
            if (trHeight != null)
                style.AddIfMissing("height",
                    string.Format(NumberFormatInfo.InvariantInfo, "{0:0.00}in", (decimal)trHeight / 1440m));
            var htmlRow = new XElement(Xhtml.tr,
                element.Elements().Select(e => ConvertToHtmlTransform(wordDoc, settings, e, false, currentMarginLeft, txMode, txState)));
            if (style.Any())
                htmlRow.AddAnnotation(style);
            return htmlRow;
        }

        private static bool HasStyleSeparator(XElement element)
        {
            return element != null && element.Elements(W.pPr).Elements(W.rPr).Any(e => GetBoolProp(e, W.specVanish));
        }

        private static bool IsBidi(XElement element)
        {
            return element
                .Elements(W.pPr)
                .Elements(W.bidi)
                .Any(b => b.Attribute(W.val) == null || b.Attribute(W.val).ToBoolean() == true);
        }

        private static XName GetParagraphElementName(XElement element, WordprocessingDocument wordDoc)
        {
            var elementName = Xhtml.p;

            var styleId = (string)element.Elements(W.pPr).Elements(W.pStyle).Attributes(W.val).FirstOrDefault();
            if (styleId == null) return elementName;

            var style = GetStyle(styleId, wordDoc);
            if (style == null) return elementName;

            var outlineLevel =
                (int?)style.Elements(W.pPr).Elements(W.outlineLvl).Attributes(W.val).FirstOrDefault();
            if (outlineLevel != null && outlineLevel <= 5)
            {
                elementName = Xhtml.xhtml + string.Format("h{0}", outlineLevel + 1);
            }

            return elementName;
        }

        private static XElement GetStyle(string styleId, WordprocessingDocument wordDoc)
        {
            var stylesPart = wordDoc.MainDocumentPart.StyleDefinitionsPart;
            if (stylesPart == null) return null;

            var styles = stylesPart.GetXDocument().Root;
            return styles != null
                ? styles.Elements(W.style).FirstOrDefault(s => (string)s.Attribute(W.styleId) == styleId)
                : null;
        }

        private static object CreateSectionDivs(WordprocessingDocument wordDoc, DocxToHtmlSettings settings, XElement element, DocxToHtmlTransformMode txMode, DocxToHtmlTransformState txState)
        {
            // note: when building a paging html converter, need to attend to new sections with page breaks here.
            // This code conflates adjacent sections if they have identical formatting, which is not an issue
            // for the non-paging transform.
            var groupedIntoDivs = element
                .Elements()
                .GroupAdjacent(e =>
                {
                    var sectAnnotation = e.Annotation<SectionAnnotation>();
                    return sectAnnotation != null ? sectAnnotation.SectionElement.ToString() : "";
                });

            // note: when creating a paging html converter, need to pay attention to w:rtlGutter element.
            var divList = groupedIntoDivs
                .Select(g =>
                {
                    var sectPr = g.First().Annotation<SectionAnnotation>();
                    XElement bidi = null;
                    if (sectPr != null)
                    {
                        bidi = sectPr
                            .SectionElement
                            .Elements(W.bidi)
                            .FirstOrDefault(b => b.Attribute(W.val) == null || b.Attribute(W.val).ToBoolean() == true);
                    }
                    if (sectPr == null || bidi == null)
                    {
                        var div = new XElement(Xhtml.div, CreateBorderDivs(wordDoc, settings, g, txMode, txState));
                        return div;
                    }
                    else
                    {
                        var div = new XElement(Xhtml.div,
                            new XAttribute("dir", "rtl"),
                            CreateBorderDivs(wordDoc, settings, g, txMode, txState));
                        return div;
                    }
                });
            return divList;
        }

        private enum BorderType
        {
            Paragraph,
            Cell,
        };

        /*
         * Notes on line spacing
         *
         * the w:line and w:lineRule attributes control spacing between lines - including between lines within a paragraph
         *
         * If w:spacing w:lineRule="auto" then
         *   w:spacing w:line is a percentage where 240 == 100%
         *
         *   (line value / 240) * 100 = percentage of line
         *
         * If w:spacing w:lineRule="exact" or w:lineRule="atLeast" then
         *   w:spacing w:line is in twips
         *   1440 = exactly one inch from line to line
         *
         * Handle
         * - ind
         * - jc
         * - numPr
         * - pBdr
         * - shd
         * - spacing
         * - textAlignment
         *
         * Don't Handle (yet)
         * - adjustRightInd?
         * - autoSpaceDE
         * - autoSpaceDN
         * - bidi
         * - contextualSpacing
         * - divId
         * - framePr
         * - keepLines
         * - keepNext
         * - kinsoku
         * - mirrorIndents
         * - overflowPunct
         * - pageBreakBefore
         * - snapToGrid
         * - suppressAutoHyphens
         * - suppressLineNumbers
         * - suppressOverlap
         * - tabs
         * - textBoxTightWrap
         * - textDirection
         * - topLinePunct
         * - widowControl
         * - wordWrap
         *
         */

        private static object ConvertParagraph(WordprocessingDocument wordDoc, DocxToHtmlSettings settings,
            XElement paragraph, XName elementName, bool suppressTrailingWhiteSpace, decimal currentMarginLeft, bool isBidi, DocxToHtmlTransformMode txMode, DocxToHtmlTransformState txState)
        {
            var style = DefineParagraphStyle(paragraph, elementName, suppressTrailingWhiteSpace, currentMarginLeft, isBidi);
            var rtl = isBidi ? new XAttribute("dir", "rtl") : new XAttribute("dir", "ltr");
            var firstMark = isBidi ? new XEntity("#x200f") : null;

            // Analyze initial runs to see whether we have a tab, in which case we will render
            // a span with a defined width and ignore the tab rather than rendering the text
            // preceding the tab and the tab as a span with a computed width.
            var firstTabRun = paragraph
                .Elements(W.r)
                .FirstOrDefault(run => run.Elements(W.tab).Any());
            var elementsPrecedingTab = firstTabRun != null
                ? paragraph.Elements(W.r).TakeWhile(e => e != firstTabRun)
                    .Where(e => e.Elements().Any(c => c.Attributes(PtOpenXml.TabWidth).Any())).ToList()
                : Enumerable.Empty<XElement>().ToList();

            // TODO: Revisit
            // For the time being, if a hyperlink field precedes the tab, we'll render it as before.
            var hyperlinkPrecedesTab = elementsPrecedingTab
                .Elements(W.r)
                .Elements(W.instrText)
                .Select(e => e.Value)
                .Any(value => value != null && value.TrimStart().ToUpper().StartsWith("HYPERLINK"));
            if (hyperlinkPrecedesTab)
            {
                var paraElement1 = new XElement(elementName,
                    rtl,
                    firstMark,
                    ConvertContentThatCanContainFields(wordDoc, settings, paragraph.Elements(), txMode, txState));
                paraElement1.AddAnnotation(style);
                return paraElement1;
            }

            var txElementsPrecedingTab = TransformElementsPrecedingTab(wordDoc, settings, elementsPrecedingTab, firstTabRun, txMode, txState);
            var elementsSucceedingTab = firstTabRun != null
                ? paragraph.Elements().SkipWhile(e => e != firstTabRun).Skip(1)
                : paragraph.Elements();
            var paraElement = new XElement(elementName,
                rtl,
                firstMark,
                txElementsPrecedingTab,
                ConvertContentThatCanContainFields(wordDoc, settings, elementsSucceedingTab, txMode, txState));
            paraElement.AddAnnotation(style);

            return paraElement;
        }

        private static List<object> TransformElementsPrecedingTab(WordprocessingDocument wordDoc, DocxToHtmlSettings settings,
            List<XElement> elementsPrecedingTab, XElement firstTabRun, DocxToHtmlTransformMode txMode, DocxToHtmlTransformState txState)
        {
            var tabWidth = firstTabRun != null
                ? (decimal?)firstTabRun.Elements(W.tab).Attributes(PtOpenXml.TabWidth).FirstOrDefault() ?? 0m
                : 0m;
            var precedingElementsWidth = elementsPrecedingTab
                .Elements()
                .Where(c => c.Attributes(PtOpenXml.TabWidth).Any())
                .Select(e => (decimal)e.Attribute(PtOpenXml.TabWidth))
                .Sum();
            var totalWidth = precedingElementsWidth + tabWidth;

            var txElementsPrecedingTab = elementsPrecedingTab
                .Select(e => ConvertToHtmlTransform(wordDoc, settings, e, false, 0m, txMode, txState))
                .ToList();
            if (txElementsPrecedingTab.Count > 1)
            {
                var span = new XElement(Xhtml.span, txElementsPrecedingTab);
                var spanStyle = new Dictionary<string, string>
                {
                    { "display", "inline-block" },
                    { "text-indent", "0" },
                    { "width", string.Format(NumberFormatInfo.InvariantInfo, "{0:0.000}in", totalWidth) }
                };
                span.AddAnnotation(spanStyle);
            }
            else if (txElementsPrecedingTab.Count == 1)
            {
                var element = txElementsPrecedingTab.First() as XElement;
                if (element != null)
                {
                    var spanStyle = element.Annotation<Dictionary<string, string>>();
                    spanStyle.AddIfMissing("display", "inline-block");
                    spanStyle.AddIfMissing("text-indent", "0");
                    spanStyle.AddIfMissing("width", string.Format(NumberFormatInfo.InvariantInfo, "{0:0.000}in", totalWidth));
                }
            }
            return txElementsPrecedingTab;
        }

        private static Dictionary<string, string> DefineParagraphStyle(XElement paragraph, XName elementName,
            bool suppressTrailingWhiteSpace, decimal currentMarginLeft, bool isBidi)
        {
            var style = new Dictionary<string, string>();

            var styleName = (string)paragraph.Attribute(PtOpenXml.StyleName);
            if (styleName != null)
                style.Add("PtStyleName", styleName);

            var pPr = paragraph.Element(W.pPr);
            if (pPr == null) return style;

            CreateStyleFromSpacing(style, pPr.Element(W.spacing), elementName, suppressTrailingWhiteSpace);
            CreateStyleFromInd(style, pPr.Element(W.ind), elementName, currentMarginLeft, isBidi);

            // todo need to handle
            // - both
            // - mediumKashida
            // - distribute
            // - numTab
            // - highKashida
            // - lowKashida
            // - thaiDistribute

            CreateStyleFromJc(style, pPr.Element(W.jc), isBidi);
            CreateStyleFromShd(style, pPr.Element(W.shd));

            // Pt.FontName
            var font = (string)paragraph.Attributes(PtOpenXml.FontName).FirstOrDefault();
            if (font != null)
                CreateFontCssProperty(font, style);

            DefineFontSize(style, paragraph);
            DefineLineHeight(style, paragraph);

            // vertical text alignment as of December 2013 does not work in any major browsers.
            CreateStyleFromTextAlignment(style, pPr.Element(W.textAlignment));

            style.AddIfMissing("margin-top", "0");
            style.AddIfMissing("margin-left", "0");
            style.AddIfMissing("margin-right", "0");
            style.AddIfMissing("margin-bottom", ".001pt");

            return style;
        }

        private static void CreateStyleFromInd(Dictionary<string, string> style, XElement ind, XName elementName,
            decimal currentMarginLeft, bool isBidi)
        {
            if (ind == null) return;

            var left = (decimal?)ind.Attribute(W.left);
            if (left != null && elementName != Xhtml.span)
            {
                var leftInInches = (decimal)left / 1440 - currentMarginLeft;
                style.AddIfMissing(isBidi ? "margin-right" : "margin-left",
                    leftInInches > 0m
                        ? string.Format(NumberFormatInfo.InvariantInfo, "{0:0.00}in", leftInInches)
                        : "0");
            }

            var right = (decimal?)ind.Attribute(W.right);
            if (right != null)
            {
                var rightInInches = (decimal)right / 1440;
                style.AddIfMissing(isBidi ? "margin-left" : "margin-right",
                    rightInInches > 0m
                        ? string.Format(NumberFormatInfo.InvariantInfo, "{0:0.00}in", rightInInches)
                        : "0");
            }

            var firstLine = (decimal?)ind.Attribute(W.firstLine);
            if (firstLine != null && elementName != Xhtml.span)
            {
                var firstLineInInches = (decimal)firstLine / 1440m;
                style.AddIfMissing("text-indent",
                    string.Format(NumberFormatInfo.InvariantInfo, "{0:0.00}in", firstLineInInches));
            }

            var hanging = (decimal?)ind.Attribute(W.hanging);
            if (hanging != null && elementName != Xhtml.span)
            {
                var hangingInInches = (decimal)-hanging / 1440m;
                style.AddIfMissing("text-indent",
                    string.Format(NumberFormatInfo.InvariantInfo, "{0:0.00}in", hangingInInches));
            }
        }

        private static void CreateStyleFromJc(Dictionary<string, string> style, XElement jc, bool isBidi)
        {
            if (jc != null)
            {
                var jcVal = (string)jc.Attributes(W.val).FirstOrDefault() ?? "left";
                if (jcVal == "left")
                    style.AddIfMissing("text-align", isBidi ? "right" : "left");
                else if (jcVal == "right")
                    style.AddIfMissing("text-align", isBidi ? "left" : "right");
                else if (jcVal == "center")
                    style.AddIfMissing("text-align", "center");
                else if (jcVal == "both")
                    style.AddIfMissing("text-align", "justify");
            }
        }

        private static void CreateStyleFromSpacing(Dictionary<string, string> style, XElement spacing, XName elementName,
            bool suppressTrailingWhiteSpace)
        {
            if (spacing == null) return;

            var spacingBefore = (decimal?)spacing.Attribute(W.before);
            if (spacingBefore != null && elementName != Xhtml.span)
                style.AddIfMissing("margin-top",
                    spacingBefore > 0m
                        ? string.Format(NumberFormatInfo.InvariantInfo, "{0}pt", spacingBefore / 20.0m)
                        : "0");

            var lineRule = (string)spacing.Attribute(W.lineRule);
            if (lineRule == "auto")
            {
                var line = (decimal)spacing.Attribute(W.line);
                if (line != 240m)
                {
                    var pct = (line / 240m) * 100m;
                    style.Add("line-height", string.Format(NumberFormatInfo.InvariantInfo, "{0:0.0}%", pct));
                }
            }
            if (lineRule == "exact")
            {
                var line = (decimal)spacing.Attribute(W.line);
                var points = line / 20m;
                style.Add("line-height", string.Format(NumberFormatInfo.InvariantInfo, "{0:0.0}pt", points));
            }
            if (lineRule == "atLeast")
            {
                var line = (decimal)spacing.Attribute(W.line);
                var points = line / 20m;
                if (points >= 14m)
                    style.Add("line-height", string.Format(NumberFormatInfo.InvariantInfo, "{0:0.0}pt", points));
            }

            var spacingAfter = suppressTrailingWhiteSpace ? 0m : (decimal?)spacing.Attribute(W.after);
            if (spacingAfter != null)
                style.AddIfMissing("margin-bottom",
                    spacingAfter > 0m
                        ? string.Format(NumberFormatInfo.InvariantInfo, "{0}pt", spacingAfter / 20.0m)
                        : "0");
        }

        private static void CreateStyleFromTextAlignment(Dictionary<string, string> style, XElement textAlignment)
        {
            if (textAlignment == null) return;

            var verticalTextAlignment = (string)textAlignment.Attributes(W.val).FirstOrDefault();
            if (verticalTextAlignment == null || verticalTextAlignment == "auto") return;

            if (verticalTextAlignment == "top")
                style.AddIfMissing("vertical-align", "top");
            else if (verticalTextAlignment == "center")
                style.AddIfMissing("vertical-align", "middle");
            else if (verticalTextAlignment == "baseline")
                style.AddIfMissing("vertical-align", "baseline");
            else if (verticalTextAlignment == "bottom")
                style.AddIfMissing("vertical-align", "bottom");
        }

        private static void DefineFontSize(Dictionary<string, string> style, XElement paragraph)
        {
            var sz = paragraph
                .DescendantsTrimmed(W.txbxContent)
                .Where(e => e.Name == W.r)
                .Select(r => FontMetrics.GetFontSize(r))
                .Max();
            if (sz != null)
                style.AddIfMissing("font-size", string.Format(NumberFormatInfo.InvariantInfo, "{0}pt", sz / 2.0m));
        }

        private static void DefineLineHeight(Dictionary<string, string> style, XElement paragraph)
        {
            var allRunsAreUniDirectional = paragraph
                .DescendantsTrimmed(W.txbxContent)
                .Where(e => e.Name == W.r)
                .Select(run => (string)run.Attribute(PtOpenXml.LanguageType))
                .All(lt => lt != "bidi");
            if (allRunsAreUniDirectional)
                style.AddIfMissing("line-height", "108%");
        }

        /*
         * Handle:
         * - b
         * - bdr
         * - caps
         * - color
         * - dstrike
         * - highlight
         * - i
         * - position
         * - rFonts
         * - shd
         * - smallCaps
         * - spacing
         * - strike
         * - sz
         * - u
         * - vanish
         * - vertAlign
         *
         * Don't handle:
         * - em
         * - emboss
         * - fitText
         * - imprint
         * - kern
         * - outline
         * - shadow
         * - w
         *
         */

        private static object ConvertRun(WordprocessingDocument wordDoc, DocxToHtmlSettings settings, XElement run, DocxToHtmlTransformMode txMode, DocxToHtmlTransformState txState)
        {
            var rPr = run.Element(W.rPr);
            if (rPr == null)
                return run.Elements().Select(e => ConvertToHtmlTransform(wordDoc, settings, e, false, 0m, txMode, txState));

            // hide all content that contains the w:rPr/w:webHidden element
            if (rPr.Element(W.webHidden) != null)
                return null;

            var style = DefineRunStyle(run);
            object content = run.Elements().Select(e => ConvertToHtmlTransform(wordDoc, settings, e, false, 0m, txMode, txState));

            // Wrap content in h:sup or h:sub elements as necessary.
            if (rPr.Element(W.vertAlign) != null)
            {
                XElement newContent = null;
                var vertAlignVal = (string)rPr.Elements(W.vertAlign).Attributes(W.val).FirstOrDefault();
                switch (vertAlignVal)
                {
                    case "superscript":
                        newContent = new XElement(Xhtml.sup, content);
                        break;
                    case "subscript":
                        newContent = new XElement(Xhtml.sub, content);
                        break;
                }
                if (newContent != null && newContent.Nodes().Any())
                    content = newContent;
            }

            var langAttribute = GetLangAttribute(run);

            XEntity runStartMark;
            XEntity runEndMark;
            DetermineRunMarks(run, rPr, style, out runStartMark, out runEndMark);

            // change appearance of a run based on whether deleting, inserting, etc.
            AdjustStylePerTxState(style, txMode, txState);

            XElement toolTipSpan = null;
            if (txMode.ToolTipText != null)
            {
                toolTipSpan = new XElement(Xhtml.span,
                    new XAttribute("class", txMode.ToolTipTextClass),
                    GetNoSpaceAttribute(txMode.ToolTipText),
                    txMode.ToolTipText);
            }

            if (style.Any() || langAttribute != null || runStartMark != null || txMode.AdditionalClass != null)
            {
                style.AddIfMissing("margin", "0");
                style.AddIfMissing("padding", "0");
                var xe = new XElement(Xhtml.span,
                    langAttribute,
                    runStartMark,
                    content,
                    runEndMark,
                    GetNoSpaceAttribute(run.Value),
                    toolTipSpan);

                xe.AddAnnotation(style);

                if (txMode.AdditionalClass != null)
                {
                    var cssClassAnnotation = new CssClassAnnotation();
                    cssClassAnnotation.Class = "OxPt-ToolTip ";
                    xe.AddAnnotation(cssClassAnnotation);
                }

                content = xe;
            }
            return content;
        }

        private static void AdjustStylePerTxState(Dictionary<string, string> style, DocxToHtmlTransformMode txMode, DocxToHtmlTransformState txState)
        {
            if (txMode.RevTrackingMode == RevTrackingMode.Delete ||
                txMode.RevTrackingMode == RevTrackingMode.Insert ||
                txMode.RevTrackingMode == RevTrackingMode.MoveFrom ||
                txMode.RevTrackingMode == RevTrackingMode.MoveTo)
            {
                if (style.ContainsKey("color"))
                    style.Remove("color");
                if (style.ContainsKey("text-decoration"))
                    style.Remove("text-decoration");
            }

            // todo todo todo todo if parameterizing, this is where to set
            // we want to set color based on author
            if (txMode.RevTrackingMode == RevTrackingMode.Delete)
            {
                style.Add("color", "#C00000");
                style.Add("text-decoration", "line-through");
            }
            else if (txMode.RevTrackingMode == RevTrackingMode.Insert)
            {
                style.Add("color", "#C00000");
                style.Add("text-decoration", "underline");
            }
            else if (txMode.RevTrackingMode == RevTrackingMode.MoveFrom)
            {
                style.Add("color", "#00C000");
                style.Add("text-decoration", "line-through");
            }
            else if (txMode.RevTrackingMode == RevTrackingMode.MoveTo)
            {
                style.Add("color", "#00C000");
                style.Add("text-decoration", "underline");
            }
        }

        [SuppressMessage("ReSharper", "FunctionComplexityOverflow")]
        private static Dictionary<string, string> DefineRunStyle(XElement run)
        {
            var style = new Dictionary<string, string>();

            var rPr = run.Elements(W.rPr).First();

            var styleName = (string)run.Attribute(PtOpenXml.StyleName);
            if (styleName != null)
                style.Add("PtStyleName", styleName);

            // W.bdr
            if (rPr.Element(W.bdr) != null && (string)rPr.Elements(W.bdr).Attributes(W.val).FirstOrDefault() != "none")
            {
                style.AddIfMissing("border", "solid windowtext 1.0pt");
                style.AddIfMissing("padding", "0");
            }

            // W.color
            var color = (string)rPr.Elements(W.color).Attributes(W.val).FirstOrDefault();
            if (color != null)
                CreateColorProperty("color", color, style);

            // W.highlight
            var highlight = (string)rPr.Elements(W.highlight).Attributes(W.val).FirstOrDefault();
            if (highlight != null)
                CreateColorProperty("background", highlight, style);

            // W.shd
            var shade = (string)rPr.Elements(W.shd).Attributes(W.fill).FirstOrDefault();
            if (shade != null)
                CreateColorProperty("background", shade, style);

            // Pt.FontName
            var sym = run.Element(W.sym);
            var font = sym != null
                ? (string)sym.Attributes(W.font).FirstOrDefault()
                : (string)run.Attributes(PtOpenXml.FontName).FirstOrDefault();
            if (font != null)
                CreateFontCssProperty(font, style);

            // W.sz
            var languageType = (string)run.Attribute(PtOpenXml.LanguageType);
            var sz = FontMetrics.GetFontSize(languageType, rPr);
            if (sz != null)
                style.AddIfMissing("font-size", string.Format(NumberFormatInfo.InvariantInfo, "{0}pt", sz / 2.0m));

            // W.caps
            if (GetBoolProp(rPr, W.caps))
                style.AddIfMissing("text-transform", "uppercase");

            // W.smallCaps
            if (GetBoolProp(rPr, W.smallCaps))
                style.AddIfMissing("font-variant", "small-caps");

            // W.spacing
            var spacingInTwips = (decimal?)rPr.Elements(W.spacing).Attributes(W.val).FirstOrDefault();
            if (spacingInTwips != null)
                style.AddIfMissing("letter-spacing",
                    spacingInTwips > 0m
                        ? string.Format(NumberFormatInfo.InvariantInfo, "{0}pt", spacingInTwips / 20)
                        : "0");

            // W.position
            var position = (decimal?)rPr.Elements(W.position).Attributes(W.val).FirstOrDefault();
            if (position != null)
            {
                style.AddIfMissing("position", "relative");
                style.AddIfMissing("top", string.Format(NumberFormatInfo.InvariantInfo, "{0}pt", -(position / 2)));
            }

            // W.vanish
            if (GetBoolProp(rPr, W.vanish) && !GetBoolProp(rPr, W.specVanish))
                style.AddIfMissing("display", "none");

            // W.u
            if (rPr.Element(W.u) != null && (string)rPr.Elements(W.u).Attributes(W.val).FirstOrDefault() != "none")
                style.AddIfMissing("text-decoration", "underline");

            // W.i
            style.AddIfMissing("font-style", GetBoolProp(rPr, W.i) ? "italic" : "normal");

            // W.b
            style.AddIfMissing("font-weight", GetBoolProp(rPr, W.b) ? "bold" : "normal");

            // W.strike
            if (GetBoolProp(rPr, W.strike) || GetBoolProp(rPr, W.dstrike))
                style.AddIfMissing("text-decoration", "line-through");

            return style;
        }

        private static void DetermineRunMarks(XElement run, XElement rPr, Dictionary<string, string> style, out XEntity runStartMark, out XEntity runEndMark)
        {
            runStartMark = null;
            runEndMark = null;

            // Only do the following for text runs.
            if (run.Element(W.t) == null) return;

            // Can't add directional marks if the font-family is symbol - they are visible, and display as a ?
            var addDirectionalMarks = true;
            if (style.ContainsKey("font-family"))
            {
                if (style["font-family"].ToLower() == "symbol")
                    addDirectionalMarks = false;
            }
            if (!addDirectionalMarks) return;

            var isRtl = rPr.Element(W.rtl) != null;
            if (isRtl)
            {
                runStartMark = new XEntity("#x200f"); // RLM
                runEndMark = new XEntity("#x200f"); // RLM
            }
            else
            {
                var paragraph = run.Ancestors(W.p).First();
                var paraIsBidi = paragraph
                    .Elements(W.pPr)
                    .Elements(W.bidi)
                    .Any(b => b.Attribute(W.val) == null || b.Attribute(W.val).ToBoolean() == true);

                if (paraIsBidi)
                {
                    runStartMark = new XEntity("#x200e"); // LRM
                    runEndMark = new XEntity("#x200e"); // LRM
                }
            }
        }

        private static XAttribute GetLangAttribute(XElement run)
        {
            const string defaultLanguage = "en-US"; // todo need to get defaultLanguage

            var rPr = run.Elements(W.rPr).First();
            var languageType = (string)run.Attribute(PtOpenXml.LanguageType);

            string lang = null;
            if (languageType == "western")
                lang = (string)rPr.Elements(W.lang).Attributes(W.val).FirstOrDefault();
            else if (languageType == "bidi")
                lang = (string)rPr.Elements(W.lang).Attributes(W.bidi).FirstOrDefault();
            else if (languageType == "eastAsia")
                lang = (string)rPr.Elements(W.lang).Attributes(W.eastAsia).FirstOrDefault();
            if (lang == null)
                lang = defaultLanguage;

            return lang != defaultLanguage ? new XAttribute("lang", lang) : null;
        }

        private static void AdjustTableBorders(WordprocessingDocument wordDoc)
        {
            // Note: when implementing a paging version of the HTML transform, this needs to be done
            // for all content parts, not just the main document part.

            var xd = wordDoc.MainDocumentPart.GetXDocument();
            foreach (var tbl in xd.Descendants(W.tbl))
                AdjustTableBorders(tbl);
            wordDoc.MainDocumentPart.PutXDocument();
        }

        private static void AdjustTableBorders(XElement tbl)
        {
            var ta = tbl
                .Elements(W.tr)
                .Select(r => r
                    .Elements(W.tc)
                    .SelectMany(c =>
                        Enumerable.Repeat(c,
                            (int?)c.Elements(W.tcPr).Elements(W.gridSpan).Attributes(W.val).FirstOrDefault() ?? 1))
                    .ToArray())
                .ToArray();

            for (var y = 0; y < ta.Length; y++)
            {
                for (var x = 0; x < ta[y].Length; x++)
                {
                    var thisCell = ta[y][x];
                    FixTopBorder(ta, thisCell, x, y);
                    FixLeftBorder(ta, thisCell, x, y);
                    FixBottomBorder(ta, thisCell, x, y);
                    FixRightBorder(ta, thisCell, x, y);
                }
            }
        }

        private static void FixTopBorder(XElement[][] ta, XElement thisCell, int x, int y)
        {
            if (y > 0)
            {
                var rowAbove = ta[y - 1];
                if (x < rowAbove.Length - 1)
                {
                    XElement cellAbove = ta[y - 1][x];
                    if (cellAbove != null &&
                        thisCell.Elements(W.tcPr).Elements(W.tcBorders).FirstOrDefault() != null &&
                        cellAbove.Elements(W.tcPr).Elements(W.tcBorders).FirstOrDefault() != null)
                    {
                        ResolveCellBorder(
                            thisCell.Elements(W.tcPr).Elements(W.tcBorders).Elements(W.top).FirstOrDefault(),
                            cellAbove.Elements(W.tcPr).Elements(W.tcBorders).Elements(W.bottom).FirstOrDefault());
                    }
                }
            }
        }

        private static void FixLeftBorder(XElement[][] ta, XElement thisCell, int x, int y)
        {
            if (x > 0)
            {
                XElement cellLeft = ta[y][x - 1];
                if (cellLeft != null &&
                    thisCell.Elements(W.tcPr).Elements(W.tcBorders).FirstOrDefault() != null &&
                    cellLeft.Elements(W.tcPr).Elements(W.tcBorders).FirstOrDefault() != null)
                {
                    ResolveCellBorder(
                        thisCell.Elements(W.tcPr).Elements(W.tcBorders).Elements(W.left).FirstOrDefault(),
                        cellLeft.Elements(W.tcPr).Elements(W.tcBorders).Elements(W.right).FirstOrDefault());
                }
            }
        }

        private static void FixBottomBorder(XElement[][] ta, XElement thisCell, int x, int y)
        {
            if (y < ta.Length - 1)
            {
                var rowBelow = ta[y + 1];
                if (x < rowBelow.Length - 1)
                {
                    XElement cellBelow = ta[y + 1][x];
                    if (cellBelow != null &&
                        thisCell.Elements(W.tcPr).Elements(W.tcBorders).FirstOrDefault() != null &&
                        cellBelow.Elements(W.tcPr).Elements(W.tcBorders).FirstOrDefault() != null)
                    {
                        ResolveCellBorder(
                            thisCell.Elements(W.tcPr).Elements(W.tcBorders).Elements(W.bottom).FirstOrDefault(),
                            cellBelow.Elements(W.tcPr).Elements(W.tcBorders).Elements(W.top).FirstOrDefault());
                    }
                }
            }
        }

        private static void FixRightBorder(XElement[][] ta, XElement thisCell, int x, int y)
        {
            if (x < ta[y].Length - 1)
            {
                XElement cellRight = ta[y][x + 1];
                if (cellRight != null &&
                    thisCell.Elements(W.tcPr).Elements(W.tcBorders).FirstOrDefault() != null &&
                    cellRight.Elements(W.tcPr).Elements(W.tcBorders).FirstOrDefault() != null)
                {
                    ResolveCellBorder(
                        thisCell.Elements(W.tcPr).Elements(W.tcBorders).Elements(W.right).FirstOrDefault(),
                        cellRight.Elements(W.tcPr).Elements(W.tcBorders).Elements(W.left).FirstOrDefault());
                }
            }
        }

        private static readonly Dictionary<string, int> BorderTypePriority = new Dictionary<string, int>()
        {
            { "single", 1 },
            { "thick", 2 },
            { "double", 3 },
            { "dotted", 4 },
        };

        private static readonly Dictionary<string, int> BorderNumber = new Dictionary<string, int>()
        {
            {"single", 1 },
            {"thick", 2 },
            {"double", 3 },
            {"dotted", 4 },
            {"dashed", 5 },
            {"dotDash", 5 },
            {"dotDotDash", 5 },
            {"triple", 6 },
            {"thinThickSmallGap", 6 },
            {"thickThinSmallGap", 6 },
            {"thinThickThinSmallGap", 6 },
            {"thinThickMediumGap", 6 },
            {"thickThinMediumGap", 6 },
            {"thinThickThinMediumGap", 6 },
            {"thinThickLargeGap", 6 },
            {"thickThinLargeGap", 6 },
            {"thinThickThinLargeGap", 6 },
            {"wave", 7 },
            {"doubleWave", 7 },
            {"dashSmallGap", 5 },
            {"dashDotStroked", 5 },
            {"threeDEmboss", 7 },
            {"threeDEngrave", 7 },
            {"outset", 7 },
            {"inset", 7 },
        };

        private static void ResolveCellBorder(XElement border1, XElement border2)
        {
            if (border1 == null || border2 == null)
                return;
            if ((string)border1.Attribute(W.val) == "nil" || (string)border2.Attribute(W.val) == "nil")
                return;
            if ((string)border1.Attribute(W.sz) == "nil" || (string)border2.Attribute(W.sz) == "nil")
                return;

            var border1Val = (string)border1.Attribute(W.val);
            var border1Weight = 1;
            if (BorderNumber.ContainsKey(border1Val))
                border1Weight = BorderNumber[border1Val];

            var border2Val = (string)border2.Attribute(W.val);
            var border2Weight = 1;
            if (BorderNumber.ContainsKey(border2Val))
                border2Weight = BorderNumber[border2Val];

            if (border1Weight != border2Weight)
            {
                if (border1Weight < border2Weight)
                    BorderOverride(border2, border1);
                else
                    BorderOverride(border1, border2);
            }

            if ((decimal)border1.Attribute(W.sz) > (decimal)border2.Attribute(W.sz))
            {
                BorderOverride(border1, border2);
                return;
            }

            if ((decimal)border1.Attribute(W.sz) < (decimal)border2.Attribute(W.sz))
            {
                BorderOverride(border2, border1);
                return;
            }

            var border1Type = (string)border1.Attribute(W.val);
            var border2Type = (string)border2.Attribute(W.val);
            if (BorderTypePriority.ContainsKey(border1Type) &&
                BorderTypePriority.ContainsKey(border2Type))
            {
                var border1Pri = BorderTypePriority[border1Type];
                var border2Pri = BorderTypePriority[border2Type];
                if (border1Pri < border2Pri)
                {
                    BorderOverride(border2, border1);
                    return;
                }
                if (border2Pri < border1Pri)
                {
                    BorderOverride(border1, border2);
                    return;
                }
            }

            var color1Str = (string)border1.Attribute(W.color);
            if (color1Str == "auto")
                color1Str = "000000";
            var color2Str = (string)border2.Attribute(W.color);
            if (color2Str == "auto")
                color2Str = "000000";
            if (color1Str != null && color2Str != null && color1Str != color2Str)
            {
                try
                {
                    var color1 = Convert.ToInt32(color1Str, 16);
                    var color2 = Convert.ToInt32(color2Str, 16);
                    if (color1 < color2)
                    {
                        BorderOverride(border1, border2);
                        return;
                    }
                    if (color2 < color1)
                    {
                        BorderOverride(border2, border1);
                    }
                }
                // if the above throws ArgumentException, FormatException, or OverflowException, then abort
                catch (Exception)
                {
                    // Ignore
                }
            }
        }

        private static void BorderOverride(XElement fromBorder, XElement toBorder)
        {
            toBorder.Attribute(W.val).Value = fromBorder.Attribute(W.val).Value;
            if (fromBorder.Attribute(W.color) != null)
                toBorder.SetAttributeValue(W.color, fromBorder.Attribute(W.color).Value);
            if (fromBorder.Attribute(W.sz) != null)
                toBorder.SetAttributeValue(W.sz, fromBorder.Attribute(W.sz).Value);
            if (fromBorder.Attribute(W.themeColor) != null)
                toBorder.SetAttributeValue(W.themeColor, fromBorder.Attribute(W.themeColor).Value);
            if (fromBorder.Attribute(W.themeTint) != null)
                toBorder.SetAttributeValue(W.themeTint, fromBorder.Attribute(W.themeTint).Value);
        }

        private static void CalculateSpanWidthForTabs(WordprocessingDocument wordDoc, DocxToHtmlTransformState txState)
        {
            // Note: when implementing a paging version of the HTML transform, this needs to be done
            // for all content parts, not just the main document part.

            // w:defaultTabStop in settings
            int defaultTabStop = 720;
            if (wordDoc.MainDocumentPart.DocumentSettingsPart != null)
            {
                var sxd = wordDoc.MainDocumentPart.DocumentSettingsPart.GetXDocument();
                defaultTabStop = (int?)sxd.Descendants(W.defaultTabStop).Attributes(W.val).FirstOrDefault() ?? 720;
            }

            var pxd = wordDoc.MainDocumentPart.GetXDocument();
            var root = pxd.Root;
            if (root == null) return;

            var newRoot = (XElement)CalculateSpanWidthTransform(root, defaultTabStop, txState);
            root.ReplaceWith(newRoot);
            wordDoc.MainDocumentPart.PutXDocument();
        }

        // TODO: Refactor. This method is way too long.
        [SuppressMessage("ReSharper", "FunctionComplexityOverflow")]
        private static object CalculateSpanWidthTransform(XNode node, int defaultTabStop, DocxToHtmlTransformState txState)
        {
            var element = node as XElement;
            if (element == null) return node;

            // if it is not a paragraph or if there are no tabs in the paragraph,
            // then no need to continue processing.
            if (element.Name != W.p ||
                !element.DescendantsTrimmed(W.txbxContent).Where(d => d.Name == W.r).Elements(W.tab).Any())
            {
                // TODO: Revisit. Can we just return the node if it is a paragraph that does not have any tab?
                return new XElement(element.Name,
                    element.Attributes(),
                    element.Nodes().Select(n => CalculateSpanWidthTransform(n, defaultTabStop, txState)));
            }

            var clonedPara = new XElement(element);

            var leftInTwips = 0;
            var firstInTwips = 0;

            var ind = clonedPara.Elements(W.pPr).Elements(W.ind).FirstOrDefault();
            if (ind != null)
            {
                // todo need to handle start and end attributes

                var left = (int?)ind.Attribute(W.left);
                if (left != null)
                    leftInTwips = (int)left;

                var firstLine = 0;
                var firstLineAtt = (int?)ind.Attribute(W.firstLine);
                if (firstLineAtt != null)
                    firstLine = (int)firstLineAtt;

                var hangingAtt = (int?)ind.Attribute(W.hanging);
                if (hangingAtt != null)
                    firstLine = -(int)hangingAtt;

                firstInTwips = leftInTwips + firstLine;
            }

            // calculate the tab stops, in twips
            var tabs = clonedPara
                .Elements(W.pPr)
                .Elements(W.tabs)
                .FirstOrDefault();

            if (tabs == null)
            {
                if (leftInTwips == 0)
                {
                    tabs = new XElement(W.tabs,
                        Enumerable.Range(1, 100)
                            .Select(r => new XElement(W.tab,
                                new XAttribute(W.val, "left"),
                                new XAttribute(W.pos, r * defaultTabStop))));
                }
                else
                {
                    tabs = new XElement(W.tabs,
                        new XElement(W.tab,
                            new XAttribute(W.val, "left"),
                            new XAttribute(W.pos, leftInTwips)));
                    tabs = AddDefaultTabsAfterLastTab(tabs, defaultTabStop);
                }
            }
            else
            {
                if (leftInTwips != 0)
                {
                    tabs.Add(
                        new XElement(W.tab,
                            new XAttribute(W.val, "left"),
                            new XAttribute(W.pos, leftInTwips)));
                }
                tabs = AddDefaultTabsAfterLastTab(tabs, defaultTabStop);
            }

            var twipCounter = firstInTwips;
            var contentToMeasure = element.DescendantsTrimmed(z => z.Name == W.txbxContent || z.Name == W.pPr || z.Name == W.rPr).ToArray();
            var currentElementIdx = 0;
            while (true)
            {
                var currentElement = contentToMeasure[currentElementIdx];

                if (currentElement.Name == W.br)
                {
                    twipCounter = leftInTwips;

                    currentElement.Add(new XAttribute(PtOpenXml.TabWidth,
                        string.Format(NumberFormatInfo.InvariantInfo,
                            "{0:0.000}", (decimal)firstInTwips / 1440m)));

                    currentElementIdx++;
                    if (currentElementIdx >= contentToMeasure.Length)
                        break; // we're done
                }

                if (currentElement.Name == W.tab)
                {
                    var runContainingTabToReplace = currentElement.Parent;
                    var fontNameAtt = runContainingTabToReplace.Attribute(PtOpenXml.pt + "FontName") ??
                                      runContainingTabToReplace.Ancestors(W.p).First().Attribute(PtOpenXml.pt + "FontName");

                    var testAmount = twipCounter;

                    var tabAfterText = tabs
                        .Elements(W.tab)
                        .FirstOrDefault(t => (int)t.Attribute(W.pos) > testAmount);

                    if (tabAfterText == null)
                    {
                        // something has gone wrong, so put 1/2 inch in
                        if (currentElement.Attribute(PtOpenXml.TabWidth) == null)
                            currentElement.Add(
                                new XAttribute(PtOpenXml.TabWidth, 720m));
                        break;
                    }

                    var tabVal = (string)tabAfterText.Attribute(W.val);
                    if (tabVal == "right" || tabVal == "end")
                    {
                        var textAfterElements = contentToMeasure
                            .Skip(currentElementIdx + 1);

                        // take all the content until another tab, br, or cr
                        var textElementsToMeasure = textAfterElements
                            .TakeWhile(z =>
                                z.Name != W.tab &&
                                z.Name != W.br &&
                                z.Name != W.cr)
                            .ToList();

                        var textAfterTab = textElementsToMeasure
                            .Where(z => z.Name == W.t)
                            .Select(t => (string)t)
                            .StringConcatenate();

                        var dummyRun2 = new XElement(W.r,
                            fontNameAtt,
                            runContainingTabToReplace.Elements(W.rPr),
                            new XElement(W.t, textAfterTab));

                        var sourceParagraph = runContainingTabToReplace
                            .Ancestors(W.p)
                            .First();

                        var dummyParagraph2 = new XElement(W.p,
                            sourceParagraph.Attributes(),
                            sourceParagraph.Element(W.pPr),
                            dummyRun2);

                        InitializeStateGraphics(txState);
                        var widthOfTextAfterTab = FontMetrics.CalcWidthOfRunInTwips(dummyRun2, txState.Graphics);
                        var delta2 = (int)tabAfterText.Attribute(W.pos) - widthOfTextAfterTab - twipCounter;
                        if (delta2 < 0)
                            delta2 = 0;
                        currentElement.Add(
                            new XAttribute(PtOpenXml.TabWidth,
                                string.Format(NumberFormatInfo.InvariantInfo, "{0:0.000}", (decimal)delta2 / 1440m)),
                            GetLeader(tabAfterText));
                        twipCounter = Math.Max((int)tabAfterText.Attribute(W.pos), twipCounter + widthOfTextAfterTab);

                        var lastElement = textElementsToMeasure.LastOrDefault();
                        if (lastElement == null)
                            break; // we're done

                        currentElementIdx = Array.IndexOf(contentToMeasure, lastElement) + 1;
                        if (currentElementIdx >= contentToMeasure.Length)
                            break; // we're done

                        continue;
                    }
                    if (tabVal == "decimal")
                    {
                        var textAfterElements = contentToMeasure
                            .Skip(currentElementIdx + 1);

                        // take all the content until another tab, br, or cr
                        var textElementsToMeasure = textAfterElements
                            .TakeWhile(z =>
                                z.Name != W.tab &&
                                z.Name != W.br &&
                                z.Name != W.cr)
                            .ToList();

                        var textAfterTab = textElementsToMeasure
                            .Where(z => z.Name == W.t)
                            .Select(t => (string)t)
                            .StringConcatenate();

                        if (textAfterTab.Contains("."))
                        {
                            var mantissa = textAfterTab.Split('.')[0];

                            var dummyRun4 = new XElement(W.r,
                                fontNameAtt,
                                runContainingTabToReplace.Elements(W.rPr),
                                new XElement(W.t, mantissa));

                            InitializeStateGraphics(txState);
                            var widthOfMantissa = FontMetrics.CalcWidthOfRunInTwips(dummyRun4, txState.Graphics);
                            var delta2 = (int)tabAfterText.Attribute(W.pos) - widthOfMantissa - twipCounter;
                            if (delta2 < 0)
                                delta2 = 0;
                            currentElement.Add(
                                new XAttribute(PtOpenXml.TabWidth,
                                    string.Format(NumberFormatInfo.InvariantInfo, "{0:0.000}", (decimal)delta2 / 1440m)),
                                GetLeader(tabAfterText));

                            var decims = textAfterTab.Substring(textAfterTab.IndexOf('.'));
                            dummyRun4 = new XElement(W.r,
                                fontNameAtt,
                                runContainingTabToReplace.Elements(W.rPr),
                                new XElement(W.t, decims));

                            InitializeStateGraphics(txState);
                            var widthOfDecims = FontMetrics.CalcWidthOfRunInTwips(dummyRun4, txState.Graphics);
                            twipCounter = Math.Max((int)tabAfterText.Attribute(W.pos) + widthOfDecims, twipCounter + widthOfMantissa + widthOfDecims);

                            var lastElement = textElementsToMeasure.LastOrDefault();
                            if (lastElement == null)
                                break; // we're done

                            currentElementIdx = Array.IndexOf(contentToMeasure, lastElement) + 1;
                            if (currentElementIdx >= contentToMeasure.Length)
                                break; // we're done

                            continue;
                        }
                        else
                        {
                            var dummyRun2 = new XElement(W.r,
                                fontNameAtt,
                                runContainingTabToReplace.Elements(W.rPr),
                                new XElement(W.t, textAfterTab));

                            InitializeStateGraphics(txState);
                            var widthOfTextAfterTab = FontMetrics.CalcWidthOfRunInTwips(dummyRun2, txState.Graphics);
                            var delta2 = (int)tabAfterText.Attribute(W.pos) - widthOfTextAfterTab - twipCounter;
                            if (delta2 < 0)
                                delta2 = 0;
                            currentElement.Add(
                                new XAttribute(PtOpenXml.TabWidth,
                                    string.Format(NumberFormatInfo.InvariantInfo, "{0:0.000}", (decimal)delta2 / 1440m)),
                                GetLeader(tabAfterText));
                            twipCounter = Math.Max((int)tabAfterText.Attribute(W.pos), twipCounter + widthOfTextAfterTab);

                            var lastElement = textElementsToMeasure.LastOrDefault();
                            if (lastElement == null)
                                break; // we're done

                            currentElementIdx = Array.IndexOf(contentToMeasure, lastElement) + 1;
                            if (currentElementIdx >= contentToMeasure.Length)
                                break; // we're done

                            continue;
                        }
                    }
                    if ((string)tabAfterText.Attribute(W.val) == "center")
                    {
                        var textAfterElements = contentToMeasure
                            .Skip(currentElementIdx + 1);

                        // take all the content until another tab, br, or cr
                        var textElementsToMeasure = textAfterElements
                            .TakeWhile(z =>
                                z.Name != W.tab &&
                                z.Name != W.br &&
                                z.Name != W.cr)
                            .ToList();

                        var textAfterTab = textElementsToMeasure
                            .Where(z => z.Name == W.t)
                            .Select(t => (string)t)
                            .StringConcatenate();

                        var dummyRun4 = new XElement(W.r,
                            fontNameAtt,
                            runContainingTabToReplace.Elements(W.rPr),
                            new XElement(W.t, textAfterTab));

                        InitializeStateGraphics(txState);
                        var widthOfText = FontMetrics.CalcWidthOfRunInTwips(dummyRun4, txState.Graphics);
                        var delta2 = (int)tabAfterText.Attribute(W.pos) - (widthOfText / 2) - twipCounter;
                        if (delta2 < 0)
                            delta2 = 0;
                        currentElement.Add(
                            new XAttribute(PtOpenXml.TabWidth,
                                string.Format(NumberFormatInfo.InvariantInfo, "{0:0.000}", (decimal)delta2 / 1440m)),
                            GetLeader(tabAfterText));
                        twipCounter = Math.Max((int)tabAfterText.Attribute(W.pos) + widthOfText / 2, twipCounter + widthOfText);

                        var lastElement = textElementsToMeasure.LastOrDefault();
                        if (lastElement == null)
                            break; // we're done

                        currentElementIdx = Array.IndexOf(contentToMeasure, lastElement) + 1;
                        if (currentElementIdx >= contentToMeasure.Length)
                            break; // we're done

                        continue;
                    }
                    if (tabVal == "left" || tabVal == "start" || tabVal == "num")
                    {
                        var delta = (int)tabAfterText.Attribute(W.pos) - twipCounter;
                        currentElement.Add(
                            new XAttribute(PtOpenXml.TabWidth,
                                string.Format(NumberFormatInfo.InvariantInfo, "{0:0.000}", (decimal)delta / 1440m)),
                            GetLeader(tabAfterText));
                        twipCounter = (int)tabAfterText.Attribute(W.pos);

                        currentElementIdx++;
                        if (currentElementIdx >= contentToMeasure.Length)
                            break; // we're done

                        continue;
                    }
                }

                if (currentElement.Name == W.t)
                {
                    // TODO: Revisit. This is a quick fix because it doesn't work on Azure.
                    // Given the changes we've made elsewhere, though, this is not required
                    // for the first tab at least. We could also enhance that other change
                    // to deal with all tabs.
                    //var runContainingTabToReplace = currentElement.Parent;
                    //var paragraphForRun = runContainingTabToReplace.Ancestors(W.p).First();
                    //var fontNameAtt = runContainingTabToReplace.Attribute(PtOpenXml.FontName) ??
                    //                  paragraphForRun.Attribute(PtOpenXml.FontName);
                    //var languageTypeAtt = runContainingTabToReplace.Attribute(PtOpenXml.LanguageType) ??
                    //                      paragraphForRun.Attribute(PtOpenXml.LanguageType);

                    //var dummyRun3 = new XElement(W.r, fontNameAtt, languageTypeAtt,
                    //    runContainingTabToReplace.Elements(W.rPr),
                    //    currentElement);
                    //var widthOfText = CalcWidthOfRunInTwips(dummyRun3);
                    const int widthOfText = 0;
                    currentElement.Add(new XAttribute(PtOpenXml.TabWidth,
                        string.Format(NumberFormatInfo.InvariantInfo, "{0:0.000}", (decimal)widthOfText / 1440m)));
                    twipCounter += widthOfText;

                    currentElementIdx++;
                    if (currentElementIdx >= contentToMeasure.Length)
                        break; // we're done

                    continue;
                }

                currentElementIdx++;
                if (currentElementIdx >= contentToMeasure.Length)
                    break; // we're done
            }

            return new XElement(element.Name,
                element.Attributes(),
                element.Nodes().Select(n => CalculateSpanWidthTransform(n, defaultTabStop, txState)));
        }

        private static XAttribute GetLeader(XElement tabAfterText)
        {
            var leader = (string)tabAfterText.Attribute(W.leader);
            if (leader == null)
                return null;
            return new XAttribute(PtOpenXml.Leader, leader);
        }

        private static XElement AddDefaultTabsAfterLastTab(XElement tabs, int defaultTabStop)
        {
            var lastTabElement = tabs
                .Elements(W.tab)
                .Where(t => (string)t.Attribute(W.val) != "clear" && (string)t.Attribute(W.val) != "bar")
                .OrderBy(t => (int)t.Attribute(W.pos))
                .LastOrDefault();
            if (lastTabElement != null)
            {
                if (defaultTabStop == 0)
                    defaultTabStop = 720;
                var rangeStart = (int)lastTabElement.Attribute(W.pos) / defaultTabStop + 1;
                var tempTabs = new XElement(W.tabs,
                    tabs.Elements().Where(t => (string)t.Attribute(W.val) != "clear" && (string)t.Attribute(W.val) != "bar"),
                    Enumerable.Range(rangeStart, 100)
                    .Select(r => new XElement(W.tab,
                        new XAttribute(W.val, "left"),
                        new XAttribute(W.pos, r * defaultTabStop))));
                tempTabs = new XElement(W.tabs,
                    tempTabs.Elements().OrderBy(t => (int)t.Attribute(W.pos)));
                return tempTabs;
            }
            else
            {
                tabs = new XElement(W.tabs,
                    Enumerable.Range(1, 100)
                    .Select(r => new XElement(W.tab,
                        new XAttribute(W.val, "left"),
                        new XAttribute(W.pos, r * defaultTabStop))));
            }
            return tabs;
        }

        private static void InsertAppropriateNonbreakingSpaces(WordprocessingDocument wordDoc)
        {
            foreach (var part in wordDoc.ContentParts())
            {
                var pxd = part.GetXDocument();
                var root = pxd.Root;
                if (root == null) return;

                var newRoot = (XElement)InsertAppropriateNonbreakingSpacesTransform(root);
                root.ReplaceWith(newRoot);
                part.PutXDocument();
            }
        }

        // Non-breaking spaces are not required if we use appropriate CSS, i.e., "white-space: pre-wrap;".
        // We only need to make sure that empty w:p elements are translated into non-empty h:p elements,
        // because empty h:p elements would be ignored by browsers.
        // Further, in addition to not being required, non-breaking spaces would change the layout behavior
        // of spans having consecutive spaces. Therefore, avoiding non-breaking spaces has the additional
        // benefit of leading to a more faithful representation of the Word document in HTML.
        private static object InsertAppropriateNonbreakingSpacesTransform(XNode node)
        {
            XElement element = node as XElement;
            if (element != null)
            {
                // child content of run to look for
                // W.br
                // W.cr
                // W.dayLong
                // W.dayShort
                // W.drawing
                // W.monthLong
                // W.monthShort
                // W.noBreakHyphen
                // W.object
                // W.pgNum
                // W.pTab
                // W.separator
                // W.softHyphen
                // W.sym
                // W.t
                // W.tab
                // W.yearLong
                // W.yearShort
                if (element.Name == W.p)
                {
                    // Translate empty paragraphs to paragraphs having one run with
                    // a normal space. A non-breaking space, i.e., \x00A0, is not
                    // required if we use appropriate CSS.
                    bool hasContent = element
                        .Elements()
                        .Where(e => e.Name != W.pPr)
                        .DescendantsAndSelf()
                        .Any(e =>
                            e.Name == W.dayLong ||
                            e.Name == W.dayShort ||
                            e.Name == W.drawing ||
                            e.Name == W.monthLong ||
                            e.Name == W.monthShort ||
                            e.Name == W.noBreakHyphen ||
                            e.Name == W._object ||
                            e.Name == W.pgNum ||
                            e.Name == W.ptab ||
                            e.Name == W.separator ||
                            e.Name == W.softHyphen ||
                            e.Name == W.sym ||
                            e.Name == W.t ||
                            e.Name == W.tab ||
                            e.Name == W.yearLong ||
                            e.Name == W.yearShort
                        );
                    if (hasContent == false)
                        return new XElement(element.Name,
                            element.Attributes(),
                            element.Nodes().Select(n => InsertAppropriateNonbreakingSpacesTransform(n)),
                            new XElement(W.r,
                                element.Elements(W.pPr).Elements(W.rPr),
                                new XElement(W.t, " ")));
                }

                return new XElement(element.Name,
                    element.Attributes(),
                    element.Nodes().Select(n => InsertAppropriateNonbreakingSpacesTransform(n)));
            }
            return node;
        }

        private class SectionAnnotation
        {
            public XElement SectionElement;
        }

        private static void AnnotateForSections(WordprocessingDocument wordDoc)
        {
            var xd = wordDoc.MainDocumentPart.GetXDocument();

            var document = xd.Root;
            if (document == null) return;

            var body = document.Element(W.body);
            if (body == null) return;

            // move last sectPr into last paragraph
            var lastSectPr = body.Elements(W.sectPr).LastOrDefault();
            if (lastSectPr != null)
            {
                // if the last thing in the document is a table, Word will always insert a paragraph following that.
                var lastPara = body
                    .DescendantsTrimmed(W.txbxContent)
                    .LastOrDefault(p => p.Name == W.p);

                if (lastPara != null)
                {
                    var lastParaProps = lastPara.Element(W.pPr);
                    if (lastParaProps != null)
                        lastParaProps.Add(lastSectPr);
                    else
                        lastPara.Add(new XElement(W.pPr, lastSectPr));

                    lastSectPr.Remove();
                }
            }

            var reverseDescendants = xd.Descendants().Reverse().ToList();
            var currentSection = InitializeSectionAnnotation(reverseDescendants);

            foreach (var d in reverseDescendants)
            {
                if (d.Name == W.sectPr)
                {
                    if (d.Attribute(XNamespace.Xmlns + "w") == null)
                        d.Add(new XAttribute(XNamespace.Xmlns + "w", W.w));

                    currentSection = new SectionAnnotation()
                    {
                        SectionElement = d
                    };
                }
                else
                    d.AddAnnotation(currentSection);
            }
        }

        private static SectionAnnotation InitializeSectionAnnotation(IEnumerable<XElement> reverseDescendants)
        {
            var currentSection = new SectionAnnotation()
            {
                SectionElement = reverseDescendants.FirstOrDefault(e => e.Name == W.sectPr)
            };
            if (currentSection.SectionElement != null &&
                currentSection.SectionElement.Attribute(XNamespace.Xmlns + "w") == null)
                currentSection.SectionElement.Add(new XAttribute(XNamespace.Xmlns + "w", W.w));

            // todo what should the default section props be?
            if (currentSection.SectionElement == null)
                currentSection = new SectionAnnotation()
                {
                    SectionElement = new XElement(W.sectPr,
                        new XAttribute(XNamespace.Xmlns + "w", W.w),
                        new XElement(W.pgSz,
                            new XAttribute(W._w, 12240),
                            new XAttribute(W.h, 15840)),
                        new XElement(W.pgMar,
                            new XAttribute(W.top, 1440),
                            new XAttribute(W.right, 1440),
                            new XAttribute(W.bottom, 1440),
                            new XAttribute(W.left, 1440),
                            new XAttribute(W.header, 720),
                            new XAttribute(W.footer, 720),
                            new XAttribute(W.gutter, 0)),
                        new XElement(W.cols,
                            new XAttribute(W.space, 720)),
                        new XElement(W.docGrid,
                            new XAttribute(W.linePitch, 360)))
                };

            return currentSection;
        }

        private static object CreateBorderDivs(WordprocessingDocument wordDoc, DocxToHtmlSettings settings, IEnumerable<XElement> elements,
            DocxToHtmlTransformMode txMode, DocxToHtmlTransformState txState)
        {
            return elements.GroupAdjacent(e =>
            {
                var pBdr = e.Elements(W.pPr).Elements(W.pBdr).FirstOrDefault();
                if (pBdr != null)
                {
                    var indStr = string.Empty;
                    var ind = e.Elements(W.pPr).Elements(W.ind).FirstOrDefault();
                    if (ind != null)
                        indStr = ind.ToString(SaveOptions.DisableFormatting);
                    return pBdr.ToString(SaveOptions.DisableFormatting) + indStr;
                }
                return e.Name == W.tbl ? "table" : string.Empty;
            })
                .Select(g =>
                {
                    if (g.Key == string.Empty)
                    {
                        return (object)GroupAndVerticallySpaceNumberedParagraphs(wordDoc, settings, g, 0m, txMode, txState);
                    }
                    if (g.Key == "table")
                    {
                        return g.Select(gc => ConvertToHtmlTransform(wordDoc, settings, gc, false, 0, txMode, txState));
                    }
                    var pPr = g.First().Elements(W.pPr).First();
                    var pBdr = pPr.Element(W.pBdr);
                    var style = new Dictionary<string, string>();
                    GenerateBorderStyle(pBdr, W.top, style, BorderType.Paragraph);
                    GenerateBorderStyle(pBdr, W.right, style, BorderType.Paragraph);
                    GenerateBorderStyle(pBdr, W.bottom, style, BorderType.Paragraph);
                    GenerateBorderStyle(pBdr, W.left, style, BorderType.Paragraph);

                    var currentMarginLeft = 0m;
                    var ind = pPr.Element(W.ind);
                    if (ind != null)
                    {
                        var leftInInches = (decimal?)ind.Attribute(W.left) / 1440m ?? 0;
                        var hangingInInches = -(decimal?)ind.Attribute(W.hanging) / 1440m ?? 0;
                        currentMarginLeft = leftInInches + hangingInInches;

                        style.AddIfMissing("margin-left",
                            currentMarginLeft > 0m
                                ? string.Format(NumberFormatInfo.InvariantInfo, "{0:0.00}in", currentMarginLeft)
                                : "0");
                    }

                    var div = new XElement(Xhtml.div,
                        GroupAndVerticallySpaceNumberedParagraphs(wordDoc, settings, g, currentMarginLeft, txMode, txState));
                    div.AddAnnotation(style);
                    return div;
                })
            .ToList();
        }

        private static IEnumerable<object> GroupAndVerticallySpaceNumberedParagraphs(WordprocessingDocument wordDoc, DocxToHtmlSettings settings,
            IEnumerable<XElement> elements, decimal currentMarginLeft, DocxToHtmlTransformMode txMode, DocxToHtmlTransformState txState)
        {
            var grouped = elements
                .GroupAdjacent(e =>
                {
                    var abstractNumId = (string)e.Attribute(PtOpenXml.pt + "AbstractNumId");
                    if (abstractNumId != null)
                        return "num:" + abstractNumId;
                    var contextualSpacing = e.Elements(W.pPr).Elements(W.contextualSpacing).FirstOrDefault();
                    if (contextualSpacing != null)
                    {
                        var styleName = (string)e.Elements(W.pPr).Elements(W.pStyle).Attributes(W.val).FirstOrDefault();
                        if (styleName == null)
                            return "";
                        return "sty:" + styleName;
                    }
                    return "";
                })
                .ToList();
            var newContent = grouped
                .Select(g =>
                {
                    if (g.Key == "")
                        return g.Select(e => ConvertToHtmlTransform(wordDoc, settings, e, false, currentMarginLeft, txMode, txState));
                    var last = g.Count() - 1;
                    return g.Select((e, i) => ConvertToHtmlTransform(wordDoc, settings, e, i != last, currentMarginLeft, txMode, txState));
                });
            return (IEnumerable<object>)newContent;
        }

        private class BorderMappingInfo
        {
            public string CssName;
            public decimal CssSize;
        }

        private static readonly Dictionary<string, BorderMappingInfo> BorderStyleMap = new Dictionary<string, BorderMappingInfo>()
        {
            { "single", new BorderMappingInfo() { CssName = "solid", CssSize = 1.0m }},
            { "dotted", new BorderMappingInfo() { CssName = "dotted", CssSize = 1.0m }},
            { "dashSmallGap", new BorderMappingInfo() { CssName = "dashed", CssSize = 1.0m }},
            { "dashed", new BorderMappingInfo() { CssName = "dashed", CssSize = 1.0m }},
            { "dotDash", new BorderMappingInfo() { CssName = "dashed", CssSize = 1.0m }},
            { "dotDotDash", new BorderMappingInfo() { CssName = "dashed", CssSize = 1.0m }},
            { "double", new BorderMappingInfo() { CssName = "double", CssSize = 2.5m }},
            { "triple", new BorderMappingInfo() { CssName = "double", CssSize = 2.5m }},
            { "thinThickSmallGap", new BorderMappingInfo() { CssName = "double", CssSize = 4.5m }},
            { "thickThinSmallGap", new BorderMappingInfo() { CssName = "double", CssSize = 4.5m }},
            { "thinThickThinSmallGap", new BorderMappingInfo() { CssName = "double", CssSize = 6.0m }},
            { "thickThinMediumGap", new BorderMappingInfo() { CssName = "double", CssSize = 6.0m }},
            { "thinThickMediumGap", new BorderMappingInfo() { CssName = "double", CssSize = 6.0m }},
            { "thinThickThinMediumGap", new BorderMappingInfo() { CssName = "double", CssSize = 9.0m }},
            { "thinThickLargeGap", new BorderMappingInfo() { CssName = "double", CssSize = 5.25m }},
            { "thickThinLargeGap", new BorderMappingInfo() { CssName = "double", CssSize = 5.25m }},
            { "thinThickThinLargeGap", new BorderMappingInfo() { CssName = "double", CssSize = 9.0m }},
            { "wave", new BorderMappingInfo() { CssName = "solid", CssSize = 3.0m }},
            { "doubleWave", new BorderMappingInfo() { CssName = "double", CssSize = 5.25m }},
            { "dashDotStroked", new BorderMappingInfo() { CssName = "solid", CssSize = 3.0m }},
            { "threeDEmboss", new BorderMappingInfo() { CssName = "ridge", CssSize = 6.0m }},
            { "threeDEngrave", new BorderMappingInfo() { CssName = "groove", CssSize = 6.0m }},
            { "outset", new BorderMappingInfo() { CssName = "outset", CssSize = 4.5m }},
            { "inset", new BorderMappingInfo() { CssName = "inset", CssSize = 4.5m }},
        };

        private static void GenerateBorderStyle(XElement pBdr, XName sideXName, Dictionary<string, string> style, BorderType borderType)
        {
            string whichSide;
            if (sideXName == W.top)
                whichSide = "top";
            else if (sideXName == W.right)
                whichSide = "right";
            else if (sideXName == W.bottom)
                whichSide = "bottom";
            else
                whichSide = "left";
            if (pBdr == null)
            {
                style.Add("border-" + whichSide, "none");
                if (borderType == BorderType.Cell &&
                    (whichSide == "left" || whichSide == "right"))
                    style.Add("padding-" + whichSide, "5.4pt");
                return;
            }

            var side = pBdr.Element(sideXName);
            if (side == null)
            {
                style.Add("border-" + whichSide, "none");
                if (borderType == BorderType.Cell &&
                    (whichSide == "left" || whichSide == "right"))
                    style.Add("padding-" + whichSide, "5.4pt");
                return;
            }
            var type = (string)side.Attribute(W.val);
            if (type == "nil" || type == "none")
            {
                style.Add("border-" + whichSide + "-style", "none");

                var space = (decimal?)side.Attribute(W.space) ?? 0;
                if (borderType == BorderType.Cell &&
                    (whichSide == "left" || whichSide == "right"))
                    if (space < 5.4m)
                        space = 5.4m;
                style.Add("padding-" + whichSide,
                    space == 0 ? "0" : string.Format(NumberFormatInfo.InvariantInfo, "{0:0.0}pt", space));

            }
            else
            {
                var sz = (int)side.Attribute(W.sz);
                var space = (decimal?)side.Attribute(W.space) ?? 0;
                var color = (string)side.Attribute(W.color);
                if (color == null || color == "auto")
                    color = "windowtext";
                else
                    color = ConvertColor(color);

                decimal borderWidthInPoints = Math.Max(1m, Math.Min(96m, Math.Max(2m, sz)) / 8m);

                var borderStyle = "solid";
                if (BorderStyleMap.ContainsKey(type))
                {
                    var borderInfo = BorderStyleMap[type];
                    borderStyle = borderInfo.CssName;
                    if (type == "double")
                    {
                        if (sz <= 8)
                            borderWidthInPoints = 2.5m;
                        else if (sz <= 18)
                            borderWidthInPoints = 6.75m;
                        else
                            borderWidthInPoints = sz / 3m;
                    }
                    else if (type == "triple")
                    {
                        if (sz <= 8)
                            borderWidthInPoints = 8m;
                        else if (sz <= 18)
                            borderWidthInPoints = 11.25m;
                        else
                            borderWidthInPoints = 11.25m;
                    }
                    else if (type.ToLower().Contains("dash"))
                    {
                        if (sz <= 4)
                            borderWidthInPoints = 1m;
                        else if (sz <= 12)
                            borderWidthInPoints = 1.5m;
                        else
                            borderWidthInPoints = 2m;
                    }
                    else if (type != "single")
                        borderWidthInPoints = borderInfo.CssSize;
                }
                if (type == "outset" || type == "inset")
                    color = "";
                var borderWidth = string.Format(NumberFormatInfo.InvariantInfo, "{0:0.0}pt", borderWidthInPoints);

                style.Add("border-" + whichSide, borderStyle + " " + color + " " + borderWidth);
                if (borderType == BorderType.Cell &&
                    (whichSide == "left" || whichSide == "right"))
                    if (space < 5.4m)
                        space = 5.4m;

                style.Add("padding-" + whichSide,
                    space == 0 ? "0" : string.Format(NumberFormatInfo.InvariantInfo, "{0:0.0}pt", space));
            }
        }

        private static readonly Dictionary<string, Func<string, string, string>> ShadeMapper = new Dictionary<string, Func<string, string, string>>()
        {
            { "auto", (c, f) => c },
            { "clear", (c, f) => f },
            { "nil", (c, f) => f },
            { "solid", (c, f) => c },
            { "diagCross", (c, f) => ConvertColorFillPct(c, f, .75) },
            { "diagStripe", (c, f) => ConvertColorFillPct(c, f, .75) },
            { "horzCross", (c, f) => ConvertColorFillPct(c, f, .5) },
            { "horzStripe", (c, f) => ConvertColorFillPct(c, f, .5) },
            { "pct10", (c, f) => ConvertColorFillPct(c, f, .1) },
            { "pct12", (c, f) => ConvertColorFillPct(c, f, .125) },
            { "pct15", (c, f) => ConvertColorFillPct(c, f, .15) },
            { "pct20", (c, f) => ConvertColorFillPct(c, f, .2) },
            { "pct25", (c, f) => ConvertColorFillPct(c, f, .25) },
            { "pct30", (c, f) => ConvertColorFillPct(c, f, .3) },
            { "pct35", (c, f) => ConvertColorFillPct(c, f, .35) },
            { "pct37", (c, f) => ConvertColorFillPct(c, f, .375) },
            { "pct40", (c, f) => ConvertColorFillPct(c, f, .4) },
            { "pct45", (c, f) => ConvertColorFillPct(c, f, .45) },
            { "pct50", (c, f) => ConvertColorFillPct(c, f, .50) },
            { "pct55", (c, f) => ConvertColorFillPct(c, f, .55) },
            { "pct60", (c, f) => ConvertColorFillPct(c, f, .60) },
            { "pct62", (c, f) => ConvertColorFillPct(c, f, .625) },
            { "pct65", (c, f) => ConvertColorFillPct(c, f, .65) },
            { "pct70", (c, f) => ConvertColorFillPct(c, f, .7) },
            { "pct75", (c, f) => ConvertColorFillPct(c, f, .75) },
            { "pct80", (c, f) => ConvertColorFillPct(c, f, .8) },
            { "pct85", (c, f) => ConvertColorFillPct(c, f, .85) },
            { "pct87", (c, f) => ConvertColorFillPct(c, f, .875) },
            { "pct90", (c, f) => ConvertColorFillPct(c, f, .9) },
            { "pct95", (c, f) => ConvertColorFillPct(c, f, .95) },
            { "reverseDiagStripe", (c, f) => ConvertColorFillPct(c, f, .5) },
            { "thinDiagCross", (c, f) => ConvertColorFillPct(c, f, .5) },
            { "thinDiagStripe", (c, f) => ConvertColorFillPct(c, f, .25) },
            { "thinHorzCross", (c, f) => ConvertColorFillPct(c, f, .3) },
            { "thinHorzStripe", (c, f) => ConvertColorFillPct(c, f, .25) },
            { "thinReverseDiagStripe", (c, f) => ConvertColorFillPct(c, f, .25) },
            { "thinVertStripe", (c, f) => ConvertColorFillPct(c, f, .25) },
        };

        private static readonly Dictionary<string, string> ShadeCache = new Dictionary<string, string>();

        // fill is the background, color is the foreground
        private static string ConvertColorFillPct(string color, string fill, double pct)
        {
            if (color == "auto")
                color = "000000";
            if (fill == "auto")
                fill = "ffffff";
            var key = color + fill + pct.ToString(CultureInfo.InvariantCulture);
            if (ShadeCache.ContainsKey(key))
                return ShadeCache[key];
            var fillRed = Convert.ToInt32(fill.Substring(0, 2), 16);
            var fillGreen = Convert.ToInt32(fill.Substring(2, 2), 16);
            var fillBlue = Convert.ToInt32(fill.Substring(4, 2), 16);
            var colorRed = Convert.ToInt32(color.Substring(0, 2), 16);
            var colorGreen = Convert.ToInt32(color.Substring(2, 2), 16);
            var colorBlue = Convert.ToInt32(color.Substring(4, 2), 16);
            var finalRed = (int)(fillRed - (fillRed - colorRed) * pct);
            var finalGreen = (int)(fillGreen - (fillGreen - colorGreen) * pct);
            var finalBlue = (int)(fillBlue - (fillBlue - colorBlue) * pct);
            var returnValue = string.Format("{0:x2}{1:x2}{2:x2}", finalRed, finalGreen, finalBlue);
            ShadeCache.Add(key, returnValue);
            return returnValue;
        }

        private static void CreateStyleFromShd(Dictionary<string, string> style, XElement shd)
        {
            if (shd == null)
                return;
            var shadeType = (string)shd.Attribute(W.val);
            var color = (string)shd.Attribute(W.color);
            var fill = (string)shd.Attribute(W.fill);
            if (ShadeMapper.ContainsKey(shadeType))
            {
                color = ShadeMapper[shadeType](color, fill);
            }
            if (color != null)
            {
                var cvtColor = ConvertColor(color);
                if (!string.IsNullOrEmpty(cvtColor))
                    style.AddIfMissing("background", cvtColor);
            }
        }

        private static readonly Dictionary<string, string> NamedColors = new Dictionary<string, string>()
        {
            {"black", "black"},
            {"blue", "blue" },
            {"cyan", "aqua" },
            {"green", "green" },
            {"magenta", "fuchsia" },
            {"red", "red" },
            {"yellow", "yellow" },
            {"white", "white" },
            {"darkBlue", "#00008B" },
            {"darkCyan", "#008B8B" },
            {"darkGreen", "#006400" },
            {"darkMagenta", "#800080" },
            {"darkRed", "#8B0000" },
            {"darkYellow", "#808000" },
            {"darkGray", "#A9A9A9" },
            {"lightGray", "#D3D3D3" },
            {"none", "" },
        };

        private static void CreateColorProperty(string propertyName, string color, Dictionary<string, string> style)
        {
            if (color == null)
                return;

            // "auto" color is black for "color" and white for "background" property.
            if (color == "auto")
                color = propertyName == "color" ? "black" : "white";

            if (NamedColors.ContainsKey(color))
            {
                var lc = NamedColors[color];
                if (lc == "")
                    return;
                style.AddIfMissing(propertyName, lc);
                return;
            }
            style.AddIfMissing(propertyName, "#" + color);
        }

        private static string ConvertColor(string color)
        {
            // "auto" color is black for "color" and white for "background" property.
            // As this method is only called for "background" colors, "auto" is translated
            // to "white" and never "black".
            if (color == "auto")
                color = "white";

            if (NamedColors.ContainsKey(color))
            {
                var lc = NamedColors[color];
                if (lc == "")
                    return "black";
                return lc;
            }
            return "#" + color;
        }

        private static readonly Dictionary<string, string> FontFallback = new Dictionary<string, string>()
        {
            { "Arial", @"'{0}', 'sans-serif'" },
            { "Arial Narrow", @"'{0}', 'sans-serif'" },
            { "Arial Rounded MT Bold", @"'{0}', 'sans-serif'" },
            { "Arial Unicode MS", @"'{0}', 'sans-serif'" },
            { "Baskerville Old Face", @"'{0}', 'serif'" },
            { "Berlin Sans FB", @"'{0}', 'sans-serif'" },
            { "Berlin Sans FB Demi", @"'{0}', 'sans-serif'" },
            { "Calibri Light", @"'{0}', 'sans-serif'" },
            { "Gill Sans MT", @"'{0}', 'sans-serif'" },
            { "Gill Sans MT Condensed", @"'{0}', 'sans-serif'" },
            { "Lucida Sans", @"'{0}', 'sans-serif'" },
            { "Lucida Sans Unicode", @"'{0}', 'sans-serif'" },
            { "Segoe UI", @"'{0}', 'sans-serif'" },
            { "Segoe UI Light", @"'{0}', 'sans-serif'" },
            { "Segoe UI Semibold", @"'{0}', 'sans-serif'" },
            { "Tahoma", @"'{0}', 'sans-serif'" },
            { "Trebuchet MS", @"'{0}', 'sans-serif'" },
            { "Verdana", @"'{0}', 'sans-serif'" },
            { "Book Antiqua", @"'{0}', 'serif'" },
            { "Bookman Old Style", @"'{0}', 'serif'" },
            { "Californian FB", @"'{0}', 'serif'" },
            { "Cambria", @"'{0}', 'serif'" },
            { "Constantia", @"'{0}', 'serif'" },
            { "Garamond", @"'{0}', 'serif'" },
            { "Lucida Bright", @"'{0}', 'serif'" },
            { "Lucida Fax", @"'{0}', 'serif'" },
            { "Palatino Linotype", @"'{0}', 'serif'" },
            { "Times New Roman", @"'{0}', 'serif'" },
            { "Wide Latin", @"'{0}', 'serif'" },
            { "Courier New", @"'{0}'" },
            { "Lucida Console", @"'{0}'" },
        };

        private static void CreateFontCssProperty(string font, Dictionary<string, string> style)
        {
            if (FontFallback.ContainsKey(font))
            {
                style.AddIfMissing("font-family", string.Format(FontFallback[font], font));
                return;
            }
            style.AddIfMissing("font-family", font);
        }

        private static bool GetBoolProp(XElement runProps, XName xName)
        {
            var p = runProps.Element(xName);
            if (p == null)
                return false;
            var v = p.Attribute(W.val);
            if (v == null)
                return true;
            var s = v.Value.ToLower();
            if (s == "0" || s == "false")
                return false;
            if (s == "1" || s == "true")
                return true;
            return false;
        }

        private static object ConvertContentThatCanContainFields(WordprocessingDocument wordDoc, DocxToHtmlSettings settings,
            IEnumerable<XElement> elements, DocxToHtmlTransformMode txMode, DocxToHtmlTransformState txState)
        {
            var grouped = elements
                .GroupAdjacent(e =>
                {
                    var stack = e.Annotation<Stack<FieldRetriever.FieldElementTypeInfo>>();
                    return stack == null || !stack.Any() ? (int?)null : stack.Select(st => st.Id).Min();
                })
                .ToList();

            var txformed = grouped
                .Select(g =>
                {
                    var key = g.Key;
                    if (key == null)
                        return (object)g.Select(n => ConvertToHtmlTransform(wordDoc, settings, n, false, 0m, txMode, txState));

                    var instrText = FieldRetriever.InstrText(g.First().Ancestors().Last(), (int)key)
                        .TrimStart('{').TrimEnd('}');

                    var parsed = FieldRetriever.ParseField(instrText);
                    if (parsed.FieldType != "HYPERLINK")
                        return g.Select(n => ConvertToHtmlTransform(wordDoc, settings, n, false, 0m, txMode, txState));

                    var content = g.DescendantsAndSelf(W.r).Select(run => ConvertRun(wordDoc, settings, run, txMode, txState));
                    var a = parsed.Arguments.Length > 0
                        ? new XElement(Xhtml.a, new XAttribute("href", parsed.Arguments[0]), content)
                        : new XElement(Xhtml.a, content);
                    var a2 = a as XElement;
                    if (!a2.Nodes().Any())
                    {
                        a2.Add(new XText(""));
                        return a2;
                    }
                    return a;
                })
                .ToList();

            return txformed;
        }

        #region Image Processing

        // Don't process wmf files (with contentType == "image/x-wmf") because GDI consumes huge amounts
        // of memory when dealing with wmf perhaps because it loads a DLL to do the rendering?
        // It actually works, but is not recommended.
        private static readonly List<string> ImageContentTypes = new List<string>
        {
            "image/png", "image/gif", "image/tiff", "image/jpeg"
        };


        public static XElement ProcessImage(WordprocessingDocument wordDoc,
            XElement element, Func<DocxToHtmlImageInfo, XElement> imageHandler)
        {
            if (imageHandler == null)
            {
                return null;
            }
            if (element.Name == W.drawing)
            {
                return ProcessDrawing(wordDoc, element, imageHandler);
            }
            if (element.Name == W.pict || element.Name == W._object)
            {
                return ProcessPictureOrObject(wordDoc, element, imageHandler);
            }
            return null;
        }

        private static XElement ProcessDrawing(WordprocessingDocument wordDoc,
            XElement element, Func<DocxToHtmlImageInfo, XElement> imageHandler)
        {
            var containerElement = element.Elements()
                .FirstOrDefault(e => e.Name == WP.inline || e.Name == WP.anchor);
            if (containerElement == null) return null;

            string hyperlinkUri = null;
            var hyperlinkElement = element
                .Elements(WP.inline)
                .Elements(WP.docPr)
                .Elements(A.hlinkClick)
                .FirstOrDefault();
            if (hyperlinkElement != null)
            {
                var rId = (string)hyperlinkElement.Attribute(R.id);
                if (rId != null)
                {
                    var hyperlinkRel = wordDoc.MainDocumentPart.HyperlinkRelationships.FirstOrDefault(hlr => hlr.Id == rId);
                    if (hyperlinkRel != null)
                    {
                        hyperlinkUri = hyperlinkRel.Uri.ToString();
                    }
                }
            }

            var extentCx = (int?)containerElement.Elements(WP.extent)
                .Attributes(NoNamespace.cx).FirstOrDefault();
            var extentCy = (int?)containerElement.Elements(WP.extent)
                .Attributes(NoNamespace.cy).FirstOrDefault();
            var altText = (string)containerElement.Elements(WP.docPr).Attributes(NoNamespace.descr).FirstOrDefault() ??
                          ((string)containerElement.Elements(WP.docPr).Attributes(NoNamespace.name).FirstOrDefault() ?? "");

            var blipFill = containerElement.Elements(A.graphic)
                .Elements(A.graphicData)
                .Elements(Pic._pic).Elements(Pic.blipFill).FirstOrDefault();
            if (blipFill == null) return null;

            var imageRid = (string)blipFill.Elements(A.blip).Attributes(R.embed).FirstOrDefault();
            if (imageRid == null) return null;

            var pp3 = wordDoc.MainDocumentPart.Parts.FirstOrDefault(pp => pp.RelationshipId == imageRid);
            if (pp3 == null) return null;

            var imagePart = (ImagePart)pp3.OpenXmlPart;
            if (imagePart == null) return null;

            // If the image markup points to a NULL image, then following will throw an ArgumentOutOfRangeException
            try
            {
                imagePart = (ImagePart)wordDoc.MainDocumentPart.GetPartById(imageRid);
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }

            var contentType = imagePart.ContentType;
            if (!ImageContentTypes.Contains(contentType))
                return null;

            using (var partStream = imagePart.GetStream())
            using (var bitmap = new Bitmap(partStream))
            {
                if (extentCx != null && extentCy != null)
                {
                    var imageInfo = new DocxToHtmlImageInfo()
                    {
                        Bitmap = bitmap,
                        ImgStyleAttribute = new XAttribute("style",
                            string.Format(NumberFormatInfo.InvariantInfo,
                                "width: {0}in; height: {1}in",
                                (float)extentCx / (float)DocxToHtmlImageInfo.EmusPerInch,
                                (float)extentCy / (float)DocxToHtmlImageInfo.EmusPerInch)),
                        ContentType = contentType,
                        DrawingElement = element,
                        AltText = altText,
                    };
                    var imgElement2 = imageHandler(imageInfo);
                    if (hyperlinkUri != null)
                    {
                        return new XElement(XhtmlNoNamespace.a,
                            new XAttribute(XhtmlNoNamespace.href, hyperlinkUri),
                            imgElement2);
                    }
                    return imgElement2;
                }

                var imageInfo2 = new DocxToHtmlImageInfo()
                {
                    Bitmap = bitmap,
                    ContentType = contentType,
                    DrawingElement = element,
                    AltText = altText,
                };
                var imgElement = imageHandler(imageInfo2);
                if (hyperlinkUri != null)
                {
                    return new XElement(XhtmlNoNamespace.a,
                        new XAttribute(XhtmlNoNamespace.href, hyperlinkUri),
                        imgElement);
                }
                return imgElement;
            }
        }

        private static XElement ProcessPictureOrObject(WordprocessingDocument wordDoc,
            XElement element, Func<DocxToHtmlImageInfo, XElement> imageHandler)
        {
            var imageRid = (string)element.Elements(VML.shape).Elements(VML.imagedata).Attributes(R.id).FirstOrDefault();
            if (imageRid == null) return null;

            try
            {
                var pp = wordDoc.MainDocumentPart.Parts.FirstOrDefault(pp2 => pp2.RelationshipId == imageRid);
                if (pp == null) return null;

                var imagePart = (ImagePart)pp.OpenXmlPart;
                if (imagePart == null) return null;

                var contentType = imagePart.ContentType;
                if (!ImageContentTypes.Contains(contentType))
                    return null;

                using (var partStream = imagePart.GetStream())
                {
                    try
                    {
                        using (var bitmap = new Bitmap(partStream))
                        {
                            var imageInfo = new DocxToHtmlImageInfo()
                            {
                                Bitmap = bitmap,
                                ContentType = contentType,
                                DrawingElement = element
                            };

                            var style = (string)element.Elements(VML.shape).Attributes("style").FirstOrDefault();
                            if (style == null) return imageHandler(imageInfo);

                            var tokens = style.Split(';');
                            var widthInPoints = WidthInPoints(tokens);
                            var heightInPoints = HeightInPoints(tokens);
                            if (widthInPoints != null && heightInPoints != null)
                            {
                                imageInfo.ImgStyleAttribute = new XAttribute("style",
                                    string.Format(NumberFormatInfo.InvariantInfo,
                                        "width: {0}pt; height: {1}pt", widthInPoints, heightInPoints));
                            }
                            return imageHandler(imageInfo);
                        }
                    }
                    catch (OutOfMemoryException)
                    {
                        // the Bitmap class can throw OutOfMemoryException, which means the bitmap is messed up, so punt.
                        return null;
                    }
                    catch (ArgumentException)
                    {
                        return null;
                    }
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
        }

        private static float? HeightInPoints(IEnumerable<string> tokens)
        {
            return SizeInPoints(tokens, "height");
        }

        private static float? WidthInPoints(IEnumerable<string> tokens)
        {
            return SizeInPoints(tokens, "width");
        }

        private static float? SizeInPoints(IEnumerable<string> tokens, string name)
        {
            var sizeString = tokens
                .Select(t => new
                {
                    Name = t.Split(':').First(),
                    Value = t.Split(':').Skip(1).Take(1).FirstOrDefault()
                })
                .Where(p => p.Name == name)
                .Select(p => p.Value)
                .FirstOrDefault();

            if (sizeString != null &&
                sizeString.Length > 2 &&
                sizeString.Substring(sizeString.Length - 2) == "pt")
            {
                float size;
                if (float.TryParse(sizeString.Substring(0, sizeString.Length - 2), out size))
                    return size;
            }
            return null;
        }

        #endregion
    }

    internal class CssClassAnnotation
    {
        public string Class;
    }

    public static class HtmlConverterExtensions
    {
        public static void AddIfMissing(this Dictionary<string, string> style, string propName, string value)
        {
            if (style.ContainsKey(propName))
                return;
            style.Add(propName, value);
        }
    }
}
