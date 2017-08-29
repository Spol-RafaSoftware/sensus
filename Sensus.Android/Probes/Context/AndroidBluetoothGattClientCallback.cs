﻿// Copyright 2014 The Rector & Visitors of the University of Virginia
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using Android.Bluetooth;
using Sensus.Probes.Context;
using System.Text;

namespace Sensus.Android.Probes.Context
{
    public class AndroidBluetoothGattClientCallback : BluetoothGattCallback
    {
        public event EventHandler<BluetoothDeviceProximityDatum> DeviceIdEncountered;

        private AndroidBluetoothDeviceProximityProbe _probe;
        private DateTimeOffset _encounterTimestamp;

        public AndroidBluetoothGattClientCallback(AndroidBluetoothDeviceProximityProbe probe, DateTimeOffset encounterTimestamp)
        {
            _probe = probe;
            _encounterTimestamp = encounterTimestamp;
        }

        public override void OnConnectionStateChange(BluetoothGatt gatt, GattStatus status, ProfileState newState)
        {
            if (status == GattStatus.Success && newState == ProfileState.Connected)
            {
                try
                {
                    gatt.DiscoverServices();
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Exception while discovering services:  " + ex, LoggingLevel.Normal, GetType());
                    DisconnectPeripheral(gatt);
                }
            }
            // ensure that all disconnected peripherals get closed (released). without closing, we'll use up all the BLE interfaces.
            else if (newState == ProfileState.Disconnected)
            {
                try
                {
                    gatt.Close();
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Exception while closing disconnected client:  " + ex, LoggingLevel.Normal, GetType());
                }
            }
        }

        public override void OnServicesDiscovered(BluetoothGatt gatt, GattStatus status)
        {
            BluetoothGattService deviceIdService;
            try
            {
                deviceIdService = gatt.GetService(_probe.DeviceIdService.Uuid);
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while getting device ID service:  " + ex, LoggingLevel.Normal, GetType());
                DisconnectPeripheral(gatt);
                return;
            }

            BluetoothGattCharacteristic deviceIdCharacteristic;
            try
            {
                deviceIdCharacteristic = deviceIdService.GetCharacteristic(_probe.DeviceIdCharacteristic.Uuid);
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while getting device ID characteristic:  " + ex, LoggingLevel.Normal, GetType());
                DisconnectPeripheral(gatt);
                return;
            }

            try
            {
                gatt.ReadCharacteristic(deviceIdCharacteristic);
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while reading device ID characteristic:  " + ex, LoggingLevel.Normal, GetType());
                DisconnectPeripheral(gatt);
            }
        }

        public override void OnCharacteristicRead(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, GattStatus status)
        {
            try
            {
                byte[] deviceIdBytes = characteristic.GetValue();
                string deviceIdEncountered = Encoding.UTF8.GetString(deviceIdBytes);
                DeviceIdEncountered?.Invoke(this, new BluetoothDeviceProximityDatum(_encounterTimestamp, deviceIdEncountered));
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while getting device ID characteristic value after reading it:  " + ex, LoggingLevel.Normal, GetType());
            }
            finally
            {
                DisconnectPeripheral(gatt);
            }
        }

        private void DisconnectPeripheral(BluetoothGatt gatt)
        {
            try
            {
                gatt.Disconnect();
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while disconnecting peripheral:  " + ex, LoggingLevel.Normal, GetType());
            }
        }
    }
}
