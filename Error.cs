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
	/// Class that helps returning error conditions from subroutines in the way they can be programmatically processed and delivering verbose messages to interactive user at the same time
	/// </summary>
	public class  InteractionError {
		#region Constructor Overloads
		/// <summary>
		/// Creates new InteractionError object based on one of predefined error ID and description of what has happened
		/// </summary>
		/// <param name="id">One of predefined IDs</param>
		/// <param name="description">Error Description</param>
		public InteractionError(ErrorsIds id,string description){
			ID=id;Description=description;
		}
		/// <summary>
		/// Creates new InteractionError object based on arbitrary error ID and description of what has happened
		/// </summary>
		/// <param name="customID">arbitrary error ID</param>
		/// <param name="description">Error Description</param>
		public InteractionError(int customID,string description) {
			CustomID=customID; Description=description;
		}
		/// <summary>
		/// Creates empty instance of InteractionError
		/// </summary>
		public InteractionError(){
		}
		/// <summary>
		/// Creates instance of InteractionError with URL of where the caller should be redirecting
		/// </summary>
		/// <param name="redirectToURI">URL of where the caller should be redirecting</param>
		public InteractionError(string redirectToURI) {
			RedirectToURI=redirectToURI;
		}
		#endregion

		#region Append
		/// <summary>
		/// Appends to existing InteractionError
		/// </summary>
		/// <param name="id">Id of the Error. It replaces existing id only if the existing Id is ErrorsIds.UNINITIALIZED</param>
		/// <param name="description">| delimiter is Appended to existing description, followed by value of this parameter</param>
		public void Append(ErrorsIds id,string description){
			if(this.ID==ErrorsIds.UNINITIALIZED){//Get the new one, otherwise keep the original
				this.ID=id;
			}
			this.Description=UTILS.ConcatenateWithDelimiters("|",this.Description,description);
		}

		/// <summary>
		/// Appends to existing InteractionError. It replaces existing id only if the existing Id is ErrorsIds.UNINITIALIZED. | delimiter is Appended to existing description, followed by description property of the appended InteractionError
		/// </summary>
		/// <param name="anotherError">InteractionError to Append</param>
		public void Append(InteractionError anotherError){
			if(this.ID==ErrorsIds.UNINITIALIZED){ 
				this.ID=anotherError.ID;
			}
			this.Description=UTILS.ConcatenateWithDelimiters("|",this.Description,anotherError.Description);
			this.Details=UTILS.Concatenate(this.Details,anotherError.Details);
		}
		#endregion
		
		#region Fields
		/// <summary>
		/// Description of the Error, typically passed to the unteractive user
		/// </summary>
		public string Description;
		/// <summary>
		/// Often client needs to create a functional Acknowledgement from what is received in this object, but data is needed as array of values later properly joined into DSV that has delimiters and qualifiers escaped.
		/// </summary>
		public string[] Details;
		/// <summary>
		/// When Id property does not have applicable member of enumeration to choose from, any integer number can be used to extend that range
		/// </summary>
		public int CustomID;
		/// <summary>
		/// Id of the Error as per ErrorsIds enumeration
		/// </summary>
		public ErrorsIds ID=ErrorsIds.UNINITIALIZED;
		/// <summary>
		/// URL to where the caller should be redirecting after receiving this object back
		/// </summary>
		public string RedirectToURI;
		#endregion

		#region ErrorsIds
		/// <summary>
		/// Predefined typical Errors to return to caller to take action. No hard rules here, use what you consider applicable.
		/// </summary>
		public enum ErrorsIds{
			/// <summary>
			/// The ID had not beed initialized, or had been set to this for appended InteractionError's Id to take presedence.
			/// </summary>
			UNINITIALIZED = 0,
			/// <summary>
			/// Exception occurred
			/// </summary>
			Exception=-1,
			/// <summary>
			/// Has not been authorised to execute
			/// </summary>
			Unauthorized=1,
			/// <summary>
			/// Invalid user input, etc.
			/// </summary>
			Invalid=2,
			/// <summary>
			/// No value had provided by the user
			/// </summary>
			NoValue=3,
			/// <summary>
			/// What User asked for does not exist
			/// </summary>
			DoesNotExist=4,
			/// <summary>
			/// What User wanted to create already exists
			/// </summary>
			AlreadyExists=5,
			/// <summary>
			/// Authorised to execute part of code, but not the whole thing
			/// </summary>
			NeedMoreAuthorisation=6,
			/// <summary>
			/// User provided value which is too small
			/// </summary>
			TooSmall=7,
			/// <summary>
			/// User provided value which is too big
			/// </summary>
			TooBig=8,
			/// <summary>
			/// User provided something which is not required
			/// </summary>
			NotRequired=9,
			/// <summary>
			/// Warning to show to the User even if the operation has succeeded
			/// </summary>
			Warning=10,
			/// <summary>
			/// Note to show to the User
			/// </summary>
			Note = 11,
			/// <summary>
			/// Execution resulted in multiple results where only one had been expected
			/// </summary>
			MultipleValues=12,
		}
		#endregion
	}
}