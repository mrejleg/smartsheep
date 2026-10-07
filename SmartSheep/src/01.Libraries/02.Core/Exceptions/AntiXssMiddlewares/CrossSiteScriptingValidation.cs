using Microsoft.AspNetCore.Http;

namespace Project.Core.Exceptions.AntiXssMiddlewares
{
    public static class CrossSiteScriptingValidation
    {
        private static readonly char[] StartingChars = { '<', '&' };

        #region Public methods
        public static bool IsDangerousString(string s, out int matchIndex)
        {
            //bool inComment = false;
            matchIndex = 0;

            for (var i = 0; ;)
            {

                // Look for the start of one of our patterns 
                var n = s.IndexOfAny(StartingChars, i);

                // If not found, the string is safe
                if (n < 0) return false;

                // If it's the last char, it's safe 
                if (n == s.Length - 1) return false;

                matchIndex = n;

                switch (s[n])
                {
                    case '<':
                        // If the < is followed by a letter or '!', it's unsafe (looks like a tag or HTML comment)
                        if (IsAtoZ(s[n + 1]) || s[n + 1] == '!' || s[n + 1] == '/' || s[n + 1] == '?') return true;
                        break;
                    case '&':
                        // If the & is followed by a #, it's unsafe (e.g. S) 
                        if (s[n + 1] == '#') return true;
                        break;

                }

                // Continue searching
                i = n + 1;
            }
        }
        #endregion

        #region Private methods
        private static bool IsAtoZ(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        }
        #endregion

        public static void AddHeaders(this IHeaderDictionary headers)
        {
            if (headers["P3P"].IsNullOrEmpty())
            {
                headers["P3P"] = "CP=\"IDC DSP COR ADM DEVi TAIi PSA PSD IVAi IVDi CONi HIS OUR IND CNT\"";
            }
        }

        public static bool IsNullOrEmpty<T>(this IEnumerable<T> source)
        {
            return source == null || !source.Any();
        }

        public static bool IsSafeFile(string content)
        {
            var result = false;

            if (content.ToLower().Contains("bmp") ||
                content.ToLower().Contains("gif") ||
                content.ToLower().Contains("ico") ||
                content.ToLower().Contains("jpg") ||
                content.ToLower().Contains("jpeg") ||
                content.ToLower().Contains("tif") ||
                content.ToLower().Contains("png") ||
                content.ToLower().Contains("tiff") ||
                content.ToLower().Contains("wmf") ||
                content.ToLower().Contains("wmf") ||
                content.ToLower().Contains("xlsx") ||
                content.ToLower().Contains("xls") ||
                content.ToLower().Contains("pdf") 
                )
            {
                result = true;
            }

            return result;
        }
    }
}
