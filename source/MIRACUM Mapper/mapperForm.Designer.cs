namespace UKER_Mapper
{
    partial class mapperForm
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(mapperForm));
            this.sourceTermsList = new System.Windows.Forms.ListBox();
            this.mappingTermsList = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.showAbove = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.showBelow = new System.Windows.Forms.ComboBox();
            this.addNewMappingBtn = new System.Windows.Forms.Button();
            this.removeMappingBtn = new System.Windows.Forms.Button();
            this.targetCode = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.secondarySourceCode = new System.Windows.Forms.TextBox();
            this.secondarySourceCodeCondition = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.documentationText = new System.Windows.Forms.RichTextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.saveMappingBtn = new System.Windows.Forms.Button();
            this.WorkingStatus = new System.Windows.Forms.Label();
            this.addCommentBtn = new System.Windows.Forms.Button();
            this.SourceDescr = new System.Windows.Forms.RichTextBox();
            this.TermInfo = new System.Windows.Forms.RichTextBox();
            this.logInButton = new System.Windows.Forms.Button();
            this.sourceFilter = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.mappingVersion = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.sendToAccept = new System.Windows.Forms.ComboBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.nextVersionBtn = new System.Windows.Forms.Button();
            this.previousVersionBtn = new System.Windows.Forms.Button();
            this.visualizeBtn = new System.Windows.Forms.Button();
            this.currentLevelIndicator = new System.Windows.Forms.Label();
            this.loincSearchBtn = new System.Windows.Forms.Button();
            this.aktZustLabel = new System.Windows.Forms.Label();
            this.nextZustLabel = new System.Windows.Forms.Label();
            this.showDeleted = new System.Windows.Forms.CheckBox();
            this.showAllMappings = new System.Windows.Forms.CheckBox();
            this.blockPanel = new UKER_Mapper.TransparentPanel();
            this.SuspendLayout();
            // 
            // sourceTermsList
            // 
            resources.ApplyResources(this.sourceTermsList, "sourceTermsList");
            this.sourceTermsList.Name = "sourceTermsList";
            this.sourceTermsList.SelectedIndexChanged += new System.EventHandler(this.sourceTerms_SelectedIndexChanged);
            // 
            // mappingTermsList
            // 
            resources.ApplyResources(this.mappingTermsList, "mappingTermsList");
            this.mappingTermsList.Name = "mappingTermsList";
            this.mappingTermsList.Sorted = true;
            this.mappingTermsList.SelectedIndexChanged += new System.EventHandler(this.mappingTermsList_SelectedIndexChanged);
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // showAbove
            // 
            resources.ApplyResources(this.showAbove, "showAbove");
            this.showAbove.FormattingEnabled = true;
            this.showAbove.Name = "showAbove";
            this.showAbove.SelectedValueChanged += new System.EventHandler(this.showAbove_SelectedValueChanged);
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // showBelow
            // 
            resources.ApplyResources(this.showBelow, "showBelow");
            this.showBelow.FormattingEnabled = true;
            this.showBelow.Name = "showBelow";
            this.showBelow.SelectedValueChanged += new System.EventHandler(this.showBelow_SelectedValueChanged);
            // 
            // addNewMappingBtn
            // 
            resources.ApplyResources(this.addNewMappingBtn, "addNewMappingBtn");
            this.addNewMappingBtn.Name = "addNewMappingBtn";
            this.addNewMappingBtn.UseVisualStyleBackColor = true;
            this.addNewMappingBtn.Click += new System.EventHandler(this.addNewMapping_Click);
            // 
            // removeMappingBtn
            // 
            this.removeMappingBtn.BackColor = System.Drawing.Color.Pink;
            resources.ApplyResources(this.removeMappingBtn, "removeMappingBtn");
            this.removeMappingBtn.ForeColor = System.Drawing.Color.Black;
            this.removeMappingBtn.Name = "removeMappingBtn";
            this.removeMappingBtn.UseVisualStyleBackColor = false;
            this.removeMappingBtn.Click += new System.EventHandler(this.removeMappingBtn_Click);
            // 
            // targetCode
            // 
            resources.ApplyResources(this.targetCode, "targetCode");
            this.targetCode.Name = "targetCode";
            this.targetCode.TextChanged += new System.EventHandler(this.targetCode_TextChanged);
            this.targetCode.KeyDown += new System.Windows.Forms.KeyEventHandler(this.targetCode_KeyDown_1);
            this.targetCode.KeyUp += new System.Windows.Forms.KeyEventHandler(this.targetCode_KeyUp_1);
            this.targetCode.Leave += new System.EventHandler(this.targetCode_Leave);
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.Name = "label6";
            // 
            // label7
            // 
            resources.ApplyResources(this.label7, "label7");
            this.label7.Name = "label7";
            // 
            // secondarySourceCode
            // 
            resources.ApplyResources(this.secondarySourceCode, "secondarySourceCode");
            this.secondarySourceCode.Name = "secondarySourceCode";
            this.secondarySourceCode.TextChanged += new System.EventHandler(this.secondarySourceCode_TextChanged);
            // 
            // secondarySourceCodeCondition
            // 
            resources.ApplyResources(this.secondarySourceCodeCondition, "secondarySourceCodeCondition");
            this.secondarySourceCodeCondition.Name = "secondarySourceCodeCondition";
            this.secondarySourceCodeCondition.TextChanged += new System.EventHandler(this.secondarySourceCodeCondition_TextChanged);
            // 
            // label8
            // 
            resources.ApplyResources(this.label8, "label8");
            this.label8.Name = "label8";
            // 
            // documentationText
            // 
            this.documentationText.BackColor = System.Drawing.SystemColors.Window;
            resources.ApplyResources(this.documentationText, "documentationText");
            this.documentationText.Name = "documentationText";
            this.documentationText.TextChanged += new System.EventHandler(this.documentationText_TextChanged);
            this.documentationText.Leave += new System.EventHandler(this.documentation_Leave);
            // 
            // label9
            // 
            resources.ApplyResources(this.label9, "label9");
            this.label9.Name = "label9";
            // 
            // saveMappingBtn
            // 
            this.saveMappingBtn.BackColor = System.Drawing.Color.LightGreen;
            resources.ApplyResources(this.saveMappingBtn, "saveMappingBtn");
            this.saveMappingBtn.ForeColor = System.Drawing.Color.Black;
            this.saveMappingBtn.Name = "saveMappingBtn";
            this.saveMappingBtn.UseVisualStyleBackColor = false;
            this.saveMappingBtn.Click += new System.EventHandler(this.saveMappingBtn_Click);
            // 
            // WorkingStatus
            // 
            this.WorkingStatus.BackColor = System.Drawing.Color.Salmon;
            this.WorkingStatus.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            resources.ApplyResources(this.WorkingStatus, "WorkingStatus");
            this.WorkingStatus.Name = "WorkingStatus";
            // 
            // addCommentBtn
            // 
            resources.ApplyResources(this.addCommentBtn, "addCommentBtn");
            this.addCommentBtn.Name = "addCommentBtn";
            this.addCommentBtn.UseVisualStyleBackColor = true;
            this.addCommentBtn.Click += new System.EventHandler(this.addCommentButton_Click);
            // 
            // SourceDescr
            // 
            this.SourceDescr.BackColor = System.Drawing.SystemColors.Control;
            this.SourceDescr.BorderStyle = System.Windows.Forms.BorderStyle.None;
            resources.ApplyResources(this.SourceDescr, "SourceDescr");
            this.SourceDescr.Name = "SourceDescr";
            this.SourceDescr.ReadOnly = true;
            // 
            // TermInfo
            // 
            this.TermInfo.BackColor = System.Drawing.SystemColors.Control;
            this.TermInfo.BorderStyle = System.Windows.Forms.BorderStyle.None;
            resources.ApplyResources(this.TermInfo, "TermInfo");
            this.TermInfo.Name = "TermInfo";
            this.TermInfo.ReadOnly = true;
            // 
            // logInButton
            // 
            resources.ApplyResources(this.logInButton, "logInButton");
            this.logInButton.Name = "logInButton";
            this.logInButton.UseVisualStyleBackColor = true;
            this.logInButton.Click += new System.EventHandler(this.logInButton_Click);
            // 
            // sourceFilter
            // 
            resources.ApplyResources(this.sourceFilter, "sourceFilter");
            this.sourceFilter.Name = "sourceFilter";
            this.sourceFilter.TextChanged += new System.EventHandler(this.sourceFilter_TextChanged);
            this.sourceFilter.KeyDown += new System.Windows.Forms.KeyEventHandler(this.sourceFilter_KeyDown);
            // 
            // label10
            // 
            resources.ApplyResources(this.label10, "label10");
            this.label10.Name = "label10";
            // 
            // mappingVersion
            // 
            resources.ApplyResources(this.mappingVersion, "mappingVersion");
            this.mappingVersion.BackColor = System.Drawing.Color.LightGray;
            this.mappingVersion.Name = "mappingVersion";
            // 
            // label11
            // 
            resources.ApplyResources(this.label11, "label11");
            this.label11.BackColor = System.Drawing.Color.LightGray;
            this.label11.Name = "label11";
            // 
            // sendToAccept
            // 
            resources.ApplyResources(this.sendToAccept, "sendToAccept");
            this.sendToAccept.FormattingEnabled = true;
            this.sendToAccept.Name = "sendToAccept";
            this.sendToAccept.SelectedValueChanged += new System.EventHandler(this.sendToAccept_SelectedValueChanged);
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.LightGray;
            resources.ApplyResources(this.panel1, "panel1");
            this.panel1.Name = "panel1";
            // 
            // nextVersionBtn
            // 
            this.nextVersionBtn.BackColor = System.Drawing.Color.Silver;
            resources.ApplyResources(this.nextVersionBtn, "nextVersionBtn");
            this.nextVersionBtn.ForeColor = System.Drawing.Color.Black;
            this.nextVersionBtn.Name = "nextVersionBtn";
            this.nextVersionBtn.UseVisualStyleBackColor = false;
            this.nextVersionBtn.Click += new System.EventHandler(this.nextVersion_Click);
            // 
            // previousVersionBtn
            // 
            this.previousVersionBtn.BackColor = System.Drawing.Color.Silver;
            resources.ApplyResources(this.previousVersionBtn, "previousVersionBtn");
            this.previousVersionBtn.ForeColor = System.Drawing.Color.Black;
            this.previousVersionBtn.Name = "previousVersionBtn";
            this.previousVersionBtn.UseVisualStyleBackColor = false;
            this.previousVersionBtn.Click += new System.EventHandler(this.previousVersion_Click);
            // 
            // visualizeBtn
            // 
            resources.ApplyResources(this.visualizeBtn, "visualizeBtn");
            this.visualizeBtn.Name = "visualizeBtn";
            this.visualizeBtn.UseVisualStyleBackColor = true;
            this.visualizeBtn.Click += new System.EventHandler(this.visualizeBtn_Click);
            // 
            // currentLevelIndicator
            // 
            this.currentLevelIndicator.BackColor = System.Drawing.Color.Transparent;
            resources.ApplyResources(this.currentLevelIndicator, "currentLevelIndicator");
            this.currentLevelIndicator.Name = "currentLevelIndicator";
            // 
            // loincSearchBtn
            // 
            resources.ApplyResources(this.loincSearchBtn, "loincSearchBtn");
            this.loincSearchBtn.Name = "loincSearchBtn";
            this.loincSearchBtn.UseVisualStyleBackColor = true;
            this.loincSearchBtn.Click += new System.EventHandler(this.loincSearchBtn_Click);
            // 
            // aktZustLabel
            // 
            resources.ApplyResources(this.aktZustLabel, "aktZustLabel");
            this.aktZustLabel.Name = "aktZustLabel";
            // 
            // nextZustLabel
            // 
            resources.ApplyResources(this.nextZustLabel, "nextZustLabel");
            this.nextZustLabel.Name = "nextZustLabel";
            // 
            // showDeleted
            // 
            resources.ApplyResources(this.showDeleted, "showDeleted");
            this.showDeleted.Checked = true;
            this.showDeleted.CheckState = System.Windows.Forms.CheckState.Checked;
            this.showDeleted.Name = "showDeleted";
            this.showDeleted.UseVisualStyleBackColor = true;
            this.showDeleted.CheckedChanged += new System.EventHandler(this.showDeleted_CheckedChanged);
            // 
            // showAllMappings
            // 
            resources.ApplyResources(this.showAllMappings, "showAllMappings");
            this.showAllMappings.Checked = true;
            this.showAllMappings.CheckState = System.Windows.Forms.CheckState.Checked;
            this.showAllMappings.Name = "showAllMappings";
            this.showAllMappings.UseVisualStyleBackColor = true;
            this.showAllMappings.CheckedChanged += new System.EventHandler(this.showAllMappings_CheckedChanged);
            // 
            // blockPanel
            // 
            resources.ApplyResources(this.blockPanel, "blockPanel");
            this.blockPanel.Name = "blockPanel";
            this.blockPanel.Click += new System.EventHandler(this.blockPanel_Click_1);
            // 
            // mapperForm
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.removeMappingBtn);
            this.Controls.Add(this.blockPanel);
            this.Controls.Add(this.documentationText);
            this.Controls.Add(this.visualizeBtn);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.mappingVersion);
            this.Controls.Add(this.nextVersionBtn);
            this.Controls.Add(this.showAllMappings);
            this.Controls.Add(this.previousVersionBtn);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.showDeleted);
            this.Controls.Add(this.nextZustLabel);
            this.Controls.Add(this.aktZustLabel);
            this.Controls.Add(this.loincSearchBtn);
            this.Controls.Add(this.currentLevelIndicator);
            this.Controls.Add(this.sendToAccept);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.sourceFilter);
            this.Controls.Add(this.logInButton);
            this.Controls.Add(this.TermInfo);
            this.Controls.Add(this.SourceDescr);
            this.Controls.Add(this.addCommentBtn);
            this.Controls.Add(this.WorkingStatus);
            this.Controls.Add(this.saveMappingBtn);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.secondarySourceCodeCondition);
            this.Controls.Add(this.secondarySourceCode);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.targetCode);
            this.Controls.Add(this.addNewMappingBtn);
            this.Controls.Add(this.showBelow);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.showAbove);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.mappingTermsList);
            this.Controls.Add(this.sourceTermsList);
            this.Controls.Add(this.panel1);
            this.Name = "mapperForm";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.mapperForm_FormClosed);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox sourceTermsList;
        private System.Windows.Forms.ListBox mappingTermsList;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox showAbove;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox showBelow;
        private System.Windows.Forms.Button addNewMappingBtn;
        private System.Windows.Forms.Button removeMappingBtn;
        private System.Windows.Forms.TextBox targetCode;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox secondarySourceCode;
        private System.Windows.Forms.TextBox secondarySourceCodeCondition;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.RichTextBox documentationText;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Button saveMappingBtn;
        private System.Windows.Forms.Label WorkingStatus;
        private System.Windows.Forms.Button addCommentBtn;
        private System.Windows.Forms.RichTextBox SourceDescr;
        private System.Windows.Forms.RichTextBox TermInfo;
        private System.Windows.Forms.Button logInButton;
        private System.Windows.Forms.TextBox sourceFilter;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label mappingVersion;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.ComboBox sendToAccept;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button previousVersionBtn;
        private System.Windows.Forms.Button nextVersionBtn;
        private System.Windows.Forms.Button visualizeBtn;
        private System.Windows.Forms.Label currentLevelIndicator;
        private System.Windows.Forms.Button loincSearchBtn;
        private System.Windows.Forms.Label aktZustLabel;
        private System.Windows.Forms.Label nextZustLabel;
        private System.Windows.Forms.CheckBox showDeleted;
        private System.Windows.Forms.CheckBox showAllMappings;
        private TransparentPanel blockPanel;
    }
}

