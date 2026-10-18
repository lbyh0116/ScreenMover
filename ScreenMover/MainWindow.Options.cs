using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ScreenMover.Models;
using ScreenMover.Services;
namespace ScreenMover;

public sealed partial class MainWindow {
    AppSettings settings=new();
    readonly MoveHistory moves=new();
    readonly HiddenWindows hidden=new();
    bool initialized,dialogOpen;

    void SearchChanged(object sender,TextChangedEventArgs e){if(initialized)UpdateWindows();}
    void ScopeChanged(object sender,RoutedEventArgs e){if(initialized){UpdateWindows();RebuildButtons();}}
    async void UndoClicked(object sender,RoutedEventArgs e) {
        if(busy||moves.Count==0)return;
        busy=true;UpdateStatus();
        try{ShowResults("撤销移动",await Task.Run(moves.Undo));}
        catch(Exception ex){Notice("撤销失败："+ex.Message);}
        finally{busy=false;await Refresh();UpdateStatus();}
    }
    void ShowResults(string verb,IReadOnlyList<OperationResult> results) =>
        Notice($"{verb} {results.Count} 个窗口：成功 {results.Count(r=>r.Success)}，失败 {results.Count(r=>!r.Success)}"+
        string.Concat(results.Select(r=>$"\n{r.Title}：{r.Message}")));
    async Task MoveBatch(List<WindowInfo> items,DisplayInfo target) {
        if(busy||items.Count==0)return;
        busy=true;UpdateStatus();
        try{ShowResults("移动",await Task.Run(()=>moves.Move(items,target,settings.MoveMargin,settings.ShrinkToFit)));}
        catch(Exception ex){Notice("移动失败："+ex.Message);}
        finally{busy=false;await Refresh();UpdateStatus();}
    }
    async void HiddenClicked(object sender,RoutedEventArgs e) {
        if(dialogOpen||busy)return;dialogOpen=true;
        var panel=new StackPanel{Spacing=12,MinWidth=360};
        panel.Children.Add(new TextBlock{Text="选择记录后可找回当前虚拟桌面。原桌面会保留，避免影响其中的其他窗口。记录会在软件重启后保留。",TextWrapping=TextWrapping.Wrap});
        var list=new ListView{SelectionMode=ListViewSelectionMode.Single,MaxHeight=300};
        var detail=new TextBlock{TextWrapping=TextWrapping.Wrap};
        var row=new StackPanel{Orientation=Orientation.Horizontal,Spacing=8};
        var recover=new Button{Content="找回选中窗口",IsEnabled=false};
        var forget=new Button{Content="移除失效记录",IsEnabled=false};
        row.Children.Add(recover);row.Children.Add(forget);panel.Children.Add(list);panel.Children.Add(row);panel.Children.Add(detail);
        void Populate(){
            list.Items.Clear();
            foreach(var entry in hidden.Read().OrderByDescending(x=>x.HiddenAt)){
                var text=new TextBlock{Text=entry.ToString()+(MoveHistory.Alive(entry.Saved)?"":"\n原窗口已关闭 / 进程已重启"),TextWrapping=TextWrapping.Wrap,MaxWidth=430};
                list.Items.Add(new ListViewItem{Content=text,Tag=entry});
            }
            if(list.Items.Count==0)detail.Text="暂无隐藏记录";
            recover.IsEnabled=forget.IsEnabled=false;
            HiddenButton.Content=$"已隐藏 · {hidden.Read().Count}";
        }
        list.SelectionChanged+=(_,_)=>{
            var entry=(list.SelectedItem as ListViewItem)?.Tag as HiddenWindow;
            bool alive=entry!=null&&MoveHistory.Alive(entry.Saved);
            recover.IsEnabled=alive&&!busy&&PlatformSupport.Current.CanHide;forget.IsEnabled=entry!=null&&!alive&&!busy;
        };
        var dialog=new ContentDialog{XamlRoot=Root.XamlRoot,Title="已隐藏窗口",CloseButtonText="关闭",Content=panel};
        recover.Click+=async(_,_)=>{
            if((list.SelectedItem as ListViewItem)?.Tag is not HiddenWindow entry)return;
            busy=true;UpdateStatus();recover.IsEnabled=forget.IsEnabled=false;dialog.IsEnabled=false;detail.Text="正在找回…";
            try{
                var result=await Task.Run(()=>hidden.Recover(entry));Populate();detail.Text=result.Message;
                Notice($"找回窗口：{entry.Saved.Title} · {result.Message}");
            }catch(Exception ex){detail.Text=ex.Message;}
            finally{busy=false;dialog.IsEnabled=true;UpdateStatus();await Refresh();}
        };
        forget.Click+=(_,_)=>{
            if((list.SelectedItem as ListViewItem)?.Tag is not HiddenWindow entry||MoveHistory.Alive(entry.Saved))return;
            try{hidden.Forget(entry.Id);Populate();Notice("已移除失效记录："+entry.Saved.Title);}
            catch(Exception ex){detail.Text=ex.Message;}
        };
        Populate();
        try{await dialog.ShowAsync();}finally{dialogOpen=false;}
    }
}
