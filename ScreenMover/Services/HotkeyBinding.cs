namespace ScreenMover.Services;

public sealed class HotkeyBinding(Func<int,HotkeySpec,bool> register,Action<int> unregister) : IDisposable {
    readonly Action<int> release=unregister;
    public int CurrentId {get;private set;}
    public string Current {get;private set;}="";
    bool disposed,preparing;int pendingId;
    public Change Prepare(string? value){
        ObjectDisposedException.ThrowIf(disposed,this);
        if(preparing)throw new InvalidOperationException("快捷键正在修改");
        var spec=HotkeySpec.Parse(value);string name=spec?.Name??"";
        int id=name==Current?CurrentId:spec==null?0:CurrentId==0x5301?0x5302:0x5301;
        bool reserved=id!=0&&id!=CurrentId;
        if(reserved&&!register(id,spec!))throw new InvalidOperationException("该快捷键已被占用或被系统保留，请换一个组合");
        preparing=true;pendingId=reserved?id:0;return new(this,id,name,reserved);
    }
    public sealed class Change(HotkeyBinding owner,int id,string name,bool reserved) : IDisposable {
        bool done;
        public void Commit(){
            if(done)return;ObjectDisposedException.ThrowIf(owner.disposed,owner);done=true;
            if(owner.CurrentId!=0&&owner.CurrentId!=id)owner.release(owner.CurrentId);
            owner.CurrentId=id;owner.Current=name;owner.preparing=false;owner.pendingId=0;
        }
        public void Dispose(){if(done)return;done=true;if(reserved&&!owner.disposed)owner.release(id);owner.preparing=false;owner.pendingId=0;}
    }
    public void Dispose(){if(disposed)return;disposed=true;if(pendingId!=0)release(pendingId);if(CurrentId!=0)release(CurrentId);pendingId=CurrentId=0;Current="";}
}
