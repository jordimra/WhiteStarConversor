using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace WhiteStarConversor
{
    public class UmlElement
    {
        public string Guid { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsAbstract { get; set; }
        public List<UmlAttribute> Attributes { get; } = new List<UmlAttribute>();
        public List<UmlOperation> Operations { get; } = new List<UmlOperation>();
    }

    public class UmlAttribute
    {
        public string Name { get; set; } = string.Empty;
        public string Visibility { get; set; } = "vkPublic";
    }

    public class UmlOperation
    {
        public string Name { get; set; } = string.Empty;
        public string Visibility { get; set; } = "vkPublic";
        public string ReturnType { get; set; } = "void";
        public List<string> Parameters { get; } = new List<string>();
    }

    public class UmlRelation
    {
        public string Guid { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ClientGuid { get; set; } = string.Empty;
        public string SupplierGuid { get; set; } = string.Empty;
        public List<UmlAssociationEnd> Connections { get; } = new List<UmlAssociationEnd>();
    }

    public class UmlAssociationClass
    {
        public string ClassGuid { get; set; } = string.Empty;
        public string AssociationGuid { get; set; } = string.Empty;
    }

    public class UmlAssociationEnd
    {
        public string ParticipantGuid { get; set; } = string.Empty;
        public bool IsNavigable { get; set; } = true;
        public string Aggregation { get; set; } = "akNone"; // akAggregate, akComposite, akNone
        public string Multiplicity { get; set; } = string.Empty;
    }

    public class UmlParser
    {
        public Dictionary<string, UmlElement> Elements { get; } = new Dictionary<string, UmlElement>();
        public List<UmlRelation> Relations { get; } = new List<UmlRelation>();
        public List<UmlAssociationClass> AssociationClasses { get; } = new List<UmlAssociationClass>();

        public void Parse(string filePath)
        {
            XDocument doc = XDocument.Load(filePath);
            XNamespace xpd = "http://www.staruml.com";

            var objects = doc.Descendants(XName.Get("OBJ", xpd.NamespaceName)).ToList();

            foreach (var obj in objects)
            {
                string? type = obj.Attribute("type")?.Value;
                string? guid = obj.Attribute("guid")?.Value;

                if (string.IsNullOrEmpty(type) || string.IsNullOrEmpty(guid))
                    continue;

                string[] elementTypes = { "UMLClass", "UMLInterface", "UMLObject", "UMLEnumeration", "UMLDataType", "UMLPrimitiveType" };
                if (elementTypes.Contains(type))
                {
                    var elem = ParseElement(obj, type, guid, xpd);
                    Elements[guid] = elem;
                }
                else if (type == "UMLGeneralization" || type == "UMLRealization" || type == "UMLDependency")
                {
                    var rel = ParseRelation(obj, type, guid, xpd);
                    if (rel != null)
                    {
                        Relations.Add(rel);
                    }
                }
                else if (type == "UMLAssociation")
                {
                    var assoc = ParseAssociation(obj, type, guid, xpd);
                    if (assoc != null)
                    {
                        Relations.Add(assoc);
                    }
                }
                else if (type == "UMLAssociationClass")
                {
                    string? classSide = GetRefValue(obj, "ClassSide", xpd);
                    string? assocSide = GetRefValue(obj, "AssociationSide", xpd);
                    if (!string.IsNullOrEmpty(classSide) && !string.IsNullOrEmpty(assocSide))
                    {
                        AssociationClasses.Add(new UmlAssociationClass
                        {
                            ClassGuid = classSide,
                            AssociationGuid = assocSide
                        });
                    }
                }
            }

            Relations.RemoveAll(r => 
                (r.Type == "UMLAssociation" && r.Connections.Any(c => !Elements.ContainsKey(c.ParticipantGuid))) ||
                (r.Type != "UMLAssociation" && (!Elements.ContainsKey(r.ClientGuid) || !Elements.ContainsKey(r.SupplierGuid)))
            );

            AssociationClasses.RemoveAll(ac => 
                !Elements.ContainsKey(ac.ClassGuid) || !Relations.Any(r => r.Guid == ac.AssociationGuid)
            );

            // Generar clases de asociación para relaciones muchos a muchos
            var existingAssocClassGuids = AssociationClasses.Select(ac => ac.AssociationGuid).ToHashSet();
            
            foreach (var rel in Relations.Where(r => r.Type == "UMLAssociation").ToList())
            {
                if (rel.Connections.Count == 2)
                {
                    bool isManyToMany = rel.Connections[0].Multiplicity.Contains("*") && rel.Connections[1].Multiplicity.Contains("*");
                    if (isManyToMany && !existingAssocClassGuids.Contains(rel.Guid))
                    {
                        string className = !string.IsNullOrEmpty(rel.Name) 
                            ? rel.Name 
                            : $"{Elements[rel.Connections[0].ParticipantGuid].Name}{Elements[rel.Connections[1].ParticipantGuid].Name}";
                        
                        string newClassGuid = Guid.NewGuid().ToString();
                        
                        var newClass = new UmlElement
                        {
                            Guid = newClassGuid,
                            Type = "UMLClass",
                            Name = className
                        };
                        
                        Elements[newClassGuid] = newClass;
                        
                        AssociationClasses.Add(new UmlAssociationClass
                        {
                            ClassGuid = newClassGuid,
                            AssociationGuid = rel.Guid
                        });
                    }
                }
            }
        }

        private UmlElement ParseElement(XElement obj, string type, string guid, XNamespace xpd)
        {
            var elem = new UmlElement
            {
                Guid = guid,
                Type = type,
                Name = GetAttrValue(obj, "Name", xpd) ?? "Unnamed"
            };

            string? isAbstractStr = GetAttrValue(obj, "IsAbstract", xpd);
            elem.IsAbstract = isAbstractStr != null && isAbstractStr.Equals("True", StringComparison.OrdinalIgnoreCase);

            var attributes = obj.Elements(XName.Get("OBJ", xpd.NamespaceName))
                .Where(e => e.Attribute("type")?.Value == "UMLAttribute" || e.Attribute("type")?.Value == "UMLEnumerationLiteral");

            foreach (var attrObj in attributes)
            {
                var attr = new UmlAttribute
                {
                    Name = GetAttrValue(attrObj, "Name", xpd) ?? string.Empty,
                    Visibility = GetAttrValue(attrObj, "Visibility", xpd) ?? "vkPublic"
                };
                elem.Attributes.Add(attr);
            }

            var operations = obj.Elements(XName.Get("OBJ", xpd.NamespaceName))
                .Where(e => e.Attribute("type")?.Value == "UMLOperation");

            foreach (var opObj in operations)
            {
                var op = new UmlOperation
                {
                    Name = GetAttrValue(opObj, "Name", xpd) ?? string.Empty,
                    Visibility = GetAttrValue(opObj, "Visibility", xpd) ?? "vkPublic"
                };

                var parameters = opObj.Elements(XName.Get("OBJ", xpd.NamespaceName))
                    .Where(e => e.Attribute("type")?.Value == "UMLParameter");

                foreach (var paramObj in parameters)
                {
                    string direction = GetAttrValue(paramObj, "DirectionKind", xpd) ?? "pdkIn";
                    if (direction == "pdkReturn")
                    {
                        op.ReturnType = GetAttrValue(paramObj, "TypeExpression", xpd) ?? "void";
                    }
                    else
                    {
                        string paramName = GetAttrValue(paramObj, "Name", xpd) ?? string.Empty;
                        if (!string.IsNullOrEmpty(paramName))
                        {
                            op.Parameters.Add(paramName);
                        }
                    }
                }

                elem.Operations.Add(op);
            }

            return elem;
        }

        private UmlRelation? ParseRelation(XElement obj, string type, string guid, XNamespace xpd)
        {
            string? clientRef = null;
            string? supplierRef = null;

            if (type == "UMLGeneralization")
            {
                clientRef = GetRefValue(obj, "Child", xpd);
                supplierRef = GetRefValue(obj, "Parent", xpd);
            }
            else
            {
                clientRef = GetRefValue(obj, "Client", xpd);
                supplierRef = GetRefValue(obj, "Supplier", xpd);
            }

            if (string.IsNullOrEmpty(clientRef) || string.IsNullOrEmpty(supplierRef))
                return null;

            return new UmlRelation
            {
                Guid = guid,
                Type = type,
                Name = GetAttrValue(obj, "Name", xpd) ?? string.Empty,
                ClientGuid = clientRef,
                SupplierGuid = supplierRef
            };
        }

        private UmlRelation? ParseAssociation(XElement obj, string type, string guid, XNamespace xpd)
        {
            var assoc = new UmlRelation
            {
                Guid = guid,
                Type = type,
                Name = GetAttrValue(obj, "Name", xpd) ?? string.Empty
            };

            var connections = obj.Elements(XName.Get("OBJ", xpd.NamespaceName))
                .Where(e => e.Attribute("type")?.Value == "UMLAssociationEnd");

            foreach (var connObj in connections)
            {
                string? participant = GetRefValue(connObj, "Participant", xpd);
                if (string.IsNullOrEmpty(participant))
                    continue;

                string? isNavigableStr = GetAttrValue(connObj, "IsNavigable", xpd);
                bool isNavigable = isNavigableStr == null || !isNavigableStr.Equals("False", StringComparison.OrdinalIgnoreCase);

                string aggregation = GetAttrValue(connObj, "Aggregation", xpd) ?? "akNone";
                string multiplicity = GetAttrValue(connObj, "Multiplicity", xpd) ?? string.Empty;

                assoc.Connections.Add(new UmlAssociationEnd
                {
                    ParticipantGuid = participant,
                    IsNavigable = isNavigable,
                    Aggregation = aggregation,
                    Multiplicity = multiplicity
                });
            }

            if (assoc.Connections.Count < 2)
                return null;

            return assoc;
        }

        private string? GetAttrValue(XElement obj, string attrName, XNamespace xpd)
        {
            return obj.Elements(XName.Get("ATTR", xpd.NamespaceName))
                .FirstOrDefault(e => e.Attribute("name")?.Value == attrName)?
                .Value;
        }

        private string? GetRefValue(XElement obj, string refName, XNamespace xpd)
        {
            return obj.Elements(XName.Get("REF", xpd.NamespaceName))
                .FirstOrDefault(e => e.Attribute("name")?.Value == refName)?
                .Value;
        }
    }
}
