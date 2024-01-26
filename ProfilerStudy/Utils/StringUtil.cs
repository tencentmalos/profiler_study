using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace FramePro
{
    public static class StringUtil
    {
        public static string GetCombinedString(List<string> strArray, string combinedSeparator)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < strArray.Count; i++)
            {
                if (i == 0)
                {
                    sb.Append(strArray[i]);
                }
                else
                {
                    sb.Append(combinedSeparator);
                    sb.Append(strArray[i]);
                }
            }

            return sb.ToString();
        }

        public static void CombineToStringBuffer(StringBuilder outputSb, List<string> strArray, string combinedSeparator)
        {
            for (int i = 0; i < strArray.Count; i++)
            {
                if (i == 0)
                {
                    outputSb.Append(strArray[i]);
                }
                else
                {
                    outputSb.Append(combinedSeparator);
                    outputSb.Append(strArray[i]);
                }
            }
        }

        public static string WildcardToRegex(string pattern)
        {
            //. 为正则表达式的通配符，表示：与除 \n 之外的任何单个字符匹配。
            //* 为正则表达式的限定符，表示：匹配上一个元素零次或多次
            //? 为正则表达式的限定符，表示：匹配上一个元素零次或一次
            return "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
        }

        public static bool IsMatchWildcard(string pattern, string detectTarget)
        {
            string needPattern = WildcardToRegex(pattern);
            return Regex.IsMatch(detectTarget, needPattern);

        }

        public static string ReplicateString(string org, int replicateCount)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < replicateCount; i++)
            {
                sb.Append(org);
            }
            return sb.ToString();
        }

        public static string ReplaceToLinuxLineEnd(string content)
        {
            return content.Replace("\r\n", "\n");
        }

        public static bool HasChinese(string content)
        {
            return Regex.IsMatch(content, @"[\u4e00-\u9fa5]");
        }

        public static string ParseToEmbededPath(string relativePath)
        {
            relativePath = relativePath.Replace("/", ".");
            relativePath = relativePath.Replace("\\", ".");

            return $"SNTableTools.{relativePath}";
        }
    }


}
