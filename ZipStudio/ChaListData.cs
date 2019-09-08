using MessagePack;
using System.Collections.Generic;

[MessagePackObject(true)]
public class ChaListData
{
    [IgnoreMember]
    public static readonly string ChaListDataMark = "【ChaListData】";

    public string mark { get; set; }
    public int categoryNo { get; set; }
    public int distributionNo { get; set; }
    public string filePath { get; set; }
    public List<string> lstKey { get; set; }
    public Dictionary<int, List<string>> dictList { get; set; }

    public ChaListData()
    {
        mark = string.Empty;
        categoryNo = 0;
        distributionNo = 0;
        filePath = string.Empty;
        lstKey = new List<string>();
        dictList = new Dictionary<int, List<string>>();
    }
}
