using System;
using System.IO;
using System.Windows.Forms;
using System.Drawing;

namespace WhiteStarConversor
{
    public class MainForm : Form
    {
        private TextBox txtInputFile = null!;
        private TextBox txtOutputDir = null!;
        private ComboBox cbFormat = null!;
        private Button btnBrowseInput = null!;
        private Button btnBrowseOutput = null!;
        private Button btnExecute = null!;
        private TextBox txtLog = null!;
        private CheckBox chkSameDirectory = null!;

        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "White Star Conversor";
            this.Size = new Size(650, 500);
            this.MinimumSize = new Size(500, 400);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Layout general
            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 6,
                Padding = new Padding(15)
            };

            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));

            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F)); // Fichero entrada
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F)); // Carpeta salida
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F)); // Checkbox mismo dir
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F)); // Formato
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F)); // Botón procesar
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Log

            // Fila 0: Archivo de entrada
            Label lblInput = new Label { Text = "Archivo UML:", Anchor = AnchorStyles.Left | AnchorStyles.Right, AutoSize = true };
            txtInputFile = new TextBox { Dock = DockStyle.Fill };
            btnBrowseInput = new Button { Text = "Buscar...", Dock = DockStyle.Fill };
            btnBrowseInput.Click += BtnBrowseInput_Click;

            mainLayout.Controls.Add(lblInput, 0, 0);
            mainLayout.Controls.Add(txtInputFile, 1, 0);
            mainLayout.Controls.Add(btnBrowseInput, 2, 0);

            // Fila 1: Carpeta de salida
            Label lblOutput = new Label { Text = "Carpeta Salida:", Anchor = AnchorStyles.Left | AnchorStyles.Right, AutoSize = true };
            txtOutputDir = new TextBox { Dock = DockStyle.Fill };
            btnBrowseOutput = new Button { Text = "Buscar...", Dock = DockStyle.Fill };
            btnBrowseOutput.Click += BtnBrowseOutput_Click;

            mainLayout.Controls.Add(lblOutput, 0, 1);
            mainLayout.Controls.Add(txtOutputDir, 1, 1);
            mainLayout.Controls.Add(btnBrowseOutput, 2, 1);

            // Fila 2: Checkbox Mismo directorio
            chkSameDirectory = new CheckBox
            {
                Text = "Mismo directorio que el de entrada",
                Dock = DockStyle.Fill,
                Checked = false
            };
            chkSameDirectory.CheckedChanged += ChkSameDirectory_CheckedChanged;
            mainLayout.Controls.Add(chkSameDirectory, 1, 2);
            mainLayout.SetColumnSpan(chkSameDirectory, 2);

            // Fila 3: Formato
            Label lblFormat = new Label { Text = "Formato Salida:", Anchor = AnchorStyles.Left | AnchorStyles.Right, AutoSize = true };
            cbFormat = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            cbFormat.Items.Add("Todos");
            foreach (var strat in Program.Strategies)
            {
                cbFormat.Items.Add(strat.Key);
            }
            cbFormat.SelectedIndex = 0;

            mainLayout.Controls.Add(lblFormat, 0, 3);
            mainLayout.Controls.Add(cbFormat, 1, 3);

            // Fila 4: Botón lanzar
            btnExecute = new Button 
            { 
                Text = "Iniciar Conversión", 
                Dock = DockStyle.Fill, 
                BackColor = Color.LightBlue, 
                Font = new Font(this.Font, FontStyle.Bold) 
            };
            btnExecute.Click += BtnExecute_Click;
            mainLayout.Controls.Add(btnExecute, 0, 4);
            mainLayout.SetColumnSpan(btnExecute, 3);

            // Fila 5: Log de salida
            txtLog = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9F)
            };
            mainLayout.Controls.Add(txtLog, 0, 5);
            mainLayout.SetColumnSpan(txtLog, 3);

            this.Controls.Add(mainLayout);
        }

        private void BtnBrowseInput_Click(object? sender, EventArgs e)
        {
            using OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Archivos UML (*.uml)|*.uml|Todos los archivos (*.*)|*.*";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtInputFile.Text = ofd.FileName;
                
                // Si la carpeta de salida está vacía o está marcada la opción de "mismo directorio", poner la misma por defecto
                if (string.IsNullOrWhiteSpace(txtOutputDir.Text) || chkSameDirectory.Checked)
                {
                    txtOutputDir.Text = Path.GetDirectoryName(ofd.FileName) ?? string.Empty;
                }
            }
        }

        private void BtnBrowseOutput_Click(object? sender, EventArgs e)
        {
            using FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                txtOutputDir.Text = fbd.SelectedPath;
            }
        }

        private void ChkSameDirectory_CheckedChanged(object? sender, EventArgs e)
        {
            bool isChecked = chkSameDirectory.Checked;
            txtOutputDir.Enabled = !isChecked;
            btnBrowseOutput.Enabled = !isChecked;

            if (isChecked && !string.IsNullOrWhiteSpace(txtInputFile.Text))
            {
                txtOutputDir.Text = Path.GetDirectoryName(txtInputFile.Text) ?? string.Empty;
            }
        }

        private void BtnExecute_Click(object? sender, EventArgs e)
        {
            string inputPath = txtInputFile.Text;
            string targetPath = txtOutputDir.Text;
            string selectedFormat = cbFormat.SelectedItem?.ToString() ?? "Todos";

            if (string.IsNullOrWhiteSpace(inputPath))
            {
                MessageBox.Show("Por favor, selecciona un archivo de entrada.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!File.Exists(inputPath))
            {
                MessageBox.Show("El archivo de entrada no existe.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            txtLog.Clear();
            btnExecute.Enabled = false;

            try
            {
                Log("Iniciando proceso...");
                
                // Si seleccionó "Todos", mandamos nulo para que ejecute todas las estrategias
                string? formatArg = selectedFormat.Equals("Todos", StringComparison.OrdinalIgnoreCase) ? null : selectedFormat;

                // Ejecutar la conversión pasando la función que escribe en el log de la UI
                Program.RunConversion(inputPath, formatArg, targetPath, Log);
            }
            catch (Exception ex)
            {
                Log($"Error: {ex.Message}");
            }
            finally
            {
                btnExecute.Enabled = true;
            }
        }

        private void Log(string message)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action<string>(Log), message);
            }
            else
            {
                txtLog.AppendText(message + Environment.NewLine);
            }
        }
    }
}
