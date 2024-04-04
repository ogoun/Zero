﻿using System.Collections.Generic;
using System.Linq;

namespace ZeroLevel.Implementation.Semantic.Helpers
{
    public static class StopWords
    {
        public static string[] Stopwords => _stop_words.ToArray();
        public static string[] StopHtml => _stop_words.ToArray();
        public static string[] StopAll => _stop_words.Concat(_html_tags).ToArray();

        private readonly static HashSet<string> _stop_words = new HashSet<string> { "a", "about", "all", "am", "an", "and", "any", "are", "as", "at", "be", "been", "but", "by", "can", "could", "do", "for", "from", "has", "have", "i", "if", "in", "is", "it", "me", "my", "no", "not", "of", "on", "one", "or", "so", "that", "the", "them", "there", "they", "this", "to", "was", "we", "what", "which", "will", "with", "would", "you", "а", "будем", "будет", "будете", "будешь", "буду", "будут", "будучи", "будь", "будьте", "бы", "был", "была", "были", "было", "быть", "в", "вам", "вами", "вас", "весь", "во", "вот", "все", "всё", "всего", "всей", "всем", "всём", "всеми", "всему", "всех", "всею", "всея", "всю", "вся", "вы", "да", "для", "до", "его", "едим", "едят", "ее", "её", "ей", "ел", "ела", "ем", "ему", "емъ", "если", "ест", "есть", "ешь", "еще", "ещё", "ею", "же", "за", "и", "из", "или", "им", "ими", "имъ", "их", "к", "как", "кем", "ко", "когда", "кого", "ком", "кому", "комья", "которая", "которого", "которое", "которой", "котором", "которому", "которою", "которую", "которые", "который", "которым", "которыми", "которых", "кто", "меня", "мне", "мной", "мною", "мог", "моги", "могите", "могла", "могли", "могло", "могу", "могут", "мое", "моё", "моего", "моей", "моем", "моём", "моему", "моею", "можем", "может", "можете", "можешь", "мои", "мой", "моим", "моими", "моих", "мочь", "мою", "моя", "мы", "на", "нам", "нами", "нас", "наса", "наш", "наша", "наше", "нашего", "нашей", "нашем", "нашему", "нашею", "наши", "нашим", "нашими", "наших", "нашу", "не", "него", "нее", "неё", "ней", "нем", "нём", "нему", "нет", "нею", "ним", "ними", "них", "но", "о", "об", "один", "одна", "одни", "одним", "одними", "одних", "одно", "одного", "одной", "одном", "одному", "одною", "одну", "ой", "он", "она", "оне", "они", "оно", "от", "по", "при", "с", "сам", "сама", "сами", "самим", "самими", "самих", "само", "самого", "самом", "самому", "саму", "свое", "своё", "своего", "своей", "своем", "своём", "своему", "своею", "свои", "свой", "своим", "своими", "своих", "свою", "своя", "себе", "себя", "собой", "собою", "та", "так", "такая", "такие", "таким", "такими", "таких", "такого", "такое", "такой", "таком", "такому", "такою", "такую", "те", "тебе", "тебя", "тем", "теми", "тех", "то", "тобой", "тобою", "того", "той", "только", "том", "томах", "тому", "тот", "тою", "ту", "ты", "у", "уже", "чего", "чем", "чём", "чему", "что", "чтобы", "эта", "эти", "этим", "этими", "этих", "это", "этого", "этой", "этом", "этому", "этот", "этою", "эту", "я", "ещë", "еë", "моë", "моëм", "всë", "кто-то ", "что-то", "мені", "наші", "нашої", "нашій", "нашою", "нашім", "ті", "тієї", "тією", "тії", "теє" };
        private readonly static HashSet<string> _html_tags = new HashSet<string> { "doctype", "a", "accesskey", "charset", "coords", "download", "href", "hreflang", "name", "rel", "rev", "shape", "tabindex", "target", "title", "type", "abbr", "title", "acronym", "address", "applet", "align", "alt", "archive", "code", "codebase", "height", "hspace", "vspace", "width", "area", "accesskey", "alt", "coords", "href", "hreflang", "nohref", "shape", "tabindex", "target", "type", "article", "aside", "audio", "autoplay", "controls", "loop", "muted", "preload", "src", "b", "base", "href", "target", "basefont", "color", "face", "size", "bdi", "bdo", "dir", "bgsound", "balance", "loop", "src", "volume", "big", "blink", "blockquote", "body", "alink", "background", "bgcolor", "bgproperties", "bottommargin", "leftmargin", "link", "rightmargin", "scroll", "text", "topmargin", "vlink", "br", "clear", "button", "accesskey", "autofocus", "disabled", "form", "formaction", "formenctype", "formmethod", "formnovalidate", "formtarget", "name", "type", "value", "canvas", "caption", "align", "valign", "center", "cite", "code", "col", "align", "char", "charoff", "span", "valign", "width", "colgroup", "align", "char", "charoff", "span", "valign", "width", "command", "comment", "datalist", "dd", "del", "cite", "datetime", "details", "dfn", "dir", "div", "align", "title", "dl", "dt", "em", "embed", "align", "height", "hidden", "hspace", "pluginspage", "src", "type", "vspace", "width", "fieldset", "disabled", "form", "title", "figcaption", "figure", "font", "color", "face", "size", "footer", "form", "accept-charset", "action", "autocomplete", "enctype", "method", "name", "novalidate", "target", "frame", "bordercolor", "frameborder", "name", "noresize", "scrolling", "src", "frameset", "border", "bordercolor", "cols", "frameborder", "framespacing", "rows", "h1", "align", "h2", "align", "h3", "align", "h4", "align", "h5", "align", "h6", "align", "head", "profile", "header", "hgroup", "hr", "align", "color", "noshade", "size", "width", "html", "manifest", "title", "xmlns", "i", "iframe", "align", "allowtransparency", "frameborder", "height", "hspace", "marginheight", "marginwidth", "name", "sandbox", "scrolling", "seamless", "src", "srcdoc", "vspace", "width", "img", "align", "alt", "border", "height", "hspace", "ismap", "longdesc", "lowsrc", "src", "usemap", "vspace", "width", "input", "accept", "accesskey", "align", "alt", "autocomplete", "autofocus", "border", "checked", "disabled", "form", "formaction", "formenctype", "formmethod", "formnovalidate", "formtarget", "list", "max", "maxlength", "min", "multiple", "name", "pattern", "placeholder", "readonly", "required", "size", "src", "step", "tabindex", "type", "value", "ins", "cite", "datetime", "isindex", "kbd", "keygen", "label", "accesskey", "for", "legend", "accesskey", "align", "title", "li", "type", "value", "link", "charset", "href", "media", "rel", "sizes", "type", "listing", "main", "map", "name", "mark", "marquee", "behavior", "bgcolor", "direction", "height", "hspace", "loop", "scrollamount", "scrolldelay", "truespeed", "vspace", "width", "menu", "label", "type", "meta", "charset", "content", "http-equiv", "name", "meter", "high", "low", "max", "min", "optimum", "value", "multicol", "nav", "nobr", "noembed", "noframes", "noscript", "object", "align", "archive", "classid", "code", "codebase", "codetype", "data", "height", "hspace", "tabindex", "type", "vspace", "width", "ol", "reversed", "start", "type", "optgroup", "disabled", "label", "option", "disabled", "label", "selected", "value", "output", "p", "align", "param", "name", "type", "value", "valuetype", "plaintext", "pre", "progress", "q", "rp", "rt", "ruby", "s", "samp", "script", "async", "defer", "language", "src", "type", "section", "select", "accesskey", "autofocus", "disabled", "form", "multiple", "name", "required", "size", "tabindex", "small", "source", "media", "src", "type", "spacer", "span", "strike", "strong", "style", "media", "type", "sub", "summary", "sup", "table", "align", "background", "bgcolor", "border", "bordercolor", "cellpadding", "cellspacing", "cols", "frame", "height", "rules", "summary", "width", "tbody", "align", "bgcolor", "char", "charoff", "valign", "td", "abbr", "align", "axis", "background", "bgcolor", "bordercolor", "char", "charoff", "colspan", "headers", "height", "nowrap", "rowspan", "scope", "valign", "width", "textarea", "accesskey", "autofocus", "cols", "disabled", "form", "maxlength", "name", "placeholder", "readonly", "required", "rows", "tabindex", "wrap", "tfoot", "align", "bgcolor", "char", "charoff", "valign", "th", "abbr", "align", "axis", "background", "bgcolor", "bordercolor", "char", "charoff", "colspan", "headers", "height", "nowrap", "rowspan", "scope", "valign", "width", "thead", "align", "bgcolor", "char", "charoff", "valign", "time", "datetime", "pubdate", "title", "tr", "align", "bgcolor", "bordercolor", "char", "charoff", "valign", "track", "tt", "u", "ul", "type", "var", "video", "autoplay", "controls", "height", "loop", "poster", "preload", "src", "width", "wbr", "xmp" };

        public static bool IsStopWord(string word)
        {
            return _stop_words.Contains(word.Trim().ToLowerInvariant());
        }

        public static bool IsHtmlTag(string word)
        {
            return _html_tags.Contains(word.Trim().ToLowerInvariant());
        }
    }
}