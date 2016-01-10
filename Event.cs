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
namespace CommonSenseSoft.Log {
	/// <summary>
	/// Class to keep data about event which can be later on logged in a structured manner
	/// </summary>
	public class Event {
		/// <summary>
		/// Usually a 
		/// </summary>
		public string Location;

		/// <summary>
		/// ID of the event message. Refer to the EventsMetadata.xml file.
		/// </summary>
		public int EventId;

		/// <summary>
		/// Array of values to replace placeholders in the EventMetadata Text.
		/// </summary>
		public string[] EventParams;

		/// <summary>
		/// DateTime when the Event was recorded.
		/// </summary>
		public System.DateTime TimeStamp;
	}
}