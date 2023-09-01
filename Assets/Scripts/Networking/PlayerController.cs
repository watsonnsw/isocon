using System.Collections;
using System.Collections.Generic;
using System.Text;
using Mirror;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerController : NetworkBehaviour
{
    public override void OnStartServer()
    {
        base.OnStartServer();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        Toast.Add("Host disconnected.");
    }

    public static void Disconnect() {
        Toast.Add("Disconnected.");
        UI.System.Q("PlayerList").Clear();
        UI.ToggleDisplay("SelectedTokenPanel", false);
        GameObject[] objs = GameObject.FindGameObjectsWithTag("TokenData");
        for (int i = 0; i < objs.Length; i++) {
            objs[i].GetComponent<TokenData>().Disconnect();
        }
        StartupPanel.Disconnect();
    }
}
