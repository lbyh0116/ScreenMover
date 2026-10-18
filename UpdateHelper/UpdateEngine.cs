using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Xml;
using System.Xml.Linq;

namespace ScreenMover.UpdateCore {
    public sealed class UpdateFile {
        public string Name,Hash;
        public UpdateFile(string name,string hash){Name=name;Hash=hash;}
    }
    public sealed class UpdatePlan {
        public string Target,Stage,Version;
        public int ParentId;
        public long ParentStarted;
        public List<UpdateFile> Files=new List<UpdateFile>();
        public UpdatePlan(string target,string stage,string version){Target=target;Stage=stage;Version=version;}
        public void Save(string path){
            new XDocument(new XElement("ScreenMoverUpdate",new XAttribute("format",1),
                new XElement("target",Target),new XElement("stage",Stage),new XElement("version",Version),
                new XElement("parent",new XAttribute("pid",ParentId),new XAttribute("started",ParentStarted)),
                new XElement("files",Files.Select(f=>new XElement("file",new XAttribute("name",f.Name),new XAttribute("sha256",f.Hash)))))).Save(path);
        }
        public static UpdatePlan Load(string path){
            var settings=new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=2*1024*1024};
            using(var reader=XmlReader.Create(path,settings)){
                var root=XDocument.Load(reader).Root;
                if(root==null||root.Name!="ScreenMoverUpdate"||(int?)root.Attribute("format")!=1)throw new InvalidDataException("Invalid update plan");
                var plan=new UpdatePlan(Element(root,"target").Value,Element(root,"stage").Value,Element(root,"version").Value);
                var parent=Element(root,"parent");plan.ParentId=int.Parse(Attribute(parent,"pid"));plan.ParentStarted=long.Parse(Attribute(parent,"started"));
                foreach(var file in Element(root,"files").Elements("file"))plan.Files.Add(new UpdateFile(Attribute(file,"name"),Attribute(file,"sha256")));
                return plan;
            }
        }
        static XElement Element(XElement root,string name){var value=root.Element(name);if(value==null)throw new InvalidDataException("Missing update plan element");return value;}
        static string Attribute(XElement root,string name){var value=root.Attribute(name);if(value==null)throw new InvalidDataException("Missing update plan attribute");return value.Value;}
    }
    public static class UpdateEngine {
        public static string Root(string path){
            if(string.IsNullOrWhiteSpace(path)||!Path.IsPathRooted(path)||path.StartsWith(@"\\",StringComparison.Ordinal))throw new InvalidDataException("Only local absolute paths are allowed");
            string full=Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
            if(full.Length<=(Path.GetPathRoot(full)??"").Length)throw new InvalidDataException("Cannot update a drive root");
            NoLinks(full);return full;
        }
        public static void NoLinks(string path){
            string current=Path.GetFullPath(path);
            while(!string.IsNullOrEmpty(current)){
                if((Directory.Exists(current)||File.Exists(current))&&(File.GetAttributes(current)&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("Reparse points are not allowed in update paths");
                var parent=Path.GetDirectoryName(current);if(parent==current)break;current=parent??"";
            }
        }
        public static string Relative(string name){
            if(string.IsNullOrWhiteSpace(name)||name.Length>230||Path.IsPathRooted(name))throw new InvalidDataException("Invalid package path");
            string clean=name.Replace('/',Path.DirectorySeparatorChar);
            foreach(string part in clean.Split(Path.DirectorySeparatorChar)){
                if(part==""||part=="."||part==".."||part.EndsWith(".",StringComparison.Ordinal)||part.EndsWith(" ",StringComparison.Ordinal)||part.IndexOfAny(Path.GetInvalidFileNameChars())>=0)throw new InvalidDataException("Unsafe package path");
                string stem=part.Split('.')[0].ToUpperInvariant();
                if(stem=="CON"||stem=="PRN"||stem=="AUX"||stem=="NUL"||(stem.Length==4&&(stem.StartsWith("COM",StringComparison.Ordinal)||stem.StartsWith("LPT",StringComparison.Ordinal))&&stem[3]>='0'&&stem[3]<='9'))throw new InvalidDataException("Reserved package path");
            }
            return clean;
        }
        public static string Inside(string root,string relative){
            string path=Path.GetFullPath(Path.Combine(root,Relative(relative)));
            if(!path.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Path escaped update directory");
            NoLinks(path);return path;
        }
        public static string Hash(string path){using(var sha=SHA256.Create())using(var stream=File.OpenRead(path))return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();}
        public static void Validate(UpdatePlan plan){
            plan.Target=Root(plan.Target);plan.Stage=Root(plan.Stage);
            if(plan.Target.Equals(plan.Stage,StringComparison.OrdinalIgnoreCase)||plan.Stage.StartsWith(plan.Target+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||plan.Target.StartsWith(plan.Stage+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Update staging must be outside installation directory");
            if(!File.Exists(Path.Combine(plan.Target,"ScreenMover.exe"))||!File.Exists(Path.Combine(plan.Stage,"ScreenMover.exe")))throw new InvalidDataException("ScreenMover executable is missing");
            if(plan.Files.Count==0||plan.Files.Count>4000)throw new InvalidDataException("Invalid update file count");
            var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach(var file in plan.Files){
                string name=Relative(file.Name);if(!seen.Add(name))throw new InvalidDataException("Duplicate update path");
                if(file.Hash==null||file.Hash.Length!=64||file.Hash.Any(c=>!Uri.IsHexDigit(c)))throw new InvalidDataException("Invalid file checksum");
                if(!Hash(Inside(plan.Stage,name)).Equals(file.Hash,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Staged file checksum mismatch");
                Inside(plan.Target,name);
            }
            if(!seen.Contains("ScreenMover.exe"))throw new InvalidDataException("Executable not included in update manifest");
        }
        static void Retry(Action action){
            for(int i=0;;i++)try{action();return;}catch(IOException){if(i>=19)throw;Thread.Sleep(250);}
        }
        static void Replace(string source,string destination){
            Directory.CreateDirectory(Path.GetDirectoryName(destination)??throwPath());
            string temp=destination+"."+Guid.NewGuid().ToString("N")+".update-tmp";
            try{File.Copy(source,temp,false);Retry(()=>{if(File.Exists(destination))File.Replace(temp,destination,null);else File.Move(temp,destination);});}
            finally{if(File.Exists(temp))File.Delete(temp);}
        }
        public static void Apply(UpdatePlan plan,string backup,Action<int> afterFile){
            Validate(plan);backup=Root(backup);
            if(Directory.Exists(backup))throw new IOException("Backup directory already exists");
            if(backup.StartsWith(plan.Target+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||backup.Equals(plan.Target,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Backup must be outside installation directory");
            Directory.CreateDirectory(backup);
            var originals=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach(var file in plan.Files){
                string target=Inside(plan.Target,file.Name);
                if(File.Exists(target)){string saved=Inside(backup,file.Name);Directory.CreateDirectory(Path.GetDirectoryName(saved)??throwPath());File.Copy(target,saved,false);originals.Add(file.Name);}
            }
            // Durable backup inventory remains available if the updater is interrupted.
            File.WriteAllLines(backup+".inventory.txt",originals);
            var touched=new List<UpdateFile>();
            try{
                foreach(var file in plan.Files){
                    touched.Add(file);Replace(Inside(plan.Stage,file.Name),Inside(plan.Target,file.Name));
                    if(afterFile!=null)afterFile(touched.Count);
                }
                foreach(var file in plan.Files)if(!Hash(Inside(plan.Target,file.Name)).Equals(file.Hash,StringComparison.OrdinalIgnoreCase))throw new IOException("Installed checksum mismatch");
            }catch(Exception installError){
                var errors=new List<Exception>{installError};
                foreach(var file in touched.AsEnumerable().Reverse())try{
                    string target=Inside(plan.Target,file.Name);
                    if(originals.Contains(file.Name))Replace(Inside(backup,file.Name),target);else if(File.Exists(target))Retry(()=>File.Delete(target));
                }catch(Exception rollbackError){errors.Add(rollbackError);}
                if(errors.Count>1)throw new AggregateException("Update failed and rollback was incomplete. Backups were retained.",errors);
                throw new IOException("Update failed; original files were restored.",installError);
            }
        }
        static string throwPath(){throw new InvalidDataException("Missing parent directory");}
    }
}
