using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractiveCMDShell
{
	System.Diagnostics.ProcessStartInfo startInfo;
	System.Diagnostics.Process process;
	System.Threading.Thread thread;
	System.IO.StreamReader output;

	string lineBuffer = "";
	List<string> lines = new List<string>();
	bool m_Running = false;


	public InteractiveCMDShell() {
		startInfo = new System.Diagnostics.ProcessStartInfo("Cmd.exe");
		//startInfo.WorkingDirectory = "C:\\Windows\\System32\\";
		startInfo.WorkingDirectory = Application.dataPath + "/StreamingAssets";
		Debug.Log("Opening cmd in " +  startInfo.WorkingDirectory);
		startInfo.UseShellExecute = false;
		startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
		startInfo.CreateNoWindow = true;
		startInfo.ErrorDialog = false;
		startInfo.RedirectStandardInput = true;
		startInfo.RedirectStandardOutput = true;
		process = new System.Diagnostics.Process();
		process.StartInfo = startInfo;
		process.Start();
		output = process.StandardOutput;
		thread = new System.Threading.Thread(Thread);
		thread.Start();
	}
	~InteractiveCMDShell() {
		try {
			Stop();
		}
		catch { }
	}

	public void RunCommand(string aInput) {
		if (m_Running) {
			process.StandardInput.WriteLine(aInput);
			process.StandardInput.Flush();
		}
	}
	public void Stop() {
		if (process != null) {
			process.Kill();
			thread.Join(200);
			thread.Abort();
			process = null;
			thread = null;
			m_Running = false;
		}
	}
	public string GetCurrentLine() {
		if (!m_Running)
			return "";
		return lineBuffer;
	}
	public void GetRecentLines(List<string> aLines) {
		if (!m_Running || aLines == null)
			return;
		if (lines.Count == 0)
			return;
		lock (lines) {
			if (lines.Count > 0) {
				aLines.AddRange(lines);
				lines.Clear();
			}
		}
	}

	void Thread() {
		m_Running = true;
		try {
			while (true) {

				int c = output.Read();
				if (c <= 0)
					break;
				else if (c == '\n') {
					lock (lines) {
						lines.Add(lineBuffer);
						lineBuffer = "";
					}
				}
				else if (c != '\r')
					lineBuffer += (char)c;
			}
			Debug.Log("CMDProcess Thread finished");
		}
		catch (Exception e) {
			Debug.LogException(e);
		}
		m_Running = false;
	}
}