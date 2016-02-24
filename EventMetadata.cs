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
	/// Reads and stores Events metadata, also helps format run-time Events 
	/// </summary>
	internal class EventMetadata {
		#region Constants
		/// <summary>
		/// Placeholder to use in Text to be replaced with data provided by caller.
		/// </summary>
		internal const string PLACEHOLDER="???";
		#endregion

		#region Fields
		/// <summary>
		/// Id of the Event. It is int because some apps that may use it will accept numbers only, so it is better to be safe than sorry
		/// Desireable notation is Category as the first number, followed by 3 arbitrary numbers.
		/// </summary>
		internal int Id;

		/// <summary>
		/// Category of the Event.
		/// </summary>
		internal EventCategory Category;

		/// <summary>
		/// 1=Error, 2=Warning, 4=Information, 8=SuccessAudit, 16=FailureAudit
		/// </summary>
		internal System.Diagnostics.EventLogEntryType Type;

		/// <summary>
		/// Text of the EventMetadata. The placeholders will be replaced with run-time values.
		/// </summary>
		internal string Text;
		#endregion

		#region Methods

		#region ToString
		/// <summary>
		/// Replaces placeholders in the EventMetadata description with supplied parameters, then compiles properties of this object into meaningful to humans, standard string
		/// </summary>
		/// <param name="descriptionReplacements">Optional Array of values to replace placeholders in the Text of the Event. That is required then run-time specifics should be mentioned in the Text</param>
		internal string ToString(string[] descriptionReplacements) {
			return this.Category.Type+"\t"+this.Id.ToString()+": "+ReplacePlaceholdersInDescription(descriptionReplacements);
		}
		/// <summary>
		/// Compiles properties of this object into meaningful to humans, standard string
		/// </summary>
		internal string ToString() {
			return ToString(null);
		}
		#endregion

		#region ReplacePlaceholdersInDescription
		private string ReplacePlaceholdersInDescription(string[] descriptionReplacements) {
			if(descriptionReplacements==null) { return this.Text; }
			if(Text==null) { throw new Exception("The Text property is not initialized. It needs to be read from config file first."); }
			string strDescription=this.Text;
			string strOut="";
			for(int i=0;i<descriptionReplacements.Length;i++) {
				int intReplacementPosition=strDescription.IndexOf(EventMetadata.PLACEHOLDER);
				if(intReplacementPosition!=-1) {//Otherwise it is implementator passing extra parameters which we just ignore
					strOut+=strDescription.Substring(0,intReplacementPosition)+((descriptionReplacements[i])??"null");
					strDescription=strDescription.Substring(intReplacementPosition+EventMetadata.PLACEHOLDER.Length);
				}
			}
			return strOut+strDescription;
		}
		#endregion

		#endregion
		
	}

	internal struct EventCategory {
		internal byte Id;
		internal string Type;
	}

}
