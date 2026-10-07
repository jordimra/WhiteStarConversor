using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WhiteStarConversor
{
    public class CSharpSkeletonStrategy : IConversionStrategy
    {
        public string Key => "csharp";
        public string Description => "Generación de esqueletos de clases en archivos C# (.cs)";
        public string DefaultExtension => "_csharp";

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
                string filePath = Path.Combine(outDir, elem.Name + ".cs");
                var sb = new StringBuilder();

                sb.AppendLine("using System;");
                sb.AppendLine();
                sb.AppendLine("namespace GeneratedSource");
                sb.AppendLine("{");

                string typeKeyword = "class";
                if (elem.Type == "UMLInterface")
                {
                    typeKeyword = "interface";
                }
                else if (elem.IsAbstract)
                {
                    typeKeyword = "abstract class";
                }

                var parents = new List<string>();
                
                var herencia = parser.Relations.FirstOrDefault(r => r.Type == "UMLGeneralization" && r.ClientGuid == elem.Guid);
                if (herencia != null)
                {
                    string parentName = GetNameByGuid(parser, herencia.SupplierGuid);
                    parents.Add(parentName);
                }

                var realizaciones = parser.Relations.Where(r => r.Type == "UMLRealization" && r.ClientGuid == elem.Guid);
                foreach (var real in realizaciones)
                {
                    string supplierName = GetNameByGuid(parser, real.SupplierGuid);
                    parents.Add(supplierName);
                }

                string inheritancePart = parents.Any() ? " : " + string.Join(", ", parents) : "";

                sb.AppendLine($"    public {typeKeyword} {elem.Name}{inheritancePart}");
                sb.AppendLine("    {");

                foreach (var attr in elem.Attributes)
                {
                    string vis = GetCsVisibility(attr.Visibility);
                    sb.AppendLine($"        {vis} object {attr.Name}; // TODO: Especificar tipo");
                }

                if (elem.Attributes.Any() && elem.Operations.Any())
                {
                    sb.AppendLine();
                }

                foreach (var op in elem.Operations)
                {
                    string vis = GetCsVisibility(op.Visibility);
                    string retType = MapCsType(op.ReturnType);
                    var csParams = op.Parameters.Select(p => $"object {p.ToLower()}").ToList();
                    string paramsStr = string.Join(", ", csParams);

                    if (elem.Type == "UMLInterface")
                    {
                        sb.AppendLine($"        {retType} {op.Name}({paramsStr});");
                    }
                    else
                    {
                        sb.AppendLine($"        {vis} {retType} {op.Name}({paramsStr})");
                        sb.AppendLine("        {");
                        if (retType != "void")
                        {
                            sb.AppendLine("            throw new NotImplementedException();");
                        }
                        else
                        {
                            sb.AppendLine("            // TODO: Implementar");
                        }
                        sb.AppendLine("        }");
                    }
                    sb.AppendLine();
                }

                sb.AppendLine("    }");
                sb.AppendLine("}");

                File.WriteAllText(filePath, sb.ToString());
            }

            Console.WriteLine($"Esqueletos de código C# generados en el directorio: {outDir}");
        }

        private string GetNameByGuid(UmlParser parser, string guid) =>
            parser.Elements.TryGetValue(guid, out var elem) ? elem.Name : guid;

        private string GetCsVisibility(string visibility) =>
            visibility switch
            {
                "vkPrivate" => "private",
                "vkProtected" => "protected",
                "vkPackage" => "internal",
                _ => "public"
            };

        private string MapCsType(string type) =>
            type.ToLower() switch
            {
                "integer" => "int",
                "boolean" => "bool",
                "double" => "double",
                "float" => "float",
                "string" => "string",
                "void" => "void",
                _ => string.IsNullOrEmpty(type) ? "void" : type
            };
    }
}
