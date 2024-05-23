using BarRaider.SdTools;
using NetLimiter.Service;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetLimiterPlugin
{
    [PluginActionId("com.rodeka.nlplugin.blockports")]
    public class KeyAction : KeypadBase
    {
        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings();
                instance.Name = String.Empty;
                instance.AppPath = String.Empty;
                instance.StartPort = 0;
                instance.EndPort = 0;
                instance.BPS = 0;
                return instance;
            }

            private NLClient client = new NLClient();
            private Filter filt;
            private Rule rule;
            private Filter filtModel;
            private Rule ruleModel;
            private bool ready = false;

            public void CreateFilter()
            {

                if (this.AppPath == String.Empty || this.Name == String.Empty)
                    return;
                this.client.Connect();
                this.filtModel = new Filter(this.Name);
                this.filtModel.Functions.Add(new FFAppIdEqual(new AppId(this.AppPath)));
                if (this.StartPort > 0 && this.EndPort > 0)
                    this.filtModel.Functions.Add(new FFRemotePortInRange(new PortRangeFilterValue(this.StartPort, this.EndPort)));
                this.ruleModel = new LimitRule(RuleDir.In, this.BPS);
                this.filt = this.client.Filters.Find(x => x.Name == this.Name);
                if (this.filt != null)
                    this.client.RemoveFilter(this.filt);
                this.filt = this.client.AddFilter(this.filtModel);
                this.rule = this.client.AddRule(this.filtModel.Id, this.ruleModel);
                this.ready = true;
                this.Disable();
            }

            public void RemoveFilter()
            {
                if (!this.ready)
                    return;

                this.filt = this.client.Filters.Find(x => x.Name == this.Name);
                if (this.filt != null)
                    this.client.RemoveFilter(this.filt);
            }

            public bool IsEnabled() =>
                    this.rule.IsEnabled;

            public void Disable()
            {
                if (!this.ready)
                    return;

                this.rule.IsEnabled = false;
                this.client.UpdateRule(this.rule);
            }

            public void Toggle()
            {
                if (!this.ready)
                    return;

                this.rule.IsEnabled = !this.rule.IsEnabled;
                this.client.UpdateRule(this.rule);
            }

            public bool IsReady()
            {
                return this.ready;
            }

            [FilenameProperty]
            [JsonProperty(PropertyName = "name")]
            public string Name { get; set; }

            [JsonProperty(PropertyName = "appPath")]
            public string AppPath { get; set; }

            [JsonProperty(PropertyName = "startPort")]
            public ushort StartPort { get; set; }

            [JsonProperty(PropertyName = "endPort")]
            public ushort EndPort { get; set; }

            [JsonProperty(PropertyName = "bps")]
            public ushort BPS { get; set; }


        }

        #region Private Members

        private PluginSettings settings;

        #endregion
        public KeyAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            if (payload.Settings == null)
            {
                this.settings = PluginSettings.CreateDefaultSettings();
                SaveSettings();
            }
            else
            {
                Connection.SetStateAsync((uint)0);
                this.settings = payload.Settings.ToObject<PluginSettings>();
                this.settings.CreateFilter();
                if (this.settings.IsReady())
                {
                    Connection.ShowAlert();
                }
            }
        }

        public override void Dispose()
        {
            this.settings.RemoveFilter();
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Destructor called");
        }

        public override void KeyPressed(KeyPayload payload)
        {
            this.settings.Toggle();
            Logger.Instance.LogMessage(TracingLevel.INFO, "Key Pressed");
        }

        public override void KeyReleased(KeyPayload payload) 
        {
            if (this.settings.IsEnabled())
                Connection.ShowOk();
        }

        public override void OnTick() { }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Tools.AutoPopulateSettings(settings, payload.Settings);
            SaveSettings();
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }

        #region Private Methods

        private Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(settings));
        }

        #endregion

    }
}