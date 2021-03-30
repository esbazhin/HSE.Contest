using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

namespace HSE.Contest.ClassLibrary
{
    public static class TypeNameFixer
    {
        public static string Fix(Type t)
        {
            if (t.IsGenericTypeDefinition)
            {
                string[] name = t.Name.Split('`');
                if (name.Length > 1)
                {
                    int count = int.Parse(name[1]);
                    string T;

                    List<string> ts = new List<string>();
                    for (int i = 0; i < count; i++)
                    {
                        ts.Add($"T{(i + 1)}");
                    }
                    T = string.Join(',', ts);

                    return $"{name[0]}<{T}>";
                }
            }

            if (t.IsGenericType)
            {
                var ts = t.GenericTypeArguments.Select(t => Fix(t));
                string[] name = t.Name.Split('`');
                return $"{name[0]}<{string.Join(',', ts)}>";
            }

            if (t.IsGenericParameter)
            {
                return $"T{(t.GenericParameterPosition + 1)}";
            }

            if (t.IsArray)
            {
                return $"{Fix(t.GetElementType())}[]";
            }

            return t.Name switch
            {
                "Boolean" => "bool",
                "Byte" => "byte",
                "SByte" => "sbyte",
                "Char" => "char",
                "Decimal" => "decimal",
                "Double" => "double",
                "Single" => "float",
                "Int32" => "int",
                "UInt32" => "uint",
                "Int64" => "long",
                "UInt64" => "ulong",
                "Int16" => "short",
                "UInt16" => "ushort",
                "Object" => "object",
                "String" => "string",
                _ => t.Name,
            };
        }
    }

    public class StudentTask
    {
        [Key]
        [Column(name: "id")]
        public int Id { get; set; }
        [Column(name: "name")]
        public string Name { get; set; }

        [Column(name: "groupId")]
        public int? GroupId { get; set; }

        [Column(name: "tl")]
        public int? TimeLimit { get; set; }

        [Column(name: "taskText", TypeName = "text")]
        string TaskText { get; set; }

        [Column(name: "from", TypeName = "timestamptz")]
        public DateTime From { get; set; } = DateTime.Now.Date;
        [Column(name: "to", TypeName = "timestamptz")]
        public DateTime To { get; set; } = DateTime.Now.Date.AddMinutes(20);

        private readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind
        };

        public string ConvertToJson()
        {
            var json = JsonConvert.SerializeObject(this, serializerSettings);
            return json;
        }

        public void LoadJson(string json)
        {
            //var dateTimeConverter = new IsoDateTimeConverter();
            var ob = JsonConvert.DeserializeObject<StudentTask>(json, serializerSettings);
            Id = ob.Id;
            Name = ob.Name;
        }
    }

    public class CommonTest
    {
        public string Name { get; set; }
        public string Input { get; set; }
        public string Output { get; set; }
        public int Key { get; set; }
    }

    public class ClassDefinition
    {
        public string Name { get; set; }
        public string Visibility { get; set; }
        public string Modifier { get; set; }
        public string Base { get; set; }
        public List<string> ImplementedInterfaces { get; set; } = new List<string>();
        public List<MethodDefinition> Constructors { get; set; } = new List<MethodDefinition>();
        public List<FieldDefinition> Fields { get; set; } = new List<FieldDefinition>();
        public List<PropertyDefinition> Properties { get; set; } = new List<PropertyDefinition>();
        public List<MethodDefinition> Methods { get; set; } = new List<MethodDefinition>();
        public List<FieldDefinition> Events { get; set; } = new List<FieldDefinition>();
        public int Key { get; set; }

        public ClassDefinition() { }

        public ClassDefinition(Type type, int k)
        {
            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static;
            Name = TypeNameFixer.Fix(type);
            Visibility = GetVisibility(type);
            Modifier = GetModifier(type);
            Base = TypeNameFixer.Fix(type.BaseType);
            Key = k;
            ImplementedInterfaces = type.GetInterfaces().Select(i => i.Name).ToList();
            Constructors = type.GetConstructors(flags).Select((c, i) => new MethodDefinition(c, i)).ToList();
            Fields = type.GetFields(flags).Select((f, i) => new FieldDefinition(f, i)).ToList();
            Properties = type.GetProperties(flags).Select((p, i) => new PropertyDefinition(p, i)).ToList();
            Methods = type.GetMethods(flags).Select((m, i) => new MethodDefinition(m, i)).ToList();
            Events = type.GetEvents(flags).Select((e, i) => new FieldDefinition(e, i)).ToList();
        }

        private string GetVisibility(Type t)
        {
            List<string> res = new List<string>();
            if (t.IsPublic)
            {
                res.Add("public");
            }
            if (t.IsNotPublic)
            {
                res.Add("private");
            }
            return String.Join(' ', res);
        }
        private string GetModifier(Type t)
        {
            List<string> res = new List<string>();
            if (t.IsAbstract)
            {
                res.Add("abstract");
            }
            if (t.IsSealed)
            {
                res.Add("sealed");
            }
            return String.Join(' ', res);
        }
    }

    public class FieldDefinition
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Visibility { get; set; }
        public string Modifier { get; set; }
        public int Key { get; set; }

        public FieldDefinition() { }
        public FieldDefinition(FieldInfo f, int k)
        {
            Name = f.Name;
            Type = TypeNameFixer.Fix(f.FieldType);
            Visibility = GetVisibility(f);
            Modifier = GetModifier(f);
            Key = k;
        }

        public FieldDefinition(EventInfo f, int k)
        {
            Name = f.Name;
            Type = TypeNameFixer.Fix(f.EventHandlerType);
            if (f.RaiseMethod != null)
            {
                Visibility = MethodDefinition.GetVisibility(f.RaiseMethod);
                Modifier = MethodDefinition.GetModifier(f.RaiseMethod);
            }
            else
            {
                Visibility = MethodDefinition.GetVisibility(f.AddMethod);
                Modifier = MethodDefinition.GetModifier(f.AddMethod);
            }
            Key = k;
        }

        private string GetVisibility(FieldInfo t)
        {
            List<string> res = new List<string>();
            if (t.IsPublic)
            {
                res.Add("public");
            }
            if (t.IsPrivate)
            {
                res.Add("private");
            }
            return String.Join(' ', res);
        }

        private string GetModifier(FieldInfo t)
        {
            List<string> res = new List<string>();
            if (t.IsInitOnly)
            {
                res.Add("readonly");
            }
            if (t.IsStatic)
            {
                res.Add("static");
            }
            return String.Join(' ', res);
        }
    }

    public class MethodDefinition
    {
        public string Name { get; set; }
        public string ReturnType { get; set; }
        public string Visibility { get; set; }
        public string Modifier { get; set; }
        public int Key { get; set; }
        public List<ParameterDefinition> Parameters { get; set; } = new List<ParameterDefinition>();
        public List<TestDefinition> Tests { get; set; } = new List<TestDefinition>();

        public MethodDefinition() { }
        public MethodDefinition(MethodInfo m, int k)
        {
            Name = m.Name;
            ReturnType = TypeNameFixer.Fix(m.ReturnType);
            Visibility = GetVisibility(m);
            Modifier = GetModifier(m);
            Key = k;
            Parameters = m.GetParameters().Select((p, i) => new ParameterDefinition(p, i)).ToList();
        }

        public MethodDefinition(ConstructorInfo m, int k)
        {
            Visibility = GetVisibility(m);
            Modifier = GetModifier(m);
            Key = k;
            Parameters = m.GetParameters().Select((p, i) => new ParameterDefinition(p, i)).ToList();
        }

        public static string GetVisibility(MethodBase t)
        {
            List<string> res = new List<string>();
            if (t.IsPublic)
            {
                res.Add("public");
            }
            if (t.IsPrivate)
            {
                res.Add("private");
            }
            return String.Join(' ', res);
        }

        public static string GetModifier(MethodBase t)
        {
            List<string> res = new List<string>();
            if (t.IsAbstract)
            {
                res.Add("abstract");
            }
            if (t.IsStatic)
            {
                res.Add("static");
            }
            if (t.IsVirtual)
            {
                res.Add("virtual");
            }
            return String.Join(' ', res);
        }
    }

    public class ParameterDefinition
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string DefaultValue { get; set; }
        public int Position { get; set; }
        public bool IsOut { get; set; }
        public int Key { get; set; }

        public ParameterDefinition() { }
        public ParameterDefinition(ParameterInfo pr, int k)
        {
            Name = pr.Name;
            Type = TypeNameFixer.Fix(pr.ParameterType);
            DefaultValue = pr.DefaultValue.ToString();
            Position = pr.Position;
            IsOut = pr.IsOut;
            Key = k;
        }

        public bool Equals(ParameterDefinition other)
        {
            return Type == other.Type && Position == other.Position;
        }
    }

    public class TestDefinition
    {
        public string Name { get; set; }
        public List<object> Inputs { get; set; }
        public object Output { get; set; }
        public int Key { get; set; }
    }

    public class PropertyDefinition
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public MethodDefinition GetMethod { get; set; }
        public MethodDefinition SetMethod { get; set; }
        public int Key { get; set; }

        public PropertyDefinition() { }
        public PropertyDefinition(PropertyInfo p, int k)
        {
            Name = p.Name;
            Type = TypeNameFixer.Fix(p.PropertyType);
            if (p.CanRead)
            {
                GetMethod = new MethodDefinition(p.GetMethod, 0);
            }
            if (p.CanWrite)
            {
                SetMethod = new MethodDefinition(p.SetMethod, 1);
            }
            Key = k;
        }
    }
}