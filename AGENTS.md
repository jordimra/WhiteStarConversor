# AGENTS.md

## Contexto
Herramienta C# (.NET 10, Windows-only) que convierte modelos UML (XML de StarUML/WhiteStarUML, `.uml`) en diagramas (PlantUML, Mermaid), esqueletos de código (C#, Java, Python, C++) y documentación Markdown. Doble interfaz: sin argumentos abre WinForms; con argumentos corre en CLI. Proyecto plano: un solo `.csproj`, un único namespace `WhiteStarConversor`, sin paquetes NuGet, sin tests, sin CI, sin lint.

## Comandos
- Build: `dotnet build`
- CLI: `dotnet run -- Ejemplo\Ejemplo.uml [--format <clave>] [dirSalida]`
  - Claves: `plantuml`, `mermaid`, `csharp`, `java`, `python`, `cpp`, `markdown` (o `todos`).
  - `dirSalida` es cualquier argumento posicional que no sea `--format`.
  - Sin `--format`: ejecuta las 7 estrategias.
- Verificación (no existe test suite): `dotnet build` + correr la CLI contra `Ejemplo\Ejemplo.uml` y comparar con las salidas ya commitadas en `Ejemplo\` (`Ejemplo.puml`, `Ejemplo.mermaid`, `Ejemplo.md`, `Ejemplo_csharp\`, `Ejemplo_cpp\`, `Ejemplo_java\`, `Ejemplo_python\`).

## ⚠️ Cuidado con los ficheros de ejemplo
Los `.cs` de `Ejemplo\Ejemplo_csharp\` NO deben compilarse en el proyecto: por diseño emiten parámetros `object <nombre>` que chocan con keywords de C# (ej. `object string`) y fallan con CS1001/CS1003. El csproj los excluye (`Compile Remove="Ejemplo\**"`); si se mueven de carpeta, mantener esa exclusión o el build se rompe.
No "corregir" los `.cs` de ejemplo: son salida generada. Si hay que actualizarlos, regenerarlos con la CLI, no editarlos a mano.

## Arquitectura
- `UmlParser.cs`: parsea los elementos `OBJ` del namespace `http://www.staruml.com` (clases, interfaces, enumeraciones, data types; generalización, realización, dependencia, asociaciones y clases de asociación). Solo diagramas de clases.
- `IConversionStrategy.cs` + 7 clases `*Strategy.cs`: cada estrategia expone `Key` (clave CLI, minúscula), `DefaultExtension` y `Execute(parser, inputPath, targetPath)`.
- Añadir un formato: crear la clase que implemente `IConversionStrategy` y registrarla en la lista estática `Program.Strategies` (Program.cs:11). `Program.RunConversion` es el punto común de CLI y UI.
- `MainForm.cs`: UI WinForms construida en código (sin `.Designer.cs`); reutiliza `Program.RunConversion`.
- Salidas: estrategias de esqueleto escriben en `<base>\<nombreEntrada>_<sufijo>\` (ej. `Ejemplo_csharp\`, un fichero por elemento); estrategias de documento (`plantuml`, `mermaid`, `markdown`) escriben un único fichero `<nombreEntrada>.<ext>` (si `targetPath` es directorio, dentro de él).

## Convenciones
- Comentarios y mensajes de usuario en español.
- El código generado usa el namespace `GeneratedSource`.
- Mapeo de visibilidad UML→C#: `vkPackage` → `internal`.
- Los esqueletos son solo prototipos: atributos `object` con `// TODO: Especificar tipo`, métodos con `throw new NotImplementedException()` o vacíos. No traducir lógica algorítmica.
