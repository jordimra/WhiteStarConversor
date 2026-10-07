# WhiteStarConversor

WhiteStarConversor es una herramienta desarrollada en C# diseñada para interpretar archivos de modelos UML (como `.uml` o `.emf` exportados por herramientas como StarUML o WhiteStarUML) y convertirlos automáticamente a múltiples formatos. Permite generar diagramas como código, esqueletos de clases para distintos lenguajes de programación y documentación técnica de forma simultánea.

Cuenta con una doble interfaz: puede ser utilizado a través de una **Interfaz Gráfica (WinForms)** para mayor comodidad o desde la **Línea de Comandos (CLI)**, lo que facilita su integración en pipelines o scripts automatizados.

## 🚀 Características

El conversor utiliza un patrón de diseño "Estrategia" para manejar las diferentes salidas generadas a partir del mismo modelo UML. Actualmente soporta los siguientes formatos:

*   **Diagramas de Clases como Código:**
    *   **PlantUML** (`.puml`): Generación clásica de diagramas UML.
    *   **Mermaid** (`.mermaid`): Diagramas compatibles directamente con Markdown, GitHub y otras herramientas web.
*   **Esqueletos de Código (Scaffolding):**
    *   **C#** (`.cs`)
    *   **Java** (`.java`)
    *   **Python** (`.py`)
    *   **C++** (`.h` y `.cpp`)
*   **Documentación Técnica:**
    *   **Markdown** (`.md`): Un documento que resume todas las clases, atributos, métodos y sus relaciones.

## 💻 Uso

### Uso de Interfaz Gráfica
Simplemente ejecuta el programa sin argumentos. Se abrirá una ventana donde podrás seleccionar el archivo de entrada y generar las conversiones de forma visual.

### Uso por Línea de Comandos
Puedes ejecutar la herramienta desde la consola de la siguiente manera:

```bash
WhiteStarConversor <archivo.uml> [opciones] [ruta_salida]
```

**Opciones disponibles:**
*   `--format <formato>`: Especifica un único formato de salida. Formatos soportados: `todos`, `plantuml`, `mermaid`, `csharp`, `java`, `python`, `cpp`, `markdown`.
*   Si no se especifica el parámetro `--format`, se ejecutarán TODAS las estrategias, generando los diferentes archivos en la ruta especificada (o junto al archivo origen).

---

## ⚠️ Diferencias Clave: Mermaid vs PlantUML

Uno de los principales objetivos de este proyecto es exportar diagramas para la web o documentación (Markdown). Para ello usa tanto PlantUML como Mermaid. Sin embargo, **Mermaid presenta ciertas limitaciones semánticas** respecto a UML tradicional que el conversor intenta mitigar, pero es importante conocer:

### 1. Clases de Asociación (Association Classes)
*   **PlantUML:** Soporta de forma nativa la notación de clases de asociación. Si tienes una relación de muchos a muchos que necesita propiedades (una clase intermedia), PlantUML permite enlazarla a la relación misma usando la sintaxis `(ClaseA, ClaseB) .. ClaseAsociacion`. Esto mantiene la semántica UML perfecta.
*   **Mermaid:** El renderizador de `classDiagram` en Mermaid **no soporta** la representación gráfica real de las clases de asociación (no se pueden conectar clases a las "aristas"). 
    *   *¿Cómo lo soluciona WhiteStarConversor?:* Para evitar perder la información en Mermaid, el conversor dibuja la "Clase de Asociación" como una clase normal y genera conexiones directas hacia ambos participantes (`ClaseA -- ClaseAsociacion` y `ClaseB -- ClaseAsociacion`). Visualmente cumple su función, pero semánticamente deja de ser una "Clase de Asociación" estricta según el estándar UML.

### 2. Estereotipos y Tipos
*   **PlantUML:** Permite diferenciar visualmente y mediante iconos si un bloque es una interface, una clase abstracta o un enumerador.
*   **Mermaid:** Depende del uso de estereotipos textuales (`<<interface>>`, `<<enumeration>>`, `<<dataType>>`) insertados en la clase. Aunque es correcto, a nivel de diseño el diagrama de Mermaid puede ser visualmente menos rico y más uniforme que el de PlantUML.

### 3. Enrutamiento (Routing) y Complejidad Visual
*   Mermaid suele tener problemas de enrutamiento (cruce excesivo de flechas o cajas desalineadas) en diagramas UML que son muy grandes o complejos. PlantUML, gracias al motor Graphviz, suele calcular posiciones óptimas de forma mucho más eficiente para redes intrincadas.

---

## 🚧 Otras posibles carencias y limitaciones conocidas

Actualmente, el parseo (`UmlParser.cs`) y la generación de la herramienta están enfocados al core del diseño orientado a objetos. Existen algunas limitaciones en la versión actual:

1.  **Exclusividad de Diagramas de Clases:** El parser está diseñado únicamente para extraer información estructural (Clases, Interfaces, Relaciones, Atributos y Operaciones). **No soporta** la exportación ni el parseo de otros diagramas UML (como diagramas de Secuencia, Estados, Casos de Uso, Actividad o Despliegue).
2.  **Lógica Interna en Esqueletos:** Las estrategias de generación de código (`CSharpSkeletonStrategy`, etc.) generan únicamente "esqueletos". Esto significa que se declaran las clases, herencias, interfaces, y prototipos de funciones, pero el cuerpo de los métodos estará siempre vacío o tendrá valores de retorno simulados. No intenta deducir ni traducir lógica algorítmica.
3.  **Metadatos Avanzados:** Aunque el parser extrae la visibilidad (`Public`, `Private`, etc.), tipos de retorno y parámetros, ciertas propiedades UML más avanzadas (como modificadores `static`, valores por defecto explícitos, o propiedades transitorias) podrían no reflejarse en todos los lenguajes destino de la misma forma debido a las diferencias semánticas de cada lenguaje.
4.  **Generación de Clases Automáticas:** El sistema auto-genera una Clase de Asociación cuando detecta una relación `muchos-a-muchos` (`*` a `*`) pura en el modelo. El nombre generado es una concatenación de los participantes, lo que a veces requiere una refactorización manual posterior del nombre para que tenga sentido de negocio.

## 🤝 Contribuciones
Para aportar nuevas estrategias (ej. TypeScript, Ruby o exportación a JSON), simplemente crea una nueva clase que implemente `IConversionStrategy` y regístrala en la lista estática `Strategies` en la clase `Program.cs`.
