using System;
using System.IO;
using System.Linq;
using System.Text;

namespace WhiteStarConversor
{
    public class MarkdownDocStrategy : IConversionStrategy
    {
        public string Key => "markdown";
        public string Description => "Documentación detallada del modelo en formato Markdown (.md)";
        public string DefaultExtension => ".md";

        public void Execute(UmlParser parser, string inputFilePath, string? targetPath)
        {
            string outPath = targetPath ?? Path.ChangeExtension(inputFilePath, DefaultExtension);
            if (Directory.Exists(outPath))
            {
                outPath = Path.Combine(outPath, Path.GetFileNameWithoutExtension(inputFilePath) + DefaultExtension);
            }

            var sb = new StringBuilder();
            sb.AppendLine($"# Documentación Técnica del Modelo: {Path.GetFileNameWithoutExtension(inputFilePath)}");
            sb.AppendLine();
            sb.AppendLine($"Generado el {DateTime.Now:dd/MM/yyyy a las HH:mm:ss}.");
            sb.AppendLine();

            sb.AppendLine("## 1. Índice de Clases e Interfaces");
            sb.AppendLine();
            foreach (var elem in parser.Elements.Values.OrderBy(e => e.Name))
            {
                string modifier = elem.Type == "UMLInterface" ? " (Interfaz)" : (elem.IsAbstract ? " (Clase Abstracta)" : " (Clase)");
                sb.AppendLine($"* [{elem.Name}](#{elem.Name.ToLower()}){modifier}");
            }
            sb.AppendLine();

            sb.AppendLine("## 2. Detalle de los Elementos");
            sb.AppendLine();

            foreach (var elem in parser.Elements.Values.OrderBy(e => e.Name))
            {
                string elemType = elem.Type == "UMLInterface" ? "Interfaz" : (elem.IsAbstract ? "Clase Abstracta" : "Clase");
                sb.AppendLine($"### {elem.Name}");
                sb.AppendLine();
                sb.AppendLine($"* **Tipo:** {elemType}");

                var baseRelations = parser.Relations.Where(r => r.ClientGuid == elem.Guid);
                foreach (var rel in baseRelations)
                {
                    string targetName = GetNameByGuid(parser, rel.SupplierGuid);
                    string relType = rel.Type == "UMLGeneralization" ? "Hereda de" : "Realiza la interfaz";
                    sb.AppendLine($"* **{relType}:** {targetName}");
                }
                sb.AppendLine();

                if (elem.Attributes.Any())
                {
                    sb.AppendLine("#### Atributos / Campos");
                    sb.AppendLine();
                    sb.AppendLine("| Visibilidad | Nombre | Tipo |");
                    sb.AppendLine("| --- | --- | --- |");
                    foreach (var attr in elem.Attributes)
                    {
                        sb.AppendLine($"| {GetVisibilityText(attr.Visibility)} | `{attr.Name}` | *Por definir* |");
                    }
                    sb.AppendLine();
                }

                if (elem.Operations.Any())
                {
                    sb.AppendLine("#### Métodos / Operaciones");
                    sb.AppendLine();
                    sb.AppendLine("| Visibilidad | Nombre | Parámetros | Retorno |");
                    sb.AppendLine("| --- | --- | --- | --- |");
                    foreach (var op in elem.Operations)
                    {
                        string paramsStr = op.Parameters.Any() ? string.Join(", ", op.Parameters.Select(p => $"`{p}`")) : "*Ninguno*";
                        sb.AppendLine($"| {GetVisibilityText(op.Visibility)} | `{op.Name}` | {paramsStr} | `{op.ReturnType}` |");
                    }
                    sb.AppendLine();
                }

                sb.AppendLine("---");
                sb.AppendLine();
            }

            File.WriteAllText(outPath, sb.ToString());
            Console.WriteLine($"Documentación Markdown guardada en: {outPath}");
        }

        private string GetNameByGuid(UmlParser parser, string guid) =>
            parser.Elements.TryGetValue(guid, out var elem) ? elem.Name : guid;

        private string GetVisibilityText(string visibility) =>
            visibility switch
            {
                "vkPrivate" => "Privado (`-`)",
                "vkProtected" => "Protegido (`#`)",
                "vkPackage" => "Paquete (`~`)",
                _ => "Público (`+`)"
            };
    }
}
