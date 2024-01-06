using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using gf;
using GForce;
using GameCtrler;
using UnityEngine.Android;
using System.Linq;
using UnityEngine.UI;

public class Main : MonoBehaviour
{
    private GForceListener gForceListener;

    IGameController[] IGForceGameControllerGroup = new IGameController[1];
    GForceGameController[] ctrlerGroup = new GForceGameController[1];
    List<GForceGameController> showGfroceGameCtrlerList = new List<GForceGameController>();
    GameGforceListener gameGforceListener = new GameGforceListener();

    List<Device> fondDevice = new List<Device>();

    public Text DeviceText;
    GForceGameController connectDevice;

    void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaClass unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        var unityActivity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");

        unityActivity.Call("runOnUiThread", new AndroidJavaRunnable(() => {
            GForceHub.instance.Prepare();
        }));

        string[] strs = new string[] {
            "android.permission.BLUETOOTH",
            "android.permission.BLUETOOTH_ADMIN",
            //"android.permission.ACCESS_COARSE_LOCATION",
            "android.permission.ACCESS_FINE_LOCATION"
        };

        strs.ToList().ForEach(s =>
        {
            Permission.RequestUserPermission(s);
            Debug.Log("add RequestUserPermission: " + s);
        });
#else
        GForceHub.instance.Prepare();
#endif

        gForceListener = new GForceListener();
        Hub.Instance.registerListener(gForceListener);

        gForceListener = new GForceListener();
        Hub.Instance.registerListener(gForceListener);
        gameGforceListener.initGForceListener(gForceListener);
        gameGforceListener.onFondDevice += OnFondDevice;
        IGForceGameControllerGroup[0] = GameControllerManager.CreateGameController("GForceGameController");
        gForceListener.RegisterGForceDevice(IGForceGameControllerGroup[0] as GForceDevice);
        ctrlerGroup[0] = (GForceGameController)IGForceGameControllerGroup[0];
        showGfroceGameCtrlerList.Add(ctrlerGroup[0]);

        Hub.Instance.startScan();
    }

    private void OnFondDevice(Device _device)
    {
        for (int i = 0; i <= fondDevice.Count; i++)
        {
            if (i == fondDevice.Count)
            {
                fondDevice.Add(_device);
                return;
            }
            else
            {
                if (fondDevice[i].getAddress() == _device.getAddress())
                {
                    fondDevice[i] = _device;
                }
            }
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            Hub.Instance.startScan();
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            if (fondDevice.Count > 0
                && fondDevice[0] != null)
            {
                Hub.Instance.stopScan();
                fondDevice[0].connect();
                DeviceText.text = "连接设备:" + fondDevice[0].getName();
                connectDevice = ctrlerGroup[0];
            }
        }

        if (ctrlerGroup[0].GetEmgValue() != null)
        {
            DeviceText.text = "当前四元数数据为" + ctrlerGroup[0].GetQuaternion()
                + "当前肌电数据为" + ctrlerGroup[0].GetEmgValue()[0];
        }

        ctrlerGroup[0].Tick();
    }

    private void OnDestroy()
    {
        GForceHub.instance.Terminate();
    }
}
