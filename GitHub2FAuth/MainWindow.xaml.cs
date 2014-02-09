// foreach or for that is the question http://habrahabr.ru/post/192130/#comment_6681074
// HttpWebResponse.Close() inside a using statement http://stackoverflow.com/questions/505231/should-i-call-close-on-httpwebresponse-even-if-its-inside-a-using-statement
//
// async http://msdn.microsoft.com/ru-ru/library/hh156513.aspx
// Асинхронное программирование с использованием async и await http://msdn.microsoft.com/ru-ru/library/hh191443.aspx
// Make Multiple Web Requests (async and await) http://msdn.microsoft.com/en-us/library/hh696703.aspx?cs-save-lang=1&cs-lang=csharp
// Знакомство с асинхронными операциями в C# 5 http://habrahabr.ru/post/109345
// 
// Marshal.SecureStringToBSTR http://msdn.microsoft.com/en-us/library/system.runtime.interopservices.marshal.securestringtobstr(v=vs.110).aspx
// Безопасные строки в .net–класс SecureString http://www.regfordev.com/2010/11/net-securestring.html
// 
// git-credential-winstore http://gitcredentialstore.codeplex.com/SourceControl/latest#git-credential-winstore/Program.cs
//
// GitHub API v3 http://developer.github.com/v3
// OAuth | GitHub API | Create a new authorization http://developer.github.com/v3/oauth/#create-a-new-authorization
// GitHub API | Working with two-factor authentication http://developer.github.com/v3/auth/#working-with-two-factor-authentication
//
//
//NOTE: do it hardcode (WebRequest -> TcpClient): http://stackoverflow.com/a/6755142
//
//TODO: use "Secure Desktop":
// http://stackoverflow.com/questions/6095244/managed-application-on-secure-desktop-under-windows-7
// http://stackoverflow.com/questions/1188396/createdesktop-with-vista-uac-c-windows
// http://stackoverflow.com/questions/1395351/createdesktop-with-vista-and-uac-on-c-windows
// http://stackoverflow.com/questions/1434363/pinvoke-createdesktop
// or Credentials Provider API:
// http://stackoverflow.com/questions/3286386/run-managed-code-on-secure-desktop
// http://social.msdn.microsoft.com/Forums/windowsdesktop/en-US/350a2632-7514-44d6-a199-048356b2bf60/credential-provider-credui-secure-desktop-and-custom-dialog?forum=windowssecurity
// http://stackoverflow.com/questions/1715335/how-to-use-the-secure-desktop-in-windows-vista-and-w7/6508448#6508448


using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Net;
using System.IO;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace GitHub2FAuth
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow:Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		private void onload(object sender, EventArgs e)
		{
			this.Top = 0;
		}

		private async void onclosed(object sender, EventArgs e)
		{
			await SignOut();
		}

		//http://stackoverflow.com/questions/692342/net-httpwebrequest-getresponse-raises-exception-when-http-status-code-400-ba
		//NOTE: dead code
		private async Task<HttpWebResponse> GetResponseWoEAsync(HttpWebRequest request)
		{
			try{                      return (HttpWebResponse)await request.GetResponseAsync();
			} catch(WebException e) { return (HttpWebResponse)e.Response; }
		}

		private struct ID_token { public string id, token; }

		private async Task<ID_token> CreateGitHubToken(/*NOTE: (for pseudo sec) do not set pass.Password & otpass.Password in param.*/)
		{
			var ghAPICon = HttpWebRequest.CreateHttp("https://api.github.com/authorizations");
			ghAPICon.Method = "POST";
			ghAPICon.UserAgent = "ASOIU GitHub 2FAuth v0.1*";	//TODO: set version from assembly
			ghAPICon.KeepAlive = false;
			ghAPICon.ServicePoint.Expect100Continue = false;
			//httpSync.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
			ghAPICon.Headers.Add(HttpRequestHeader.CacheControl, "private, no-cache, no-store, max-age=0");
			
			try{
				string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(user.Text +":"+ pass.Password));	//TODO: filter ":" in user.Text
				ghAPICon.Headers.Add("Authorization", "Basic " + credentials);
				ghAPICon.Headers.Add("X-GitHub-OTP", otpass.Password);

				using(var rsBody = ghAPICon.GetRequestStream()){
				#if HOME
					byte[] sendBuf = Encoding.UTF8.GetBytes("{\"scopes\":[\"repo\"],\"note\":\"Home GitHub 2FAuth\"}");
				#else
					byte[] sendBuf = Encoding.UTF8.GetBytes("{\"scopes\":[\"repo\"],\"note\":\"ASOIU GitHub 2FAuth\"}");
				#endif
					rsBody.Write(sendBuf, 0, sendBuf.Length);
				}

				using(var ghAPIConResponse = (HttpWebResponse)await ghAPICon.GetResponseAsync()) {
					ghAPICon.Headers.Clear();

					using(var answer = new StreamReader(ghAPIConResponse.GetResponseStream(), Encoding.UTF8)) {
						var stringJsonAnswer = await answer.ReadToEndAsync();

						if(ghAPIConResponse.StatusCode == HttpStatusCode.Created) {
							dynamic jsonAnswer = new dJSON().Deserialize(stringJsonAnswer);
							return new ID_token() { id = jsonAnswer.id.ToString(), token = jsonAnswer.token };
						}

						var webException = new WebException(stringJsonAnswer, null, WebExceptionStatus.ProtocolError, ghAPIConResponse/*NOTE: not work*/) { HelpLink = "http://developer.github.com/v3/" };
						webException.Data["Headers"] = ghAPIConResponse.Headers;
						throw webException;	//to App.xaml.cs
					}
				}
			}finally{
				ghAPICon.Headers.Clear();
				GC.Collect();
			}
		}

		//Delete credential from GitHub http://developer.github.com/v3/oauth/#delete-an-authorization
		//NOTE: not working yet (see SignOut() ): http://stackoverflow.com/questions/17217750/revoking-oauth-access-token-results-in-404-not-found
		private async Task DelGitHubToken(string id, string userName, string password)
		{
			var ghAPICon = HttpWebRequest.CreateHttp("https://api.github.com/authorizations/" + id);
			ghAPICon.Method = "DELETE";
			ghAPICon.UserAgent = "ASOIU GitHub 2FAuth v0.1*";	//TODO: set version from assembly
			ghAPICon.KeepAlive = false;
			ghAPICon.ServicePoint.Expect100Continue = false;
			//httpSync.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
			ghAPICon.Headers.Add(HttpRequestHeader.CacheControl, "private, no-cache, no-store, max-age=0");

			try {
				string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(userName +":"+ password));	//TODO: filter ":" in user.Text
				ghAPICon.Headers.Add("Authorization", "Basic " + credentials);

				using(var ghAPIConResponse = (HttpWebResponse)await ghAPICon.GetResponseAsync()) {
					ghAPICon.Headers.Clear();
					
					if(ghAPIConResponse.StatusCode == HttpStatusCode.NoContent) return;
					
					var webException = new WebException("", null, WebExceptionStatus.ProtocolError, ghAPIConResponse) { HelpLink = "http://developer.github.com/v3/" };
					webException.Data["Headers"] = ghAPIConResponse.Headers;
					throw webException;	//to App.xaml.cs
				}
			} finally {
				ghAPICon.Headers.Clear();
				GC.Collect();
			}
		}

		private void setWinStore(string id, string password)
		{
			NativeMethods.CREDENTIAL cred = new NativeMethods.CREDENTIAL() {
				userName = user.Text,
				credentialBlob = password,
				credentialBlobSize = (uint)Encoding.Unicode.GetByteCount(password),
				targetAlias = id,

				type = NativeMethods.CRED_TYPE.GENERIC,
				persist = NativeMethods.CRED_PERSIST.SESSION,
				attributeCount = 0
			};

			cred.targetName = "git:https://github.com";
			if(!NativeMethods.CredWrite(ref cred)) throw new Exception("Failed to write credential: " + GetLastErrorMessage());

			cred.targetName = "https://github.com/";
			if(!NativeMethods.CredWrite(ref cred)) throw new Exception("Failed to write credential: " + GetLastErrorMessage());
			
		}

		private async Task SignOut()
		{
			//NOTE: not working yet (see DelGitHubToken() )
			/*{//Revoke old credential
				IntPtr credPtr;
				NativeMethods.CredRead("https://github.com/", NativeMethods.CRED_TYPE.GENERIC, 0, out credPtr);
				NativeMethods.CREDENTIAL cred = (NativeMethods.CREDENTIAL)Marshal.PtrToStructure(credPtr, typeof(NativeMethods.CREDENTIAL));

				await DelGitHubToken(cred.targetAlias, cred.userName, cred.credentialBlob);

				NativeMethods.CredFree(ref credPtr);
			}*/
			{//delWinStore
				if(	!NativeMethods.CredDelete("https://github.com/", NativeMethods.CRED_TYPE.GENERIC) | 
					!NativeMethods.CredDelete("git:https://github.com", NativeMethods.CRED_TYPE.GENERIC)) {

					//TODO: do not restart on this exception -> create new exception class & filter it in App.xaml.cs
					//throw new Exception("Failed to delete credential: " + GetLastErrorMessage());
				}
			}
		}

		private async void SignIn_Click(object sender, RoutedEventArgs e)
		{
			var taskSignOut = SignOut();

			var taskCreateGitHubToken = CreateGitHubToken();
			GC.Collect();
			ID_token token = await taskCreateGitHubToken;

			await taskSignOut;

			setWinStore(token.id, token.token);
			GC.Collect();	//NOTE: maybe delete this?

			MessageBox.Show("SignIn: Ok");	//TODO: delete this & set StartupEventArgs.Args
			this.Closed -= onclosed;
			throw new Exception("SignIn: Ok");	//to App.xaml.cs
		}

		private async void SignOut_Click(object sender, RoutedEventArgs e)
		{
			await SignOut();

			MessageBox.Show("SignOut: Ok");	//TODO: delete this & set StartupEventArgs.Args
			this.Closed -= onclosed;
			throw new Exception("SignOut: Ok");	//to App.xaml.cs
		}

		private static string GetLastErrorMessage()
		{
			return new Win32Exception(Marshal.GetLastWin32Error()).Message;
		}

//NOTE: dead code
#if DEBUG
		private void exit(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		private void otpGen(object sender, RoutedEventArgs e)
		{
			PasscodeGenerator otp = new PasscodeGenerator();
			MessageBox.Show(otp.GenerateTimeoutCode("000"));
		}
#endif

	}
}
/*
 * HTTP/1.1 201 Created
 * {
 *	"id":123,
 *	"url":"https://api.github.com/authorizations/123",
 *	"app":{
 *		"name":"ASOIU GitHub 2FAuth (API)",
 *		"url":"http://developer.github.com/v3/oauth/#oauth-authorizations-api",
 *		"client_id":"00000000000000000000"
 *	},
 *	"token":"123789abef",
 *	"note":"ASOIU GitHub 2FAuth",
 *	"note_url":null,
 *	"created_at":"2011-01-11T11:11:11Z",
 *	"updated_at":"2011-01-11T11:11:11Z",
 *	"scopes":["repo"]
 * }
*/