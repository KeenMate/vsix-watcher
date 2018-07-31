using EnvDTE;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VSIXProject1.Models;

namespace VSIXProject1.Helpers
{
	public static class Helper
	{
		public static List<FileWatcherModel> FileWatchers = new List<FileWatcherModel>();

		public static ProjectItem LocateProjectItem(ProjectItems items, string targetName)
		{
			foreach (ProjectItem item in items)
			{
				if (item.ProjectItems.Count > 0)
				{
					if (item.Name == targetName)
					{
						return item;
					}
					else
						LocateProjectItem(item.ProjectItems, targetName);
				}
				else
				{
					if (item.Name == targetName)
					{
						return item;
					}
				}
			}
			return null;
		}

		public static string GetRelativePath(string root, string path)
		{
			string relative = path;
			for (int i = 0; i < path.Length; i++)
			{
				try
				{
					if (path.ToLower()[i] == root.ToLower()[i])
					{
						relative = relative.Substring(1);
					}
					else break;
				}
				catch (Exception ex) { break; }
			}
			return relative;
		}

		public static void Fw_Changed(object sender, RenamedEventArgs e)
		{
			foreach (FileWatcherModel item in FileWatchers)
			{
				if (e.Name == item.InputFileName)
				{
					if (!item.TargetItem.IsOpen)
						item.TargetItem.Open();
					item.TargetItem.Save();
					break;
				}
			}
		}
	}
}
