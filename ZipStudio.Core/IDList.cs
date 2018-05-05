using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ZipStudio.Core
{
    public class IDList
    {
        public int CategoryID { get; set; }

        public int DistributionID { get; set; }

        public string DataPath { get; set; }

        public IList<string> DataColumns { get; set; }

        public IList<string> DataRows { get; set; }

        public static IDList FromCSV(StringReader reader)
        {
            try
            {
                IDList list = new IDList();

                list.CategoryID = int.Parse(reader.ReadLine().Trim());
            }
            catch (Exception ex) when
                (ex is exception)
            {

            }
        }
    }
}
