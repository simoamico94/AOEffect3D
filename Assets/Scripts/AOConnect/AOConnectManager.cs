using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;

public class AOConnectManager : MonoBehaviour
{
	[DllImport("__Internal")]
	private static extern void evaluateUnity(string pid, string data);

	public TMP_InputField inputFieldPid;
	public TMP_InputField inputFieldData;

	public void Evaluate()
	{
		CallEvaluateWithParams(inputFieldPid.text, inputFieldData.text);
	}

	// Call this method with PID and data as arguments
	public void CallEvaluateWithParams(string pid, string data)
	{
		if (Application.platform == RuntimePlatform.WebGLPlayer)
		{
			evaluateUnity(pid, data);
		}
	}
}
