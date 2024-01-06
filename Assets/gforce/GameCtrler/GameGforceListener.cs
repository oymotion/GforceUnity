using GForce;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameCtrler;
using gf;
using static gf.Device;
using System;

public class GameGforceListener
{
    #region GforceListener
    public class UIListenerImpl : UIListener
    {
        GameGforceListener gameGforceListener;

        /// 
        /// <param name="device"></param>
        public override void onDeviceConnected(Device device)
        {
            Debug.Log("onDeviceConnected  address: " + device.getAddress());
        }

        /// 
        /// <param name="device"></param>
        public override void onDeviceDiscard(Device device)
        {
            Debug.Log("onDeviceDiscard  address: " + device.getAddress());

        }

        /// 
        /// <param name="device"></param>
        /// <param name="reason"></param>
        public override void onDeviceDisconnected(Device device, int reason)
        {
            Debug.Log("onDeviceDisconnected  address: " + device.getAddress());
        }

        /// 
        /// <param name="device"></param>
        public override void onDeviceFound(Device device)
        {
            Debug.Log("onDeviceFound  name: " + device.getName());

            gameGforceListener.FoundDevice(device);
        }

        public override void onScanFinished()
        {
            Debug.Log("onScanFinished ");

        }

        /// 
        /// <param name="state"></param>
        public override void onStateChanged(Hub.HubState state)
        {
            Debug.Log("onStateChanged  state: " + state);

        }

        ///
        /// <param name="device"></param>
        /// <param name="state"></param>
        public override void onDeviceStatusChanged(Device device, Status status)
        {
            Debug.Log("onDeviceStatusChanged  status: " + status);
            if (status == Status.ReCenter)
            {
                gameGforceListener.RestOrientationData(device);
            }
        }

        public UIListenerImpl(GameGforceListener gameGforceListener)
        {
            this.gameGforceListener = gameGforceListener;
        }
    }
    #endregion

    private UIListenerImpl uiListenerImpl;
    GForceListener gForceListener=null;

    public Action<Device> onFondDevice;

    public void SetGForceListener(GForceListener _GForceListener)
    {
        if (_GForceListener == null) 
        {
            Debug.LogError("{GameGforceListener}(SetGForceListener) _GForceListener is null");
            return;
        }
        gForceListener = _GForceListener;
    }

    public void FoundDevice(Device value)
    {
        onFondDevice?.Invoke(value);
    }

    public void RestOrientationData(Device _device)
    {

    }

    public void initGForceListener(GForceListener _listener)
    {
        gForceListener = _listener;

        Debug.Log("[GameGforceListener::initGForceListener] gForceListener.RegisterUIListener(uiListenerImpl)");

        if (uiListenerImpl == null)
        {
            uiListenerImpl = new UIListenerImpl(this);
            gForceListener.RegisterUIListener(uiListenerImpl);
            Debug.Log("[GameGforceListener::initGForceListener] gForceListener.RegisterUIListener(uiListenerImpl) over");
        }
    }
}
