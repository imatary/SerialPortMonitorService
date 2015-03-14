using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;

namespace SerialPortMonitorService
{
    public enum ServiceState
    {
        SERVICE_STOPPED = 0x00000001,
        SERVICE_START_PENDING = 0x00000002,
        SERVICE_STOP_PENDING = 0x00000003,
        SERVICE_RUNNING = 0x00000004,
        SERVICE_CONTINUE_PENDING = 0x00000005,
        SERVICE_PAUSE_PENDING = 0x00000006,
        SERVICE_PAUSED = 0x00000007,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ServiceStatus
    {
        public long dwServiceType;
        public ServiceState dwCurrentState;
        public long dwControlsAccepted;
        public long dwWin32ExitCode;
        public long dwServiceSpecificExitCode;
        public long dwCheckPoint;
        public long dwWaitHint;
    };

    public partial class SerialPortMonitorService : ServiceBase
    {
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(IntPtr handle, ref ServiceStatus serviceStatus);
        private ServiceStatus _serviceStatus;
        private System.Diagnostics.EventLog eventLog;
        private int eventId;

        public SerialPortMonitorService(string[] args)
        {
            InitializeComponent();

            String arr = args[0];

            eventLog = new System.Diagnostics.EventLog();
            if (!System.Diagnostics.EventLog.SourceExists("MySource"))
            {
                System.Diagnostics.EventLog.CreateEventSource("MySource", "MyNewLog");
            }
            eventLog.Source = "MySource";
            eventLog.Log = "MyNewLog";

            eventLog.WriteEntry("Starting with args: " + arr);
        }

        protected override void OnStart(string[] args)
        {
            _serviceStatus = new ServiceStatus();
            SetStartPendingStatus(_serviceStatus);

            eventLog.WriteEntry("In OnStart");

            // Set up a timer to trigger every minute.
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = 6000; // 60 seconds
            timer.Elapsed += new System.Timers.ElapsedEventHandler(this.OnTimer);
            timer.Start();

            SetRunningStatus(_serviceStatus);
        }

        private void DoAll()
        {
            string connectionString = "Data Source=(local);Initial Catalog=SensorsDB;" + "Integrated Security=true";

            // Provide the query string with a parameter placeholder.
            string queryString =
                "select top @number from * Temperatures;";

            // Specify the parameter value.
            int paramValue = 5;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                // Create the Command and Parameter objects.
                SqlCommand command = new SqlCommand(queryString, connection);
                command.Parameters.AddWithValue("@number", paramValue);

                try
                {
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        eventLog.WriteEntry("Fuck: " + reader[0] /*+ ", " + reader[1] + ", " + reader[2]*/);
                    }
                    reader.Close();
                }
                catch (Exception ex)
                {
                    eventLog.WriteEntry("Failed to access database: " + ex.Message);
                }
            }
        }

        private void SetRunningStatus(ServiceStatus serviceStatus)
        {
            // Update the service state to Running.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }

        private void SetStartPendingStatus(ServiceStatus serviceStatus)
        {
            // Update the service state to Start Pending.            
            serviceStatus.dwCurrentState = ServiceState.SERVICE_START_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }

        protected override void OnStop()
        {
            eventLog.WriteEntry("In OnStop");
        }

        public void OnTimer(object sender, System.Timers.ElapsedEventArgs args)
        {
            // TODO: Insert monitoring activities here.
            DoAll();
            eventLog.WriteEntry("Monitoring the System", EventLogEntryType.Information, eventId++);
        }


    }
}
