﻿using JosephM.Application.Application;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using JosephM.Core.AppConfig;
using System.Collections.Generic;
using JosephM.Core.Extentions;
using JosephM.Core.Security;

namespace JosephM.Application.Desktop.Module.ApplicationInsights
{
    public class ApplicationInsightsLogger : IApplicationLogger
    {
        public ApplicationInsightsLogger(string instrumentationKey, IApplicationController applicationController)
        {
            InstrumentationKey = instrumentationKey;
            ApplicationController = applicationController;
            SessionId = Guid.NewGuid().ToString();
            AnonymousString = "Anonymous " + StringEncryptor.HashString(UserName);

            TelemetryConfiguration.Active.InstrumentationKey = InstrumentationKey;

            #if DEBUG
                IsDebugMode = true;
            #endif

            var telemetryConfiguration = new TelemetryConfiguration(InstrumentationKey);

            //this tells to promptly send data if debugging
            telemetryConfiguration.TelemetryChannel.DeveloperMode = IsDebugMode;
            //for when debuuging if want to send data uncomment this line
            //IsDebugMode = false;

            var tc = new TelemetryClient(telemetryConfiguration);
            tc.InstrumentationKey = InstrumentationKey;
            tc.Context.Cloud.RoleInstance = ApplicationController.ApplicationName;
            tc.Context.User.Id = string.Empty;
            tc.Context.Session.Id = SessionId;
            tc.Context.Device.OperatingSystem = Environment.OSVersion.ToString();
            TelemetryClient = tc;
        }

        public bool IsDebugMode { get; }

        public string InstrumentationKey { get; }
        public IApplicationController ApplicationController { get; }
        public string SessionId { get; }
        public TelemetryClient TelemetryClient { get; }

        private string AnonymousString { get; set; }

        private string UserName
        {
            get
            {
                return Environment.UserName;
            }
        }

        public void LogEvent(string eventName, IDictionary<string, string> properties = null)
        {
            var settings = ApplicationController.ResolveType<ApplicationInsightsSettings>();
            if (!IsDebugMode && settings.AllowUseLogging)
            {
                TelemetryClient.Context.User.Id = settings.AllowCaptureUsername ? UserName : AnonymousString;
                TelemetryClient.TrackEvent(eventName, properties);
            }
        }

        public void LogException(Exception ex)
        {
            LogEvent("General Error", new Dictionary<string, string> { { "Error", ex.Message }, { "Error Trace", ex.DisplayString() } });
        }
    }
}