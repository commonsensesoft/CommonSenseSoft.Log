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
	/// Mostly copies of methods found in CommonSenseSoft.Lib so that there is no dependancy on that library.
	/// </summary>
	internal static class UTILS {

		#region Repeat
		/// <summary>
		/// Repeats text as many times as required. Use PadLeft() if there is only one character to repeat
		/// </summary>
		/// <param name="text">Text to repeat</param>
		/// <param name="times">Times to repeat</param>
		/// <returns></returns>
		internal static string Repeat(string text,int times) {
			string strText="";
			for(int i=0;i<times;i++) {
				strText+=text;
			}
			return strText;
		}
		#endregion

		//internal static string Remove_L(string text, string delimiter){
		//	//Removing the left part of string off till right delimiter
		//	if (text==null) return null;//Delimiter is usually hard-coded, so we do not waste performance on it
		//		int r = text.IndexOf(delimiter);
		//		if (r > -1){
		//			return text.Remove(0, r + delimiter.Length);
		//		}
		//		else{
		//			return text;
		//		}
		//}

		//internal static string Remove_R_All(string text, string delimiter){
		//	if (text==null) return null;//Delimiter is usually hard-coded, so we do not waste performance on it
		//		int r = text.IndexOf(delimiter);
		//		if (r > -1){
		//			return text.Remove(r,text.Length - r);
		//		}
		//		else{
		//			return text;
		//		}
		//}

		#region ConcatenateWithDelimiters
		/// <summary>
		/// Concatenates supplied values using the delimiter. If the value is null or empty string, the delimiter is skipped. Handy for creating strings from arguments some of which may be missing values.
		/// </summary>
		/// <param name="delimiter">Delimiter to insert between non-empty values</param>
		/// <param name="values">params array of values to be delimited</param>
		internal static string ConcatenateWithDelimiters(string delimiter,params string[] values){
			if(values==null)return"";
			if(values.Length==0)return"";
			if(values.Length==1){
				return((values[0]==null)?"":values[0]);
			}
			else{
				System.Text.StringBuilder sb=new System.Text.StringBuilder();
				bool blnFirstDone=false;
				for(int i=0;i<values.Length;i++){
					if(!string.IsNullOrEmpty(values[i])){
						if(blnFirstDone){
							sb.Append(delimiter);
						}
						else{
							blnFirstDone=true;
						}
						sb.Append(values[i]);
					}
				}
				return sb.ToString();	
			}
		}
		#endregion

		#region Concatenate
		/// <summary>
		/// Concatenates 2 Arrays of strings
		/// </summary>
		/// <param name="array1"></param>
		/// <param name="array2"></param>
		/// <returns></returns>
		internal static string[] Concatenate(string[] array1, string[] array2){
			if(array2==null||array2.Length==0)return array1;
			if(array1==null||array1.Length==0)return array2;
			long lngLen1 = array1.Length ;
			long lngLen2 = array2.Length ;
			ReDim(ref array1,lngLen1 + lngLen2);
			for(long l=0;l<lngLen2;l++){
				array1[lngLen1+l] = array2[l];
			}
			return array1;
		}
		#endregion

		#region ReDim
		/// <summary>
		/// Changes size of string array preserving its elements
		/// </summary>
		/// <param name="arrayToReDim"></param>
		/// <param name="newSize"></param>
		internal static void ReDim(ref string[] arrayToReDim, long newSize){
			string[] strNew = new string[newSize];
			if(arrayToReDim!=null){
				long lngCopySize = (newSize>arrayToReDim.Length)? arrayToReDim.Length: newSize;
				for(long l=0;l<lngCopySize;l++){
					strNew[l] = arrayToReDim[l];
				}
			}
			arrayToReDim = strNew;
		}
		#endregion

	}
}