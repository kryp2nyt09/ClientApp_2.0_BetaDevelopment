﻿using CMS2.BusinessLogic;
using CMS2.Client.SyncHelper;
using CMS2.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using Telerik.WinControls;
using CMS2.Client.Properties;
using System.Configuration;
using CMS2.Client;
using CMS2.DataAccess;
using System.Threading;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Core.Metadata.Edm;
using System.ServiceProcess;
using CMS2.Common;

namespace CMS2_Client
{
    public partial class Extract_Database : Telerik.WinControls.UI.RadForm
    {

        #region Properties
        private bool isSubServer { get; set; }
        private bool isLocalConnected { get; set; }
        private bool isMainConnected { get; set; }
        private string _localServer { get; set; }
        private string _localDbName { get; set; }
        private string _localUsername { get; set; }
        private string _localPassword { get; set; }

        private string _mainServer { get; set; }
        private string _mainDbName { get; set; }
        private string _mainUsername { get; set; }
        private string _mainPassword { get; set; }

        private string _localConnectionString { get; set; }
        private string _mainConnectionString { get; set; }

        private string _filter { get; set; }

        public string _branchCorpOfficeId { get; set; }

        private BranchCorpOfficeBL _bcoService;

        private Synchronization _synchronization;

        private List<BranchCorpOffice> _branchCorpOffices;

        private List<SyncTables> _entities;

        #endregion

        #region Constructor
        public Extract_Database()
        {
            InitializeComponent();
        }
        #endregion

        #region Events
        private void Extract_Database_Load(object sender, EventArgs e)
        {
            this.isSubServer = true;
            this.isLocalConnected = false;
            this.isMainConnected = false;
            this.testMainConnection.Visible = false;
            this.testLocalConnection.Visible = false;
            this.dboBranchCoprOffice.Enabled = false;
            this.SetEntities();
            this.radProgressBar1.Value1 = 0;
            this.radProgressBar1.Maximum = _entities.Count + 5;
            this.lblProgressState.Text = "";
        }
        private void LocalTestConnection_Click(object sender, EventArgs e)
        {
            if (IsDataValid_Local())
            {
                GatherInputs();
                if (isSubServer)
                {
                    _localConnectionString = String.Format("Server={0};Database={1};User Id={2};Password={3};Connect Timeout=180;Connection Lifetime=0;Pooling=true;", _localServer, "master", _localUsername, _localPassword);
                }
                else
                {
                    _localConnectionString = String.Format("Server={0};Database={1};User Id={2};Password={3};Connect Timeout=180;Connection Lifetime=0;Pooling=true;", _localServer, _localDbName, _localUsername, _localPassword);

                }

                SqlConnection localConnection = new SqlConnection(_localConnectionString);
                try
                {
                    localConnection.Open();
                    isLocalConnected = true;
                    testLocalConnection.Text = "Success";
                    testLocalConnection.Visible = true;
                    testLocalConnection.ForeColor = Color.Green;

                }
                catch (Exception)
                {
                    isLocalConnected = false;
                    testLocalConnection.Text = "Failed";
                    testLocalConnection.Visible = true;
                    testLocalConnection.ForeColor = Color.Red;
                }
                finally
                {
                    localConnection.Dispose();
                }
            }
        }
        private void MainTestConnection_Click(object sender, EventArgs e)
        {
            if (IsDataValid_Main())
            {
                GatherInputs();
                _mainConnectionString = String.Format("Server={0};Database={1};User Id={2};Password={3};", _mainServer, _mainDbName, _mainUsername, _mainPassword);
                SqlConnection mainConnection = new SqlConnection(_mainConnectionString);
                try
                {
                    mainConnection.Open();
                    isMainConnected = true;
                    testMainConnection.Text = "Success";
                    testMainConnection.Visible = true;
                    testMainConnection.ForeColor = Color.Green;
                    loadbranchCorp(_mainConnectionString);
                    dboBranchCoprOffice.Enabled = true;
                }
                catch (Exception)
                {
                    isMainConnected = false;
                    testMainConnection.Text = "Failed";
                    testMainConnection.Visible = true;
                    testMainConnection.ForeColor = Color.Red;
                }
                finally
                {
                    mainConnection.Dispose();
                }
            }
        }
        private void SubServer_ToggleStateChanged(object sender, Telerik.WinControls.UI.StateChangedEventArgs args)
        {
            ResetAll();
            ToggleEnableDisableMainServer(true);
            isSubServer = true;
        }
        private void ClientApp_ToggleStateChanged(object sender, Telerik.WinControls.UI.StateChangedEventArgs args)
        {
            ResetAll();
            ToggleEnableDisableMainServer(false);
            isSubServer = false;
        }
        private void Extract_Click(object sender, EventArgs e, int i = 3000)
        {
            int index = dboBranchCoprOffice.SelectedItem.ToString().IndexOf(" ");
            _filter = dboBranchCoprOffice.SelectedItem.ToString().Substring(0, index);
            _branchCorpOfficeId = dboBranchCoprOffice.SelectedValue.ToString();
            if (isSubServer && isLocalConnected && isMainConnected)
            {
                SetEntities();
                Worker.RunWorkerAsync();
            }
            else if (!isSubServer && isLocalConnected)
            {
                WriteToConfig(_localConnectionString.Replace("master", _localDbName));
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private void Extract_Work(object sender, DoWorkEventArgs e)
        {
            DropDatabaseIfExist();
            CreateDatabase();

            //Provisioning Server and Local for synchronization
            radProgressBar1.Value1 = 0;
            StartProvision();

            //synchronizing databases
            radProgressBar1.Value1 = 0;
            StartSynchronization(Worker);

            WriteToConfig(_localConnectionString.Replace("master", _localDbName));
            WriteToConfig(_mainConnectionString);
            SaveSyncServiceSettings();
        }
        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            radProgressBar1.Value1 += e.ProgressPercentage;
            lblProgressState.Text = e.UserState.ToString();
        }
        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            lblProgressState.Text = "Completed.";
            radProgressBar1.Value1 = radProgressBar1.Maximum;
            System.Threading.Thread.Sleep(3000);
        }
        #endregion

        #region Methods
        private void ToggleEnableDisableMainServer(bool Flag)
        {
            MainServer.Enabled = Flag;
            MainDbName.Enabled = Flag;
            MainUsername.Enabled = Flag;
            MainPassword.Enabled = Flag;
            MainTestConnection.Enabled = Flag;
        }
        private void WriteToConfig(string connString)
        {
            Worker.ReportProgress(0, "Saving application settings.");
            var appConfigDoc = new XmlDocument();
            appConfigDoc.Load(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);
            foreach (XmlElement xElement in appConfigDoc.DocumentElement)
            {
                if (xElement.Name == "connectionStrings")
                {
                    var nodes = xElement.ChildNodes;
                    foreach (XmlElement item in nodes)
                    {
                        if (item.Attributes["name"].Value.Equals("Cms"))
                        {
                            item.Attributes["connectionString"].Value = connString;
                            break;
                        }
                    }
                    break;
                }
            }
            appConfigDoc.Save(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);

            Settings.Default.IsSynchronizationSetup = true;
            Settings.Default.LocalDbServer = _localServer;
            Settings.Default.LocalDbName = _localDbName;
            Settings.Default.LocalDbUsername = _localUsername;
            Settings.Default.LocalDbPassword = _localPassword;
            Settings.Default.CentralServerIp = _mainServer;
            Settings.Default.CentralDbName = _mainDbName;
            Settings.Default.CentralUsername = _mainUsername;
            Settings.Default.CentralPassword = _mainPassword;
            Settings.Default.Filter = _filter;
            Settings.Default.DeviceBcoId = _branchCorpOfficeId;            
            Settings.Default.Save();

            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings["isSync"].Value = "true";
            config.AppSettings.Settings["Filter"].Value = _filter;
            config.AppSettings.Settings["BcoId"].Value = _branchCorpOfficeId;
            config.Save(ConfigurationSaveMode.Modified);
            Worker.ReportProgress(0, "Application settings was saved.");
        }
        private void GatherInputs()
        {
            _localServer = LocalServer.Text;
            _localDbName = LocalDbName.Text;
            _localUsername = LocalUsername.Text;
            _localPassword = LocalPassword.Text;

            _mainServer = MainServer.Text;
            _mainDbName = MainDbName.Text;
            _mainUsername = MainUsername.Text;
            _mainPassword = MainPassword.Text;
        }
        private void ResetAll()
        {
            _localServer = "";
            _localDbName = "";
            _localUsername = "";
            _localPassword = "";

            _mainServer = "";
            _mainDbName = "";
            _mainUsername = "";
            _mainPassword = "";

            LocalServer.Text = _localServer;
            LocalDbName.Text = _localDbName;
            LocalUsername.Text = _localUsername;
            LocalPassword.Text = _localPassword;

            MainServer.Text = _mainServer;
            MainDbName.Text = _mainDbName;
            MainUsername.Text = _mainUsername;
            MainPassword.Text = _mainPassword;

            testLocalConnection.Visible = false;
            testMainConnection.Visible = false;

            dboBranchCoprOffice.Enabled = false;
        }
        private void DropDatabaseIfExist()
        {
            using (SqlConnection connection = new SqlConnection(_localConnectionString))
            {
                using (SqlCommand command = new SqlCommand("Select COUNT(*) from master.dbo.sysdatabases where name = '" + _localDbName + "'", connection))
                {
                    try
                    {
                        connection.Open();
                        int count = (Int32)command.ExecuteScalar();

                        if (count == 1)
                        {
                            command.CommandText = "Use master alter database[" + _localDbName + "] set single_user with rollback immediate; DROP DATABASE [" + _localDbName + "]";
                            command.ExecuteNonQuery();
                            Worker.ReportProgress(0, "Database was dropped.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Worker.ReportProgress(0, "Database was unable to drop.");
                    }
                }
            }

        }
        private void CreateDatabase()
        {
            using (SqlConnection connection = new SqlConnection(_localConnectionString))
            {
                using (SqlCommand command = new SqlCommand("Create Database " + _localDbName, connection))
                {
                    try
                    {
                        connection.Open();
                        command.ExecuteNonQuery();
                        Worker.ReportProgress(0, "Database " + _localDbName + " was created.");
                    }
                    catch (Exception ex)
                    {
                        Worker.ReportProgress(0, "Unable to create database " + _localDbName + ".");
                    }
                }
            }

        }
        private void loadbranchCorp(string conString)
        {
            using (SqlConnection connectionString = new SqlConnection(conString))
            {
                try
                {
                    SqlCommand command = new SqlCommand("SELECT * FROM BranchCorpOffice ORDER BY BranchCorpOfficeName ASC", connectionString);
                    SqlDataAdapter dataAdapter = new SqlDataAdapter(command);
                    DataTable dt = new DataTable();
                    dataAdapter.Fill(dt);

                    dboBranchCoprOffice.ValueMember = "BranchCorpOfficeId";
                    dboBranchCoprOffice.DisplayMember = "BranchCorpOfficeName";
                    dboBranchCoprOffice.DataSource = dt;

                    connectionString.Close();
                }
                catch (Exception ex)
                {
                    // MessageBox.Show("Error occured!");
                }
            }
        }
        private bool IsDataValid_Local()
        {
            bool isValid = true;
            if (string.IsNullOrEmpty(LocalServer.Text) || string.IsNullOrEmpty(LocalDbName.Text) || string.IsNullOrEmpty(LocalUsername.Text) || string.IsNullOrEmpty(LocalPassword.Text))
            {
                MessageBox.Show("Please fill out all fields.", "Data Error", MessageBoxButtons.OK);
                isValid = false;
            }

            return isValid;
        }
        private bool IsDataValid_Main()
        {
            bool isValid = true;
            if (string.IsNullOrEmpty(MainServer.Text) || string.IsNullOrEmpty(MainDbName.Text) || string.IsNullOrEmpty(MainUsername.Text) || string.IsNullOrEmpty(MainPassword.Text))
            {
                MessageBox.Show("Please fill out all fields.", "Data Error", MessageBoxButtons.OK);
                isValid = false;
            }

            return isValid;
        }
        private void SetEntities()
        {

            using (CmsContext context = new CmsContext())
            {
                ObjectContext objContext = ((IObjectContextAdapter)context).ObjectContext;
                MetadataWorkspace workspace = objContext.MetadataWorkspace;


                IEnumerable<EntityType> tables = workspace.GetItems<EntityType>(DataSpace.SSpace);

                _entities = new List<SyncTables>();
                foreach (var item in tables)
                {
                    SyncTables table = new SyncTables();
                    table.TableName = item.Name;
                    _entities.Add(table);
                }
            }
        }
        private void StartSynchronization(BackgroundWorker worker)
        {
            List<CMS2.Client.SyncHelper.ThreadState> listOfThread = new List<CMS2.Client.SyncHelper.ThreadState>();
            List<ManualResetEvent> syncEvents = new List<ManualResetEvent>();
            List<ManualResetEvent> syncEvents1 = new List<ManualResetEvent>();

            for (int i = 0; i < _entities.Count - 1; i++)
            {
                CMS2.Client.SyncHelper.ThreadState _threadState = new CMS2.Client.SyncHelper.ThreadState();
                _threadState.table = _entities[i];
                _threadState.worker = worker;
                listOfThread.Add(_threadState);

                if (i <= 50)
                {
                    syncEvents.Add(_threadState._event);
                }
                else
                {
                    syncEvents1.Add(_threadState._event);
                }

                try
                {
                    Synchronize sync = new Synchronize(_entities[i].TableName, _filter, _threadState._event, new SqlConnection(_localConnectionString), new SqlConnection(_mainConnectionString));
                    ThreadPool.QueueUserWorkItem(new WaitCallback(sync.PerformSync), _threadState);
                    _threadState._event.WaitOne();
                }
                catch (Exception ex)
                {
                }
            }
        }
        private void StartProvision()
        {
            List<CMS2.Client.SyncHelper.ThreadState> listOfState = new List<CMS2.Client.SyncHelper.ThreadState>();
            List<ManualResetEvent> provisionEvents = new List<ManualResetEvent>();
            List<ManualResetEvent> provisionEvents1 = new List<ManualResetEvent>();
            SqlConnection localConnection = new SqlConnection(_localConnectionString);
            SqlConnection mainConnection = new SqlConnection(_mainConnectionString);

            for (int i = 0; i < _entities.Count - 1; i++)
            {
                //if (_entities[i].isSelected == false) continue;
                CMS2.Client.SyncHelper.ThreadState state = new CMS2.Client.SyncHelper.ThreadState();
                state.table = _entities[i];
                state.worker = Worker;
                state.maximumSize = _entities.Count;
                listOfState.Add(state);
                if (i <= 60)
                {
                    provisionEvents.Add(state._event);
                }
                else
                {
                    provisionEvents1.Add(state._event);
                }

                try
                {
                    Provision provision = new Provision(_entities[i].TableName, localConnection, mainConnection, state._event, _filter, _branchCorpOfficeId);
                    ThreadPool.QueueUserWorkItem(new WaitCallback(provision.Prepare_Database_For_Synchronization), state);
                    state._event.WaitOne();

                }
                catch (Exception ex)
                {

                }
            }

        }
        private void SaveSyncServiceSettings()
        {
            try
            {
                XmlDocument appConfigDoc = new XmlDocument();
                appConfigDoc.Load(OpenFile.FileName);
                foreach (XmlElement xElement in appConfigDoc.DocumentElement)
                {
                    if (xElement.Name == "connectionStrings")
                    {
                        var nodes = xElement.ChildNodes;
                        foreach (XmlElement item in nodes)
                        {
                            if (item.Attributes["name"].Value.Equals("AP_CARGO_SERVICE.Properties.Settings.LocalConnectionString"))
                            {
                                item.Attributes["connectionString"].Value = _localConnectionString;
                            }
                            else if (item.Attributes["name"].Value.Equals("AP_CARGO_SERVICE.Properties.Settings.ServerConnectionString"))
                            {
                                item.Attributes["connectionString"].Value = _mainConnectionString;
                            }
                        }
                    }
                    if (xElement.Name == "applicationSettings")
                    {
                        XmlNodeList nodes = xElement.ChildNodes;

                        foreach (XmlElement item in nodes)
                        {
                            if (item.Name.Equals("AP_CARGO_SERVICE.Properties.Settings"))
                            {
                                XmlNodeList settings = item.ChildNodes;
                                foreach (XmlElement xitem in settings)
                                {
                                    switch (xitem.Attributes["name"].Value)
                                    {
                                        case "BranchCorpOfficeId":
                                            foreach (XmlElement xNode in xitem)
                                            {
                                                xNode.InnerText = _branchCorpOfficeId;
                                            }
                                            break;
                                        case "Filter":
                                            foreach (XmlElement xNode in xitem)
                                            {
                                                xNode.InnerText = _filter;
                                            }
                                            break;
                                        case "Provision":
                                            foreach (XmlElement xNode in xitem)
                                            {
                                                xNode.InnerText = "false";
                                            }
                                            break;
                                        case "DeprovisionServer":
                                            foreach (XmlElement xNode in xitem)
                                            {
                                                xNode.InnerText = "false";
                                            }
                                            break;
                                        case "DeprovisionClient":
                                            foreach (XmlElement xNode in xitem)
                                            {
                                                xNode.InnerText = "false";
                                            }
                                            break;
                                        default:
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }
                appConfigDoc.Save(OpenFile.FileName);
            }
            catch (Exception ex)
            {
                Logs.ErrorLogs("Saving Sync Settings configuration", "SaveSyncSettings", ex.Message);
            }

        }
        private void StartService()
        {
            try
            {
                ServiceController service = new ServiceController("Synchronization Service", Environment.MachineName);
                TimeSpan timeout = TimeSpan.FromMilliseconds(300000);

                if (service.Status == ServiceControllerStatus.Stopped)
                {
                    service.Start();
                    lblProgressState.Text = "Starting Synchronization Service...";
                    service.WaitForStatus(ServiceControllerStatus.Running, timeout);
                    lblProgressState.Text = "Synchronization Service started.";
                    Log.WriteLogs("Synchronization Service started.");
                }
            }
            catch (Exception ex)
            {
                Logs.ErrorLogs("Starting Service", "StartService", ex.Message);
            }

        }
        private void StopService()
        {
            try
            {
                ServiceController service = new ServiceController("Synchronization Service", Environment.MachineName);
                TimeSpan timeout = TimeSpan.FromMilliseconds(60000);
                if (service.Status == ServiceControllerStatus.Running)
                {
                    service.Stop();
                    lblProgressState.Text = "Stopping Synchronization Service...";
                    service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                    Log.WriteLogs("Synchronization Service was stop.");
                }
            }
            catch (Exception ex)
            {
                Logs.ErrorLogs("Stopping Service", "StopService", ex.Message);
            }

        }
        #endregion
    }
}
