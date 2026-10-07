using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WhiteStarConversor
{
    public class JavaSkeletonStrategy : IConversionStrategy
    {
        public string Key => "java";
        public string Description => "Generación de esqueletos de clases en archivos Java (.java)";
        public string DefaultExtension => "_java";

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
                string filePath = Path.Combine(outDir, elem.Name + ".java");
                var sb = new StringBuilder();

                sb.AppendLine("package generatedsource;");
                sb.AppendLine();

                string typeKeyword = "class";
                if (elem.Type == "UMLInterface")
                {
                    typeKeyword = "interface";
                }
                else if (elem.IsAbstract)
                {
                    typeKeyword = "abstract class";
                }

                // Extends e Implements
                string extendsPart = "";
                var implementsList = new List<string>();

                var herencia = parser.Relations.FirstOrDefault(r => r.Type == "UMLGeneralization" && r.ClientGuid == elem.Guid);
                if (herencia != null)
                {
                    string parentName = GetNameByGuid(parser, herencia.SupplierGuid);
                    extendsPart = " extends " + parentName;
                }

                var realizaciones = parser.Relations.Where(r => r.Type == "UMLRealization" && r.ClientGuid == elem.Guid);
                foreach (var real in realizaciones)
                {
                    string supplierName = GetNameByGuid(parser, real.SupplierGuid);
                    implementsList.Add(supplierName);
                }

                string implementsPart = "";
                if (implementsList.Any())
                {
                    // Si el elemento actual es una interfaz, en Java extiende otras interfaces con "extends"
                    if (elem.Type == "UMLInterface")
                    {
                        implementsPart = " extends " + string.Join(", ", implementsList);
                    }
                    else
                    {
                        implementsPart = " implements " + string.Join(", ", implementsList);
                    }
                }

                sb.AppendLine($"public {typeKeyword} {elem.Name}{extendsPart}{implementsPart}");
                sb.AppendLine("{");

                // Atributos
                foreach (var attr in elem.Attributes)
                {
                    string vis = GetJavaVisibility(attr.Visibility);
                    string visPart = string.IsNullOrEmpty(vis) ? "" : vis + " ";
                    sb.AppendLine($"    {visPart}Object {attr.Name}; // TODO: Especificar tipo");
                }

                if (elem.Attributes.Any() && elem.Operations.Any())
                {
                    sb.AppendLine();
                }

                // Operaciones
                foreach (var op in elem.Operations)
                {
                    string vis = GetJavaVisibility(op.Visibility);
                    string visPart = string.IsNullOrEmpty(vis) ? "" : vis + " ";
                    string retType = MapJavaType(op.ReturnType);
                    var javaParams = op.Parameters.Select(p => $"Object {p.ToLower()}").ToList();
                    string paramsStr = string.Join(", ", javaParams);

                    if (elem.Type == "UMLInterface")
                    {
                        sb.AppendLine($"    {retType} {op.Name}({paramsStr});");
                    }
                    else
                    {
                        sb.AppendLine($"    {visPart}{retType} {op.Name}({paramsStr})");
                        sb.AppendLine("    {");
                        if (retType != "void")
                        {
                            sb.AppendLine("        throw new UnsupportedOperationException(\"Not implemented yet.\");");
                        }
                        else
                        {
                            sb.AppendLine("        // TODO: Implementar");
                        }
                        sb.AppendLine("    }");
                    }
                    sb.AppendLine();
                }

                sb.AppendLine("}");

                File.WriteAllText(filePath, sb.ToString());
            }

            Console.WriteLine($"Esqueletos de código Java generados en el directorio: {outDir}");
        }

        private string GetNameByGuid(UmlParser parser, string guid) =>
            parser.Elements.TryGetValue(guid, out var elem) ? elem.Name : guid;

        private string GetJavaVisibility(string visibility) =>
            visibility switch
            {
                "vkPrivate" => "private",
                "vkProtected" => "protected",
                "vkPackage" => "", // Visibilidad package-private por defecto en Java
                _ => "public"
            };

        private string MapJavaType(string type) =>
            type.ToLower() switch
            {
                "integer" => "int",
                "boolean" => "boolean",
                "double" => "double",
                "float" => "float",
                "string" => "String",
                "void" => "void",
                _ => string.IsNullOrEmpty(type) ? "void" : type
            };
    }
}
