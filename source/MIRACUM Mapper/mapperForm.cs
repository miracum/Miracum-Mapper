//    MIRACUM Mapper, a program for mapping local medical codes to a standard teminology.
//    Copyright (C) 2019-2025 The MIRACUM Project, Universitätsklinikum Erlangen.
//    Implemented by: Sebastian Mate (Sebastian.Mate(at)uk-erlangen.de)
//
//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with this program.  If not, see <https://www.gnu.org/licenses/>.


//    #########################################################
//    ##                                                     ##
//    ##   If you encounter problems with the form designer  ##
//    ##   related to the class TransparentPanel, see:       ##
//    ##   https://stackoverflow.com/a/29247115              ##
//    ##                                                     ##
//    #########################################################

using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Globalization;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Data.Common;

namespace UKER_Mapper
{
    public partial class mapperForm : Form
    {
        private int thisVersion = 1;
        private int schemaVersion = 0;
        private DataSet ds = new DataSet();
        private DataTable dt = new DataTable();
        private Boolean loadingInProgress = true;
        private int filterFromLevel = 0;
        private int filterToLevel = 99;
        private int userFilterFrom = 0;
        private int userFilterTo = 99;
        private int userRejectTo = 0;
        private int userAcceptTo = 0;
        private string userTransitions = "";
        private bool userAllowBrowseHistory = false;
        private string userForcedFilter = "";
        private int currentSourceTermSelection = 0;
        private webBrowser wb = new webBrowser();
        private string lastSaved = "";
        private string jdbcConnectionString = "";
        private string activeDirectoryServer = "";
        private string visualizerUrl = "";
        private int lastSelectedMapping = 0;
        private int unsavedChanges = 0;
        private int userNotified = 0;
        private Boolean alwaysNotify = false;
        private String editedMapping = "";
        private int[] backupItemPos = new int[2] { 0, 0 };
        private string[] backupItemName = new string[2] { "", "" };
        private bool working = false;

        List<Point> orLocation = new List<Point>();
        List<int> orWidth = new List<int>();
        List<int> orHeigth = new List<int>();
        List<float> orFontSize = new List<float>();

        NpgsqlConnection dbConnection1;
        NpgsqlConnection dbConnection2;
        NpgsqlConnection dbConnection3;

        String projectIndentifier = "Default";

        public void printDebug(string msg) // Direct debugging output
        {
            Console.WriteLine(msg);
        }
        private void log(string text) // Write to log in database
        {
            startWorking();
            try
            {
                var cmd = new NpgsqlCommand();
                cmd.CommandText = "INSERT INTO log (text) VALUES (@text)";
                cmd.Parameters.AddWithValue("text", text);
                cmd.Connection = dbConnection3;
                cmd.ExecuteNonQuery();
                cmd.Dispose();

            }
            catch (Exception msg)
            {
                MessageBox.Show(msg.ToString(), "MIRACUM Mapper: ERROR");
                throw;
            }
            stopWorking();
        }

        // --- Stepless UI Scaling Methods ---

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, Int32 wMsg, bool wParam, Int32 lParam);
        private const int WM_SETREDRAW = 11;

        private Size oldSize;
        private void backupCtrlSizes()
        {
            foreach (Control cnt in this.Controls)
            {
                orLocation.Add(cnt.Location);
                orWidth.Add(cnt.Width);
                orHeigth.Add(cnt.Height);
                orFontSize.Add(cnt.Font.Size);
            }
        }

        protected override void OnResize(System.EventArgs e)
        {
            SendMessage(this.Handle, WM_SETREDRAW, false, 0); // see: https://stackoverflow.com/a/126952
            disableEventHandlers();
            base.OnResize(e);
            int i = 0;
            foreach (Control cnt in this.Controls)
            {
                RestoreSize(cnt, i);
                ResizeAll(cnt, base.Size);
                i++;
            }
            enableEventHandlers();
            SendMessage(this.Handle, WM_SETREDRAW, true, 0);
            this.Refresh();
        }

        private void RestoreSize(Control cnt, int i)
        {
            //printDebug("calling " + System.Reflection.MethodBase.GetCurrentMethod().Name + "()");
            cnt.Location = orLocation[i];
            cnt.Width = orWidth[i];
            cnt.Height = orHeigth[i];
            cnt.Font = new Font(cnt.Font.FontFamily.Name, orFontSize[i], cnt.Font.Style);

        }
        private void ResizeAll(Control cnt, Size newSize)
        {
            //printDebug("calling " + System.Reflection.MethodBase.GetCurrentMethod().Name + "()");
            float factor = ((float)newSize.Height / (float)oldSize.Height);
            cnt.Scale(new SizeF((float)newSize.Width / (float)oldSize.Width, (float)newSize.Height / (float)oldSize.Height));
            cnt.Font = new Font(cnt.Font.FontFamily.Name, cnt.Font.Size * factor, cnt.Font.Style);
        }

        // END --- Stepless UI Scaling Methods ---

        public mapperForm()
        {

            // Aktivieren um Englisch zu erzwingen:
            //var culture = new CultureInfo("en-US");
            //CultureInfo.DefaultThreadCurrentCulture = culture;
            //CultureInfo.DefaultThreadCurrentUICulture = culture;

            // Overall initialization of the program

            printDebug("calling " + System.Reflection.MethodBase.GetCurrentMethod().Name + "()");


            if (File.Exists(@"Project.dat"))
            {
                projectIndentifier = File.ReadAllText(@"Project.dat", Encoding.ASCII);
            }

            String connectionData = "", encrypted = "";
            // Try to get the connection data from the Windows registry:
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Miracum-Mapper");

            if (registryKey != null && registryKey.GetValue(projectIndentifier) != null) // It's already in the Windows registry, so load it from there.
            {
                encrypted = (string)registryKey.GetValue(projectIndentifier);
                connectionData = EncDec.Decrypt(encrypted, "TrustNo1");
            }

            // Read and check if the configuration file needs to be encrypted:
            try
            {

                if (File.Exists(@"Config.dat"))
                {

                    String fileContents = File.ReadAllText(@"Config.dat", Encoding.ASCII);
                    String encryptedFile;

                    if (fileContents.Contains("Server")) // It's not encrypted, so encrypt and store it away.
                    {
                        encryptedFile = EncDec.Encrypt(fileContents, "TrustNo1");
                        File.WriteAllText(@"config.dat", encryptedFile);
                    }
                    else  // It's encrypted, so decrypt it
                    {
                        encryptedFile = fileContents;
                        fileContents = EncDec.Decrypt(fileContents, "TrustNo1");
                    }

                    if (registryKey == null || registryKey.GetValue(projectIndentifier) == null) // It's not stored in the Windows registry yet
                    {
                        connectionData = fileContents;

                        // Store connection data in registry:
                        RegistryKey newKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Miracum-Mapper");
                        newKey.SetValue(projectIndentifier, encryptedFile);
                        newKey.Close();
                    }
                }

            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to process configuration file!", "MIRACUM Mapper: ERROR");
                terminate();
            }

            if (connectionData.Equals(""))
            {
                MessageBox.Show("Failed to bootstrap configuration!", "MIRACUM Mapper: ERROR");
                terminate();
            }

            connectionData = connectionData.Replace("\r", "");
            jdbcConnectionString = connectionData.Split('\n')[0].Trim().Replace("\r", "").Replace("\n", "");
            activeDirectoryServer = connectionData.Split('\n')[1].Trim().Replace("\r", "").Replace("\n", "");
            if (connectionData.Split('\n').Length > 2)
            {
                visualizerUrl = connectionData.Split('\n')[2].Trim().Replace("\r", "").Replace("\n", "");
            }

            string[] connectionStringParts = jdbcConnectionString.Split(';');
            string server = "", database = "";

            for (int a = 0; a < connectionStringParts.Length; a++)
            {
                string part = connectionStringParts[a];
                string[] partParts = connectionStringParts[a].Split('=');
                string value = "";
                if (partParts.Length > 1) value = partParts[1];

                if (part.ToLower().Contains("database"))
                {
                    database = value;
                }
                if (part.ToLower().Contains("server"))
                {
                    server = value;
                }
            }

            InitializeComponent();

            dbConnection1 = new NpgsqlConnection(jdbcConnectionString);
            dbConnection1.Open();
            dbConnection2 = new NpgsqlConnection(jdbcConnectionString);
            dbConnection2.Open();
            dbConnection3 = new NpgsqlConnection(jdbcConnectionString);
            dbConnection3.Open();

            //mappingTermsList.DrawMode = DrawMode.OwnerDrawFixed;

            log("Starting application " + this.Text);
            this.CenterToScreen();
            showAbove.DropDownStyle = ComboBoxStyle.DropDownList;
            showBelow.DropDownStyle = ComboBoxStyle.DropDownList;

            if (!CultureInfo.CurrentCulture.Name.Equals("de-DE")) // switch GUI to English if necessary
            {
                label1.Text = "Source Terminology";
                label2.Text = "Mappings";
                label5.Text = "Target Terminology";
                label6.Text = "Code:";
                label7.Text = "Second Local Code:";
                label8.Text = "Condition:";
                label9.Text = "Documentation:";
                label9.Text = "Documentation:";
                label10.Text = "Filter:";
                label3.Text = "From:";
                label4.Text = "To:";
                addNewMappingBtn.Text = "Add New Mapping";
                aktZustLabel.Text = "Current state:";
                nextZustLabel.Text = "Next state:";
                showDeleted.Text = "Also display deleted mappings";
                showAllMappings.Text = "Show mappings of any state";
                addCommentBtn.Text = "Add Comment";
                visualizeBtn.Text = "Visualize";
                removeMappingBtn.Text = "Delete";
                saveMappingBtn.Text = "Accept/Save";
                logInButton.Text = "Login";
                loincSearchBtn.Text = "Search";
                WorkingStatus.Text = "Not logged in!";
                searchDoku.Text = "Search";
                searchInfo.Text = "Search";
                searchSource.Text = "Search";
                searchTarget.Text = "Search";
                noMappingBtn.Text = "No Mapping";
                aktualisierenBtn.Text = "Refresh";
            }

            if (!projectIndentifier.Equals("Default"))
            {
                if (CultureInfo.CurrentCulture.Name.Equals("de-DE")) this.Text = this.Text + "            Projekt: " + projectIndentifier;
                if (!CultureInfo.CurrentCulture.Name.Equals("de-DE")) this.Text = this.Text + "            Project: " + projectIndentifier;
                this.Refresh();
            }

            if (!server.Equals("") && !database.Equals(""))
            {
                if (CultureInfo.CurrentCulture.Name.Equals("de-DE")) this.Text = this.Text + "            Datenbank: " + database + ", Server: " + server + "";
                if (!CultureInfo.CurrentCulture.Name.Equals("de-DE")) this.Text = this.Text + "            Database: " + database + ", Server: " + server + "";
                this.Refresh();
            }

            backupCtrlSizes();

            oldSize = base.Size;
            this.Size = new Size(1000, 725);
            this.CenterToScreen();

            if (this.WindowState == FormWindowState.Minimized)
            {
                this.WindowState = FormWindowState.Normal;
            }
            this.WindowState = FormWindowState.Minimized;
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();

            searchDoku.Visible = false;
            searchInfo.Visible = false;
            searchSource.Visible = false;
            searchTarget.Visible = false;
            blockPanel.Visible = false;
            removeMappingBtn.Visible = false;
            saveMappingBtn.Visible = false;
            noMappingBtn.Visible = false;
            aktualisierenBtn.Visible = false;
            visualizeBtn.Visible = false;

            checkSoftwareVersion();
        }

        private void checkSoftwareVersion() // Check if we're compatible with the database
        {
            printDebug("calling " + System.Reflection.MethodBase.GetCurrentMethod().Name + "()");

            int reqVersion = 0;
            try
            {
                var cmd = new NpgsqlCommand("SELECT requiresversion FROM requiresversion", dbConnection1);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    reqVersion = Int32.Parse(reader["requiresversion"].ToString());
                }
                reader.Close();

            }
            catch (Exception msg)
            {
                MessageBox.Show(msg.ToString(), "MIRACUM Mapper: ERROR");
                throw;
                terminate();
            }
            if (reqVersion > thisVersion || reqVersion == 0)
            {
                MessageBox.Show("A newer version of MIRACUM Mapper is required by the server. Please update this program!", "MIRACUM Mapper: Software Version Outdated!");
                terminate();
            }

            // Test, if DB schema version is greater than 1 (this is the case if column "local_password_md5" exits in table "userids"):
            try
            {
                var cmd = new NpgsqlCommand("SELECT column_name FROM information_schema.columns WHERE table_name = 'userids' and column_name = 'local_password_md5'", dbConnection1);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    if (reader["column_name"].ToString().Equals("local_password_md5"))
                    {
                        schemaVersion = 2;
                    }
                }
                reader.Close();

            }
            catch (Exception msg)
            {
                MessageBox.Show(msg.ToString(), "MIRACUM Mapper: ERROR");
                throw;
                terminate();
            }

            if (schemaVersion >= 2)
            {
                // Test, if DB schema version is greater than 1 (this is the case if column "local_password_md5" exits in table "userids"):
                try
                {
                    var cmd = new NpgsqlCommand("select schemaversion from schemaversion", dbConnection1);
                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        schemaVersion = Int32.Parse(reader["schemaversion"].ToString());
                    }
                    reader.Close();

                }
                catch (Exception msg)
                {
                    MessageBox.Show(msg.ToString(), "MIRACUM Mapper: ERROR");
                    throw;
                    terminate();
                }
            }


            if (schemaVersion >= 3)
            {
                String nextJdbcConnectionString = "";
                String nextActiveDirectoryServer = "";
                String nextVisualizerUrl = "";

                try
                {
                    var cmd = new NpgsqlCommand("select db, ldap, visualizer from next_connection", dbConnection1);
                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        nextJdbcConnectionString = reader["db"].ToString().Trim().Replace("\r", "").Replace("\n", "");
                        nextActiveDirectoryServer = reader["ldap"].ToString().Trim().Replace("\r", "").Replace("\n", "");
                        nextVisualizerUrl = reader["visualizer"].ToString().Trim().Replace("\r", "").Replace("\n", "");
                    }
                    reader.Close();


                    if (!String.Equals(nextActiveDirectoryServer, activeDirectoryServer) || !String.Equals(nextJdbcConnectionString, jdbcConnectionString) || !String.Equals(nextVisualizerUrl, visualizerUrl))
                    {

                        // Store connection data in registry:
                        RegistryKey newKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Miracum-Mapper");
                        newKey.SetValue(projectIndentifier, EncDec.Encrypt(nextJdbcConnectionString + "\n" + nextActiveDirectoryServer + "\n" + nextVisualizerUrl, "TrustNo1"));
                        newKey.Close();

                        //File.WriteAllBytes(@"config.dat", Encoding.ASCII.GetBytes(EncDec.Encrypt(nextJdbcConnectionString + "\n" + nextActiveDirectoryServer + "\n" + nextVisualizerUrl, "TrustNo1")));
                    }
                    if (!String.Equals(nextActiveDirectoryServer, activeDirectoryServer))
                    {
                        MessageBox.Show("LDAP server configuration has been updated. Please restart the program.", "MIRACUM Mapper");
                        terminate();
                    }
                    if (!String.Equals(nextJdbcConnectionString, jdbcConnectionString))
                    {
                        MessageBox.Show("Database server configuration has been updated. Please restart the program.", "MIRACUM Mapper");
                        terminate();
                    }
                    if (!String.Equals(nextVisualizerUrl, visualizerUrl))
                    {
                        MessageBox.Show("Visualizer server configuration has been updated. Please restart the program. " + nextVisualizerUrl + " " + visualizerUrl, "MIRACUM Mapper");
                        terminate();
                    }
                }
                catch (Exception msg)
                {
                    MessageBox.Show(msg.ToString(), "MIRACUM Mapper: ERROR");
                    throw;
                    terminate();
                }
            }
        }

        private void terminate() // Terminate program
        {
            printDebug("calling " + System.Reflection.MethodBase.GetCurrentMethod().Name + "()");

            log("Terminating application.");

            if (dbConnection1 != null && dbConnection1.FullState != ConnectionState.Closed) { dbConnection1.Close(); }
            if (dbConnection2 != null && dbConnection2.FullState != ConnectionState.Closed) { dbConnection2.Close(); }
            if (dbConnection3 != null && dbConnection3.FullState != ConnectionState.Closed) { dbConnection3.Close(); }

            if (System.Windows.Forms.Application.MessageLoop)
            {
                // WinForms app
                System.Windows.Forms.Application.Exit();
            }
            else
            {
                // Console app
                System.Environment.Exit(1);
            }
        }

        private void authenticate(string user) // Authenticate user against database
        {
            printDebug("calling " + System.Reflection.MethodBase.GetCurrentMethod().Name + "()");

            Boolean authenticated = false;
            Boolean enabled = false;
            userFilterFrom = 0;
            userFilterTo = 99;
            userAllowBrowseHistory = false;
            userForcedFilter = "";
            userTransitions = "";

            try
            {
                var cmd = new NpgsqlCommand();
                cmd.CommandText = "SELECT userid, enabled, filterfrom, filterto, allowbrowsehistory, forcedfilter, transitions FROM userids WHERE userid = @user";
                cmd.Parameters.AddWithValue("user", user);
                cmd.Connection = dbConnection1;

                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    if (reader["userid"].ToString().Equals(user)) authenticated = true;
                    if (reader["enabled"].ToString().Equals("True")) enabled = true;
                    userFilterFrom = Int32.Parse(reader["filterfrom"].ToString());
                    userFilterTo = Int32.Parse(reader["filterto"].ToString());
                    userTransitions = reader["transitions"].ToString();
                    if (reader["allowbrowsehistory"].ToString().Equals("True")) userAllowBrowseHistory = true;
                    userForcedFilter = reader["forcedfilter"].ToString();
                }
                reader.Close();

            }
            catch (Exception msg)
            {
                MessageBox.Show(msg.ToString(), "MIRACUM Mapper: ERROR");
                throw;
                terminate();
            }

            if (authenticated && enabled)
            {
                loadExpertLevels();
                loadingInProgress = true;
                setFilterFromLevel();
                setFilterToLevel();
                updateWindowStartingWithSoureTerms();
                loadingInProgress = false;
                log("User " + user + " authenticated.");
                sourceTermsList.Enabled = true;
                mappingTermsList.Enabled = true;

                if (visualizerUrl.Contains("http"))
                {
                    visualizeBtn.Visible = true;
                }

                sourceFilter.Enabled = true;
                showAbove.Enabled = true;
                showBelow.Enabled = true;
                addNewMappingBtn.Enabled = true;
                removeMappingBtn.Enabled = true;
                showAllMappings.Enabled = true;
                showDeleted.Enabled = true;

                if (userAllowBrowseHistory)
                {
                    previousVersionBtn.Enabled = true;
                    nextVersionBtn.Enabled = true;
                }
                checkDisableNewMapping();
            }
            else
            {
                if (!authenticated)
                {
                    startWorking();
                    try
                    {
                        var cmd = new NpgsqlCommand();
                        cmd.CommandText = "INSERT INTO userids (userid) VALUES (@user)";
                        cmd.Parameters.AddWithValue("user", user);
                        cmd.Connection = dbConnection1;
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception msg)
                    {
                        MessageBox.Show(msg.ToString(), "MIRACUM Mapper: ERROR");
                        throw;
                    }
                    stopWorking();
                    log("User " + user + " registered.");
                    MessageBox.Show("You are now registered. Please contact us so that we activate your account.", "MIRACUM Mapper Registration");
                    terminate();
                }
                if (!enabled)
                {
                    log("User " + user + " is not activated.");
                    MessageBox.Show("Your account is not not yet activated. Please contact us again so that we activate your account.", "MIRACUM Mapper Registration");
                    terminate();
                }
            }
        }

        private void loadExpertLevels() // Load the state descriptions (= expert levels) from database
        {
            printDebug("calling " + System.Reflection.MethodBase.GetCurrentMethod().Name + "()");
            startWorking();
            try
            {
                // Temporarily disable event handler for the two level combo boxes:
                this.showBelow.SelectedValueChanged -= new System.EventHandler(this.showBelow_SelectedValueChanged);
                this.showAbove.SelectedValueChanged -= new System.EventHandler(this.showAbove_SelectedValueChanged);

                showAbove.Items.Clear();
                showBelow.Items.Clear();
                sendToAccept.Items.Clear();

                // Read Expert Levels from Database
                int maxLevel = 0;
                int index = 0;
                var cmd = new NpgsqlCommand("SELECT description, level FROM expertlevel ORDER BY level", dbConnection1);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string description = reader["description"].ToString();
                    int level = Int32.Parse(reader["level"].ToString());

                    showAbove.Items.Add(translate(description));
                    if (level == userFilterFrom)
                    {
                        showAbove.SelectedIndex = index;
                    }

                    showBelow.Items.Add(translate(description));
                    if (level == userFilterTo)
                    {
                        showBelow.SelectedIndex = index;
                    }
                    index++;
                }
                reader.Close();

            }
            catch (Exception msg)
            {
                MessageBox.Show(msg.ToString(), "MIRACUM Mapper: ERROR");
                throw;
            }

            // Enable event handler for the two level combo boxes again:
            this.showBelow.SelectedValueChanged += new System.EventHandler(this.showBelow_SelectedValueChanged);
            this.showAbove.SelectedValueChanged += new System.EventHandler(this.showAbove_SelectedValueChanged);
            stopWorking();
        }

        //Boolean disableBackupAndRestorePositions = false;
        private void loadSourceTerms() // Load the source terms list on the left side of the window
        {
            printDebug("calling " + System.Reflection.MethodBase.GetCurrentMethod().Name + "() #############");

            startWorking();
            disableEventHandlers();
            //mappingTermsList.Items.Clear();
            //resetTerminologyPart(false);
            //backupListPositionAndName(sourceTermsList, 0, 0);
            //backupListPositionAndName(mappingTermsList, 0, 1);
            sourceTermsList.Items.Clear();
            //disableBackupAndRestorePositions = true;

            try
            {
                List<String> alreadyMapped = new List<String>();

                var cmd = new NpgsqlCommand("SELECT distinct source_code FROM mapping", dbConnection1);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string source_code = reader["source_code"].ToString();
                    alreadyMapped.Add(source_code);
                }
                reader.Close();

                string textFilter = "";
                string wordFilter = "";
                string textFilter2 = "";
                string wordFilter2 = "";

                if (!sourceFilter.Text.Trim().Equals(""))
                {
                    string[] words = sourceFilter.Text.Trim().Split(' ');
                    for (int a = 0; a < words.Length; a++)
                    {
                        wordFilter = "";
                        wordFilter2 = "";
                        String sWord = words[a].ToUpper();
                        if (!sWord.Contains("%"))
                        {
                            sWord = "%" + sWord + "%";
                        }

                        if (searchSource.Checked)
                        {
                            wordFilter = wordFilter + " upper(source_code) ";
                            wordFilter2 = wordFilter2 + " upper(source_code) ";
                        }

                        if (searchTarget.Checked)
                        {
                            wordFilter = wordFilter + " upper(target_code) ";
                        }

                        if (searchInfo.Checked)
                        {
                            wordFilter = wordFilter + " upper(source_desc) ";
                            wordFilter2 = wordFilter2 + " upper(source_desc) ";
                        }

                        if (searchDoku.Checked)
                        {
                            wordFilter = wordFilter + " upper(documentation) ";
                        }

                        if (!wordFilter.Equals(""))
                        {
                            wordFilter = "AND " + wordFilter.Trim().Replace("  ", " || ") + " SIMILAR TO '" + sWord + "' ";
                        }

                        if (!wordFilter2.Equals(""))
                        {
                            wordFilter2 = "AND " + wordFilter2.Trim().Replace("  ", " || ") + " SIMILAR TO '" + sWord + "' ";
                        }

                        textFilter = textFilter + wordFilter;
                        textFilter2 = textFilter2 + wordFilter2;
                    }
                }

                string forcedFilter = " ";
                if (!userForcedFilter.Equals("")) forcedFilter = " AND " + userForcedFilter;

                string sql = @"SELECT DISTINCT source_code
	                        , source_desc
	                        , max(ts) ts
                        FROM (
	                        WITH all_mappings AS (
			                        WITH latest_mapping_versions AS (
					                        SELECT DISTINCT ON (
							                        source_code
							                        , target_code
							                        ) source_code
						                        , target_code
						                        , version max_version
						                        , TIMESTAMP timestmp
					                        FROM mapping
					                        ORDER BY source_code ASC
						                        , target_code ASC
						                        , version DESC
						                        , timestmp DESC
					                        )
			                        SELECT * FROM (
			                            SELECT lm.source_code
				                            , lm.target_code
				                            , lm.max_version
				                            , lm.timestmp timestmp
				                            , am.sec_source_code
				                            , am.sec_source_code_cond
				                            , am.documentation
				                            , am.mapping_level
				                            , am.deleted
				                            , am.saved_by
				                            , am.sw_version
				                            , coalesce(st.source_desc, '') AS source_desc
			                            FROM latest_mapping_versions lm
			                            LEFT JOIN mapping am ON lm.source_code = am.source_code
				                            AND lm.target_code = am.target_code
				                            AND lm.max_version = am.version
			                            LEFT JOIN sourceterms st ON lm.source_code = st.source_code
                                    ) SQ
                                    WHERE 1 = 1 $TEXTFILTER$
			                        ORDER BY source_code ASC
			                        )
		                        , not_deleted_mappings AS (
			                        SELECT mm.source_code
				                        , mm.source_desc
				                        , mm.mapping_level
				                        , mm.timestmp ts
			                        FROM all_mappings mm
			                        WHERE mm.deleted != 1
			                        )
		                        , deleted_mappings AS (
			                        SELECT mm.source_code
				                        , mm.source_desc
				                        , 0 mapping_level
				                        , mm.timestmp ts
			                        FROM all_mappings mm
			                        WHERE mm.deleted = 1
				                        AND mm.source_code NOT IN (
					                        SELECT source_code
					                        FROM not_deleted_mappings
					                        )
			                        )
		                        , unmapped_codes AS (
			                        SELECT st.source_code
				                        , source_desc
				                        , 0
				                        , to_timestamp('1900-01-01 1:00:00', 'YYYY-MM-DD HH:MI:SS') AS ts
			                        FROM sourceterms st
			                        WHERE st.source_code NOT IN (
					                        SELECT source_code
					                        FROM not_deleted_mappings
					                        )
				                        AND st.source_code NOT IN (
					                        SELECT source_code
					                        FROM deleted_mappings
					                        )
                                        $TEXTFILTER2$
			                        )
	                        SELECT *
	                        FROM not_deleted_mappings
	                        UNION
	                        SELECT *
	                        FROM deleted_mappings
	                        UNION
	                        SELECT *
	                        FROM unmapped_codes
	                        )
                        WHERE mapping_level >= '$FILTERFROMLEVEL$'
	                        AND mapping_level <= '$FITLERTOLEVEL$' $FORCEDFILTER$
                        GROUP BY source_code
	                        , source_desc
                        ORDER BY ts DESC
	                        , source_code ASC";

                if (searchTarget.Checked || searchDoku.Checked)
                {
                    // When searching Target or Doku, disable adding all missing codes to the Source list via subquery "unmapped_codes":
                    textFilter2 = " AND 1 = 0 ";
                }

                if (!sourceFilter.Text.Trim().Equals(""))
                {
                    // When searching for anything, disable the requirement that a mapping has to be not deleted in subquery "not_deleted_mappings":
                    sql = sql.Replace("WHERE mm.deleted != 1", "");
                }

                sql = sql.Replace("$TEXTFILTER$", textFilter);
                sql = sql.Replace("$TEXTFILTER2$", textFilter2);
                sql = sql.Replace("$FILTERFROMLEVEL$", filterFromLevel + "");
                sql = sql.Replace("$FITLERTOLEVEL$", filterToLevel + "");
                sql = sql.Replace("$FORCEDFILTER$", forcedFilter);

                Console.WriteLine(sql);

                try
                {
                    var cmd2 = new NpgsqlCommand(sql, dbConnection1);
                    var reader2 = cmd2.ExecuteReader();
                    while (reader2.Read())
                    {
                        string source_code = reader2["source_code"].ToString();
                        string source_desc = reader2["source_desc"].ToString();
                        ListboxItem item = new ListboxItem();

                        string tag = "";
                        if (alreadyMapped.Contains(source_code))
                        {
                            tag = "**** ";
                        }

                        item.Text = source_code;
                        if (!sourceTermsList.Items.Contains(item)) sourceTermsList.Items.Add(item);
                    }
                    reader2.Close();
                }
                catch (Exception msg)
                {
                    MessageBox.Show("Invalid filter expression!", "MIRACUM Mapper: ERROR");
                }
            }
            catch (Exception msg)
            {
                MessageBox.Show(msg.ToString(), "MIRACUM Mapper: ERROR");
                throw;
            }

            //restoreListByName(sourceTermsList, 0);
            loadMappings();
            //restoreListByName(mappingTermsList, 1);
            enableEventHandlers();
            stopWorking();
        }

        private void backupListPositionAndName(ListBox listBox, int delta, int index) // Backup selection in lists
        {
            printDebug("calling " + System.Reflection.MethodBase.GetCurrentMethod().Name + "()");
            printDebug("backing up index " + index);

            //if (disableBackupAndRestorePositions) return;

            int pos = listBox.SelectedIndex + delta;
            backupItemPos[index] = pos;

            if (listBox.SelectedItem != null && pos < listBox.Items.Count)
            {
                backupItemName[index] = listBox.Items[pos].ToString();
                printDebug("Backed up term: " + listBox.Items[pos].ToString());
                ;
            }
        }
        private void restoreListByName(ListBox listBox, int index) // Restore selection in lists
        {
            printDebug("calling " + System.Reflection.MethodBase.GetCurrentMethod().Name + "()");
            printDebug("restoring index " + index);

            //if (disableBackupAndRestorePositions) return;

            for (int i = 0; i < listBox.Items.Count; i++)
            {
                string item = listBox.Items[i].ToString();
                if (backupItemName[index].Replace("(X) ", "").Equals(item) ||
                    ("(X) " + backupItemName[index]).Equals(item))
                {
                    listBox.SetSelected(i, true);
                    break;
                }
            }

            checkDisableNewMapping();
        }
        private void restoreListByPos(ListBox listBox, int index) // Restore selection in lists
        {
            printDebug("calling " + System.Reflection.MethodBase.GetCurrentMethod().Name + "()");
            printDebug("restoring index " + index);

            //if (disableBackupAndRestorePositions) return;

            if (backupItemPos[index] + 1 < listBox.Items.Count - 1)
            {
                listBox.SelectedIndex = backupItemPos[index] + 1;
            }
        }

        private void stopWorking() // Set status bar on the bottom of the window to "Ready"
        {
            printDebug("calling " + System.Reflection.MethodBase.GetCurrentMethod().Name + "()");
            if (unsavedChanges > 0) return;
            working = false;
            WorkingStatus.Text = "" + sourceTermsList.Items.Count + " Codes der Quellterminologie angezeigt";
            if (!CultureInfo.CurrentCulture.Name.Equals("de-DE")) WorkingStatus.Text = "Displaying " + sourceTermsList.Items.Count + " codes in source terminology";
            WorkingStatus.BackColor = Color.LightGreen;
        }

        private void startWorking() // Set status bar on the bottom of the window to "Working"
        {
            printDebug("calling " + System.Reflection.MethodBase.GetCurrentMethod().Name + "()");
            if (unsavedChanges > 0) return;
            working = true;
            WorkingStatus.Text = "Arbeite ...";
            if (!CultureInfo.CurrentCulture.Name.Equals("de-DE")) WorkingStatus.Text = "Working ...";
            WorkingStatus.BackColor = Color.Salmon;
            WorkingStatus.Update();
        }

        private void addNewMapping_Click(object sender, EventArgs e) // User adds a new mapping (middle column)
        {
            printDebug("calling " + System.Reflection.MethodBase.GetCurrentMethod().Name + "()");

            disableEventHandlers();
            ListboxItem item = new ListboxItem();
            item.Text = "Neues Mapping";
            editedMapping = item.Text;
            mappingTermsList.Items.Add(item);
            mappingTermsList.SelectedIndex = mappingTermsList.Items.Count - 1;

            targetCode.Text = "Neues Mapping";
            secondarySourceCode.Text = "";
            secondarySourceCodeCondition.Text = "";
            documentationText.Text = "";
            currentLevelIndicator.Text = "";
            mappingVersion.Text = "0";
            loadTransitions(-1);

            // Do not show the buttons because we don't want to save "Neues Mapping" directly:
            removeMappingBtn.Visible = false;
            saveMappingBtn.Visible = false;

            enableInputFields();
            targetCode.Focus();
            setUnsavedChanges(2);
            handleTargetCodeUpdateEvent();
            enableEventHandlers();
        }

        private void loadTransitions(int requestedStartLevel) // Loads the state transitions for a given level
        {
            printDebug("calling " + System.Reflection.MethodBase.GetCurrentMethod().Name + "()");
            this.sendToAccept.SelectedValueChanged -= new System.EventHandler(this.sendToAccept_SelectedValueChanged);

            printDebug("");
            printDebug("Processing state transitions:");
            printDebug("requestedStartLevel=" + requestedStartLevel);

            sendToAccept.Items.Clear();
            string[] transis = userTransitions.Split(' ');
            for (int a = 0; a < transis.Length; a++)
            {
                int transitionFrom = Int32.Parse(transis[a].Split('-')[0]);
                int transitionTo = Int32.Parse(transis[a].Split('-')[1]);
                printDebug("Trans: " + transitionFrom + "-" + transitionTo);

                if (requestedStartLevel != -1) // A specific version start state was given, add all target states for the requested start level.
                {
                    if (requestedStartLevel == transitionFrom && !sendToAccept.Items.Contains(showAbove.Items[transitionTo].ToString()))
                    {
                        printDebug("Adding " + showAbove.Items[transitionTo].ToString());
                        sendToAccept.Items.Add(showAbove.Items[transitionTo].ToString());
                        saveMappingBtn.Text = "=> " + showAbove.Items[transitionTo].ToString();
                        sendToAccept.SelectedIndex = sendToAccept.Items.Count - 1;
                        userAcceptTo = transitionTo;
                    }
                }
                else  // No specific start state was requested. Add ALL target levels from the user from the user's permitted transitions 
                      // where the start and target levels are NOT equal. E.g., for the permitted transitions "0-2 0-3 1-2 1-3 2-3 2-2 3-2 3-3 5-5",
                      // it will add 2 and 3.
                {
                    if (transitionFrom != transitionTo && !sendToAccept.Items.Contains(showAbove.Items[transitionTo].ToString()))
                    {
                        printDebug("Adding " + showAbove.Items[transitionTo].ToString());
                        //sendToAccept.Items.Clear(); // <= Enabled = only display the last one
                        sendToAccept.Items.Add(showAbove.Items[transitionTo].ToString());
                        saveMappingBtn.Text = "=> " + showAbove.Items[transitionTo].ToString();
                        sendToAccept.SelectedIndex = sendToAccept.Items.Count - 1;
                        userAcceptTo = transitionTo;
                    }
                }
            }

            if (sendToAccept.Items.Count == 1 && sendToAccept.Items[0].ToString().Equals(currentLevelIndicator.Text))
            {
                saveMappingBtn.Text = "Speichern";
                if (!CultureInfo.CurrentCulture.Name.Equals("de-DE"))
                    saveMappingBtn.Text = "Save";
            }

            printDebug("");
            this.sendToAccept.SelectedValueChanged += new System.EventHandler(this.sendToAccept_SelectedValueChanged);
        }

        private String translate(string input)
        {
            String result = input;
            if (!CultureInfo.CurrentCulture.Name.Equals("de-DE"))
            {
                switch (input)
                {
                    case "Ungemappt":
                        result = "Not Mapped";
                        break;
                    case "Zum Überarbeiten (Exp.)":
                        result = "To Revise by Expert";
                        break;
                    case "Gemappt (Experte)":
                        result = "Mapped By Expert";
                        break;
                    case "In Bearbeitung (Labor)":
                        result = "In Work by Lab";
                        break;
                    case "Validiert (Labor)":
                        result = "Validated by Lab";
                        break;
                    case "In Bearbeitung (Experte)":
                        result = "In Work by Lab";
                        break;
                }
            }
            return result;
        }

        private void saveMappingBtn_Click(object sender, EventArgs e) // User presses the green button on the lower right (= save)
        {
            backupListPositionAndName(sourceTermsList, 1, 0);
            backupListPositionAndName(mappingTermsList, 0, 1);

            printDebug("calling " + System.Reflection.MethodBase.GetCurrentMethod().Name + "()");
            disableEventHandlers();
            if (saveMappingBtn.Text.Equals("Speichern") || saveMappingBtn.Text.Equals("Save"))
            {
                if (CultureInfo.CurrentCulture.Name.Equals("de-DE"))
                    saveMapping(false, false, "Gespeichert", true);
                if (!CultureInfo.CurrentCulture.Name.Equals("de-DE"))
                    saveMapping(false, false, "Saved", true);
            }
            else
            {
                saveMapping(false, false, sendToAccept.Text, true);
            }

            //loadSourceTerms();
            restoreListByName(sourceTermsList, 0);
            loadMappings();
            restoreListByName(mappingTermsList, 1); // Ohne Effekt
            enableEventHandlers();
            setUnsavedChanges(0);
        }

        private void removeMappingBtn_Click(object sender, EventArgs e) // User presses the delete button
        {

            printDebug("calling " + System.Reflection.MethodBase.GetCurrentMethod().Name + "()");
            disableEventHandlers();
            backupListPositionAndName(sourceTermsList, 0, 0);
            backupListPositionAndName(mappingTermsList, 0, 1);

            if (removeMappingBtn.Text.Equals("Speichern") || removeMappingBtn.Text.Equals("Save"))
            {
                if (CultureInfo.CurrentCulture.Name.Equals("de-DE"))
                    saveMapping(true, true, "Gespeichert", false);
                if (!CultureInfo.CurrentCulture.Name.Equals("de-DE"))
                    saveMapping(true, true, "Saved", false);
            }
            else
            {
                if (isDeleted == false)
                {
                    if (CultureInfo.CurrentCulture.Name.Equals("de-DE"))
                        saveMapping(true, true, "Gelöscht", false);
                    if (!CultureInfo.CurrentCulture.Name.Equals("de-DE"))
                        saveMapping(true, true, "Deleted", false);
                }
                else
                {
                    if (CultureInfo.CurrentCulture.Name.Equals("de-DE"))
                        saveMapping(true, false, "Wiederhergestellt", false);
                    if (!CultureInfo.CurrentCulture.Name.Equals("de-DE"))
                        saveMapping(true, false, "Restored", false);
                }
            }

            //loadSourceTerms(); -- Liste NICHT neu laden.
            //restoreListByName(sourceTermsList, 0); -- Liste NICHT neu laden.
            aktualisierenBtn.Visible = true;
            loadMappings();
            restoreListByName(mappingTermsList, 1);
            enableEventHandlers();
            setUnsavedChanges(0);
        }

        private void saveMapping(bool deleteAction, bool isDeleted, string text, bool jumpToNext) // Saves the mapping to the database
        {
            printDebug("calling " + System.Reflection.MethodBase.GetCurrentMethod().Name + "()");
            startWorking();
            //backupListPositionAndName(sourceTermsList, 0);
            //backupListPositionAndName(mappingTermsList, 1);

            try
            {
                mappingVersion.Text = "" + (Int32.Parse(mappingVersion.Text) + 1);
            }
            catch (Exception e)
            {
                MessageBox.Show("Bitte an Sebastian melden: Konnte Version nicht inkrementieren.", "MIRACUM Mapper: ERROR");
                throw;
            }
            string source_code = "";
            try
            {
                source_code = sourceTermsList.SelectedItem.ToString();
            }
            catch (Exception e)
            {
                MessageBox.Show("Bitte an Sebastian melden: Kein SourceTerm ausgewählt.", "MIRACUM Mapper: ERROR");
                throw;
            }

            string mapping_level = "99";
            mapping_level = "" + userAcceptTo;
            if (deleteAction) mapping_level = "" + userRejectTo;
            addDocumentation("" + text);

            try
            {
                string target_code = targetCode.Text;
                lastSaved = target_code;
                string sec_source_code = secondarySourceCode.Text;
                string sec_source_code_cond = secondarySourceCodeCondition.Text;
                string documentation = documentationText.Text;
                string version = mappingVersion.Text;
                string deleted = "0";
                if (isDeleted) deleted = "1";
                //string insertString = "'" + source_code + "', '" + target_code + "', '" + sec_source_code + "', '" + sec_source_code_cond + "', '" + documentation + "', '" + mapping_level + "', " + version + ", " + deleted + "" +
                //    ", '" + userLoggedIn + "', '" + this.Text + "'";

                var cmd = new NpgsqlCommand();
                cmd.CommandText = "INSERT INTO mapping (source_code, target_code, sec_source_code, sec_source_code_cond, documentation, mapping_level, version, deleted, saved_by, sw_version) VALUES (@source_code, @target_code, @sec_source_code, @sec_source_code_cond, @documentation, @mapping_level,@version, @deleted, @userLoggedIn, @thisText)";
                cmd.Parameters.AddWithValue("source_code", source_code);
                cmd.Parameters.AddWithValue("target_code", target_code);
                cmd.Parameters.AddWithValue("sec_source_code", sec_source_code);
                cmd.Parameters.AddWithValue("sec_source_code_cond", sec_source_code_cond);
                cmd.Parameters.AddWithValue("documentation", documentation);
                cmd.Parameters.AddWithValue("mapping_level", Int32.Parse(mapping_level));
                cmd.Parameters.AddWithValue("version", Int32.Parse(version));
                cmd.Parameters.AddWithValue("deleted", Int32.Parse(deleted));
                cmd.Parameters.AddWithValue("userLoggedIn", userLoggedIn);
                cmd.Parameters.AddWithValue("thisText", this.Text);
                cmd.Connection = dbConnection1;
                cmd.ExecuteNonQuery();

            }
            catch (Exception msg)
            {
                MessageBox.Show(msg.ToString(), "MIRACUM Mapper: ERROR");  // FIXME
                throw;
            }

            documentationText.ReadOnly = true;
            //currentSourceTermSelection = sourceTermsList.SelectedIndex;
            hideSaveDeleteButtons();
            disableInputFields();

            //loadSourceTerms();
            //loadMappings();
            //restoreListByName(mappingTermsList, 1);
            //if (jumpToNext)
            //{
            //    sourceTermsList.SetSelected(currentSourceTermSelection, true);
            //restoreListByPos(sourceTermsList, 0);
            //}
            //loadMappings();
            //restoreListByName(mappingTermsList, 1);
            aktualisierenBtn.Visible = true;
            updateLabVisalizer();
            stopWorking();
        }

        private void sourceTerms_SelectedIndexChanged(object sender, EventArgs e) // User has selected another source term on the left column
        {
            if (eventHandlersDisabled > 0) return;
            printDebug("calling " + System.Reflection.MethodBase.GetCurrentMethod().Name + "()");
            checkDisableNewMapping();
            disableInputFields();
            hideSaveDeleteButtons();
            addCommentBtn.Enabled = false;
            resetTerminologyPart(false);
            updateLabVisalizer();
            loadMappings();
        }

        private void checkDisableNewMapping()
        {
            if (sourceTermsList.SelectedIndex == -1)
            {
                addNewMappingBtn.Enabled = false;
                targetCode.Enabled = false;
                loincSearchBtn.Enabled = false;
            }
            else
            {
                addNewMappingBtn.Enabled = true;
                targetCode.Enabled = true;
                loincSearchBtn.Enabled = true;
            }
        }

        private void updateLabVisalizer() // Update the Miracum LabVisualizer
        {
            if (sourceTermsList.SelectedItem != null && wb.Visible) wb.showUrl(visualizerUrl + sourceTermsList.SelectedItem.ToString());
        }

        private void disableInputFields() // Hide the input fields on the right lower side
        {
            printDebug("calling " + System.Reflection.MethodBase.GetCurrentMethod().Name + "()");
            targetCode.Enabled = false;
            secondarySourceCode.Enabled = false;
            secondarySourceCodeCondition.Enabled = false;
            documentationText.Enabled = false;
            addCommentBtn.Enabled = false;
            sendToAccept.Enabled = false;
        }

        private void enableInputFields() // Show the input fields on the right lower side
        {
            printDebug("calling " + System.Reflection.MethodBase.GetCurrentMethod().Name + "()");
            targetCode.Enabled = true;
            secondarySourceCode.Enabled = true;
            secondarySourceCodeCondition.Enabled = true;
            documentationText.Enabled = true;
            addCommentBtn.Enabled = true;
            sendToAccept.Enabled = true;
        }

        private void loadMappings() // Loads and displays the mappings in the middle column
        {
            printDebug("calling " + System.Reflection.MethodBase.GetCurrentMethod().Name + "() #############");

            backupListPositionAndName(mappingTermsList, 0, 1);
            disableEventHandlers();
            SourceDescr.Text = "";

            if (sourceTermsList.SelectedItem != null)
            {
                documentationText.ReadOnly = false;
                string selectedSourceTerm = sourceTermsList.SelectedItem.ToString();
                mappingTermsList.Items.Clear();

                startWorking();
                try
                {
                    var cmd1 = new NpgsqlCommand();
                    cmd1.CommandText = "SELECT source_code, source_desc FROM sourceterms WHERE source_code = @selectedSourceTerm";
                    cmd1.Parameters.AddWithValue("selectedSourceTerm", selectedSourceTerm);
                    cmd1.Connection = dbConnection1;

                    NpgsqlDataReader reader1 = cmd1.ExecuteReader();
                    while (reader1.Read())
                    {
                        SourceDescr.Text = reader1["source_desc"].ToString();
                    }
                    reader1.Close();

                    // Add all mappings
                    string sql =
                        "select * from(select source_code, target_code, max(version) maxversion from mapping " +
                        "where source_code = '" + selectedSourceTerm + "' group by source_code, target_code) m1 " +
                        "join(select source_code, target_code, version, mapping_level from mapping) m2 " +
                        "on m1.source_code = m2.source_code and m1.target_code = m2.target_code and m1.maxversion = m2.version ";

                    if (!showAllMappings.Checked) sql += "where mapping_level >= " + filterFromLevel + " and mapping_level <= " + filterToLevel + " ";

                    printDebug(sql);

                    cmd1 = new NpgsqlCommand(sql, dbConnection1);
                    reader1 = cmd1.ExecuteReader();

                    if (reader1.HasRows)
                    {
                        while (reader1.Read())
                        {
                            string source_code = reader1["source_code"].ToString();
                            string target_code = reader1["target_code"].ToString();
                            string maxversion = reader1["maxversion"].ToString();

                            // Test if it's deleted. If not, add it to the list.

                            if (showDeleted.Checked)
                            {
                                sql = "select source_code, target_code, version, deleted from mapping where source_code = '" + source_code + "' and target_code ='" + target_code + "' and version = '" + maxversion + "'";
                            }
                            else
                            {
                                sql = "select source_code, target_code, version, deleted from mapping where source_code = '" + source_code + "' and target_code ='" + target_code + "' and version = '" + maxversion + "' and deleted = 0";
                            }

                            NpgsqlCommand cmd2 = new NpgsqlCommand(sql, dbConnection2);
                            NpgsqlDataReader reader2 = cmd2.ExecuteReader();
                            if (reader2.HasRows)
                            {
                                while (reader2.Read())
                                {
                                    source_code = reader2["source_code"].ToString();
                                    target_code = reader2["target_code"].ToString();
                                    string deleted = reader2["deleted"].ToString();

                                    ListboxItem item = new ListboxItem();
                                    item.Text = target_code;

                                    if (deleted == "1" && CultureInfo.CurrentCulture.Name.Equals("de-DE")) item.Text = "(X) " + item.Text;
                                    if (deleted == "1" && !CultureInfo.CurrentCulture.Name.Equals("de-DE")) item.Text = "(X) " + item.Text;

                                    mappingTermsList.Items.Add(item);
                                }
                            }
                            reader2.Close();
                        }
                    }
                    reader1.Close();
                }
                catch (Exception msg)
                {
                    MessageBox.Show(msg.ToString(), "MIRACUM Mapper: ERROR");
                    throw;
                }

                if (mappingTermsList.Items.Count > 0)
                {
                    restoreListByName(mappingTermsList, 1);
                    if (mappingTermsList.SelectedItem == null)
                    {
                        mappingTermsList.SelectedIndex = 0;
                        mappingTermsList.SetSelected(0, true);
                    }
                    else
                    {
                        restoreListByName(mappingTermsList, 1);
                    }
                    resetTerminologyPart(false);
                    loadTerminologyPart(sourceTermsList.SelectedItem.ToString(), mappingTermsList.SelectedItem.ToString(), -1, false);
                    loadTargetCodeDescription();
                    enableInputFields();
                }
            }
            else
            {
                mappingTermsList.Items.Clear();
                resetTerminologyPart(false);
            }
            enableEventHandlers();
            backupMappingList();
            stopWorking();
        }

        private void backupMappingList()  // Backup the mapping list (middle) for later restoring
        {
            oldList.Clear();
            for (int a = 0; a < mappingTermsList.Items.Count; a++)
            {
                printDebug("Backuped: " + mappingTermsList.Items[a].ToString());
                oldList.Add(mappingTermsList.Items[a].ToString());
            }
        }

        private void updateWindowStartingWithSoureTerms() // Load all lists, starting from the source terms list on the left
        {
            disableEventHandlers();
            backupListPositionAndName(sourceTermsList, 0, 0);
            backupListPositionAndName(mappingTermsList, 0, 1);
            loadSourceTerms();
            restoreListByName(sourceTermsList, 0);
            loadMappings();
            restoreListByName(mappingTermsList, 1);
            enableEventHandlers();
            aktualisierenBtn.Visible = false;
        }

        private void updateWindowStartingWithMappings() // Load all lists, starting from the source terms list on the left
        {
            disableEventHandlers();
            backupListPositionAndName(sourceTermsList, 0, 0);
            backupListPositionAndName(mappingTermsList, 0, 1);
            restoreListByName(sourceTermsList, 0);
            loadMappings();
            restoreListByName(mappingTermsList, 1);
            enableEventHandlers();
        }

        private void showAbove_SelectedValueChanged(object sender, EventArgs e) // User has selected another filter option
        {
            disableEventHandlers();
            printDebug("calling " + System.Reflection.MethodBase.GetCurrentMethod().Name + "()");
            setFilterFromLevel();
            updateWindowStartingWithSoureTerms();
            enableEventHandlers();
        }

        private void setFilterFromLevel()
        {
            printDebug("calling " + System.Reflection.MethodBase.GetCurrentMethod().Name + "()");

            startWorking();
            try
            {
                var cmd = new NpgsqlCommand(
                    "SELECT level FROM expertlevel WHERE description = '" +
                    showAbove.SelectedItem.ToString() + "'"
                    , dbConnection1);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    filterFromLevel = reader.GetInt32(0);
                }
                reader.Close();

            }
            catch (Exception msg)
            {
                MessageBox.Show(msg.ToString(), "MIRACUM Mapper: ERROR");
                throw;
            }

            if (showAbove.SelectedIndex > showBelow.SelectedIndex) showBelow.SelectedIndex = showAbove.SelectedIndex;
            stopWorking();
        }

        private void showBelow_SelectedValueChanged(object sender, EventArgs e) // User has selected another filter option
        {
            printDebug("calling " + System.Reflection.MethodBase.GetCurrentMethod().Name + "()");
            disableEventHandlers();
            setFilterToLevel();
            updateWindowStartingWithSoureTerms();
            enableEventHandlers();
        }

        private void setFilterToLevel()
        {
            printDebug("calling " + System.Reflection.MethodBase.GetCurrentMethod().Name + "()");
            startWorking();
            try
            {
                var cmd = new NpgsqlCommand("SELECT level FROM expertlevel WHERE description = '" + showBelow.SelectedItem.ToString() + "'", dbConnection1);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    filterToLevel = reader.GetInt32(0);
                }
                reader.Close();
            }
            catch (Exception msg)
            {
                MessageBox.Show(msg.ToString(), "MIRACUM Mapper: ERROR");
                throw;

            }
            stopWorking();
            if (showAbove.SelectedIndex > showBelow.SelectedIndex) showAbove.SelectedIndex = showBelow.SelectedIndex;
        }

        private void addCommentButton_Click(object sender, EventArgs e) // User has clicked on the "add comment" button
        {
            printDebug("calling " + System.Reflection.MethodBase.GetCurrentMethod().Name + "()");
            documentationText.Enabled = true;
            documentationText.ReadOnly = false;
            addDocumentation("");
            setUnsavedChanges(1);
        }

        private void addDocumentation(string text)
        {
            printDebug("calling " + System.Reflection.MethodBase.GetCurrentMethod().Name + "()");
            documentationText.ReadOnly = false;
            String timeStamp = "\n\n" + (DateTime.Now).ToString("yyyy-MM-dd HH:mm") + " " + userLoggedIn + ": " + text;
            timeStamp = timeStamp.Replace("MEDADS\\", "");
            documentationText.AppendText(timeStamp);
            if (documentationText.Text.StartsWith("\n\n")) documentationText.Text = documentationText.Text.Substring(2, documentationText.Text.Length - 2);
            cleanDocumentation();
            documentationText.Focus();
            documentationText.SelectionStart = documentationText.Text.Length;

            if (removeMappingBtn.Text.Equals("Wiederherstellen")) { removeMappingBtn.Text = "Speichern"; }
            if (removeMappingBtn.Text.Equals("Restore")) { removeMappingBtn.Text = "Save"; }

        }

        private void documentation_Leave(object sender, EventArgs e) // User has left the documentation field, clean up the text
        {
            cleanDocumentation();
        }

        private void cleanDocumentation() // Clean up documentation text
        {
            string lastText = "";
            while (!documentationText.Text.Equals(lastText))
            {
                lastText = documentationText.Text;
                documentationText.Text = documentationText.Text.Replace("\n\n\n", "\n\n");
                documentationText.Text = documentationText.Text.Replace("'", "´");
            }
        }

        private void resetTerminologyPart(Boolean keepTargeCode) // Wipe the right part of the window
        {
            printDebug("calling " + System.Reflection.MethodBase.GetCurrentMethod().Name + "()");
            disableEventHandlers();

            TermInfo.Text = "";
            if (!keepTargeCode) targetCode.Text = "";
            secondarySourceCode.Text = "";
            secondarySourceCodeCondition.Text = "";
            documentationText.Text = "";
            mappingVersion.Text = "";
            currentLevelIndicator.Text = "";

            enableEventHandlers();
        }

        Boolean isDeleted = false;
        private void loadTerminologyPart(string sCode, string tCode, int requestVersion, Boolean keepTargetCode) // Load the right part of the window
        {
            printDebug("calling " + System.Reflection.MethodBase.GetCurrentMethod().Name + "() #############");

            disableEventHandlers();

            if (tCode.StartsWith("(X) "))
            {
                tCode = tCode.Replace("(X) ", "");
            }

            startWorking();

            if (!keepTargetCode)
            {
                disableInputFields();
            }

            if (targetCode.Text != null && !targetCode.Text.Trim().Equals("Neues Mapping"))
            {
                saveMappingBtn.Visible = true;
                sendToAccept.Visible = true;
                nextZustLabel.Visible = true;
                noMappingBtn.Visible = false;
            }

            removeMappingBtn.Visible = false; // Assume there is nothing to be restored/deleted until we really get something from the DB

            try
            {
                NpgsqlCommand cmd;
                String sql = "";
                if (requestVersion == -1)
                {
                    sql = "select version, source_code, target_code, sec_source_code, sec_source_code_cond, documentation, mapping_level, deleted "
                         + "  from ("
                         + "      SELECT max(version) as version, source_code, target_code, sec_source_code, sec_source_code_cond, documentation, mapping_level, deleted "
                         + "      FROM mapping where source_code = '" + sCode + "' AND target_code = '" + tCode + "'"
                         + "      GROUP by source_code, target_code, sec_source_code, sec_source_code_cond, documentation, mapping_level, deleted"
                         + "  ) as A"
                         + "  where version in ("
                         + "      SELECT max(version) "
                         + "      FROM mapping "
                         + "      where source_code = '" + sCode + "' AND target_code = '" + tCode + "')";
                    cmd = new NpgsqlCommand(sql, dbConnection1);
                }
                else
                {
                    sql = "select version, source_code, target_code, sec_source_code, sec_source_code_cond, documentation, mapping_level, deleted "
                         + "  from ("
                         + "      SELECT version, source_code, target_code, sec_source_code, sec_source_code_cond, documentation, mapping_level, deleted "
                         + "      FROM mapping where source_code = '" + sCode + "' AND target_code = '" + tCode + "'"
                         + "  ) as A"
                         + "  where version = '" + requestVersion + "'";

                    cmd = new NpgsqlCommand(sql, dbConnection1);
                }

                NpgsqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    sendToAccept.Items.Clear();
                    hideSaveDeleteButtons();
                }

                if (reader.HasRows) Console.WriteLine("reader.HasRows");
                if (mappingTermsList.Items.Count > 0) Console.WriteLine("mappingTermsList.Items.Count > 0");
                if (requestVersion == -1) Console.WriteLine("requestVersion == -1");

                while (reader.Read())
                {
                    enableInputFields();

                    int version = Int32.Parse(reader["version"].ToString());
                    int deleted = Int32.Parse(reader["deleted"].ToString());

                    string source_code = reader["source_code"].ToString();
                    string target_code = reader["target_code"].ToString();
                    string sec_source_code = reader["sec_source_code"].ToString();
                    string sec_source_code_cond = reader["sec_source_code_cond"].ToString();
                    string documentation = reader["documentation"].ToString();
                    string mapping_level = reader["mapping_level"].ToString();

                    if (!keepTargetCode) targetCode.Text = target_code;
                    targetCode.SelectionStart = targetCode.TextLength;

                    secondarySourceCode.Text = sec_source_code;
                    secondarySourceCodeCondition.Text = sec_source_code_cond;
                    documentationText.Text = documentation;

                    mappingVersion.Text = "" + version;
                    currentLevelIndicator.Text = showAbove.Items[Int32.Parse(mapping_level)].ToString();

                    userRejectTo = Int32.Parse(mapping_level);

                    sendToAccept.Items.Clear();
                    loadTransitions(Int32.Parse(mapping_level));

                    if (requestVersion == -1 && deleted == 0)
                    {
                        saveMappingBtn.Visible = true;
                        sendToAccept.Visible = true;
                        nextZustLabel.Visible = true;
                    }
                    if (deleted == 1)
                    {
                        removeMappingBtn.Text = "Wiederherstellen";
                        if (!CultureInfo.CurrentCulture.Name.Equals("de-DE")) removeMappingBtn.Text = "Restore";
                        isDeleted = true;
                        saveMappingBtn.Visible = false;
                    }
                    else
                    {
                        removeMappingBtn.Text = "Löschen";
                        if (!CultureInfo.CurrentCulture.Name.Equals("de-DE")) removeMappingBtn.Text = "Delete";
                        isDeleted = false;
                        saveMappingBtn.Visible = true;
                    }
                    removeMappingBtn.Visible = true;
                }
                reader.Close();

            }
            catch (Exception msg)
            {
                MessageBox.Show(msg.ToString(), "MIRACUM Mapper: ERROR");
                throw;
            }

            setUnsavedChanges(0);
            documentationText.ReadOnly = true;
            enableEventHandlers();
            stopWorking();
        }

        int eventHandlersDisabled = 0;

        private void enableEventHandlers()  // Temporarily disable the event handlers (e.g. those that fire when another list item is selected)
        {
            //printDebug("calling " + System.Reflection.MethodBase.GetCurrentMethod().Name + "()");
            if (eventHandlersDisabled > 0) eventHandlersDisabled--;
            printDebug("eventHandlersDisabled=" + eventHandlersDisabled);
        }

        private void disableEventHandlers() // Re-enable the event handlers (e.g. those that fire when another list item is selected)
        {
            //printDebug("calling " + System.Reflection.MethodBase.GetCurrentMethod().Name + "()");
            eventHandlersDisabled++;
            printDebug("eventHandlersDisabled=" + eventHandlersDisabled);
        }

        private void hideSaveDeleteButtons() // Hide the "save" and "delete" buttons
        {
            saveMappingBtn.Visible = false;
            sendToAccept.Visible = false;
            nextZustLabel.Visible = false;
            removeMappingBtn.Visible = false;
        }

        public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                //Convert the byte array to hexadecimal string prior to .NET 5
                StringBuilder sb = new System.Text.StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

        string userLoggedIn = "";
        private void logInButton_Click(object sender, EventArgs e) // User clicked on the "login" button
        {

            printDebug("calling " + System.Reflection.MethodBase.GetCurrentMethod().Name + "()");
            LoginForm lf = new LoginForm();
            string pass = "";

            if (lf.ShowDialog(this) == DialogResult.OK)
            {
                userLoggedIn = lf.user.Text;
                pass = lf.password.Text;
            }

            lf.Dispose();
            bool isValid = false;
            string local_password = "";
            log("DB schema version: " + schemaVersion);
            if (schemaVersion >= 2)
            {
                string userid = lf.user.Text;
                var cmd = new NpgsqlCommand();
                cmd.CommandText = "select local_password_md5 from userids where userid = @userid";
                cmd.Parameters.AddWithValue("userid", userid);
                cmd.Connection = dbConnection1;
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    local_password = reader["local_password_md5"].ToString();
                    log("Found a local user password to be used for authentication.");
                }
                reader.Close();
            }

            if (local_password.Equals(""))
            {
                // LDAP-Authentifizerung
                try
                {
                    PrincipalContext pc = new PrincipalContext(ContextType.Domain, activeDirectoryServer);
                    isValid = pc.ValidateCredentials(userLoggedIn, pass);
                }
                catch (Exception msg)
                {
                    MessageBox.Show(msg.ToString(), "MIRACUM Mapper: ERROR");
                    throw;
                }

                if (isValid)
                {
                    log("Valid login from " + userLoggedIn + " (correct LDAP password).");
                    authenticate(userLoggedIn);
                }
                else
                {
                    log("Bad login from " + userLoggedIn + " (bad LDAP password).");
                    MessageBox.Show("Falsches Login!");
                }
            }
            else
            { // Lokale Passwort-Authentifizerung
                if (local_password.ToUpper().Equals(CreateMD5(pass).ToUpper()))
                {
                    isValid = true;
                }

                if (isValid)
                {
                    log("Valid login from " + userLoggedIn + " (correct local password).");
                    authenticate(userLoggedIn);
                }
                else
                {
                    log("Bad login from " + userLoggedIn + " (bad local password).");
                    MessageBox.Show("Falsches Login!");
                }
            }
            //isValid = true;
        }

        private void sourceFilter_KeyDown(object sender, KeyEventArgs e) // User is entering text into the filter field
        {
            printDebug("calling " + System.Reflection.MethodBase.GetCurrentMethod().Name + "()");
            if (e.KeyCode == Keys.Enter)
            {
                triggerSearch();
            }
        }

        private void triggerSearch()
        {
            disableEventHandlers();
            mappingTermsList.Items.Clear();
            targetCode.Text = "";
            secondarySourceCode.Text = "";
            secondarySourceCodeCondition.Text = "";
            documentationText.Text = "";

            updateWindowStartingWithSoureTerms();

            if (sourceTermsList.Items.Count > 0)
            {
                enableEventHandlers();
            }
            enableEventHandlers();
        }

        private void previousVersion_Click(object sender, EventArgs e) // User wants to view the previous version of the mapping
        {
            printDebug("calling " + System.Reflection.MethodBase.GetCurrentMethod().Name + "()");
            loadTerminologyPart(sourceTermsList.SelectedItem.ToString(), mappingTermsList.SelectedItem.ToString(), (Int32.Parse(mappingVersion.Text) - 1), false);
        }

        private void nextVersion_Click(object sender, EventArgs e) // User wants to view the next version of the mapping
        {
            printDebug("calling " + System.Reflection.MethodBase.GetCurrentMethod().Name + "()");
            loadTerminologyPart(sourceTermsList.SelectedItem.ToString(), mappingTermsList.SelectedItem.ToString(), (Int32.Parse(mappingVersion.Text) + 1), false);
        }

        private void visualizeBtn_Click(object sender, EventArgs e) // User wants to open the LabVisualizer
        {
            printDebug("calling " + System.Reflection.MethodBase.GetCurrentMethod().Name + "()");
            wb.Show();
            if (sourceTermsList.SelectedItem != null) wb.showUrl(visualizerUrl + sourceTermsList.SelectedItem.ToString());
        }

        private void mapperForm_FormClosed(object sender, FormClosedEventArgs e) // User has closed the main window
        {
            printDebug("calling " + System.Reflection.MethodBase.GetCurrentMethod().Name + "()");
            terminate();
        }

        private void loadTargetCodeDescription() // Check if the target code entered by the user is valid
        {
            printDebug("calling " + System.Reflection.MethodBase.GetCurrentMethod().Name + "() #############");
            startWorking();
            try
            {
                if (targetCode.Text.Equals(""))
                {
                    TermInfo.Text = "";
                }
                else if (targetCode.Text.Equals("Kein Mapping"))
                {
                    if (CultureInfo.CurrentCulture.Name.Equals("de-DE"))
                    {
                        TermInfo.Text = "Der Code soll nicht gemappt werden.";
                    }
                    else
                    {
                        TermInfo.Text = "The code should not be mapped.";
                    }
                }
                else if (targetCode.Text.Equals("Neues Mapping"))
                {
                    if (CultureInfo.CurrentCulture.Name.Equals("de-DE"))
                    {
                        TermInfo.Text = "Bitte den Code oben eingeben, oder:";
                    }
                    else
                    {
                        TermInfo.Text = "Please enter the code above, or:";
                    }
                    noMappingBtn.Visible = true;
                }
                else
                {
                    TermInfo.Text = "Unbekannter Code!";
                }

                var cmd = new NpgsqlCommand();
                cmd.CommandText = "SELECT description FROM terminfo where code = @targetCode";
                cmd.Parameters.AddWithValue("targetCode", targetCode.Text.Trim());
                cmd.Connection = dbConnection1;
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string description = reader["description"].ToString();
                    TermInfo.Text = description;
                }
                reader.Close();

            }
            catch (Exception msg)
            {
                //Console.WriteLine(msg.ToString());
                MessageBox.Show(msg.ToString(), "MIRACUM Mapper: ERROR");
                throw;
            }
            //loadTransitions();
            stopWorking();
        }

        private void setAcceptLevel() // Set the expert level the mapping is going to be saved by default
        {
            printDebug("calling " + System.Reflection.MethodBase.GetCurrentMethod().Name + "()");
            startWorking();
            try
            {
                var cmd = new NpgsqlCommand("SELECT level FROM expertlevel WHERE description = '" + sendToAccept.SelectedItem.ToString() + "'", dbConnection1);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    userAcceptTo = reader.GetInt32(0);
                }
                reader.Close();

            }
            catch (Exception msg)
            {
                MessageBox.Show(msg.ToString(), "MIRACUM Mapper: ERROR");
                throw;

            }
            stopWorking();
            saveMappingBtn.Text = "=> " + sendToAccept.Text;
        }

        private void sendToAccept_SelectedValueChanged(object sender, EventArgs e) // User has selected another "next mapping level"
        {
            printDebug("calling " + System.Reflection.MethodBase.GetCurrentMethod().Name + "()");
            setAcceptLevel();
        }

        private void loincSearchBtn_Click(object sender, EventArgs e) // User has clicked on the "search" button
        {
            System.Diagnostics.Process.Start("https://s.details.loinc.org/LOINC/" + targetCode.Text + ".html?sections=Comprehensive");
        }

        private void showDeleted_CheckedChanged(object sender, EventArgs e) // User wants to view deleted mappings
        {
            updateWindowStartingWithMappings();
        }

        private void showAllMappings_CheckedChanged(object sender, EventArgs e) // User wants to view all mappings
        {
            updateWindowStartingWithMappings();
        }

        private void setUnsavedChanges(int status) // User has changed something, warn about unsaved changes
        {
            unsavedChanges = status;
            if (status == 0)
            {
                previousVersionBtn.Enabled = true;
                nextVersionBtn.Enabled = true;
                blockPanel.Visible = false;
                stopWorking();
            }
            else
            {
                WorkingStatus.Text = "Nicht gespeicherte Änderungen";
                if (!CultureInfo.CurrentCulture.Name.Equals("de-DE")) WorkingStatus.Text = "Unsaved Changes";
                WorkingStatus.BackColor = Color.Salmon;

                if (userNotified < 3 || alwaysNotify)
                {
                    blockPanel.Visible = true;
                }
                else
                {
                    blockPanel.Visible = false;
                }
                previousVersionBtn.Enabled = false;
                nextVersionBtn.Enabled = false;
            }
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            setUnsavedChanges(0);
            resetTerminologyPart(false);
            loadMappings();
        }

        private async void blinkButtons() // Flash the save/delete buttons
        {
            for (int a = 0; a < 3; a++)
            {
                await Task.Delay(75);
                removeMappingBtn.BackColor = Color.Red;
                saveMappingBtn.BackColor = Color.Green;
                await Task.Delay(75);
                removeMappingBtn.BackColor = Color.Pink;
                saveMappingBtn.BackColor = Color.LightGreen;
            }
        }

        private void blockPanel_Click_1(object sender, EventArgs e) // User has clicked on the invisible blocking panel
        {
            Console.WriteLine("userNotified=" + userNotified + " alwaysNotify=" + alwaysNotify);
            if (userNotified < 3 || alwaysNotify)
            {
                DialogResult dialogResult;

                if (CultureInfo.CurrentCulture.Name.Equals("de-DE"))
                {
                    dialogResult = MessageBox.Show("Sie haben das Mapping bearbeitet, die Änderungen aber noch nicht übernommen. Änderungen verwerfen?", "Änderungen verwerfen?", MessageBoxButtons.YesNo);
                }
                else
                {
                    dialogResult = MessageBox.Show("You have edited the mapping, but have not yet accepted the changes. Discard changes?", "Discard changes?", MessageBoxButtons.YesNo);
                }

                userNotified++;

                if (dialogResult == DialogResult.Yes)
                {
                    noMappingBtn.Visible = false;
                    setUnsavedChanges(0);
                    blockPanel.Visible = false;
                    resetTerminologyPart(false);
                    loadMappings();
                }
                else
                {
                    blinkButtons();
                    if (CultureInfo.CurrentCulture.Name.Equals("de-DE")) MessageBox.Show("Sie können die Änderungen mit dem roten oder grünen Button unten rechts im Fenster übernehmen.", "Info");
                    if (!CultureInfo.CurrentCulture.Name.Equals("de-DE")) MessageBox.Show("You can accept the changes with the red or green button at the bottom right of the window.", "Info");
                }

                if (userNotified == 3)
                {
                    DialogResult dialogResult2;

                    if (CultureInfo.CurrentCulture.Name.Equals("de-DE"))
                    {
                        dialogResult2 = MessageBox.Show("Möchten Sie zukünftig auf nicht übernommene Änderungen hingewiesen werden?", "Auf nicht übernommene Änderungen hinweisen?", MessageBoxButtons.YesNo);
                    }
                    else
                    {
                        dialogResult2 = MessageBox.Show("Would you like to be informed in the future about changes that have not been accepted?", "Warn about changes that have not been accepted?", MessageBoxButtons.YesNo);
                    }

                    if (dialogResult2 == DialogResult.Yes)
                    {
                        alwaysNotify = true;
                    }
                    else
                    {
                        alwaysNotify = false;
                        blockPanel.Visible = false;
                    }
                }
            }
        }

        private void targetCode_KeyUp_1(object sender, KeyEventArgs e) // User has entered something into the target code field
        {
            printDebug("calling " + System.Reflection.MethodBase.GetCurrentMethod().Name + "()");
            handleTargetCodeUpdateEvent();
        }

        private void handleTargetCodeUpdateEvent()
        {
            this.mappingTermsList.SelectedIndexChanged -= new System.EventHandler(this.mappingTermsList_SelectedIndexChanged);

            if (!lastTargetCode.Equals(targetCode.Text))
            {
                resetTerminologyPart(true);
                loadTargetCodeDescription();
                loadTransitions(-1);
                setUnsavedChanges(1);
            }

            if (mappingVersion.Text.Equals("") || mappingVersion.Text is null) mappingVersion.Text = "0";

            if (targetCode.Text != null && !targetCode.Text.Trim().Equals(""))
            {
                if (!lastTargetCode.Equals(targetCode.Text))
                {
                    loadTerminologyPart(sourceTermsList.SelectedItem.ToString(), targetCode.Text.Trim(), -1, true);
                }

                Boolean doNotRemove = false;
                Boolean doNotAdd = false;

                // Restore original list:
                mappingTermsList.Items.Clear();
                for (int a = 0; a < oldList.Count; a++)
                {
                    printDebug(oldList[a].ToString());
                    mappingTermsList.Items.Add(oldList[a].ToString());
                    if (oldList[a].ToString().Equals(lastTargetCode.Trim())) doNotRemove = true; // Do not remove entry if it's in the original mappings list.
                }

                // Remove the old targetCode.Text (before keypress):
                for (int n = mappingTermsList.Items.Count - 1; n >= 0; --n)
                {
                    string removelistitem = lastTargetCode;
                    if (mappingTermsList.Items[n].ToString().Equals(removelistitem))
                    {
                        if (!doNotRemove) mappingTermsList.Items.RemoveAt(n);
                    }
                    if (mappingTermsList.Items[n].ToString().Equals("(X) " + targetCode.Text.Trim()) ||
                        mappingTermsList.Items[n].ToString().Equals(targetCode.Text.Trim()))
                    {
                        mappingTermsList.SelectedIndex = n;
                        doNotAdd = true;
                    }
                }

                if (!doNotAdd)
                {
                    mappingTermsList.Items.Add(targetCode.Text.Trim());
                    mappingTermsList.SelectedIndex = mappingTermsList.Items.Count - 1;
                }
                editedMapping = targetCode.Text;
            }
            else
            {
                hideSaveDeleteButtons();
            }

            if (!lastTargetCode.Equals(targetCode.Text))
            {
                setUnsavedChanges(1);
            }

            this.mappingTermsList.SelectedIndexChanged += new System.EventHandler(this.mappingTermsList_SelectedIndexChanged);
            lastTargetCode = targetCode.Text;
        }

        String lastTargetCode = "";
        private List<String> oldList = new List<String>();

        private void targetCode_KeyDown_1(object sender, KeyEventArgs e)
        {
            lastTargetCode = targetCode.Text;
        }

        private void mappingTermsList_SelectedIndexChanged(object sender, EventArgs e) // USer has selected another mapping in the middle column
        {
            if (eventHandlersDisabled > 0) return;
            disableInputFields();

            if (alwaysNotify == false && unsavedChanges > 0 && userNotified >= 3)
            {
                string removelistitem = lastTargetCode;
                for (int n = mappingTermsList.Items.Count - 1; n >= 0; --n)
                {
                    if (mappingTermsList.Items[n].ToString().Equals(removelistitem) ||
                        mappingTermsList.Items[n].ToString().Equals("Neues Mapping"))
                    {
                        if (!oldList.Contains(removelistitem)) mappingTermsList.Items.RemoveAt(n);
                        setUnsavedChanges(0);
                    }
                }
            }

            printDebug("calling " + System.Reflection.MethodBase.GetCurrentMethod().Name + "()");
            if (mappingTermsList.SelectedItem != null && sourceTermsList.SelectedItem != null)
            {
                resetTerminologyPart(false);
                loadTerminologyPart(sourceTermsList.SelectedItem.ToString(), mappingTermsList.SelectedItem.ToString(), -1, false);
                loadTargetCodeDescription();
                enableInputFields();

            }
            lastSelectedMapping = mappingTermsList.SelectedIndex;
        }

        private void targetCode_Leave(object sender, EventArgs e) // User has left the target code field, clean the code
        {
            targetCode.Text = targetCode.Text.Trim();
            targetCode.SelectionStart = targetCode.TextLength;
        }

        private void targetCode_TextChanged(object sender, EventArgs e) // User has modified the target code field, notify about unsaved changes
        {
            targetCode.Text = sanitizeInput(targetCode.Text);
            if (eventHandlersDisabled > 0) return;
            setUnsavedChanges(1);
        }

        private void documentationText_TextChanged(object sender, EventArgs e) // User has modified the documentation field, notify about unsaved changes
        {
            //documentationText.Text = sanitizeInput(documentationText.Text);
            if (eventHandlersDisabled > 0) return;
            setUnsavedChanges(1);
        }

        private void secondarySourceCode_TextChanged(object sender, EventArgs e) // User has modified the secondary code field, notify about unsaved changes
        {
            secondarySourceCode.Text = sanitizeInput(secondarySourceCode.Text);
            if (eventHandlersDisabled > 0) return;
            setUnsavedChanges(1);
        }

        private void secondarySourceCodeCondition_TextChanged(object sender, EventArgs e) // User has modified the condition field, notify about unsaved changes
        {
            secondarySourceCodeCondition.Text = sanitizeInput(secondarySourceCodeCondition.Text);
            if (eventHandlersDisabled > 0) return;
            setUnsavedChanges(1);
        }

        private void sourceFilter_TextChanged(object sender, EventArgs e)
        {
            showOrHideSearchCheckboxes();
        }

        private void showOrHideSearchCheckboxes()
        {
            sourceFilter.Text = sanitizeInput(sourceFilter.Text);

            if (!sourceFilter.Text.Equals(""))
            {
                searchDoku.Visible = true;
                searchInfo.Visible = true;
                searchSource.Visible = true;
                searchTarget.Visible = true;
                label1.TextAlign = ContentAlignment.MiddleLeft;
                label2.TextAlign = ContentAlignment.MiddleLeft;
                if (documentationText.Enabled) searchDoku.BackColor = SystemColors.Window;
                if (!documentationText.Enabled) searchDoku.BackColor = SystemColors.Control;
            }
            else
            {
                searchDoku.Visible = false;
                searchInfo.Visible = false;
                searchSource.Visible = false;
                searchTarget.Visible = false;
                label1.TextAlign = ContentAlignment.MiddleCenter;
                label2.TextAlign = ContentAlignment.MiddleCenter;
            }
        }

        private string sanitizeInput(string text)
        {
            String returnText = text.Replace("--", "").Replace("/*", "").Replace("*/", "").Replace("\"", "").Replace("'", "").Replace(";", "");
            if (!text.Equals(returnText))
            {
                MessageBox.Show("Ungültige Eingabe erkannt!", "MIRACUM Mapper");
            }
            return returnText;
        }

        private void TermInfo_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(e.LinkText);
        }

        private void documentationText_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(e.LinkText);
        }

        private void searchSource_CheckedChanged(object sender, EventArgs e)
        {
            checkSearchFields();
            triggerSearch();
        }

        private void checkSearchFields()
        {
            // We need at least one checkbox checked:
            if (!searchDoku.Checked && !searchInfo.Checked && !searchSource.Checked && !searchTarget.Checked)
            {
                searchSource.Checked = true;
            }
        }

        private void searchInfo_CheckedChanged(object sender, EventArgs e)
        {
            checkSearchFields();
            triggerSearch();
        }

        private void searchTarget_CheckedChanged(object sender, EventArgs e)
        {
            checkSearchFields();
            triggerSearch();
        }

        private void searchDoku_CheckedChanged(object sender, EventArgs e)
        {
            checkSearchFields();
            triggerSearch();
        }

        private void mapperForm_MouseClick(object sender, MouseEventArgs e)
        {
            searchDoku.Visible = false;
            searchInfo.Visible = false;
            searchSource.Visible = false;
            searchTarget.Visible = false;
            label1.TextAlign = ContentAlignment.MiddleCenter;
            label2.TextAlign = ContentAlignment.MiddleCenter;
            if (documentationText.Enabled) searchDoku.BackColor = SystemColors.Window;
            if (!documentationText.Enabled) searchDoku.BackColor = SystemColors.Control;
        }

        private void sourceFilter_MouseClick(object sender, MouseEventArgs e)
        {
            showOrHideSearchCheckboxes();
        }

        private void sourceTermsList_SelectedValueChanged(object sender, EventArgs e)
        {
            checkDisableNewMapping();
        }

        private void noMappingBtn_Click(object sender, EventArgs e)
        {
            targetCode.Text = "Kein Mapping";
            noMappingBtn.Visible = false;
            handleTargetCodeUpdateEvent();
        }

        private void aktualisierenBtn_Click(object sender, EventArgs e)
        {
            loadSourceTerms();
            aktualisierenBtn.Visible = false;
        }
    }

    public class ListboxItem
    {
        public string Text { get; set; }
        public object Value { get; set; }
        public override string ToString()
        {
            return Text;
        }
    }
}
