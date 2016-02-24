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

//TODO: Refactor module variables names
//TODO: Rewrite not to use  System.Web.Mail.SmtpMail because it may be causing System Event Log entries "The application-specific permission settings do not grant Local Launch permission for the COM Server application with CLSID" 
using System;
using System.Collections;
using System.Configuration;

namespace CommonSenseSoft.Log{
	
	/// <summary>
	/// Provides enhanced logging and notification capabilities
	/// </summary>
	public static class Logging{
		//public Logging(){}
		
		#region Module Level Variables
		static Hashtable _htbAlerts=new Hashtable();
		const int MESSAGE_RECOGNITION_LEN = 25;
		static string EXCEPTION_DETAILS_DELIMITER=((char)0).ToString();//that is to put a delimiter before the details, so that we can always extract the initial message
		
		static string _strLogFilePathName = "";
		static string _strLogFileFallback;
		static bool _blnUseUTC = true; 
		static bool _blnUseDateForLogFileName = false;
		static bool _blnLogFileSizeExceeded = false;
		static int _intLogLevel = (int) LogLevels.Fatal;
		static bool _blnInteractive = false;
		static int _intAlertsLogLevel = (int) LogLevels.None;
		static string _strAlertsMailServers;
		static string _strAlertsRecipients;
		static string _strAlertsMailAccount;
		static double _dblAlertResetIntervalMinutes=30; //Interval after which the same message will trigger alert again
		static int _intAlertsJournalSize=100; //Number of messages memorised by this component
		static bool _blnAlertsUseMessageText=true; //If set to true, the first 15 characters of a message or exception will be used as a messageID (exceptionID) if the parameter is not present
		static int _intProcessMemoryThresholdKb;//Expected maximum amount of RAM your application can take.
		static short _srtLogFileSizeThresholdMb=1;//That is when the App starts complaining about the Log file being too Large
		static short _srtLogFileSizeLimitMb=2;//it is dangerous to allow log file grow with no limit
		static long _lngLogFileSizeLimitBytes=2*1048576;//internal representation of msrtLogFileSizeLimitMb in bytes
		static bool _blnDateInsertedIntoFileName=false;//If the date has been inserted already, we need to replace it rather than inserting again
		static bool _blnUseLowestLogLevelIfLocalLevelConfigured=false;
		static bool _blnSuppressAssembliesOutput=false;
		#endregion

		#region PROPERTIES

		#region Log File - related

		#region LogFilePathName
		/// <summary>
		/// Full name of log file. If omitted, AppSettings["LogFile"] or Log.Log files in Application directory will be used
		/// </summary>
		public static string LogFilePathName{
			set{_strLogFilePathName=value;}
			get{return _strLogFilePathName;}
		}
		#endregion

		#region LogFileFallback
		/// <summary>
		/// If we fail writing to logFile, this file is used to log, as well as Event Log 
		/// </summary>
		public static string LogFileFallback {
			get{
				if(_strLogFileFallback==null){
					string path=System.Reflection.Assembly.GetExecutingAssembly().CodeBase;//that gives directory of the dll
					int intStartIndex = path.IndexOf("///");
					if (intStartIndex>-1) {
						int intEndIndex = path.LastIndexOf("/");
						path = path.Substring(intStartIndex + 3,intEndIndex - intStartIndex - 3);
						path = path.Replace("/",@"\");
					}
					#region Prevent Web Application from restarting itself when it logs to its bin directory
					if(path.EndsWith("\\bin")){
						path=path.Remove(path.Length-4);
					}
					#endregion
					_strLogFileFallback=path+@"\__FALLBACK.LOG";
				}
				return _strLogFileFallback;
			}
		}
		#endregion

		#region LogLevel
		/// <summary>
		/// Log level. If specified in LogMessage, the message will be logged if the specified EventMetadata logLevel is equal or less than this value.
		/// </summary>
		public static int LogLevel{
			set{_intLogLevel=value;}
			get{return _intLogLevel;}
		}
		#endregion

		#region UseUTC
		/// <summary>
		/// Should UTC be used when logging. Default = true
		/// </summary>
		public static bool UseUTC{
			set{_blnUseUTC=value;}
			get{return _blnUseUTC;}
		}
		#endregion

		#region UseDateForLogFileName
		/// <summary>
		/// If true, the log file name will be ignored, and every day new log file will be used to log to, with file name representing the date.
		/// Handy when the application is running absolutely unattended and the log file's size should not exceed a daily quota.
		/// Default=false
		/// </summary>
		public static bool UseDateForLogFileName{
			set{_blnUseDateForLogFileName=value;}
			get{return _blnUseDateForLogFileName;}
		}
		#endregion

		#region LogFileSizeLimitMb
		/// <summary>
		/// As the log file reaches this size, no more logging will be performed into that file. That value should be greater than LogFileSizeThresholdMb so that we get notifications of the log file approaching its limit BEFORE the limit has been reached
		/// </summary>
		public static short LogFileSizeLimitMb{
			set{
				_srtLogFileSizeLimitMb=value;
				_lngLogFileSizeLimitBytes=_srtLogFileSizeLimitMb*1048576;
				if(_srtLogFileSizeThresholdMb>0){
					if(value<=_srtLogFileSizeThresholdMb) throw new Exception("LogFileSizeLimitMb must be greater than LogFileSizeThresholdMb, otherwise the logging will stop before any notification could be made.");
				}
			}
			get{return _srtLogFileSizeLimitMb;}
		}
		#endregion

		#region LogFileSizeThresholdMb
		/// <summary>
		/// As the log file reaches this size, further logging to that file will be accompanied with notifications of that issue
		/// </summary>
		public static short LogFileSizeThresholdMb{
			set{
				if(_srtLogFileSizeLimitMb>0){
					if(value>=_srtLogFileSizeLimitMb)throw new Exception("LogFileSizeThresholdMb must be lower than LogFileSizeLimitMb, otherwise the logging will stop before any notification could be made.");
				}
				_srtLogFileSizeThresholdMb=value;
			}
			get{return _srtLogFileSizeThresholdMb;}
		}
		#endregion

		#region ProcessMemoryThresholdKb
		/// <summary>
		/// Should the App consume more memory, the CheckProcessMemory() will log this problem, the caller will not have to do that itself.
		/// </summary>
		public static int ProcessMemoryThresholdKb{
			set{_intProcessMemoryThresholdKb=value;}
			get{return _intProcessMemoryThresholdKb;}
		}
		#endregion

		#region UseLowestLogLevel
		/// <summary>
		/// When set to true, the locally set log level has precedence if it is lower than global log level. Otherwise (or if not set) greater of Global and Local is used because the intention is to see the order of events.
		/// </summary>
		public static bool UseLowestLogLevel{
			set{_blnUseLowestLogLevelIfLocalLevelConfigured=value;}
			get{return _blnUseLowestLogLevelIfLocalLevelConfigured;}
		}
		#endregion

		#region SuppressAssembliesOutput
		/// <summary>
		/// In environments that are well-controlled there is no value in outputing all the Assemblies data, so here is an option to surpress that output
		/// </summary>
		public static bool SuppressAssembliesOutput{
			set{_blnSuppressAssembliesOutput=value;}
			get{return _blnSuppressAssembliesOutput;}
		}
		#endregion

		#endregion


		#region Alerts - related

		#region AlertsLogLevel
		/// <summary>
		/// Log level. If specified in LogMessage, the message will be logged if the specified EventMetadata logLevel is equal or less than this value.
		/// It is of integer type so that it is easier to set it in config file.
		/// See the LogLevels enumeration for the mappings.
		/// The default is none, so that the logging does not fail when not completely configured.
		/// </summary>
		public static int AlertsLogLevel{
			set{_intAlertsLogLevel=value;}
			get{return _intAlertsLogLevel;}
		}
		#endregion

		#region AlertsMailServers
		/// <summary>
		/// Semicolon-delimited list of SMTP servers to send the email alerts. The first available will be used.
		/// </summary>
		public static string AlertsMailServers{
			set{_strAlertsMailServers=value;}
			get{return _strAlertsMailServers;}
		}
		#endregion

		#region AlertsRecipients
		/// <summary>
		/// Semicolon-delimited list of recipients of the alerts
		/// </summary>
		public static string AlertsRecipients{
			set{_strAlertsRecipients=value;}
			get{return _strAlertsRecipients;}
		}
		#endregion

		#region AlertsMailAccount
		/// <summary>
		/// Valid Email account used to send alerts by email. Valid in terms that many SMTP servers will be matching the domain name of that account to domain name of sending SMTP server.
		/// </summary>
		public static string AlertsMailAccount{
			set{_strAlertsMailAccount=value;}
			get{return _strAlertsMailAccount;}
		}
		#endregion

		#region AlertResetIntervalMinutes
		/// <summary>
		/// Interval after which the same notifications will be allowed to be emailed again. Default is 30 min. That is to prevent of flooding mailboxes with the same notification, especially if the problem repeats every second.
		/// </summary>
		public static double AlertResetIntervalMinutes{
			set{_dblAlertResetIntervalMinutes=value;}
			get{return _dblAlertResetIntervalMinutes;}
		}
		#endregion

		#region AlertsJournalSize
		/// <summary>
		/// Number of messages memorised by the component, so that these messages were not sent as alerts more often than specified by AlertResetIntervalMinutes
		/// </summary>
		public static int AlertsJournalSize{
			set{_intAlertsJournalSize=value;}
			get{return _intAlertsJournalSize;}
		}
		#endregion

		#region AlertsUseMessageText
		/// <summary>
		/// There should be a way to prevent sending the same message every second, only because an exception happens every second in a particular part of application.
		/// The preferred way is to use exceptionId (messageId) parameter of LogException or LogMessage methods, where there is a danger of the same exception(or message) being thrown too often.
		/// As a safety net, in absense of the exceptionId, the first 15 characters of the EventMetadata Text will be used instead.
		/// Default=true. If set to false, the only alerts sent will be for calls to LogException or LogMessage that specify the exceptionId (messageId) parameter.
		/// </summary>
		public static bool AlertsUseMessageText{
			set{_blnAlertsUseMessageText=value;}
			get{return _blnAlertsUseMessageText;}
		}
		#endregion

		#endregion
	
		#region Interactive
		/// <summary>
		/// If set to true, and an exception is logged, a Windows form will be presented to the user, so that the developer implementing this component does not have to develop any user dialog.
		/// Do not use this functionality in unattended applications, as there will be no one to let the application proceed further!
		/// Default=false 
		/// </summary>
		public static bool Interactive{
			set{_blnInteractive=value;}
			get{return _blnInteractive;}
		}
		#endregion
		
		#region ApplicationName
		/// <summary>
		/// Here the caller may pass the Application name they want to appear in logs.
		/// </summary>
		public static string ApplicationName;
		#endregion
		
		#endregion
		
		#region LogException
		/// <summary>
		/// Logs Exception to log file. Depending on Alerts configuration, this may also result in sending an SMTP message
		/// </summary>
		/// <param name="action">action attempted, that resulted in the exception</param>
		/// <param name="e">Exception to log</param>
		/// <param name="throwBack">Do you want this exception being thrown back to you? Default=false</param>
		/// <param name="logFile">Log file. If omitted, LogFilePathName, AppSettings["LogFile"] or Log.Log files will be used</param>
		/// <param name="exceptionID">Unique ID assigned in your code. Required so that the same exception does not send SMTP alerts every second</param>
		public static void LogException(string action,Exception e,bool throwBack,string logFile,string exceptionID){
            //logFile=GetLogFilePathName(logFile);
            string strAction=(action==null||action=="")?"":strAction="Action:["+action+"]\r\n";
			LogLevels LogLevelToUse=LogLevels.Fatal;
			
			#region Get Exception Type
			string strType=e.GetType().FullName;
			if(e.Message.StartsWith(strType)){
				strType="";
			}
			#endregion
			
			#region Check if Exception is to be converted to Info
			switch(e.GetType().FullName){
				#region SqlException
				case "System.Data.SqlClient.SqlException":
				break;
				#endregion
				#region OdbcException
				case "System.Data.Odbc.OdbcException":
				break;
				#endregion
				#region Web
				case "System.Web.HttpException":
					if(e.Message.StartsWith("The file ") && e.Message.EndsWith(" does not exist.")){//Non-existent page was called. Happens with bots especially bing
						//Moving known bots functionality from Web library not currently warranted, just do something good enough
						if(System.Web.HttpContext.Current!=null && System.Web.HttpContext.Current.Request!=null){
							if(System.Web.HttpContext.Current.Request.UserAgent.IndexOf("http://",StringComparison.OrdinalIgnoreCase)!=-1
							|| System.Web.HttpContext.Current.Request.UserAgent.IndexOf("https://",StringComparison.OrdinalIgnoreCase)!=-1
							|| System.Web.HttpContext.Current.Request.UserAgent.IndexOf("bot",StringComparison.OrdinalIgnoreCase)!=-1){
								LogLevelToUse=LogLevels.Information;
								strAction+="EXCEPTION DOWNGRADED TO INFO DUE TO BOT IDENTIFIED CALLING NON-EXISTENT PAGE\r\n";
							}
						}
					} 
				break;
				case "System.Web.HttpParseException":
                case "System.Web.HttpRequestValidationException":
                case "System.Web.HttpUnhandledException":
				break;
				#endregion
			}
			#endregion

			#region Use LogMessage
			//Temporarily place inner exceptions first and see what happens. Otherwise all System.Web.HttpUnhandledException look the same and don't trigger emails string strMsg = strType+e.Message+EXCEPTION_DETAILS_DELIMITER+Environment.NewLine+GetInfoSpecificToExceptionType(e)+GetInnerExceptions(e)+strAction
			string strMsg=GetInnerExceptions(e)+strType+": "+e.Message+EXCEPTION_DETAILS_DELIMITER+Environment.NewLine+GetInfoSpecificToExceptionType(e)+strAction
			+"Stack Trace:{"+CleanCallStack(e.StackTrace)+"}"+((_blnSuppressAssembliesOutput)?"":GetAssembliesEngaged());
			LogMessage(strMsg.Replace(EXCEPTION_DETAILS_DELIMITER,""),logFile,true,LogLevelToUse,-1,exceptionID);//We do not need the delimiter logged, but want it be passed on
			#endregion
			
			if(throwBack){throw e;}
		}
		#region Overloads
		#region Own code base overloads
		/// <summary>
		/// This overload is iseful when there is no actual exception in the system, but we want an event to treat as an exception
		/// </summary>
		/// <param name="exceptionMessage">Exception message</param>
		/// <param name="throwBack">Should an exception be thrown back into the system?</param>
		/// <param name="logFile">Log file if different from globally configured</param>
		/// <param name="exceptionID">Unique ID assigned in your code. Required so that the same exception does not send SMTP alerts every second</param>
		public static void LogException(string exceptionMessage,bool throwBack,string logFile,string exceptionID) {
			//logFile=GetLogFilePathName(logFile);
			string strMsg = exceptionMessage+((_blnSuppressAssembliesOutput)?"":GetAssembliesEngaged());
			LogMessage(strMsg,logFile,true,LogLevels.Fatal,-1,exceptionID);
			if (_blnInteractive) {
				frmError objErrorForm = new frmError();
				objErrorForm.txtError.Text = strMsg;
				objErrorForm.butViewLog.Tag=_strLogFilePathName;
				objErrorForm.ShowDialog();
			}
			if (throwBack) { throw new Exception(exceptionMessage); }
		}
		#endregion

		/// <summary>
		/// Logs Exception to log file. Depending on Alerts configuration, this may also result in sending an SMTP message
		/// </summary>
		/// <param name="e">Exception to log</param>
		/// <param name="throwBack">Do you want this exception being thrown back to you? Default=false</param>
		/// <param name="logFile">Log file. If omitted, logFilePathName, AppSettings["LogFile"] or Log.Log files will be used</param>
		public static void LogException(Exception e, bool throwBack, string logFile){
			LogException(e, throwBack, logFile, "");
		}

		/// <summary>
		/// Logs Exception to log file. Depending on Alerts configuration, this may also result in sending an SMTP message
		/// </summary>
		/// <param name="e">Exception to log</param>
		/// <param name="throwBack">Do you want this exception being thrown back to you? Default=false</param>
		public static void LogException(Exception e, bool throwBack){
			LogException(e,throwBack,null,"");
		}
		
		/// <summary>
		/// Logs Exception to log file. Depending on Alerts configuration, this may also result in sending an SMTP message
		/// </summary>
		/// <param name="e">Exception to log</param>
		/// <param name="exceptionID">Unique ID assigned in your code. Required so that the same exception does not send SMTP alerts every second</param>
		/// <param name="throwBack">Do you want this exception being thrown back to you? Default=false</param>
		public static void LogException(Exception e, string exceptionID, bool throwBack){
			LogException(e,throwBack,null,exceptionID);
		}
		/// <summary>
		/// Logs Exception to log file. Depending on Alerts configuration, this may also result in sending an SMTP message
		/// </summary>
		/// <param name="e">Exception to log</param>
		/// <param name="exceptionID">Unique ID assigned in your code. Required so that the same exception does not send SMTP alerts every second</param>
		public static void LogException(Exception e, string exceptionID){
			LogException(e,false,null,exceptionID);
		}

		/// <summary>
		/// Logs Exception to log file. Depending on Alerts configuration, this may also result in sending an SMTP message
		/// </summary>
		/// <param name="e">Exception to log</param>
		public static void LogException(Exception e){
			LogException(e,false,null,"");
		}

		/// <summary>
		/// This overload is iseful when there is no actual exception in the system, but we want an event to treat as an exception
		/// </summary>
		/// <param name="exceptionMessage">Exception message</param>
		/// <param name="throwBack">Should an exception be thrown back into the system?</param>
		/// <param name="logFile">Log file if different from globally configured</param>
		public static void LogException(string exceptionMessage,bool throwBack,string logFile) {
			LogException(exceptionMessage,throwBack,logFile,"");
		}
		/// <summary>
		/// This overload is iseful when there is no actual exception in the system, but we want an event to treat as an exception
		/// </summary>
		/// <param name="exceptionMessage">Exception message</param>
		/// <param name="throwBack">Should an exception be thrown back into the system?</param>
		public static void LogException(string exceptionMessage,bool throwBack) {
			LogException(exceptionMessage,throwBack,null,"");
		}
		/// <summary>
		/// This overload is iseful when there is no actual exception in the system, but we want an event to treat as an exception
		/// </summary>
		/// <param name="exceptionMessage">Exception message</param>
		public static void LogException(string exceptionMessage) {
			LogException(exceptionMessage,false,null,"");
		}
		/// <summary>
		/// This overload is iseful when there is no actual exception in the system, but we want an event to treat as an exception
		/// </summary>
		/// <param name="exceptionMessage">Exception message</param>
		/// <param name="exceptionID">Unique ID assigned in your code. Required so that the same exception does not send SMTP alerts every second</param>
		public static void LogException(string exceptionMessage,string exceptionID) {
			LogException(exceptionMessage,false,null,exceptionID);
		}
		
		/// <summary>
		/// Logs Exception to log file. Depending on Alerts configuration, this may also result in sending an SMTP message
		/// </summary>
		/// <param name="e">Exception to log</param>
		/// <param name="throwBack">Do you want this exception being thrown back to you? Default=false</param>
		/// <param name="logFile">Log file. If omitted, LogFilePathName, AppSettings["LogFile"] or Log.Log files will be used</param>
		/// <param name="exceptionID">Unique ID assigned in your code. Required so that the same exception does not send SMTP alerts every second</param>
		public static void LogException(Exception e,bool throwBack,string logFile,string exceptionID) {
			LogException(null,e,throwBack,logFile,exceptionID);
		}
		/// <summary>
		/// Logs Exception to log file. Depending on Alerts configuration, this may also result in sending an SMTP message
		/// </summary>
		/// <param name="action">action attempted, that resulted in the exception</param>
		/// <param name="e">Exception to log</param>
		/// <param name="throwBack">Do you want this exception being thrown back to you? Default=false</param>
		/// <param name="exceptionID">Unique ID assigned in your code. Required so that the same exception does not send SMTP alerts every second</param>
		public static void LogException(string action,Exception e,bool throwBack,string exceptionID){
			LogException(action,e,throwBack,null,exceptionID);
		}
		/// <summary>
		/// Logs Exception to log file. Depending on Alerts configuration, this may also result in sending an SMTP message
		/// </summary>
		/// <param name="action">action attempted, that resulted in the exception</param>
		/// <param name="e">Exception to log</param>
		/// <param name="throwBack">Do you want this exception being thrown back to you? Default=false</param>
		public static void LogException(string action,Exception e,bool throwBack) {
			LogException(action,e,throwBack,null,"");
		}
		/// <summary>
		/// Logs Exception to log file. Depending on Alerts configuration, this may also result in sending an SMTP message
		/// </summary>
		/// <param name="action">action attempted, that resulted in the exception</param>
		/// <param name="e">Exception to log</param>
		public static void LogException(string action,Exception e) {
			LogException(action,e,false,null,"");
		}

		#endregion
		#endregion

		#region RelayException
		/// <summary>
		/// Throws generic Exception back into stack, adding text specified in action parameter. 
		/// Information specific to particular flavour of the exception relayed is passed in exception's description, so do not worry about adding it manually.
		/// Useful when logging is not reasonable at this stack level.
		/// </summary>
		/// <param name="actionFailed">Action that resulted in this exception</param>
		/// <param name="e">exception occurred</param>
		static public void RelayException(string actionFailed,Exception e){
			string strAction=(actionFailed==null||actionFailed=="")?"":strAction="Action:["+actionFailed+"]\r\n";
			string strType=e.GetType().FullName;
			if(e.Message.StartsWith(strType)) {
				strType="";
			}
			string strMsg="{"+e.Message+"}"+EXCEPTION_DETAILS_DELIMITER+" "+strType+Environment.NewLine+GetInfoSpecificToExceptionType(e)+GetInnerExceptions(e)+strAction+CleanCallStack(e.StackTrace);
		    throw new Exception(strMsg);
		}

		/// <summary>
		/// Throws generic Exception back into stack, adding text specified in action parameter. 
		/// Information specific to particular flavour of the exception relayed is passed in exception's description, so do not worry about adding it manually.
		/// Useful when an evasive action failed but logging is not reasonable at this stack level.
		/// </summary>
		/// <param name="e">exception occurred</param>
		static public void RelayException(Exception e){
			RelayException("",e);
		}
		#endregion

		#region LogMessage
		/// <summary>
		/// Logs message to file. Depending on Alerts configuration, this may also result in sending an SMTP message.
		/// </summary>
		/// <param name="message">Message text to be logged (and may be sent)</param>
		/// <param name="logFile">Log file. If omitted, LogFilePathName, AppSettings["LogFile"] or Log.Log files will be used</param>
		/// <param name="insertBlankLines">Inserts empty lines before and after the message when writing to the log.</param>
		/// <param name="logLevel">Defines what the minimum level of logging should be configured (see LogLevel property) in order to log this message and AlertsLogLevel property to mail this message. This allows changing the level of details logged without changing and recompiling the application.Default=ExceptionsOnly</param>
		/// <param name="localLogLevel">Redefines global log level for this particular call</param>
		/// <param name="messageID">Unique ID of the message. It is required so that the message does not cause Alert more than once in a period configured (AlertResetIntervalMinutes property)</param>
		public static void LogMessage(string message, string logFile, enuBlankLines insertBlankLines, LogLevels logLevel, int localLogLevel, string messageID){
			#region Immediately check if we are logging before collecting any info
			if(_blnUseLowestLogLevelIfLocalLevelConfigured && localLogLevel>0){//If it is 0 it means it is not set and -1 is passed to this proc when the param is omitted
				if(((int)logLevel>_intLogLevel||(int)logLevel>localLogLevel) && (int)logLevel>_intAlertsLogLevel) return;
			}
			else{
				if((int)logLevel>_intLogLevel && (int)logLevel>localLogLevel && (int)logLevel>_intAlertsLogLevel) return;
			}
			#endregion
			
			string strAction="Start";
			string strMessageToThrowToCaller="";
			System.Text.StringBuilder sb=new System.Text.StringBuilder(TimeStamp);
			try{
				string strMessageFallBack="";
				
				#region Get the calling method
				System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace();
				System.Diagnostics.StackFrame stackFrame;
				System.Reflection.MethodBase methodBase=null;
				for(int i=1;i<10;i++){//The trace may be full of our internal stuff which we don't want
					stackFrame = stackTrace.GetFrame(i);
					methodBase = stackFrame.GetMethod();
					if(methodBase.DeclaringType.Name!="Logging")break; 
				}
				#endregion
				#region Create the message's meta tag. It is needed for both - Log and Alert
				strAction="MetaTag";
				sb.Append("\t");
				sb.Append(System.Threading.Thread.CurrentThread.GetHashCode().ToString());
				sb.Append(" ");sb.Append(System.Threading.Thread.CurrentThread.Name);
				sb.Append("\t");sb.Append(((int)logLevel));sb.Append("/");sb.Append(localLogLevel);sb.Append("/");sb.Append(_intLogLevel);sb.Append("/");sb.Append(_intAlertsLogLevel);
				sb.Append("\t");sb.Append(System.Environment.MachineName);
				sb.Append("\t");sb.Append(System.Environment.UserName);
				sb.Append("\t");sb.Append(methodBase.DeclaringType.Name);sb.Append(".");sb.Append(methodBase.Name);sb.Append(": ");
				sb.Append(message);
				methodBase=null;stackFrame=null;stackTrace=null;
				#endregion
				
				#region Log To File
				//NOT EVERY USER CAN WRITE TO EVENT LOG, SO WE KEEP IT AS A FALL-BACK
				//WHEN LOG FILE IS UNACCESSIBLE WE ATTEMPT TO LOG TO BACKUP LOG AND EVENT LOG.
				if((_blnUseLowestLogLevelIfLocalLevelConfigured && ((int)logLevel<=_intLogLevel && ((int)logLevel<=localLogLevel || localLogLevel<=0))) 
				|| !_blnUseLowestLogLevelIfLocalLevelConfigured && ((int)logLevel<=_intLogLevel || (int)logLevel<=localLogLevel)){
					//Then we actually worry about the file we are writing into
					logFile=GetLogFilePathName(logFile);//That exuses every caller from calling GetLogFilePathName(logFile), especially if this method figures out that we are not writing this call
					
					if(! _blnLogFileSizeExceeded){//That refers to the globally-configured file unless there happened to be a local file that tripped that flag
						#region Check File Size
						strAction="File Size";
						try {
							// Check size of the log file and do not allow more than 2 Mb
							System.IO.FileInfo fi = new System.IO.FileInfo(logFile);
							if(fi.Exists){
								if(fi.Length > _lngLogFileSizeLimitBytes){
									sb=new System.Text.StringBuilder("\r\n\r\nThe log file size has exceeded ");
									sb.Append(_srtLogFileSizeLimitMb);
									sb.Append("Mb limit, and no logging will be performed to this file anymore. Fix the cause of the excessive logging or reconfigure the limit.");
									_blnLogFileSizeExceeded = true;
									#region fix the log level to a level of sending an alert, so that the user is notified about not writing to the log
									if((int)logLevel > _intAlertsLogLevel){
										logLevel=(LogLevels)_intAlertsLogLevel;
									}
									#endregion
								}
							}
						}catch{}//do not abuse resources.
						#endregion
						
						#region Insert blank lines as requested
						switch(insertBlankLines){
							case enuBlankLines.None:
							break;
							case enuBlankLines.DoubleTopAndBottom://THat what was in previous implementation
								sb.Insert(0,Environment.NewLine+Environment.NewLine);sb.AppendLine();
							break;
							case enuBlankLines.TopAndBottom:
								sb.Insert(0,Environment.NewLine);sb.AppendLine();
							break;
							case enuBlankLines.Top:
								sb.Insert(0,Environment.NewLine);
							break;
							case enuBlankLines.Bottom:
								sb.AppendLine();
							break;
							default:
								throw new NotImplementedException("insertBlankLines="+insertBlankLines.ToString()+" is not implemented");
						}
						#endregion
						
						strAction="Log to " + logFile;
						strMessageFallBack=WriteLineToFileWithExceptionReturned(logFile,sb.ToString());

						if(strMessageFallBack!=""){
							#region fix the log level to a level of sending an alert, so that the user is notified about failure of writing to the log
							if((int)logLevel > _intAlertsLogLevel){
								logLevel=(LogLevels)_intAlertsLogLevel;
							}
							#endregion
									
							#region Fall back to other media
							//We need to alter the message itself because now it also goes to Email alert
							sb.Insert(0,TimeStamp + "\tCould not log message to [" + logFile+"] file. \r\nReason: ["+strMessageFallBack+"]\r\nMessage attempted to log:[");
							sb.Append("]");
							strAction="Log to fallback file";
							WriteLineToFileWithExceptionReturned(LogFileFallback,"\r\n"+sb.ToString());
							
							//try to write to EventLog (will need permission to create a registry key, which may be haemorrhoid
							//To enable writing to event log, fire regedt32.exe tool and navigate to
							// HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Eventlog key. Select Permissions in Security menu (on top)
							// and add ASPNET (for website) or another account you are using for service, etc.
							strAction="Log to EventLog";
							try{
								//System.Security.Policy.ApplicationSecurityInfo asi = new System.Security.Policy.ApplicationSecurityInfo(AppDomain.CurrentDomain.ActivationContext);
								//System.Diagnostics.EventLog.WriteEntry(asi.ApplicationId.Name,strMessageFallBack,System.Diagnostics.EventLogEntryType.Error);
								System.Diagnostics.EventLog.WriteEntry(AppDomain.CurrentDomain.FriendlyName,strMessageFallBack,System.Diagnostics.EventLogEntryType.Error);
							}
							catch(Exception exx){//report the problem to fallback file
								strAction="Log EventLog failure to LogFileFallback";
								sb.Insert(0,TimeStamp+"\tCould not log message to EventLog when logging to the primry log file failed\r\nReason: ["+exx.Message+"], User=" + System.Environment.UserName + "\r\nMessage attempted to log:[");
								sb.Append("]");
								string strFailure=WriteLineToFileWithExceptionReturned(LogFileFallback,"\r\n"+sb.ToString());
									if(strFailure!=""){
										strAction="Create message to throw to caller";
										strMessageToThrowToCaller=
										"Failed to write to fallback log file ["+LogFileFallback+"] Error=["+strFailure
										+"\r\nAlso failed to write to either ["+logFile+"] OR Windows Event Log. Error:{"+strMessageFallBack
										+"}.\r\nTo enable writing to Event Log, start regedit.exe (regedt32.exe in older Windows) tool and navigate to "
										+@"//HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Eventlog key. Select Permissions in Security menu (on top) or in context menu "
										+"and add ASPNET (for website) or another account you are using for service, etc.\r\n"
										+"This account also needs Write permission for directory configured for the log file, and ideally for directory where this dll resides, for fail-back logging.";
									}
							}
							#endregion
						}//if(strMessageFallBack!="")
					}
					
					
				}
				#endregion
				
				#region ALERTS
				if((int)logLevel <= _intAlertsLogLevel){//We do not bother with local log levels when sending alerts. Local log levels are for active troubleshooting.
					bool blnSend=false;
					#region Figure out message key
					string strKey="";
					strAction="Figure out message key";
					if(messageID.Length > 0){
						strKey=messageID;
					}
					else{
						if(_blnAlertsUseMessageText){
							if(message.Length > MESSAGE_RECOGNITION_LEN){
								strKey=message.Substring(0,MESSAGE_RECOGNITION_LEN);
							}
							else{strKey=message;}
						}
					}
					#endregion
					#region Update Journal with new key
					strAction="Update Journal with new key";
					if (strKey.Length > 0) {//we can surpress repetitive alerts from this piece of code
						if(_htbAlerts.ContainsKey(strKey)){
							DateTime dtm = (DateTime)_htbAlerts[strKey];
							if(dtm.AddMinutes(_dblAlertResetIntervalMinutes)<DateTime.Now){
								//Update the key
								_htbAlerts[strKey]=DateTime.Now;
								blnSend=true;
							}
						}
						else{
							_htbAlerts.Add(strKey,DateTime.Now);
							blnSend=true;
						}
					}
					#endregion
					#region Ensure that the journal does not grow too big
					if(_htbAlerts.Count>_intAlertsJournalSize){
						DateTime dtmMinDate = DateTime.MaxValue;
						string strMinDateKey = "";
						DateTime dtmCutOff = DateTime.Now.AddMinutes(-_dblAlertResetIntervalMinutes);
						string strKeysToRemove="";
						strAction="List Journal messages to remove";
						System.Collections.IDictionaryEnumerator enu = _htbAlerts.GetEnumerator();
						try{
							while(enu.MoveNext()){
								DateTime dtmCurr = (DateTime)enu.Value;
								string strCurr = (string)enu.Key;
								if(dtmCurr<dtmCutOff){
									strKeysToRemove+="|"+strCurr;
								}
								if(dtmCurr<dtmMinDate){
									dtmMinDate=dtmCurr;
									strMinDateKey=strCurr;
								}
							}
						}
						catch{}//concurrent process may invalidate the enumerator at any time. Let it be so.
						enu=null;
						
						strAction="Remove old messages from Journal";
						if(strKeysToRemove.Length>0){
							char[] chaSeparator = {'|'};
							string[] strKeysBeRemoved = strKeysToRemove.Remove(0,1).Split(chaSeparator);
							for(int i=0;i<strKeysBeRemoved.Length;i++){
								try{
									_htbAlerts.Remove(strKeysBeRemoved[i]);
								}
								catch{}
							}
						}
						else{//There are no obsolete keys. Remove the oldest one
							try{
								_htbAlerts.Remove(strMinDateKey);
							}
							catch{}
						}
					}
					#endregion
					
					#region Send the Alert
					if(blnSend){
						strAction="Send SMTP message";
						string strSmtpErrors;
						bool blnSmtpSent=SendSMTP(_strAlertsRecipients,"EXCEPTION OCCURED"
						,System.AppDomain.CurrentDomain.FriendlyName + " at " + System.Environment.MachineName + ":\r\n\r\n"
						+sb.ToString()+ "\r\nSee log file "+ logFile+" for details.\r\n\r\n\r\n\r\n"
						,MailPriority.High, out strSmtpErrors);
						if (!blnSmtpSent ||strSmtpErrors!="") {
							if(!_blnLogFileSizeExceeded){
							strAction="Attempt writing SMTP failure to "+LogFilePathName;
								if(blnSmtpSent){
	//								WriteLineToFile(LogFilePathName,"\r\n"+TimeStamp+"\tThe Alert had been sent, but Primary SMTP servers failed: "+
									WriteLineToFileWithExceptionReturned(LogFilePathName,"\r\n"+TimeStamp+"\tThe Alert had been sent, but Primary SMTP servers failed: "+
									strSmtpErrors+"\r\n");//We do not care if the log file is accesible or not. We worried about this before
									//So we can warn about this problem by email
									strAction="Attempt sending SMTP failure by email";
									SendSMTP(_strAlertsRecipients,"EXCEPTION OCCURED"
									,System.AppDomain.CurrentDomain.FriendlyName + " at " + System.Environment.MachineName 
									+":\r\n\r\nCan't send messages using primary SMTP servers:\r\n"+strSmtpErrors
									+"\r\nSee log file "+ logFile+" for details.\r\n\r\n\r\n\r\n"
									,MailPriority.High, out strSmtpErrors);
								}
								else{
	//								WriteLineToFile(LogFilePathName,"\r\n"+TimeStamp+"\tCOULD NOT SEND ALERT: "+
									WriteLineToFileWithExceptionReturned(LogFilePathName,"\r\n"+TimeStamp+"\tCOULD NOT SEND ALERT: "+
									strSmtpErrors+"\r\n");//We do not care if the log file is accesible or not. We worried about this before
								}
							}
						}
					}
					#endregion
				}
				#endregion
			}
			catch(Exception e){//here we catch any unexpected exception
				sb=new System.Text.StringBuilder(TimeStamp);
				sb.Append("\tUNEXPECTED ERROR.\r\nAction=[");
				sb.Append(strAction);sb.Append("]\r\nError=[");sb.Append(e.Message);sb.Append("], User=");sb.Append(System.Environment.UserName);
				sb.Append("\rLocationce=[");sb.Append(e.Source);sb.Append("]\r\nStack Trace=[");sb.Append(e.StackTrace);sb.Append("]");
//				WriteLineToFile("FallBack.log",strMessage);
				WriteLineToFileWithExceptionReturned(LogFileFallback,sb.ToString());
			}
			if(strMessageToThrowToCaller!="")throw new Exception(strMessageToThrowToCaller);
			
			#region Show Pop-up desktop form if asked and warranted
			if(_blnInteractive && (int)logLevel<3){//Warnings, Errors and Fatal
				frmError objErrorForm = new frmError();
				objErrorForm.txtError.Text = sb.ToString();
				objErrorForm.butViewLog.Tag=_strLogFilePathName;
				objErrorForm.ShowDialog();
			}
			#endregion
		}

		#region LogMessage overloads
		/// <summary>
		/// Logs message to file. Depending on Alerts configuration, this may also result in sending an SMTP message.
		/// </summary>
		/// <param name="message">Message text to be logged (and may be sent)</param>
		/// <param name="logFile">Log file. If omitted, LogFilePathName, AppSettings["LogFile"] or Log.Log files will be used</param>
		/// <param name="surroundWithEmptyLines">Inserts empty lines before and after the message when writing to the log.</param>
		/// <param name="logLevel">Defines what the minimum level of logging should be configured (see LogLevel property) in order to log this message and AlertsLogLevel property to mail this message. This allows changing the level of details logged without changing and recompiling the application.Default=ExceptionsOnly</param>
		/// <param name="localLogLevel">Redefines global log level for this particular call</param>
		/// <param name="messageID">Unique ID of the message. It is required so that the message does not cause Alert more than once in a period configured (AlertResetIntervalMinutes property)</param>
		public static void LogMessage(string message, string logFile, bool surroundWithEmptyLines, LogLevels logLevel, int localLogLevel, string messageID){
			if(surroundWithEmptyLines){
				LogMessage(message,logFile,enuBlankLines.DoubleTopAndBottom,logLevel,localLogLevel,messageID);
			}
			else{
				LogMessage(message,logFile,enuBlankLines.None,logLevel,localLogLevel,messageID);
			}
		}

		/// <summary>
		/// Logs message to file. Depending on Alerts configuration, this may also result in sending an SMTP message.
		/// </summary>
		/// <param name="message">Message text to be logged (and may be sent)</param>
		/// <param name="logFile">Log file. If omitted, LogFilePathName, AppSettings["LogFile"] or Log.Log files will be used</param>
		/// <param name="surroundWithEmptyLines">Inserts empty lines before and after the message when writing to the log.</param>
		/// <param name="logLevel">Defines what the minimum level of logging should be configured (see LogLevel property) in order to log this message and AlertsLogLevel property to mail this message. This allows changing the level of details logged without changing and recompiling the application.Default=ExceptionsOnly</param>
		public static void LogMessage(string message, string logFile, bool surroundWithEmptyLines, LogLevels logLevel){
			if(surroundWithEmptyLines){
				LogMessage(message,logFile,enuBlankLines.DoubleTopAndBottom,logLevel,-1,"");
			}
			else{
				LogMessage(message,logFile,enuBlankLines.None,logLevel,-1,"");
			}
			//LogMessage(message,logFile,surroundWithEmptyLines,logLevel,-1,"");
		}
		/// <summary>
		/// Logs message to file. Depending on Alerts configuration, this may also result in sending an SMTP message.
		/// </summary>
		/// <param name="message">Message text to be logged (and may be sent)</param>
		/// <param name="surroundWithEmptyLines">Inserts empty lines before and after the message when writing to the log.</param>
		/// <param name="logLevel">Defines what the minimum level of logging should be configured (see LogLevel property) in order to log this message and AlertsLogLevel property to mail this message. This allows changing the level of details logged without changing and recompiling the application.Default=ExceptionsOnly</param>
		/// <param name="messageID">Unique ID of the message. It is required so that the message does not cause Alert more than once in a period configured (AlertResetIntervalMinutes property)</param>
		public static void LogMessage(string message, bool surroundWithEmptyLines, LogLevels logLevel, string messageID){
			if(surroundWithEmptyLines){
				LogMessage(message,null,enuBlankLines.DoubleTopAndBottom,logLevel,-1,"");
			}
			else{
				LogMessage(message,null,enuBlankLines.None,logLevel,-1,"");
			}
		}
		/// <summary>
		/// Logs message to file. Depending on Alerts configuration, this may also result in sending an SMTP message.
		/// </summary>
		/// <param name="message">Message text to be logged (and may be sent)</param>
		/// <param name="logFile">Log file. If omitted, LogFilePathName, AppSettings["LogFile"] or Log.Log files will be used</param>
		public static void LogMessage(string message, string logFile){
			LogMessage(message,logFile,enuBlankLines.None,LogLevels.Fatal,-1,"");
			//LogMessage(message,GetLogFilePathName(),false,LogLevels.Fatal,-1,"");
		}
		/// <summary>
		/// Logs message to file. Depending on Alerts configuration, this may also result in sending an SMTP message.
		/// </summary>
		/// <param name="message">Message text to be logged (and may be sent)</param>
		/// <param name="surroundWithEmptyLines">Inserts empty lines before and after the message when writing to the log.</param>
		public static void LogMessage(string message, bool surroundWithEmptyLines){
			if(surroundWithEmptyLines){
				LogMessage(message,null,enuBlankLines.DoubleTopAndBottom,LogLevels.Fatal,-1,"");
			}
			else{
				LogMessage(message,null,enuBlankLines.None,LogLevels.Fatal,-1,"");
			}
		}
		/// <summary>
		/// Logs message to file. Depending on Alerts configuration, this may also result in sending an SMTP message.
		/// </summary>
		/// <param name="message">Message text to be logged (and may be sent)</param>
		/// <param name="logLevel">Defines what the minimum level of logging should be configured (see LogLevel property) in order to log this message and AlertsLogLevel property to mail this message. This allows changing the level of details logged without changing and recompiling the application.Default=ExceptionsOnly</param>
		public static void LogMessage(string message, LogLevels logLevel){
			LogMessage(message,null,enuBlankLines.None,logLevel,-1,"");
		}
		/// <summary>
		/// Logs message to file. Depending on Alerts configuration, this may also result in sending an SMTP message.
		/// </summary>
		/// <param name="message">Message text to be logged (and may be sent)</param>
		/// <param name="logLevel">Defines what the minimum level of logging should be configured (see LogLevel property) in order to log this message and AlertsLogLevel property to mail this message. This allows changing the level of details logged without changing and recompiling the application.Default=ExceptionsOnly</param>
		/// <param name="localLogLevel">Redefines global log level for this particular call</param>
		public static void LogMessage(string message,LogLevels logLevel,int localLogLevel) {
			LogMessage(message,null,enuBlankLines.None,logLevel,localLogLevel,"");
		}

		/// <summary>
		/// Logs message to file. Depending on Alerts configuration, this may also result in sending an SMTP message.
		/// </summary>
		/// <param name="message">Message text to be logged (and may be sent)</param>
		/// <param name="surroundWithEmptyLines">Inserts empty lines before and after the message when writing to the log.</param>
		/// <param name="logLevel">Defines what the minimum level of logging should be configured (see LogLevel property) in order to log this message and AlertsLogLevel property to mail this message. This allows changing the level of details logged without changing and recompiling the application.Default=ExceptionsOnly</param>
		public static void LogMessage(string message, bool surroundWithEmptyLines, LogLevels logLevel){
			if(surroundWithEmptyLines){
				LogMessage(message,null,enuBlankLines.DoubleTopAndBottom,logLevel,-1,"");
			}
			else{
				LogMessage(message,null,enuBlankLines.None,logLevel,-1,"");
			}
		}

		/// <summary>
		/// Logs message to file. Depending on Alerts configuration, this may also result in sending an SMTP message.
		/// </summary>
		/// <param name="message">Message text to be logged (and may be sent)</param>
		/// <param name="surroundWithEmptyLines">Inserts empty lines before and after the message when writing to the log.</param>
		/// <param name="logLevel">Defines what the minimum level of logging should be configured (see LogLevel property) in order to log this message and AlertsLogLevel property to mail this message. This allows changing the level of details logged without changing and recompiling the application.Default=ExceptionsOnly</param>
		/// <param name="localLogLevel">Redefines global log level for this particular call</param>
		public static void LogMessage(string message,bool surroundWithEmptyLines,LogLevels logLevel,int localLogLevel) {
			if(surroundWithEmptyLines){
				LogMessage(message,null,enuBlankLines.DoubleTopAndBottom,logLevel,localLogLevel,"");
			}
			else{
				LogMessage(message,null,enuBlankLines.None,logLevel,localLogLevel,"");
			}
		}
		/// <summary>
		/// Logs message to file. Depending on Alerts configuration, this may also result in sending an SMTP message.
		/// </summary>
		/// <param name="message">Message text to be logged (and may be sent)</param>
		/// <param name="surroundWithEmptyLines">Inserts empty lines before and after the message when writing to the log.</param>
		/// <param name="logLevel">Defines what the minimum level of logging should be configured (see LogLevel property) in order to log this message and AlertsLogLevel property to mail this message. This allows changing the level of details logged without changing and recompiling the application.Default=ExceptionsOnly</param>
		/// <param name="localLogLevel">Redefines global log level for this particular call</param>
		/// <param name="includeHttpRequestData">Includes HTTP query and form data along with the message</param>
		public static void LogMessage(string message,bool surroundWithEmptyLines,LogLevels logLevel,int localLogLevel,bool includeHttpRequestData){
			//Immediately check if we are logging before collecting any info. Collecting the request data is expensive
			if(_blnUseLowestLogLevelIfLocalLevelConfigured && localLogLevel>0){//If it is 0 it means it is not set and -1 is passed to this proc when the param is omitted
				if(((int)logLevel>_intLogLevel||(int)logLevel>localLogLevel) && (int)logLevel>_intAlertsLogLevel) return;
			}
			else{
				if((int)logLevel>_intLogLevel && (int)logLevel>localLogLevel && (int)logLevel>_intAlertsLogLevel) return;
			}
			if(includeHttpRequestData)message+=Environment.NewLine+GetWebRequestData();
			if(surroundWithEmptyLines){
				LogMessage(message,null,enuBlankLines.DoubleTopAndBottom,logLevel,localLogLevel,"");
			}
			else{
				LogMessage(message,null,enuBlankLines.None,logLevel,localLogLevel,"");
			}
		}

		/// <summary>
		/// Logs message to file. Depending on Alerts configuration, this may also result in sending an SMTP message.
		/// </summary>
		/// <param name="message">Message text to be logged (and may be sent)</param>
		/// <param name="logLevel">Defines what the minimum level of logging should be configured (see LogLevel property) in order to log this message and AlertsLogLevel property to mail this message. This allows changing the level of details logged without changing and recompiling the application.Default=ExceptionsOnly</param>
		/// <param name="localLogLevel">Redefines global log level for this particular call</param>
		/// <param name="insertBlankLines">How you want the blank lines being inserted</param>
		public static void LogMessage(string message, LogLevels logLevel,int localLogLevel,enuBlankLines insertBlankLines){
			LogMessage(message,null,insertBlankLines,logLevel,localLogLevel,"");
		}

		#region Debug
		/// <summary>
		/// Most verbose level 9. The code is typically placed around pieces of code that are prone to problems, as well as logging changes of vital values
		/// </summary>
		/// <param name="message">Message text to be logged (and may be sent)</param>
		/// <param name="localLogLevel">Redefines global log level for this particular call</param>
		/// <param name="localLogFile">Log file if needs to be different from Application-wide. If omitted, globally-configured LogFilePathName, AppSettings["LogFile"] or LogForUnconfiguredProcess.log files will be used</param>
		public static void Debug(string message,int localLogLevel=-1,string localLogFile=null){
			LogMessage(message,localLogFile,enuBlankLines.None,LogLevels.Debug,localLogLevel,"");
		}
		#endregion

		#region Parameters
		/// <summary>
		/// Best placed at the top of procedures. Include parameters values still unaffected.
		/// </summary>
		/// <param name="message">Message text to be logged (and may be sent)</param>
		/// <param name="localLogLevel">Redefines global log level for this particular call</param>
		/// <param name="localLogFile">Log file if needs to be different from Application-wide. If omitted, globally-configured LogFilePathName, AppSettings["LogFile"] or LogForUnconfiguredProcess.log files will be used</param>
		public static void Parameters(string message,int localLogLevel=-1,string localLogFile=null) {
			LogMessage(message,localLogFile,enuBlankLines.None,LogLevels.Parameters,localLogLevel,"");
		}
		#endregion

		#region Call
		/// <summary>
		/// Best placed at the top of procedures. That allows accurate recording of time the procedure being called. Optionally you may want to include parameters values still unaffected.
		/// </summary>
		/// <param name="message">Message text to be logged (and may be sent)</param>
		/// <param name="localLogLevel">Redefines global log level for this particular call</param>
		/// <param name="localLogFile">Log file if needs to be different from Application-wide. If omitted, globally-configured LogFilePathName, AppSettings["LogFile"] or LogForUnconfiguredProcess.log files will be used</param>
		public static void Call(string message,int localLogLevel=-1,string localLogFile=null){
			LogMessage(message,localLogFile,enuBlankLines.None,LogLevels.Calls,localLogLevel,"");
		}
		#endregion

		#region Info
		/// <summary>
		/// Log informational message, not related to any particular procedure
		/// </summary>
		/// <param name="message">Message text to be logged (and may be sent)</param>
		/// <param name="localLogLevel">Redefines global log level for this particular call</param>
		/// <param name="localLogFile">Log file if needs to be different from Application-wide. If omitted, globally-configured LogFilePathName, AppSettings["LogFile"] or LogForUnconfiguredProcess.log files will be used</param>
		public static void Info(string message,int localLogLevel=-1,string localLogFile=null){
			LogMessage(message,localLogFile,enuBlankLines.None,LogLevels.Information,localLogLevel,"");
		}
		#endregion

		#region Warn
		/// <summary>
		/// Log a condition that does not constitute an Exception, but still concerning.
		/// </summary>
		/// <param name="message">Message text to be logged (and may be sent)</param>
		/// <param name="localLogLevel">Redefines global log level for this particular call</param>
		/// <param name="localLogFile">Log file if needs to be different from Application-wide. If omitted, globally-configured LogFilePathName, AppSettings["LogFile"] or LogForUnconfiguredProcess.log files will be used</param>
		public static void Warn(string message,int localLogLevel=-1,string localLogFile=null){
			LogMessage(message,localLogFile,enuBlankLines.None,LogLevels.Warnings,localLogLevel,"");
		}
		#endregion Warn

		#region Exception
		/// <summary>
		/// Log a condition that is not aFatal error, but rather an exception, which is typically anticipated and taken care of.
		/// </summary>
		/// <param name="message">Message text to be logged (and may be sent)</param>
		/// <param name="localLogLevel">Redefines global log level for this particular call</param>
		/// <param name="localLogFile">Log file if needs to be different from Application-wide. If omitted, globally-configured LogFilePathName, AppSettings["LogFile"] or LogForUnconfiguredProcess.log files will be used</param>
		public static void Exception(string message,int localLogLevel=-1,string localLogFile=null) {
			LogMessage(message,localLogFile,enuBlankLines.None,LogLevels.Exceptions,localLogLevel,"");
		}
		#endregion

		#region Fatal
		/// <summary>
		/// Error from which there is no recovery. This call is typically placed at the top of call stack so that all the stack is being captured and logged.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="localLogLevel">Redefines global log level for this particular call</param>
		/// <param name="localLogFile">Log file if needs to be different from Application-wide. If omitted, globally-configured LogFilePathName, AppSettings["LogFile"] or LogForUnconfiguredProcess.log files will be used</param>
		public static void Fatal(string message,int localLogLevel=-1,string localLogFile=null){
			LogMessage(message,localLogFile,enuBlankLines.None,LogLevels.Fatal,localLogLevel,"");
		}
		#endregion

		#endregion
		
		#endregion
		

		#region SendSMTP
		/// <summary>
		/// Sends plain text SMTP message using servers configured
		/// </summary>
		/// <param name="addresses">;(semicolon) delimited list of email addresses</param>
		/// <param name="subject">Subject of the SMTP message</param>
		/// <param name="body">Body of the SMTP message</param>
		/// <param name="priority">Priority of the message</param>
		/// <param name="errors">Error messages returned by the SMTP servers</param>
		/// <param name="useHtmlFormat">Should HTML format be used to render the message?</param>
		/// <param name="from">Specify if you want it different from what is configured as AlertsMailAccount</param>
		/// <param name="cc">;(semicolon) delimited list of Carbon Copy email addresses</param>
		/// <param name="bcc">;(semicolon) delimited list of Blind Carbon Copy email addresses (not seen to any recipient)</param>
		/// <param name="encoding">The default character set is "us-ascii". You may want UTF8 or something else.</param>
		/// <param name="attachments">Array of files to be attached to the message</param>
		/// <returns>true if the message had been sent. To get info about any SMTP server failure analyze the errors output parameter</returns>
		public static bool SendSMTP(string addresses,string subject,string body,MailPriority priority,out string errors
		,bool useHtmlFormat,string from,string cc,string bcc,System.Text.Encoding encoding,System.Collections.ArrayList attachments) {
			bool blnSmtpSent=false;
			string strFrom;
			errors="";
			#region Sanity check
			if(_strAlertsMailServers==null || _strAlertsMailServers==""){
				errors="CommonSenseSoft.Log: No SMTP server configured."; return false;
			}
			if(from==null || from.Trim()==""){
				if (_strAlertsMailAccount==null || _strAlertsMailAccount=="") {
					errors="CommonSenseSoft.Log: Neither the AlertsMailAccount property is configured, nor 'from' parameter is passed to this method."; return false;
				}
				else{strFrom = _strAlertsMailAccount;}
			}
			else{strFrom = from;}
			if (addresses==null) {
				errors="CommonSenseSoft.Log: No recipient email address is specified."; return false;
			}
			else{
				addresses=addresses.Trim();
				if(addresses.Replace(";","").Trim().Length<4) {//string of ; ; ; ; ; ; ; ; ; ; will also be considered empty
					errors="CommonSenseSoft.Log: No valid recipient email address is specified."; return false;
				}
				
			}
			#endregion
			
			#region Create and configure message
			System.Web.Mail.MailMessage Msg = new System.Web.Mail.MailMessage();
			Msg.From=strFrom;
			Msg.To=addresses;// this .NET class is OK with accepting semicolon-delimited lists of addresses
			Msg.Subject=subject;
			Msg.Body=body;
			if(useHtmlFormat){Msg.BodyFormat=System.Web.Mail.MailFormat.Html;}
			else{Msg.BodyFormat=System.Web.Mail.MailFormat.Text;}
			Msg.Priority=(System.Web.Mail.MailPriority)priority;
			if (cc!=null && cc.Length>3){Msg.Cc=cc;}
			if (bcc!=null && bcc.Length>3){Msg.Bcc=bcc;}
			Msg.BodyEncoding=encoding;
			if(attachments!=null){
				for(int i=0;i<attachments.Count;i++){
					//We are OK with throwing exception if there is no attachment
					Msg.Attachments.Add(new System.Web.Mail.MailAttachment(attachments[i].ToString()));
				}
			}
			#endregion
			
			#region Go through servers and try to send
			char[] chaDelimiters = { ',',';','|' };
			string[] strServers = _strAlertsMailServers.Split(chaDelimiters);
			for (int i=0;i<strServers.Length;i++) {
				DateTime dtmStart=DateTime.Now;
				try {
					if(strServers[i].Trim()==""){continue;}//Someone may stick delimiter in the end.
					if(strServers[i].ToLower()=="localhost"){
						System.Web.Mail.SmtpMail.SmtpServer="127.0.0.1";
					}
					else{
						System.Web.Mail.SmtpMail.SmtpServer=strServers[i];
					}
					System.Web.Mail.SmtpMail.Send(Msg);
					Msg = null;
					blnSmtpSent = true;
					break;
				}
				catch (Exception e) { errors+=strServers[i]+": "+e.Message.Trim()+" ("+dtmStart.ToString("mm:ss.f")+" - "+DateTime.Now.ToString("mm:ss.f")+"); "; }
			}
			#endregion
			
			if(errors!=""){
				if(blnSmtpSent){errors="Primary SMTP servers failure: "+errors;}
				else{errors="All SMTP servers failed: "+errors; }
				Logging.LogMessage(errors,LogLevels.Fatal);
			}
			return blnSmtpSent;
		}
		
		#region Overloads
		/// <summary>
		/// Sends plain text SMTP message using servers configured
		/// </summary>
		/// <param name="addresses">; or , or | delimited list of email addresses</param>
		/// <param name="subject">Subject of the SMTP message</param>
		/// <param name="body">Body of the SMTP message</param>
		/// <param name="priority">Priority of the message</param>
		/// <returns>true if the message had been sent. To get info about any SMTP server failure analyze the errors output parameter</returns>
		public static bool SendSMTP(string addresses,string subject,string body,MailPriority priority) {
			string strOut;
			return SendSMTP(addresses,subject,body,priority,out strOut,false,null,null,null,System.Text.Encoding.Default,null);
		}
		/// <summary>
		/// Sends plain text SMTP message using servers configured. Priority is Normal
		/// </summary>
		/// <param name="addresses">; or , or | delimited list of email addresses</param>
		/// <param name="subject">Subject of the SMTP message</param>
		/// <param name="body">Body of the SMTP message</param>
		/// <returns>true if the message had been sent. To get info about any SMTP server failure analyze the errors output parameter</returns>
		public static bool SendSMTP(string addresses,string subject,string body) {
			string strOut;
			return SendSMTP(addresses,subject,body,MailPriority.Normal,out strOut,false,null,null,null,System.Text.Encoding.Default,null);
		}

		/// <summary>
		/// Sends plain text SMTP message using servers configured
		/// </summary>
		/// <param name="addresses">; or , or | delimited list of email addresses</param>
		/// <param name="subject">Subject of the SMTP message</param>
		/// <param name="body">Body of the SMTP message</param>
		/// <param name="priority">Priority of the message</param>
		/// <param name="errors">Error messages returned by the SMTP servers</param>
		/// <returns>true if the message had been sent. To get info about any SMTP server failure analyze the errors output parameter</returns>
		public static bool SendSMTP(string addresses,string subject,string body,MailPriority priority,out string errors) {
			return SendSMTP(addresses,subject,body,priority,out errors,false,null,null,null,System.Text.Encoding.Default,null);
		}
		/// <summary>
		/// Sends SMTP message using servers configured
		/// </summary>
		/// <param name="addresses">;(semicolon) delimited list of email addresses</param>
		/// <param name="subject">Subject of the SMTP message</param>
		/// <param name="body">Body of the SMTP message</param>
		/// <param name="priority">Priority of the message</param>
		/// <param name="errors">Error messages returned by the SMTP servers</param>
		/// <param name="useHtmlFormat">Should HTML format be used to render the message?</param>
		/// <returns>true if the message had been sent. To get info about any SMTP server failure analyze the errors output parameter</returns>
		public static bool SendSMTP(string addresses,string subject,string body,MailPriority priority,out string errors
		,bool useHtmlFormat) {
			return SendSMTP(addresses,subject,body,priority,out errors,useHtmlFormat,null,null,null,System.Text.Encoding.Default,null);
		}
		/// <summary>
		/// Sends SMTP message using servers configured
		/// </summary>
		/// <param name="addresses">;(semicolon) delimited list of email addresses</param>
		/// <param name="subject">Subject of the SMTP message</param>
		/// <param name="body">Body of the SMTP message</param>
		/// <param name="priority">Priority of the message</param>
		/// <param name="errors">Error messages returned by the SMTP servers</param>
		/// <param name="useHtmlFormat">Should HTML format be used to render the message?</param>
		/// <param name="from">Specify if you want it different from what is configured as AlertsMailAccount</param>
		/// <returns>true if the message had been sent. To get info about any SMTP server failure analyze the errors output parameter</returns>
		public static bool SendSMTP(string addresses,string subject,string body,MailPriority priority,out string errors
		,bool useHtmlFormat,string from) {
			return SendSMTP(addresses,subject,body,priority,out errors,useHtmlFormat,from,null,null,System.Text.Encoding.Default,null);
		}
		/// <summary>
		/// Sends SMTP message using servers configured
		/// </summary>
		/// <param name="addresses">;(semicolon) delimited list of email addresses</param>
		/// <param name="subject">Subject of the SMTP message</param>
		/// <param name="body">Body of the SMTP message</param>
		/// <param name="priority">Priority of the message</param>
		/// <param name="errors">Error messages returned by the SMTP servers</param>
		/// <param name="useHtmlFormat">Should HTML format be used to render the message?</param>
		/// <param name="from">Specify if you want it different from what is configured as AlertsMailAccount</param>
		/// <param name="cc">;(semicolon) delimited list of Carbon Copy email addresses</param>
		/// <returns>true if the message had been sent. To get info about any SMTP server failure analyze the errors output parameter</returns>
		public static bool SendSMTP(string addresses,string subject,string body,MailPriority priority,out string errors
		,bool useHtmlFormat,string from,string cc) {
			return SendSMTP(addresses,subject,body,priority,out errors,useHtmlFormat,from,cc,null,System.Text.Encoding.Default,null);
		}
		/// <summary>
		/// Sends SMTP message using servers configured
		/// </summary>
		/// <param name="addresses">;(semicolon) delimited list of email addresses</param>
		/// <param name="subject">Subject of the SMTP message</param>
		/// <param name="body">Body of the SMTP message</param>
		/// <param name="priority">Priority of the message</param>
		/// <param name="errors">Error messages returned by the SMTP servers</param>
		/// <param name="useHtmlFormat">Should HTML format be used to render the message?</param>
		/// <param name="from">Specify if you want it different from what is configured as AlertsMailAccount</param>
		/// <param name="cc">;(semicolon) delimited list of Carbon Copy email addresses</param>
		/// <param name="bcc">;(semicolon) delimited list of Blind Carbon Copy email addresses (not seen to any recipient)</param>
		/// <returns>true if the message had been sent. To get info about any SMTP server failure analyze the errors output parameter</returns>
		public static bool SendSMTP(string addresses,string subject,string body,MailPriority priority,out string errors
		,bool useHtmlFormat,string from,string cc,string bcc) {
			return SendSMTP(addresses,subject,body,priority,out errors,useHtmlFormat,from,cc,bcc,System.Text.Encoding.Default,null);
		}
		/// <summary>
		/// Sends SMTP message using servers configured
		/// </summary>
		/// <param name="addresses">;(semicolon) delimited list of email addresses</param>
		/// <param name="subject">Subject of the SMTP message</param>
		/// <param name="body">Body of the SMTP message</param>
		/// <param name="priority">Priority of the message</param>
		/// <param name="errors">Error messages returned by the SMTP servers</param>
		/// <param name="useHtmlFormat">Should HTML format be used to render the message?</param>
		/// <param name="from">Specify if you want it different from what is configured as AlertsMailAccount</param>
		/// <param name="cc">;(semicolon) delimited list of Carbon Copy email addresses</param>
		/// <param name="bcc">;(semicolon) delimited list of Blind Carbon Copy email addresses (not seen to any recipient)</param>
		/// <param name="encoding">The default character set is "us-ascii". You may want UTF8 or something else.</param>
		/// <returns>true if the message had been sent. To get info about any SMTP server failure analyze the errors output parameter</returns>
		public static bool SendSMTP(string addresses,string subject,string body,MailPriority priority,out string errors
		,bool useHtmlFormat,string from,string cc,string bcc,System.Text.Encoding encoding) {
			return SendSMTP(addresses,subject,body,priority,out errors,useHtmlFormat,from,cc,bcc,encoding,null);
		}
		/// <summary>
		/// Sends SMTP message using servers configured
		/// </summary>
		/// <param name="addresses">;(semicolon) delimited list of email addresses</param>
		/// <param name="subject">Subject of the SMTP message</param>
		/// <param name="body">Body of the SMTP message</param>
		/// <param name="priority">Priority of the message</param>
		/// <param name="useHtmlFormat">Should HTML format be used to render the message?</param>
		/// <param name="from">Specify if you want it different from what is configured as AlertsMailAccount</param>
		/// <param name="cc">;(semicolon) delimited list of Carbon Copy email addresses</param>
		/// <param name="bcc">;(semicolon) delimited list of Blind Carbon Copy email addresses (not seen to any recipient)</param>
		/// <param name="encoding">The default character set is "us-ascii". You may want UTF8 or something else.</param>
		/// <param name="attachments">Semicolon (;)-delimited string of files to be attached to the message</param>
		/// <param name="errors">Error messages returned by the SMTP servers</param>
		/// <returns>true if the message had been sent. To get info about any SMTP server failure analyze the errors output parameter</returns>
		public static bool SendSMTP(string addresses,string subject,string body,MailPriority priority
		,bool useHtmlFormat,string from,string cc,string bcc,System.Text.Encoding encoding,string attachments,out string errors) {
			char[] chaDelimiter={';'};
			System.Collections.ArrayList arl = null;
			if(attachments!=null && attachments!=""){
				arl=new ArrayList();
				string[] strAttachments = attachments.Trim().Split(chaDelimiter);
				for(int i=0;i<strAttachments.Length;i++){
					string strAttachment = strAttachments[i].Trim();
					if(strAttachment!=""){
						arl.Add(strAttachment);
					}
				}
			}
			return SendSMTP(addresses,subject,body,priority,out errors,useHtmlFormat,from,cc,bcc,encoding,arl);
		}
		#endregion
		#endregion
		
		#region InitializeFromAppConfig
		/// <summary>
		/// Call this method when starting your application, and all the properties of this class will be initialized based on appSettings section of your App.config or Web.config 
		/// The keys you need in your appSettings section should have the same names as properties of this class, case sensitive.
		/// </summary>
		public static void InitializeFromAppConfig(){
			string strAction="";
			try{
				strAction="Read LogFilePathName";
				if(ConfigurationManager.AppSettings["LogFilePathName"]!=null){
					if(ConfigurationManager.AppSettings["LogFilePathName"].ToString().Trim()!=""){
						_strLogFilePathName=ConfigurationManager.AppSettings["LogFilePathName"].ToString();
					}
				}
				strAction="Read LogFileSizeLimitMb";
				LogFileSizeLimitMb=ToShort(ConfigurationManager.AppSettings["LogFileSizeLimitMb"],2);
				strAction="Read LogFileSizeThresholdMb";
				LogFileSizeThresholdMb=ToShort(ConfigurationManager.AppSettings["LogFileSizeThresholdMb"],1);
				strAction="Read ProcessMemoryThresholdKb";
				_intProcessMemoryThresholdKb=ToInt(ConfigurationManager.AppSettings["ProcessMemoryThresholdKb"],1024);
				strAction="Read UseDateForLogFileName";
				_blnUseDateForLogFileName=ToBoolean(ConfigurationManager.AppSettings["UseDateForLogFileName"]);
				strAction="Read LogLevel";
				_intLogLevel=ToInt(ConfigurationManager.AppSettings["LogLevel"],2);
				strAction="Read AlertsLogLevel";
				_intAlertsLogLevel=ToInt(ConfigurationManager.AppSettings["AlertsLogLevel"],2);
				strAction="Read AlertResetIntervalMinutes";
				_dblAlertResetIntervalMinutes=ToInt(ConfigurationManager.AppSettings["AlertResetIntervalMinutes"],30);
				strAction="Read AlertsJournalSize";
				_intAlertsJournalSize=ToInt(ConfigurationManager.AppSettings["AlertsJournalSize"],100);
				strAction="Read AlertsMailAccount";
				_strAlertsMailAccount=ConfigurationManager.AppSettings["AlertsMailAccount"].ToString();
				strAction="Read AlertsMailServers";
				_strAlertsMailServers=ConfigurationManager.AppSettings["AlertsMailServers"].ToString();
				strAction="Read AlertsRecipients";
				_strAlertsRecipients=ConfigurationManager.AppSettings["AlertsRecipients"].ToString();
				_blnAlertsUseMessageText=true;
				strAction="Read UseUTC";
				_blnUseUTC=ToBoolean(ConfigurationManager.AppSettings["UseUTC"]);
				strAction="Read ApplicationName";
				if(ConfigurationManager.AppSettings["ApplicationName"]!=null){
					if(ConfigurationManager.AppSettings["ApplicationName"].ToString().Trim()!=""){
						ApplicationName=ConfigurationManager.AppSettings["ApplicationName"].ToString();
					}
				}
				Interactive=ToBoolean(ConfigurationManager.AppSettings["Interactive"]);
				
				if(ConfigurationManager.AppSettings["UseLowestLogLevel"]!=null){
					if(ConfigurationManager.AppSettings["UseLowestLogLevel"].ToString().Trim()!=""){
						_blnUseLowestLogLevelIfLocalLevelConfigured=ToBoolean(ConfigurationManager.AppSettings["UseLowestLogLevel"]);
					}
				}
				if(ConfigurationManager.AppSettings["SuppressAssembliesOutput"]!=null){
					if(ConfigurationManager.AppSettings["SuppressAssembliesOutput"].ToString().Trim()!=""){
						_blnSuppressAssembliesOutput=ToBoolean(ConfigurationManager.AppSettings["SuppressAssembliesOutput"]);
					}
				}
			}
			catch(Exception e){
				LogException(strAction,e,true);
			}
		}
		#endregion
		
		#region CheckLogFileSize
		/// <summary>
		/// Checks the size of physical Log File against the LogFileSizeThresholdMb setting. Will log a warning if the size has exceeded the threshold.
		/// </summary>
		public static void CheckLogFileSize(){
			System.IO.FileInfo fi;
			if(System.IO.File.Exists(Logging.LogFilePathName)) {
				fi=new System.IO.FileInfo(Logging.LogFilePathName);
				if(fi.Length>_srtLogFileSizeThresholdMb*1024*1024) {
					Logging.LogMessage("Log file size has reached "
						+(fi.Length/1024).ToString()+" Kb, which exceeds "+_srtLogFileSizeThresholdMb.ToString()+" Mb threshold.",true,LogLevels.Warnings);
				}
			}
		}
		#endregion

		#region CheckProcessMemory
		/// <summary>
		/// Checks the process memory. Always Logs it for LogLevels.Information. Also logs it for LogLevels.Warnings when it exceeds mintProcessMemoryThresholdKb
		/// </summary>
		public static void CheckProcessMemory(){
			if(_intProcessMemoryThresholdKb<=0)return;
			System.Diagnostics.Process P=System.Diagnostics.Process.GetCurrentProcess();
            long lngRamUsed=P.WorkingSet64/1024;
			long lngVmUsed=P.PrivateMemorySize64/1024;
            Logging.Info("Process' RAM use="+lngRamUsed.ToString("###,###,###,###,###,##0")+"Kb, Working Set="+lngVmUsed.ToString("###,###,###,###,###,##0")
			+"Kb, Threads="+System.Diagnostics.Process.GetCurrentProcess().Threads.Count.ToString());
			if(lngVmUsed>_intProcessMemoryThresholdKb) {
				Logging.LogMessage("Process has exceeded Memory threshhold. RAM use="+lngRamUsed.ToString("###,###,###,###,###,##0")+"Kb, Working Set="+lngVmUsed.ToString("###,###,###,###,###,##0")+"Kb",true,LogLevels.Warnings);
			}
		}
		#endregion

		#region GetWebRequestData
		/// <summary>
		/// Returns string of all important data about web request
		/// </summary>
		public static string GetWebRequestData(System.Web.HttpRequest request=null){
			System.Text.StringBuilder sb=new System.Text.StringBuilder();
			if(request==null){
				if(System.Web.HttpContext.Current==null){
					sb.AppendLine("Unfortunately, System.Web.HttpContext.Current==null");
				}
				else{
					if(System.Web.HttpContext.Current.Request==null){
						sb.AppendLine("Unfortunately, System.Web.HttpContext.Current.Request==null");
					}
					else{
						request=System.Web.HttpContext.Current.Request;
					}
				}
			}
			if(request!=null){
				sb.Append("URL=[");sb.Append(NullSafe(request.Url));sb.AppendLine("]");
				sb.Append("Referrer=[");sb.Append(NullSafe(request.UrlReferrer));sb.AppendLine("]");
				sb.Append("Host=[");sb.Append(NullSafe(request.UserHostAddress));sb.AppendLine("]");
				sb.Append("Agent=[");sb.Append(NullSafe(request.UserAgent));sb.AppendLine("]");
				sb.Append("PhysicalPath=[");
					try{
						sb.Append(NullSafe(request.PhysicalPath));
					}
					catch(Exception ep){
						sb.Append("Exception getting Request.PhysicalPath:{");sb.Append(ep.Message);sb.Append("}");
					}
				sb.AppendLine("]");

				#region Collect Browser's capabilities
				System.Web.HttpBrowserCapabilities b = request.Browser;
				//sb.AppendLine(b[""]);//Gives the standard string, same as "Agent"
				//sb.Append("~Cook:");sb.Append(Parse.ToByte(b["cookies"],0));
				//sb.Append("~Crawler:");sb.Append(Parse.ToByte(b["crawler"],0));
				//sb.Append("~Mob:");sb.Append(Parse.ToByte(b["isMobileDevice"],0));
				//sb.Append("~MobManif=");sb.Append(b["mobileDeviceManufacturer"].Replace("Unknown",""));
				sb.Append("BrowserType=[");sb.Append(NullSafe(b.Type));sb.Append("], BrowserVersion=[");sb.Append(NullSafe(b.Version));
				sb.Append("], Platform=[");sb.Append(NullSafe(b.Platform));sb.AppendLine("],");
				
				sb.Append("EcmVer=[");sb.Append(NullSafe(b["ecmascriptversion"]).Replace("Unknown",""));sb.Append("], JS=[");
				sb.Append(NullSafe(b["javascript"]).Replace("Unknown",""));sb.Append("], JsVer=[");sb.Append(NullSafe(b["javascriptversion"]).Replace("Unknown",""));sb.AppendLine("],");
				
				sb.Append("InputType=[");sb.Append(NullSafe(b.InputType));sb.Append("], CanSendMail=[");sb.Append(NullSafe(b.CanSendMail));sb.Append("], CanCall=[");sb.Append(NullSafe(b.CanInitiateVoiceCall));sb.AppendLine("],");
				
				sb.Append("MobModel=[");sb.Append(NullSafe(b["mobileDeviceModel"]).Replace("Unknown",""));
				sb.Append("], Gateway=[");sb.Append(NullSafe(b.GatewayVersion));sb.AppendLine("]");
				#endregion
			
				#region Try to collect Forms data
				try{
					if(request.Form!=null){
						sb.Append("Form Data=[");
						for(int i=0;i<request.Form.AllKeys.Length;i++){
							if(request.Form.AllKeys[i].StartsWith("__"))continue;//we do not need that hashed rubbish
							sb.Append(Environment.NewLine);
							sb.Append("\t");
							sb.Append(request.Form.AllKeys[i]);
							sb.Append("={");
							try{sb.Append(request.Form[i]);}
							catch(Exception ef){sb.Append("ERROR READING THIS KEY:["+ef.Message+"]");}
							sb.Append("}");
						}
						sb.Append(Environment.NewLine);
						sb.Append("]");
					}
				}
				catch(Exception he){sb.Append(Environment.NewLine+"ERROR reading request headers:["+he.Message+"]");}
				#endregion
				sb.AppendLine();
			}
			return sb.ToString();
		}
		#endregion

		#region GetAssemblyDirectory
		/// <summary>
		/// Returns directory where the executing assembly resides. Useful when storing configs and data in the same directory
		/// </summary>
		/// <returns>Full path to directory where executing assembly resides</returns>
		static public string GetAssemblyDirectory() {
			string path = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
			int intStartIndex = path.IndexOf("///");
			if(intStartIndex>-1) {
				int intEndIndex = path.LastIndexOf("/");
				path = path.Substring(intStartIndex + 3,intEndIndex - intStartIndex - 3);
				path = path.Replace("/","\\");
			}
			else {
				Exception ex = new Exception("Assembly Path [" + path + "] has unexpected format.");
				throw ex;
			}
			return path;
		}
		#endregion

		#region AppendChangeTrace
		/// <summary>
		/// Returns Change trace in a standard way with ODBC canonical timestamp and delimiters already cared for.
		/// </summary>
		/// <param name="existingText">Text which is already there</param>
		/// <param name="textToAppend">Text to append in front of existing text</param>
		/// <param name="affectingUser">User making the change</param>
		public static string AppendChangeTrace(string existingText,string textToAppend,string affectingUser=null){
			return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")+(string.IsNullOrEmpty(affectingUser)?"":" User: "+affectingUser)+" "+textToAppend+"; "+existingText??"";
		}
		#endregion
			
		#region PRIVATE MEMBERS
		#region GetLogFilePathName
		static string GetLogFilePathName(string localPathName){
			if(localPathName==null || localPathName==""){//No request for a separate pathname
				if (_strLogFilePathName == ""){//Take the opportunity to set it
					_strLogFilePathName = GetAssemblyDirectory() + "\\LogForUnconfiguredProcess.log";
				}
				if(_blnUseDateForLogFileName) {
					string strDate;
					#region Update the PathName with current date
					if(_blnUseUTC){
						strDate = System.DateTime.UtcNow.ToString("yyyyMMdd");
					}
					else{
						strDate = System.DateTime.Now.ToString("yyyyMMdd");
					}
					int intPos = _strLogFilePathName.LastIndexOf(".");
					if(intPos==-1){//No extension for the log file had been specified
						if(_blnDateInsertedIntoFileName){//replace then
							_strLogFilePathName=_strLogFilePathName.Substring(0,_strLogFilePathName.Length-8)+strDate+".log";
						}
						else{
							_strLogFilePathName+=strDate+".log";
						}
					}
					else{//Insert the date in front of extension
						if(_blnDateInsertedIntoFileName){//replace then
							_strLogFilePathName=_strLogFilePathName.Substring(0,intPos-8)+strDate+_strLogFilePathName.Substring(intPos);
						}
						else{
							_strLogFilePathName = _strLogFilePathName.Substring(0,intPos) + strDate + _strLogFilePathName.Substring(intPos);
						}
					}
					_blnDateInsertedIntoFileName=true;//so that next time we rather replace
					#endregion
				}
				return _strLogFilePathName; //return it changed or not
			}
			else{
				return localPathName;
			}
		}
		#endregion

		#region WriteLineToFileWithExceptionReturned
		/// <summary>
		/// Writes a line of text to file. 
		/// </summary>
		/// <param name="fileName">File path name</param>
		/// <param name="text">Text to write</param>
		/// <returns>Error message if write to file fails</returns>
		[System.Diagnostics.DebuggerStepThrough] 
		static string WriteLineToFileWithExceptionReturned(string fileName, string text){
			System.IO.StreamWriter sw=null;
			try{
				#region give it many attempts to write. It may be deceiving to write out of sequence, so we limit ourselves to 100ms
				for(int i=1;i<=50;i++){//give it a preliminaty series of tries with no error
					try{
						sw = new System.IO.StreamWriter(fileName,true);
						if(i==1){
							sw.WriteLine(text);
						}
						else{
							sw.WriteLine(text+"  WA"+i.ToString());
						}
						return "";
					}
					catch{System.Threading.Thread.Sleep(20);}
					finally{if(sw!=null)sw.Dispose();}
				}
				#endregion
				lock(typeof(System.IO.StreamWriter)){
					sw = new System.IO.StreamWriter(fileName,true);
					sw.WriteLine(text);
					return "";
				}
			}
			catch(Exception e){return "Error writing to ["+fileName+"]: "+e.Message;}
			finally{if(sw!=null)sw.Dispose();}
		}
		#endregion

		#region TimeStamp
		/// <summary>
		/// Returns string representing current time in consistent format, whether in local or UTC time, depending on UseUTC setting
		/// </summary>
		private static string TimeStamp{
			get{
				if (_blnUseUTC) {
					return System.DateTime.UtcNow.ToString("dd-MMM-yy HH:mm:ss.fff");
				}
				else {
					return System.DateTime.Now.ToString("dd-MMM-yy HH:mm:ss.fff");
				}
			}
		}
		#endregion
		
		#region GetAssembliesEngaged
		/// <summary>
		/// Renders text of Assemblies engaged by the process, excluding as much as possible .NET assemblies. It is useful for determining whether a problem is caused by assemblies of wrong versions deployed
		/// </summary>
		private static string GetAssembliesEngaged() {
			string strResult="\r\nASSEMBLIES ENGAGED:"; 
			try{
				foreach (System.Reflection.Assembly ass in System.AppDomain.CurrentDomain.GetAssemblies()) {
					//This returns something like TestHarness, Version=1.0.2.0, Culture=neutral, PublicKeyToken=null
					string strA=ass.FullName;
					if (strA.StartsWith("mscorlib") || strA.StartsWith("Microsoft") || strA.StartsWith("System") || strA.StartsWith("vshost")|| strA.StartsWith("CommonSenseSoft.Log")) continue;
					if(strA.Contains("Version=0."))continue;//Some dynamically-generated crap
					strResult+="\r\n"+ass.FullName;
				}
			}
			catch{}
			return strResult;
		}

		#endregion

		#region GetInfoSpecificToExceptionType
		/// <summary>
		/// Extracts data specific to particular exception type passed to this method
		/// </summary>
		/// <param name="e">Exception to extract data from</param>
		/// <returns>Text specific to the exception type</returns>
		private static string GetInfoSpecificToExceptionType(Exception e){
			System.Text.StringBuilder sb=new System.Text.StringBuilder();
			int intLineNumber=-1;
			try{
				switch(e.GetType().FullName){
					#region SqlException
					case "System.Data.SqlClient.SqlException":
						switch(e.GetBaseException().GetType().FullName){
							case "System.Data.SqlClient.SqlException":
								System.Data.SqlClient.SqlException SQLEx = (System.Data.SqlClient.SqlException)e.GetBaseException();
								if(SQLEx!=null){
									for(int i=0;i<SQLEx.Errors.Count;i++){
										//The Data Provider keeps complaining about the same line with more useful messages. Skip them
										if(SQLEx.Errors[i].LineNumber!=intLineNumber){
											intLineNumber=SQLEx.Errors[i].LineNumber;
											if(i!=0){
												sb.Append("EventMetadata=[");sb.Append(SQLEx.Errors[i].Message);sb.Append("], ");
											}
											sb.Append("Procedure=[");sb.Append(SQLEx.Errors[i].Procedure);sb.Append("], Line=[");
											sb.Append(SQLEx.Errors[i].LineNumber);sb.AppendLine("]");
											sb.Append("CLASS=");sb.Append(SQLEx.Class.ToString());sb.Append(" \tSTATE=");
											sb.Append(SQLEx.State.ToString());sb.Append(" \tNUMBER=");sb.AppendLine(SQLEx.Number.ToString());
										}
									}
									SQLEx=null;
								}
							break;
							case "System.ComponentModel.Win32Exception":
								System.ComponentModel.Win32Exception Win32Ex=(System.ComponentModel.Win32Exception)e.GetBaseException();
								if(Win32Ex!=null){
									foreach(System.Collections.Generic.KeyValuePair<object,object> Pair in Win32Ex.Data){
										sb.Append("[");
										sb.Append((Pair.Key==null)?"NULL":Pair.Key.ToString());
										sb.Append("]=[");
										sb.Append((Pair.Value==null)?"NULL":Pair.Value.ToString());
										sb.AppendLine("]");
									}
									Win32Ex=null;
								}
							break;
						}
					break;
					#endregion
					#region OdbcException
					case "System.Data.Odbc.OdbcException":
						System.Data.Odbc.OdbcException OdbcEx = (System.Data.Odbc.OdbcException)e.GetBaseException();
						if(OdbcEx!=null){
							for(int i=0;i<OdbcEx.Errors.Count;i++){
								sb.Append("Message=[");sb.Append(OdbcEx.Errors[i].Message);sb.Append("], ");
								sb.Append("Source=[");sb.Append(OdbcEx.Errors[i].Source);sb.Append("], ");
								sb.Append("NativeError=[");sb.Append(OdbcEx.Errors[i].NativeError);sb.Append("]");
								sb.Append("ErrorCode=");sb.Append(OdbcEx.ErrorCode.ToString());
								sb.Append(" \tSTATE=[");sb.Append(OdbcEx.Errors[i].SQLState);sb.Append("]");
							}
							OdbcEx=null;
						}
					break;
					#endregion
					#region Web
					case "System.Web.HttpException":
					case "System.Web.HttpParseException":
                    case "System.Web.HttpRequestValidationException":
                    case "System.Web.HttpUnhandledException":
						try{
							if(System.Web.HttpContext.Current!=null && System.Web.HttpContext.Current.Request!=null){
								sb.Append("http://whois.domaintools.com/");
								sb.AppendLine(NullSafe(System.Web.HttpContext.Current.Request.UserHostAddress));
							}
							sb.Append(GetWebRequestData());
						}
						catch(Exception ex){
							sb.AppendLine("COULD NOT GET REQUEST DATA: "+ex.Message);
						}
					break;
					#endregion
				}
			}
			catch(Exception esb){
				sb.Append("GetInfoSpecificToExceptionType failed to get the info:{");
				sb.Append(esb.Message);
				sb.AppendLine("}, Stack Trace:{");
				sb.Append(esb.StackTrace);
				sb.AppendLine("}");
			}
			return sb.ToString();
		}
		#endregion

		#region GetInnerExceptions
		/// <summary>
		/// Extracts inner exceptions from an Exception
		/// </summary>
		/// <param name="ex">Exception to extract inner exceptions from</param>
		/// <returns>String of Inner Exceptions</returns>
		private static string GetInnerExceptions(Exception ex){
			string strLeadingTabs="";
			System.Text.StringBuilder sb=new System.Text.StringBuilder();
			while(ex.InnerException != null) {
				strLeadingTabs+="\t";
				sb.Append(strLeadingTabs);
				sb.AppendLine(ex.InnerException.Message);
				sb.Append(strLeadingTabs);
				sb.Append("Stack:{");
				if(!string.IsNullOrEmpty(ex.InnerException.StackTrace)){
					sb.AppendLine();
					sb.Append(strLeadingTabs);
					sb.AppendLine(CleanCallStack(ex.InnerException.StackTrace).Replace(Environment.NewLine,Environment.NewLine+strLeadingTabs));
					sb.Append(strLeadingTabs);
				}
				sb.AppendLine("}");
				ex = ex.InnerException;
			}
			if(sb.Length > 0) { 
				//Comment out while this is the first what is shown to user sb.Insert(0,"INNER EXCEPTIONS:\r\n");
				sb.Insert(0,"INNER EXCEPTIONS START\r\n");
				sb.AppendLine("END OF INNER EXCEPTIONS.");
			}
			return sb.ToString();
		}
		#endregion

		#region Parse into short
		/// <summary>
		/// Very tolerant parser into short datatype.
		/// </summary>
		/// <param name="val">can be null, DBNull.Value, false, true, "", "no", "yes"</param>
		/// <param name="defaultValue">Default value to be returned in case of null, DBNull.Value, ""</param>
		/// <returns>value interpreted as short, or throws exception if the value is not foreseen</returns>
		private static short ToShort(object val,short defaultValue) {
			if(val==null||val==DBNull.Value||val.ToString()=="") { return defaultValue; }
			string strVal=val.ToString().ToLower();
			switch(strVal) {
				case "false":
				case "no":
					return 0;
				case "true":
				case "yes":
					return 1;
				default:
					return short.Parse(strVal);
			}
		}
		#endregion

		#region ToBoolean
		/// <summary>
		/// Very tolerant parser into boolean datatype.
		/// </summary>
		/// <param name="val">can be null, DBNull.Value, false, true, 0, 1, -1, "", "no", "yes"</param>
		/// <returns>value interpreted as true or false, or throws exception if the value is not foreseen</returns>
		private static bool ToBoolean(object val) {
			if(val==null||val==DBNull.Value) { return false; }
			switch(val.ToString().ToLower()) {
				case "false":
				case "0":
				case "":
				case "no":
					return false;
				case "true":
				case "1":
				case "-1":
				case "yes":
					return true;
				default:
					throw new Exception("Could not parse value ["+val.ToString()+"] as Boolean");
			}

			return System.Boolean.Parse(val.ToString());
		}
		#endregion

		#region Parse into int
		/// <summary>
		/// Very tolerant parser into int datatype.
		/// </summary>
		/// <param name="val">can be null, DBNull.Value, false, true, "", "no", "yes"</param>
		/// <param name="defaultValue">Default value to be returned in case of null, DBNull.Value, ""</param>
		/// <returns>value interpreted as int, or throws exception if the value is not foreseen</returns>
		private static int ToInt(object val,int defaultValue) {
			if(val==null||val==DBNull.Value||val.ToString()=="") { return defaultValue; }
			string strVal=val.ToString().ToLower();
			switch(strVal) {
				case "false":
				case "no":
					return 0;
				case "true":
				case "yes":
					return 1;
				default:
					return int.Parse(strVal);
			}
		}
		#endregion
		
		#region NullSafe
		/// <summary>
		/// Returns empty string if the object is null
		/// </summary>
		/// <param name="obj">Any object that implements ToString() method</param>
		/// <returns>Either cast to string or empty string, without failing on null</returns>
		public static string NullSafe(Object obj){
			if(obj==null){return "";}
			return obj.ToString();
		}
		#endregion

		#region CleanCallStack
		/// <summary>
		/// Cleans Call Stack from information unnecessary for logging
		/// </summary>
		/// <param name="stackTrace">Call Stack text to clean</param>
		/// <returns></returns>
		static string CleanCallStack(string stackTrace){
			string srtStackTrace="";
			if(stackTrace!=null){
				srtStackTrace=stackTrace.TrimStart();
				int intPos=srtStackTrace.LastIndexOf("CommonSenseSoft.Log.Logging.RelayException(");
				if(intPos!=-1) {
					intPos=srtStackTrace.IndexOf("\r\n",intPos);
					if(intPos!=-1) {
						//The next line will show the line where the exception has been re-thrown, i.e. this method, and we don't want it
						int intPosSkipped = -1;
						try { intPosSkipped=srtStackTrace.IndexOf("\r\n",intPos+2); } catch { }
						intPos=(intPosSkipped==-1)?intPos:intPosSkipped;
						try { srtStackTrace=srtStackTrace.Substring(intPos+2); } catch { }
					}
				}
				intPos=srtStackTrace.LastIndexOf("CommonSenseSoft.Log.Logging.LogException(");
				if(intPos!=-1){
					intPos=srtStackTrace.IndexOf("\r\n",intPos);
					if(intPos!=-1){
						srtStackTrace=srtStackTrace.Substring(intPos+2);
					}
				}
//				LogMessage("Stack Cleanup ["+srtStackTrace+"]",LogLevels.Debug);
				intPos=srtStackTrace.IndexOf("at System.");//the system may be at calling end as in case of System.Web.UI. as well as at the bottom of the call
				while(intPos!=-1){
					int intPosEnd=srtStackTrace.IndexOf("\r\n",intPos);
//					LogMessage("Stack Cleanup "+intPos.ToString()+" => "+intPosEnd.ToString(),LogLevels.Debug);
					if(intPosEnd==-1)break;
					srtStackTrace=srtStackTrace.Remove(intPos,intPosEnd-intPos+2);
					intPos=srtStackTrace.IndexOf("at System.");
				}
			}
			return srtStackTrace;
		}
		#endregion

		#endregion
		
		#region enuBlankLines
		/// <summary>
		/// Predefined ways of Blank Line appearing when writing a line into a log file
		/// </summary>
		public enum enuBlankLines{
			/// <summary>
			/// No Blank Lines.
			/// </summary>
			None = 0,
			/// <summary>
			/// Two blank lines before and two blank lines After
			/// </summary>
			DoubleTopAndBottom=1,
			/// <summary>
			/// One blank line before and one blank line After
			/// </summary>
			TopAndBottom=2,
			/// <summary>
			/// One blank line before
			/// </summary>
			Top=3,
			/// <summary>
			/// One blank line after
			/// </summary>
			Bottom=4,
		}
		#endregion
	}

	#region LogLevels ENUM
	/// <summary>
	/// Pre-defined levels of logging.
	/// </summary>
	public enum LogLevels{
		/// <summary>
		/// No logging.
		/// </summary>
		None = -1,
		/// <summary>
		/// Error from which no recovery had been implemented, AKA Failure.
		/// </summary>
		Fatal = 0,
		/// <summary>
		/// Errors and Exceptions are logged only.
		/// </summary>
		Exceptions = 1,
		/// <summary>
		/// Errors, Exceptions and Warnings are logged only.
		/// </summary>
		Warnings = 2,
		/// <summary>
		/// Errors, Exceptions, Warnings and Information are logged only.
		/// </summary>
		Information = 4,
		/// <summary>
		/// Errors, Exceptions, Warnings, Information and Procedures calls are logged.
		/// </summary>
		Calls = 6,
		/// <summary>
		/// Errors, Exceptions, Warnings, Information, Procedures calls and Procedures parameters values are logged.
		/// </summary>
		Parameters = 7,
		/// <summary>
		/// Errors, Exceptions, Warnings, Information, Procedures calls, Procedures parameters values and execution steps inside procedures are logged.
		/// </summary>
		Debug = 9
	}
	#endregion

	#region MailPriority ENUM
	/// <summary>
	/// Specifies the priority level for the e-mail message.
	/// </summary>
	public enum MailPriority {
		/// <summary>
		/// Specifies that the e-mail message has normal priority.
		/// </summary>
		Normal=0,
		/// <summary>
		/// Specifies that the e-mail message has low priority.
		/// </summary>
		Low = 1,
		/// <summary>
		///  Specifies that the e-mail message has high priority.
		/// </summary>
		High=2,
	}
	#endregion
}
