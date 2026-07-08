using System.IO;
using PEdit.Models;

namespace PEdit.Services
{
    public class FileSystemService
    {
        public TreeItem GetDirectoryTree(string path)
        {
            var rootInfo = new DirectoryInfo(path);
            var rootItem = new TreeItem
            {
                Name = rootInfo.Name,
                FullPath = rootInfo.FullName,
                IsDirectory = true
            };

            PopulateChildren(rootInfo, rootItem);
            return rootItem;
        }

        private void PopulateChildren(DirectoryInfo directory, TreeItem parentNode)
        {
            try
            {
                foreach (var dir in directory.GetDirectories())
                {
                    var node = new TreeItem { Name = dir.Name, FullPath = dir.FullName, IsDirectory = true };
                    parentNode.Children.Add(node);
                    PopulateChildren(dir, node); // Ricorsione per le sottocartelle
                }

                foreach (var file in directory.GetFiles())
                {
                    parentNode.Children.Add(new TreeItem { Name = file.Name, FullPath = file.FullName, IsDirectory = false });
                }
            }
            catch (System.UnauthorizedAccessException)
            {
                // Ignora le cartelle a cui non si ha accesso
            }
        }
    }
}