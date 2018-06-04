using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using AsrsModel;
using ModuleCrossPnP;
using AsrsInterface;
namespace AsrsControl
{
    public partial class CtlTaskView : BaseChildView,ICtlTaskView
    {
        private CtlTaskPresenter taskPresenter = null;
       
        public CtlTaskView(string captionText):base(captionText)
        {
            InitializeComponent();
            this.Text = captionText;
            this.taskPresenter = new CtlTaskPresenter(this);
        }
        public void SetAsrsResManage(IAsrsManageToCtl asrsResManage)
        {
            this.taskPresenter.SetAsrsResManage(asrsResManage);
        }
        #region ICtlTaskView接口实现
        public void RefreshTaskDisp(DataTable dt)
        {
            BindingSource bs = new BindingSource();
            bs.DataSource = dt;
            bindingNavigator1.BindingSource = bs;
            dataGridView1.DataSource = bs;
        }
     
        #endregion
        #region UI事件

        private void buttonRefresh_Click(object sender, EventArgs e)
        {
            taskPresenter.QueryTask();
        }

        private void CtlTaskView_Load(object sender, EventArgs e)
        {
            this.comboBox1.Items.Clear();
            this.comboBox1.Items.AddRange(new string[] {"所有","A1库","B1库","C1库","C2库","托盘绑定1","托盘绑定2","分拣1","分拣2","分拣3" });

            this.comboBox2.Items.AddRange(new string[] { "所有", SysCfg.EnumAsrsTaskType.产品入库.ToString(), 
                SysCfg.EnumAsrsTaskType.产品出库.ToString(), 
                SysCfg.EnumAsrsTaskType.空框入库.ToString(), 
                SysCfg.EnumAsrsTaskType.空框出库.ToString(), 
                SysCfg.EnumAsrsTaskType.移库.ToString(),
                SysCfg.EnumAsrsTaskType.托盘装载.ToString(),
                SysCfg.EnumAsrsTaskType.OCV测试分拣.ToString()});

            this.comboBox3.Items.AddRange(new string[] { "所有", SysCfg.EnumTaskStatus.待执行.ToString(), SysCfg.EnumTaskStatus.执行中.ToString(), SysCfg.EnumTaskStatus.已完成.ToString(), SysCfg.EnumTaskStatus.超时.ToString(), SysCfg.EnumTaskStatus.任务撤销.ToString() });

            this.comboBox1.SelectedIndex = 0;
            this.comboBox2.SelectedIndex = 0;
            this.comboBox3.SelectedIndex = 0;
            taskPresenter.TaskFilter.NodeName = this.comboBox1.Text;
            taskPresenter.TaskFilter.TaskType = this.comboBox2.Text;
            taskPresenter.TaskFilter.TaskStatus = this.comboBox3.Text;
            this.dateTimePicker1.DataBindings.Add("Value", taskPresenter.TaskFilter, "StartDate");
            this.dateTimePicker2.DataBindings.Add("Value", taskPresenter.TaskFilter, "EndDate");
            this.comboBox1.DataBindings.Add("Text", taskPresenter.TaskFilter, "NodeName");
            this.comboBox2.DataBindings.Add("Text", taskPresenter.TaskFilter, "TaskType");
            this.comboBox3.DataBindings.Add("Text", taskPresenter.TaskFilter, "TaskStatus");
          
        }
        private void btnDelTask_Click(object sender, EventArgs e)
        {
            OnDelTask();
        }
        private void OnDelTask()
        {
            if(parentPNP.RoleID>2)
            {
                MessageBox.Show("没有足够的权限，请切换到管理员用户");
                return;
            }
            List<string> taskIds = new List<string>();
            foreach(DataGridViewRow dr in dataGridView1.SelectedRows)
            {
                taskIds.Add(dr.Cells["任务ID"].Value.ToString());
            }
            if(taskIds.Count()>0)
            {
                taskPresenter.DelTask(taskIds);
            }
        }
        #endregion   
    }
}
