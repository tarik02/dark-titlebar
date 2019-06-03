using System;
using System.Threading;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using RegistryUtils;

namespace dark_titlebar
{
	static class DarkTitlebar
	{
		[DllImport("kernel32.dll")]
		static extern IntPtr GetConsoleWindow();

		[DllImport("user32.dll")]
		static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

		const int SW_HIDE = 0;
		const int SW_SHOW = 5;


		const string AppName = "dark-titlebar";
		const string Key = @"SOFTWARE\Microsoft\Windows\DWM";

		private static bool changing = false;

		static void Main(string[] args)
		{
			if (args.Length != 0)
			{
				if (args.Length == 1)
				{
					switch (args[0])
					{
					case "set-startup":
						SetStartup(true);
						Console.WriteLine("Startup entry added.");
						return;
					case "unset-startup":
						SetStartup(false);
						Console.WriteLine("Startup entry removed.");
						return;
					}
				}

				Console.WriteLine($"Usage:");
				Console.WriteLine($"    {AppName} [action]");
				Console.WriteLine($"    If action is omitted, then app is started in daemon mode.");
				Console.WriteLine($"");
				Console.WriteLine($"Actions:");
				Console.WriteLine($"    set-startup      Make app auto run at Windows startup.");
				Console.WriteLine($"    unset-startup    Make app not auto run at Windows startup.");

				return;
			}

			ShowWindow(GetConsoleWindow(), SW_HIDE);

			UpdateRegistry();

			var monitor = new RegistryMonitor(
				RegistryHive.CurrentUser,
				Key
			);
			monitor.RegChanged += new EventHandler(OnRegChanged);
			monitor.Start();

			try
			{
				var waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, "CF2D4313-33DE-489D-9721-6AFF69841DEA", out bool createdNew);
				var signaled = false;

				if (!createdNew)
				{
					waitHandle.Set();

					return;
				}

				var timer = new Timer(OnTimerElapsed, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));

				do
				{
					signaled = waitHandle.WaitOne(TimeSpan.FromSeconds(5));
				} while (!signaled);
			}
			finally
			{
				monitor.Stop();
			}
		}

		private static void OnRegChanged(object sender, EventArgs e)
		{
			if (changing)
			{
				return;
			}

			Console.WriteLine("Change");

			changing = true;

			try
			{
				UpdateRegistry();
			}
			finally
			{
				changing = false;
			}
		}

		private static void OnTimerElapsed(object state)
		{
		}

		private static void UpdateRegistry()
		{
			RegistryKey key = Registry.CurrentUser.OpenSubKey(Key, true);
			if (key != null)
			{
				try
				{
					key.SetValue("Composition", unchecked((int) 0x00000001), RegistryValueKind.DWord);
					key.SetValue("ColorizationGlassAttribute", unchecked((int) 0x00000000), RegistryValueKind.DWord);
					key.SetValue("EnableAeroPeek", unchecked((int) 0x00000001), RegistryValueKind.DWord);
					key.SetValue("AccentColor", unchecked((int) 0x00010101), RegistryValueKind.DWord);
					key.SetValue("ColorPrevalence", unchecked((int) 0x00000001), RegistryValueKind.DWord);
					key.SetValue("AccentColorInactive", unchecked((int) 0x00010101), RegistryValueKind.DWord);
					key.SetValue("ColorizationColor", unchecked((int) 0xc4498205u), RegistryValueKind.DWord);
					key.SetValue("ColorizationColorBalance", unchecked((int) 0x00000059), RegistryValueKind.DWord);
					key.SetValue("ColorizationAfterglow", unchecked((int) 0xc4498205), RegistryValueKind.DWord);
					key.SetValue("ColorizationAfterglowBalance", unchecked((int) 0x0000000a), RegistryValueKind.DWord);
					key.SetValue("ColorizationBlurBalance", unchecked((int) 0x00000001), RegistryValueKind.DWord);
					key.SetValue("EnableWindowColorization", unchecked((int) 0x00000001), RegistryValueKind.DWord);
				}
				finally
				{
					key.Close();
				}
			}
		}

		private static void SetStartup(bool value)
		{
			RegistryKey key = Registry.CurrentUser.OpenSubKey(
				@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true
			);

			if (value)
			{
				var executablePath = System.Reflection.Assembly.GetEntryAssembly().Location;
				key.SetValue(AppName, executablePath);
			}
			else
			{
				key.DeleteValue(AppName, false);
			}
		}
	}
}
