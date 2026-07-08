using System.Collections.ObjectModel;

namespace PEdit.Models
{
    public class TreeItem
    {
        public string Name { get; set; }
        public string FullPath { get; set; }
        public bool IsDirectory { get; set; }
        public ObservableCollection<TreeItem> Children { get; set; }

        public TreeItem()
        {
            Children = new ObservableCollection<TreeItem>();
        }
    }
}