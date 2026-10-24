namespace ScreenMover.Services;

public sealed record HotkeySpec(uint Modifiers,uint Key,string Name) {
    public static string KeyName(uint key)=>key switch {
        >=65 and <=90=>((char)key).ToString(),>=48 and <=57=>((char)key).ToString(),
        >=112 and <=135=>"F"+(key-111),32=>"Space",9=>"Tab",33=>"PageUp",34=>"PageDown",35=>"End",36=>"Home",
        37=>"Left",38=>"Up",39=>"Right",40=>"Down",45=>"Insert",46=>"Delete",_=>""
    };
    public static HotkeySpec? Parse(string? text){
        if(string.IsNullOrWhiteSpace(text))return null;
        if(text.Length>96)throw new FormatException(L.T("快捷键内容过长"));
        var parts=text.Split('+',StringSplitOptions.TrimEntries);
        uint mods=0;
        foreach(string part in parts[..^1]){
            uint flag=part.ToUpperInvariant() switch{"CTRL" or "CONTROL"=>2,"ALT"=>1,"SHIFT"=>4,"WIN"=>8,_=>0};
            if(flag==0||(mods&flag)!=0)throw new FormatException(L.T("快捷键修饰键无效或重复"));
            mods|=flag;
        }
        if((mods&11)==0)throw new FormatException(L.T("全局快捷键需要包含 Ctrl、Alt 或 Win"));
        uint key=Enumerable.Range(0,256).Select(v=>(uint)v).FirstOrDefault(v=>KeyName(v).Equals(parts[^1],StringComparison.OrdinalIgnoreCase)&&KeyName(v)!="");
        if(key==0||key==123)throw new FormatException(L.T("请选择字母、数字、功能键或方向键；F12 为系统保留键"));
        if((mods==8&&(key is 76 or 68 or 69 or 82 or 9 or 32))||((mods&3)==3&&key==46)||(mods==1&&(key is 9 or 115))||(mods==2&&key==27))
            throw new FormatException(L.T("该组合为 Windows 保留快捷键"));
        var names=new List<string>();if((mods&2)!=0)names.Add("Ctrl");if((mods&1)!=0)names.Add("Alt");if((mods&4)!=0)names.Add("Shift");if((mods&8)!=0)names.Add("Win");
        names.Add(KeyName(key));return new(mods,key,string.Join('+',names));
    }
}
