using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TextFileProcessor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string inputFilesPath;
        private string outputFilesPath;
        private StringBuilder OutputSummarySB = new StringBuilder();
        private BankDetails bankdetail;
        private int totalFiles;
        private int currentFile;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog openDialog = new VistaFolderBrowserDialog();
            var result = openDialog.ShowDialog();
            if (result == true)
            {
                this.txtInputPath.Text = openDialog.SelectedPath;
                this.inputFilesPath = openDialog.SelectedPath;
            }
        }

        private void UploadOutput_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog openDialog = new VistaFolderBrowserDialog();
            var result = openDialog.ShowDialog();
            if (result == true)
            {
                this.txtOutputPath.Text = openDialog.SelectedPath;
                this.outputFilesPath = openDialog.SelectedPath;
            }

        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Cursor = Cursors.Wait;
                currentFile = 0;

                if (File.Exists(inputFilesPath))
                {
                    OutputSummarySB.AppendLine(await ProcessTextFiles(inputFilesPath));
                }
                else if (Directory.Exists(inputFilesPath))
                {
                    string[] fileEntries = Directory.GetFiles(inputFilesPath);
                    totalFiles = fileEntries.Length;
                    foreach (string fileName in fileEntries)
                    {
                        currentFile++;
                        OutputSummarySB.AppendLine(await ProcessTextFiles(fileName));
                        SummaryTextBlock.Text = $"Processing {currentFile} of {totalFiles} files";
                    }
                }

                var OutputFileName = System.IO.Path.Combine(outputFilesPath, "Summary" + DateTime.Now.ToString("MM-dd-yyyy-H-mm") + ".csv");
                writetoOutputFile(OutputSummarySB.ToString(), OutputFileName);
                MessageBox.Show("Processing of files Completed, Check the summary folder");
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Concat(ex.Message.ToString(), ex.StackTrace.ToString()));
            }
            finally
            {
                this.Cursor = Cursors.Arrow;
            }
        }

        private Task<string> ProcessTextFiles(string fileName)
        {
            bankdetail = new BankDetails();
            using (StreamReader sr = new StreamReader(fileName))
            {
                var FullText = sr.ReadToEnd();
                ProcessTextUsingRegex(FullText);
                int startIndex = fileName.LastIndexOf('\\') + 1;
                int length = fileName.LastIndexOf('.') - startIndex;
                fileName = fileName.Substring(startIndex, length);
                return Task.FromResult($" {fileName.Trim()} | {bankdetail.AccountNo} | {bankdetail.IFSCCode} | { bankdetail.BankName} | { bankdetail.BranchName} | Mob: {bankdetail.Mobile} | {bankdetail.PersonName} ");

            }
        }

        private void ProcessText(string line)
        {
            double outputNumber;

            if (string.IsNullOrWhiteSpace(line))
                return;

            if (line.ToUpper().Contains("ACCOUNT") || ((line.Length > 8) && (Double.TryParse(line, out outputNumber))))
            {
                bankdetail.AccountNo = bankdetail.AccountNo + " " + line;
            }

            if (line.ToUpper().Contains("NAME") || line.ToUpper().Contains("MRS"))
            {
                bankdetail.PersonName = bankdetail.PersonName + " " + line;
            }

            if (line.ToUpper().Contains("BANK"))
            {
                bankdetail.BankName = bankdetail.BankName + " " + line;
            }

            if ((line.Length == 10) && (Double.TryParse(line, out outputNumber)))
            {
                bankdetail.Mobile = bankdetail.Mobile + " " + line;
            }

            if (line.ToUpper().Contains("BR") || line.ToUpper().Contains("BRANCH"))
            {
                bankdetail.BranchName = bankdetail.BranchName + " " + line;
            }

            if (line.ToUpper().Contains("IFSC") || line.ToUpper().Contains("IOBA") || line.ToUpper().Contains("IDIB") ||
                line.ToUpper().Contains("SBIN") || line.ToUpper().Contains("BARB") || line.ToUpper().Contains("BKID") ||
                line.ToUpper().Contains("SYNB") || line.ToUpper().Contains("VIJB") || line.ToUpper().Contains("UCBA") ||
                line.ToUpper().Contains("UTBI") || line.ToUpper().Contains("UTIB") || line.ToUpper().Contains("CNRB") ||
                line.ToUpper().Contains("ANDB") || line.ToUpper().Contains("HDFC") || line.ToUpper().Contains("CBIN") ||
                line.ToUpper().Contains("ICIC") || line.ToUpper().Contains("ORBC") || line.ToUpper().Contains("SIBL") ||
                line.ToUpper().Contains("PUNB") || line.ToUpper().Contains("TMBL") || line.ToUpper().Contains("UBIN") || line.ToUpper().Contains("FDRL"))
            {
                bankdetail.IFSCCode = bankdetail.IFSCCode + " " + line;
            }

            if (line.ToUpper().Contains("NAGAR") || line.ToUpper().Contains("STREET") || line.ToUpper().Contains("ROAD") || line.ToUpper().Contains("INDIA") || line.ToUpper().Contains("TAMIL"))
            {
                bankdetail.Address = bankdetail.Address + " " + line;
            }


        }

        private void ProcessTextUsingRegex(string fullText)
        {
            getIFSCCode(fullText);
            if (!string.IsNullOrWhiteSpace(bankdetail.IFSCCode))
            {
                getBankDetails(fullText);
            }
            getAccountNo(fullText);
            getBranch(fullText);

            getPhoneNo(fullText);

            getName(fullText);


        }

        private void getName(string fullText)
        {
            string[] lines = fullText.Split('\n');

            int i = 0;
            foreach (var line in lines)
            {

                if (line.ToUpper().Contains("MR") || line.ToUpper().Contains("MRS"))
                    bankdetail.PersonName = string.Concat(lines[i - 1].Trim(), lines[i].Trim(), lines[i + 1].Trim());

                i++;
            }

            i = 0;
            foreach (var line in lines)
            {

                if (line.ToUpper().Contains("NAME"))
                    bankdetail.PersonName = string.Concat(bankdetail.PersonName, lines[i - 1].Trim(), lines[i].Trim(), lines[i + 1].Trim());

                i++;
            }
        }

        private void getPhoneNo(string fullText)
        {
            foreach (var word in fullText.Split(' '))
            {
                Regex regex = new Regex("^[6-9]{1}[0-9]{9}$");
                Match match = regex.Match(word.Trim());
                if (match.Success)
                    bankdetail.Mobile = match.Value;
            }
        }

        private void getAccountNo(string fullText)
        {
            Regex regex;
            //if (!string.IsNullOrWhiteSpace(bankdetail.IFSCCode) && bankdetail.IFSCCode.Contains("IDIB"))
            //    regex = new Regex("^[0-9]{10,16}$");
            //else
            regex = new Regex("^[0-9]{10,16}$");
            foreach (var word in fullText.Split(' '))
            {
                Match match = regex.Match(word.Trim());
                if (match.Success)
                {
                    if (string.IsNullOrWhiteSpace(bankdetail.AccountNo))
                        bankdetail.AccountNo = match.Value;
                    else
                        bankdetail.AccountNo = string.Concat(bankdetail.AccountNo, "-", match.Value);
                }

            }




        }

        private void getBranch(string fullText)
        {
            string[] lines = fullText.Split('\n');
            int i = 0;
            foreach (var line in lines)
            {

                if (line.ToUpper().Contains("BRANCH"))
                    bankdetail.BranchName = string.Concat(lines[i - 1].Trim(), lines[i].Trim(), lines[i + 1].Trim());

                i++;
            }

            i = 0;
            foreach (var line in lines)
            {

                if (line.ToUpper().Contains("NAME") || line.ToUpper().Contains("BR"))
                    bankdetail.BranchName = string.Concat(bankdetail.BranchName, lines[i - 1].Trim(), lines[i].Trim(), lines[i + 1].Trim());

                i++;
            }


        }

        private void getBankDetails(string fullText)
        {
            switch (bankdetail.IFSCCode.Substring(0, 4))
            {
                case "JOBA":
                    bankdetail.BankName = "Indian Overseas Bank";
                    break;
                case "IOBA":
                    bankdetail.BankName = "Indian Overseas Bank";
                    break;
                case "BKID":
                    bankdetail.BankName = "Bank Of India";
                    break;
                case "ICIC":
                    bankdetail.BankName = "ICICI Bank";
                    break;
                case "HDFC":
                    bankdetail.BankName = "HDFC Bank";
                    break;
                case "SBIN":
                    bankdetail.BankName = "State Bank Of India";
                    break;
                case "BARB":
                    bankdetail.BankName = "Bank Of Baroda";
                    break;
                case "IDIB":
                    bankdetail.BankName = "Indian Bank";
                    break;
                case "VIJB":
                    bankdetail.BankName = "Vijaya Bank";
                    break;
                case "IBKL":
                    bankdetail.BankName = "IDBI Bank";
                    break;
                case "KVBL":
                    bankdetail.BankName = "Karur Vysya Bank";
                    break;
                case "ANDB":
                    bankdetail.BankName = "Andhra Bank";
                    break;
                case "CBIN":
                    bankdetail.BankName = "Central Bank Of India";
                    break;
                case "CNRB":
                    bankdetail.BankName = "Canara Bank";
                    break;
                case "PUNB":
                    bankdetail.BankName = "Punjab National Bank";
                    break;
                default:
                    bankdetail.BankName = "Unknown";
                    break;

            }
        }

        private void getIFSCCode(string fullText)
        {
            foreach (var word in fullText.Split(' '))
            {
                Regex regex = new Regex("^[A-Za-z]{4}[0-9]{7}$");
                Match match = regex.Match(word.Trim());
                if (match.Success)
                    bankdetail.IFSCCode = match.Value;
            }
            if (!string.IsNullOrWhiteSpace(bankdetail.IFSCCode))
                return;
            foreach (var word in fullText.Split(' '))
            {
                Regex regex = new Regex("^[A-Za-z]{4}[A-Za-z0-9]{4}[0-9]{3}$");
                Match match = regex.Match(word.Trim());
                if (match.Success)
                    bankdetail.IFSCCode = match.Value;
            }

            if (!string.IsNullOrWhiteSpace(bankdetail.IFSCCode))
                return;

            int i = 0;
            string[] words = fullText.Split(' ');
            foreach (var word in words)
            {
                if (word.ToUpper().Contains("IFSC"))
                    bankdetail.IFSCCode = string.Concat(words[i].Trim(), "-", words[i + 1].Trim(), "-", words[i + 2].Trim(), "-", words[i + 3].Trim());
                i++;
            }
        }

        private void writetoOutputFile(string fileContent, string fileName)
        {
            using (StreamWriter OutputTextSW = new StreamWriter(fileName))
            {
                OutputTextSW.WriteLine(fileContent);
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }
    }
}
