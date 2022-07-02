using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace FolderAnalytics
{
    public partial class Form1 : Form
    {
        public bool IsProcess = false;

        public uint DirCount = 0;
        public uint FileCount = 0;
        public uint ErrorCount = 0;
        public Dictionary<string, uint> FilesExtension = new Dictionary<string, uint>();
        public Dictionary<string, long> TotalFilesSizeWithExtension = new Dictionary<string, long>();
        public Dictionary<string, string> TotalFilesPathWithExtension = new Dictionary<string, string>();
        public Dictionary<string, Queue<string>> TotalFilesExtensionWithPath = new Dictionary<string, Queue<string>>();

        public Form1()
        {
            InitializeComponent();


            listView1.Columns.Add(new ColumnHeader() { Text = "Extension", Width = 200 });
            listView1.Columns.Add(new ColumnHeader() { Text = "Quantity", Width = 150 });
            listView1.Columns.Add(new ColumnHeader() { Text = "Overall size", Width = 100 });

            listView1.View = View.Details;
        }

        public void MakeDataInfo()
        {
            listView1.Items.Clear();

            Dictionary<string, uint> TempDictionary = new Dictionary<string, uint>();
            foreach (var pair in FilesExtension.OrderByDescending(pair => pair.Value))
                if (pair.Key != "")
                {
                    //Console.WriteLine($"{pair.Key}   x{pair.Value}");
                    TempDictionary.Add(pair.Key, pair.Value);
                }

            foreach (var pair in TotalFilesSizeWithExtension.OrderByDescending(pair => pair.Value))
                if (pair.Key != "")
                {
                    listView1.Items.Add(new ListViewItem(new string[] {
                        pair.Key,
                        TempDictionary[pair.Key].ToString(),
                        (Math.Round((float)(pair.Value) / 1024 / 1024, 2)).ToString() + "mb"
                    })
                    { Tag = pair.Key });
                }

            WriteLine("Directories count - " + DirCount.ToString());
            WriteLine("Files count       - " + FileCount.ToString());
        }

        Stopwatch _sw = new Stopwatch();
        uint _step = 0;
        uint _error = 0;
        public void ProcDir(string path)
        {
            // Recurse into subdirectories of this directory.
            string[] subdirectoryEntries = { };
            try { subdirectoryEntries = System.IO.Directory.GetDirectories(path); }
            catch (Exception ex) { _error++; }

            foreach (string subdirectory in subdirectoryEntries)
            {
                DirCount++;
                ProcDir(subdirectory);
            }

            // Process the list of files found in the directory.
            string[] fileEntries = { };
            try { fileEntries = System.IO.Directory.GetFiles(path); }
            catch (Exception ex) { _error++; }
            foreach (string fileName in fileEntries)
            {
                _step++;
                try
                {
                    if (checkBox1.Checked && new FileInfo(fileName).Extension != textBox2.Text) continue;
                    FileCount++;

                    TotalFilesPathWithExtension.Add(fileName, new FileInfo(fileName).Extension);

                    if (FilesExtension.ContainsKey(new FileInfo(fileName).Extension))
                        FilesExtension[new FileInfo(fileName).Extension]++;
                    else
                        FilesExtension.Add(new FileInfo(fileName).Extension, 1);

                    if (TotalFilesSizeWithExtension.ContainsKey(new FileInfo(fileName).Extension))
                        TotalFilesSizeWithExtension[new FileInfo(fileName).Extension] += new FileInfo(fileName).Length;
                    else
                        TotalFilesSizeWithExtension.Add(new FileInfo(fileName).Extension, new FileInfo(fileName).Length);

                    if (TotalFilesExtensionWithPath.ContainsKey(new FileInfo(fileName).Extension))
                        TotalFilesExtensionWithPath[new FileInfo(fileName).Extension].Enqueue(fileName);
                    else
                    {
                        TotalFilesExtensionWithPath.Add(new FileInfo(fileName).Extension, new Queue<string>());
                        TotalFilesExtensionWithPath[new FileInfo(fileName).Extension].Enqueue(fileName);
                    }
                }
                catch (Exception ex)
                {
                    _error++;
                }
            }

            if (_sw.ElapsedMilliseconds >= 10)
            {
                var invok = BeginInvoke((Action)delegate
                {
                    richTextBox1.Text =
                        $"\nLoad files" +
                        $"\nThe program may freeze" +
                        "\nItems processed : " + _step.ToString() +
                        "\nnErrors : " + _error.ToString() +
                        "\nError rate : " + (Math.Round((float)((float)(_error) / (float)(_step)), 8)*100).ToString() + "%";
                    label1.Text = path;
                });
                _sw.Restart();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (IsProcess)
            {
                MessageBox.Show("Wait for an already running process to finish !", "Problem",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _step = 0;
            _error = 0;
            listView1.Items.Clear();
            richTextBox1.Text = "Wait";
            richTextBox1.Enabled = false;
            IsProcess = true;
            label1.Visible = true;
            _sw.Start();

            Task.Run(() => {
                Stopwatch sw = new Stopwatch(); sw.Start();
                ProcDir(textBox1.Text);
                var invoke = BeginInvoke((Action)delegate
                {
                    richTextBox1.Enabled = true;
                    richTextBox1.Clear();
                    richTextBox1.Text += $"\n\nProcess time - {sw.ElapsedMilliseconds}ms";
                    MakeDataInfo();
                    IsProcess = false;
                    label1.Visible = false;
                });
            });
        }

        private void WriteLine(string text) => 
            richTextBox1.Text += $"\n{text}";

        private void button3_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
            listView1.Clear();
        }

        private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (IsProcess)
            {
                MessageBox.Show("Wait for an already running process to finish !", "Problem",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            richTextBox1.Clear();
            richTextBox1.Text = "Wait";
            richTextBox1.Enabled = false;
            IsProcess = true;
            label1.Visible = true;

            foreach (ListViewItem v in listView1.SelectedItems)
            {
                Task.Run(() =>
                {
                    Stopwatch sw = new Stopwatch(); sw.Start();
                    string output = "";
                    uint step = 0;
                    /*foreach (string path in TotalFilesPathWithExtension.Keys)
                    {
                        if (new FileInfo(path).Extension == v.Tag.ToString())
                        {
                            step++;
                            output += "\n" + path;
                            if (sw.ElapsedMilliseconds >= 10)
                            {
                                var invok2 = BeginInvoke((Action)delegate
                                {
                                    richTextBox1.Text =
                                        $"\nПоиск {v.Tag.ToString()} файлов" +
                                        "\nОбработанно элементов : " + step.ToString() +
                                        "\nИз : " + FilesExtension[v.Text].ToString() +
                                        "\nОбщий размер файлов с таким расширением : " + TotalFilesSizeWithExtension[v.Text].ToString();
                                    label1.Text = path;
                                });
                                sw.Restart();
                            }
                        }
                    }*/
                    if (TotalFilesExtensionWithPath.ContainsKey(v.Text))
                    { 
                        foreach (string path in TotalFilesExtensionWithPath[v.Text])
                        {
                            step++;
                            output += "\n" + path;
                            
                            if (sw.ElapsedMilliseconds >= 10)
                            {
                                var invok2 = BeginInvoke((Action)delegate
                                {
                                    richTextBox1.Text =
                                        $"\nSearch {v.Tag.ToString()} files" +
                                        "\nItems processed : " + step.ToString() +
                                        "\nOf : " + FilesExtension[v.Text].ToString() +
                                        "\nThe total size of files with this extension : " + TotalFilesSizeWithExtension[v.Text].ToString();
                                    label1.Text = path;
                                });
                                sw.Restart();
                            }
                        }
                    }
                    var invoke = BeginInvoke((Action)delegate
                    {
                        richTextBox1.Enabled = true;
                        richTextBox1.Clear();
                        WriteLine(output);
                        IsProcess = false;
                        label1.Visible = false;
                    });
                });
            }
        }

        public static void GetAllFiles(string rootDirectory, List<string> files)
        {
            string[] directories = Directory.GetDirectories(rootDirectory);
            files.AddRange(Directory.GetFiles(rootDirectory));

            foreach (string path in directories)
                GetAllFiles(path, files);
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            this.textBox2.Enabled = checkBox1.Checked;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult dialog = this.folderBrowserDialog1.ShowDialog();

            if (dialog == DialogResult.OK)
            {
                this.textBox1.Text = this.folderBrowserDialog1.SelectedPath;
            }
        }
    }
}
