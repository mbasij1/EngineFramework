using System;
using System.Collections.Generic;
using System.Text;

namespace EngineFramework.Extensions
{
    public static class StringExtensions
    {
        public static string PersianUnify(this string text) => text
            .Replace("ي", "ی")
            .Replace("ك", "ک")
            .Replace("دِ", "د")
            .Replace("بِ", "ب")
            .Replace("زِ", "ز")
            .Replace("ذِ", "ذ")
            .Replace("شِ", "ش")
            .Replace("سِ", "س")
            .Replace("ضِ", "ض")
            .Replace("فِ", "ف");
    }
}
