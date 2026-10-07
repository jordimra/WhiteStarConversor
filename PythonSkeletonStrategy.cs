using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WhiteStarConversor
{
    public class PythonSkeletonStrategy : IConversionStrategy
    {
        public string Key => "python";
        public string Description => "Generación de esqueletos de clases en archivos Python (.py)";
        public string DefaultExtension => "_python";

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
                string filePath = Path.Combine(outDir, elem.Name + ".py");
                var sb = new StringBuilder();

                // Determinar clases base para la herencia múltiple en Python
                var baseClasses = new List<string>();

                var herencia = parser.Relations.FirstOrDefault(r => r.Type == "UMLGeneralization" && r.ClientGuid == elem.Guid);
                if (herencia != null)
                {
                    baseClasses.Add(GetNameByGuid(parser, herencia.SupplierGuid));
                }

                var realizaciones = parser.Relations.Where(r => r.Type == "UMLRealization" && r.ClientGuid == elem.Guid);
                foreach (var real in realizaciones)
                {
                    baseClasses.Add(GetNameByGuid(parser, real.SupplierGuid));
                }

                string basePart = baseClasses.Any() ? $"({string.Join(", ", baseClasses)})" : "";

                sb.AppendLine($"class {elem.Name}{basePart}:");
                
                // Comentario de documentación
                string docType = elem.Type == "UMLInterface" ? "Interfaz" : (elem.IsAbstract ? "Clase Abstracta" : "Clase");
                sb.AppendLine($"    \"\"\"");
                sb.AppendLine($"    Representa la {docType} {elem.Name}");
                sb.AppendLine($"    \"\"\"");
                sb.AppendLine();

                // Constructor __init__ para inicializar atributos
                sb.AppendLine("    def __init__(self):");
                if (baseClasses.Any())
                {
                    sb.AppendLine("        super().__init__()");
                }

                if (elem.Attributes.Any())
                {
                    foreach (var attr in elem.Attributes)
                    {
                        string name = GetPythonName(attr.Name, attr.Visibility);
                        sb.AppendLine($"        self.{name} = None  # type: Any ({GetVisibilityName(attr.Visibility)})");
                    }
                }
                else if (!baseClasses.Any())
                {
                    sb.AppendLine("        pass");
                }

                sb.AppendLine();

                // Operaciones
                if (elem.Operations.Any())
                {
                    foreach (var op in elem.Operations)
                    {
                        string name = GetPythonName(op.Name, op.Visibility);
                        var pyParams = new List<string> { "self" };
                        pyParams.AddRange(op.Parameters.Select(p => p.ToLower()));
                        string paramsStr = string.Join(", ", pyParams);

                        sb.AppendLine($"    def {name}({paramsStr}):");
                        sb.AppendLine($"        \"\"\"");
                        sb.AppendLine($"        Retorna: {op.ReturnType}");
                        sb.AppendLine($"        \"\"\"");
                        sb.AppendLine($"        raise NotImplementedError(\"El método {name} no está implementado.\")");
                        sb.AppendLine();
                    }
                }
                else if (!elem.Attributes.Any() && !baseClasses.Any())
                {
                    // Si la clase está completamente vacía
                    sb.AppendLine("    pass");
                }

                File.WriteAllText(filePath, sb.ToString());
            }

            Console.WriteLine($"Esqueletos de código Python generados en el directorio: {outDir}");
        }

        private string GetNameByGuid(UmlParser parser, string guid) =>
            parser.Elements.TryGetValue(guid, out var elem) ? elem.Name : guid;

        private string GetPythonName(string name, string visibility)
        {
            if (string.IsNullOrEmpty(name)) return "unnamed";
            
            // Convención de nombres privados y protegidos de Python
            return visibility switch
            {
                "vkPrivate" => "__" + name,
                "vkProtected" => "_" + name,
                "vkPackage" => "_" + name,
                _ => name
            };
        }

        private string GetVisibilityName(string visibility) =>
            visibility switch
            {
                "vkPrivate" => "Private",
                "vkProtected" => "Protected",
                "vkPackage" => "Package-Private",
                _ => "Public"
            };
    }
}
