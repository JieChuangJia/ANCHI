using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using ModuleCrossPnP;
using LogInterface;
//using CtlDBAccess.BLL;
using MesDBAccess.Model;
using MesDBAccess.BLL;
namespace ConfigManage
{
    public partial class SysSettingView : BaseChildView
    {
        private BatchBll batchBll = new BatchBll();
        private  ProcessStepBll processStepBll = new ProcessStepBll();
        private List<FlowCtlBaseModel.CtlNodeBaseModel> nodeCfgList = new List<FlowCtlBaseModel.CtlNodeBaseModel>();
        #region  公有接口
       // public string CaptionText { get { return captionText; } set { captionText = value; this.Text = captionText; } }
        public SysSettingView(string captionText):base(captionText)
        {
            InitializeComponent();
            //sysCfg = new SysCfgsettingModel();

           
            this.Text = captionText;
            //this.captionText = captionText;
           
           
            

        }
        public void SetCfgNodes(List<FlowCtlBaseModel.CtlNodeBaseModel> cfgNodes)
        {
            nodeCfgList = cfgNodes;
        }
        public override void ChangeRoleID(int roleID)
        {
            
            if(roleID !=1)
            {
                if(this.tabControl1.TabPages.Contains(tabPage3))
                {
                    this.tabControl1.TabPages[tabPage3.Name].Parent = null;
                }
               
            }
            else
            {
                if (!this.tabControl1.TabPages.Contains(tabPage3))
                {
                    this.tabControl1.TabPages.Add(this.tabPage3);
                }
                this.tabControl1.TabPages[tabPage3.Name].Parent = this.tabControl1;
            }
        }
        #endregion

        private void buttonCfgApply_Click(object sender, EventArgs e)
        {
            string reStr = "";
     
            SysCfg.SysCfgModel.AsrsStoreTime = float.Parse(this.textBoxA1BurninTime.Text);
            foreach (FlowCtlBaseModel.CtlNodeBaseModel node in nodeCfgList)
            {
                if (node.NodeID == "1001")
                {
                    node.NodeEnabled =this.checkBoxHouseA.Checked ;
                }
                else if (node.NodeID == "1002")
                {
                    node.NodeEnabled = this.checkBoxHouseB.Checked;
                }
                else if (node.NodeID == "1003")
                {
                    node.NodeEnabled = this.checkBoxHouseC1.Checked;
                }
                else if (node.NodeID == "1004")
                {
                     node.NodeEnabled=this.checkBoxHouseC2.Checked;
                }
                else
                {
                    break;
                }
                node.SaveCfg();
            }
            if (!SysCfg.SysCfgModel.SaveCfg(ref reStr))
            {
                MessageBox.Show(reStr);
                return;
            }
            OnModifyProcessParams();
            MessageBox.Show("设置已保存！");
            
        }

        private void buttonCancelSet_Click(object sender, EventArgs e)
        {
            
        }

        private void SysSettingView_Load(object sender, EventArgs e)
        {
            if(this.tabControl1.TabPages.Contains(tabPage2))
            {
                tabPage2.Parent = null;
            }
            //this.checkBoxHouseA.Checked = SysCfg.SysCfgModel.HouseEnabledA;
            //this.checkBoxHouseB.Checked = SysCfg.SysCfgModel.HouseEnabledB;
            this.textBoxA1BurninTime.Text = SysCfg.SysCfgModel.AsrsStoreTime.ToString();
        //    this.checkBoxUnbind.Checked = SysCfg.SysCfgModel.UnbindMode;
            foreach (FlowCtlBaseModel.CtlNodeBaseModel node in nodeCfgList)
            {
                if(node.NodeID == "1001")
                {
                    this.checkBoxHouseA.Checked = node.NodeEnabled;
                }
                else if(node.NodeID == "1002")
                {
                    this.checkBoxHouseB.Checked = node.NodeEnabled;
                }
                else if (node.NodeID == "1003")
                {
                    this.checkBoxHouseC1.Checked = node.NodeEnabled;
                }
                else if (node.NodeID == "1004")
                {
                    this.checkBoxHouseC2.Checked = node.NodeEnabled;
                }
                else
                {
                    break;
                }
            }
            OnDispProcessParams();

        }

        private void btnRefresBatchCfg_Click(object sender, EventArgs e)
        {
            OnRefreshBatchDisp();
        }

        private void buttonAddBatch_Click(object sender, EventArgs e)
        {
            OnAddBatch();
        }

        private void buttonCancelBatch_Click(object sender, EventArgs e)
        {
            OnCancelAddbatch();
        }

        private void btnDelBatchCfg_Click(object sender, EventArgs e)
        {
            OnDelSelBatch();
        }
        private void OnRefreshBatchDisp()
        {
            DataSet ds = batchBll.GetAllList();
            DataTable dt = ds.Tables[0];
            
            this.dataGridView2.DataSource = dt;
            this.dataGridView2.Columns["batchName"].HeaderText = "产品批次";
            this.dataGridView2.Columns["createTime"].HeaderText = "创建时间";
            this.dataGridView2.Columns["createTime"].DefaultCellStyle.Format = "yyyy-MM-dd HH:mm:ss";
            
            this.dataGridView2.Columns["remark"].HeaderText = "备注";
            this.dataGridView2.Columns["createTime"].Width = 200;
            this.dataGridView2.Columns["remark"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            this.dataGridView2.Columns["remark"].SortMode = DataGridViewColumnSortMode.NotSortable;
        }
        private void OnAddBatch()
        {
            string batchName = this.textBoxBatch.Text;
            if(batchBll.Exists(batchName))
            {
                MessageBox.Show("已经存在同名的批次号，请重新输入！");
                this.textBoxBatch.Text = "";
                return;
            }
            BatchModel batch = new BatchModel();
            batch.batchName = batchName;
            batch.createTime = System.DateTime.Parse(System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            batchBll.Add(batch);
            OnRefreshBatchDisp();
        }
        private void OnCancelAddbatch()
        {
            this.textBoxBatch.Text = "";
            this.richTextBoxMark.Text = "";
        }
        private void OnDelSelBatch()
        {
            if (this.dataGridView2.SelectedRows.Count < 1)
            {
                MessageBox.Show("未选中记录 ");
                return;
            }
            List<string> rds = new List<string>();
            foreach (DataGridViewRow row in this.dataGridView2.SelectedRows)
            {
                rds.Add(row.Cells["batchName"].Value.ToString());
            }
            foreach(string str in rds)
            {
                batchBll.Delete(str);
            }
            OnRefreshBatchDisp();
        }

        private void OnModifyBatch()
        {
            if (this.dataGridView2.SelectedRows.Count < 1)
            {
                MessageBox.Show("未选中记录 ");
                return;
            }
            foreach (DataGridViewRow row in this.dataGridView2.SelectedRows)
            {
                string batchName = row.Cells["batchName"].Value.ToString();
                BatchModel batch = batchBll.GetModel(batchName); // new BatchModel();
                if(batch == null)
                {
                    continue;
                }
                batch.remark = row.Cells["remark"].Value.ToString();
                batchBll.Update(batch);
            }
            
        }
        private void btnModifyBatch_Click(object sender, EventArgs e)
        {
            OnModifyBatch();
            MessageBox.Show("修改完成");
        }

        private void dataGridView2_CellClick(object sender, DataGridViewCellEventArgs e)
        {
           
        }

        private void buttonImportCfg_Click(object sender, EventArgs e)
        {
            OnImportCfg();
        }
        private void OnImportCfg()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "xml files|*.xml|All files|*.*";
            dlg.Multiselect = false;
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string filePath = System.IO.Path.GetFullPath(dlg.FileName);
                if (!System.IO.File.Exists(filePath))
                {
                    Console.WriteLine("文件不存在：" + filePath);
                    return;
                }
                FileStream fS = new FileStream(filePath, FileMode.Open);
                StreamReader reader = new StreamReader(fS);
                string fileContent = reader.ReadToEnd();
                this.richTextBox1.Text = fileContent;
                fS.Close();
            }
        }
        private void buttonDispCfg_Click(object sender, EventArgs e)
        {
            OnDispCfg();
        }
        private void OnDispCfg()
        {
            this.richTextBox1.Text = "";
            
            CtlDBAccess.BLL.SysCfgBll sysCfgBll = new CtlDBAccess.BLL.SysCfgBll();
            CtlDBAccess.Model.SysCfgDBModel cfgModel = sysCfgBll.GetModel(SysCfg.SysCfgModel.SysCfgFileName);
            if(cfgModel != null)
            {
                this.richTextBox1.Text = cfgModel.cfgFile;
            }
        }
        private void buttonSaveCfg_Click(object sender, EventArgs e)
        {
            OnSaveCfg();
        }
        private void OnSaveCfg()
        {
            CtlDBAccess.BLL.SysCfgBll sysCfgBll = new CtlDBAccess.BLL.SysCfgBll();
            
            if(!sysCfgBll.Exists(SysCfg.SysCfgModel.SysCfgFileName))
            {
                CtlDBAccess.Model.SysCfgDBModel cfgModel = new CtlDBAccess.Model.SysCfgDBModel();
                cfgModel.sysCfgName = SysCfg.SysCfgModel.SysCfgFileName;
                cfgModel.cfgFile = this.richTextBox1.Text;
                cfgModel.modifyTime = System.DateTime.Now;
                sysCfgBll.Add(cfgModel);
            }
            else
            {
                CtlDBAccess.Model.SysCfgDBModel cfgModel = sysCfgBll.GetModel(SysCfg.SysCfgModel.SysCfgFileName);
                cfgModel.cfgFile = this.richTextBox1.Text;
                cfgModel.modifyTime = System.DateTime.Now;
                sysCfgBll.Update(cfgModel);
            }
            string reStr = ""; 
            
            XElement root = null;
            SysCfg.SysCfgModel.LoadCfg(ref root,ref reStr);
            MessageBox.Show("配置文件已保存！");
        }

        private void OnExportCfg()
        {
            if(this.richTextBox1.Text== "")
            {
                MessageBox.Show("配置文件为空");
                return;
            }
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "xml files|*.xml";
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string filePath = System.IO.Path.GetFullPath(dlg.FileName);
                FileStream fS = new FileStream(filePath, FileMode.OpenOrCreate);
                StreamWriter fW = new StreamWriter(fS);
                fW.Write(this.richTextBox1.Text);
                fW.Close();
                fS.Close();
                MessageBox.Show("导出成功");
            }
        }
        private void btnExportCfg_Click(object sender, EventArgs e)
        {
            OnExportCfg();
        }

        private void btnDispProcessParams_Click(object sender, EventArgs e)
        {
            OnDispProcessParams();
        }
        private void OnDispProcessParams()
        {
            string strWhere = string.Format("stepCata='{0}'","存储");
            DataSet ds = processStepBll.GetList(100, strWhere, "processSeq");
            DataTable dt = ds.Tables[0];
            this.dataGridView1.DataSource = dt;
            this.dataGridView1.Columns["processSeq"].Visible = false;
            this.dataGridView1.Columns["processStepID"].Visible = false;
            this.dataGridView1.Columns["stepCata"].Visible = false;
           
            this.dataGridView1.Columns["tag2"].Visible = false;
            this.dataGridView1.Columns["tag3"].Visible = false;
            this.dataGridView1.Columns["tag4"].Visible = false;
            this.dataGridView1.Columns["tag5"].Visible = false;
            this.dataGridView1.Columns["processStepName"].ReadOnly = true;
            this.dataGridView1.Columns["processStepName"].HeaderText = "老化工艺";
            this.dataGridView1.Columns["tag1"].HeaderText = "老化时间（小时）";
            this.dataGridView1.Columns["processStepName"].Width = 200;
            this.dataGridView1.Columns["tag1"].Width = 200;
            this.dataGridView1.Columns["tag1"].ValueType = typeof(float);
        }
        private void OnModifyProcessParams()
        {
            DataTable dt = this.dataGridView1.DataSource as DataTable;
            if(dt == null || dt.Rows.Count<1)
            {
                return;
            }
            foreach(DataRow dr in dt.Rows)
            {
                string processID = dr["processStepID"].ToString();
                ProcessStepModel process = processStepBll.GetModel(processID);
                if(process == null)
                {
                    continue;
                }
                float processVal= 0;
                if(!float.TryParse(dr["tag1"].ToString(),out processVal))
                {
                    continue;
                }
                process.tag1 = dr["tag1"].ToString();
                processStepBll.Update(process);
            }
        }
    }
}
