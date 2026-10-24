using System.IO.Pipes;
using System.Runtime.InteropServices;
namespace ScreenMover.Services;

public sealed class SingleInstance : IDisposable {
    [DllImport("user32.dll")] static extern bool AllowSetForegroundWindow(uint process);
    readonly Mutex mutex;
    readonly CancellationTokenSource stop=new();
    readonly string name;
    Task? listener;
    int disposed;
    public static string Name {get{using var identity=System.Security.Principal.WindowsIdentity.GetCurrent();using var process=System.Diagnostics.Process.GetCurrentProcess();return "ScreenMover-"+identity.User?.Value+"-"+process.SessionId;}}
    SingleInstance(string key,Mutex gate){name=key;mutex=gate;}
    public static SingleInstance? Claim(string name){
        var gate=new Mutex(false,@"Local\"+name);
        bool acquired;try{acquired=gate.WaitOne(0);}catch(AbandonedMutexException){acquired=true;}
        if(acquired)return new(name,gate);
        gate.Dispose();return null;
    }
    public void Listen(Action activate){
        ObjectDisposedException.ThrowIf(disposed!=0,this);
        if(listener!=null)throw new InvalidOperationException(L.T("实例监听已经启动"));
        listener=Task.Run(async()=>{
            while(!stop.IsCancellationRequested){
                try{
                    using var server=new NamedPipeServerStream(name,PipeDirection.InOut,1,PipeTransmissionMode.Byte,PipeOptions.Asynchronous|PipeOptions.CurrentUserOnly);
                    await server.WaitForConnectionAsync(stop.Token);
                    using var limit=CancellationTokenSource.CreateLinkedTokenSource(stop.Token);limit.CancelAfter(3000);
                    using var reader=new StreamReader(server,leaveOpen:true);
                    using var writer=new StreamWriter(server,leaveOpen:true){AutoFlush=true};
                    await writer.WriteLineAsync(Environment.ProcessId.ToString());
                    if(await ReadShortLine(reader,limit.Token)=="show"){activate();await writer.WriteLineAsync("ok");}
                }catch(OperationCanceledException){}catch(ObjectDisposedException){}
                catch(Exception ex)when(ex is IOException or UnauthorizedAccessException or InvalidDataException){
                    try{await Task.Delay(200,stop.Token);}catch(OperationCanceledException){}
                }
            }
        });
    }
    public static async Task<bool> Activate(string name){
        try{
            using var timeout=new CancellationTokenSource(5000);
            using var client=new NamedPipeClientStream(".",name,PipeDirection.InOut,PipeOptions.Asynchronous|PipeOptions.CurrentUserOnly);
            await client.ConnectAsync(timeout.Token);
            using var reader=new StreamReader(client,leaveOpen:true);
            using var writer=new StreamWriter(client,leaveOpen:true){AutoFlush=true};
            if(uint.TryParse(await ReadShortLine(reader,timeout.Token),out uint process))AllowSetForegroundWindow(process);
            await writer.WriteLineAsync("show");
            return await ReadShortLine(reader,timeout.Token)=="ok";
        }catch(Exception ex)when(ex is IOException or InvalidDataException or OperationCanceledException or UnauthorizedAccessException){return false;}
    }
    public static async Task<string?> ReadShortLine(StreamReader reader,CancellationToken token){
        var text=new System.Text.StringBuilder();var one=new char[1];
        while(await reader.ReadAsync(one,token)!=0){if(one[0]=='\n')return text.ToString().TrimEnd('\r');if(text.Length>=32)throw new InvalidDataException(L.T("实例消息过长"));text.Append(one[0]);}
        return text.Length==0?null:text.ToString();
    }
    public void Dispose(){if(Interlocked.Exchange(ref disposed,1)!=0)return;stop.Cancel();if(listener==null)stop.Dispose();else _=listener.ContinueWith(_=>stop.Dispose(),TaskScheduler.Default);try{mutex.ReleaseMutex();}catch(ApplicationException){}mutex.Dispose();}
}
