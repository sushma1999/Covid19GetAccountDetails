using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CharacterRecognition
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string imageFilePath;
        private string outputTextFilePath;
        private string OutputTextFromImage;
        private StringBuilder OutputWriterSB;
        private int totalImages;
        private int currentImage;

        public MainWindow()
        {
            InitializeComponent();
           
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Cursor = Cursors.Wait;
                imageFilePath = ImageFolder.Text;
                currentImage = 0;

                if (File.Exists(imageFilePath))
                {
                    await ProcessImage(imageFilePath);
                }
                else if (Directory.Exists(imageFilePath))
                {
                    string[] fileEntries = Directory.GetFiles(imageFilePath);
                    totalImages = fileEntries.Length;
                    foreach (string fileName in fileEntries)
                    {
                        currentImage++;
                        await ProcessImage(fileName);
                    }
                }
                MessageBox.Show("Processing of files Completed, Check the summary folder");
            }
            catch ( Exception ex)
            {
                MessageBox.Show(string.Concat(ex.Message.ToString(), ex.StackTrace.ToString()));
            }
            finally
            {
                this.Cursor = Cursors.Arrow;
            }
        }

        private async Task ProcessImage(string imagepath)
        {
            OutputWriterSB = new StringBuilder();
            OutputWriterSB.AppendLine("Generate Text Button clicked");

            //outputTextFilePath = OutputFolder.Text;
            OutputWriterSB.AppendLine("Read Image path from textbox");
            var textRecognition = TextRecognitionMode.Handwritten;
            TextOperationResult textResult;

            using (Stream imageFileStream = File.OpenRead(imagepath))
            {
                OutputWriterSB.AppendLine("Create stream from Image");
                textResult = await RecognizeAsync(
                    async (ComputerVisionClient client) => await client.RecognizeTextInStreamAsync(imageFileStream, textRecognition),
                    headers => headers.OperationLocation);
                OutputTextFromImage = LogTextRecognitionResult(textResult);
                //OutputTextBlock.Text = OutputWriterSB.ToString();
                OutputTextBlock.Text = $"Processing {currentImage} of {totalImages} images";
            }
            var fileName = outputTextFilePath + imagepath.Substring(imagepath.LastIndexOf('\\'), imagepath.LastIndexOf('.') - imagepath.LastIndexOf('\\')) + ".txt";
            writetoOutputFile(OutputTextFromImage, fileName);
        }

        private void writetoOutputFile(string fileContent, string fileName)
        {
            using (StreamWriter OutputTextSW = new StreamWriter(fileName))
            {
                OutputTextSW.WriteLine(fileContent);
            }
        }

        private async Task<TextOperationResult> RecognizeAsync<T>(Func<ComputerVisionClient, Task<T>> GetHeadersAsyncFunc, Func<T, string> GetOperationUrlFunc) where T : new()
        {
            // -----------------------------------------------------------------------
            // KEY SAMPLE CODE STARTS HERE
            // -----------------------------------------------------------------------
            var result = default(TextOperationResult);
            OutputWriterSB.AppendLine("Create result variable for storing result");
            //
            // Create Cognitive Services Vision API Service client.
            //
            var Credentials = new ApiKeyServiceClientCredentials("d8358f4194c8447bbca7c9e1605f15b0");
            var Endpoint = "https://sushmavisionapi.cognitiveservices.azure.com/";
            using (var client = new ComputerVisionClient(Credentials) { Endpoint = Endpoint })
            {
                Debug.WriteLine("ComputerVisionClient is created");
                OutputWriterSB.AppendLine("ComputerVisionClient is created");
                try
                {
                    Debug.WriteLine("Calling ComputerVisionClient.RecognizeTextAsync()...");
                    OutputWriterSB.AppendLine("Calling ComputerVisionClient.RecognizeTextAsync()...");

                    T recognizeHeaders = await GetHeadersAsyncFunc(client);
                    string operationUrl = GetOperationUrlFunc(recognizeHeaders);
                    string operationId = operationUrl.Substring(operationUrl.LastIndexOf('/') + 1);
                    Debug.WriteLine("Calling ComputerVisionClient.GetTextOperationResultAsync()...");
                    OutputWriterSB.AppendLine("Calling ComputerVisionClient.GetTextOperationResultAsync()...");
                    result = await client.GetTextOperationResultAsync(operationId);

                    for (int attempt = 1; attempt <= 3; attempt++)
                    {
                        if (result.Status == TextOperationStatusCodes.Failed || result.Status == TextOperationStatusCodes.Succeeded)
                        {
                            break;
                        }

                        Debug.WriteLine(string.Format("Server status: {0}, wait {1} seconds...", result.Status, TimeSpan.FromSeconds(3)));
                        await Task.Delay(TimeSpan.FromSeconds(3));

                        Debug.WriteLine("Calling ComputerVisionClient.GetTextOperationResultAsync()...");
                        result = await client.GetTextOperationResultAsync(operationId);

                    }

                }
                catch (Exception ex)
                {
                    result = new TextOperationResult() { Status = TextOperationStatusCodes.Failed };
                    Debug.WriteLine(ex.Message);
                }
                return result;
            }
        }

        /// <summary>
        /// Log text from the given TextOperationResult object.
        /// </summary>
        /// <param name="results">The TextOperationResult.</param>
        protected string LogTextRecognitionResult(TextOperationResult results)
        {
            StringBuilder stringBuilder = new StringBuilder();
            OutputWriterSB.AppendLine("Read the output from TextOperationResults and convert to string");
            if (results != null && results.RecognitionResult != null && results.RecognitionResult.Lines != null && results.RecognitionResult.Lines.Count > 0)
            {
                stringBuilder.Append("Text: ");
                stringBuilder.AppendLine();
                foreach (var line in results.RecognitionResult.Lines)
                {
                    foreach (var word in line.Words)
                    {
                        stringBuilder.Append(word.Text);
                        stringBuilder.Append(" ");
                    }

                    stringBuilder.AppendLine();
                    stringBuilder.AppendLine();
                }
            }

            if (string.IsNullOrWhiteSpace(stringBuilder.ToString()))
            {
                Debug.WriteLine("No text is recognized.");
            }
            else
            {
                Debug.WriteLine(stringBuilder.ToString());
            }

            if (results.Status == TextOperationStatusCodes.Running || results.Status == TextOperationStatusCodes.NotStarted)
            {
                Debug.WriteLine($"Status is {results.Status} after trying 3 times");
            }

            return stringBuilder.ToString();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //imageFilePath = @"C:\Covid19\Input";
            //outputTextFilePath = @"C:\Covid19\Output";
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {          
           
            VistaFolderBrowserDialog openDialog = new VistaFolderBrowserDialog();    
            var result = openDialog.ShowDialog();
            if(result==true)
            {
                this.ImageFolder.Text = openDialog.SelectedPath;
                this.imageFilePath = openDialog.SelectedPath;
            }
        }

        private void Browse2_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog openDialog = new VistaFolderBrowserDialog();
            var result = openDialog.ShowDialog();
            if (result == true)
            {
                this.OutputFolder.Text = openDialog.SelectedPath;
                this.outputTextFilePath = openDialog.SelectedPath;
            }
        }
    }
}
