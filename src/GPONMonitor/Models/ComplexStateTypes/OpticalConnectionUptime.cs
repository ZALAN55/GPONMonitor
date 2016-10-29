﻿using Newtonsoft.Json;
using System;

namespace GPONMonitor.Models.ComplexStateTypes
{
    public class OpticalConnectionUptime
    {
        public int? Value
        {
            get
            {
                return Value;
            }
            set
            {
                if (value != null)
                {
                    TimeSpan timeSpan = TimeSpan.FromSeconds(Convert.ToDouble(value));
                    string displayTimeSpan = timeSpan.ToString("dd, hh:mm:tt");

                    DescriptionEng = displayTimeSpan;
                    DescriptionPol = displayTimeSpan;
                    Severity = SeverityLevel.Default;

                    Value = value;
                }
                else
                {
                    DescriptionEng = null;
                    DescriptionPol = null;
                    Severity = SeverityLevel.Unknown;

                    Value = null;
                }
            }
        }

        public string DescriptionEng { get; private set; }
        public string DescriptionPol { get; private set; }
        public SeverityLevel Severity { get; private set; }

        [JsonIgnore]
        public string SnmpOID { get; private set; } = "1.3.6.1.4.1.6296.101.23.3.1.1.23";          // ONU optical connection uptime (the elapsed time after ont is up) (followed by OnuPortId and OnuId)
    }
}