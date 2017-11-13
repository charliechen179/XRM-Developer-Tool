﻿#region

using JosephM.Application.Application;
using JosephM.Application.Modules;
using JosephM.Core.AppConfig;
using JosephM.Core.Extentions;
using JosephM.Core.Log;
using JosephM.Prism.XrmModule.Crud;
using JosephM.Prism.XrmModule.Xrm;
using JosephM.Record.Xrm.XrmRecord;
using System;
using System.Configuration;

#endregion

namespace JosephM.Prism.XrmModule.XrmConnection
{
    public class XrmConnectionModule : ModuleBase
    {
        public override void RegisterTypes()
        {
            var configManager = Resolve<ISettingsManager>();

            try
            {
                var xrmConfiguration = configManager.Resolve<XrmRecordConfiguration>();
                RefreshXrmServices(xrmConfiguration, ApplicationController);
            }
            catch (ConfigurationErrorsException ex)
            {
                ApplicationController.UserMessage(
                    string.Concat("Warning!! There was an error reading the crm connection from config\n",
                        ex.DisplayString()));
            }

            RegisterTypeForNavigation<XrmMaintainViewModel>();
            RegisterTypeForNavigation<XrmCreateViewModel>();
            RegisterTypeForNavigation<XrmConnectionDialog>();
            //RegisterType<XrmFormController>();
            //RegisterType<XrmFormService>();
        }

        private static IXrmRecordConfiguration LastXrmConfiguration { get; set; }

        public static void RefreshXrmServices(IXrmRecordConfiguration xrmConfiguration, IApplicationController controller)
        {
            controller.RegisterInstance<IXrmRecordConfiguration>(xrmConfiguration);
            var serviceConnection = new XrmRecordService(xrmConfiguration, controller.ResolveType<LogController>(), formService: new XrmFormService());
            controller.RegisterInstance(serviceConnection);
            LastXrmConfiguration = xrmConfiguration;
            if (xrmConfiguration.OrganizationUniqueName == null)
                controller.AddNotification("XRMCONNECTION", "Not Connected");
            else
            {
                controller.DoOnAsyncThread(() =>
                {
                    try
                    {
                        controller.AddNotification("XRMCONNECTION", "Connecting...");
                        var verify = serviceConnection.VerifyConnection();
                        if (LastXrmConfiguration != xrmConfiguration)
                            return;
                        if (verify.IsValid)
                        {
                            controller.AddNotification("XRMCONNECTION", string.Format("Connected To Instance '{0}'", xrmConfiguration));
                            var preLoadRecordTypes = serviceConnection.GetAllRecordTypes();
                        }
                        else
                        {
                            controller.AddNotification("XRMCONNECTION", string.Format("Error Connecting To Instance '{0}'", xrmConfiguration));
                        }
                    }
                    catch (Exception ex)
                    {
                        if (LastXrmConfiguration != xrmConfiguration)
                            return;
                        controller.AddNotification("XRMCONNECTION", ex.Message);
                        controller.ThrowException(ex);
                    }
                });
            }
        }

        public override void InitialiseModule()
        {
            AddSetting("Connect To Crm", ConnectToCrm);
            AddHelpUrl("Connect To Crm", "ConnectToCrm");
        }

        private void ConnectToCrm()
        {
            NavigateTo<XrmConnectionDialog>();
        }
    }
}