using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using VSIXProject1.DTEHandler;
using VSIXProject1.Forms;
using VSIXProject1.Helpers;
using VSIXProject1.Models;
using Task = System.Threading.Tasks.Task;

namespace VSIXProject1
{
	/// <summary>
	/// Command handler
	/// </summary>
	internal sealed class Command1
	{
		/// <summary>
		/// Command ID.
		/// </summary>
		public const int CommandId = 0x0100;

		/// <summary>
		/// Command menu group (command set GUID).
		/// </summary>
		public static readonly Guid CommandSet = new Guid("a5fbfdbb-13e5-4c1f-ad45-11d7d7402fb6");

		/// <summary>
		/// VS Package that provides this command, not null.
		/// </summary>
		private readonly AsyncPackage package;

		/// <summary>
		/// Initializes a new instance of the <see cref="Command1"/> class.
		/// Adds our command handlers for menu (commands must exist in the command table file)
		/// </summary>
		/// <param name="package">Owner package, not null.</param>
		/// <param name="commandService">Command service to add command to, not null.</param>
		private Command1(AsyncPackage package, OleMenuCommandService commandService)
		{

			this.package = package ?? throw new ArgumentNullException(nameof(package));
			commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

			var menuCommandID = new CommandID(CommandSet, CommandId);
			var menuItem = new OleMenuCommand(this.Execute, menuCommandID);
			menuItem.BeforeQueryStatus += menuCommand_BeforeQueryStatus;
			commandService.AddCommand(menuItem);
		}

		private void menuCommand_BeforeQueryStatus(object sender, EventArgs e)
		{
			var menuCommand = sender as OleMenuCommand;
			if (menuCommand != null)
			{
				// start by assuming that the menu will not be shown
				menuCommand.Visible = false;
				menuCommand.Enabled = false;

				IVsHierarchy hierarchy = null;
				uint itemid = VSConstants.VSITEMID_NIL;

				if (!IsSingleProjectItemSelection(out hierarchy, out itemid)) return;
				// Get the file path
				string itemFullPath = null;
				((IVsProject)hierarchy).GetMkDocument(itemid, out itemFullPath);
				var transformFileInfo = new FileInfo(itemFullPath);

				if (transformFileInfo.Name.EndsWith(".json") || transformFileInfo.Name.EndsWith(".config"))
				{
					menuCommand.Visible = true;
					menuCommand.Enabled = true;
				}
			}
		}

		public static bool IsSingleProjectItemSelection(out IVsHierarchy hierarchy, out uint itemid)
		{
			hierarchy = null;
			itemid = VSConstants.VSITEMID_NIL;
			int hr = VSConstants.S_OK;

			var monitorSelection = Package.GetGlobalService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
			var solution = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution;
			if (monitorSelection == null || solution == null)
			{
				return false;
			}

			IVsMultiItemSelect multiItemSelect = null;
			IntPtr hierarchyPtr = IntPtr.Zero;
			IntPtr selectionContainerPtr = IntPtr.Zero;

			try
			{
				hr = monitorSelection.GetCurrentSelection(out hierarchyPtr, out itemid, out multiItemSelect, out selectionContainerPtr);

				if (ErrorHandler.Failed(hr) || hierarchyPtr == IntPtr.Zero || itemid == VSConstants.VSITEMID_NIL)
				{
					// there is no selection
					return false;
				}

				// multiple items are selected
				if (multiItemSelect != null) return false;

				// there is a hierarchy root node selected, thus it is not a single item inside a project

				if (itemid == VSConstants.VSITEMID_ROOT) return false;

				hierarchy = Marshal.GetObjectForIUnknown(hierarchyPtr) as IVsHierarchy;
				if (hierarchy == null) return false;

				Guid guidProjectID = Guid.Empty;

				if (ErrorHandler.Failed(solution.GetGuidOfProject(hierarchy, out guidProjectID)))
				{
					return false; // hierarchy is not a project inside the Solution if it does not have a ProjectID Guid
				}

				// if we got this far then there is a single project item selected
				return true;
			}
			finally
			{
				if (selectionContainerPtr != IntPtr.Zero)
				{
					Marshal.Release(selectionContainerPtr);
				}

				if (hierarchyPtr != IntPtr.Zero)
				{
					Marshal.Release(hierarchyPtr);
				}
			}
		}

		/// <summary>
		/// Gets the instance of the command.
		/// </summary>
		public static Command1 Instance
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the service provider from the owner package.
		/// </summary>
		private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
		{
			get
			{
				return this.package;
			}
		}

		/// <summary>
		/// Initializes the singleton instance of the command.
		/// </summary>
		/// <param name="package">Owner package, not null.</param>
		public static async Task InitializeAsync(AsyncPackage package)
		{
			// Verify the current thread is the UI thread - the call to AddCommand in Command1's constructor requires
			// the UI thread.
			ThreadHelper.ThrowIfNotOnUIThread();

			OleMenuCommandService commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
			Instance = new Command1(package, commandService);
		}


		/// <summary>
		/// This function is the callback used to execute the command when the menu item is clicked.
		/// See the constructor to see how the menu item is associated with this function using
		/// OleMenuCommandService service and MenuCommand class.
		/// </summary>
		/// <param name="sender">Event sender.</param>
		/// <param name="e">Event args.</param>
		private void Execute(object sender, EventArgs e)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			Form f = new GetTTName();
			if (f.ShowDialog() == DialogResult.OK)
			{
				IVsHierarchy hierarchy = null;
				uint itemid = VSConstants.VSITEMID_NIL;

				if (!IsSingleProjectItemSelection(out hierarchy, out itemid)) return;
				// Get the file path
				string itemFullPath = null;
				((IVsProject)hierarchy).GetMkDocument(itemid, out itemFullPath);
				var configFile = new FileInfo(itemFullPath);

				FileWatcherModel m = new FileWatcherModel();

				FileSystemWatcher fileWatcher = new FileSystemWatcher(configFile.DirectoryName);
				fileWatcher.Filter = configFile.Name;
				fileWatcher.Renamed += Helper.Fw_Changed;
				fileWatcher.EnableRaisingEvents = true;
				m.watcher = fileWatcher;
				m.InputFileName = configFile.FullName;

				var dte = Handler.DTE;
				Array ar = dte.ActiveSolutionProjects as Array;
				ProjectItem projItem = null;
				foreach (Project item in ar)
				{
					projItem = Helper.LocateProjectItem(item.ProjectItems, m.InputFileName);
					if (projItem != null) break;
				}
				m.TargetItem = projItem;
				Helper.FileWatchers.Add(m);

				Solution sol = Handler.DTE.Solution;
				DirectoryInfo solutionLoc = new DirectoryInfo(Path.GetDirectoryName(sol.FullName));

				string relativePathTarget = Helper.GetRelativePath(solutionLoc.FullName, m.TargetItem.FileNames[0]);
				string relativePathInput = Helper.GetRelativePath(solutionLoc.FullName, m.InputFileName);

				string path = Path.GetDirectoryName(sol.FullName);
				File.AppendAllText(Path.Combine(path, "watcher.config"), relativePathInput + ";" + relativePathTarget);
			}
		}		
	}
}
