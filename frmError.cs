/*
Copyright (C) 2016 Nicholas Blake.
This work is licensed under the Apache License, Version 2.0; you may not use this work except in compliance with the License. 
A copy of the License is included in LICENSE file and can also be obtained at http://www.apache.org/licenses/LICENSE-2.0.
See the NOTICE file distributed with this work for additional information regarding copyright ownership.
Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied, including, without limitation, any warranties or conditions of TITLE,
NON-INFRINGEMENT, MERCHANTABILITY, or FITNESS FOR A PARTICULAR PURPOSE.
See the License for the specific language governing permissions and limitations under the License.
*/

using System;
using System.Windows.Forms;

namespace CommonSenseSoft.Log{
	/// <summary>
	/// This form can be reused by Windows applications when showing internal App's exceptions is not a concern.
	/// It is also useful for test harnesses, so that you immediately see the errors.
	/// This form will show if Logging.Interactive is set to true and the messaqge level is Warnings, Errors or Fatal
	/// </summary>
	internal class frmError : System.Windows.Forms.Form	{
		private System.Windows.Forms.Label lblAnnouncement;
		public System.Windows.Forms.TextBox txtError;
		private System.Windows.Forms.Button butContinue;
		private System.Windows.Forms.Button butIgnore;
		private System.Windows.Forms.Button butExit;
		public System.Windows.Forms.Button butViewLog;
		private System.Windows.Forms.Label lblIcon;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public frmError(){
			InitializeComponent();
			this.Text = "CommonSenseSoft Logging    Ver." + this.ProductVersion;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(frmError));
			this.lblAnnouncement = new System.Windows.Forms.Label();
			this.txtError = new System.Windows.Forms.TextBox();
			this.butContinue = new System.Windows.Forms.Button();
			this.butIgnore = new System.Windows.Forms.Button();
			this.butExit = new System.Windows.Forms.Button();
			this.butViewLog = new System.Windows.Forms.Button();
			this.lblIcon = new System.Windows.Forms.Label();
			this.SuspendLayout();

			#region lblAnnouncement
			this.lblAnnouncement.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.lblAnnouncement.ForeColor = System.Drawing.Color.Red;
			this.lblAnnouncement.Location = new System.Drawing.Point(72, 0);
			this.lblAnnouncement.Name = "lblAnnouncement";
			this.lblAnnouncement.Size = new System.Drawing.Size(440, 48);
			this.lblAnnouncement.TabIndex = 0;
			this.lblAnnouncement.Text = "An error occured withing your application.\r\nPlease contact your system administrator.";
			this.lblAnnouncement.TextAlign = System.Drawing.ContentAlignment.TopCenter;
			#endregion

			#region txtError
			this.txtError.Location = new System.Drawing.Point(8, 56);
			this.txtError.Multiline = true;
			this.txtError.Name = "txtError";
			this.txtError.ReadOnly = true;
			this.txtError.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.txtError.Size = new System.Drawing.Size(560, 216);
			this.txtError.TabIndex = 1;
			this.txtError.Text = "";
			#endregion

			#region butContinue
			this.butContinue.DialogResult = System.Windows.Forms.DialogResult.Ignore;
			this.butContinue.Font = new System.Drawing.Font("Georgia", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.butContinue.Location = new System.Drawing.Point(8, 280);
			this.butContinue.Name = "butContinue";
			this.butContinue.Size = new System.Drawing.Size(112, 32);
			this.butContinue.TabIndex = 2;
			this.butContinue.Text = "Continue";
			this.butContinue.Click += new System.EventHandler(this.butContinue_Click);
			#endregion

			#region butIgnore
			this.butIgnore.Font = new System.Drawing.Font("Georgia", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.butIgnore.Location = new System.Drawing.Point(157, 280);
			this.butIgnore.Name = "butIgnore";
			this.butIgnore.Size = new System.Drawing.Size(112, 32);
			this.butIgnore.TabIndex = 2;
			this.butIgnore.Text = "Ignore";
			this.butIgnore.Click += new System.EventHandler(this.butIgnore_Click);
			#endregion

			#region butExit
			this.butExit.Font = new System.Drawing.Font("Georgia", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.butExit.Location = new System.Drawing.Point(306, 280);
			this.butExit.Name = "butExit";
			this.butExit.Size = new System.Drawing.Size(112, 32);
			this.butExit.TabIndex = 0;
			this.butExit.Text = "Exit App";
			this.butExit.Click += new System.EventHandler(this.butExit_Click);
			#endregion

			#region butViewLog
			this.butViewLog.Font = new System.Drawing.Font("Georgia", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.butViewLog.Location = new System.Drawing.Point(455, 280);
			this.butViewLog.Name = "butViewLog";
			this.butViewLog.Size = new System.Drawing.Size(112, 32);
			this.butViewLog.TabIndex = 2;
			this.butViewLog.Text = "View Log";
			this.butViewLog.Click += new System.EventHandler(this.butViewLog_Click);
			#endregion

			#region lblIcon 
			this.lblIcon.Image = ((System.Drawing.Image)(resources.GetObject("lblIcon.Image")));
			this.lblIcon.Location = new System.Drawing.Point(8, 0);
			this.lblIcon.Name = "lblIcon";
			this.lblIcon.Size = new System.Drawing.Size(56, 48);
			this.lblIcon.TabIndex = 3;
			#endregion

			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(576, 317);
			this.Controls.Add(this.lblIcon);
			this.Controls.Add(this.butContinue);
			this.Controls.Add(this.txtError);
			this.Controls.Add(this.lblAnnouncement);
			this.Controls.Add(this.butIgnore);
			this.Controls.Add(this.butExit);
			this.Controls.Add(this.butViewLog);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "frmError";
			this.Text = "Logging";
			this.ResumeLayout(false);

		}
		#endregion

		private void butExit_Click(object sender, System.EventArgs e) {
			if(MessageBox.Show(this,"Are you sure you want to terminate the Application?","Prepared to terminate the Application."
			,MessageBoxButtons.YesNo,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2)==DialogResult.Yes){
				this.Close();
				this.Dispose();
				Environment.Exit(-1);
			}
		}

		private void butViewLog_Click(object sender, System.EventArgs e) {
			System.Diagnostics.ProcessStartInfo info = new System.Diagnostics.ProcessStartInfo();
			info.UseShellExecute=true;
			info.FileName=butViewLog.Tag.ToString();
			try{
				System.Diagnostics.Process.Start(info);
			}
			catch(Exception ex){
				MessageBox.Show(ex.Message,"Failed to open "+info.FileName,MessageBoxButtons.OK);
			}
			info=null;
		}

		private void butIgnore_Click(object sender, System.EventArgs e) {
			MessageBox.Show("Not implemented yet.");
		}

		private void butContinue_Click(object sender, System.EventArgs e) {
			//Continue = true;
			this.Close();
			this.Dispose();
		}

	}
}