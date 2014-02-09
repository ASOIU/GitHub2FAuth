using System;
using System.Diagnostics;
using System.Windows;
using System.Text;
using System.Collections;
using System.Net;
using System.IO;
using System.Reflection;
using System.Security.Principal;

namespace GitHub2FAuth
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App:Application
    {
		private readonly string sLogSource = "ASOIU GitHub 2F Auth";


		private void onStartUp(object s, StartupEventArgs ea)
		{
			//Something wrong?
			//Restart to clear private data! (maybe-> recursive restart)
			Application.Current.DispatcherUnhandledException += (sender, e) => {
				e.Handled = true;
				onException(e.Exception, EventLogEntryType.Warning);

				Process.Start(Assembly.GetExecutingAssembly().Location);
				this.Shutdown(1);
			};
			AppDomain.CurrentDomain.UnhandledException += (sender, e) => {
				onException((Exception)e.ExceptionObject, EventLogEntryType.Warning);

				Process.Start(Assembly.GetExecutingAssembly().Location);
				this.Shutdown(1);
			};
			AppDomain.CurrentDomain.FirstChanceException += (sender, e) => {
				onException(e.Exception, EventLogEntryType.Information);
			};

			//NOTE: run once as Admin
			if(!EventLog.SourceExists(sLogSource)) {
				if(new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator)) {
					EventLog.CreateEventSource(sLogSource, "Application");
				} else {
					MessageBox.Show("Run once as Admin");
				}

				this.Shutdown(1);
			}
		}

		private void onException(Exception e, EventLogEntryType eType)
		{
			StringBuilder error = new StringBuilder();

			do{
				error.AppendLine("Source: "			 + e.Source);
				error.AppendLine("Type: "			 + e.GetType());
				error.AppendLine("Target Site: "	 + e.TargetSite);
				error.AppendLine("Help Link: "		 + e.HelpLink);
				
				error.AppendLine("\nMessage: \n"	 + e.Message);

				if(e is WebException) {
					error.AppendLine("\nWeb Status: ");
					error.AppendLine(((WebException)e).Status.ToString());
					try {
						error.AppendLine("\nWeb Message Body: ");
						using(var answer = new StreamReader(((WebException)e).Response.GetResponseStream(), Encoding.UTF8)) {
							error.AppendLine(answer.ReadToEnd());
						}
						error.AppendLine("\nWeb Response Headers: ");
						error.AppendLine(((WebException)e).Response.Headers.ToString());
					} catch { }
				}
				
				if(e.Data.Count > 1) {
					error.AppendLine("\nExtra details: ");
					foreach(DictionaryEntry item in e.Data)
						error.AppendFormat(" Key: {0,-20} Value: {1}\n", item.Key.ToString(), item.Value);
				}

				error.AppendLine("\nStack Trace: " + e.StackTrace);

			}while((e = e.InnerException) != null);

			EventLog.WriteEntry(sLogSource, error.ToString(), eType/*TODO: control this*/);			
		}
	}
}
