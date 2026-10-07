using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WhiteStarConversor
{
    public class MermaidStrategy : IConversionStrategy
    {
        public string Key => "mermaid";
        public string Description => "Diagrama de clases en Mermaid (ClassDiagram)";
        public string DefaultExtension => ".mermaid";

        public void Execute(UmlParser parser, string inputFilePath, string? targetPath)
        {
            string outPath = targetPath ?? Path.ChangeExtension(inputFilePath, DefaultExtension);
            if (Directory.Exists(outPath))
            {
                outPath = Path.Combine(outPath, Path.GetFileNameWithoutExtension(inputFilePath) + DefaultExtension);
            }

            var sb = new StringBuilder();
            sb.AppendLine("classDiagram");

            foreach (var elem in parser.Elements.Values)
            {
                sb.AppendLine($"    class {elem.Name} {{");
                
                if (elem.Type == "UMLInterface")
                {
                    sb.AppendLine("        <<interface>>");
                }
                else if (elem.Type == "UMLEnumeration")
                {
                    sb.AppendLine("        <<enumeration>>");
                }
                else if (elem.Type == "UMLDataType" || elem.Type == "UMLPrimitiveType")
                {
                    sb.AppendLine("        <<dataType>>");
                }
                else if (elem.IsAbstract)
                {
                    sb.AppendLine("        <<abstract>>");
                }

                foreach (var attr in elem.Attributes)
                {
                    sb.AppendLine($"        {GetVisibilityChar(attr.Visibility)}{attr.Name}");
                }

                foreach (var op in elem.Operations)
                {
                    string args = string.Join(", ", op.Parameters);
                    sb.AppendLine($"        {GetVisibilityChar(op.Visibility)}{op.Name}({args}) {op.ReturnType}");
                }
                
                sb.AppendLine("    }");
            }
            sb.AppendLine();

            var herencias = parser.Relations.Where(r => r.Type == "UMLGeneralization").ToList();
            var realizaciones = parser.Relations.Where(r => r.Type == "UMLRealization").ToList();
            var dependencias = parser.Relations.Where(r => r.Type == "UMLDependency").ToList();
            var asociaciones = parser.Relations.Where(r => r.Type == "UMLAssociation").ToList();
            var assocClassGuids = parser.AssociationClasses.Select(ac => ac.AssociationGuid).ToHashSet();

            if (herencias.Any())
            {
                sb.AppendLine("    %% Herencia");
                foreach (var rel in herencias)
                {
                    string parent = GetNameByGuid(parser, rel.SupplierGuid);
                    string child = GetNameByGuid(parser, rel.ClientGuid);
                    string label = string.IsNullOrEmpty(rel.Name) ? "" : $" : {rel.Name}";
                    sb.AppendLine($"    {parent} <|-- {child}{label}");
                }
                sb.AppendLine();
            }

            if (realizaciones.Any())
            {
                sb.AppendLine("    %% Realización");
                foreach (var rel in realizaciones)
                {
                    string supplier = GetNameByGuid(parser, rel.SupplierGuid);
                    string client = GetNameByGuid(parser, rel.ClientGuid);
                    string label = string.IsNullOrEmpty(rel.Name) ? "" : $" : {rel.Name}";
                    sb.AppendLine($"    {supplier} <|.. {client}{label}");
                }
                sb.AppendLine();
            }

            var assocsLines = new List<string>();

            foreach (var assoc in asociaciones)
            {
                if (assocClassGuids.Contains(assoc.Guid))
                    continue;

                var conn0 = assoc.Connections[0];
                var conn1 = assoc.Connections[1];
                string name0 = GetNameByGuid(parser, conn0.ParticipantGuid);
                string name1 = GetNameByGuid(parser, conn1.ParticipantGuid);
                string label = string.IsNullOrEmpty(assoc.Name) ? "" : $" : {assoc.Name}";

                if (conn1.Aggregation == "akAggregate")
                {
                    assocsLines.Add($"    {name1} o-- {name0}{label}");
                }
                else if (conn0.Aggregation == "akAggregate")
                {
                    assocsLines.Add($"    {name0} o-- {name1}{label}");
                }
                else if (conn1.Aggregation == "akComposite")
                {
                    assocsLines.Add($"    {name1} *-- {name0}{label}");
                }
                else if (conn0.Aggregation == "akComposite")
                {
                    assocsLines.Add($"    {name0} *-- {name1}{label}");
                }
                else
                {
                    string link = "--";
                    if (!conn0.IsNavigable && conn1.IsNavigable)
                        link = "-->";
                    else if (conn0.IsNavigable && !conn1.IsNavigable)
                        link = "<--";
                    else
                        link = "--";

                    assocsLines.Add($"    {name0} {link} {name1}{label}");
                }
            }

            if (assocsLines.Any())
            {
                sb.AppendLine("    %% Asociaciones, Agregaciones y Composiciones");
                foreach (var line in assocsLines) sb.AppendLine(line);
                sb.AppendLine();
            }

            if (dependencias.Any())
            {
                sb.AppendLine("    %% Dependencia");
                foreach (var rel in dependencias)
                {
                    string supplier = GetNameByGuid(parser, rel.SupplierGuid);
                    string client = GetNameByGuid(parser, rel.ClientGuid);
                    string label = string.IsNullOrEmpty(rel.Name) ? "" : $" : {rel.Name}";
                    sb.AppendLine($"    {client} ..> {supplier}{label}");
                }
                sb.AppendLine();
            }

            if (parser.AssociationClasses.Any())
            {
                sb.AppendLine("    %% Clases de Asociación");
                foreach (var assocClass in parser.AssociationClasses)
                {
                    string className = GetNameByGuid(parser, assocClass.ClassGuid);
                    var assoc = parser.Relations.FirstOrDefault(r => r.Guid == assocClass.AssociationGuid);
                    if (assoc != null && assoc.Connections.Count >= 2)
                    {
                        string participant0 = GetNameByGuid(parser, assoc.Connections[0].ParticipantGuid);
                        string participant1 = GetNameByGuid(parser, assoc.Connections[1].ParticipantGuid);
                        
                        sb.AppendLine($"    {participant0} -- {className}");
                        sb.AppendLine($"    {participant1} -- {className}");
                    }
                }
                sb.AppendLine();
            }

            File.WriteAllText(outPath, sb.ToString());
            Console.WriteLine($"Archivo Mermaid guardado en: {outPath}");
        }

        private string GetNameByGuid(UmlParser parser, string guid) =>
            parser.Elements.TryGetValue(guid, out var elem) ? elem.Name : guid;

        private string GetVisibilityChar(string visibility) =>
            visibility switch { "vkPrivate" => "-" , "vkProtected" => "#" , "vkPackage" => "~" , _ => "+" };
    }
}
