using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ZipStudio.Core;

namespace ZipStudio.GUI
{
    internal class ZipEntryNode : TreeNode
    {
        public ZipEntryInfo Entry { get; protected set; }

        public ZipEntryNode(ZipEntryInfo entry)
        {
            Entry = entry;

            SetColor();

            Text = entry.FileName.TrimEnd('/');

            if (Text.Contains('/'))
                Text = Text.Remove(0, Text.LastIndexOf('/') + 1);
        }

        protected void SetColor()
        {
            if (!Entry.IsDirectory)
            {
                if (Entry.FileName.Equals("manifest.xml", StringComparison.OrdinalIgnoreCase))
                {
                    ForeColor = Color.SlateBlue;
                    return;
                }

                if (Entry.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    ForeColor = Color.OrangeRed;
                    return;
                }
            }

            ForeColor = Color.Black;
        }

        public static IEnumerable<ZipEntryNode> GenerateNodes(IEnumerable<ZipEntryInfo> entries)
        {
            List<ZipEntryNode> topLevelList = new List<ZipEntryNode>();
            List<ZipEntryNode> allCreated = new List<ZipEntryNode>();

            foreach (var entry in entries)
            {
                if (!string.IsNullOrWhiteSpace(entry.FileName))
                    allCreated.Add(new ZipEntryNode(entry));
            }

            foreach (ZipEntryNode node in allCreated.OrderByDescending(x => x.Entry.FileName.TrimEnd('/').Count(y => y == '/')))
            {
                bool foundOwner = false;
                int slashCount = node.Entry.FileName.TrimEnd('/').Count(y => y == '/');

                foreach (ZipEntryNode potentialParentNode in allCreated)
                {
                    if (potentialParentNode.Entry.IsDirectory &&
                        node.Entry.FileName.StartsWith(potentialParentNode.Entry.FileName, StringComparison.OrdinalIgnoreCase) &&
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
