using System.Text;
using UnityEngine;

namespace SSBX
{
    public static class ResourceKeys
    {
        public static string UiBuildingInfoById(string id) => $"ui/building_info/id/{id}";
        public static string UiBuildingInfoByCategory(BuildingCategory cat) => $"ui/building_info/category/{cat}";
        public static string UiBuildingInfoByType(string type) => $"ui/building_info/type/{type}";
        public static string UiBuildingInfoDefault => "ui/building_info/default";

        /// <summary>（可选）把中文等转换为可读的ASCII Key；若你想强制ASCII再启用。</summary>
        public static string Slugify(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var sb = new StringBuilder(s.Length);
            foreach (var ch in s)
            {
                if (char.IsLetterOrDigit(ch)) sb.Append(ch);
                else if (char.IsWhiteSpace(ch) || ch == '_' || ch == '-') sb.Append('_');
                else sb.Append('_');
            }
            return sb.ToString();
        }
    }
}
