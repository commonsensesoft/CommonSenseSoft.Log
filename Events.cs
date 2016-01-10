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

namespace CommonSenseSoft.Log {
	
	/// <summary>
	/// Events functionality for clients wanting to use Errors, Warnings and Info collected and presented hierarhically.
	/// Provides functionality to conclude if the hierarchy suceeded or failed, without using Exceptions, which should be left to their own course.
	/// </summary>
	public class Events{
		#region Private members
		private System.Collections.Generic.List<Event> _Events=new System.Collections.Generic.List<Event>();
		private System.Collections.Generic.List<Events> _DownstreamEventsCollections=new System.Collections.Generic.List<Events>();//Recursively add inner errors
		private bool _blnEventsExist=false;
		private bool _blnErrorsExist=false;
		private bool _blnAnySuccess=false;
		#endregion

		#region Properties and Fields
		/// <summary>
		/// To maintain consistent time format HH:mm:ss.fff
		/// </summary>
		public string TimeStampFormat="HH:mm:ss.fff";//We assume that by default the caller will take care of the date
		
		#region Exist
		/// <summary>
		/// Fastest way to find if any event exists.
		/// </summary>
		public bool Exist{
			get { return _blnEventsExist; }
		}
		#endregion

		#region ErrorsRecorded
		/// <summary>
		/// Fastest way to find if any Error exists. Errors are those which DO NOT allow the transaction proceed.
		/// </summary>
		public bool ErrorsRecorded {
			get { return _blnErrorsExist; }
		}
		#endregion

		#region GetSuccessStatus
		/// <summary>
		/// If the owner object is not null or transactional, any successful child object means success, and that goes recursively.
		/// </summary>
		public enuSuccessStatus GetSuccessStatus(bool isOwnerTransactional) {
		    if(isOwnerTransactional) {
		        if(ErrorsRecorded)return enuSuccessStatus.Failed;
		        return enuSuccessStatus.Succeeded;
		    }
		    else {
		        if(Exist){
		            if(ErrorsRecorded){
		                if(_blnAnySuccess)return enuSuccessStatus.PartiallySucceeded;
		                return enuSuccessStatus.Failed;
		            }
		            else{
		                return enuSuccessStatus.Succeeded;
		            }
		        }
		        else{
		            return enuSuccessStatus.Succeeded;
		        }
		    }
		}
		#endregion
		
		/// <summary>
		/// Name of the object holding the events. The name should be suitable for reporting
		/// </summary>
		//Can't use type names because they are not suitable for users, especially after dotfuscation
		internal string OwnerObjectNameForReporting;
		
		/// <summary>
		/// Id of the object that holds the events
		/// </summary>
		internal string OwnerObjectId;

		#endregion

		#region Methods

		#region Add
		/// <summary>
		/// Adds Event to Events collection
		/// </summary>
		/// <param name="messageId">Id of the message. The other parts of the message are retreived from resource file by this ID. You need to refer to the file for correct IDs</param>
		/// <param name="messageTextReplacementValues">Array of values to replace placeholders in the Text of the EventMetadata.</param>
		public void Add(int messageId,params string[] messageTextReplacementValues) {
			Event evt=new Event();
			#region Get the calling method
			System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace();
			System.Diagnostics.StackFrame stackFrame=null;
			System.Reflection.MethodBase methodBase=null;
			for(int i=1;i<5;i++) {//The trace may be full of our internal stuff which we don't want
				stackFrame = stackTrace.GetFrame(i);
				methodBase = stackFrame.GetMethod();
				if(methodBase.DeclaringType.Name!="Events") break;
			}
			#endregion
			evt.Location=methodBase.Name;
			evt.EventId=messageId;
			evt.EventParams=messageTextReplacementValues;
			evt.TimeStamp=DateTime.Now;
			_Events.Add(evt);
			_blnEventsExist=true;
			#region Set flags that depend on message properties
			switch(EventsMetadata.Event(messageId).Type){
				case System.Diagnostics.EventLogEntryType.Error:
					_blnErrorsExist=true;
				break;
				default:
					_blnAnySuccess=true;
				break;
			}
			#endregion
		}
		/// <summary>
		/// Adds Event to Events collection
		/// </summary>
		/// <param name="messageId">Id of the message. The other parts of the message are retreived from resource file by this ID. You need to refer to the file for correct IDs</param>
		public void Add(int messageId) {
			Add(messageId,null);
		}
		#endregion

		#region AddObjectEvents
		/// <summary>
		/// Events object from the object passed to this method gets added to collection of Events collections.
		/// These recursive collections allow building a hierarchical tree of Events for later reporting by the top caller object.
		/// The method updates properties of the Events object with properties of the passed host object, so that the host did not have to do it itself.
		/// </summary>
		/// <param name="objectWithEvents">Object to take Events and other properties from.</param>
		public void AddObjectEvents(IEventsCollector objectWithEvents) {
			Events EventsOfPassedObject=objectWithEvents.Events;
			//We could set these properties in constructor, but most likely they are not initialized at the time, and we need to do it for every object.
			//We could do that when Add method is called to add a single event, but too many calls to use extra parameter
			if(EventsOfPassedObject.Exist){//otherwise we may end up with adding empty object, and we do not want that
				EventsOfPassedObject.OwnerObjectId=objectWithEvents.Id;
				EventsOfPassedObject.OwnerObjectNameForReporting=objectWithEvents.ObjectNameForReporting;
				_DownstreamEventsCollections.Add(EventsOfPassedObject);
				_blnEventsExist=true;
				if(EventsOfPassedObject._blnErrorsExist) {
					_blnErrorsExist=true;//Otherwise do not touch it
				}
				enuSuccessStatus Status=EventsOfPassedObject.GetSuccessStatus(objectWithEvents.Transactional);
				_blnAnySuccess=(Status!=enuSuccessStatus.Failed);
			}
		}
		#endregion

		#region ToString()
		/// <summary>
		/// Returns hierarchical representation of Events as far as text can go.
		/// </summary>
		public override string ToString() {
			return ToString(null);
		}
		
		/// <summary>
		/// Returns hierarchical representation of Events as far as text can go.
		/// </summary>
		/// <param name="entryType">Entry type filter for returned events</param>
		public string ToString(System.Diagnostics.EventLogEntryType? entryType) {
			return ToString(entryType,0);
		}
		
		/// <summary>
		/// Returns hierarchical representation of Events as far as text can go.
		/// </summary>
		/// <param name="entryType">Entry type filter for returned events</param>
		/// <param name="recursionDepth">How deep in the recurtion we are.</param>
		private string ToString(System.Diagnostics.EventLogEntryType? entryType,byte recursionDepth) {
			//Group the Events by their Type
			char chaTab=char.Parse("\t");
			System.Diagnostics.EventLogEntryType[] arrEntryTypes;
			if(entryType==null){
				arrEntryTypes=new System.Diagnostics.EventLogEntryType[3];
				arrEntryTypes[0]=System.Diagnostics.EventLogEntryType.Error;
				arrEntryTypes[1]=System.Diagnostics.EventLogEntryType.Warning;
				arrEntryTypes[2]=System.Diagnostics.EventLogEntryType.Information;
			}
			else{
				arrEntryTypes=new System.Diagnostics.EventLogEntryType[1];
				arrEntryTypes[0]=(System.Diagnostics.EventLogEntryType)entryType;
			}
			System.Text.StringBuilder stbAll=new System.Text.StringBuilder();
			for(int intEntryType=0;intEntryType<arrEntryTypes.Length;intEntryType++){
				System.Text.StringBuilder stb=new System.Text.StringBuilder();
				#region Output local events
				for(int i=0;i<_Events.Count;i++) {//We do not provide enumerator for this class. We want things to happen inside
					Event evt=_Events[i];
					if(EventsMetadata.Event(evt.EventId).Type==arrEntryTypes[intEntryType]){
						stb.Append(Environment.NewLine);
//						stb.Append(Environment.NewLine);
						stb.Append(chaTab,recursionDepth+1);
						stb.Append(evt.TimeStamp.ToString(TimeStampFormat)); stb.Append(chaTab);
//USE PADDING !!!!!!!!!!!!!!!!!
						stb.Append(evt.Location.PadRight(25)); stb.Append(chaTab);
						stb.Append(EventsMetadata.Event(evt.EventId).ToString(evt.EventParams).Replace(Environment.NewLine,Environment.NewLine+UTILS.Repeat("\t",recursionDepth+9)));
					}
				}
				#endregion
				#region Recursively add inner events
				if(_DownstreamEventsCollections.Count>0){//Otherwise don't even bother
					//You can still have all the elements of undesireable type, so first find if you have something to report
					System.Text.StringBuilder stbInner=new System.Text.StringBuilder();
					for(int ii=0;ii<_DownstreamEventsCollections.Count;ii++){
						string strInnerEvents=_DownstreamEventsCollections[ii].ToString(arrEntryTypes[intEntryType],(byte)(recursionDepth+1));
						if(strInnerEvents!=""){
							stbInner.Append(Environment.NewLine);
							stbInner.Append(Environment.NewLine);
							stbInner.Append(chaTab,recursionDepth+1);
							stbInner.Append(_DownstreamEventsCollections[ii].OwnerObjectNameForReporting);//Type of the object
							stbInner.Append(":");
							stbInner.Append(chaTab);
							stbInner.Append("[");
							stbInner.Append(_DownstreamEventsCollections[ii].OwnerObjectId);//Name of the object
							stbInner.Append("]");
							stbInner.Append(strInnerEvents);
						}
					}
					if(stbInner.Length>0) {//Then there are inner events to report in this type. 
//						if(recursionDepth>0){//In current implementation, the topmost level rarely has own events, but that may change
							stb.Append(Environment.NewLine);
							stb.Append(Environment.NewLine);
							stb.Append(chaTab,recursionDepth+1);
							stb.Append("INNER EVENTS:");
//						}
						stb.Append(stbInner);
					}
				}
				#endregion
				#region Append EventType if there is something to append, and we are at the top level of recursion
				if(stb.Length>0&&recursionDepth==0) {//Otherwise at the top of the entire thing the Type is already printed
					stbAll.Append(Environment.NewLine);
					stbAll.Append(Environment.NewLine);
					stbAll.Append(Environment.NewLine);
					stbAll.Append(arrEntryTypes[intEntryType].ToString().ToUpper());
					switch(arrEntryTypes[intEntryType]){
						case System.Diagnostics.EventLogEntryType.Error:
						case System.Diagnostics.EventLogEntryType.Warning:
							stbAll.Append("S");
						break;
					}
					stbAll.Append(":");
				}
				#endregion
				
				stbAll.Append(stb);
			}//for(int intEntryType=0;intEntryType<arrEntryTypes.Length;intEntryType++)
			return stbAll.ToString();
		}
		#endregion
		#endregion

		#region Enumerations 
		/// <summary>
		/// Success status of the operation. That typically applied to bulk processing of multiple records when some may succeed some not.
		/// Not to be confused with System.Diagnostics.EventLogEntryType used in EventsMetadata!!!!!!!!!!!!
		/// </summary>
		public enum enuSuccessStatus{
			/// <summary>
			/// The operation Failed completely
			/// </summary>
			Failed = 0,
			/// <summary>
			/// The operation Succeeded completely
			/// </summary>
			Succeeded = 1,
			/// <summary>
			/// The operation Succeeded partially, there had been Failures as well
			/// </summary>
			PartiallySucceeded = 2,
		}
		#endregion
	}
	
}