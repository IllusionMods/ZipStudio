using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ionic.Zip;

namespace ZipStudio.GUI
{
    class ZipEntryNode : TreeNode
    {
        public ZipEntry Entry { get; protected set; }

        public ZipEntryNode(ZipEntry entry)
        {
            Entry = entry;

            SetColor();

            Text = entry.FileName.TrimEnd('/');

            if (Text.Contains('/'))
                Text = Text.Remove(0, Text.IndexOf('/') + 1);
        }

        protected void SetColor()
        {
            if (!Entry.IsDirectory)
            {
                if (Entry.FileName.ToLower() == "manifest.xml")
                {
                    ForeColor = Color.SlateBlue;
                    return;
                }
                
                if (Entry.FileName.EndsWith(".csv"))
                {
                    ForeColor = Color.OrangeRed;
                    return;
                }
            }

            ForeColor = Color.Black;
        }

        public static IEnumerable<ZipEntryNode> GenerateNodes(ZipFile zipFile)
        {
            List<ZipEntryNode> topLevelList = new List<ZipEntryNode>();
            List<ZipEntryNode> allCreated = new List<ZipEntryNode>();

            foreach (ZipEntry entry in zipFile)
            {
                if (!string.IsNullOrWhiteSpace(entry.FileName))
                    allCreated.Add(new ZipEntryNode(entry));
            }
            
            foreach (ZipEntryNode node in allCreated.OrderByDescending(x => x.Entry.FileName.Count(y => y == '/')))
            {
                bool foundOwner = false;
                int slashCount = node.Entry.FileName.TrimEnd('/').Count(y => y == '/');

                foreach (ZipEntryNode potentialParentNode in allCreated)
                {
                    if (potentialParentNode.Entry.IsDirectory &&
                        node.Entry.FileName.StartsWith(potentialParentNode.Entry.FileName) &&
                        potentialParentNode.Entry.FileName.TrimEnd('/').Count(x => x == '/') == slashCount - 1)
                    {
                        foundOwner = true;
                        potentialParentNode.Nodes.Add(node);
                    }
                }

                if (!foundOwner)
                {
                    topLevelList.Add(node);
                }
            }

            return topLevelList;
        }
    }
}
