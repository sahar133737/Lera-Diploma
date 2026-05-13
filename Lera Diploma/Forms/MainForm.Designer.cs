namespace Lera_Diploma.Forms
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Panel panelSidebar;
        private System.Windows.Forms.Panel panelHost;
        private System.Windows.Forms.Label lblModuleTitle;
        private System.Windows.Forms.Panel panelTop;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.panelSidebar = new System.Windows.Forms.Panel();
            this.panelTop = new System.Windows.Forms.Panel();
            this.lblModuleTitle = new System.Windows.Forms.Label();
            this.panelHost = new System.Windows.Forms.Panel();
            this.panelTop.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelSidebar
            // 
            this.panelSidebar.BackColor = Lera_Diploma.Forms.UiTheme.SidebarBg;
            this.panelSidebar.Dock = System.Windows.Forms.DockStyle.Left;
            this.panelSidebar.Location = new System.Drawing.Point(0, 0);
            this.panelSidebar.Name = "panelSidebar";
            this.panelSidebar.Padding = new System.Windows.Forms.Padding(0);
            this.panelSidebar.Size = new System.Drawing.Size(272, 600);
            this.panelSidebar.TabIndex = 0;
            // 
            // panelTop
            // 
            this.panelTop.BackColor = Lera_Diploma.Forms.UiTheme.HeaderBg;
            this.panelTop.Controls.Add(this.lblModuleTitle);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(272, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Padding = new System.Windows.Forms.Padding(16, 12, 16, 12);
            this.panelTop.Size = new System.Drawing.Size(876, 56);
            this.panelTop.TabIndex = 1;
            // 
            // lblModuleTitle
            // 
            this.lblModuleTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblModuleTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblModuleTitle.ForeColor = Lera_Diploma.Forms.UiTheme.TextPrimary;
            this.lblModuleTitle.Location = new System.Drawing.Point(16, 14);
            this.lblModuleTitle.Name = "lblModuleTitle";
            this.lblModuleTitle.Size = new System.Drawing.Size(844, 32);
            this.lblModuleTitle.TabIndex = 0;
            this.lblModuleTitle.Text = "Модуль";
            // 
            // panelHost
            // 
            this.panelHost.BackColor = Lera_Diploma.Forms.UiTheme.PageBackground;
            this.panelHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelHost.Location = new System.Drawing.Point(272, 48);
            this.panelHost.Name = "panelHost";
            this.panelHost.Padding = new System.Windows.Forms.Padding(12);
            this.panelHost.Size = new System.Drawing.Size(876, 552);
            this.panelHost.TabIndex = 2;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = Lera_Diploma.Forms.UiTheme.PageBackground;
            this.ClientSize = new System.Drawing.Size(1104, 600);
            this.Controls.Add(this.panelHost);
            this.Controls.Add(this.panelTop);
            this.Controls.Add(this.panelSidebar);
            this.MinimumSize = new System.Drawing.Size(900, 600);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "ИС финансового отдела";
            this.panelTop.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
