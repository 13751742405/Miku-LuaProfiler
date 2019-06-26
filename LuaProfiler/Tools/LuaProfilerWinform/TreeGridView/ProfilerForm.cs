using AdvancedDataGridView;
using System;
using System.Drawing;
using System.Windows.Forms;
using HookLib;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;
using System.Diagnostics;
using EasyHook;
using System.Runtime.Remoting;
using System.Linq;

namespace MikuLuaProfiler
{
    public partial class ProfilerForm : Form
    {
        public ProfilerForm()
        {
            InitializeComponent();

            SetStyle();
        }

        private void SetStyle()
        {
            attachmentColumn.DefaultCellStyle.NullValue = null;

            // load image strip
            this.imageStrip.ImageSize = new System.Drawing.Size(16, 16);
            this.imageStrip.TransparentColor = System.Drawing.Color.Magenta;
            this.imageStrip.ImageSize = new Size(16, 16);
            this.imageStrip.Images.AddStrip(Properties.Resources.newGroupPostIconStrip);

            tvTaskList.ImageList = imageStrip;

            // attachment header cell
            this.attachmentColumn.HeaderCell = new AttachmentColumnHeader(imageStrip.Images[2]);

            FillFormInfo();
        }

        private void FillFormInfo()
        {
            Font boldFont = new Font(tvTaskList.DefaultCellStyle.Font, FontStyle.Bold);

            TreeGridNode node = tvTaskList.Nodes.Add(null, "����վ��1", "", "");
            node.ImageIndex = 0;
            node.DefaultCellStyle.Font = boldFont;
            node = node.Nodes.Add(null, "����վ��11", DateTime.Now.ToString("yyyyMMdd HH:mm:ss"), "�����");
            node.ImageIndex = 1;
            node = node.Parent.Nodes.Add(null, "����վ��12", DateTime.Now.ToString("yyyyMMdd HH:mm:ss"), "�����");
            node.ImageIndex = 1;
            node = node.Parent.Nodes.Add(null, "����վ��13", DateTime.Now.ToString("yyyyMMdd HH:mm:ss"), "�����");
            node.ImageIndex = 1;
            node = node.Parent.Nodes.Add(null, "����վ��14", DateTime.Now.ToString("yyyyMMdd HH:mm:ss"), "�����");
            node.ImageIndex = 1;
            for (int i = 15, imax = 1000; i < imax; i++)
            {
                node = node.Parent.Nodes.Add(null, "����վ��" + i, DateTime.Now.ToString("yyyyMMdd HH:mm:ss"), "�����");
                node.ImageIndex = 1;
            }
            node = node.Nodes.Add(null, "����վ��15", DateTime.Now.ToString("yyyyMMdd HH:mm:ss"), "�����");
            node.ImageIndex = 1;

            node = tvTaskList.Nodes.Add(null, @"����վ��2", "", "");
            node.ImageIndex = 0;
            node.DefaultCellStyle.Font = boldFont;
            node = node.Nodes.Add(null, "����վ��22", DateTime.Now.ToString("yyyyMMdd HH:mm:ss"), "δ���");
            node.ImageIndex = 1;

            node = tvTaskList.Nodes.Add(null, @"����վ��3", "", "");
            node.ImageIndex = 1;

            node = tvTaskList.Nodes.Add(null, @"����վ��4", "", "");
            node.ImageIndex = 1;
        }

        internal class AttachmentColumnHeader : DataGridViewColumnHeaderCell
        {
            public Image _image;
            public AttachmentColumnHeader(Image img)
                : base()
            {
                this._image = img;
            }
            protected override void Paint(Graphics graphics, Rectangle clipBounds, Rectangle cellBounds, int rowIndex, DataGridViewElementStates dataGridViewElementState, object value, object formattedValue, string errorText, DataGridViewCellStyle cellStyle, DataGridViewAdvancedBorderStyle advancedBorderStyle, DataGridViewPaintParts paintParts)
            {
                base.Paint(graphics, clipBounds, cellBounds, rowIndex, dataGridViewElementState, value, formattedValue, errorText, cellStyle, advancedBorderStyle, paintParts);
                graphics.DrawImage(_image, cellBounds.X + 4, cellBounds.Y + 2);
            }
            protected override object GetValue(int rowIndex)
            {
                return null;
            }
        }

        private void injectButton_Click(object sender, EventArgs e)
        {
            Process[] process = Process.GetProcessesByName(injectTextBox.Text);
            if (process.Length > 0)
            {
                var p = Process.GetProcessById(process.FirstOrDefault().Id);
                if (p == null)
                {
                    MessageBox.Show("ָ���Ľ��̲�����!");
                    return;
                }

                if (IsWin64Emulator(p.Id) != IsWin64Emulator(Process.GetCurrentProcess().Id))
                {
                    var currentPlat = IsWin64Emulator(Process.GetCurrentProcess().Id) ? 64 : 32;
                    var targetPlat = IsWin64Emulator(p.Id) ? 64 : 32;
                    MessageBox.Show(string.Format("��ǰ������{0}λ����Ŀ�������{1}λ�������������ѡ�����±�������ԣ�", currentPlat, targetPlat));
                    return;
                }

                RegGACAssembly();
                InstallHookInternal(p.Id);
            }
            else
            {
                MessageBox.Show("�ý��̲����ڣ�");
            }
        }

        #region inject
        #region filed
        private HookServer serverInterface;
        #endregion

        private bool RegGACAssembly()
        {
            var dllName = "EasyHook.dll";
            var dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dllName);
            if (!RuntimeEnvironment.FromGlobalAccessCache(Assembly.LoadFrom(dllPath)))
            {
                new System.EnterpriseServices.Internal.Publish().GacInstall(dllPath);
                Thread.Sleep(100);
            }

            dllName = "HookLib.dll";
            dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dllName);
            new System.EnterpriseServices.Internal.Publish().GacRemove(dllPath);
            if (!RuntimeEnvironment.FromGlobalAccessCache(Assembly.LoadFrom(dllPath)))
            {
                new System.EnterpriseServices.Internal.Publish().GacInstall(dllPath);
                Thread.Sleep(100);
            }

            return true;
        }

        private bool InstallHookInternal(int processId)
        {
            try
            {
                var parameter = new HookParameter
                {
                    Msg = "�Ѿ��ɹ�ע��Ŀ�����",
                    HostProcessId = RemoteHooking.GetCurrentProcessId()
                };

                serverInterface = new HookServer();
                string channelName = null;
                RemoteHooking.IpcCreateServer<HookServer>(ref channelName, System.Runtime.Remoting.WellKnownObjectMode.Singleton, serverInterface);

                RemoteHooking.Inject(
                    processId,
                    InjectionOptions.Default,
                    typeof(HookParameter).Assembly.Location,
                    typeof(HookParameter).Assembly.Location,
                    channelName,
                    parameter
                );
            }
            catch (Exception ex)
            {
                Debug.Print(ex.ToString());
                return false;
            }

            return true;
        }

        private static bool IsWin64Emulator(int processId)
        {
            var process = Process.GetProcessById(processId);
            if (process == null)
                return false;

            if ((Environment.OSVersion.Version.Major > 5)
                || ((Environment.OSVersion.Version.Major == 5) && (Environment.OSVersion.Version.Minor >= 1)))
            {
                bool retVal;

                return !(IsWow64Process(process.Handle, out retVal) && retVal);
            }

            return false; // not on 64-bit Windows Emulator
        }

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsWow64Process([In] IntPtr process, [Out] out bool wow64Process);

        private void button2_Click(object sender, EventArgs e)
        {
            MessageBox.Show(serverInterface.IsInstalled().ToString());
        }
        #endregion
    }
}