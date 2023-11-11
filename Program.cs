using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ConvertFrameworkLibrary
{
    internal class Program
    {
        static void Main(string[] args)
        {
            do
            {
                Console.WriteLine("Ange sökvägen till DLL-filen:");
                string dllPath = Console.ReadLine().Replace("\"", "");

                if (!File.Exists(dllPath))
                {
                    Console.WriteLine("DLL-filen kunde inte hittas på den angivna sökvägen.");
                    return;
                }

                try
                {
                    Assembly assembly = Assembly.LoadFrom(dllPath);
                    Type[] types = assembly.GetTypes();

                    foreach (Type type in types)
                    {
                        string typeName = GetTypeName(type);

                        if (string.IsNullOrEmpty(typeName))
                        {
                            typeName = "AnonymousType_" + Guid.NewGuid();
                        }

                        var @namespace = assembly.FullName.Split(',')[0];

                        var path = Directory.CreateDirectory(@namespace).FullName;
                        string fileName = @namespace + "." + typeName + ".txt";

                        using (StreamWriter writer = new StreamWriter($"{assembly.FullName.Split(',')[0]}\\{fileName}"))
                        {
                            writer.WriteLine($"namespace {@namespace}");
                            writer.WriteLine("{");
                            writer.WriteLine($"\t{GetVisibility(type)} {GetModifier(type)} {typeName}");
                            writer.WriteLine("\t{");

                            PropertyInfo[] properties = type.GetProperties();
                            foreach (PropertyInfo property in properties)
                            {
                                writer.WriteLine($"  {property.PropertyType.FullName} {property.Name}");
                            }

                            MethodInfo[] methods = type.GetMethods();
                            foreach (MethodInfo method in methods)
                            {
                                string returnType = (method.ReturnType == typeof(void)) ? "void" : method.ReturnType.FullName;
                                writer.WriteLine($"  {returnType} {method.Name}");

                                ParameterInfo[] parameters = method.GetParameters();
                                foreach (ParameterInfo parameter in parameters)
                                {
                                    writer.WriteLine($"    {parameter.ParameterType.FullName} {parameter.Name}");
                                }
                            }
                            writer.WriteLine("\t}");
                            writer.WriteLine("}");
                        }

                        Console.WriteLine($"Innehållet för typen {typeName} har skrivits till filen: {fileName}");
                    }
                }
                catch (ReflectionTypeLoadException ex)
                {
                    foreach (var loaderException in ex.LoaderExceptions)
                    {
                        Console.WriteLine("Loader Exception: " + loaderException.Message);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ett fel inträffade: " + ex.Message);
                }
            } while (true);
        }

        static string GetTypeName(Type type)
        {
            if (type.IsGenericType)
            {
                // Om typen är generisk, extrahera basnamnet
                string baseName = type.Name.Split('`')[0];
                // Ta bort otillåtna tecken från filnamnet
                return RemoveInvalidFileNameChars(baseName);
            }
            else
            {
                // Ta bort otillåtna tecken från filnamnet
                return RemoveInvalidFileNameChars(type.Name);
            }
        }

        static string RemoveInvalidFileNameChars(string input)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            return new string(input.Where(c => !invalidChars.Contains(c)).ToArray());
        }
        static string GetVisibility(Type type)
        {
            if (type.IsPublic)
            {
                return "public";
            }
            else if (type.IsNotPublic)
            {
                return "internal"; // Byt ut mot andra relevanta modifierare om så önskas
            }
            else
            {
                return "unknown"; // Hantera andra fall om nödvändigt
            }
        }

        static string GetModifier(Type type)
        {
            if (type.IsSealed && type.IsAbstract)
            {
                return "static";
            }
            else if (type.IsAbstract)
            {
                return "abstract";
            }
            else if (type.IsSealed)
            {
                return "sealed";
            }
            else
            {
                return "class"; // Standard modifier om inget av ovanstående gäller
            }
        }
    }
}
