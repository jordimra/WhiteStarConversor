using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace WhiteStarConversor
{
    class Program
    {
        public static readonly List<IConversionStrategy> Strategies = new List<IConversionStrategy>
        {
            new PlantUmlStrategy(),
            new MermaidStrategy(),
            new CSharpSkeletonStrategy(),
            new JavaSkeletonStrategy(),
            new PythonSkeletonStrategy(),
            new CppSkeletonStrategy(),
            new MarkdownDocStrategy()
        };

        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                // Iniciar la UI de Windows Forms si no hay argumentos
                ApplicationConfiguration.Initialize();
                Application.Run(new MainForm());
                return;
            }

            string inputPath = args[0];
            if (!File.Exists(inputPath))
            {
                Console.WriteLine($"Error: El archivo de entrada '{inputPath}' no existe.");
                return;
            }

            string? selectedFormat = null;
            string? targetPath = null;

            for (int i = 1; i < args.Length; i++)
            {
                if (args[i] == "--format" && i + 1 < args.Length)
                {
                    selectedFormat = args[i + 1].ToLower();
                    i++;
                }
                else
                {
                    targetPath = args[i];
                }
            }

            RunConversion(inputPath, selectedFormat, targetPath, Console.WriteLine);
        }

        public static void RunConversion(string inputPath, string? selectedFormat, string? targetPath, Action<string> log)
        {
            try
            {
                var parser = new UmlParser();
                parser.Parse(inputPath);

                List<IConversionStrategy> strategiesToRun;

                if (!string.IsNullOrEmpty(selectedFormat) && !selectedFormat.Equals("todos", StringComparison.OrdinalIgnoreCase))
                {
                    var strat = Strategies.FirstOrDefault(s => s.Key.Equals(selectedFormat, StringComparison.OrdinalIgnoreCase));
                    if (strat == null)
                    {
                        log($"Error: Formato '{selectedFormat}' no soportado.");
                        return;
                    }
                    strategiesToRun = new List<IConversionStrategy> { strat };
                }
                else
                {
                    strategiesToRun = Strategies;
                }

                foreach (var strategy in strategiesToRun)
                {
                    log($"Ejecutando estrategia: {strategy.Description} ({strategy.Key})...");
                    strategy.Execute(parser, inputPath, targetPath);
                }

                log("\nProcesamiento completado con éxito.");
            }
            catch (Exception ex)
            {
                log($"Ocurrió un error durante el procesamiento: {ex.Message}");
                log(ex.StackTrace ?? string.Empty);
            }
        }

        public static void PrintUsage()
        {
            Console.WriteLine("Uso: WhiteStarConversor <archivo.uml> [opciones] [ruta_salida]");
            Console.WriteLine("\nOpciones:");
            Console.WriteLine("  --format <formato>   Especifica un formato de salida específico.");
            Console.WriteLine("                       Formatos soportados:");
            foreach (var strat in Strategies)
            {
                Console.WriteLine($"                         - {strat.Key,-10} : {strat.Description}");
            }
            Console.WriteLine("\nSi no se especifica --format ni ruta_salida, se ejecutarán TODAS las estrategias simultáneamente,");
            Console.WriteLine("generando los archivos de salida correspondientes al lado del archivo .uml original.");
        }
    }
}
