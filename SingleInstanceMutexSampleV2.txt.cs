/*
*	Singe Instance Mutex Sample (version 2)
*
*	Here is code that demonstrates one way of making a single instance application.
*
*	I've crammed all this code into a single file to make it easy for you read
*	to get an overview.
*
*	In a real application, you'd want to split these classes into multiple files.
*
*	You can use all this code directly in one of your applications, or put some
*	of the code in a class library so that you can easily reuse it in all your applications.
*	The code is designed to work either way.
*/

using System;
using System.Windows.Forms;
using System.Threading;
using System.Runtime.InteropServices;
using System.Reflection;

/*******************************************************************/

/*
*	Program  Main()
*
*	The only special thing about Main
*	is that it calls SingleInstance.Start() at the top,
*	and SingleInstance.Stop() at the bottom.
*
*/

// using System.Windows.Forms;

static class Program
{
	[STAThread]
	static void Main()
	{
		if (!SingleInstance.Start()) {
			SingleInstance.ShowFirstInstance();
			return;
		}
		
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(false);

		try {
			MainForm mainForm = new MainForm();
			Application.Run(mainForm);
		} catch (Exception e) {
			MessageBox.Show(e.Message);
		}
		
		SingleInstance.Stop();
	}
}

/*
*	MainForm()
*
*	The most important thing you must do in your main form
*	is override WndProc, and check for the message WM_SHOWFIRSTINSTANCE.
*
*	If that message is found, then call whatever code you want
*	to restore your application from the taskbar or the notification area (tray).
*	This sample version contains code for using a notification icon.
*
*/

public partial class MainForm : Form
{
        bool minimizedToTray;
        NotifyIcon notifyIcon;

	public MainForm()
	{
		InitializeComponent();
		this.Text = Program.ApplicationName;
	}
	protected override void WndProc(ref Message message)
	{
		if (message.Msg == SingleInstance.WM_SHOWFIRSTINSTANCE) {
			ShowWindow();
		}
		base.WndProc(ref message);
	}
        private void btnMinToTray_Click(object sender, EventArgs e)
        {
		// Tie this function to a button on your main form that will minimize your
		// application to the notification icon area (aka system tray).
		MinimizeToTray();
        }
        void MinimizeToTray()
        {
		notifyIcon = new NotifyIcon();
		//notifyIcon.Click += new EventHandler(NotifyIconClick);
		notifyIcon.DoubleClick += new EventHandler(NotifyIconClick);
		notifyIcon.Icon = this.Icon;
		notifyIcon.Text = ProgramInfo.AssemblyTitle;
		notifyIcon.Visible = true;
		this.WindowState = FormWindowState.Minimized;
		this.Hide();
		minimizedToTray = true;
        }
        public void ShowWindow()
        {
		if (minimizedToTray) {
			notifyIcon.Visible = false;
			this.Show();
			this.WindowState = FormWindowState.Normal;
			minimizedToTray = false;
		} else {
			WinApi.ShowToFront(this.Handle);
		}
        }
        void NotifyIconClick(Object sender, System.EventArgs e)
        {
		ShowWindow();
        }
}

/* All of the code below can optionally be put in a class library and reused with all your applications. */

/*
*	SingeInstance
*
*	This is where the magic happens.
*
*	Start() tries to create a mutex.
*	If it detects that another instance is already using the mutex, then it returns FALSE.
*	Otherwise it returns TRUE.
*	(Notice that a GUID is used for the mutex name, which is a little better than using the application name.)
*
*	If another instance is detected, then you can use ShowFirstInstance() to show it
*	(which will work as long as you override WndProc as shown above).
*
*	ShowFirstInstance() broadcasts a message to all windows.
*	The message is WM_SHOWFIRSTINSTANCE.
*	(Notice that a GUID is used for WM_SHOWFIRSTINSTANCE.
*	That allows you to reuse this code in multiple applications without getting
*	strange results when you run them all at the same time.)
*
*/

// using System.Threading;

static public class SingleInstance
{
	public static readonly int WM_SHOWFIRSTINSTANCE =
		WinApi.RegisterWindowMessage("WM_SHOWFIRSTINSTANCE|{0}", ProgramInfo.AssemblyGuid);
	static Mutex mutex;
	static public bool Start()
	{
		bool onlyInstance = false;
		string mutexName = String.Format("Local\\{0}", ProgramInfo.AssemblyGuid);

		// if you want your app to be limited to a single instance
		// across ALL SESSIONS (multiple users & terminal services), then use the following line instead:
		// string mutexName = String.Format("Global\\{0}", ProgramInfo.AssemblyGuid);
		
		mutex = new Mutex(true, mutexName, out onlyInstance);
		return onlyInstance;
	}
	static public void ShowFirstInstance()
	{
		WinApi.PostMessage(
			(IntPtr)WinApi.HWND_BROADCAST,
			WM_SHOWFIRSTINSTANCE,
			IntPtr.Zero,
			IntPtr.Zero);
	}
	static public void Stop()
	{
		mutex.ReleaseMutex();
	}
}

/*
*	WinApi
*
*	This class is just a wrapper for your various WinApi functions.
*
*	In this sample only the bare essentials are included.
*	In my own WinApi class, I have all the WinApi functions that any
*	of my applications would ever need.
*
*/

// using System.Runtime.InteropServices;

static public class WinApi
{
	[DllImport("user32")]
	public static extern int RegisterWindowMessage(string message);
	
	public static int RegisterWindowMessage(string format, params object[] args)
	{
		string message = String.Format(format, args);
		return RegisterWindowMessage(message);
	}

	public const int HWND_BROADCAST = 0xffff;
        public const int SW_SHOWNORMAL = 1;
	
	[DllImport("user32")]
        public static extern bool PostMessage(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam);

	[DllImportAttribute ("user32.dll")]
	public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
	
	[DllImportAttribute ("user32.dll")]
	public static extern bool SetForegroundWindow(IntPtr hWnd);
	
	public static void ShowToFront(IntPtr window)
	{
		ShowWindow(window, SW_SHOWNORMAL);
		SetForegroundWindow(window);
	}
}

/*
*	ProgramInfo
*
*	This class is just for getting information about the application.
*	Each assembly has a GUID, and that GUID is useful to us in this application,
*	so the most important thing in this class is the AssemblyGuid property.
*
*	GetEntryAssembly() is used instead of GetExecutingAssembly(), so that you
*	can put this code into a class library and still get the results you expect.
*	(Otherwise it would return info on the DLL assembly instead of your application.)
*/

// using System.Reflection;

static public class ProgramInfo
{
        static public string AssemblyGuid
        {
		get
		{
			object[] attributes = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(System.Runtime.InteropServices.GuidAttribute), false);
			if (attributes.Length == 0) {
				return String.Empty;
			}
			return ((System.Runtime.InteropServices.GuidAttribute)attributes[0]).Value;
		}
        }
        static public string AssemblyTitle
	{
		get
		{
			object[] attributes = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
			if (attributes.Length > 0) {
				AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
				if (titleAttribute.Title != "") {
					return titleAttribute.Title;
			}
		}
			return System.IO.Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().CodeBase);
		}
	}
}
