﻿using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using Task = System.Threading.Tasks.Task;
using System.Collections.Generic;
using System.IO;
using VSIXProject1.Models;
using VSIXProject1.Helpers;
using VSIXProject1.DTEHandler;

namespace VSIXProject1
{
	/// <summary>
	/// This is the class that implements the package exposed by this assembly.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The minimum requirement for a class to be considered a valid package for Visual Studio
	/// is to implement the IVsPackage interface and register itself with the shell.
	/// This package uses the helper classes defined inside the Managed Package Framework (MPF)
	/// to do it: it derives from the Package class that provides the implementation of the
	/// IVsPackage interface and uses the registration attributes defined in the framework to
	/// register itself and its components with the shell. These attributes tell the pkgdef creation
	/// utility what data to put into .pkgdef file.
	/// </para>
	/// <para>
	/// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
	/// </para>
	/// </remarks>
	[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
	[InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
	[ProvideMenuResource("Menus.ctmenu", 1)]
	[Guid(Command1Package.PackageGuidString)]
	[ProvideAutoLoad(UIContextGuids80.SolutionExists)]
	[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
	public sealed class Command1Package : AsyncPackage
	{
		/// <summary>
		/// Command1Package GUID string.
		/// </summary>
		public const string PackageGuidString = "e9bfbcd9-41bb-4bac-a27f-c8902ab63ced";

		/// <summary>
		/// Initializes a new instance of the <see cref="Command1"/> class.
		/// </summary>
		public Command1Package()
		{
			// Inside this method you can place any initialization code that does not require
			// any Visual Studio service because at this point the package object is created but
			// not sited yet inside Visual Studio environment. The place to do all the other
			// initialization is the Initialize method.
		}

		#region Package Members

		/// <summary>
		/// Initialization of the package; this method is called right after the package is sited, so this is the place
		/// where you can put all the initialization code that rely on services provided by VisualStudio.
		/// </summary>
		/// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
		/// <param name="progress">A provider for progress updates.</param>
		/// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
		protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
		{
			Handler.DTE = GetService(typeof(SDTE)) as DTE;
			Handler.DTE.Events.SolutionEvents.Opened += SolutionEvents_Opened;
			// When initialized asynchronously, the current thread may be a background thread at this point.
			// Do any initialization that requires the UI thread after switching to the UI thread.
			await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
			await Command1.InitializeAsync(this);
		}


		private void SolutionEvents_Opened()
		{
			Array ar = Handler.DTE.ActiveSolutionProjects as Array;
			Solution sol = Handler.DTE.Solution;
			if (File.Exists(Path.Combine(Path.GetDirectoryName(sol.FullName), "watcher.config")))
			{
				string[] lines = File.ReadAllLines(Path.Combine(Path.GetDirectoryName(sol.FullName), "watcher.config"));
				foreach (string s in lines)
				{
					string[] split = s.Split(';');
					FileInfo input = new FileInfo(Path.Combine(Path.GetDirectoryName(sol.FullName), split[0].TrimStart('\\')));
					FileInfo target = new FileInfo(Path.Combine(Path.GetDirectoryName(sol.FullName), split[1].TrimStart('\\')));
					if (File.Exists(input.FullName) && File.Exists(target.FullName))
					{
						FileWatcherModel m = new FileWatcherModel();
						FileSystemWatcher fileWatcher = new FileSystemWatcher(input.DirectoryName);
						fileWatcher.Filter = input.Name;
						fileWatcher.Renamed += Helper.Fw_Changed;
						fileWatcher.EnableRaisingEvents = true;
						m.watcher = fileWatcher;
						m.InputFileName = input.Name;
						ProjectItem projItem = null;
						foreach (Project item in ar)
						{
							projItem = Helper.LocateProjectItem(item.ProjectItems, target.Name);
							if (projItem != null) break;
						}
						m.TargetItem = projItem;
						Helper.FileWatchers.Add(m);
					}
				}
			}
		}



		#endregion
	}
}
