﻿using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using GPONMonitor.Exceptions;

namespace GPONMonitor.Models
{
    public class Olt
    {
        public int Id { get; private set; }
        public string Name { get; private set; }
        public IPAddress SnmpIPAddress { get; private set; }
        public int SnmpPort { get; private set; }
        public VersionCode SnmpVersion { get; private set; }
        public string SnmpCommunity { get; private set; }
        public int SnmpTimeout { get; private set; }


        // OLT SNMP OIDs
        const string _snmpOIDOltDescription = "1.3.6.1.2.1.1.1.0";
        const string _snmpOIDOltUptime = "1.3.6.1.2.1.1.3.0";
        const string _snmpOIDListOnuDescription = "1.3.6.1.4.1.6296.101.23.3.1.1.18";
        const string _snmpOIDGetOnuModelType = "1.3.6.1.4.1.6296.101.23.3.1.1.17";      // (followed by OnuPortId and OnuId)


        public Olt(int id, string name, string snmpIPAddress, string snmpPort, string snmpVersion, string snmpCommunity, string snmpTimeout)
        {
            IPAddress tryParseSnmpIPAddress;
            int tryParseSnmpPort;
            int tryParseSnmpTimeout;


            if ((id >= 0 && id <= 128))
                Id = id;
            else
                throw new ArgumentException("Incorrect OLT id");


            if (!string.IsNullOrWhiteSpace(name))
                Name = name;
            else
                throw new ArgumentException("Incorrect OLT name");


            if (IPAddress.TryParse(snmpIPAddress, out tryParseSnmpIPAddress))
                SnmpIPAddress = tryParseSnmpIPAddress;
            else
                throw new ArgumentException("Incorrect OLT SNMP IP address");
            

            if (int.TryParse(snmpPort, out tryParseSnmpPort) &&
                (tryParseSnmpPort > 0 && tryParseSnmpPort < 65535))
                SnmpPort = tryParseSnmpPort;
            else
                throw new ArgumentException("Incorrect OLT SNMP port number");


            switch (snmpVersion)
            {
                case "1":
                    SnmpVersion = VersionCode.V1;
                    break;
                case "2":
                    SnmpVersion = VersionCode.V2;
                    break;
                case "3":
                    SnmpVersion = VersionCode.V3;
                    break;
                default:
                    throw new ArgumentException("Incorrect OLT SNMP version");
            }


            if (!string.IsNullOrWhiteSpace(snmpCommunity))
                SnmpCommunity = snmpCommunity;
            else
                throw new ArgumentException("Incorrect OLT SNMP community");


            if (int.TryParse(snmpTimeout, out tryParseSnmpTimeout) &&
                (tryParseSnmpTimeout > 0 && tryParseSnmpTimeout < 60000))
                SnmpTimeout = tryParseSnmpTimeout;
            else
                throw new ArgumentException("Incorrect OLT SNMP timeout");
        }


        private async Task<IList<Variable>> SnmpGetAsync(VersionCode snmpVersion, string oid)
        {
            Task<IList<Variable>> task = Messenger.GetAsync(snmpVersion,
                                    new IPEndPoint(SnmpIPAddress, SnmpPort),
                                    new OctetString(SnmpCommunity),
                                    new List<Variable> { new Variable(new ObjectIdentifier(oid)) });
            return await task;
        }


        private async Task<List<Variable>> SnmpWalkAsync(VersionCode snmpVersion, string oid, int timeout, WalkMode walkMode)
        {
            List<Variable> snmpWalkResult = new List<Variable>();

            Task<int> taskWalk = Messenger.WalkAsync(snmpVersion,
                                    new IPEndPoint(SnmpIPAddress, SnmpPort),
                                    new OctetString(SnmpCommunity),
                                    new ObjectIdentifier(oid),
                                    snmpWalkResult,
                                    timeout,
                                    walkMode);
            await taskWalk;
            return snmpWalkResult;
        }


        public async Task<string> GetDescriptionAsync()
        {
            List<Variable> snmpResponseDescription = new List<Variable>();

            try
            {
                snmpResponseDescription = await SnmpGetAsync(SnmpVersion, _snmpOIDOltDescription) as List<Variable>;
            }
            catch (Exception exception)
            {
                throw new SnmpConnectionException("SNMP request error: " + exception.Message);
            }

            if (snmpResponseDescription.Count == 0)
                throw new SnmpConnectionException("SNMP request error: no results has been returned");

            return snmpResponseDescription.First().Data.ToString();
        }


        public async Task<string> GetUptimeAsync()
        {
            List<Variable> snmpResponseUptime = new List<Variable>();

            try
            {
                snmpResponseUptime = await SnmpGetAsync(SnmpVersion, _snmpOIDOltUptime) as List<Variable>;
            }
            catch (Exception exception)
            {
                throw new SnmpConnectionException("SNMP request error: " + exception.Message);
            }

            if (snmpResponseUptime.Count == 0)
                throw new SnmpConnectionException("SNMP request error: no results has been returned");

            return snmpResponseUptime.First().Data.ToString().Split('.').First();
        }


        public async Task<List<OnuShortDescription>> GetOnuDescriptionListAsync()
        {
            List<OnuShortDescription> onuList = new List<OnuShortDescription>();
            List<Variable> snmpResponseOnuList = new List<Variable>();

            try
            {
                snmpResponseOnuList = await SnmpWalkAsync(SnmpVersion, _snmpOIDListOnuDescription, SnmpTimeout, WalkMode.WithinSubtree);
            }
            catch (Exception exception)
            {
                throw new SnmpConnectionException("SNMP request error: " + exception.Message);
            }

            foreach (Variable variable in snmpResponseOnuList)
            {
                uint oltPortId = variable.Id.ToNumerical().ToArray().ElementAt(13); 
                uint onuId = variable.Id.ToNumerical().ToArray().ElementAt(14);

                onuList.Add(new OnuShortDescription(oltPortId, onuId, variable.Data.ToString().Replace("_"," "))); 
            }

            return onuList;
        }

        public async Task<string> GetOnuModelAsync(uint oltPortId, uint onuId)
        {
            List<Variable> snmpResponseOnuModel = new List<Variable>();

            try
            {
                snmpResponseOnuModel = await SnmpGetAsync(SnmpVersion, _snmpOIDGetOnuModelType + "." + oltPortId + "." + onuId) as List<Variable>;
            }
            catch (Exception exception)
            {
                throw new SnmpConnectionException("SNMP request error: " + exception.Message);
            }

            if (snmpResponseOnuModel.Count == 0)
                throw new SnmpConnectionException("SNMP request error: no results has been returned");
            
            return snmpResponseOnuModel.First().Data.ToString();
        }
    }
}