using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Configuration;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System.Threading.Tasks;
using System.Security.Principal;
using Microsoft.Azure;

namespace CryptBitLibrary
{
    public static class Logger
    {
        public enum Severity { Error, Information, Critical, Verbose, Warning };
        private static TelemetryClient telemetryClient = new TelemetryClient();
        
        static Logger()
        {
            //Setup our telemtry client to be able to call 
            string appInsightsKey = CommonHelper.GetSetting("AppInsightsInstrumentationKey");
            telemetryClient.InstrumentationKey = appInsightsKey;
        }

        public static void TrackException(Exception thrownException, int eventID, string message, bool sendAdminNoficiation = false)
        {

            //Data to push into AI which can be searched on
            Dictionary<string, string> prop = new Dictionary<string, string>();
            prop["message"] = message;
            prop["eventID"] = eventID.ToString();
            prop["machine"] = System.Environment.MachineName;
            prop["principal"] = WindowsIdentity.GetCurrent().Name;
            prop["process"] = Process.GetCurrentProcess().ProcessName; ;
            prop["processid"] = Process.GetCurrentProcess().Id.ToString();




            telemetryClient.TrackException(thrownException, prop);

            //Log to System.Diagnostics as well for redundancy
            Trace.TraceError("Exception: {0}, Message:{1}", thrownException.GetType().FullName, thrownException.Message);



        }

        public static void TrackTrace(string message, int eventID, Severity sev)
        {

            //Data to push into AI which can be searched on
            Dictionary<string, string> prop = new Dictionary<string, string>();
            prop["message"] = message;
            prop["eventID"] = eventID.ToString();
            prop["machine"] = System.Environment.MachineName;
            prop["principal"] = WindowsIdentity.GetCurrent().Name;
            prop["process"] = Process.GetCurrentProcess().ProcessName; ;
            prop["processid"] = Process.GetCurrentProcess().Id.ToString();



            try
            {


                Microsoft.ApplicationInsights.DataContracts.SeverityLevel sevai = SeverityLevel.Information;


                switch (sev)
                {
                    case Severity.Critical:
                        sevai = Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Critical;
                        break;

                    case Severity.Error:
                        sevai = Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error;
                        break;

                    case Severity.Information:
                        sevai = Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Information;
                        break;

                    case Severity.Verbose:
                        sevai = Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Verbose;
                        break;

                    case Severity.Warning:
                        sevai = Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Warning;
                        break;
                }

                telemetryClient.TrackTrace(message, sevai, prop);

                //Log to System.Diagnostics as well for redundancy
                Trace.WriteLine(String.Format("Message:{0}, Severity:{1}", message, sev));

            }
            catch (Exception ex)
            {
                TrackException(ex, 0, "Error writing trace");
            }

        }



    }
}





