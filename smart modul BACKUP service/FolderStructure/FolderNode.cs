using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smart_modul_BACKUP_service.FolderStructure
{
    /// <summary>
    /// Informace o složce a jejím obsahu.
    /// </summary>
    public class FolderNode
    {
        /// <summary>
        /// Obsažené složky
        /// </summary>
        public FolderNode[] childFolders;

        /// <summary>
        /// Obsažené soubory
        /// </summary>
        public FileNode[] childFiles;

        /// <summary>
        /// Název této složky
        /// </summary>
        public string name;

        /// <summary>
        /// Toto pole využívá FolderObserver
        /// </summary>
        public bool fChanging;

        /// <summary>
        /// Zdali je tato složka prázdná
        /// </summary>
        public bool IsEmpty => childFiles.Length == 0 && childFolders.Length == 0;

        public static FolderNode FromPath(string path)
        {
            List<FolderNode> folders = new List<FolderNode>();
            List<FileNode> files = new List<FileNode>();

            foreach (var dir in Directory.GetDirectories(path))
                folders.Add(FolderNode.FromPath(dir));

            foreach(var file in Directory.GetFiles(path))
            {
                var info = new FileInfo(file);

                files.Add(new FileNode()
                {
                    lastEdit = info.LastWriteTime,
                    name = info.Name
                });
            }

            return new FolderNode()
            {
                childFiles = files.ToArray(),
                childFolders = folders.ToArray(),
                name = Path.GetFileName(path)
            };
        }

        /// <summary>
        /// Vrací, jestli jsou informace obsažené v této instanci stejné jako v other
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool CompareAgainst(FolderNode other)
        {
            if (other.name != this.name)
                return false;

            if (other.childFiles.Length != this.childFiles.Length)
                return false;

            if (other.childFolders.Length != this.childFolders.Length)
                return false;

            foreach(var otherFile in other.childFiles)
            {
                var myFile = this.childFiles.FirstOrDefault(f => f.name == otherFile.name);
                if (myFile == null)
                    return false;
                else
                {
                    if (myFile.lastEdit != otherFile.lastEdit)
                        return false;
                }
            }

            foreach(var otherFolder in other.childFolders)
            {
                var myFolder = this.childFolders.FirstOrDefault(f => f.name == otherFolder.name);
                if (myFolder == null)
                    return false;
                else
                {
                    if (!myFolder.CompareAgainst(otherFolder))
                        return false;
                }
            }

            return true;
        }
    }
}
