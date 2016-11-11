using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;


namespace NetTest
{
    class JParser
    {
        //private string JSource;
        public Regex reWords = new Regex("/[*][^/]*[^*]*[*]/|//.*$|\\s+|(?:\\w|\\.)+|\\W", RegexOptions.Multiline);
        public Regex reQuoted = new Regex("(?<quote>['\"])(?<contents>(?:\\\\.|(?!\\\\)(?!\\1).)*)\\1");
        static string meSingleQuote(Match match)
        {
            if (match.Groups["quote"].Value == "\"") return match.Value;
            string v = match.Groups["contents"].Value;
            string[] vv = Regex.Split(v, "\\\\.");
            for (int i = 0; i < vv.Length; i++)
                if (vv[i] == "\\'") vv[i] = "'";
                else if (vv[i] != "\\\\") vv[i] = vv[i].Replace("\"", "\\\"");
            return "\"" + String.Join(String.Empty, vv) + "\"";
        }
        public MatchEvaluator ReplaceSingleQuote = new MatchEvaluator(meSingleQuote);

        public Regex reFirstWords = new Regex("((?:^|{|}|;)\\s*)(\\w+)", RegexOptions.Multiline);
        static string meFirstWords(Match match)
        {
            switch (match.Groups[2].Value)
            {
                case "import":
                    return match.Groups[1].Value + "using";
                case "package":
                    return match.Groups[1].Value + "namespace";
                case "String":
                    return match.Groups[1].Value + "string";
                case "final":
                    return match.Groups[1].Value + "sealed";
                case "class":
                case "function":
                case "interface":
                    return match.Groups[1].Value + "public " + match.Groups[2].Value;
            }
            return match.Value;
        }
        public MatchEvaluator ReplaceFirstWords = new MatchEvaluator(meFirstWords);

        public Regex reFunctions = new Regex("function\\s+(\\w{3}\\s+)?(\\w+)\\(([^()]*)\\)(\\s*:\\s*(?:\\w+\\.?)*(?:\\[\\])?)?");
        static string meFunctions(Match match)
        {
            StringBuilder s = new StringBuilder();

            if (match.Groups[1].Success)
            {
                if (match.Groups[1].Value.Trim() == "get") s.Append(match.Groups[4].Value.Trim().Substring(1).Trim());
                else
                {
                    string[] ssx = match.Groups[3].Value.Trim().Split(':');
                    s.Append((ssx.Length > 0 && ssx[ssx.Length - 1].Trim().Length > 0) ? ssx[ssx.Length - 1].Trim() : "void");
                }
                s.Append(" " + match.Groups[2].Value);

                s.Append(" {\r\n\t\t\t" + match.Groups[1].Value.Trim());
            }
            else
            {
                s.Append(match.Groups[4].Success ? match.Groups[4].Value.Trim().Substring(1).Trim() : "void");
                s.Append(" " + match.Groups[2].Value);

                s.Append("(");

                if (match.Groups[3].Success && match.Groups[3].Length > 0)
                {
                    string[] ss = match.Groups[3].Value.Trim().Split(',');

                    for (int x = 0; x < ss.Length; x++)
                    {
                        string[] pp = ss[x].Split(":"[0]);
                        ss[x] = ((pp.Length == 1) ? "Object " : (pp[1].Trim() + " ")) + pp[0].Trim();
                    }
                    s.Append(String.Join(",", ss));
                }
                s.Append(")");

            }
            return s.ToString();
        }
        public MatchEvaluator ReplaceFunctions = new MatchEvaluator(meFunctions);

        public Regex reVars1stPass = new Regex("(?:var\\s+)?(\\w+)\\s*(?::\\s*((?:\\w|\\.)+(?:\\[\\])?))?(\\s*=\\s*)((new\\s+)((?:\\w|\\.)+\\[?))?");
        static string meVars1stPass(Match match)
        {
            StringBuilder s = new StringBuilder();
            if (match.Groups[2].Success) s.Append(match.Groups[2].Value + " ");
            else if (match.Groups[6].Success)
            {
                string f = match.Groups[6].Value;
                s.Append(f);
                if (f[f.Length - 1] == '[') s.Append(']');
                s.Append(' ');
            }
            s.Append(match.Groups[1].Value);
            s.Append(match.Groups[3].Value + match.Groups[4].Value);
            return s.ToString();
        }
        public MatchEvaluator ReplaceVars1 = new MatchEvaluator(meVars1stPass);

        public Regex reVarsLastPass = new Regex("(?:var|,)\\s+(\\w+)(?::((?:\\w|\\.)+(?:\\[\\])?))");
        static string meVarsLastPass(Match match)
        {
            return match.Groups[2].Value + " " + match.Groups[1].Value;
        }
        public MatchEvaluator ReplaceVarsLast = new MatchEvaluator(meVarsLastPass);

        public Regex reCatch = new Regex("(}\\s*catch\\s*\\(\\s*)(\\w+)\\s*(:\\s*(?:\\w|\\.)+)?(\\s*\\)\\s*{)");
        static string meCatch(Match match)
        {
            return match.Groups[1].Value + (match.Groups[3].Success ? match.Groups[3].Value.Substring(1) : "Exception") + " " + match.Groups[2].Value + match.Groups[4].Value;
        }
        public MatchEvaluator ReplaceCatch = new MatchEvaluator(meCatch);

        public Regex reEOL = new Regex("(\\*/|[+:;{])?(?:\\s*}?\\s*)*(?://.+)?\\n\\s*([+])?(?!\\s*{)");
        static string meEOL(Match match)
        {
            return ((match.Groups[1].Success || match.Groups[2].Success || match.Index == 0) ? String.Empty : ";") + match.Value;
        }
        public bool OpenCommentExists(string s, bool open)
        {
            MatchCollection mc = Regex.Matches(s, "/\\*|\\*/");
            foreach (Match m in mc) open = m.Value == "/*";
            return open;
        }
        public MatchEvaluator ReplaceEOL = new MatchEvaluator(meEOL);

        public Regex reNew = new Regex("new\\s+\\w+\\s*(\\(|\\[)?");
        static string meNew(Match match)
        {
            return match.Value + (match.Groups[1].Success ? String.Empty : "()");
        }
        public MatchEvaluator ReplaceNew = new MatchEvaluator(meNew);

        public Regex reForEach = new Regex("for(\\s*\\([^;()]+\\))");
        static string meForEach(Match match) { return "foreach" + match.Groups[1].Value; }
        public MatchEvaluator ReplaceForEach = new MatchEvaluator(meForEach);

        public Regex reCAccessors = new Regex("(.+{\\s*(?:s|g)et\\s*{)");

        public Regex reClassInherit = new Regex("(.+class\\s+\\w+)(?:\\s+extends\\s+(\\w+))?(?:\\s+implements\\s+((?:\\w+\\s*,?\\s*)+)){");
        static string meClassInherit(Match match)
        {
            if (!match.Groups[2].Success && !match.Groups[3].Success) return match.Value;
            string suffix = match.Groups[2].Success ? match.Groups[2].Value : String.Empty;
            if (match.Groups[3].Success) suffix = suffix == String.Empty ? match.Groups[3].Value : (suffix + ", " + match.Groups[3].Value);
            return match.Groups[1].Value + " : " + suffix + "{";
        }
        public MatchEvaluator ReplaceClassInherit = new MatchEvaluator(meClassInherit);

    }
    //user_pref("dom.disable_open_during_load", false);
    //user_pref("javascript.enabled", false);
    //user_pref("permissions.default.image", 2);
    //user_pref("security.enable_java", false);
}
