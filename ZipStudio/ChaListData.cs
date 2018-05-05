using MessagePack;
using System.Collections.Generic;
using System.IO;

[MessagePackObject(true)]
public class ChaListData
{
    [IgnoreMember]
    public static readonly string ChaListDataMark = "【ChaListData】";

    public string mark
    {
        get;
        set;
    }

    public int categoryNo
    {
        get;
        set;
    }

    public int distributionNo
    {
        get;
        set;
    }

    public string filePath
    {
        get;
        set;
    }

    public List<string> lstKey
    {
        get;
        set;
    }

    public Dictionary<int, List<string>> dictList
    {
        get;
        set;
    }

    public ChaListData()
    {
        this.mark = string.Empty;
        this.categoryNo = 0;
        this.distributionNo = 0;
        this.filePath = string.Empty;
        this.lstKey = new List<string>();
        this.dictList = new Dictionary<int, List<string>>();
    }
}
