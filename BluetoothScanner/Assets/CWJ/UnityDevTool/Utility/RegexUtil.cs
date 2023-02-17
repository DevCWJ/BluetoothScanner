using System.Text.RegularExpressions;

namespace CWJ
{
    public static class RegexUtil
    {
        // 출처 : http://www.csharpstudy.com/Practical/Prac-validemail.aspx
        // 정규식 관련기술. 구글링 결과를 참고함
        //Regular Expression

        /// <summary>
        /// <para>문자의 모든 공백을 제거</para>
        /// <para>Regex.Replace보다는 string.Replace가 빠름</para>
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string RemoveAllSpacesWithRegex(this string str)
        {
            return Regex.Replace(str, @"\s", "");
        }

        public static string[] RemoveAllSpacesWithRegex(this string[] strArray)
        {
            if (strArray != null)
            {
                for (int i = 0; i < strArray.Length; i++)
                {
                    if (string.IsNullOrEmpty(strArray[i]))
                    {
                        continue;
                    }
                    strArray[i] = strArray[i].RemoveAllSpacesWithRegex();
                }
            }
            return strArray;
        }

        public static string ExtractNumber(this string str)
        {
            return Regex.Replace(str, @"\D", "");
        }

        public static string ExtractText(this string str)
        {
            return Regex.Replace(str, @"\d", "");
        }

        //public const string EmailPattern = "[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?";
        public const string EmailPattern = @"^([0-9a-zA-Z]" + //Start with a digit or alphabetical
                                            @"([\+\-_\.][0-9a-zA-Z]+)*" + // No continuous or ending +-_. chars in email
                                            @")+" +
                                            @"@(([0-9a-zA-Z][-\w]*[0-9a-zA-Z]*\.)+[a-zA-Z0-9]{2,17})$";

        public const string SpecialPattern = @"[^a-zA-Z\s\d\u3131-\u318E\uAC00-\uD7A3]";

        /// <summary>
        /// 이메일의 형식이 맞는지 확인
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public static bool IsValidEmail(this string email)
        {
            return Regex.IsMatch(email, EmailPattern);
        }

        /// <summary>
        /// <para>문자열 길이 검사 기능</para>
        /// 최소값(min) 이상 최대값(max) 이하 길이인지 검사
        /// </summary>
        /// <param name="str"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static bool IsAround(this string str, int min, int max)
        {
            return (str.Length >= min) && (str.Length <= max);
        }

        /// <summary>
        /// 특수문자를 포함하고있는지
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool HasSpecialCharacter(this string str)
        {
            return !str.Equals(Regex.Replace(str, SpecialPattern, "", RegexOptions.Singleline));
        }

        public static string SplitCamelCase(this string camelCaseString)
        {
            if (string.IsNullOrEmpty(camelCaseString)) return camelCaseString;

            string camelCase = Regex.Replace(Regex.Replace(camelCaseString, @"(\P{Ll})(\P{Ll}\p{Ll})", "$1 $2"), @"(\p{Ll})(\P{Ll})", "$1 $2");
            string firstLetter = camelCase.Substring(0, 1).ToUpper();

            if (camelCaseString.Length > 1)
            {
                string rest = camelCase.Substring(1);

                return firstLetter + rest;
            }
            return firstLetter;
        }
    }
}