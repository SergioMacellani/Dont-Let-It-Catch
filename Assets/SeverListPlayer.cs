using System;
using System.Collections;
using System.Collections.Generic;
using NetBuff;
using NetBuff.Components;
using NetBuff.Misc;
using TMPro;
using UnityEngine;

public class SeverListPlayer : NetworkBehaviour
{
    public TextMeshProUGUI nickText;
    public StringNetworkValue nick = new("1000ton");
    public BoolNetworkValue isBlue = new(false);

    private void OnEnable()
    {
        WithValues(nick, isBlue);
        nick.OnValueChanged += OnChangeNick;
        isBlue.OnValueChanged += OnChangeTeam;
    }

    private void OnChangeTeam(bool oldvalue, bool newvalue)
    {
        var parent = CTFManager.instance.GetServerListSide(isBlue.Value);
        transform.parent = parent;
        transform.eulerAngles = Vector3.zero;
    }

    private void OnChangeNick(string oldvalue, string newvalue)
    {
        TryGetComponent(out nickText);
        nickText.text = newvalue;
    }

    public override void OnSpawned(bool isRetroactive)
    {
        TryGetComponent(out nickText);
        var parent = CTFManager.instance.GetServerListSide(isBlue.Value);
        transform.parent = parent;
        transform.eulerAngles = Vector3.zero;
        
        if (HasAuthority)
        {
            nick.Value = NetworkManager.Instance.IsServerRunning
                ? PlayerPrefs.GetString("Nick", $"Host ({OwnerId})") + " (Host)"
                : PlayerPrefs.GetString("Nick", $"Player ({OwnerId})");
            isBlue.Value = CTFManager.instance.GetServerListSide(OwnerId);
        }
    }
}
