using Microsoft.Windows.Storage.Pickers;
using ScreenMover.Services;
namespace ScreenMover;

public sealed partial class MainWindow {
    async Task<string?> SavePath(string name,string extension,string label){
        var picker=new FileSavePicker(AppWindow.Id){SuggestedFileName=name,DefaultFileExtension=extension,SuggestedStartLocation=PickerLocationId.DocumentsLibrary};
        picker.FileTypeChoices.Add(label,[extension]);
        return (await picker.PickSaveFileAsync())?.Path;
    }
    async Task<string?> OpenSettingsPath(){
        var picker=new FileOpenPicker(AppWindow.Id){SuggestedStartLocation=PickerLocationId.DocumentsLibrary};
        picker.FileTypeFilter.Add(".json");return (await picker.PickSingleFileAsync())?.Path;
    }
    async Task<bool> ExportSettings(){
        var path=await SavePath("ScreenMover-settings",".json",L.T("ScreenMover 设置"));if(path==null)return false;
        string profile=SettingsProfile.Export(settings);await Task.Run(()=>SettingsProfile.WriteText(path,profile));return true;
    }
    async Task<bool> ExportDiagnostics(){
        var path=await SavePath("ScreenMover-diagnostics-"+DateTime.Now.ToString("yyyyMMdd-HHmmss"),".zip",L.T("ScreenMover 诊断包"));if(path==null)return false;
        var current=settings with{};
        await Task.Run(()=>DiagnosticReport.Write(path,DiagnosticReport.Build(current,new DisplayService().Read().Displays)));return true;
    }
}
