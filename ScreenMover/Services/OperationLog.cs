using System.Text;
namespace ScreenMover.Services;

public static class OperationLog {
    public const int MemoryEntries=500;
    const int MaximumReadBytes=2*1024*1024;
    public static IReadOnlyList<string> ReadTail(string path){
        if(!File.Exists(path))return [];
        using var file=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite);
        bool skip=file.Length>MaximumReadBytes;
        if(skip)file.Seek(-MaximumReadBytes,SeekOrigin.End);
        using var reader=new StreamReader(file,Encoding.UTF8);
        if(skip)reader.ReadLine();
        var lines=new Queue<string>();
        while(reader.ReadLine() is {} line){lines.Enqueue(ForDisplay(line));if(lines.Count>MemoryEntries)lines.Dequeue();}
        return lines.ToList();
    }
    public static string ForDisplay(string value)=>value.Length>4096?value[..4096]+L.T("…（记录已截断）"):value;
    public static void Append(string path,string entry){
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        if(File.Exists(path)&&new FileInfo(path).Length>=4*1024*1024)
            File.Move(path,Path.Combine(Path.GetDirectoryName(path)!,"operations-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff")+"-"+Guid.NewGuid().ToString("N")[..6]+".log"));
        if(entry.Length>65536)entry=entry[..65536]+L.T("…（记录已截断）");
        File.AppendAllText(path,entry+Environment.NewLine);
    }
}
