///////////////////////////////////////////////////////////
//  GForceDevice.cs
//  Implementation of the Class GForceDevice
//  Generated by Enterprise Architect
//  Created on:      02-2月-2021 16:07:33
//  Original author: hebin
///////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using gf;


namespace GForce
{
    public class GForceDevice
    {
        public enum GForceDeviceState : int
        {
            STATE_DISCONNECTED = 0,
            STATE_CONNECTING = 1,
            STATE_CONNECTED = 2,
            STATE_DISCONNECTING = 3,
            STATE_READY = 4
        }//end DeviceState

        public struct SimpleQuaternion
        {
            public SimpleQuaternion(float w, float x, float y, float z)
            {
                this.w = w;
                this.x = x;
                this.y = y;
                this.z = z;
            }

            public float w;
            public float x;
            public float y;
            public float z;
        }

        private enum InternalState : int
        {
            STATUS_DISCONNECTED,
            STATUS_CONNECTED,
            STATUS_EMG_CONFIG_PENDING,
            STATUS_EMG_CONFIG_IN_PROGRESS,
            STATUS_EMG_CONFIG_ERROR,
            STATUS_EMG_CONFIG_FINISHED,
            STATUS_DATA_SWITCH_PENDING,
            STATUS_DATA_SWITCH_IN_PROGRESS,
            STATUS_DATA_SWITCH_ERROR,
            STATUS_DATA_SWITCH_FINISHED,
            STATUS_IDLE
        }//end InternalState


        private static Dictionary<String/*address*/, GForceDevice> deviceMapping = new Dictionary<String, GForceDevice>();

        private Device device;

        protected Quaternion quaternion = Quaternion.Identity;
        Quaternion restQuaternion = Quaternion.Identity;
        Quaternion primordialQuaternion;
        protected uint gesture = 0;
        private byte[] adcData;


        protected long _lastGestureDataTime = 0;
        protected long _lastEMGDataTime = 0;
        protected long _lastQuaternionDataTime = 0;

        bool isSetInternalState = false;

        private InternalState internalState
        {
            get
            {
                return internalStateValue;
            }
            set
            {
                GForceLogger.Log("{GForceDevice}[internalState]set is " + value);
                internalStateValue = value;
                isSetInternalState = true;
            }
        }

        private InternalState internalStateValue = InternalState.STATUS_DISCONNECTED;

        private long _connectedTime = -1;
        private long _errorTime = -1;
        private long _configResetTime = -1;

        private uint _dataSwitchNew = 0;
        private uint _dataSwitchPending = 0;
        private uint _prevDataSwitchSet = 0;

        private uint _emgSampleRateNew = 0;
        private uint _emgSampleRatePending = 0;
        private uint _prevEmgSampleRateSet = 0;

        private uint _emgChannelMapNew = 0x00;
        private uint _emgChannelMapPending = 0x00;
        private uint _prevEmgChannelMapSet = 0x00;

        private uint _emgResolutionNew = 0;
        private uint _emgResolutionPending = 0;
        private uint _prevEmgResolutionSet = 0;

        private uint _emgPacketLenNew = 0;
        private uint _emgPacketLenPending = 0;
        private uint _prevEmgPacketLenSet = 0;

        protected List<int> _prevEmgChannelsSet;

        uint dataSwitchNew = (uint)DataNotifFlags.DNF_DEVICE_STATUS;

        public int ChannelMapCount {  get; private set; } = 0; //注意:仅作为显示参数

        uint dataSwichMap = (uint)DataNotifFlags.DNF_DEVICE_STATUS;

        private List<GfroceData> updataData =new List<GfroceData>(); //更新数据记录

        //float waitTime = 0;

        //bool isWait = false;

        ~GForceDevice()
        {
            device.disconnect();
            device = null;
        }

        public GForceDevice()
        {
            //internalState = InternalState.STATUS_DISCONNECTED;
        }

        public Device GetDevice()
        {
            return device;
        }

        /// <summary>
        /// 获取四元数
        /// </summary>
        /// <returns></returns>
        public Quaternion GetQuaternion()
        {
            lock (this)
            {

                quaternion = primordialQuaternion;

                quaternion = restQuaternion * quaternion;

                return quaternion;
            }
        }
        
        /// <summary>
        /// 陀螺仪数据复位
        /// </summary>
        public virtual void RestOrientationData()
        {
            lock (this)
            {
                restQuaternion = Quaternion.Inverse(primordialQuaternion);
                GForceLogger.Log("复原后值为:" + (restQuaternion * quaternion));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public uint GetGesture()
        {
            lock (this)
            {
                return gesture;
            }
        }


        public byte[] GetRawADC()
        {
            lock (this)
            {
                return adcData;
            }
        }

        /// <summary>
        /// 返回存储的所有数据并清空
        /// </summary>
        /// <returns></returns>
        public List<GfroceData> GetUpdata()
        {
            List<GfroceData> data = new List<GfroceData>();
            //List<GfroceData[]> data = new List<GfroceData[]>(updataData.ToArray());
            //data.AddRange(updataData.ToArray());
            for (int i = 0; i < updataData.Count; i++)
            {
                data.Add(updataData[i]);
            }
            updataData.Clear();
            return data;
        }

        public void ClearUpdataData()
        {
            updataData.Clear();
        }

        /// <summary>
        /// 添加数据
        /// </summary>
        /// <param name="data"></param>
        private void AddUpdataData(byte[] data, Device.DataType type)
        {
            GfroceData tmp = new GfroceData();
            tmp.exData = data;
            tmp.dataType = type;
            updataData.Add(tmp);
        }



        public GForceDeviceState GetDeviceState()
        {
            GForceDeviceState state = GForceDeviceState.STATE_DISCONNECTED;

            if (device == null)
            {
                return state;
            }

            switch (device.getConnectionStatus())
            {
                case Device.ConnectionStatus.Disconnected:
                    state = GForceDeviceState.STATE_DISCONNECTED;
                    break;

                case Device.ConnectionStatus.Disconnecting:
                    state = GForceDeviceState.STATE_DISCONNECTING;
                    break;

                case Device.ConnectionStatus.Connecting:
                    state = GForceDeviceState.STATE_CONNECTING;
                    break;

                case Device.ConnectionStatus.Connected:
                    if (_configResetTime > 0 || internalState != InternalState.STATUS_IDLE)
                        state = GForceDeviceState.STATE_CONNECTED;
                    else
                        state = GForceDeviceState.STATE_READY;
                    break;
            }
            return state;
        }

        /// <summary>
        /// back
        /// </summary>
        /// <param name="device"></param>
        public void onDeviceConnected(Device device)
        {
            if (this.device != null)
            {
                return;
            }

            GForceLogger.Log("[GForceDevice] onDeviceConnected ");
            lock (deviceMapping)
            {
                if (deviceMapping.ContainsKey(device.getAddress()))
                {
                    GForceLogger.LogWarning("onDeviceConnected(): duplicated device " + device.getAddress());
                    deviceMapping.Remove(device.getAddress());
                }

                deviceMapping.Add(device.getAddress(), this);
            }

            this.device = device;
            _connectedTime = GetTimeInMilliseconds();

            internalState = InternalState.STATUS_CONNECTED;

            if (_prevDataSwitchSet != 0) _dataSwitchNew = _prevDataSwitchSet;
            _dataSwitchPending = 0;
            _prevDataSwitchSet = 0;

            if (_prevEmgSampleRateSet != 0) _emgSampleRateNew = _prevEmgSampleRateSet;
            _emgSampleRatePending = 0;
            _prevEmgSampleRateSet = 0;

            if (_prevEmgResolutionSet != 0) _emgResolutionNew = _prevEmgResolutionSet;
            _emgResolutionPending = 0;
            _prevEmgResolutionSet = 0;

            if (_prevEmgChannelMapSet != 0) _emgChannelMapNew = _prevEmgChannelMapSet;
            _emgChannelMapPending = 0x00;
            _prevEmgChannelMapSet = 0x00;

            if (_prevEmgPacketLenSet != 0) _emgPacketLenNew = _prevEmgPacketLenSet;
            _emgPacketLenPending = 0;
            _prevEmgPacketLenSet = 0;

            _prevEmgChannelsSet = null;


            //uint dataSwitchNew = (uint)DataNotifFlags.DNF_DEVICE_STATUS;

            if (device != null)
            {
                switch (device.GetDeviceType())
                {
                    case Device.DeviceType.None:
                        {
                            _emgSampleRateNew = 0;
                            _emgChannelMapNew = 0;
                            _emgResolutionNew = 0;
                            _emgPacketLenNew = 0;
                            break;
                        }
                    case Device.DeviceType.GforceDuo:
                    case Device.DeviceType.GforceDual:/*_emgSampleRateNew 有内部处理 */
                        {
                            _emgSampleRateNew = 50;
                            _emgChannelMapNew = 0x03;
                            _emgResolutionNew = 8;
                            _emgPacketLenNew = 128;
                            dataSwichMap |= (uint)DataNotifFlags.DNF_QUATERNION;
                            dataSwichMap |= (uint)DataNotifFlags.DNF_EMG_RAW;
                            dataSwitchNew |= (uint)DataNotifFlags.DNF_QUATERNION;
                            dataSwitchNew |= (uint)DataNotifFlags.DNF_EMG_RAW;
                            ChannelMapCount = 2;
                            break;
                        }
                    case Device.DeviceType.GforceReh:/*_emgSampleRateNew 有内部处理 */
                        {
                            _emgSampleRateNew = 650;
                            _emgChannelMapNew = 0xFF;
                            _emgResolutionNew = 8;
                            _emgPacketLenNew = 128;
                            dataSwichMap |= (uint)DataNotifFlags.DNF_QUATERNION;
                            dataSwichMap |= (uint)DataNotifFlags.DNF_EMG_RAW;
                            
                            dataSwitchNew |= (uint)DataNotifFlags.DNF_QUATERNION;
                            dataSwitchNew |= (uint)DataNotifFlags.DNF_EMG_RAW;
                            ChannelMapCount = 8;
                            break;
                        }
                    case Device.DeviceType.GfroceOct:
                    case Device.DeviceType.GforceProPlus: /*_emgSampleRateNew直接使用的原始值 */
                        {
                            _emgSampleRateNew = 650;
                            _emgChannelMapNew = 0xFF;
                            _emgResolutionNew = 8;
                            _emgPacketLenNew = 128;
                            dataSwichMap |= (uint)DataNotifFlags.DNF_QUATERNION;
                            dataSwichMap |= (uint)DataNotifFlags.DNF_EMG_RAW;
                            dataSwichMap |= (uint)DataNotifFlags.DNF_EMG_GESTURE;
                            dataSwitchNew |= (uint)DataNotifFlags.DNF_QUATERNION;
                            dataSwitchNew |= (uint)DataNotifFlags.DNF_EMG_RAW;
                            ChannelMapCount = 8;
                            break;
                        }
                    case Device.DeviceType.GforceMotion:
                        {
                            _emgSampleRateNew = 650;
                            _emgChannelMapNew = 0x00;
                            _emgResolutionNew = 8;
                            _emgPacketLenNew = 256;
                            dataSwichMap |= (uint)DataNotifFlags.DNF_QUATERNION;
                            //dataSwichMap |= (uint)DataNotifFlags.DNF_EMG_RAW;
                            dataSwitchNew |= (uint)DataNotifFlags.DNF_QUATERNION;
                            ChannelMapCount = 0;
                            break;
                        }
                    case Device.DeviceType.Gforce200:
                        {
                            _emgSampleRateNew = 650;
                            _emgChannelMapNew = 0x00;
                            _emgResolutionNew = 8;
                            _emgPacketLenNew = 128;
                            dataSwichMap |= (uint)DataNotifFlags.DNF_QUATERNION;
                            dataSwitchNew |= (uint)DataNotifFlags.DNF_QUATERNION;
                            ChannelMapCount = 0;
                            break;
                        }
                    case Device.DeviceType.ORehabArm:
                    case Device.DeviceType.ORehabLeg:
                        {
                            _emgSampleRateNew = 250;
                            _emgChannelMapNew = 0x01;
                            _emgResolutionNew = 8;
                            _emgPacketLenNew = 64;
                            dataSwichMap |= (uint)DataNotifFlags.DNF_QUATERNION;
                            dataSwichMap |= (uint)DataNotifFlags.DNF_EMG_RAW;
                            dataSwitchNew |= (uint)DataNotifFlags.DNF_QUATERNION;
                            ChannelMapCount = 1;
                            break;
                        }
                    case Device.DeviceType.NeuCirLite:
                        {
                            _emgSampleRateNew = 650;
                            _emgChannelMapNew = 0xFF;
                            _emgResolutionNew = 8;
                            _emgPacketLenNew = 64;
                            dataSwitchNew |= (uint)DataNotifFlags.DNF_QUATERNION;
                            dataSwichMap |= (uint)DataNotifFlags.DNF_QUATERNION;
                            dataSwichMap |= (uint)DataNotifFlags.DNF_MAGANG;
                            dataSwichMap |= (uint)DataNotifFlags.DNF_EMG_RAW;
                            dataSwichMap |= (uint)DataNotifFlags.DNF_EMG_GESTURE;

                            //dataSwitchNew |= (uint)DataNotifFlags.DNF_EMG_RAW;
                            //dataSwitchNew |= (uint)DataNotifFlags.DNF_MAGANG;
                            ChannelMapCount = 4;
                            adcData = new byte[2];
                            break;
                        }
                    default:
                        break;
                }
            }

            GForceLogger.Log("device" + device.getName() + "setEMG,_emgSampleRateNew" + _emgSampleRateNew
                + "_emgChannelMapNew:" + _emgChannelMapNew
                + "_emgResolutionNew" + _emgResolutionNew
                + "_emgPacketLenNew" + _emgPacketLenNew
                + "dataSwitchNew" + dataSwitchNew);

            SetEMGConfig(_emgSampleRateNew, _emgChannelMapNew, _emgResolutionNew, _emgPacketLenNew);
            SetDataSwitch(dataSwitchNew);

            _configResetTime = GetTimeInMilliseconds();
        }

        public void onDeviceDiscard(Device device)
        {

        }

        /// <summary>
        /// Callback when device disconnected
        /// </summary>
        /// <param name="device"></param>
        /// <param name="reason"></param>
        public void onDeviceDisconnected(Device device, int reason)
        {
            GForceLogger.Log("[GForceDevice] onDeviceDisconnected ");

            lock (deviceMapping)
            {
                deviceMapping.Remove(device.getAddress());
            }

            if (this.device != null && this.device.getAddress().Equals(device.getAddress()))
            {
                this.device = null;
                internalState = InternalState.STATUS_DISCONNECTED;
            }
        }

        public void onDeviceStatusChanged(Device device, Device.Status status)
        {
        }

        /// <summary>
        /// Set Device EMG config
        /// </summary>
        /// <param name="sampleRate">采样率</param>
        /// <param name="channelMap">16进制通道数量</param>
        /// <param name="resolution">ADC精度</param>
        /// <param name="packetLen">包长度</param>
        public void SetEMGConfig(uint sampleRate, uint channelMap, uint resolution, uint packetLen)
        {
            _emgSampleRateNew = sampleRate;
            _emgChannelMapNew = channelMap;
            _emgResolutionNew = resolution;
            _emgPacketLenNew = packetLen;

            _configResetTime = GetTimeInMilliseconds();
        }

        public uint GetEMGConfigSampleRate()
        {
            return _emgSampleRateNew;
        }

        public uint GetEMGConfigChannelMap()
        {
            return _emgChannelMapNew;
        }

        public uint GetEMGConfigResolution()
        {
            return _emgResolutionNew;
        }

        public uint GetEMGConfigPacketLen()
        {
            return _emgPacketLenNew;
        }

        /// <summary>
        /// Add device channel switch
        /// </summary>
        /// <param name="dataNotifFlags"></param>
        public void AddDataSwitch(uint dataNotifFlags)
        {
            if ((dataSwichMap & dataNotifFlags) == 0) 
            {
                return;
            }

            if (device != null)
            {
                dataSwitchNew |= dataNotifFlags;
                SetDataSwitch(dataSwitchNew);
            }
        }

        /// <summary>
        /// Rest Data Switch
        /// </summary>
        public void RestDataSwitch()
        {

            if (device != null)
            {
                dataSwitchNew = (uint)DataNotifFlags.DNF_DEVICE_STATUS;
                SetDataSwitch(dataSwitchNew);
            }
        }

        public bool CheckDataSwitchMap(uint dataSwitch)
        {
            if ((dataSwitch & dataSwichMap) >0)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Set Data Switch
        /// </summary>
        /// <param name="dataSwitch">打开的通道的集合</param>
        public void SetDataSwitch(uint dataSwitch)
        {
            _dataSwitchNew = dataSwitch;

            _configResetTime = GetTimeInMilliseconds();
        }

        public void TickGForce()
        {
            //GForceLogger.Log("internalState: " + internalState);

            //if (GetTimeInMilliseconds() - _connectedTime > waitTime)
            //{
            //    isWait = false;
            //    isSetInternalState = true;
            //}
             

            if (isSetInternalState)
            {
                isSetInternalState = true;
                // State machine
                switch (internalState)
                {
                    case InternalState.STATUS_DISCONNECTED:
                        // Nothing
                        break;

                    case InternalState.STATUS_CONNECTED:
                        isSetInternalState = true;
                        if (GetTimeInMilliseconds() - _connectedTime > 2000)
                        {
                            internalState = InternalState.STATUS_IDLE;

                        }
                        break;

                    case InternalState.STATUS_EMG_CONFIG_ERROR:
                        isSetInternalState = true;
                        if (_errorTime < 0)
                        {
                            _errorTime = GetTimeInMilliseconds();
                        }
                        else if (GetTimeInMilliseconds() - _errorTime > 2000)
                        {
                            internalState = InternalState.STATUS_EMG_CONFIG_PENDING;
                            
                        }
                        break;

                    case InternalState.STATUS_EMG_CONFIG_PENDING:
                        {
                            RetCode res;

                            internalState = InternalState.STATUS_EMG_CONFIG_IN_PROGRESS;

                            // Disable notification
                            res = device.enableDataNotification(0);
                            GForceLogger.Log("Return of enableDataNotification(0): " + res);

                            Thread.Sleep(1000);

                            // Set EMG config
                            _errorTime = -1;

                            GForceLogger.Log("setEmgConfig deviceName" + device.getName() + "_emgSampleRatePending:" + _emgSampleRatePending
                                + "_emgChannelMapPending" + _emgChannelMapPending
                                + "_emgPacketLenPending" + _emgPacketLenPending
                                + "_emgResolutionPending" + _emgResolutionPending);

                            res = device.setEmgConfig(_emgSampleRatePending, _emgChannelMapPending, _emgPacketLenPending, _emgResolutionPending, (Device device, uint resp) =>
                            {
                                GForceLogger.LogFormat("Response of setEmgConfig(...): {0}, Device: {1}", (RetCode)resp, device.getAddress());

                                GForceDevice dev = GForceDevice.GetControllerForDevice(device);

                                if (dev != null)
                                {
                                    dev.EMGConfigCallback(resp);
                                }
                            });

                            GForceLogger.Log("Return of setEmgConfig(...): " + res);
                        }
                        break;

                    case InternalState.STATUS_EMG_CONFIG_IN_PROGRESS:
                        // Nothing
                        break;

                    case InternalState.STATUS_EMG_CONFIG_FINISHED:
                        {
                            RetCode res;

                            // Enable notification
                            res = device.enableDataNotification(1);
                            GForceLogger.Log("Return of enableDataNotification(1): " + res);

                            internalState = InternalState.STATUS_IDLE;
                        }
                        break;

                    case InternalState.STATUS_DATA_SWITCH_ERROR:
                        isSetInternalState = true;
                        if (_errorTime < 0)
                        {
                            _errorTime = GetTimeInMilliseconds();
                            
                        }
                        else if (GetTimeInMilliseconds() - _errorTime > 2000)
                        {
                            internalState = InternalState.STATUS_DATA_SWITCH_PENDING;
                        }
                        break;

                    case InternalState.STATUS_DATA_SWITCH_PENDING:
                        {
                            RetCode res;

                            internalState = InternalState.STATUS_DATA_SWITCH_IN_PROGRESS;

                            // Disable notification
                            res = device.enableDataNotification(0);
                            GForceLogger.Log("Return of enableDataNotification(0): " + res);

                            // Set data switch
                            _errorTime = -1;

                            res = device.setDataSwitch(_dataSwitchPending, (Device device, uint resp) =>
                            {
                                GForceLogger.LogFormat("Response of setDataSwitch({0}): {1}, Device: {2}", _dataSwitchPending, (RetCode)resp, device.getAddress());

                                GForceDevice dev = GetControllerForDevice(device);

                                if (dev != null)
                                {
                                    dev.DataSwitchConfigCallback(resp);
                                }
                            });

                            GForceLogger.Log("setDataSwitch(" + _dataSwitchPending + ") returned: " + res);
                        }
                        break;

                    case InternalState.STATUS_DATA_SWITCH_IN_PROGRESS:
                        // Nothing
                        break;

                    case InternalState.STATUS_DATA_SWITCH_FINISHED:
                        {
                            RetCode res;

                            // Enable notification
                            res = device.enableDataNotification(1);
                            GForceLogger.Log("Return of enableDataNotification(1): " + res);

                            internalState = InternalState.STATUS_IDLE;

                        }
                        break;

                    case InternalState.STATUS_IDLE:
                        isSetInternalState = true;
                        if (_configResetTime > 0 && GetTimeInMilliseconds() - _configResetTime > 200)
                        {
                            if (_emgChannelMapNew != 0 &&
                                (_emgSampleRateNew != _prevEmgSampleRateSet ||
                                 _emgChannelMapNew != _prevEmgChannelMapSet ||
                                 _emgResolutionNew != _prevEmgResolutionSet ||
                                 _emgPacketLenNew != _prevEmgPacketLenSet))
                            {
                                GForceLogger.LogFormat("_emgSampleRateNew:{0}， _emgChannelMapNew: {1}, _emgResolutionNew: {2}, _emgPacketLenNew: {3}",
                                    _emgSampleRateNew, _emgChannelMapNew, _emgResolutionNew, _emgPacketLenNew);

                                GForceLogger.LogFormat("_prevEmgSampleRateSet:{0}， _prevEmgChannelMapSet: {1}, _prevEmgResolutionSet: {2}, _prevEmgPacketLenSet: {2}",
                                    _prevEmgSampleRateSet, _prevEmgChannelMapSet, _prevEmgResolutionSet, _prevEmgPacketLenSet);

                                _emgSampleRatePending = _emgSampleRateNew;
                                _emgChannelMapPending = _emgChannelMapNew;
                                _emgResolutionPending = _emgResolutionNew;
                                _emgPacketLenPending = _emgPacketLenNew;

                                internalState = InternalState.STATUS_EMG_CONFIG_PENDING;
                            }

                            _configResetTime = -1;
                            
                        }
                        else if (_dataSwitchNew != _prevDataSwitchSet)
                        {
                            GForceLogger.LogFormat("_dataSwitchNew: {0}， current: {1}", _dataSwitchNew, _prevDataSwitchSet);

                            _dataSwitchPending = _dataSwitchNew;

                            internalState = InternalState.STATUS_DATA_SWITCH_PENDING;
                        }

                        break;
                } //end of switch (InternalState)
            } // end of if (isSetInternalState)
        }

        public virtual void UpdateEMGData(byte[] emgData)
        {
            GForceLogger.Log("emgData.Length: " + emgData.Length);

            lock (this)
            {
                if(adcData!= emgData)
                {
                    adcData = emgData;

                    byte[] data = new byte[ChannelMapCount];
                    for (int k = 0, l = 0; k < emgData.Length; k++)
                    {
                        data[l] = emgData[k];
                        if (l == ChannelMapCount - 1) 
                        {
                            l = 0;
                            //AddUpdataData(data, Device.DataType.Emgraw);
                            //DataPanel.AddData(data, GetDevice().getName(), Device.DataType.Emgraw.ToString());
                            //DataPanel.AddData(data,device.getName());
                        }
                        else
                        {
                            l++;
                        }
                    }
                }

            }

            _lastEMGDataTime = GetTimeInMilliseconds();
        }

        public virtual void UpdateMagAngleData(byte[] emgData)
        {
            //GForceLogger.Log("emgData.Length: " + emgData.Length);

            //只需要取第二个数值就好
            lock (this)
            {
                if (adcData !=  emgData)
                {
                    adcData = emgData;

                }

            }

            _lastEMGDataTime = GetTimeInMilliseconds();
        }

        public virtual void UpdateOrientationData(float w, float x, float y, float z)
        {
            lock (this)
            {
                primordialQuaternion.W = w;
                primordialQuaternion.X = x;
                primordialQuaternion.Y = y;
                primordialQuaternion.Z = z;
            }
            Quaternion tmp = GetQuaternion();

            byte[] data= { (byte)(tmp.W * 100), (byte)(tmp.X * 100), (byte)(tmp.Y * 100), (byte)(tmp.Z * 100) };
            
            //AddUpdataData(data, Device.DataType.Quaternion);
            _lastQuaternionDataTime = GetTimeInMilliseconds();
        }

        public virtual bool GetAppControlMode()
        {
            //两个bit 
            //第一个是判断是否在app控制模式  0是关 1是开
            //第二个是判断设备是否在运动     0是停止 1是运动
            //bool _value = false;
            device.getAppControlMode((Device device,uint resp,byte[] val)=> 
            {
                GForceLogger.Log("[GForceDevice]{GetAppControlMode}" + val);
            });
            return false;
        }

        public virtual void SetAppControlMode(bool _enable = false)
        {
            uint enable = 0;
            if (_enable)
            {
                enable = 1;
            }

            RetCode retcode = device.setAppControlMode(enable, (Device device, uint resp) => 
            {
                GForceLogger.LogFormat("Response of SetAppControlMode({0}): {1}, Device: {2}", enable, (RetCode)resp, device.getAddress());
            });

            GForceLogger.Log("[GFroceDevice]{SetAppControlMode}: " + retcode);
        }

        public virtual void TurnToAngle(uint _index, uint _angle)
        {
            RetCode retcode = device.turnToAngle(_index, _angle,(Device device, uint resp) =>
            {
                GForceLogger.LogFormat("Response of TurnToAngle({0}{1}): {2}, Device: {3}", _index, _angle,(RetCode)resp, device.getAddress());
            });

            GForceLogger.Log("[GFroceDevice]{TurnToAngle}: " + retcode);
        }

        public virtual byte[] GetNeuCirParams()
        {

            byte[] NeuCirParams =new byte[6];
            device.getNeuCirParams((Device device, uint resp, byte[] val) =>
            {
                //0: 手势最小持续时间
                //1: 手势最小保持时间
                //2: 屈肘手势
                //3: 伸肘手势
                //4: 最小角度
                //5: 最大角度
                GForceLogger.Log("[GForceDevice]{GetNeuCirParams}" + val.Length);
                for (int i = 0; i < val.Length; i++)
                {
                    GForceLogger.Log("[GForceDevice]{GetNeuCirParams}val  I :" +i+"  value:  " + val[i]);
                }
                NeuCirParams = val;
            });
            return NeuCirParams;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_data"></param>
        public virtual void SetNeuCirParams(byte[] _data)
        {
            device.setNeuCirParams(_data, (Device device, uint resp) =>
            {
                GForceLogger.LogFormat("Response of SetNeuCirParams({0}), Device: {1}",(RetCode)resp, device.getAddress());
            });
        }

        public virtual void UpdateGesture(uint gesture)
        {
            lock (this)
            {
                this.gesture = gesture;
            }

            _lastGestureDataTime = GetTimeInMilliseconds();
        }

        public static List<Device> GetConnectedDevices()    // TODO: Optimize
        {
            GForceLogger.Log("deviceMapping.length: " + deviceMapping.Count);

            List<Device> connectedDevices = new List<Device>();

            lock (deviceMapping)
            {
                foreach (var dev in deviceMapping.Values)
                {
                    connectedDevices.Add(dev.GetDevice());
                }
            }

            return connectedDevices;
        }

        private static GForceDevice GetControllerForDevice(Device device)
        {
            GForceLogger.Log("deviceMapping.length: " + deviceMapping.Count);

            lock (deviceMapping)
            {
                if (deviceMapping.ContainsKey(device.getAddress()))
                {
                    return deviceMapping[device.getAddress()];
                }
                else
                {
                    return null;
                }
            }
        }

        private void EMGConfigCallback(uint resp)
        {
            _prevEmgSampleRateSet = _emgSampleRatePending;
            _prevEmgChannelMapSet = _emgChannelMapPending;
            _prevEmgResolutionSet = _emgResolutionPending;
            _prevEmgPacketLenSet = _emgPacketLenPending;

            _prevEmgChannelsSet = new List<int>();

            for (int i = 0; i < 8; i++)
            {
                if ((_prevEmgChannelMapSet & (1 << i)) != 0)
                    _prevEmgChannelsSet.Add(i);
            }

            if (internalState == InternalState.STATUS_EMG_CONFIG_IN_PROGRESS)
            {
                // Don't set status if changed elsewere
                if ((RetCode)resp == RetCode.GF_SUCCESS)
                {
                    internalState = InternalState.STATUS_EMG_CONFIG_FINISHED;
                }
                else
                {
                    internalState = InternalState.STATUS_EMG_CONFIG_ERROR;
                }
            }
        }

        private void DataSwitchConfigCallback(uint resp)
        {
            if (internalState == InternalState.STATUS_DATA_SWITCH_IN_PROGRESS)
            {
                if ((RetCode)resp == RetCode.GF_SUCCESS)
                {
                    _prevDataSwitchSet = _dataSwitchPending;
                    internalState = InternalState.STATUS_DATA_SWITCH_FINISHED;
                }
                else
                {
                    internalState = InternalState.STATUS_DATA_SWITCH_ERROR;
                }
            }
        }

        private long GetTimeInMilliseconds()
        {
            return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }

    }//end GForceDevice

    public class GfroceData
    {
        public  byte[] exData;
        public Device.DataType dataType;
    }
}//end namespace GForce