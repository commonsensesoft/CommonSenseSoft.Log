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
using System.Data;
using System.Collections.Generic;
namespace CommonSenseSoft.Log {

	/// <summary>
	/// Provides External Application with means of defining its Events in a structured way in a config file.  
	/// </summary>
	internal static class EventsMetadata {
		#region Private members
		const string EVENTS_CODES_FILE="EventsMetadata.xml";
		static Dictionary<int,EventMetadata> _dicEvents=new Dictionary<int,EventMetadata>();
		static List<EventCategory> _EventCategories=new List<EventCategory>();
		#endregion
		#region Constructor
		/// <summary>
		/// There is not much harm in explicit constructor. Should occur when App starts rather than any functionality kicks in God knows when. It is even better to fail on start.
		/// </summary>
		static EventsMetadata() {
			Deserialize();
		}
		#endregion

		#region Properties
		/// <summary>
		/// Returns EventMetadata object populated from config file
		/// </summary>
		/// <param name="eventId">ID of the EventMetadata in config file.</param>
		/// <returns>Populated EventMetadata object if the ID is valid. Otherwise the properties are populated with defaults, and the Text telling that the EventMetadata could not be found</returns>
		internal static EventMetadata Event(int eventId) {//Can't use indexers in static classes
			try {
				return _dicEvents[eventId];
			}
			catch(KeyNotFoundException) {
				//This is a serious misconfiguration, but we are not going to fail.
				Logging.LogMessage("EventMetadata eventId="+eventId.ToString()+" is not configured! Most likely, your "+EVENTS_CODES_FILE+" file is not up-to date.",true,LogLevels.Fatal);
				EventMetadata msg=new EventMetadata();
				msg.Id=eventId;
				msg.Category.Id=1;
				msg.Type=System.Diagnostics.EventLogEntryType.Warning;
				msg.Text="EventMetadata eventId="+eventId.ToString()+" is not configured! Most likely, your "+EVENTS_CODES_FILE+" file is not up-to date.";
				return msg;
			}
		}
		#endregion

		#region Private methods
		#region Deserialize
		/// <summary>
		/// Deserializes the Events from file and keeps them in RAM
		/// </summary>
			private static void Deserialize() {
			const string CATEGORY_NODE="Category";
			const string EVENT_NODE="Event";
			string strAction="";
			DataSet ds=new DataSet();
			DataTable dt;
			string strConfigFile=Logging.GetAssemblyDirectory()+"\\"+EVENTS_CODES_FILE;
			try {
				strAction="Reading config file.";
				ds.ReadXml(strConfigFile);
				#region Read Categories
				strAction="Attempting to read ["+CATEGORY_NODE+"] Table.number of tables="+ds.Tables.Count.ToString();
				dt=ds.Tables[CATEGORY_NODE];
				if(dt==null) { throw new Exception("Could not find ["+CATEGORY_NODE+"] elements."); }
				//We do not clear the dictionary beforehand. If it is not clean, the cause must be fixed. We do not expect to deserialize more than once.
				int intCounter=0;
				foreach(DataRow dr in dt.Rows) {
					intCounter++;
					strAction="Reading "+CATEGORY_NODE+" element #"+intCounter.ToString();
					EventCategory Cat=new EventCategory();
					Cat.Id=byte.Parse(dr["Id"].ToString());
					Cat.Type=dr["Type"].ToString();
					_EventCategories.Add(Cat);
				}
				#endregion
				
				#region Read Events
				strAction="Attempting to read ["+EVENT_NODE+"] Table.";
				dt=ds.Tables[EVENT_NODE];
				if(dt==null) { throw new Exception("Could not find ["+EVENT_NODE+"] elements."); }
				//We do not clear the dictionary beforehand. If it is not clean, the cause must be fixed. We do not expect to deserialize more than once.
				intCounter=0;
				foreach(DataRow dr in dt.Rows) {
					intCounter++;
					strAction="Reading "+EVENT_NODE+" element #"+intCounter.ToString();
					EventMetadata Evt=new EventMetadata();
					Evt.Id=int.Parse(dr["Id"].ToString());
					strAction+=", Id="+Evt.Id.ToString();
					Evt.Category.Id=byte.Parse(dr["Category"].ToString());
					Evt.Type=(System.Diagnostics.EventLogEntryType)int.Parse(dr["Type"].ToString());
					Evt.Text=dr["Text"].ToString();
					_dicEvents.Add(Evt.Id,Evt);
				}
				#endregion
			}
			catch(Exception e) {
				Logging.RelayException("Configuration File ["+strConfigFile+"]: "+strAction,e);
			}
			finally {
				if(ds!=null) { ds.Dispose(); }
			}
		}
		#endregion
		#endregion

	}
}