using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using ScreenMover.UpdateCore;
class Program {
    [STAThread] static int Main(string[] args){
        string folder=Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string log=Path.Combine(folder,"result.txt");UpdatePlan plan=null;
        try{
            if(args.Length!=1||!Path.GetFullPath(args[0]).Equals(Path.Combine(folder,"plan.xml"),StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Invalid updater invocation");
            plan=UpdatePlan.Load(args[0]);
            if(plan.ParentId<=0||plan.ParentStarted<=0)throw new InvalidDataException("Invalid parent process");
            bool exited=false;
            try{using(var parent=Process.GetProcessById(plan.ParentId)){
                if(parent.StartTime.ToUniversalTime().Ticks!=plan.ParentStarted)exited=true;
                else exited=parent.WaitForExit(60000);
            }}catch(ArgumentException){exited=true;}
            if(!exited)throw new IOException("ScreenMover did not exit. No files were replaced.");
            UpdateEngine.Apply(plan,Path.Combine(folder,"backup"),null);
            File.WriteAllText(log,"OK|"+plan.Version+"|"+DateTime.UtcNow.ToString("O"));
            try{Process.Start(new ProcessStartInfo(Path.Combine(plan.Target,"ScreenMover.exe")){UseShellExecute=true,WorkingDirectory=plan.Target});}
            catch(Exception restartError){File.AppendAllText(log,Environment.NewLine+"Restart failed: "+restartError);MessageBox.Show("更新完成，但未能重新启动。请手动打开 ScreenMover。\n"+restartError.Message,"ScreenMover 更新");}
            return 0;
        }catch(Exception ex){
            try{File.WriteAllText(log,"ERROR|"+ex);}catch{}
            MessageBox.Show("更新未完成。请查看更新结果与备份：\n"+folder+"\n\n"+ex.Message,"ScreenMover 更新失败",MessageBoxButtons.OK,MessageBoxIcon.Error);
            return 1;
        }
    }
}
