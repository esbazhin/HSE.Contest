using HSE.Contest.ClassLibrary.TestsClasses.ReflectionTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ReflectionTesterService
{
    public static class ReflectionTester
    {
        public static async Task<List<SingleReflectionTestResult>> TestClass(ClassDefinition expected, ClassDefinition actual)
        {
            string className = actual.Name;

            var tasks = new List<Task<List<SingleReflectionTestResult>>>
            {
                Task.Run(() => TestCommonInfo(expected, actual)),

                Task.Run(() => expected.ImplementedInterfaces.Select(i => new SingleReflectionTestResult(i, actual.ImplementedInterfaces.Find(intf => intf == i), $"Не имплементирован интерфейс {i} у класса {className}!")).ToList()),

                Task.Run(() => TestConstructors(expected.Constructors, actual.Constructors, className)),

                Task.Run(() => TestFields(expected.Fields, actual.Fields, className)),

                Task.Run(() => TestProperties(expected.Properties, actual.Properties, className)),

                Task.Run(() => TestMethods(expected.Methods, actual.Methods, className)),

                Task.Run(() => TestEvents(expected.Fields, actual.Fields, className))
            };

            var results = await Task.WhenAll(tasks);

            return results.Aggregate(new List<SingleReflectionTestResult>(), (res, cur) =>
            {
                var result = new List<SingleReflectionTestResult>(res);
                result.AddRange(cur);
                return result;
            });
        }

        static List<SingleReflectionTestResult> TestCommonInfo(ClassDefinition expected, ClassDefinition actual)
        {
            string className = expected.Name;
            var result = new List<SingleReflectionTestResult>
            {
                new SingleReflectionTestResult(expected.Base, actual.Base, $"Неверный базовый класс у класса {className}!"),
                new SingleReflectionTestResult(expected.Visibility, actual.Visibility, $"Неверная область видимости у класса {className}!"),
                new SingleReflectionTestResult(expected.Modifier, actual.Modifier, $"Неверный модификатор доступа у класса {className}!")
            };
            return result;
        }
        static List<SingleReflectionTestResult> TestConstructors(List<MethodDefinition> expectedC, List<MethodDefinition> actualC, string className)
        {
            var result = new List<SingleReflectionTestResult>();

            foreach (var expected in expectedC)
            {
                var actual = actualC.FindAll(c => c.Visibility == expected.Visibility && c.Modifier == expected.Modifier && c.Parameters.Count == expected.Parameters.Count);
                if (actual == null || actual.Count == 0)
                {
                    result.Add(new SingleReflectionTestResult($"Не найден {expected.Visibility} {expected.Modifier} конструктор с {expected.Parameters.Count} параметрами в классе {className}!"));
                }
                else
                {
                    var constr = actual.Find(c => c.Parameters.All(p => expected.Parameters.Find(ep => ep.Equals(p)) != null));
                    if (constr == null)
                    {
                        result.Add(new SingleReflectionTestResult($"Не найден {expected.Visibility} {expected.Modifier} конструктор с параметрами {String.Join(' ', expected.Parameters.Select(p => p.Type))} в классе {className}!"));
                    }
                    else
                    {
                        result.AddRange(TestParams(expected.Parameters, constr.Parameters, className));
                    }
                }
            }

            return result;
        }
        static List<SingleReflectionTestResult> TestParams(List<ParameterDefinition> expectedParams, List<ParameterDefinition> actualParams, string className, string mName = null)
        {
            var result = new List<SingleReflectionTestResult>();
            if (expectedParams.Count != actualParams.Count)
            {
                result.Add(new SingleReflectionTestResult($"Не совпадает кол-во параметров у метода {mName} класса {className}!"));
            }
            else
            {
                for (int i = 0; i < expectedParams.Count; i++)
                {
                    var curP = actualParams[i];
                    var expP = expectedParams[i];
                    if (curP.Position != expP.Position)
                    {
                        curP = actualParams.Find(p => p.Position == expP.Position);
                    }
                    if (curP == null)
                    {
                        result.Add(new SingleReflectionTestResult($"Не найден {expP.Position} параметр у метода {mName} класса {className}!"));
                    }
                    else
                    {
                        result.Add(new SingleReflectionTestResult(curP.Type, expP.Type, $"Неверный тип {expP.Position} параметра  метода {mName} у класса {className}!"));
                        result.Add(new SingleReflectionTestResult(curP.DefaultValue, expP.DefaultValue, $"Неверное дефолтное значение {expP.Position} параметра  метода {mName} у класса {className}!"));
                        result.Add(new SingleReflectionTestResult(curP.IsOut, expP.IsOut, $"Неверное значение out {expP.Position} параметра  метода {mName} у класса {className}!"));
                    }
                }
            }
            return result;
        }
        static List<SingleReflectionTestResult> TestFields(List<FieldDefinition> expectedFileds, List<FieldDefinition> actualFields, string className)
        {
            var result = new List<SingleReflectionTestResult>();

            foreach (var expected in expectedFileds)
            {
                var actual = actualFields.Find(f => f.Name == expected.Name);
                if (actual == null)
                {
                    result.Add(new SingleReflectionTestResult($"Не найдено поле {expected.Name} в классе {className}!"));
                }
                else
                {
                    result.Add(new SingleReflectionTestResult(expected.Type, actual.Type, $"Неверный тип поля {expected.Name} у класса {className}!"));
                    result.Add(new SingleReflectionTestResult(expected.Visibility, actual.Visibility, $"Неверная область видимости поля {expected.Name} у класса {className}!"));
                    result.Add(new SingleReflectionTestResult(expected.Modifier, actual.Modifier, $"Неверный модификатор доступа поля {expected.Name} у класса {className}!"));
                }
            }

            return result;
        }
        static List<SingleReflectionTestResult> TestProperties(List<PropertyDefinition> expectedProps, List<PropertyDefinition> actualProps, string className)
        {
            var result = new List<SingleReflectionTestResult>();

            foreach (var expected in expectedProps)
            {
                var actual = actualProps.Find(f => f.Name == expected.Name);
                if (actual == null)
                {
                    result.Add(new SingleReflectionTestResult($"Не найдено свойство {expected.Name} в классе {className}!"));
                }
                else
                {
                    result.Add(new SingleReflectionTestResult(expected.Type, actual.Type, $"Неверный тип свойства {expected.Name} у класса {className}!"));
                    if (expected.GetMethod != null && actual.GetMethod == null)
                    {
                        result.Add(new SingleReflectionTestResult($"Не найден геттер свойства {expected.Name} в классе {className}!"));
                    }
                    else
                    {
                        if (expected.GetMethod != null)
                        {
                            result.AddRange(TestSingleMethod(expected.GetMethod, actual.GetMethod, className, expected.Name, true));
                        }
                    }

                    if (expected.SetMethod != null && actual.SetMethod == null)
                    {
                        result.Add(new SingleReflectionTestResult($"Не найден cеттер свойства {expected.Name} в классе {className}!"));
                    }
                    else
                    {
                        if (expected.GetMethod != null)
                        {
                            result.AddRange(TestSingleMethod(expected.SetMethod, actual.SetMethod, className, expected.Name, false));
                        }
                    }
                }
            }

            return result;
        }
        static List<SingleReflectionTestResult> TestMethods(List<MethodDefinition> expectedM, List<MethodDefinition> actualM, string className)
        {
            var result = new List<SingleReflectionTestResult>();

            foreach (var expected in expectedM)
            {
                var actual = actualM.Find(f => f.Name == expected.Name);
                if (actual == null)
                {
                    result.Add(new SingleReflectionTestResult($"Не найден метод {expected.Name} в классе {className}!"));
                }
                else
                {
                    result.AddRange(TestSingleMethod(expected, actual, className));
                }
            }

            return result;
        }
        static List<SingleReflectionTestResult> TestSingleMethod(MethodDefinition expected, MethodDefinition actual, string className, string propName = null, bool isGetter = false)
        {
            var result = new List<SingleReflectionTestResult>();

            if (propName != null)
            {
                string type = isGetter ? "геттера" : "сеттера";
                result.Add(new SingleReflectionTestResult(expected.ReturnType, actual.ReturnType, $"Неверный возращаемый тип {type} свойства {propName} у класса {className}!"));
                result.Add(new SingleReflectionTestResult(expected.Visibility, actual.Visibility, $"Неверная область видимости {type} свойства {propName} у класса {className}!"));
                result.Add(new SingleReflectionTestResult(expected.Modifier, actual.Modifier, $"Неверный модификатор доступа {type} свойства {propName} у класса {className}!"));
            }
            else
            {
                string mName = expected.Name;
                result.Add(new SingleReflectionTestResult(expected.ReturnType, actual.ReturnType, $"Неверный возращаемый тип метода {mName} у класса {className}!"));
                result.Add(new SingleReflectionTestResult(expected.Visibility, actual.Visibility, $"Неверная область видимости метода {mName} у класса {className}!"));
                result.Add(new SingleReflectionTestResult(expected.Modifier, actual.Modifier, $"Неверный модификатор доступа метода {mName} у класса {className}!"));
                result.AddRange(TestParams(expected.Parameters, actual.Parameters, className, mName));
            }

            return result;
        }
        static List<SingleReflectionTestResult> TestEvents(List<FieldDefinition> expectedEvents, List<FieldDefinition> actualEvents, string className)
        {
            return TestFields(expectedEvents, actualEvents, className);
        }
    }
}
