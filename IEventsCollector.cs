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
	/// Any class that wants its callers to take advantage of Events collection, needs to implement this interface.
	/// In doing that, the callers can build hierarchy of events collected from downstream objects.
	/// </summary>
	public interface IEventsCollector {
		/// <summary>
		/// Name of the object holding the events. The name should be suitable for reporting.
		/// </summary>
		//Can't use type names because they are not suitable for users, especially after dotfuscation
		string ObjectNameForReporting{get;}
		/// <summary>
		/// Id of this concrete object instance
		/// </summary>
		string Id { get;}
		/// <summary>
		/// If the object is Transactional, any EventMetadata of Error Type makes the Object Failed to process.
		/// If the object is not Transactional, any non-Error EventMetadata makes the Object successfully processed.
		/// </summary>
		bool Transactional { get;}
		/// <summary>
		/// Collection of Events this object holds. The Caller can then extract it using this Interface.
		/// </summary>
		Events Events { get;}


	}
}