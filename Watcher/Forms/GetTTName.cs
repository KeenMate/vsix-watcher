using EnvDTE;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VSIXProject1.DTEHandler;

namespace VSIXProject1.Forms
{
	public partial class GetTTName : Form
	{
		public GetTTName()
		{
			InitializeComponent();
		}

		public static string FileName = null;
		private Dictionary<string, string> files = new Dictionary<string, string>();

		private void Recurse(ProjectItems items)
		{
			foreach (ProjectItem item in items)
			{
				if (item.ProjectItems.Count > 0)
				{
					if (item.Name.EndsWith(".tt"))
					{
						files.Add(item.Name, item.FileNames[0]);
						comboBox1.Items.Add(item.Name);
					}
					else
						Recurse(item.ProjectItems);
				}
				else
				{
					if (item.Name.EndsWith(".tt"))
					{
						files.Add(item.Name, item.FileNames[0]);
						comboBox1.Items.Add(item.Name);
					}
				}
			}
		}

		private void GetTTName_Load(object sender, EventArgs e)
		{
			var dte = Handler.DTE;
			Array ar = dte.ActiveSolutionProjects as Array;
			foreach (Project item in ar)
			{
				Recurse(item.ProjectItems);
			}
		}

		private void button2_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void button1_Click(object sender, EventArgs e)
		{
			if (comboBox1.SelectedIndex == -1)
			{
				MessageBox.Show("Please select a file.", "No file selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}
			FileName = files[comboBox1.SelectedItem.ToString()];
			DialogResult = DialogResult.OK;
			Close();
		}
	}
}
