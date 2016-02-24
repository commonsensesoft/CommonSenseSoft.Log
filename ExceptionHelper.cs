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
	/// Helper functions for internal use. Also useful for other Apps
	/// </summary>
	public static class ExceptionHelper {
		
		#region IsTransient
		/// <summary>
		/// Determines if this exception is likely to be transient and the cause is likely to go away
		/// </summary>
		/// <param name="e">Exception to validate</param>
		/// <returns>True if looks transient</returns>
		public static bool IsTransient(Exception e){
				switch(e.GetType().FullName){
					case "System.FormatException":
						return false;//When we have a good case of it being transient we will implement it
					case "System.Data.SqlClient.SqlException":
						System.Data.SqlClient.SqlException SQLEx = (System.Data.SqlClient.SqlException)e.GetBaseException();
						if(SQLEx!=null){
							switch(SQLEx.Number) {//not sure all the versions have the same meaning of these numbers
								case 207://Invalid column name XXX. In reality, when attempting to insert text into numeric field, we get the same error if using SELECT, and we have to use SELECT to be able to run T-SQL within insert command
								case 229://The SELECT permission was denied on the object
								case 245://Conversion failed when converting the %ls value '%.*ls' to data type %ls.
								case 8115://Arithmetic overflow error converting expression to data type int. Too large number sent to us. Or may be our column is too small!
								case 8152://String or binary data would be truncated.
									return true;
								default:
									return false;
							}
						}
					break;
					case "System.Data.Odbc.OdbcException":
						System.Data.Odbc.OdbcException OdbcEx = (System.Data.Odbc.OdbcException)e.GetBaseException();
						if(OdbcEx!=null){
							switch(OdbcEx.ErrorCode) {//not sure all the versions have the same meaning of these numbers
								case -2146232009://ERROR [23505] ERROR: duplicate key value violates unique constraint \"IX_Test_Nchar1\"\nKey (\"Nchar1\")=(Recip-1-1           ) already exists.;\nError while executing the query
									return true;
								default:
									return false;
							}
						}
					break;
					case "System.InvalidCastException":
						return false;//It's not something that resolves itself or gets recolved by changing surrounding conditions
					case "Npgsql.NpgsqlException":
						return false;//At hte moment we don't even want to depend on their reference
					case "System.InvalidOperationException":return false;
					case "System.Web.HttpException":
					case "System.Web.HttpParseException":
                    case "System.Web.HttpRequestValidationException":
                    case "System.Web.HttpUnhandledException":
						throw new NotImplementedException("Method is not implemented for exception Type=["+e.GetType().FullName+"]");
				}
				throw new NotImplementedException("Method is not implemented for exception Type=["+e.GetType().FullName+"]");
		}
		#endregion
	}
}
