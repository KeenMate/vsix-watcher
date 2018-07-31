using EnvDTE;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSIXProject1.Models
{
	public class FileWatcherModel
	{
		public FileSystemWatcher watcher { get; set; }
		public ProjectItem TargetItem { get; set; }
		public string InputFileName { get; set; }
	}
}
