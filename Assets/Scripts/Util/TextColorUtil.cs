using System.Text.RegularExpressions;

namespace Program.Utils
{
    /// <summary>
    /// 文字列にログなどで色付け表示するためのタグの付与＆除去。
    /// </summary>
    public static class TextColorUtil
    {
        /// <summary>
        /// カラー名を指定して、 colorタグで囲む。
        /// 使えるカラー名は何があるのかよくわからん。
        /// </summary>
        /// <param name="message"></param>
        /// <param name="colorName"></param>
        /// <returns></returns>
        public static string WithColor(this string message, string colorName)
        {
            return string.Format("<color=\"{0}\">{1}</color>", colorName, message);
        }

        private static Regex _regex = new Regex(@"</?\s*color\s*[^>]*>");

        /// <summary>
        /// colorタグを取り除いた文字列を返す
        /// </summary>
        /// <param name="message"></param>
        /// <param name="colorName"></param>
        /// <returns></returns>
        public static string TrimColors(this string message)
        {
            return _regex.Replace(message, "", -1 /* = replace all*/);
        }

    #region 規定のカラー

        /// <summary>
        /// 赤色
        /// </summary>
        /// <param name="origin"></param>
        /// <returns></returns>
        public static string red(this string origin)
        {
            return WithColor(origin, "red");
        }

        /// <summary>
        /// 橙色
        /// </summary>
        /// <param name="origin"></param>
        /// <returns></returns>
        public static string orange(this string origin)
        {
            return WithColor(origin, "orange");
        }

        /// <summary>
        /// 鮮やかな黄色
        /// </summary>
        /// <param name="origin"></param>
        /// <returns></returns>
        public static string yellow(this string origin)
        {
            return WithColor(origin, "yellow");
        }

        /// <summary>
        /// 黄土色
        /// </summary>
        /// <param name="origin"></param>
        /// <returns></returns>
        public static string olive(this string origin)
        {
            return WithColor(origin, "olive");
        }

        /// <summary>
        /// 鮮やかな黄緑
        /// </summary>
        /// <param name="origin"></param>
        /// <returns></returns>
        public static string lime(this string origin)
        {
            return WithColor(origin, "lime");
        }

        /// <summary>
        /// 濃い緑
        /// </summary>
        /// <param name="origin"></param>
        /// <returns></returns>
        public static string green(this string origin)
        {
            return WithColor(origin, "green");
        }

        /// <summary>
        /// 鮮やかな水色
        /// </summary>
        /// <param name="origin"></param>
        /// <returns></returns>
        public static string cyan(this string origin)
        {
            return WithColor(origin, "cyan");
        }

        /// <summary>
        /// パステル調水色
        /// </summary>
        /// <param name="origin"></param>
        /// <returns></returns>
        public static string lightblue(this string origin)
        {
            return WithColor(origin, "lightblue");
        }

        /// <summary>
        /// 鮮やかな赤紫色
        /// </summary>
        /// <param name="origin"></param>
        /// <returns></returns>
        public static string magenta(this string origin)
        {
            return WithColor(origin, "magenta");
        }

        /// <summary>
        /// 白色
        /// </summary>
        /// <param name="origin"></param>
        /// <returns></returns>
        public static string white(this string origin)
        {
            return WithColor(origin, "white");
        }

        /// <summary>
        /// 灰色
        /// </summary>
        /// <param name="origin"></param>
        /// <returns></returns>
        public static string gray(this string origin)
        {
            return WithColor(origin, "gray");
        }

        /// <summary>
        /// 黒色
        /// </summary>
        /// <param name="origin"></param>
        /// <returns></returns>
        public static string black(this string origin)
        {
            return WithColor(origin, "black");
        }

    #endregion

    }
}