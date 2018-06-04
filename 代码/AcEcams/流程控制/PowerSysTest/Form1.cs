using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using AsrsPowerInterace;
namespace PowerSysTest
{
    public partial class Form1 : Form
    {
        IAsrsAccess asrsAccess = null;
        public Form1()
        {
            InitializeComponent();
            asrsAccess = new AsrsAccess();
            this.textBoxSvcAddr.Text = @"http://localhost:8733/ZZ/WCFPowerSys/SvcPowersys/";
            //this.comboBoxInterface.Items.AddRange(new string[] {"IsAsrsCellReady",
            //                                                    "PowerFillOk",
            //                                                    "CommitCheckResult",
            //                                                    "CellValidStatNotify",
            //                                                    "CellEmerDangerNotify"});

            this.comboBoxInterface.Items.AddRange(new string[] {"IsAsrsCellReady"});
            this.comboBoxInterface.SelectedIndex = 0;
        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            string reStr="";
            if(string.IsNullOrWhiteSpace(textBoxParam.Text))
            {
                Console.WriteLine("参数为空");
                return;
            }
            string[] taskParamArray = this.textBoxParam.Text.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
            if (taskParamArray == null || taskParamArray.Count() < 1)
            {
                Console.WriteLine("参数析错误");
                return;
            }
            short row =0;
            short col =0;
            short layer=0;
            string[] cellPos=null;
            if(comboBoxInterface.Text!="CommitCheckResult")
            {
                cellPos = taskParamArray[0].Split(new string[] { "-" }, StringSplitOptions.RemoveEmptyEntries);
                if (cellPos == null || cellPos.Count() < 3)
                {
                    Console.WriteLine("入库参数解析错误");
                    return;
                }
                row = short.Parse(cellPos[0]);
                col = short.Parse(cellPos[1]);
                layer = short.Parse(cellPos[2]);
            }
            

            switch(comboBoxInterface.Text)
            {
                case "IsAsrsCellReady":
                    {
                        string[] batteryCodes =null;

                        bool isReady= false;
                        asrsAccess.IsAsrsCellReady(row,col,layer, ref batteryCodes, ref isReady, ref reStr);
                        if(isReady)
                        {

                            Console.WriteLine("{0}-{1}-{2} 就绪，可以充电,模组条码：{3},{4}", row, col, layer, batteryCodes[0],batteryCodes[1]);
                        }
                        else
                        {
                            Console.WriteLine("{0}-{1}-{2} 未就绪，禁止充电，{3}", row, col, layer,reStr);
                        }
                       // Console.WriteLine(reStr);
                        break;
                    }
                case "PowerFillOk":
                    {
                       
                        if(asrsAccess.PowerFillOk(row, col, layer, ref reStr))
                        {
                            Console.WriteLine("充电完成通知成功,{0}-{1}-{2}", row, col, layer);
                        }
                        else
                        {
                            Console.WriteLine("充电完成通知失败,{0}-{1}-{2},{3}", row, col, layer,reStr);
                        }
                        break;
                    }
                case "CellValidStatNotify":
                    {
                        bool valid = bool.Parse(taskParamArray[1]);
                        string reason = taskParamArray[2];
                       
                        if(!asrsAccess.CellValidStatNotify(row, col, layer, valid, reason, ref reStr))
                        {
                            Console.WriteLine("调用CellValidStatNotify服务失败,{0}-{1}-{2}" + reStr,row,col,layer);
                        }
                        else
                        {
                            Console.WriteLine("调用CellValidStatNotify服务成功,{0}-{1}-{2}", row, col, layer);
                        }
                        break;
                    }
                case "CommitCheckResult":
                    {
                        string barcode=taskParamArray[0];
                        int checkRe = int.Parse(taskParamArray[1]);
                        if(!asrsAccess.CommitCheckResult(barcode, checkRe, ref reStr))
                        {
                            Console.WriteLine("调用CommitCheckResult失败" + reStr);
                        }
                        else
                        {
                            Console.WriteLine("调用CommitCheckResult成功");
                        }
                        break;
                    }
                case "CellEmerDangerNotify":
                    {
                        string reason="充电故障";
                        if(!asrsAccess.CellEmerDangerNotify(row,col,layer,reason,ref reStr))
                        {
                            Console.WriteLine("调用CellEmerDangerNotify失败" + reStr);
                        }
                        else
                        {
                            Console.WriteLine("调用CellEmerDangerNotify成功");
                        }
                        break;
                    }

                default:
                    break;
            }
        }   

        private void button1_Click(object sender, EventArgs e)
        {
            string reStr="";
            if(!asrsAccess.ConnectAsrs(this.textBoxSvcAddr.Text, ref reStr))
            {
                Console.WriteLine(reStr);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Console.SetOut(new TextBoxWriter(this.richTextBoxLog));
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            this.richTextBoxLog.Text = "";
        }
    }
}
