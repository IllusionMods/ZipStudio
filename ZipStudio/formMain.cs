using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZipStudio.Core;
using ZipStudio.GUI;

namespace ZipStudio
{
    public partial class formMain : Form
    {
        private Mod currentMod;

        public Mod CurrentMod
        {
            get => currentMod;
            set
            {
                currentMod = value;
                UpdateModDetails();
            }
        }

        private void UpdateModDetails()
        {
            trvZip.Nodes.Clear();

            trvZip.Nodes.AddRange(ZipEntryNode.GenerateNodes(currentMod.ZipFile).Cast<TreeNode>().ToArray());

            trvZip.Nodes[0]?.ExpandAll();
        }

        public formMain()
        {
            InitializeComponent();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "Compressed folders (*.zip)|*.zip",

            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            CurrentMod = new Mod(dialog.FileName);
        }

        private void folderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Importer.ImportFromDirectory(out Mod mod, out string message))
                CurrentMod = mod;
            else
                MessageBox.Show(message, "Error importing", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void formMain_Load(object sender, EventArgs e)
        {
            GenerateVersionInfo();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        #region UI Special

        private void GenerateVersionInfo()
        {
            lblVersionInfo.Text = $@"ZipStudio v{Assembly.GetExecutingAssembly().GetName().Version}

Core v{typeof(Manifest).Assembly.GetName().Version}

By Bepis";
        }

        private void imgLogo_DoubleClick(object sender, EventArgs e)
        {
            if (ModifierKeys.HasFlag(Keys.Control))
            {
                MessageBox.Show("Harsh a shit");
            }
        }

        #endregion
    }
}
