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
using System.ComponentModel;
//using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace CommonSenseSoft.Log {
	/// <summary>
	/// Renders a drop-down of Log Levels
	/// </summary>
	[ToolboxData("<{0}:ComboBox_LogLevel runat=server></{0}:ComboBox_LogLevel>")]
	public class ComboBox_LogLevel:DropDownList {
	    public event EventHandler TextChanged;
        #region Properties
        //This value is assigned before the items are created, therefore we either override it or no item will be selected
        LogLevels _SelectedValue;
		bool _IgnoreValueFromRequest;
        public LogLevels SelectedValue{
		    set{
				if(value==null)_SelectedValue=value;
				else _SelectedValue=value;
			}
			get{
				ResolveValue();
				return _SelectedValue;
			}
        }
 		/// <summary>
		/// When used in grid, the grid fails to propagate the UnregisterRequiresControlState further to this control,
		/// and has other vierd things happening. Use this property to tell when the value of this control should not be taken from Request
		/// Use OnRowDataBound event of the grid to set this property to true if required
		/// </summary>
		public bool IgnoreValueFromRequest{
			set{_IgnoreValueFromRequest=value;}
			get{return _IgnoreValueFromRequest;}
		}
       #endregion


		protected override void OnPreRender(EventArgs e){//The last event you can do anything
			this.Items.Add(new ListItem("None","-1"));
			this.Items.Add(new ListItem("Fatal","0"));
			this.Items.Add(new ListItem("Exceptions","1"));
			this.Items.Add(new ListItem("Warnings","2"));
			this.Items.Add(new ListItem("Information","4"));
			this.Items.Add(new ListItem("Functions Calls","6"));
			this.Items.Add(new ListItem("Parameters values","7"));
			this.Items.Add(new ListItem("Debug","9"));
			ResolveValue();
       }
        
		
		#region IPostBackDataHandler Members
		
		#region LoadPostData - that what we get in Request
		//DOES NOT WORK IN GRID. RATHER THAN RELYING ON THIS EVENT WE GET THE VALUE WHENEVER IT IS REQUIRED USING ResolveValue()
		//[Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
		//public bool LoadPostData(string postDataKey, System.Collections.Specialized.NameValueCollection postCollection)	{
		//    _SelectedValue=Context.Request[postDataKey];
		//    return true;
		//}
		#endregion
		
		#region RaisePostDataChangedEvent
		[Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
		public void RaisePostDataChangedEvent()	{
			if (TextChanged != null){
				TextChanged (this, new EventArgs ()); // Fire the event
			}
		}
		#endregion
		#endregion
		
		#region private methods
		void ResolveValue(){
			//that ensures that the control maintains its value between posts unless assigned to
			if(Context.Request[this.UniqueID]==null || _IgnoreValueFromRequest){
				//Leave it as it is_SelectedValue=LogLevels.Fatal;
			}
			else{
				if(Context.Request[this.UniqueID]!=null){
					_SelectedValue=(LogLevels)int.Parse(Context.Request[this.UniqueID].ToString().Trim());
				}
			}
			base.SelectedValue=((int)_SelectedValue).ToString();
		}
		#endregion
	}
}