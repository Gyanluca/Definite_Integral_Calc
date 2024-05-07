using System.Text;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Markup;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using Path = System.IO.Path;
using System.Text.RegularExpressions;
using static System.Math;


namespace _03_Calcolatore_Integrali_Definiti
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        //--------------------------------------------------------------------------------Metodo del punto medio:
        private double Midpoint_rule(double a, double b, int n, Func<double, double> Func)
        {
            double h = (b - a) / n;
            double integrale = 0;

            for (int i = 0; i < n; i++)
            {
                double x = a + h * (i + 0.5);
                integrale += Func(x);
            }

            return integrale * h;
        }

        //-----------------------------------------------------------------------------------Metodo del trapezio:
        private double Trapezoidal_rule(double a, double b, int n, Func<double, double> Func)
        {
            double h = (b - a) / n;
            double integrale = 0.5 * (Func(a) + Func(b));

            for (int i = 1; i < n; i++)
            {
                double x = a + i * h;
                integrale += Func(x);
            }
            return integrale * h;
        }

        //--------------------------------------------------------------------------------Metodo Cavalieri-Simpson:
        private double Simpson_rule(double a, double b, int n, Func<double, double> Func)
        {
            if (n % 2 != 0)
            {
                throw new ArgumentException("The number of intervals must be even for Simpson's rule.");
            }

            double h = (b - a) / n;
            double integral = Func(a) + Func(b);

            for (int i = 1; i < n; i++)
            {
                double x = a + i * h;
                integral += (i % 2 == 0) ? 2 * Func(x) : 4 * Func(x);
            }

            return integral * h / 3.0;
        }

        //-------------------------------------------------------------------------------Metodo Quadratura di Gauss:
        private double Gauss_quadrature(double a, double b, int n, Func<double, double> Func)
        {
            // Punti e pesi per la quadratura di Gauss
            double[] gaussPoints = { -0.7745966692414834, 0.0, 0.7745966692414834 };
            double[] gaussWeights = { 0.5555555555555556, 0.8888888888888888, 0.5555555555555556 };

            double integral = 0;

            // Trasforma l'intervallo da [a, b] a [-1, 1]
            double alpha = (b - a) / 2;
            double beta = (b + a) / 2;

            // Calcola l'integrale utilizzando la quadratura di Gauss
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    double x = alpha * gaussPoints[j] + beta;
                    integral += gaussWeights[j] * Func(x);
                }
            }

            return alpha * integral;
        }
        //----------------------------------------------------------------------------------Metodi per i pulsanti:
        private void Calculate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double a = double.Parse(ATextBox.Text);
                double b = double.Parse(BTextBox.Text);
                int n = int.Parse(NTextBox.Text);

                //funzione inserita dall'utente:
                string functionExpression = FunctionTextBox.Text;
                Func<double, double> Func = CompileFunction(functionExpression);

                double integrale = 0;
                string selectedMethod = "";

                // Verifica quale metodo di integrazione è selezionato nell'ComboBox
                if (IntegrationMethodComboBox.SelectedItem != null)
                {
                    selectedMethod = (IntegrationMethodComboBox.SelectedItem as ComboBoxItem).Content.ToString();
                   
                    switch (selectedMethod)
                    {
                        case "Trapezoidal_rule":
                            integrale = Trapezoidal_rule(a, b, n, Func);
                            break;
                        case "Midpoint_rule":
                            integrale = Midpoint_rule(a, b, n, Func);
                            break;
                        case "Simpson's_rule":
                            integrale = Simpson_rule(a, b, n, Func);
                            break;
                        case "Gauss_quadrature":
                            integrale = Gauss_quadrature(a, b, n, Func);
                            break;
                        default:
                            MessageBox.Show("Invalid integration method selected.");
                            return;
                    }
                }
                else
                {
                    MessageBox.Show("Please select an integration method.");
                    return;
                }

                ResultTextBlock.Text = $"The definite integral of f(x) in the interval [{a}, {b}] using {selectedMethod} method is: {integrale}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An Error occured: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            ATextBox.Text = "";
            BTextBox.Text = "";
            NTextBox.Text = "";
            FunctionTextBox.Text = "";
            ResultTextBlock.Text = "";
        }
        //------------------------------------------------------------------------------------------------------
        private Func<double, double> CompileFunction(string functionExpression)
        {
            // Rimuovi tutti i caratteri non ammessi nell'espressione
            // il metodo where filtra i caratteri dell'espressione in base alle condizioni date,
            // quindi li converte in un array di caratteri e infine li concatena in una nuova stringa.
            // I caratteri ammessi sono cifre, lettere, spazi e gli operatori matematici "+", "-", "*", "/", "(" e ")"
            functionExpression = new string(functionExpression.Where(c => char.IsDigit(c) || char.IsLetter(c) || char.IsWhiteSpace(c) || "+-*/()".Contains(c)).ToArray());

            // Verifica se l'espressione contiene solo caratteri ammessi utilizzando una espressione regolare
            if (!Regex.IsMatch(functionExpression, @"^[A-Za-z0-9+\-*/().\s]+$"))
            {
                throw new InvalidOperationException("Invalid characters in function expression.");
            }

            string code = $@"using System;
                             using static System.Math;
                     namespace UserDefinedFunction 
                     {{ public static class FunctionEvaluator
                        {{ public static double Evaluate(double x)
                            {{ return {functionExpression};
                            }}
                        }}
                     }}";

            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);
            string assemblyName = Path.GetRandomFileName();
            MetadataReference[] references = new MetadataReference[]
            {
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Math).Assembly.Location)
            };

            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            // Viene utilizzato un blocco using per creare un flusso di memoria (MemoryStream) in cui verrà scritto l'output della compilazione.
            // Viene quindi chiamato il metodo Emit() per eseguire la compilazione e scrivere l'output nel flusso di memoria
            using (MemoryStream ms = new MemoryStream())
            {
                EmitResult result = compilation.Emit(ms);

                if (!result.Success)
                {
                    IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error);

                    throw new InvalidOperationException(string.Join(Environment.NewLine, failures.Select(f => f.GetMessage())));
                }
                else
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    Assembly assembly = Assembly.Load(ms.ToArray());
                    Type evaluatorType = assembly.GetType("UserDefinedFunction.FunctionEvaluator");
                    MethodInfo methodInfo = evaluatorType.GetMethod("Evaluate");
                    return (Func<double, double>)Delegate.CreateDelegate(typeof(Func<double, double>), methodInfo);
                }
            }
        }

        private void ResultTextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.SelectAll();
            }
        }

    }
}