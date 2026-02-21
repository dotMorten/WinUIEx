using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using Windows.Win32;
using Windows.Win32.System.Threading;

namespace WinUIEx;

/// <summary>
/// Based on: <br/>
/// <see href="https://devblogs.microsoft.com/performance-diagnostics/reduce-process-interference-with-task-manager-efficiency-mode"/> <br/>
/// <see href="https://devblogs.microsoft.com/performance-diagnostics/introducing-ecoqos/"/> <br/>
/// </summary>
public static class EfficiencyModeUtilities
{
	private static bool EnsureNonZero(this Windows.Win32.Foundation.BOOL value)
	{
		return value != 0;
	}

	/// <summary>
	/// Based on <see href="https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-setprocessinformation"/>
	/// </summary>
	[SupportedOSPlatform("windows10.0.16299.0")]
	public static unsafe void SetProcessQualityOfServiceLevel(QualityOfServiceLevel level)
	{
		PROCESS_POWER_THROTTLING_STATE powerThrottling = new()
		{
			Version = PInvoke.PROCESS_POWER_THROTTLING_CURRENT_VERSION
		};

		switch (level)
		{
			case QualityOfServiceLevel.Default:
				powerThrottling.ControlMask = 0;
				powerThrottling.StateMask = 0;
				break;

			case QualityOfServiceLevel.Eco when Environment.OSVersion.Version >= new Version(11, 0):
			case QualityOfServiceLevel.Low:
				powerThrottling.ControlMask = PInvoke.PROCESS_POWER_THROTTLING_EXECUTION_SPEED;
				powerThrottling.StateMask = PInvoke.PROCESS_POWER_THROTTLING_EXECUTION_SPEED;
				break;

			case QualityOfServiceLevel.High:
				powerThrottling.ControlMask = PInvoke.PROCESS_POWER_THROTTLING_EXECUTION_SPEED;
				powerThrottling.StateMask = 0;
				break;

			default:
				throw new NotImplementedException();
		}

		_ = PInvoke.SetProcessInformation(
			hProcess: PInvoke.GetCurrentProcess(),
			ProcessInformationClass: PROCESS_INFORMATION_CLASS.ProcessPowerThrottling,
			ProcessInformation: &powerThrottling,
			ProcessInformationSize: (uint)sizeof(PROCESS_POWER_THROTTLING_STATE)).EnsureNonZero();
	}

	/// <summary>
	/// Based on <see href="https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-setpriorityclass"/>
	/// </summary>
	[SupportedOSPlatform("windows5.1.2600")]
	public static unsafe void SetProcessPriorityClass(ProcessPriorityClass priorityClass)
	{
		PROCESS_CREATION_FLAGS flags = priorityClass switch
		{
			ProcessPriorityClass.Idle => PROCESS_CREATION_FLAGS.IDLE_PRIORITY_CLASS,
			ProcessPriorityClass.BelowNormal => PROCESS_CREATION_FLAGS.BELOW_NORMAL_PRIORITY_CLASS,
			ProcessPriorityClass.Normal => PROCESS_CREATION_FLAGS.NORMAL_PRIORITY_CLASS,
			ProcessPriorityClass.AboveNormal => PROCESS_CREATION_FLAGS.ABOVE_NORMAL_PRIORITY_CLASS,
			ProcessPriorityClass.High => PROCESS_CREATION_FLAGS.HIGH_PRIORITY_CLASS,
			ProcessPriorityClass.RealTime => PROCESS_CREATION_FLAGS.REALTIME_PRIORITY_CLASS,
			_ => throw new NotImplementedException(),
		};

		_ = PInvoke.SetPriorityClass(
			hProcess: PInvoke.GetCurrentProcess(),
			dwPriorityClass: flags).EnsureNonZero();
	}

	/// <summary>
	/// Enables/disables efficient mode for process <br/>
	/// Based on: <see href="https://devblogs.microsoft.com/performance-diagnostics/reduce-process-interference-with-task-manager-efficiency-mode/"/> 
	/// </summary>
	/// <param name="value"></param>
	[SupportedOSPlatform("windows10.0.16299.0")]
	public static void SetEfficiencyMode(bool value)
	{
		QualityOfServiceLevel ecoLevel = Environment.OSVersion.Version >= new Version(11, 0) ? QualityOfServiceLevel.Eco : QualityOfServiceLevel.Low;

		SetProcessQualityOfServiceLevel(value
			? ecoLevel
			: QualityOfServiceLevel.Default);
		SetProcessPriorityClass(value
			? ProcessPriorityClass.Idle
			: ProcessPriorityClass.Normal);
	}
}

/// <summary>
/// Specifies the Quality of Service (QoS) levels for process power management.
/// These levels guide the Windows scheduler in balancing performance and energy efficiency.
/// </summary>
public enum QualityOfServiceLevel
{
	/// <summary>
	/// Let the operating system manage power throttling automatically. 
	/// No specific execution speed constraints are applied by the application.
	/// </summary>
	Default,

	/// <summary>
	/// EcoQoS level. The most efficient power level available (Windows 11 and later).
	/// Prioritizes efficiency cores (E-cores) and significantly reduces CPU clock speed 
	/// to minimize thermal footprint and power consumption.
	/// </summary>
	Eco,

	/// <summary>
	/// Low-priority power level. Similar to standard power throttling in earlier Windows 10 versions.
	/// Intended for background tasks where latency and high execution speed are not critical.
	/// </summary>
	Low,

	/// <summary>
	/// High-performance level. Explicitly disables execution speed throttling mechanisms 
	/// to ensure the process has maximum access to CPU resources.
	/// </summary>
	High
}