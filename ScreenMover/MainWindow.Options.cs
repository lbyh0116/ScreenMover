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
    Action? hiddenRecordsChanged;

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
        panel.Children.Add(new TextBlock{Text="手动移回的窗口会自动更新；找回后保留原桌面。",TextWrapping=TextWrapping.Wrap,Opacity=.75,FontSize=13});
        var list=new ListView{SelectionMode=ListViewSelectionMode.Single,MaxHeight=380};
        var detail=new TextBlock{TextWrapping=TextWrapping.Wrap};
        var row=new StackPanel{Orientation=Orientation.Horizontal,Spacing=8};
        var recover=new Button{Content="找回选中窗口",IsEnabled=false};
        var forget=new Button{Content="移除失效记录",IsEnabled=false};
        row.Children.Add(recover);row.Children.Add(forget);panel.Children.Add(list);panel.Children.Add(row);panel.Children.Add(detail);
        List<HiddenWindow>? displayed=null;
        void Populate(){
            var entries=hidden.Read().OrderByDescending(x=>x.HiddenAt).ToList();
            if(displayed!=null&&displayed.SequenceEqual(entries))return;
            var selectedId=((list.SelectedItem as ListViewItem)?.Tag as HiddenWindow)?.Id;
            displayed=entries;
            list.Items.Clear();
            foreach(var entry in entries){
                var card=new StackPanel{Spacing=7,Padding=new Thickness(12),MaxWidth=430};
                card.Children.Add(new TextBlock{Text=entry.Saved.Title,FontSize=16,FontWeight=Microsoft.UI.Text.FontWeights.SemiBold,TextTrimming=TextTrimming.CharacterEllipsis,MaxLines=2,TextWrapping=TextWrapping.Wrap});
                card.Children.Add(new TextBlock{Text=$"桌面：{entry.DesktopName}",FontSize=12,Opacity=.7,TextWrapping=TextWrapping.Wrap});
                card.Children.Add(new TextBlock{Text=entry.DisplayStatus,FontSize=13,TextWrapping=TextWrapping.Wrap,LineHeight=20});
                card.Children.Add(new TextBlock{Text=entry.HiddenAt.ToString("MM-dd HH:mm"),FontSize=12,Opacity=.6});
                var item=new ListViewItem{Content=card,Tag=entry,Margin=new Thickness(0,0,24,10),HorizontalContentAlignment=HorizontalAlignment.Stretch,Style=(Style)Application.Current.Resources["WindowItemStyle"]};
                list.Items.Add(item);
            }
            detail.Text=list.Items.Count==0?"暂无隐藏记录":"";
            recover.IsEnabled=forget.IsEnabled=false;
            list.SelectedItem=list.Items.OfType<ListViewItem>().FirstOrDefault(x=>(x.Tag as HiddenWindow)?.Id==selectedId);
            HiddenButton.Content=$"已隐藏 · {hidden.ActiveCount}";
        }
        void UpdateActions(){
            var entry=(list.SelectedItem as ListViewItem)?.Tag as HiddenWindow;
            bool alive=entry!=null&&!entry.Closed;
            recover.IsEnabled=alive&&!busy&&PlatformSupport.Current.CanHide;forget.IsEnabled=entry?.Closed==true&&!busy;
        }
        list.SelectionChanged+=(_,_)=>UpdateActions();
        var dialog=new ContentDialog{XamlRoot=Root.XamlRoot,Title="已隐藏窗口",CloseButtonText="关闭",Content=panel};
        recover.Click+=async(_,_)=>{
            if((list.SelectedItem as ListViewItem)?.Tag is not HiddenWindow entry)return;
            busy=true;UpdateStatus();recover.IsEnabled=forget.IsEnabled=false;dialog.IsEnabled=false;detail.Text="正在找回…";
            try{
                var result=await Task.Run(()=>hidden.Recover(entry));Populate();detail.Text=result.Message;
                Notice($"找回窗口：{entry.Saved.Title} · {result.Message}");
            }catch(Exception ex){detail.Text=ex.Message;}
            finally{busy=false;dialog.IsEnabled=true;UpdateStatus();await Refresh();UpdateActions();}
        };
        forget.Click+=(_,_)=>{
            if((list.SelectedItem as ListViewItem)?.Tag is not HiddenWindow entry||!entry.Closed)return;
            try{hidden.Forget(entry.Id);Populate();Notice("已移除失效记录："+entry.Saved.Title);}
            catch(Exception ex){detail.Text=ex.Message;}
        };
        try{
            await Task.Run(()=>hidden.Reconcile());
            Populate();hiddenRecordsChanged=Populate;
            await dialog.ShowAsync();
        }catch(Exception ex){Notice("读取隐藏记录失败："+ex.Message);}
        finally{hiddenRecordsChanged=null;dialogOpen=false;}
    }
}
