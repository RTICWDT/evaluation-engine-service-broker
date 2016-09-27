// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Service1.Designer.cs" company="RTI">
//   Copyright (c) MPR Inc. All rights reserved.
//   see: http://code.msdn.microsoft.com/Service-Broker-Message-e81c4316#content
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace ServiceBroker
{
    using System.ServiceProcess;

    /// <summary>
    /// A service that queries to database to see what reports have been completed.
    /// </summary>
    public partial class EEServiceBroker : ServiceBase
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (this.components != null))
            {
                this.components.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            // 
            // EEServiceBroker
            // 
            this.ServiceName = "EEServiceBroker";

        }

        #endregion
    }
}
