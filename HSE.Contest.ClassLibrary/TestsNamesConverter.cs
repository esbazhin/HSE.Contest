using System.Collections.Generic;

namespace HSE.Contest.ClassLibrary
{
    public static class TestsNamesConverter
    {
        static Dictionary<string, string> ConvertDict = new Dictionary<string, string>
        {
            {"codeStyleTest", "Тест на код-стайл"},
            {"reflectionTest", "Тест на рефлекшн"},
            {"functionalTest", "Тест на ввод-вывод"}
        };

        public static string ConvertTypeToName(string type)
        {
            return IsValidType(type) ? ConvertDict[type] : type;
        }

        public static bool IsValidType(string type)
        {
            return ConvertDict.ContainsKey(type);
        }
    }
}
