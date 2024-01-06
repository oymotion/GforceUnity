/*
 * Copyright 2017, OYMotion Inc.
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in
 *    the documentation and/or other materials provided with the
 *    distribution.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 * "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
 * LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS
 * FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE
 * COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT,
 * INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
 * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS
 * OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED
 * AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
 * OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF
 * THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH
 * DAMAGE.
 *
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace gf
{

    public class Device
    {
        public const int MAX_DEVICE_STR_LEN = 64;

        public enum Gesture
        {
            Relax = 0x00,
            Fist = 0x01,
            SpreadFingers = 0x02,
            WaveIn = 0x03,
            WaveOut = 0x04,
            Pinch = 0x05,
            Shoot = 0x06,
            Undefined = 0xFF
        };

        public enum Status
        {
            None = 0,
            ReCenter = 1,
            UsbPlugged = 2,
            UsbPulled = 3,
            Motionless = 4,
        };

        public enum ConnectionStatus
        {
            Disconnected,
            Disconnecting,
            Connecting,
            Connected
        };

        public enum DataType
        {
            Invalid,
            Accelerate,
            Gyroscope,
            Magnetometer,
            Eulerangle,
            Quaternion,
            Rotationmatrix,
            Gesture,
            Emgraw,
            Hidmouse,
            Hidjoystick,
            Devicestatus,
            Log,
            MagAngle,
            MotorCurrent,
        };

        public enum DeviceType
        {
            None,

            GforceDual,
            GforceReh,
            GforceProPlus,    
            Gforce200,        
            GforceMotion,     
            GforceDuo,
            GfroceOct,

            ORehabLeg,  
            ORehabArm,

            NeuCirLite,
        }

        public delegate void ResultCallback(Device device, uint result);
        public delegate void ResultWithStringCallback(Device device, uint result, string val);
        public delegate void ResultWithUIntCallback(Device device, uint result, uint val);
        public delegate void ResultWithBytesCallback(Device device, uint result, byte[] val);

        public class ResultCallbackHolder
        {
            public ResultCallback resultCallback;
            public ResultWithStringCallback resultWithStringCallback;
            public ResultWithUIntCallback resultWithUIntCallback;
            public ResultWithBytesCallback resultWithBytesCallback;
        }


        public static Dictionary<String/*hD + cmd*/, ResultCallbackHolder> callbacks = new Dictionary<String, ResultCallbackHolder>();

        public DeviceType deviceType;

        public Device(IntPtr handle)
        {
            hD = handle;
        }

        public uint getAddrType()
        {
            return libgforce.device_get_addr_type(hD);
        }

        public RetCode getAddress(out string address)
        {
            StringBuilder str = new StringBuilder(MAX_DEVICE_STR_LEN);
            RetCode ret = libgforce.device_get_address(hD, str, (uint)str.Capacity);
            if (RetCode.GF_SUCCESS == ret)
            {
                address = str.ToString();
            }
            else
            {
                address = "";
            }
            return ret;
        }

        public string getAddress()
        {
            string address;
            getAddress(out address);

            return address;
        }

        public RetCode getName(out string name)
        {
            StringBuilder str = new StringBuilder(MAX_DEVICE_STR_LEN);
            RetCode ret = libgforce.device_get_name(hD, str, (uint)str.Capacity);

            if (RetCode.GF_SUCCESS == ret)
            {
                name = str.ToString();

                if (!name.EndsWith(")"))
                {
                    String addr = getAddress();

                    name += "(" + addr.Substring(addr.Length - 5).Replace(":", "") + ")";
                }
            }
            else
            {
                name = "" + ret;
            }

            return ret;
        }

        public string getName()
        {
            string name;
            getName(out name);
            return name;
        }

        public void SetDeviceType(DeviceType deviceType)
        {
            GForceLogger.Log("[Device] deviceType" + deviceType);
            this.deviceType = deviceType;
        }

        public DeviceType GetDeviceType()
        {
            return deviceType;
        }

        public uint getRssi()
        {
            return libgforce.device_get_rssi(hD);
        }

        public ConnectionStatus getConnectionStatus()
        {
            return libgforce.device_get_connection_status(hD);
        }

        public RetCode connect()
        {
            return libgforce.device_connect(hD);
        }

        public RetCode disconnect()
        {
            return libgforce.device_disconnect(hD);
        }


        public RetCode getFirmwareVersion(ResultWithStringCallback cb)
        {
            GForceLogger.Log("[getFirmwareVersion] hD: " + hD + ", cb: " + cb + ", callbacks.Count: " + callbacks.Count);

            lock (callbacks)
            {
                if (callbacks.ContainsKey(hD + "getFirmwareVersion"))
                {
                    callbacks.Remove(hD + "getFirmwareVersion");
                }

                ResultCallbackHolder cbHolder = new ResultCallbackHolder();
                cbHolder.resultWithStringCallback = cb;

                callbacks.Add(hD + "getFirmwareVersion", cbHolder);
            }

            return libgforce.device_get_firmware_ver(hD, (IntPtr hDevice, uint res, String ver) =>
            {
                lock (callbacks)
                {
                    if (callbacks.ContainsKey(hDevice + "getFirmwareVersion"))
                    {
                        callbacks[hDevice + "getFirmwareVersion"].resultWithStringCallback(new Device(hDevice), res, ver);
                        callbacks.Remove(hDevice + "getFirmwareVersion");
                    }
                }
            });
        }


        public RetCode getBatteryLevel(ResultWithUIntCallback cb)
        {
            GForceLogger.Log("[getBatteryLevel] hD: " + hD + ", cb: " + cb + ", callbacks.Count: " + callbacks.Count);

            lock (callbacks)
            {
                if (callbacks.ContainsKey(hD + "getBatteryLevel"))
                {
                    callbacks.Remove(hD + "getBatteryLevel");
                }

                ResultCallbackHolder cbHolder = new ResultCallbackHolder();
                cbHolder.resultWithUIntCallback = cb;

                callbacks.Add(hD + "getBatteryLevel", cbHolder);
            }

            return libgforce.device_get_battery_level(hD, (IntPtr hDevice, uint res, uint level) =>
            {
                lock (callbacks)
                {
                    if (callbacks.ContainsKey(hDevice + "getBatteryLevel"))
                    {
                        callbacks[hDevice + "getBatteryLevel"].resultWithUIntCallback(new Device(hDevice), res, level);
                        callbacks.Remove(hDevice + "getBatteryLevel");
                    }
                }
            });
        }

        /// <summary>
        /// 获得功能表
        /// </summary>
        /// <param name="cb"></param>
        /// <returns></returns>
        public RetCode getFeatureMap(ResultWithUIntCallback cb)
        {
            GForceLogger.Log("[getFeatureMap] hD: " + hD + ", cb: " + cb + ", callbacks.Count: " + callbacks.Count);

            lock (callbacks)
            {
                if (callbacks.ContainsKey(hD + "getFeatureMap"))
                {
                    callbacks.Remove(hD + "getFeatureMap");
                }

                ResultCallbackHolder cbHolder = new ResultCallbackHolder();
                cbHolder.resultWithUIntCallback = cb;

                callbacks.Add(hD + "getFeatureMap", cbHolder);
            }

            return libgforce.device_get_feature_map(hD, (IntPtr hDevice, uint res, uint map) =>
            {
                lock (callbacks)
                {
                    if (callbacks.ContainsKey(hDevice + "getFeatureMap"))
                    {
                        callbacks[hDevice + "getFeatureMap"].resultWithUIntCallback(new Device(hDevice), res, map);
                        callbacks.Remove(hDevice + "getFeatureMap");
                    }
                }
            });
        }

        public RetCode setEmgConfig(uint sampleRateHz, uint interestedChannels, uint packageDataLength, uint adcResolution, ResultCallback cb)
        {
            GForceLogger.Log("[setEmgConfig] hD: " + hD + ", cb: " + cb + ", callbacks.Count: " + callbacks.Count);

            lock (callbacks)
            {
                if (callbacks.ContainsKey(hD + "setEmgConfig"))
                {
                    callbacks.Remove(hD + "setEmgConfig");
                }

                ResultCallbackHolder cbHolder = new ResultCallbackHolder();
                cbHolder.resultCallback = cb;

                callbacks.Add(hD + "setEmgConfig", cbHolder);
            }
            
            return libgforce.device_set_emg_config(hD, sampleRateHz, interestedChannels, packageDataLength, adcResolution, (IntPtr hDevice, uint res) =>
            {
                lock (callbacks)
                {
                    if (callbacks.ContainsKey(hDevice + "setEmgConfig"))
                    {
                        callbacks[hDevice + "setEmgConfig"].resultCallback(new Device(hDevice), res);
                        callbacks.Remove(hDevice + "setEmgConfig");
                    }
                }
            });
        }

        public RetCode setDataSwitch(uint notifSwitch, ResultCallback cb)
        {
            GForceLogger.Log("[setDataSwitch] hD: " + hD + ", cb: " + cb + ", callbacks.Count: " + callbacks.Count);
            
            lock (callbacks)
            {
                if (callbacks.ContainsKey(hD + "setDataSwitch"))
                {
                    callbacks.Remove(hD + "setDataSwitch");
                }

                ResultCallbackHolder cbHolder = new ResultCallbackHolder();
                cbHolder.resultCallback = cb;

                callbacks.Add(hD + "setDataSwitch", cbHolder);
            }

            return libgforce.device_set_data_switch(hD, notifSwitch, (IntPtr hDevice, uint res) =>
            {
                lock (callbacks)
                {
                    if (callbacks.ContainsKey(hDevice + "setDataSwitch"))
                    {
                        callbacks[hDevice + "setDataSwitch"].resultCallback(new Device(hDevice), res);
                        callbacks.Remove(hDevice + "setDataSwitch");
                    }
                    else
                    {
                        GForceLogger.Log("[setDataSwitch] unhandled for device " + hDevice + ", callbacks.Count: " + callbacks.Count);
                    }
                }
            });
        }

        public RetCode enableDataNotification(uint enable)
        {
            return libgforce.device_enable_data_notification(hD, enable);
        }

        public RetCode getAppControlMode(ResultWithBytesCallback cb)
        {
            GForceLogger.Log("[getAppControlMode] hD: " + hD + ", cb: " + cb + ", callbacks.Count: " + callbacks.Count);

            lock (callbacks)
            {
                if (callbacks.ContainsKey(hD + "getAppControlMode"))
                {
                    callbacks.Remove(hD + "getAppControlMode");
                }

                ResultCallbackHolder cbHolder = new ResultCallbackHolder();
                cbHolder.resultWithBytesCallback = cb;

                callbacks.Add(hD + "getAppControlMode", cbHolder);
            }

            return libgforce.device_get_app_control_mode(hD, (IntPtr hDevice, uint res, uint dataLen, System.IntPtr data) =>
            {
                lock (callbacks)
                {
                    if (callbacks.ContainsKey(hDevice + "getAppControlMode"))
                    {
                        callbacks[hDevice + "getAppControlMode"].resultWithBytesCallback(new Device(hDevice), res, getBytes(dataLen, data));
                        callbacks.Remove(hDevice + "getAppControlMode");
                    }
                    else
                    {
                        GForceLogger.Log("[getAppControlMode] unhandled for device " + hDevice + ", callbacks.Count: " + callbacks.Count);
                    }
                }
            });
        }

            public RetCode setAppControlMode(uint enable, ResultCallback cb)
        {
            GForceLogger.Log("[setAppControlMode] hD: " + hD + ", cb: " + cb + ", callbacks.Count: " + callbacks.Count);

            lock (callbacks)
            {
                if (callbacks.ContainsKey(hD + "setAppControlMode"))
                {
                    callbacks.Remove(hD + "setAppControlMode");
                }

                ResultCallbackHolder cbHolder = new ResultCallbackHolder();
                cbHolder.resultCallback = cb;

                callbacks.Add(hD + "setAppControlMode", cbHolder);
            }

            return libgforce.device_set_app_control_mode(hD, enable, (IntPtr hDevice, uint res) =>
            {
                lock (callbacks)
                {
                    if (callbacks.ContainsKey(hDevice + "setAppControlMode"))
                    {
                        callbacks[hDevice + "setAppControlMode"].resultCallback(new Device(hDevice), res);
                        callbacks.Remove(hDevice + "setAppControlMode");
                    }
                    else
                    {
                        GForceLogger.Log("[setAppControlMode] unhandled for device " + hDevice + ", callbacks.Count: " + callbacks.Count);
                    }
                }
            });
        }

        public RetCode turnToAngle(uint index, uint angle, ResultCallback cb)
        {
            GForceLogger.Log("[turnToAngle] hD: " + hD + ", cb: " + cb + ", callbacks.Count: " + callbacks.Count);

            lock (callbacks)
            {
                if (callbacks.ContainsKey(hD + "turnToAngle"))
                {
                    callbacks.Remove(hD + "turnToAngle");
                }

                ResultCallbackHolder cbHolder = new ResultCallbackHolder();
                cbHolder.resultCallback = cb;

                callbacks.Add(hD + "turnToAngle", cbHolder);
            }

            return libgforce.device_turn_to_angle(hD, index, angle, (IntPtr hDevice, uint res) =>
            {
                lock (callbacks)
                {
                    if (callbacks.ContainsKey(hDevice + "turnToAngle"))
                    {
                        callbacks[hDevice + "turnToAngle"].resultCallback(new Device(hDevice), res);
                        callbacks.Remove(hDevice + "turnToAngle");
                    }
                    else
                    {
                        GForceLogger.Log("[turnToAngle] unhandled for device " + hDevice + ", callbacks.Count: " + callbacks.Count);
                    }
                }
            });
        }

        public RetCode getNeuCirParams(ResultWithBytesCallback cb)
        {
            GForceLogger.Log("[getNeuCirParams] hD: " + hD + ", cb: " + cb + ", callbacks.Count: " + callbacks.Count);

            lock (callbacks)
            {
                if (callbacks.ContainsKey(hD + "getNeuCirParams"))
                {
                    callbacks.Remove(hD + "getNeuCirParams");
                }

                ResultCallbackHolder cbHolder = new ResultCallbackHolder();
                cbHolder.resultWithBytesCallback = cb;

                callbacks.Add(hD + "getNeuCirParams", cbHolder);
            }

            return libgforce.device_get_neucir_params(hD, (IntPtr hDevice, uint res, uint dataLen, System.IntPtr data) =>
            {
                lock (callbacks)
                {
                    if (callbacks.ContainsKey(hDevice + "getNeuCirParams"))
                    {
                        callbacks[hDevice + "getNeuCirParams"].resultWithBytesCallback(new Device(hDevice), res, getBytes(dataLen, data));
                        callbacks.Remove(hDevice + "getNeuCirParams");
                    }
                    else
                    {
                        GForceLogger.Log("[getNeuCirParams] unhandled for device " + hDevice + ", callbacks.Count: " + callbacks.Count);
                    }
                }
            });
        }

        public RetCode setNeuCirParams(byte[] data, ResultCallback cb)
        {
            GForceLogger.Log("[setNeuCirParams] hD: " + hD + ", cb: " + cb + ", callbacks.Count: " + callbacks.Count);

            lock (callbacks)
            {
                if (callbacks.ContainsKey(hD + "setNeuCirParams"))
                {
                    callbacks.Remove(hD + "setNeuCirParams");
                }

                ResultCallbackHolder cbHolder = new ResultCallbackHolder();
                cbHolder.resultCallback = cb;

                callbacks.Add(hD + "setNeuCirParams", cbHolder);
            }
            GCHandle hObject = GCHandle.Alloc(data, GCHandleType.Pinned);
            IntPtr pObject = hObject.AddrOfPinnedObject();
            if (!hObject.IsAllocated)
            {
                return RetCode.GF_ERROR_BAD_PARAM;
            }
            return libgforce.device_set_neucir_params(hD, (uint)data.Length, pObject, (IntPtr hDevice, uint res) =>
            {
                hObject.Free();
                lock (callbacks)
                {
                    if (callbacks.ContainsKey(hDevice + "setNeuCirParams"))
                    {
                        callbacks[hDevice + "setNeuCirParams"].resultCallback(new Device(hDevice), res);
                        callbacks.Remove(hDevice + "setNeuCirParams");
                    }
                    else
                    {
                        GForceLogger.Log("[setNeuCirParams] unhandled for device " + hDevice + ", callbacks.Count: " + callbacks.Count);
                    }
                }
            });
        }

        private static byte[] getBytes(uint dataLen, IntPtr data)
        {
            byte[] dataarray = new byte[dataLen];
            Marshal.Copy(data, dataarray, 0, (int)dataLen);
            return dataarray;
        }

        private IntPtr hD;
    }
}
