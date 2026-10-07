using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WhiteStarConversor
{
    public class CppSkeletonStrategy : IConversionStrategy
    {
        public string Key => "cpp";
        public string Description => "Generación de cabeceras (.h) y código fuente (.cpp) en C++";
        public string DefaultExtension => "_cpp";

        public void Execute(UmlParser parser, string inputFilePath, string? targetPath)
        {
            string baseDir = targetPath ?? Path.GetDirectoryName(inputFilePath) ?? "";
            string outDir = Path.Combine(baseDir, Path.GetFileNameWithoutExtension(inputFilePath) + DefaultExtension);

            if (!Directory.Exists(outDir))
            {
                Directory.CreateDirectory(outDir);
            }

            foreach (var elem in parser.Elements.Values)
            {
                string headerPath = Path.Combine(outDir, elem.Name + ".h");
                string sourcePath = Path.Combine(outDir, elem.Name + ".cpp");

                // --- 1. Generar Cabecera (.h) ---
                var sbH = new StringBuilder();
                string guard = $"_{elem.Name.ToUpper()}_H_";
                sbH.AppendLine($"#ifndef {guard}");
                sbH.AppendLine($"#define {guard}");
                sbH.AppendLine();
                sbH.AppendLine("#include <string>");
                sbH.AppendLine("#include <stdexcept>");
                sbH.AppendLine();

                // Herencia
                var parents = new List<string>();
                var herencia = parser.Relations.FirstOrDefault(r => r.Type == "UMLGeneralization" && r.ClientGuid == elem.Guid);
                if (herencia != null)
                {
                    parents.Add("public " + GetNameByGuid(parser, herencia.SupplierGuid));
                }

                var realizaciones = parser.Relations.Where(r => r.Type == "UMLRealization" && r.ClientGuid == elem.Guid);
                foreach (var real in realizaciones)
                {
                    parents.Add("public " + GetNameByGuid(parser, real.SupplierGuid));
                }

                string inheritancePart = parents.Any() ? " : " + string.Join(", ", parents) : "";

                sbH.AppendLine($"class {elem.Name}{inheritancePart} {{");

                // Agrupar miembros por visibilidad
                var publicMembers = new List<string>();
                var protectedMembers = new List<string>();
                var privateMembers = new List<string>();

                // Atributos
                foreach (var attr in elem.Attributes)
                {
                    string fieldLine = $"    void* {attr.Name}; // TODO: Especificar tipo de dato";
                    AddMemberByVisibility(attr.Visibility, fieldLine, publicMembers, protectedMembers, privateMembers);
                }

                // Destructor virtual por defecto si tiene herencia o métodos virtuales
                bool hasVirtual = elem.Type == "UMLInterface" || elem.IsAbstract || parents.Any();
                if (hasVirtual)
                {
                    publicMembers.Add($"    virtual ~{elem.Name}() = default;");
                }

                // Operaciones
                foreach (var op in elem.Operations)
                {
                    string retType = MapCppType(op.ReturnType);
                    var cppParams = op.Parameters.Select(p => $"void* {p.ToLower()}").ToList(); // TODO: Tipos específicos
                    string paramsStr = string.Join(", ", cppParams);

                    string virtualKeyword = hasVirtual ? "virtual " : "";
                    string pureVirtualPart = (elem.Type == "UMLInterface" || elem.IsAbstract) ? " = 0" : "";

                    string methodLine = $"    {virtualKeyword}{retType} {op.Name}({paramsStr}){pureVirtualPart};";
                    AddMemberByVisibility(op.Visibility, methodLine, publicMembers, protectedMembers, privateMembers);
                }

                // Escribir bloques de visibilidad en el header
                if (publicMembers.Any())
                {
                    sbH.AppendLine("public:");
                    foreach (var m in publicMembers) sbH.AppendLine(m);
                }
                if (protectedMembers.Any())
                {
                    sbH.AppendLine("protected:");
                    foreach (var m in protectedMembers) sbH.AppendLine(m);
                }
                if (privateMembers.Any())
                {
                    sbH.AppendLine("private:");
                    foreach (var m in privateMembers) sbH.AppendLine(m);
                }

                sbH.AppendLine("};");
                sbH.AppendLine();
                sbH.AppendLine("#endif");

                File.WriteAllText(headerPath, sbH.ToString());

                // --- 2. Generar Implementación (.cpp) (solo si no es una interfaz pura) ---
                bool isPureInterface = elem.Type == "UMLInterface" && !elem.Attributes.Any();
                if (!isPureInterface)
                {
                    var sbCpp = new StringBuilder();
                    sbCpp.AppendLine($"#include \"{elem.Name}.h\"");
                    sbCpp.AppendLine();

                    foreach (var op in elem.Operations)
                    {
                        // Interfaces o abstractas tienen firmas virtuales puras en el .h, no se implementan en el .cpp
                        if (elem.IsAbstract || elem.Type == "UMLInterface")
                            continue;

                        string retType = MapCppType(op.ReturnType);
                        var cppParams = op.Parameters.Select(p => $"void* {p.ToLower()}").ToList();
                        string paramsStr = string.Join(", ", cppParams);

                        sbCpp.AppendLine($"{retType} {elem.Name}::{op.Name}({paramsStr}) {{");
                        if (retType != "void")
                        {
                            sbCpp.AppendLine("    throw std::logic_error(\"The method or operation is not implemented.\");");
                        }
                        else
                        {
                            sbCpp.AppendLine("    // TODO: Implementar");
                        }
                        sbCpp.AppendLine("}");
                        sbCpp.AppendLine();
                    }

                    File.WriteAllText(sourcePath, sbCpp.ToString());
                }
            }

            Console.WriteLine($"Esqueletos de código C++ (cabeceras e implementaciones) generados en: {outDir}");
        }

        private string GetNameByGuid(UmlParser parser, string guid) =>
            parser.Elements.TryGetValue(guid, out var elem) ? elem.Name : guid;

        private void AddMemberByVisibility(string visibility, string line, List<string> pub, List<string> prot, List<string> priv)
        {
            if (visibility == "vkPrivate") priv.Add(line);
            else if (visibility == "vkProtected" || visibility == "vkPackage") prot.Add(line);
            else pub.Add(line);
        }

        private string MapCppType(string type) =>
            type.ToLower() switch
            {
                "integer" => "int",
                "boolean" => "bool",
                "double" => "double",
                "float" => "float",
                "string" => "std::string",
                "void" => "void",
                _ => string.IsNullOrEmpty(type) ? "void" : type
            };
    }
}
