using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WhiteStarConversor
{
    public class PlantUmlStrategy : IConversionStrategy
    {
        public string Key => "plantuml";
        public string Description => "Diagrama de clases clásico en PlantUML";
        public string DefaultExtension => ".puml";

        public void Execute(UmlParser parser, string inputFilePath, string? targetPath)
        {
            string outPath = targetPath ?? Path.ChangeExtension(inputFilePath, DefaultExtension);
            if (Directory.Exists(outPath))
            {
                outPath = Path.Combine(outPath, Path.GetFileNameWithoutExtension(inputFilePath) + DefaultExtension);
            }

            var sb = new StringBuilder();
            sb.AppendLine("@startuml");
            sb.AppendLine();

            foreach (var elem in parser.Elements.Values)
            {
                string classType = elem.Type == "UMLInterface" ? "interface" : (elem.IsAbstract ? "abstract class" : "class");
                bool hasBody = elem.Attributes.Any() || elem.Operations.Any();

                if (hasBody)
                {
                    sb.AppendLine($"{classType} {elem.Name} {{");
                    foreach (var attr in elem.Attributes)
                    {
                        sb.AppendLine($"  {GetVisibilityChar(attr.Visibility)}{attr.Name}");
                    }
                    if (elem.Attributes.Any() && elem.Operations.Any())
                    {
                        sb.AppendLine();
                    }
                    foreach (var op in elem.Operations)
                    {
                        string args = string.Join(", ", op.Parameters);
                        sb.AppendLine($"  {GetVisibilityChar(op.Visibility)}{op.Name}({args}) : {op.ReturnType}");
                    }
                    sb.AppendLine("}");
                }
                else
                {
                    sb.AppendLine($"{classType} {elem.Name}");
                }
                sb.AppendLine();
            }

            var herencias = parser.Relations.Where(r => r.Type == "UMLGeneralization").ToList();
            var realizaciones = parser.Relations.Where(r => r.Type == "UMLRealization").ToList();
            var dependencias = parser.Relations.Where(r => r.Type == "UMLDependency").ToList();
            var asociaciones = parser.Relations.Where(r => r.Type == "UMLAssociation").ToList();

            if (herencias.Any())
            {
                sb.AppendLine("' Herencia");
                foreach (var rel in herencias)
                {
                    string parent = GetNameByGuid(parser, rel.SupplierGuid);
                    string child = GetNameByGuid(parser, rel.ClientGuid);
                    string label = string.IsNullOrEmpty(rel.Name) ? "" : $" : {rel.Name}";
                    sb.AppendLine($"{parent} <|-- {child}{label}");
                }
                sb.AppendLine();
            }

            if (realizaciones.Any())
            {
                sb.AppendLine("' Realización");
                foreach (var rel in realizaciones)
                {
                    string supplier = GetNameByGuid(parser, rel.SupplierGuid);
                    string client = GetNameByGuid(parser, rel.ClientGuid);
                    string label = string.IsNullOrEmpty(rel.Name) ? "" : $" : {rel.Name}";
                    sb.AppendLine($"{supplier} <|.. {client}{label}");
                }
                sb.AppendLine();
            }

            var directAssocs = new List<string>();
            var aggs = new List<string>();
            var comps = new List<string>();

            foreach (var assoc in asociaciones)
            {
                var conn0 = assoc.Connections[0];
                var conn1 = assoc.Connections[1];
                string name0 = GetNameByGuid(parser, conn0.ParticipantGuid);
                string name1 = GetNameByGuid(parser, conn1.ParticipantGuid);
                string label = string.IsNullOrEmpty(assoc.Name) ? "" : $" : {assoc.Name}";

                if (conn1.Aggregation == "akAggregate")
                {
                    aggs.Add($"{name1} o-- {name0}{label}");
                }
                else if (conn0.Aggregation == "akAggregate")
                {
                    aggs.Add($"{name0} o-- {name1}{label}");
                }
                else if (conn1.Aggregation == "akComposite")
                {
                    comps.Add($"{name1} *-- {name0}{label}");
                }
                else if (conn0.Aggregation == "akComposite")
                {
                    comps.Add($"{name0} *-- {name1}{label}");
                }
                else
                {
                    if (!conn0.IsNavigable && conn1.IsNavigable)
                    {
                        directAssocs.Add($"{name0} --> {name1}{label}");
                    }
                    else if (conn0.IsNavigable && !conn1.IsNavigable)
                    {
                        directAssocs.Add($"{name1} --> {name0}{label}");
                    }
                    else
                    {
                        directAssocs.Add($"{name0} -- {name1}{label}");
                    }
                }
            }

            if (directAssocs.Any())
            {
                sb.AppendLine("' Asociación directa");
                foreach (var line in directAssocs) sb.AppendLine(line);
                sb.AppendLine();
            }

            if (aggs.Any())
            {
                sb.AppendLine("' Agregación");
                foreach (var line in aggs) sb.AppendLine(line);
                sb.AppendLine();
            }

            if (comps.Any())
            {
                sb.AppendLine("' Composición");
                foreach (var line in comps) sb.AppendLine(line);
                sb.AppendLine();
            }

            if (dependencias.Any())
            {
                sb.AppendLine("' Dependencia");
                foreach (var rel in dependencias)
                {
                    string supplier = GetNameByGuid(parser, rel.SupplierGuid);
                    string client = GetNameByGuid(parser, rel.ClientGuid);
                    string label = string.IsNullOrEmpty(rel.Name) ? "" : $" : {rel.Name}";
                    sb.AppendLine($"{client} ..> {supplier}{label}");
                }
                sb.AppendLine();
            }

            if (parser.AssociationClasses.Any())
            {
                sb.AppendLine("' Clase de Asociación");
                foreach (var assocClass in parser.AssociationClasses)
                {
                    string className = GetNameByGuid(parser, assocClass.ClassGuid);
                    var assoc = parser.Relations.FirstOrDefault(r => r.Guid == assocClass.AssociationGuid);
                    if (assoc != null && assoc.Connections.Count >= 2)
                    {
                        string participant0 = GetNameByGuid(parser, assoc.Connections[0].ParticipantGuid);
                        string participant1 = GetNameByGuid(parser, assoc.Connections[1].ParticipantGuid);
                        sb.AppendLine($"({participant0}, {participant1}) .. {className}");
                    }
                }
                sb.AppendLine();
            }

            sb.AppendLine("@enduml");
            File.WriteAllText(outPath, sb.ToString());
            Console.WriteLine($"Archivo PlantUML guardado en: {outPath}");
        }

        private string GetNameByGuid(UmlParser parser, string guid) =>
            parser.Elements.TryGetValue(guid, out var elem) ? elem.Name : guid;

        private string GetVisibilityChar(string visibility) =>
            visibility switch { "vkPrivate" => "-", "vkProtected" => "#", "vkPackage" => "~", _ => "+" };
    }
}
