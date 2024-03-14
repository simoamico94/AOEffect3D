using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SendCommandButton : MonoBehaviour
{
    public string commandToSend;
    private Button button;

    void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(() => AOSManager.main.RunCommand(commandToSend));
    }
}
