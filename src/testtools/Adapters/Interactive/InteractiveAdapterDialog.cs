// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Runtime.Remoting.Messaging;
using System.Reflection;


namespace Microsoft.Protocols.TestTools
{
    /// <summary>
    /// Interactive adapter dialog.
    /// </summary>
    public partial class InteractiveAdapterDialog : Form
    {

        private object returnValue;
        private object[] outArgs;

        ParameterDataBuilder builder;
        NameValueCollection properties;

        /// <summary>
        /// The objects array returned from the UI as out parameters. 
        /// </summary>
        public object[] OutArgs
        {
            get
            {
                return outArgs;
            }
        }

        /// <summary>
        /// The object returned by UI as return value.
        /// </summary>
        public object ReturnValue
        {
            get { return returnValue; }
        }

        private InteractiveAdapterDialog()
        {
        }

        /// <summary>
        /// Creates a new instance of InteractiveAdapterDialog class.
        /// </summary>
        /// <param name="methodCall">An IMessage that contains a IDictionary of information about the method call.</param>
        /// <param name="ptfProp">A NameValueCollection of settings from the test configuration file.</param>
        public InteractiveAdapterDialog(IMethodCallMessage methodCall, NameValueCollection ptfProp)
        {
            InitializeComponent();

            if (methodCall == null)
            {
                throw new ArgumentNullException("methodCall");
            }

            if (ptfProp == null)
            {
                throw new ArgumentNullException("ptfProp");
            }

            // Stores all arguments for generating the 'OutArgs' property,
            // since ReturnMessage needs all arguments including in args.
            outArgs = methodCall.Args;

            // Change caption
            this.Text = methodCall.MethodName;

            // Set the help message
            this.textHelpMessage.Text = AdapterProxyHelpers.GetHelpMessage(methodCall);

            // Set the properties
            this.properties = ptfProp;

            // Set data grid views
            builder = BindGridToDataTable(
                methodCall,
                dataGridViewActionPara);

            // Adjust the controls
            if (!builder.HasInArg)
            {
                labelActionParameters.Visible = false;
                dataGridViewActionPara.Visible = false;
                tableLayoutPanel1.RowStyles[2].Height = 5;
                tableLayoutPanel1.RowStyles[3].Height = 5;
            }

            if ((!builder.HasReturnVal) && (!builder.HasOutArg))
            {
                labelActionResult.Visible = false;
                tableActionResult.Visible = false;
                tableLayoutPanel1.RowStyles[4].Height = 5;
                tableLayoutPanel1.RowStyles[5].Height = 5;
            }

        }

        private ParameterDataBuilder BindGridToDataTable(
            IMethodCallMessage methodCall,
            DataGridView paramterView)
        {
            ParameterDataBuilder builder = new ParameterDataBuilder(methodCall);
            builder.Build();

            // Bind to the in-arguments data
            if (builder.HasInArg)
            {
                paramterView.AutoGenerateColumns = false;
                paramterView.DataSource = builder.InArgDataTable;
            }

            // Bind to the out-arguments data
            if (builder.HasOutArg || builder.HasReturnVal)
            {
                int rowIndex = 0;
                tableActionResult.RowCount = builder.OutArgDataTable.Rows.Count;
                foreach (DataRow row in builder.OutArgDataTable.Rows)
                {
                    Type type = (Type)row[2];
                    if (type.IsByRef) type = type.GetElementType();
                    if (tableActionResult.RowStyles.Count == rowIndex) tableActionResult.RowStyles.Add(new RowStyle());
                    tableActionResult.RowStyles[rowIndex].SizeType = SizeType.Absolute;
                    tableActionResult.RowStyles[rowIndex].Height = 25;
                    tableActionResult.Controls.Add(new TextBox()
                    {
                        Text = string.Format("{0} ({1})", row[0].ToString(), type.Name),
                        Dock = DockStyle.Fill,
                        ReadOnly = true,
                        BorderStyle = BorderStyle.None
                    }, 0, rowIndex);

                    if (type.IsEnum)
                    {
                        tableActionResult.Controls.Add(new BindingEnumCombobox(builder.OutArgDataTable, rowIndex, type)
                        {
                            Dock = DockStyle.Fill,
                            DropDownStyle= ComboBoxStyle.DropDownList,
                            FlatStyle = FlatStyle.Flat
                        }, 1, rowIndex);
                    }
                    else
                    {
                        tableActionResult.Controls.Add(new BindingTextBox(builder.OutArgDataTable, rowIndex)
                            {
                                Dock = DockStyle.Fill,
                                BorderStyle = BorderStyle.None
                            }, 1, rowIndex);
                    }
                    rowIndex++;
                }
                tableActionResult.Height = rowIndex * 27;
            }
            return builder;
        }

        private void GetOutArguments()
        {
            int firstOutArgIndex = 0;
            string strValue; // The string value can be parsed by 'Parse' method.
            DataTable dt = builder.OutArgDataTable;
            int count = dt.Rows.Count;

            if (builder.HasReturnVal)
            {
                if (dt.Rows[0][1] is DBNull)
                {
                    strValue = String.Empty;
                }
                else
                {
                    strValue = (string)dt.Rows[0][1];
                }

                try
                {
                    returnValue = AdapterProxyHelpers.ParseResult(builder.RetValType, strValue);
                    firstOutArgIndex = 1;
                }
                catch (FormatException)
                {
                    MessageBox.Show("The return value is invalid, please input a valid value.",
                                    "Error",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error,
                                    MessageBoxDefaultButton.Button1,
                                    0);
                    return;
                }
            }

            int i = firstOutArgIndex;
            int j = 0; // Indexing out argument position of the passed-in arguments.
            foreach (Type t in builder.OutArgTypes)
            {
                object o = (object)dt.Rows[i++][1];
                if (o is DBNull)
                {
                    strValue = String.Empty;
                }
                else
                {
                    strValue = (string)o;
                }

                try
                {
                    outArgs[builder.OutArgIndexes[j]] = AdapterProxyHelpers.ParseResult(t, strValue);
                    j++;
                }
                catch (FormatException)
                {
                    MessageBox.Show(String.Format("The input value for out argument \"{0}\" is not valid, please input a valid value.",
                                    (string)builder.OutArgDataTable.Rows[firstOutArgIndex + j].ItemArray[0]),
                                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, 0);
                    return;
                }
            }

            // The btnSucceed.DialogResult is not set in InteractiveAdapterUI.designer.cs,
            // so this Modal Dialog would not be closed automatically when click the Succeed button.
            // We need to set the form's DialogResult property to DialogResult.OK to close this form.
            this.DialogResult = DialogResult.OK;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            GetOutArguments();
        }

        private void btnProperties_Click(object sender, EventArgs e)
        {
            StringBuilder msgText = new StringBuilder();
            foreach (string key in this.properties.AllKeys)
            {
                msgText.AppendLine(key + " : " + this.properties[key]);
            }
            MessageBox.Show(msgText.ToString(),
                "PTF Configuration Properties", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, 0);
        }
 
        private void btnAbort_Click(object sender, EventArgs e)
        {

        }

        private void InteractiveAdapterDialog_Load(object sender, EventArgs e)
        {
            Visible = true;
        }
    }

    internal class BindingTextBox : TextBox
    {
        DataTable dataTable;
        int index;
        public BindingTextBox(DataTable dataTable, int index)
        {
            this.dataTable = dataTable;
            this.index = index;
            this.Text = dataTable.Rows[index].ItemArray[1].ToString();
        }

        protected override void OnTextChanged(EventArgs e)
        {
            dataTable.Rows[index][1] = this.Text;
        }
    }
    internal class BindingEnumCombobox : ComboBox
    {
        DataTable dataTable;
        int index;
        public BindingEnumCombobox(DataTable dataTable, int index, Type type)
        {
            this.dataTable = dataTable;
            this.index = index;
            foreach (var name in type.GetEnumNames())
            {
                this.Items.Add(name);
            }
            if (string.IsNullOrEmpty(dataTable.Rows[index].ItemArray[1].ToString()))
            {
                this.Text = this.Items[0].ToString();
            }
            else
            {
                this.Text = dataTable.Rows[index].ItemArray[1].ToString();
            }
            
        }

        protected override void OnTextChanged(EventArgs e)
        {
            dataTable.Rows[index][1] = this.Text;
        }
    }
}
