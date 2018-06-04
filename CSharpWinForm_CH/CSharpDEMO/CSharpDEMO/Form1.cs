using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using CSharpDEMO;
using System.Text.RegularExpressions;
using LeanCloud;
using System.Threading;
using System.Diagnostics;
using Excel = Microsoft.Office.Interop.Excel;
using System.IO;
using System.Data.SQLite;
using System.Reflection; // 引用这个才能使用Missing字段 
namespace CSharpDEMO
{
    public partial class Form1 : Form
    {
        extend_Class s = new extend_Class();
        private static sq sql;
        //数值串转化为十六进制字符串
        private string  dataBasePath= "D:/data/test.db";
        private byte[] StrToByetArray(string hexValues)
        {
            string[] hexValuesSplit = hexValues.Split(' ');
            byte[] retBytes = new byte[hexValuesSplit.Length];

            for (int nLoop = 0; nLoop < retBytes.Length; nLoop++)
            {
                retBytes[nLoop] = Convert.ToByte(hexValuesSplit[nLoop], 16);
            }

            return retBytes;
        }
        //字符数组转化为十六进制数组
        private byte[] StrToByetArray(string[] hexValues, int nLen)
        {
            byte[] retBytes = new byte[nLen];

            for (int nLoop = 0; nLoop < retBytes.Length; nLoop++)
            {
                retBytes[nLoop] = Convert.ToByte(hexValues[nLoop], 16);
            }

            return retBytes;
        }
        //十六进制字符串转化为数值串
        private string ByteArrayToStr(byte[] byteArray, bool bNeedFormat, int nStart, int nEnd)
        {
            //nEnd传递为0，转换到数组最后；

            string strReturn = "";

            if (bNeedFormat)
            {
                strReturn = "\r\nHEX RESULT:";
            }

            int nLoop = 0;
            nLoop += nStart;
            if (nEnd == 0)
            {
                nEnd = byteArray.GetLength(0);
            }
            else if (nEnd > byteArray.GetLength(0))
            {
                nEnd = byteArray.GetLength(0);
            }


            for (; nLoop < nEnd; nLoop++)
            {
                string strHex = "";

                if (bNeedFormat)
                {
                    if (nLoop % 16 != 0)
                    {
                        strHex = string.Format(" {0:X2}", byteArray[nLoop]);
                    }
                    else
                    {
                        strHex = string.Format("\r\n  {0:X2}...{1:X2}", nLoop / 16, byteArray[nLoop]);
                    }
                }
                else
                {
                    strHex = string.Format(" {0:X2}", byteArray[nLoop]);
                }

                strReturn += strHex;
            }

            return strReturn;
        }
        //转换错误代码
        private string FormatErrorCode(byte[] byteArray)
        {
            string strErrorCode = "";
            switch (byteArray[0])
            {
                case 0x80:
                    strErrorCode = "Success";
                    break;

                case 0x81:
                    strErrorCode = "Parameter Error";
                    break;

                case 0x82:
                    strErrorCode = "communication TimeOut";
                    break;

                case 0x83:
                    strErrorCode = "Couldn't Find Card ";
                    break;

                default:
                    strErrorCode = "Commond Error";
                    break;
            }

            return strErrorCode;
        }
        //字符截取，即不区分输入字符是否输入空格，均以两个字符处理为一个串
        private string[] strCutLength(string str, int iLength)
        {
            string[] reslut = null;
            if (!string.IsNullOrEmpty(str))
            {
                int iTemp = 0;
                string strTemp = null;
                System.Collections.ArrayList strArr = new System.Collections.ArrayList();
                for (int i = 0; i < str.Length; i++)
                {
                    if (str[i] == ' ')
                    {
                        continue;
                    }
                    else
                    {
                        iTemp++;
                        strTemp += str.Substring(i, 1);
                    }

                    //校验截取的字符是否在A~F、0~9区间
                    System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex(@"^(([A-F])*(\d)*)$");
                    if (!reg.IsMatch(strTemp))
                    {
                        return reslut;
                    }

                    if ((iTemp == iLength) || (i == str.Length - 1 && !string.IsNullOrEmpty(strTemp)))
                    {
                        strArr.Add(strTemp);
                        iTemp = 0;
                        strTemp = null;
                    }
                }
                if (strArr.Count > 0)
                {
                    reslut = new string[strArr.Count];
                    strArr.CopyTo(reslut);
                }
            }
            return reslut;
        }
        //输出日志文本
        private void WriteLog(string strText, int nRet, string strErrcode)
        {
            if (nRet != 0)
            {

                textResponse.Text += System.DateTime.Now.ToLocalTime().ToString() + " " + strText + "\r\n" + strErrcode + "\r\n";
            }
            else
            {
                textResponse.Text += System.DateTime.Now.ToLocalTime().ToString() + " " + strText + " " + "\r\n";
            }

            textResponse.Select(textResponse.TextLength, 0);//光标定位到文本最后
            textResponse.ScrollToCaret();//滚动到光标处
        }

        //转换卡号专用
        private byte[] convertSNR(string str, int keyN)
        {
            string regex = "[^a-fA-F0-9]";
            string tmpJudge = Regex.Replace(str,regex,"");    
       
            //长度不对，直接退回错误
            if (tmpJudge.Length != 12) return null;

            string[] tmpResult= Regex.Split(str,regex);
            byte[] result = new byte[keyN];
            int i = 0;
            foreach (string tmp in tmpResult)
            {
                result[i] = Convert.ToByte(tmp,16);
                i++;
            }
            return result;
        }

        //数据输入判断函数2个
        public string formatStr(string str, int num_blk)
        {
            
            string tmp=Regex.Replace(str,"[^a-fA-F0-9]","");
            //长度不对直接报错
            //num_blk==-1指示不用检查位数
            if (num_blk == -1) return tmp;
            //num_blk==其它负数，为-1/num_blk
            if (num_blk < -1)
            {
                if (tmp.Length != -16 / num_blk * 2) return null;
                else return tmp;
            }
            if (tmp.Length != 16*num_blk*2) return null;
            else return tmp;
        }
        private void convertStr(byte[] after, string before, int length)
        {
            for (int i = 0; i < length; i++)
            {
                after[i] = Convert.ToByte(before.Substring(2 * i, 2), 16);
            }
        }

        //显示结果
        private String showData(string text, byte[] data, int s, int e)
        {
            //非负转换
            for (int i = 0; i < e; i++)
            {
                if (data[s + i] < 0)
                    data[s + i] = Convert.ToByte(Convert.ToInt32(data[s + i]) + 256);
            }
            textResponse.Text += text;

            //for (int i = s; i < e; i++)
            //{
            //    textResponse.Text += data[i].ToString("X2")+" ";
            //}
            //textResponse.Text += "\r\n";

            for (int i = 0; i < e; i++)
            {
                textResponse.Text += data[s + i].ToString("X2")+" ";
            }
            textResponse.Text += "\r\n\r\n";
            return textResponse.Text;
       }
        private String showData_register(string text, byte[] data, int s, int e)
        {
            //非负转换
          

            for (int i = 0; i < e; i++)
            {
                textResponse.Text += data[s + i].ToString("X2") + " ";
            }
            textResponse.Text += "\r\n\r\n";
            String str= textResponse.Text;
            return str.Substring(str.Length - 16);
        }
        //显示命令执行结果
        private void showStatue(int Code)
        {
            string msg="";
            switch (Code)
            {
                case 0x00:
                    msg = "命令执行成功 .....";
                    break;
                case 0x01:
                    msg = "命令操作失败 .....";
                    break;
                case 0x02:
                    msg = "地址校验错误 .....";
                    break;
                case 0x03:
                    msg = "找不到已选择的端口 .....";
                    break;
                case 0x04:
                    msg = "读写器返回超时 .....";
                    break;
                case 0x05:
                    msg = "数据包流水号不正确 .....";
                    break;
                case 0x07:
                    msg = "接收异常 .....";
                    break;
                case 0x0A:
                    msg = "参数值超出范围 .....";
                    break;
                case 0x80:
                    msg = "参数设置成功 .....";
                    break;
                case 0x81:
                    msg = "参数设置失败 .....";
                    break;
                case 0x82:
                    msg = "通讯超时.....";
                    break;
                case 0x83:
                    msg = "卡不存在.....";
                    break;
                case 0x84:
                    msg = "接收卡数据出错.....";
                    break;
                case 0x85:
                    msg = "未知的错误.....";
                    break;
                case 0x87:
                    msg = "输入参数或者输入命令格式错误.....";
                    break;
                case 0x89:
                    msg = "输入的指令代码不存在.....";
                    break;
                case 0x8A:
                    msg = "在对于卡块初始化命令中出现错误.....";
                    break;
                case 0x8B:
                    msg = "在防冲突过程中得到错误的序列号.....";
                    break;
                case 0x8C:
                    msg = "密码认证没通过.....";
                    break;
                case 0x8F:
                    msg = "读取器接收到未知命令.....";
                    break;
                case 0x90:
                    msg = "卡不支持这个命令.....";
                    break;
                case 0x91:
                    msg = "命令格式有错误.....";
                    break;
                case 0x92:
                    msg = "在命令的FLAG参数中，不支持OPTION 模式.....";
                    break;
                case 0x93:
                    msg = "要操作的BLOCK不存在.....";
                    break;
                case 0x94:
                    msg = "要操作的对象已经别锁定，不能进行修改.....";
                    break;
                case 0x95:
                    msg = "锁定操作不成功.....";
                    break;
                case 0x96:
                    msg = "写操作不成功.....";
                    break;
                default:
                    msg = "未知错误2.....";
                    break;
            }
            textResponse.Text += msg + "\r\n";
        }

        public Form1()
        {
            InitializeComponent();
        }

        /*读取读卡器版本号*/
        private void btn_GetVersionNum_Click(object sender, EventArgs e)
        {
            byte[] byteArry = new byte[12];
            int nRet = Reader.GetVersionNum(byteArry);
            string txt="";
            if (nRet != 0)
            {
                showStatue(byteArry[0]);
            }
            else
            {
                /*-----------------------BUG标记：数组越界，11改为12----------------------*/
                txt=showData("读卡器版本号：", byteArry, 1, 11);
            }
            int start = 8, length =32;           
            txt_GetVersionNum.Text =txt.Substring(start - 1, length);
        }



        
        //14443A-MF

        /*MF_Read_Func*/
        private void btn_MF_Read_Click(object sender, EventArgs e)
        {
             //byte mode1 = (readKeyB.Checked) ? (byte)0x01 : (byte)0x00;
            //byte mode2 = (readAll.Checked) ? (byte)0x01 : (byte)0x00;
            //byte mode = (byte)((mode1 << 1) | mode2);
            //byte blk_add = Convert.ToByte(readStart.Text, 16);
            //byte num_blk = Convert.ToByte(readNum.Text, 16);
            //byte mode1 = 0x00;
            //byte mode2 = 0x00;
            byte mode = 0x0000;
            byte blk_add = 0x10;// Convert.ToByte(readStart.Text, 16);
            byte num_blk = 0x01;//Convert.ToByte(readNum.Text, 16);
           
            string password_A = "ff ff ff ff ff ff";
            byte[] snr = new byte[6];
            // snr = convertSNR(readKey.Text, 6);
            snr = convertSNR(password_A, 6);

            if (snr == null)
            {
                MessageBox.Show("序列号无效！", "错误");
                return;
            }

            byte[] buffer = new byte[16 * num_blk];

            int nRet = Reader.MF_Read(mode, blk_add, num_blk, snr, buffer);
        
            showStatue(nRet);
            if (nRet != 0)
            {
                //strErrorCode = FormatErrorCode(buffer);
                //WriteLog("Failed: ", nRet, strErrorCode);
                showStatue(buffer[0]);
            }
            else
            {
                showData("卡号：", snr, 0, 4);
                showData("数据：", buffer, 0, 16 * num_blk);
            }

        }

     

        private void btn_UL_Request_Click(object sender, EventArgs e)
        {
            byte[] snr = new byte[7];
            byte mode = (UL_snreadAll.Checked) ? (byte)0x01 : (byte)0x00;

            int nRet = Reader.UL_Request(mode, snr);
            //string strErrorCode;

            showStatue(nRet);
            if (nRet != 0)
            {
                //strErrorCode = FormatErrorCode(snr);
                //WriteLog("Failed:", nRet, strErrorCode);
                showStatue(snr[0]);
            }
            else
            {
                showData("卡号：", snr, 0, 7);
            }
        }

        private void btn_UL_Halt_Click(object sender, EventArgs e)
        {
            int nRet = Reader.MF_Halt();

            //textResponse.Text += "命令执行成功。\r\n";
            showStatue(nRet);
        }

        private void btn_UL_HLRead_Click(object sender, EventArgs e)
        {
            byte mode = (UL_readAll.Checked) ? (byte)0x01 : (byte)0x01;
            byte blk_add = Convert.ToByte(UL_readBlock.SelectedItem.ToString(), 16);

            byte[] snr = new byte[7];
            byte[] buffer = new byte[16];

            int nRet = Reader.UL_HLRead(mode, blk_add, snr, buffer);
            //string strErrorCode;

            showStatue(nRet);
            if (nRet != 0)
            {
                //strErrorCode = FormatErrorCode(buffer);
                //WriteLog("Failed:", nRet, strErrorCode);
                showStatue(buffer[0]);
            }
            else
            {
                showData("卡号",snr,0,7);
                showData("数据：", buffer, 0, 16);
            }

        }

        private void btn_UL_HLWrite_Click(object sender, EventArgs e)
        {
            byte mode = (UL_writeAll.Checked) ? (byte)0x01 : (byte)0x00;
            byte blk_add = Convert.ToByte(UL_writeBlock.SelectedItem.ToString(), 16);

            byte[] snr = new byte[7] { 0, 0, 0, 0, 0, 0, 0 };
            byte[] buffer = new byte[4];

            string bufferStr = formatStr(UL_writeData.Text, -1);
            convertStr(buffer, bufferStr, 4);

            int nRet = Reader.UL_HLWrite(mode, blk_add, snr, buffer);
            string strErrorCode;

            if (nRet != 0)
            {
                if (nRet == 10)
                {
                    //Something Different
                    strErrorCode = FormatErrorCode(buffer);
                    WriteLog("错误: ",nRet,strErrorCode);
                    showStatue(nRet);
                }
                else
                {
                    //textResponse.Text += "命令执行成功。\r\n";
                    showStatue(snr[0]);
                }
            }
            else
            {
                showData("卡号：",snr,0,7);
            }

        }

        private void btn_TypeB_Request_Click(object sender, EventArgs e)
        {
            byte[] buffer=new byte[256];

            int nRet = Reader.TypeB_Request(buffer);
            //string strErrorCode;

            showStatue(nRet);
            if (nRet != 0)
            {
                //strErrorCode = FormatErrorCode(buffer);
                //WriteLog("Failed:", nRet, strErrorCode);
                showStatue(buffer[0]);
            }
            else
            {
                showData("数据长度：", buffer, 0, 1);
                showData("数据：", buffer, 1, buffer[0]);
            }
        }

        private void btn_TypeB_TransCOS_Click(object sender, EventArgs e)
        {
            int cmdSize = int.Parse(B_Length.Text);
            byte[] buffer = new byte[256];
            //for (int i = 0; i < 256; i++)
            //{
            //    Console.WriteLine(buffer[i].ToString());
            //}
            byte[] cmd = new byte[cmdSize];

            string cmdStr = formatStr(B_Data.Text, -1);

            int nRet = Reader.TypeB_TransCOS(cmd, cmdSize, buffer);
            //string strErrorCode;

            showStatue(nRet);
            if (nRet != 0)
            {
                //strErrorCode = FormatErrorCode(buffer);
                //WriteLog("Failed:", nRet, strErrorCode);
                showStatue(buffer[0]);
            }
            else
            {
                showData("数据：",buffer,0,8);
            }
        }

        private void btn_TYPEB_SFZSNR_Click(object sender, EventArgs e)
        {
            byte mode = 0x26;
            byte halt = 0x00;
            byte[] value = new byte[8];
            byte[] snr=new byte[1];

            int nRet = Reader.TYPEB_SFZSNR(mode, halt, snr, value);
            //string strErrorCode;

            showStatue(nRet);
            if (nRet != 0)
            {
                //strErrorCode = FormatErrorCode(snr);
                //WriteLog("Failed:", nRet, strErrorCode);
                showStatue(snr[0]);
            }
            else
            {
                if (snr[0] == 0x00)
                    textResponse.Text += "只有一张卡……\r\n";
                else
                    textResponse.Text += "有多张卡……\r\n";
                showData("", value, 0, 8);
            }


        }

        private void btn_ISO15693_Inventory_Click(object sender, EventArgs e)
        {
            byte[] Cardnumber=new byte[1];
            byte[] pBuffer = new byte[256];

            int nRet = Reader.ISO15693_Inventory(Cardnumber, pBuffer);
            //string strErrorcode;

            showStatue(nRet);
            if (nRet != 0)
            {
                //strErrorcode = FormatErrorCode(pBuffer);
                //WriteLog("Failed:", nRet, strErrorcode);
                showStatue(Cardnumber[0]);
            }
            else
            {
                textResponse.Text += "卡号：\r\n";
                textResponse.Text += Convert.ToInt32(Cardnumber[0]).ToString("X2")+"\r\n";
                showData("读到的数据为：", pBuffer, 0, 10 * Cardnumber[0]);
                for (int i = 0; i < Convert.ToInt32(Cardnumber[0]); i++)
                {
                    textResponse.Text += "第" + i.ToString() + "张卡数据：\r\n";
                    string cardData="";
                    for (int j = 0; j < 8; j++)
                    {
                        cardData += pBuffer[Convert.ToInt32(Cardnumber[0]) * 10 - (i * 10 + j) - 1].ToString("X2")+" ";
                    }
                    textResponse.Text += cardData + "\r\n";
                }
            }
        }

        private void btn_ISO15693_Read_Click(object sender, EventArgs e)
        {
            byte flags = Convert.ToByte(isoreadFlag.Text);
            byte blk_add = Convert.ToByte(isoreadStart.Text);
            byte num_blk = Convert.ToByte(isoreadNum.Text);
            byte[] uid = new byte[8];
            string uidStr = formatStr(isoreadUID.Text, -2);
            if (uidStr == null)
            {
                MessageBox.Show("UID无效！", "错误");
                return;
            }
            convertStr(uid, uidStr, 8);

            byte[] buffer = new byte[256];
            int n;
            if (flags == 0x42)
                n = 5;
            else
                n = 4;

            int nRet = Reader.ISO15693_Read(flags, blk_add, num_blk, uid, buffer);
            //string strErrorCode;

            showStatue(nRet);
            if (nRet != 0)
            {
                //strErrorCode = FormatErrorCode(buffer);
                //WriteLog("Failed:", nRet, strErrorCode);
                showStatue(buffer[0]);
            }
            else
            {
                showData("标志位：", buffer, 0, 1);
                showData("数据：", buffer, 1, n * num_blk);
            }

        }

        private void btn_ISO15693_Write_Click(object sender, EventArgs e)
        {
            byte flags = Convert.ToByte(isowriteFlag.Text);
            byte blk_add = Convert.ToByte(isowriteStart.Text);
            byte num_blk = Convert.ToByte(isowriteNum.Text);

            

            byte[] uid = new byte[8];
            string uidStr = formatStr(isoreadUID.Text,-2);
            if (uidStr == null)
            {
                MessageBox.Show("UID无效！","错误");
                return;
            }
            convertStr(uid, uidStr, 8);

            byte[] data = new byte[256];
            int n;
            if (flags == 0x42)
                n = 5;
            else
                n = 4;
            //讨论
            string dataStr;
            if ((double)num_blk * (double)n / (double)16 < 1)
                dataStr = formatStr(isowriteData.Text, (int)((double)-1/((double)num_blk * (double)n / (double)16)));
            else
                dataStr = formatStr(isowriteData.Text, num_blk * n / 16);

            convertStr(data, dataStr, num_blk * n);

            int nRet = Reader.ISO15693_Write(flags, blk_add, num_blk, uid, data);
            //string strErrorCode;

            showStatue(nRet);
            //if (nRet != 0)
            //{
            //    strErrorCode = FormatErrorCode(data);
            //    WriteLog("Failed:", nRet, strErrorCode);
            //}
            //else
            //{
            //    textResponse.Text += "Succeed!";
            //}
            if (nRet != 0)
            {
                showStatue(data[0]);
            }
        }

        private void btn_ISO15693_Lock_Click(object sender, EventArgs e)
        {
            byte flags = Convert.ToByte(blklockFlag.Text);
            byte blk_add = Convert.ToByte(blklockStart.Text);
            byte[] uid = new byte[8];
            string uidStr = formatStr(blklockUID.Text, -2);
            if (uidStr == null)
            {
                MessageBox.Show("UID无效！", "错误");
                return;
            }
            convertStr(uid, uidStr, 8);

            byte[] buffer = new byte[1];
            int nRet = Reader.ISO15693_Lock(flags, blk_add, uid, buffer);
            //string strErrorCode;

            //if (nRet != 0)
            //{
            //    strErrorCode = FormatErrorCode(buffer);
            //    WriteLog("Failed:", nRet, strErrorCode);
            //}
            //else
            //{
            //    showStatue(nRet);
            //    showStatue(buffer[0]);
            //}
            showStatue(nRet);
            showStatue(buffer[0]);
        }

        private void btn_ISO15693_Select_Click(object sender, EventArgs e)
        {
            byte flags = Convert.ToByte(cardselectFlag.Text);
            byte[] uid = new byte[8];
            string uidStr = formatStr(cardselectUID.Text, -2);
            if (uidStr == null)
            {
                MessageBox.Show("UID无效！", "错误");
                return;
            }
            convertStr(uid, uidStr, 8);

            byte[] buffer = new byte[1];
            int nRet = Reader.ISO15693_Select(flags, uid, buffer);
            //string strErrorCode;

            //if (nRet != 0)
            //{
            //    strErrorCode = FormatErrorCode(buffer);
            //    WriteLog("Failed:", nRet, strErrorCode);
            //}
            //else
            //{
            //    showStatue(nRet);
            //    showStatue(buffer[0]);
            //}
            showStatue(nRet);
            showStatue(buffer[0]);
        }

        private void btn_ISO15693_WriteAFI_Click(object sender, EventArgs e)
        {
            byte flags = Convert.ToByte(afiwriteFlag.Text);
            byte[] uid = new byte[8];
            string uidStr = formatStr(afiwriteUID.Text, -2);
            if (uidStr == null)
            {
                MessageBox.Show("UID无效！", "错误");
                return;
            }
            convertStr(uid, uidStr, 8);
            byte afi = Convert.ToByte(afiwriteAFI.Text);
            byte[] buffer = new byte[1];

            int nRet = Reader.ISO15693_WriteAFI(flags, afi, uid, buffer);
            //string strErrorCode;

            //if (nRet != 0)
            //{
            //    strErrorCode = FormatErrorCode(buffer);
            //    WriteLog("Failed:", nRet, strErrorCode);
            //}
            //else
            //{
            //    showStatue(nRet);
            //    showStatue(buffer[0]);
            //}
            showStatue(nRet);
            showStatue(buffer[0]);
        }

        private void btn_ISO15693_LockAFI_Click(object sender, EventArgs e)
        {
            byte flags = Convert.ToByte(afilockFlag.Text);
            byte[] uid = new byte[8];
            string uidStr = formatStr(afilockUID.Text, -2);
            if (uidStr == null)
            {
                MessageBox.Show("UID无效！", "错误");
                return;
            }
            convertStr(uid, uidStr, 8);

            byte[] buffer = new byte[1];
            int nRet = Reader.ISO15693_LockAFI(flags, uid, buffer);

            showStatue(nRet);
            showStatue(buffer[0]);
        }

        private void btn_ISO15693_WriteDSFID_Click(object sender, EventArgs e)
        {
            byte flags = Convert.ToByte(dsfidwriteFlag.Text);
            byte[] uid = new byte[8];
            string uidStr = formatStr(dsfidwriteUID.Text, -2);
            if (uidStr == null)
            {
                MessageBox.Show("UID无效！", "错误");
                return;
            }
            convertStr(uid, uidStr, 8);

            byte DSFID = Convert.ToByte(dsfidwriteDSFID.Text);
            byte[] buffer = new byte[1];

            int nRet = Reader.ISO15693_WriteDSFID(flags, DSFID, uid, buffer);
            showStatue(nRet);
            showStatue(buffer[0]);
        }

        private void btn_ISO15693_LockDSFID_Click(object sender, EventArgs e)
        {
            byte flags = Convert.ToByte(lockdsfidFlag.Text);
            byte[] uid = new byte[8];
            string uidStr = formatStr(lockdsfidUID.Text, -2);
            if (uidStr == null)
            {
                MessageBox.Show("UID无效！", "错误");
                return;
            }
            convertStr(uid, uidStr, 8);

            byte[] buffer = new byte[1];
            int nRet = Reader.ISO15693_LockDSFID(flags, uid, buffer);
            showStatue(nRet);
            showStatue(buffer[0]);

        }

        private void btn_ISO15693_GetSysInfo_Click(object sender, EventArgs e)
        {
            byte flag = Convert.ToByte(getsysFlag.Text);
            byte[] uid = new byte[8];
            byte[] Buffer = new byte[255];
            string uidStr = formatStr(getsysUID.Text, -2);
            if (uidStr == null)
            {
                MessageBox.Show("UID无效！", "错误");
                return;
            }
            convertStr(uid, uidStr, 8);

            int nRet = Reader.ISO15693_GetSysInfo(flag, uid, Buffer);
            showStatue(nRet);
            if (nRet != 0)
            {
                showStatue(Buffer[0]);
            }
            else
            {
                showData("标志位",Buffer,0,1);
                showData("INFO Flags:", Buffer, 1, 1);
                showData("UID:", Buffer, 2, 8);
                showData("DSFID:", Buffer, 10, 1);
                showData("AFI", Buffer, 11, 11);
                showData("Other fields:", Buffer, 12, 5);
            }

        }

        private void btn_ISO15693_GetMulSecurity_Click(object sender, EventArgs e)
        {
            byte flags = Convert.ToByte(blksecgetFlag.Text);
            byte blkAddr = Convert.ToByte(blksecgetStart.Text);
            byte blkNum = Convert.ToByte(blksecgetNum.Text);
            byte[] uid = new byte[8];
            string uidStr = formatStr(blklockUID.Text, -2);
            if (uidStr == null)
            {
                MessageBox.Show("UID无效！", "错误");
                return;
            }
            convertStr(uid, uidStr, 8);

            byte[] pBuffer = new byte[blkNum + 1];

            int nRet = Reader.ISO15693_GetMulSecurity(flags, blkAddr, blkNum, uid, pBuffer);
            showStatue(nRet);
            if (nRet == 0)
            {
                showData("标志位：", pBuffer, 0, 1);
                showData("块安全位：", pBuffer, 1, blkNum);
            }
            else
            {
                showStatue(pBuffer[0]);
            }
        }

       async private void button_register_Click(object sender, EventArgs e)
        {
            int step = 0;
            string tel = textBox_tel.Text;
            if (tel.Length != 11)
            { MessageBox.Show("电话号码格式不正确");MessageBox.Show("号码长度为："+tel.Length.ToString()); }
            else
            {
                step = 1;
                try {/*防止读写过程中出错*/
                    textBox_cardNum.Text = readCard(0x00, 1).Substring(0, 8);
                    writeCard(0x02, 1, s.String2Unicode("学生"));
                }
                catch (Exception ee) {
                    MessageBox.Show(ee.Message);
                    this.Close();
                }
                finally { }
               

                //write data to leancloud

                string Card_num = textBox_cardNum.Text;
                string name = textBox_name.Text;
                
                string gender = comboBox_gender.Text;
                string birth = comboBox_month.Text + comboBox_date.Text;
                string courseName = comboBox_courseName.Text;
                string level = comboBox_level.Text;
                string courseType = comboBox_courseType.Text;
                string courseTime = textBox_courseTime.Text;   //课时数
                string stuTeacher = comboBox_stuTeacher.Text;
                string age = textBox_age.Text;
                string date = dateTimePicker2.Value.Date.ToString();
                string date_YMD = date.Substring(0, date.Length - 8);
                int times = Convert.ToInt16(courseTime);
                int price = Convert.ToInt16(textBox_price.Text);
                int sum_money = price * times;
                label_sum.Text = sum_money.ToString();

                /*防重注册功能**/
                try
                {
                    AVQuery<AVObject> query = new AVQuery<AVObject>("Student").WhereEqualTo("name", name).WhereEqualTo("tel", tel);
                    await query.FindAsync().ContinueWith(t =>
                    {
                        IEnumerable<AVObject> persons = t.Result;
                        int sum = persons.Count();
                    });
                    int num = query.CountAsync().Result;
                   // MessageBox.Show(num.ToString());
                    if (num > 0)  //查到的数据为0个
                    {
                        MessageBox.Show("已经有注册信息，请删除后重新注册");
                        
                    }
                    else
                    {
                        step = 2;
                        AVObject Student = new AVObject("Student");
                        Student["Card_num"] = textBox_cardNum.Text;
                        Student["name"] = textBox_name.Text;
                        Student["tel"] = textBox_tel.Text;
                        Student["gender"] = comboBox_gender.Text;
                        Student["birth"] = comboBox_month.Text + comboBox_date.Text;
                        Student["age"] = age;
                        Student["courseName"] = comboBox_courseName.Text;
                        Student["level"] = comboBox_level.Text;
                        Student["courseType"] = comboBox_courseType.Text;
                        Student["courseTime"] = courseTime;          //课时数
                        Student["courseTimeLeft"] = courseTime;          //剩余课时数
                        Student["price"] = price.ToString();          //课程单价
                        Student["sum_money"] = sum_money.ToString();          //总费用
                        Student["stuTeacher"] = stuTeacher;          //学生的老师

                        
                        Student["regTime"] = date_YMD;
                        await Student.SaveAsync();
                        writeCard(0x04, 1, s.KeyID2Card(Student.ObjectId));
                        writeCard(0x02, 1, s.String2Unicode("学生"));
                    }
                }
                catch(Exception erro){
                    MessageBox.Show("网络错误"+erro.Message);
                    this.Close();
                }

                // while (Student.ObjectId == null)
                //{ Thread.Sleep(100); }
                // MessageBox.Show(Student.ObjectId);
                /********存储数据到卡中****************/
                //writeCard(0x04, 1, s.KeyID2Card(Student.ObjectId));
                // writeCard(0x02, 1, s.String2Unicode("学生"));
                /*  删掉之后卡里只存身份和objectID*/
                //writeCard(0x05, 1, s.String2Unicode(name));
                //writeCard(0x06, 1, s.S2U(tel));   

                //writeCard(0x08, 1, s.String2Unicode(gender));
                //writeCard(0x09, 1, s.String2Unicode(birth));
                //writeCard(0x0A, 1, s.S2U(age));       //剩余课时数
                //writeCard(0x0C, 1, s.String2Unicode(courseName));
                //writeCard(0x0D, 1, s.String2Unicode(level));
                //writeCard(0x0E, 1, s.String2Unicode(courseType));
                //writeCard(0x10, 1, s.S2U(date_YMD));                //注册时间
                //writeCard(0x11, 1, s.S2U(courseTime));       //课时数
                //writeCard(0x12, 1, s.S2U(price.ToString()));       //课时单价
                //writeCard(0x14, 1, s.S2U(sum_money.ToString()));       //课时总价
                //writeCard(0x15, 1, s.S2U(courseTime));       //剩余课时数
                //writeCard(0x16, 1, s.String2Unicode(stuTeacher));       //学生的老师是谁
                /*删除部分数据，卡里不再存储数据*/
                /******************************************/

                /********存储数据到本地sqlite**********************************/
                if (step == 2)
                {
                    try
                    {
                        sql = new sq("data source= D:/data/test.db");
                        //创建名为table1的数据表
                        sql.CreateTable("student", new string[] { "regTime", "Card_num", "Name", "tel", "gender", "birth", "age", "coureseName", "level", "courseType", "courseTime", "courseTimeLeft", "price", "sum_money", "stuTeacher" },
                                                    new string[] { "TEXT", "TEXT", "TEXT", "TEXT", "TEXT", "TEXT", "INTEGER", "TEXT", "TEXT", "TEXT", "INTEGER", "INTEGER", "INTEGER", "INTEGER", "TEXT" });
                        //插入数据
                        sql.InsertValues("student", new string[] { date_YMD, Card_num, name, tel, gender, birth, age, courseName, level, courseType, courseTime, courseTime, price.ToString(), sum_money.ToString(), stuTeacher });
                        sql.CloseConnection();

                        Excel.Application excelApp = new Excel.Application();
                        if (excelApp == null)
                        {
                            // if equal null means EXCEL is not installed.  
                            MessageBox.Show("Excel is not properly installed!");
                        }

                        string excelPath = @"D:\学生注册.xls";
                        string filename = excelPath;// @"D:\生产产量纪录.xls";
                                                    // open a workbook,if not exist, create a new one  
                        Excel.Workbook workBook;
                        if (File.Exists(filename))
                        {
                            workBook = excelApp.Workbooks.Open(filename, 0, false, 5, "", "", true, Excel.XlPlatform.xlWindows, "\t", false, false, 0, true, 1, 0);
                        }
                        else
                        {
                            workBook = excelApp.Workbooks.Add(true);
                        }
                        //new a worksheet  
                        Excel.Worksheet workSheet = workBook.ActiveSheet as Excel.Worksheet;
                        //write data
                        workSheet = (Excel.Worksheet)workBook.Worksheets.get_Item(1);//获得第i个sheet，准备写入  
                        workSheet.Cells[1, 1] = "注册时间";
                        workSheet.Cells[1, 2] = "卡号";
                        workSheet.Cells[1, 3] = "姓名";
                        workSheet.Cells[1, 4] = "电话";
                        workSheet.Cells[1, 5] = "性别";
                        workSheet.Cells[1, 6] = "生日";
                        workSheet.Cells[1, 7] = "年龄";
                        workSheet.Cells[1, 8] = "课程";
                        workSheet.Cells[1, 9] = "级别";
                        workSheet.Cells[1, 10] = "课程类型";
                        workSheet.Cells[1, 11] = "总课时";
                        workSheet.Cells[1, 12] = "剩余课时";
                        workSheet.Cells[1, 13] = "价格";
                        workSheet.Cells[1, 14] = "总钱数";
                        workSheet.Cells[1, 15] = "老师";

                        Microsoft.Office.Interop.Excel.Range range = workSheet.UsedRange;
                        int colCount = range.Columns.Count;
                        int rowCount = range.Rows.Count;

                        workSheet.Cells[rowCount + 1, 1] = date_YMD;
                        workSheet.Cells[rowCount + 1, 2] = Card_num;
                        workSheet.Cells[rowCount + 1, 3] = name;
                        workSheet.Cells[rowCount + 1, 4] = tel;
                        workSheet.Cells[rowCount + 1, 5] = gender;
                        workSheet.Cells[rowCount + 1, 6] = birth;
                        workSheet.Cells[rowCount + 1, 7] = age;
                        workSheet.Cells[rowCount + 1, 8] = courseName;
                        workSheet.Cells[rowCount + 1, 9] = level;
                        workSheet.Cells[rowCount + 1, 10] = courseType;
                        workSheet.Cells[rowCount + 1, 11] = courseTime;
                        workSheet.Cells[rowCount + 1, 12] = courseTime;
                        workSheet.Cells[rowCount + 1, 13] = price.ToString();
                        workSheet.Cells[rowCount + 1, 14] = sum_money.ToString();
                        workSheet.Cells[rowCount + 1, 15] = stuTeacher;
                        //set visible the Excel will run in background  
                        excelApp.Visible = false;
                        //set false the alerts will not display  
                        excelApp.DisplayAlerts = false;
                        workBook.SaveAs(filename);
                        workBook.Close(false, Missing.Value, Missing.Value);
                        //quit and clean up objects  
                        excelApp.Quit();
                        workSheet = null;
                        workBook = null;
                        excelApp = null;
                        GC.Collect();

                    }
                    catch (Exception erro)
                    {
                        MessageBox.Show("本地数据存储错误" + erro.Message);
                        this.Close();
                    }

                    /***********************************************************/
                    // 再检查一遍，同时鸣笛一声。

                    byte[] buffer = new byte[1];
                    int nRet_boomer = Reader.ControlBuzzer(20, 1, buffer);//（占空比，次数，没有用但是要的一个参数）
                    int nRet_led = Reader.ControlLED(20, 3, buffer);
                    MessageBox.Show("注册完成");
                }
                else
                { }
             
               
            }
        }
      
        private void Form1_Load(object sender, EventArgs e)
        {
          // comboBox_gender.Text = "男";
        
        comboBox_gender.SelectedIndex = comboBox_gender.Items.IndexOf("男");
          //comboBox_courseType.SelectedIndex= comboBox_gender.Items.IndexOf("一对一");
          AVClient.Initialize("uwQ2g76bvCIMHAOUrdC08Lpn-gzGzoHsz", "ioIOpV4kQ6Cw0f3Eu97348qp");
         

        }

        private void button1_Click(object sender, EventArgs e)
        {
            
            extend_Class a=new extend_Class() ;
           // string date = a.String2Unicode(dateTimePicker2.Value.Date.ToString());
            //string test = a.String2Unicode(date.Substring(0, date.Length - 9));
            Console.WriteLine("helloworld");
           
             string test = a.String2Unicode("汉字");
            Console.WriteLine(test);
            Console.WriteLine(a.StringToUnicode("汉字"));
            Console.WriteLine(a.Unicode2String(test));
            string register_date = dateTimePicker2.Value.ToString();
           
            //控制蜂鸣器和绿色LED
            byte[] buffer = new byte[1];
            int nRet_boomer = Reader.ControlBuzzer(20, 2, buffer);//（占空比，次数，没有用但是要的一个参数）
            int nRet_led = Reader.ControlLED(20,3, buffer);
            //  textBox1.Text =a.UnicodeToString(test);
            // textBox1.Text=a.readData(0x10, 0x01);
            string date = dateTimePicker2.Value.Date.ToString();            
            string txt =date.Substring(0,date.Length-8);

            //textBox1.Text = register_date.Substring(0, register_date.Length - 9);
            textBox1.Text = a.S2U(txt);
           a.writeData(a.S2U(txt), 0x10, 0x01);
            //Console.WriteLine(a.S2U(txt));
            Console.WriteLine(a.S2U(txt));
            string aa = a.S2U(txt);

            Console.WriteLine(a.U2S(aa));
            textBox1.Text = a.U2S(aa);
            MessageBox.Show("Hello~~~");
            /*
            AVObject character_get = AVObject.CreateWithoutData("Character", "5acb7bb59f54541c8bb9df2a");
            character_get.FetchAsync();
             Thread.Sleep(2000);
            textBox1.Text = character_get.Get<String>("name");
            */
        }

        private void read_btn_Click(object sender, EventArgs e)
        {
            if (cardCheck())
                textBox1.Text = readCard(0x00, 1).Substring(0,8);
            else
                textBox1.Text = "no card";
        }
        private void write_btn_Click(object sender, EventArgs e)
        {
            writeCard(0x10,1,"11 22 33 44 55 66 77 88 99 00 aa bb cc dd ab cd");
        }
        private void writeCard(byte blk_add, byte num_blk,string text)
        {
            
            byte mode = 0x0000;
           // string text_card = s.S2U(text);
            string password_A = "ff ff ff ff ff ff";
            byte[] snr = new byte[6];
            // snr = convertSNR(readKey.Text, 6);
            snr = convertSNR(password_A, 6);
            byte[] buffer = new byte[16 * num_blk];
            // string bufferStr = formatStr(text_card, num_blk);
            string bufferStr = formatStr(text, num_blk);
            convertStr(buffer, bufferStr, 16 * num_blk);
            int nRet = Reader.MF_Write(mode, blk_add, num_blk, snr, buffer);
            //string strErrorCode;
            if (buffer[0] == 0x83)
            { MessageBox.Show("没有检测到卡！"); }
            showStatue(nRet);
            if (nRet != 0)
            {
                MessageBox.Show("数据传输出错，需要重启机器！");
                this.Close();
            }
            /*
            else
            {
                MessageBox.Show("数据写入成功！");
            }
            */
        }
        private string readCard(byte blk_add, byte num_blk)
        {
            byte mode = 0x0000;
            string password_A = "ff ff ff ff ff ff";
            byte[] snr = new byte[6];
            snr = convertSNR(password_A, 6);
            byte[] buffer = new byte[16 * num_blk];
            int nRet = Reader.MF_Read(mode, blk_add, num_blk, snr, buffer);
            if (buffer[0] == 0x83)
            {
                MessageBox.Show("卡不存在！");
               return s.String2Unicode("卡不存在");
            }
            return ToHexString(buffer);
        }
        private bool cardCheck()
        {
            byte mode = 0x0000;
            string password_A = "ff ff ff ff ff ff";
            byte[] snr = new byte[6];
            snr = convertSNR(password_A, 6);
            byte[] buffer = new byte[16 * 1];
            int nRet = Reader.MF_Read(mode, 0, 1, snr, buffer);
            if (buffer[0] == 0x83)
            {
                MessageBox.Show("卡不存在！");
                return false;
            }
            else
                return true;
            
        }
        public static string ToHexString(byte[] bytes) // 0xae00cf => "AE00CF "
        {
            string hexString = string.Empty;
            if (bytes != null)
            {
                StringBuilder strB = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    strB.Append(bytes[i].ToString("X2"));
                }
                hexString = strB.ToString();
            }
            return hexString;
        }

       async private void button_register_teacher_Click(object sender, EventArgs e)
        {
            int step = 0;
            textBox_teacherCard.Text = readCard(0x00, 1).Substring(0, 8);
            string tel = textBox_teacherTel.Text;
            if (tel.Length != 11)
            { MessageBox.Show("电话号码格式不正确"); MessageBox.Show("号码长度为："+tel.Length.ToString()); }
            else
            {
                step = 1;//tel is OK!
                try
                {/*防止读写过程中出错*/
                    textBox_cardNum.Text = readCard(0x00, 1).Substring(0, 8);
                    writeCard(0x02, 1, s.String2Unicode("老师"));
                }
                catch (Exception ee)
                {
                    MessageBox.Show(ee.Message);
                    this.Close();
                }
                finally { }


                string Card_num = textBox_teacherCard.Text;
                string name = textBox_teacherName.Text;
            
                string gender = comboBox_TeacherGender.Text;
                string age = textBox_teacherAge.Text;
                string birth = comboBox_teacherBirthMon.Text + comboBox_teacherBirthDate.Text;
                string courseName = comboBox_teacherCourse.Text;
                string percent = textBox_percent.Text;
                string date = dateTimePicker2.Value.Date.ToString();
                string date_YMD = date.Substring(0, date.Length - 8);

                try
                {
                    AVQuery<AVObject> query = new AVQuery<AVObject>("Teacher").WhereEqualTo("name",name).WhereEqualTo("tel", tel);
                    await query.FindAsync().ContinueWith(t =>
                    {
                        IEnumerable<AVObject> persons = t.Result;
                        int sum = persons.Count();
                    });
                    int num = query.CountAsync().Result;
                   // MessageBox.Show(num.ToString());
                    if (num > 0)  //查到的数据为0个
                    {
                        MessageBox.Show("已经有注册信息，请删除后重新注册");

                    }
                    else
                    {
                        step = 2;// 数据没有重复
                        AVObject Teacher = new AVObject("Teacher");
                        Teacher["Card_num"] = Card_num;
                        Teacher["name"] = name;
                        Teacher["age"] = age;
                        Teacher["tel"] = tel;
                        Teacher["gender"] = comboBox_TeacherGender.Text;
                        Teacher["birth"] = comboBox_teacherBirthMon.Text + comboBox_teacherBirthDate.Text;
                        Teacher["course"] = comboBox_teacherCourse.Text;
                        Teacher["percent"] = percent;                       
                        Teacher["regTime"] = date_YMD;
                        // MessageBox.Show(date_YMD);
                        int wage = 0;
                        Teacher["wage"] = wage.ToString();
                        Task saveTask = Teacher.SaveAsync();
                        await saveTask;
                        writeCard(0x04, 1, s.KeyID2Card(Teacher.ObjectId));
                        writeCard(0x02, 1, s.String2Unicode("老师"));
                    }
                }
                catch (Exception erro)
                {
                    MessageBox.Show("网络错误" + erro.Message);
                    this.Close();
                }

                /*
                 while (Teacher.ObjectId == null)
                 { Thread.Sleep(100); }
               */


                /**********卡里只存ObjectID和身份**************************/
                //writeCard(0x05, 1, s.String2Unicode(name));
                //writeCard(0x06, 1, s.S2U(tel));

                //writeCard(0x08, 1, s.String2Unicode(gender));
                //writeCard(0x09, 1, s.String2Unicode(birth));
                //writeCard(0x0A, 1, s.S2U(age));       //老师年龄
                //writeCard(0x0C, 1, s.String2Unicode(courseName));

                //writeCard(0x10, 1, s.S2U(date_YMD));       //注册时间
                //writeCard(0x20, 1, s.S2U(wage.ToString()));       //本月工资
                //writeCard(0x18, 1, s.S2U(percent.ToString()));       //工资比例
                /***********************************************/

                /********存储数据到本地sqlite**********************************/
                if (step == 2)
                {
                    try
                    {
                        sql = new sq("data source= D:/data/test.db");
                        //创建名为table1的数据表
                        sql.CreateTable("teacher", new string[] { "regTime", "Card_num", "Name", "tel", "gender", "birth", "age", "coureseName ", "percent", "wage" },
                                                   new string[] { "TEXT", "TEXT", "TEXT", "TEXT", "TEXT", "TEXT", "INTEGER", "TEXT", "INTEGER", "INTEGER" });
                        //插入数据
                        sql.InsertValues("teacher", new string[] { date_YMD, Card_num, name, tel, gender, birth, age, courseName, percent, "0" });
                        sql.CloseConnection();
                        Excel.Application excelApp = new Excel.Application();
                        if (excelApp == null)
                        {
                            // if equal null means EXCEL is not installed.  
                            MessageBox.Show("Excel is not properly installed!");
                        }

                        string excelPath = @"D:\老师注册.xls";
                        string filename = excelPath;// @"D:\生产产量纪录.xls";
                                                    // open a workbook,if not exist, create a new one  
                        Excel.Workbook workBook;
                        if (File.Exists(filename))
                        {
                            workBook = excelApp.Workbooks.Open(filename, 0, false, 5, "", "", true, Excel.XlPlatform.xlWindows, "\t", false, false, 0, true, 1, 0);
                        }
                        else
                        {
                            workBook = excelApp.Workbooks.Add(true);
                        }
                        //new a worksheet  
                        Excel.Worksheet workSheet = workBook.ActiveSheet as Excel.Worksheet;
                        //write data
                        workSheet = (Excel.Worksheet)workBook.Worksheets.get_Item(1);//获得第i个sheet，准备写入  
                        workSheet.Cells[1, 1] = "注册时间";
                        workSheet.Cells[1, 2] = "卡号";
                        workSheet.Cells[1, 3] = "姓名";
                        workSheet.Cells[1, 4] = "电话";
                        workSheet.Cells[1, 5] = "性别";
                        workSheet.Cells[1, 6] = "生日";
                        workSheet.Cells[1, 7] = "年龄";
                        workSheet.Cells[1, 8] = "课程";
                        workSheet.Cells[1, 9] = "分成";
                        workSheet.Cells[1, 10] = "工资";


                        Microsoft.Office.Interop.Excel.Range range = workSheet.UsedRange;
                        int colCount = range.Columns.Count;
                        int rowCount = range.Rows.Count;

                        workSheet.Cells[rowCount + 1, 1] = date_YMD;
                        workSheet.Cells[rowCount + 1, 2] = Card_num;
                        workSheet.Cells[rowCount + 1, 3] = name;
                        workSheet.Cells[rowCount + 1, 4] = tel;
                        workSheet.Cells[rowCount + 1, 5] = gender;
                        workSheet.Cells[rowCount + 1, 6] = birth;
                        workSheet.Cells[rowCount + 1, 7] = age;
                        workSheet.Cells[rowCount + 1, 8] = courseName;
                        workSheet.Cells[rowCount + 1, 9] = percent;
                        workSheet.Cells[rowCount + 1, 10] = "0";

                        //set visible the Excel will run in background  
                        excelApp.Visible = false;
                        //set false the alerts will not display  
                        excelApp.DisplayAlerts = false;
                        workBook.SaveAs(filename);
                        workBook.Close(false, Missing.Value, Missing.Value);
                        //quit and clean up objects  
                        excelApp.Quit();
                        workSheet = null;
                        workBook = null;
                        excelApp = null;
                        GC.Collect();

                    }
                    catch (Exception erro)
                    {
                        MessageBox.Show("本地数据库存储错误：" + erro.Message);
                        this.Close();
                    }
                    /***********************************************************/


                    // 再检查一遍，同时鸣笛一声。

                    byte[] buffer = new byte[1];
                    int nRet_boomer = Reader.ControlBuzzer(20, 1, buffer);//（占空比，次数，没有用但是要的一个参数）
                    int nRet_led = Reader.ControlLED(20, 3, buffer);
                    MessageBox.Show("注册完成");
                }
                else
                { }
              
            }
        }

   

       async private void button_readinfo_Click(object sender, EventArgs e)
        {

            textBox_wage.Visible = false;
            label_wage.Visible = false;

            textBoxCourseTypeShow.Visible = true;
            textBox_courseTime_left.Visible = true;
            textBox_courseTimeSum.Visible = true;
            textBox_teacherShow.Visible = true;
            textBoxCourseTypeShow.Visible = true;
            label_coursetime_left.Visible = true;
            label_courseTypeShow.Visible = true;
            label_teacherShow.Visible = true;
            label_sumCourse.Visible = true;
            comboBox1.Visible = true;
            //读卡
            string ObID = "";
            try
            {
                //获取信息
                ObID = s.Card2KeyID(readCard(0x04, 1));
                //MessageBox.Show(ObID);
            }
            catch (Exception error)
            {
                MessageBox.Show("刷卡机需要重启，错误："+ error.Message);
                this.Close();

            }

            AVQuery<AVObject> query = new AVQuery<AVObject>("Student").WhereEqualTo("objectId", ObID);            
            await query.FindAsync().ContinueWith(t => {
                IEnumerable<AVObject> persons = t.Result;
                //sum = 0;
                int sum = persons.Count();

            });
            
            if (query.CountAsync().Result == 0)  //查到的数据为0个
            { MessageBox.Show("没有查到相关学生信息！"); }
            else
            {
                AVObject myObject = query.FirstAsync().Result;     
                /*************************************************************************/
                //read data from leancloud

                string Card_num = readCard(0x00, 1).Substring(0, 8);
                string name = myObject.Get<String>("name");
                string tel = myObject.Get<String>("tel");
                string gender = myObject.Get<String>("gender");
                string birth = myObject.Get<String>("birth");
                string courseName = myObject.Get<String>("courseName");
                string level = myObject.Get<String>("level");
                string courseType = myObject.Get<String>("courseType");
                string courseTime = myObject.Get<String>("courseTime");   //总课时数
                string courseTimeLeft = myObject.Get<String>("courseTimeLeft");   //剩余课时数
                string coursePrice = myObject.Get<String>("price");   //单价
                string objectID = myObject.ObjectId;
                
                int times = Convert.ToInt16(courseTimeLeft);
                if (times == 0)
                { MessageBox.Show("剩余课时为0，请充值"); }
                else
                {
                    myObject["courseTimeLeft"] = (times - 1).ToString();
                    await myObject.SaveAsync();
                    writeCard(0x15, 1, s.S2U((times - 1).ToString()));       //剩余课时数写卡
                    textBox_courseTime_left.Text = s.U2S(readCard(0x15, 1));
                    AVObject Payroll = new AVObject("Payroll");
                    Payroll["time"] = DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss");
                    Payroll["month"] = DateTime.Now.ToString("yyyyMM");
                    Payroll["student"] = myObject.Get<String>("name");
                    Payroll["teacher"] = myObject.Get<String>("stuTeacher");
                    Payroll["pay"] = myObject.Get<String>("price");
                    await Payroll.SaveAsync();
                    textBox_nameShow.Text = name;
                    textBox_shenfen.Text = "学生";
                    textBox4_courseNameShow.Text = courseName;
                    textBoxCourseTypeShow.Text = courseType;
                    textBox_telShow.Text = tel;
                    textBox_courseTime_left.Text = (times - 1).ToString();
                    textBox_courseTimeSum.Text = courseTime;
                    textBox_teacherShow.Text = myObject.Get<String>("stuTeacher");
                    string stuTeacher = textBox_teacherShow.Text;

                    byte[] buffer = new byte[1];
                    int nRet_boomer = Reader.ControlBuzzer(20, 1, buffer);//（占空比，次数，没有用但是要的一个参数）
                    int nRet_led = Reader.ControlLED(20, 3, buffer);
                    if(times-1<=3)
                    { MessageBox.Show("当前剩余"+(times-1).ToString()+"课时"+"，请及时充值！"); }
                    MessageBox.Show("扣费成功！");

                    /*****存储数据到本地***************/
                    try
                    {
                        sql = new sq("data source= D:/data/test.db");
                        //创建名为payroll的数据表
                        sql.CreateTable("payroll", new string[] { "time", "month", "student", "teacher", "pay" },
                                                    new string[] { "TEXT", "TEXT", "TEXT", "TEXT", "INTEGER"});
                        //插入数据
                        sql.InsertValues("payroll", new string[] { DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss"), DateTime.Now.ToString("yyyyMM"), name,stuTeacher, coursePrice });
                        //need update data here

                        //关闭数据库
                        sql.CloseConnection();
                        

                        //存储到excel
                        Excel.Application excelApp = new Excel.Application();
                        if (excelApp == null)
                        {
                            // if equal null means EXCEL is not installed.  
                            MessageBox.Show("Excel is not properly installed!");
                        }

                        string excelPath = @"D:\课时流水.xls";
                        string filename = excelPath;// @"D:\生产产量纪录.xls";
                                                    // open a workbook,if not exist, create a new one  
                        Excel.Workbook workBook;
                        if (File.Exists(filename))
                        {
                            workBook = excelApp.Workbooks.Open(filename, 0, false, 5, "", "", true, Excel.XlPlatform.xlWindows, "\t", false, false, 0, true, 1, 0);
                        }
                        else
                        {
                            workBook = excelApp.Workbooks.Add(true);
                        }
                        //new a worksheet  
                        Excel.Worksheet workSheet = workBook.ActiveSheet as Excel.Worksheet;
                        //write data
                        workSheet = (Excel.Worksheet)workBook.Worksheets.get_Item(1);//获得第i个sheet，准备写入  
                        workSheet.Cells[1, 1] = "上课时间";
                        workSheet.Cells[1, 2] = "月份";
                        workSheet.Cells[1, 3] = "学生姓名";
                        workSheet.Cells[1, 4] = "老师姓名";
                        workSheet.Cells[1, 5] = "课程价格";
                        

                        Microsoft.Office.Interop.Excel.Range range = workSheet.UsedRange;
                        int colCount = range.Columns.Count;
                        int rowCount = range.Rows.Count;

                        workSheet.Cells[rowCount + 1, 1] = DateTime.Now.ToString("yyyy/MM/dd,HH:mm:ss");
                        workSheet.Cells[rowCount + 1, 2] = DateTime.Now.ToString("yyyyMM");
                        workSheet.Cells[rowCount + 1, 3] = name;
                        workSheet.Cells[rowCount + 1, 4] = textBox_teacherShow.Text;
                        workSheet.Cells[rowCount + 1, 5] = coursePrice;
                       
                        //set visible the Excel will run in background  
                        excelApp.Visible = false;
                        //set false the alerts will not display  
                        excelApp.DisplayAlerts = false;
                        workBook.SaveAs(filename);
                        workBook.Close(false, Missing.Value, Missing.Value);
                        //quit and clean up objects  
                        excelApp.Quit();
                        workSheet = null;
                        workBook = null;
                        excelApp = null;
                        GC.Collect();

                    }
                    catch (Exception erro)
                    {
                        MessageBox.Show("本地数据存储错误" + erro.Message);
                        this.Close();
                    }

                }


            }

            }

      async private void button2_Click(object sender, EventArgs e)
        {
       

            if (radioButton_teacher.Checked)
            {
                AVQuery<AVObject> query = new AVQuery<AVObject>("Teacher").WhereEqualTo("name", textBox_name_loster.Text).WhereEqualTo("tel", textBox_tel_loster.Text);
  
               await query.FindAsync().ContinueWith(t => {
                    IEnumerable<AVObject> persons = t.Result;
                    int sum = persons.Count();
               
                   
                });
                int num = query.CountAsync().Result;
                if (num == 0)  //查到的数据为0个
                { MessageBox.Show("没有相关老师信息！"); }
                else
                {
                    AVObject myObject = query.FirstAsync().Result;

                    string Card_num = readCard(0x00, 1).Substring(0, 8);

                    string name = myObject["name"].ToString();
                    string tel = myObject["tel"].ToString();
                    string gender = myObject["gender"].ToString();
                    string birth = myObject["birth"].ToString();
                    string courseName = myObject["course"].ToString();                  
                    string date_YMD = myObject["regTime"].ToString();
                    try { 
                    writeCard(0x04, 1, s.KeyID2Card(myObject.ObjectId));                    
                    writeCard(0x02, 1, s.String2Unicode("老师"));                
                    byte[] buffer = new byte[1];
                    int nRet_boomer = Reader.ControlBuzzer(10, 1, buffer);//（占空比，次数，没有用但是要的一个参数）
                    MessageBox.Show("补卡成功");
                    }
                    catch(Exception err)
                    { MessageBox.Show("刷卡机器错误："+err.Message); }


                }

                //对M1卡进行读写

            }
            else if(radioButton_stu.Checked)
            {
                AVQuery<AVObject> query = new AVQuery<AVObject>("Student").WhereEqualTo("name", textBox_name_loster.Text).WhereEqualTo("tel", textBox_tel_loster.Text);
                
                await query.FindAsync().ContinueWith(t => {
                    IEnumerable<AVObject> persons = t.Result;
                    //sum = 0;
                    int sum = persons.Count();
                   
                });
                int num = query.CountAsync().Result;
               
                if (num==0)  //查到的数据为0个
                { MessageBox.Show("没有查到相关学生信息！"); }
                else
                {
                    AVObject myObject = query.FirstAsync().Result;
                   
                    /*************************************************************************/
                    //write data to leancloud

                    string Card_num = readCard(0x00, 1).Substring(0, 8);
                    string name = myObject.Get<String>("name");
                    string tel = myObject.Get<String>("tel");
                    string gender = myObject.Get<String>("gender");
                    string birth = myObject.Get<String>("birth");
                    string courseName = myObject.Get<String>("courseName");
                    string level = myObject.Get<String>("level");
                    string courseType = myObject.Get<String>("courseType");
                    string courseTime = myObject.Get<String>("courseTime");   //课时数
                    string coursePrice= myObject.Get<String>("price");   //单价
                    string courseTimeLeft = myObject.Get<String>("courseTimeLeft");   //剩余课时数
                    string age = myObject.Get<String>("age");
                   
                    //Student["courseTimeLeft"] = courseTime;          //剩余课时数
                    string objectID = myObject.ObjectId;

                    int times = Convert.ToInt16(courseTimeLeft);
                    int price = Convert.ToInt16(coursePrice);
                    int sum_money = price * times;
                    label_sum.Text = sum_money.ToString();
                    //label_sum = ToInt16(String);          

                    string date_YMD = myObject.Get<String>("regTime");
                    try {
                    writeCard(0x04, 1, s.KeyID2Card(myObject.ObjectId));
                    writeCard(0x02, 1, s.String2Unicode("学生"));
                    
                    byte[] buffer = new byte[1];
                    int nRet_boomer = Reader.ControlBuzzer(10, 1, buffer);//（占空比，次数，没有用但是要的一个参数）
                    MessageBox.Show("补卡成功");
                    }
                    catch (Exception err)
                    { MessageBox.Show("刷卡机器错误：" + err.Message); }
                    /****************************************************************************************/
                }
            }
            else
            {
                MessageBox.Show("请选择补卡人身份 ");
              
            }
        }

       async private void button_readCard_Click(object sender, EventArgs e)
        {
            try { 
                string shenfen = s.Unicode2String(readCard(0x02, 1));
                textBox_shenfen.Text = shenfen;
            }
            catch(Exception err)
            {
                MessageBox.Show ("error:"+err.Message);
            }

            if (textBox_shenfen.Text == "学生")
            {
                textBox_wage.Visible = false;
                label_wage.Visible = false;
                textBoxCourseTypeShow.Visible = true;
                textBox_courseTime_left.Visible = true;
                textBox_courseTimeSum.Visible = true;
                textBox_teacherShow.Visible = true;
                textBoxCourseTypeShow.Visible = true;
                label_coursetime_left.Visible = true;
                label_courseTypeShow.Visible = true;
                label_teacherShow.Visible = true;
                label_sumCourse.Visible = true;
                comboBox1.Visible = true;
                //读卡
                string ObID = "";
                try
                {
                    //获取信息
                    ObID = s.Card2KeyID(readCard(0x04, 1));
                    //MessageBox.Show(ObID);
                }
                catch (Exception error)
                {
                    MessageBox.Show("刷卡机需要重启，错误：" + error.Message);
                    this.Close();

                }

                AVQuery<AVObject> query = new AVQuery<AVObject>("Student").WhereEqualTo("objectId", ObID);
                await query.FindAsync().ContinueWith(t =>
                {
                    IEnumerable<AVObject> persons = t.Result;
                    //sum = 0;
                    int sum = persons.Count();

                });

                if (query.CountAsync().Result == 0)  //查到的数据为0个
                {
                    MessageBox.Show("没有查到相关学生信息！");
                }
                else
                {
                    AVObject myObject = query.FirstAsync().Result;
                    /*************************************************************************/
                    //read data from leancloud

                    string Card_num = readCard(0x00, 1).Substring(0, 8);
                    string name = myObject.Get<String>("name");
                    string tel = myObject.Get<String>("tel");
                    string gender = myObject.Get<String>("gender");
                    string birth = myObject.Get<String>("birth");
                    string courseName = myObject.Get<String>("courseName");
                    string level = myObject.Get<String>("level");
                    string courseType = myObject.Get<String>("courseType");
                    string courseTime = myObject.Get<String>("courseTime");   //总课时数
                    string courseTimeLeft = myObject.Get<String>("courseTimeLeft");   //剩余课时数
                    string coursePrice = myObject.Get<String>("price");   //单价
                    string teacher = myObject.Get<String>("stuTeacher");


                    textBox_nameShow.Text = name;

                    textBox4_courseNameShow.Text = courseName;
                    textBoxCourseTypeShow.Text = courseType;
                    textBox_telShow.Text = tel;
                    textBox_courseTime_left.Text = courseTimeLeft;
                    textBox_courseTimeSum.Text = courseTime;
                    textBox_teacherShow.Text = teacher;


                    byte[] buffer = new byte[1];
                    int nRet_boomer = Reader.ControlBuzzer(20, 1, buffer);//（占空比，次数，没有用但是要的一个参数）
                    int nRet_led = Reader.ControlLED(20, 1, buffer);
                }
            }
            else if(textBox_shenfen.Text == "老师")
            {
                textBox_wage.Visible = true;
                label_wage.Visible = true;
                textBoxCourseTypeShow.Visible = false;
                textBox_courseTime_left.Visible = false;
                textBox_courseTimeSum.Visible = false;
                textBox_teacherShow.Visible = false;
                textBoxCourseTypeShow.Visible = false;
                label_coursetime_left.Visible = false;
                label_courseTypeShow.Visible = false;
                label_teacherShow.Visible = false;
                label_sumCourse.Visible = false;
                comboBox1.Visible = false;
               
                string ObID = "";
                try
                { ObID = s.Card2KeyID(readCard(0x04, 1)); }
                catch (Exception error){
                    MessageBox.Show("刷卡机需要重启，错误：" + error.Message);
                    this.Close();
                }
                try
                {
                    AVQuery<AVObject> query = new AVQuery<AVObject>("Teacher").WhereEqualTo("objectId", ObID);
                    await query.FindAsync().ContinueWith(t => {
                        IEnumerable<AVObject> persons = t.Result;
                    });

                    if (query.CountAsync().Result == 0)  //查到的数据为0个
                    {
                        MessageBox.Show("没有查到相关老师信息！");
                    }
                    else
                    {
                        AVObject myObjectTea = query.FirstAsync().Result;
                        /*************************************************************************/
                        //read data from leancloud
                        string Card_num = myObjectTea.Get<String>("Card_num");
                        string name = myObjectTea.Get<String>("name");
                        string courseName = myObjectTea.Get<String>("course");
                        string tel = myObjectTea.Get<String>("tel");
                        string percent = myObjectTea.Get<String>("percent");

                        textBox_nameShow.Text = name;
                        textBox_shenfen.Text = "老师";
                        textBox4_courseNameShow.Text = courseName;
                        textBox_telShow.Text = tel;

                        /************************************************** ***/
                        //string name = textBox_nameShow.Text;
                        //string telNum = textBox_telShow.Text;

                        AVQuery<AVObject> queryPayRoll = new AVQuery<AVObject>("Student").WhereEqualTo("stuTeacher", name);//.WhereNotEqualTo("courseTimeLeft","0");  //WhereEqualTo("tel",telNum).        
                        IEnumerable<AVObject> myObject = await queryPayRoll.FindAsync();
                        string allStudent = "学生：\n";
                        foreach (AVObject item in myObject)
                        {
                            allStudent= allStudent+item.Get<String>("name")+"\n";
                        }
                       
                        byte[] buffer = new byte[1];
                        int nRet_boomer = Reader.ControlBuzzer(20, 1, buffer);//（占空比，次数，没有用但是要的一个参数）
                        int nRet_led = Reader.ControlLED(20, 3, buffer);
                        MessageBox.Show(allStudent);

                    }
                }
                catch (Exception ee)
                {
                    MessageBox.Show("错误：" + ee.Message);
                }
            }

            
        }

        async private void button_wage_Click(object sender, EventArgs e)
        {
            textBox_wage.Visible = true;
            label_wage.Visible = true;
            textBoxCourseTypeShow.Visible = false;
            textBox_courseTime_left.Visible = false;
            textBox_courseTimeSum.Visible = false;
            textBox_teacherShow.Visible = false;
            textBoxCourseTypeShow.Visible = false;
            label_coursetime_left.Visible = false;
            label_courseTypeShow.Visible = false;
            label_teacherShow.Visible = false;
            label_sumCourse.Visible = false;
            comboBox1.Visible = false;
            /*
            textBox_nameShow.Text = s.Unicode2String(readCard(0x05, 1));
            textBox_shenfen.Text = s.Unicode2String(readCard(0x02, 1));
            textBox4_courseNameShow.Text = s.Unicode2String(readCard(0x0C, 1));            
            textBox_telShow.Text = s.U2S(readCard(0x06, 1));
            string percent = s.U2S(readCard(0x18, 1));
            */
            string ObID = "";
            try
            {
                //获取信息
                ObID = s.Card2KeyID(readCard(0x04, 1));
                //MessageBox.Show(ObID);
            }
            catch (Exception error)
            {
                MessageBox.Show("刷卡机需要重启，错误：" + error.Message);
                this.Close();

            }
            try { 
                AVQuery<AVObject> query = new AVQuery<AVObject>("Teacher").WhereEqualTo("objectId", ObID);
                await query.FindAsync().ContinueWith(t => {
                    IEnumerable<AVObject> persons = t.Result;
                });
            
                if (query.CountAsync().Result == 0)  //查到的数据为0个
                {
                    MessageBox.Show("没有查到相关老师信息！");
                }
                else
                {
                    AVObject myObjectTea = query.FirstAsync().Result;
                    /*************************************************************************/
                    //read data from leancloud
                    string Card_num = myObjectTea.Get<String>("Card_num");
                    string name = myObjectTea.Get<String>("name");
                    string courseName = myObjectTea.Get<String>("course");
                    string tel = myObjectTea.Get<String>("tel");
                    string percent = myObjectTea.Get<String>("percent");

                    textBox_nameShow.Text = name;
                    textBox_shenfen.Text = "老师";
                    textBox4_courseNameShow.Text = courseName;
                    textBox_telShow.Text = tel;
                   
                    /************************************************** ***/
                    //string name = textBox_nameShow.Text;
                    //string telNum = textBox_telShow.Text;
                    string month = DateTime.Now.ToString("yyyyMM");
                    AVQuery<AVObject> queryPayRoll = new AVQuery<AVObject>("Payroll").WhereEqualTo("teacher", name).WhereEqualTo("month", month);  //WhereEqualTo("tel",telNum).        
                    IEnumerable<AVObject> myObject = await queryPayRoll.FindAsync();

                    int sum = 0;
                    foreach (AVObject item in myObject)
                    {sum = sum + Convert.ToInt16(item.Get<String>("pay"));}

                    sum = (sum * Convert.ToInt16(percent)) / 10;
                    textBox_wage.Text = sum.ToString();

                    byte[] buffer = new byte[1];
                    int nRet_boomer = Reader.ControlBuzzer(20, 1, buffer);//（占空比，次数，没有用但是要的一个参数）
                    int nRet_led = Reader.ControlLED(20, 3, buffer);

                    //工资单数据库存储
                    /********存储到leancloud*****************************************/
                    AVQuery<AVObject> query_pay = new AVQuery<AVObject>("Wage").WhereEqualTo("teacher", name).WhereEqualTo("month", month);  //WhereEqualTo("tel",telNum). 
                    await query_pay.FindAsync().ContinueWith(t => {
                        IEnumerable<AVObject> persons = t.Result;
                    });
             
                    if (query_pay.CountAsync().Result == 0)  //查到的数据为0个,之前没有存储过
                    {
                        AVObject wage = new AVObject("Wage");
                        wage["teacher"] = name;
                        wage["month"] = month;
                        wage["money"] = sum.ToString();
                        Task saveTask = wage.SaveAsync();
                        await saveTask;

                        /********存储到sqlite*****************************************/
                        sql = new sq("data source= D:/data/test.db");
                        //创建名为table1的数据表
                        sql.CreateTable("Wage", new string[] { "month", "Teacher", "money"},
                                                    new string[] { "TEXT", "TEXT", "INTEGER"});
                        //插入数据
                        sql.InsertValues("Wage", new string[] { month, name, sum.ToString() });
                        sql.CloseConnection();

                        /********存储到excel*****************************************/
                        Excel.Application excelApp = new Excel.Application();
                        if (excelApp == null)
                        {
                            // if equal null means EXCEL is not installed.  
                            MessageBox.Show("Excel is not properly installed!");
                        }

                        string excelPath = @"D:\老师工资流水.xls";
                        string filename = excelPath;// @"D:\生产产量纪录.xls";
                                                    // open a workbook,if not exist, create a new one  
                        Excel.Workbook workBook;
                        if (File.Exists(filename))
                        {
                            workBook = excelApp.Workbooks.Open(filename, 0, false, 5, "", "", true, Excel.XlPlatform.xlWindows, "\t", false, false, 0, true, 1, 0);
                        }
                        else
                        {
                            workBook = excelApp.Workbooks.Add(true);
                        }
                        //new a worksheet  
                        Excel.Worksheet workSheet = workBook.ActiveSheet as Excel.Worksheet;
                        //write data
                        workSheet = (Excel.Worksheet)workBook.Worksheets.get_Item(1);//获得第i个sheet，准备写入  
                        workSheet.Cells[1, 1] = "月份";
                        workSheet.Cells[1, 2] = "老师";
                        workSheet.Cells[1, 3] = "工资";
                        

                        Microsoft.Office.Interop.Excel.Range range = workSheet.UsedRange;
                        int colCount = range.Columns.Count;
                        int rowCount = range.Rows.Count;

                        workSheet.Cells[rowCount + 1, 1] = month;
                        workSheet.Cells[rowCount + 1, 2] = name;
                        workSheet.Cells[rowCount + 1, 3] = sum.ToString();
                        
                        //set visible the Excel will run in background  
                        excelApp.Visible = false;
                        //set false the alerts will not display  
                        excelApp.DisplayAlerts = false;
                        workBook.SaveAs(filename);
                        workBook.Close(false, Missing.Value, Missing.Value);
                        //quit and clean up objects  
                        excelApp.Quit();
                        workSheet = null;
                        workBook = null;
                        excelApp = null;
                        GC.Collect();

                    }
                    else
                    { }
                }
            }
            catch(Exception ee)
            {
                MessageBox.Show("错误："+ee.Message);
            }
        }

        async private void comboBox_courseName_SelectedIndexChanged(object sender, EventArgs e)
        {
      
            switch (comboBox_courseName.Text)
            {
                case "吉他":
                    comboBox_stuTeacher.Items.Clear();
                    AVQuery<AVObject> query1 = new AVQuery<AVObject>("Teacher").WhereEqualTo("course", "吉他");
                    IEnumerable<AVObject> myObject1 = await query1.FindAsync();
                    foreach (AVObject item in myObject1)
                    {
                        comboBox_stuTeacher.Items.Add(item.Get<String>("name"));
                       
                    }
                    break;
                case "二胡":
                    comboBox_stuTeacher.Items.Clear();
                    AVQuery<AVObject> query2 = new AVQuery<AVObject>("Teacher").WhereEqualTo("course", "二胡");
                    IEnumerable<AVObject> myObject2 = await query2.FindAsync();
                    foreach (AVObject item in myObject2)
                    {
                        comboBox_stuTeacher.Items.Add(item.Get<String>("name"));

                    }
                    break;
                case "尤克里里":
                    comboBox_stuTeacher.Items.Clear();
                    AVQuery<AVObject> query3 = new AVQuery<AVObject>("Teacher").WhereEqualTo("course", "尤克里里");
                    IEnumerable<AVObject> myObject3 = await query3.FindAsync();
                    foreach (AVObject item in myObject3)
                    {
                        comboBox_stuTeacher.Items.Add(item.Get<String>("name"));

                    }
                    break;
                case "古筝":
                    comboBox_stuTeacher.Items.Clear();
                    AVQuery<AVObject> query4 = new AVQuery<AVObject>("Teacher").WhereEqualTo("course", "古筝");
                    IEnumerable<AVObject> myObject4 = await query4.FindAsync();
                    foreach (AVObject item in myObject4)
                    {
                        comboBox_stuTeacher.Items.Add(item.Get<String>("name"));

                    }
                    break;
                case "钢琴":
                    comboBox_stuTeacher.Items.Clear();
                    AVQuery<AVObject> query5 = new AVQuery<AVObject>("Teacher").WhereEqualTo("course", "钢琴");
                    IEnumerable<AVObject> myObject5 = await query5.FindAsync();
                    foreach (AVObject item in myObject5)
                    {
                        comboBox_stuTeacher.Items.Add(item.Get<String>("name"));

                    }
                    break;
                case "电子琴":
                    comboBox_stuTeacher.Items.Clear();
                    AVQuery<AVObject> query6 = new AVQuery<AVObject>("Teacher").WhereEqualTo("course", "电子琴");
                    IEnumerable<AVObject> myObject6 = await query6.FindAsync();
                    foreach (AVObject item in myObject6)
                    {
                        comboBox_stuTeacher.Items.Add(item.Get<String>("name"));

                    }
                    break;
                case "小提琴":
                    comboBox_stuTeacher.Items.Clear();
                    AVQuery<AVObject> query7 = new AVQuery<AVObject>("Teacher").WhereEqualTo("course", "小提琴");
                    IEnumerable<AVObject> myObject7 = await query7.FindAsync();
                    foreach (AVObject item in myObject7)
                    {
                        comboBox_stuTeacher.Items.Add(item.Get<String>("name"));

                    }
                    break;
                default:
                    comboBox_stuTeacher.Items.Clear();
                    break;

            }
        }

       async private void button_savePayroll_Click(object sender, EventArgs e)
        {
            
            string ObID = "";
            try
            {
                //获取信息
                ObID = s.Card2KeyID(readCard(0x04, 1));
                //MessageBox.Show(ObID);
            }
            catch (Exception error)
            {
                MessageBox.Show("刷卡机需要重启，错误：" + error.Message);
                this.Close();

            }
            try
            {
                AVQuery<AVObject> queryTea = new AVQuery<AVObject>("Teacher").WhereEqualTo("objectId", ObID);
                await queryTea.FindAsync().ContinueWith(t =>
                {
                    IEnumerable<AVObject> persons = t.Result;
                });

                if (queryTea.CountAsync().Result == 0)  //查到的数据为0个
                {
                    MessageBox.Show("没有查到相关老师信息！");
                }
                else
                {
                    AVObject myObjectTea = queryTea.FirstAsync().Result;
                    /*************************************************************************/
                    //read data from leancloud
                    string Card_num = myObjectTea.Get<String>("Card_num");
                    string name = myObjectTea.Get<String>("name");
                    string courseName = myObjectTea.Get<String>("course");
                    string tel = myObjectTea.Get<String>("tel");
                    string percent = myObjectTea.Get<String>("percent");
                    textBox_nameShow.Text=name;
                    textBox_telShow.Text=tel;
                }
            }
            catch(Exception ee)
            {
                MessageBox.Show("错误："+ee.Message);
            }
            /************************************************** ***/
            //string name = textBox_nameShow.Text;
            //string telNum = textBox_telShow.Text;
            try
            {
                string ym = DateTime.Now.ToString("yyyyMM");
                //string month = DateTime.Now.ToString("yyyy年MM月dd日 HH:mm:ss");
                AVQuery<AVObject> query = new AVQuery<AVObject>("Payroll").WhereEqualTo("teacher", textBox_nameShow.Text).WhereEqualTo("month", ym);  //WhereEqualTo("tel",telNum).        
                IEnumerable<AVObject> myObject = await query.FindAsync();
                AVObject teacher = query.FirstAsync().Result;
                string fipath = @"d:\Pay\Payroll" + ym + teacher.Get<string>("teacher") + ".txt";
                WriteMessage(fipath, "\n\n");
                string title = "时间" + "\t\t\t" + "老师" + "\t" + "学生" + "\t" + "金额";
                WriteMessage(fipath, title);
                int sum = 0;
                foreach (AVObject item in myObject)
                {

                    string msg = item.Get<String>("time") + "\t\t" + item.Get<String>("teacher") + "\t" + item.Get<String>("student") + "\t" + item.Get<String>("pay");

                    WriteMessage(fipath, msg);
                    sum = sum + Convert.ToInt16(item.Get<String>("pay"));

                }
                sum = sum / 2;
                //MessageBox.Show(sum.ToString());
                WriteMessage(fipath, "总金额：" + sum.ToString());
                /*
                  Process pro = new Process();
                  pro.StartInfo.FileName = fipath;//文件路径
                  pro.StartInfo.CreateNoWindow = true;
                  pro.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                  pro.StartInfo.Verb = "Print";
                  pro.Start();
                  */
            }
            catch(Exception ee)
            {
                MessageBox.Show("错误2："+ee.Message);
            }
        }
        /// <summary>
        /// 输出指定信息到文本文件
        /// </summary>
        /// <param name="msg">输出信息</param>
        public void WriteMessage(string msg)
        {
            using (FileStream fs = new FileStream(@"d:\test.txt", FileMode.OpenOrCreate, FileAccess.Write))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.BaseStream.Seek(0, SeekOrigin.End);
                    sw.WriteLine("{0}\n", msg, DateTime.Now);
                    sw.Flush();
                }
            }
        }

        /// <summary>
        /// 输出指定信息到文本文件
        /// </summary>
        /// <param name="path">文本文件路径</param>
        /// <param name="msg">输出信息</param>
        public void WriteMessage(string path, string msg)
        {
            using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.BaseStream.Seek(0, SeekOrigin.End);
                    sw.WriteLine("{0}\n", msg, DateTime.Now);
                    sw.Flush();
                }
            }
        }

        private void button_courseAdd_Click(object sender, EventArgs e)
        {

        }

        private void button_registerExerciser_Click(object sender, EventArgs e)
        {
            try
            {
                int banlance = 0;
                string name = textBox_exerciserName.Text;
                string tel = textBox_exerciseTel.Text;
                string regTime = DateTime.Now.ToString("yyyy-MM-dd");
                string cardNum = readCard(0x00, 1).Substring(0, 8);

                AVObject exerciser = new AVObject("exerciser");
                exerciser["Card_num"] = cardNum;
                exerciser["name"] = name;
                exerciser["tel"] = tel;
                exerciser["balance"] = banlance.ToString();
                exerciser.SaveAsync();

                textBox_addMoney.Text = banlance.ToString();
                textBox_balance.Text = banlance.ToString();

                while (exerciser.ObjectId == null)
                { Thread.Sleep(100); }
                writeCard(0x04, 1, s.KeyID2Card(exerciser.ObjectId));
                // writeCard(0x02, 1, s.String2Unicode("学生"));
                writeCard(0x05, 1, s.String2Unicode(name));
                writeCard(0x06, 1, s.S2U(tel));
                writeCard(0x10, 1, s.S2U(banlance.ToString()));

                /********存储数据到本地sqlite**********************************/
                sql = new sq("data source=" + dataBasePath);
                //创建名为table1的数据表
                sql.CreateTable("exerciser", new string[] { "regTime", "Card_num", "Name", "tel", "balance" },
                                           new string[] { "TEXT", "TEXT", "TEXT", "TEXT", "INTEGER" });
                //插入数据
                sql.InsertValues("exerciser", new string[] { regTime, cardNum, name, tel, banlance.ToString() });
                sql.CloseConnection();

                /***********************************************************/
            }
            catch 
            {
                MessageBox.Show("请重启机器");
            }
            finally
            {
                byte[] buffer = new byte[1];
                int nRet_boomer = Reader.ControlBuzzer(20, 1, buffer);//（占空比，次数，没有用但是要的一个参数）
            }
            

        }

        private void button_addMoney_Click(object sender, EventArgs e)
        {
            textBox_exerciserName.Text = s.Unicode2String(readCard(0x05, 1));
            textBox_exerciseTel.Text = s.U2S(readCard(0x06, 1));
            string name = textBox_exerciserName.Text;
            string tel = textBox_exerciseTel.Text;

            AVQuery<AVObject> query = new AVQuery<AVObject>("exerciser").WhereEqualTo("name", textBox_exerciserName.Text).WhereEqualTo("tel", textBox_exerciseTel.Text);
            //IEnumerable<AVObject> item = query.FindAsync();
            int num = query.CountAsync().Result;
            if (num==0)
            {
                MessageBox.Show("没有该卡信息");
            }
            else
            {
                AVObject myObject = query.FirstAsync().Result;
                while (myObject.ObjectId == null)                           //这一步很重要，等待获取结束
                { Thread.Sleep(100); }
                int moneyCard = Convert.ToInt16(s.U2S(readCard(0x20, 1)));
                int moneyLeancloud = Convert.ToInt16(myObject.Get<string>("balance"));
         
                moneyLeancloud = moneyLeancloud + Convert.ToInt16(textBox_addMoney.Text);
                moneyCard = moneyLeancloud;
               
                writeCard(0x10, 1, s.S2U(moneyLeancloud.ToString()));
                myObject["balance"] = moneyLeancloud.ToString();                
                myObject.SaveAsync();
                
                textBox_balance.Text = moneyLeancloud.ToString();
                byte[] buffer = new byte[1];
                int nRet_boomer = Reader.ControlBuzzer(20, 1, buffer);//（占空比，次数，没有用但是要的一个参数）


                /********更新本地数据库*************************************/
                sql = new sq("data source= D:/data/test.db");
                string updateCommand= "UPDATE "+ "exerciser "+ "SET " +"balance = \"" + moneyLeancloud.ToString()+ "\" WHERE NAME= \""+ name+"\" "+"AND "+"TEL= "+tel+";";
                MessageBox.Show(updateCommand);
                sql.ExecuteQuery(updateCommand);
                sql.CloseConnection();
                /***********************************************************/


            }
        }

        private void comboBox_exerciserCourse_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch(comboBox_exerciserCourse.Text)
            {
                case "钢琴":
                    textBox_exercisePrice.Text="100";
                    break;
                case "古筝":
                    textBox_exercisePrice.Text = "50";
                    break;
                default:
                    break;
            
            }
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            textBox_exerciserName.Text = s.Unicode2String(readCard(0x05, 1));
            textBox_exerciseTel.Text = s.U2S(readCard(0x06, 1));
            string name = textBox_exerciserName.Text;
            string tel = textBox_exerciseTel.Text;

            string enterTime= DateTime.Now.ToShortTimeString().ToString();
            string enterHour= DateTime.Now.Hour.ToString();
            string enterMinute= DateTime.Now.Minute.ToString();
            string course = comboBox_exerciserCourse.Text;
            string price = textBox_exercisePrice.Text;
            textBox_startTime.Text = enterTime;
            //textBox_endTime.Text= DateTime.Now.ToShortTimeString().ToString();
            writeCard(0x02, 1, s.String2Unicode(course));//课程名
            writeCard(0x04, 1, s.S2U(price));        //单价
            writeCard(0x18, 1, s.S2U(enterTime)); //进入琴行，开始时间，写入卡
            //分开存储方便计算
            writeCard(0x1A, 1, s.S2U(enterHour)); //开始的小时  理论上用08 09 0A
            writeCard(0x1C, 1, s.S2U(enterMinute)); //开始的分钟
            byte[] buffer = new byte[1];
            int nRet_boomer = Reader.ControlBuzzer(20, 1, buffer);//（占空比，次数，没有用但是要的一个参数）
        }

        private void button3_Click(object sender, EventArgs e)
        {
            textBox_exerciserName.Text = s.Unicode2String(readCard(0x05, 1));
            textBox_exerciseTel.Text = s.U2S(readCard(0x06, 1));
            string name = textBox_exerciserName.Text;
            string tel = textBox_exerciseTel.Text;
            string cardNum= readCard(0x00, 1).Substring(0, 8);

            string enterTime = s.U2S(readCard(0x18, 1));
            string enterHour = s.U2S(readCard(0x1A, 1));
            string enterMinute = s.U2S(readCard(0x1C, 1));

            string course = s.Unicode2String(readCard(0x02, 1));
            string price = s.U2S(readCard(0x04, 1));
            string banlance = s.U2S(readCard(0x10,1));  //读取余额
            comboBox_exerciserCourse.Text = course;
            textBox_exercisePrice.Text= price;
            string endTime = DateTime.Now.ToShortTimeString().ToString();
            string endHour = DateTime.Now.Hour.ToString();
            string endMinute = DateTime.Now.Minute.ToString();

           // double bT = Convert.ToDouble(enterTime);
            double bH = Convert.ToDouble(enterHour);
            double bM = Convert.ToDouble(enterMinute);

           // double eT = Convert.ToDouble(endTime);
            double eH = Convert.ToDouble(endHour);
            double eM = Convert.ToDouble(endMinute);

            /*************************************/
            /****************计算总时间***************/
            double sumTime = (eH - bH) +(eM-bM)/60;

            textBox_sumTime.Text = sumTime.ToString();
            /*************************************/
            textBox_startTime.Text = enterTime;
            textBox_endTime.Text = endTime;

            writeCard(0x19, 1, s.S2U(endTime)); //离开琴行，开始时间，写入卡
            //分开存储方便计算
            writeCard(0x1D, 1, s.S2U(endHour)); //离开的小时
            writeCard(0x1E, 1, s.S2U(endMinute)); //离开的分钟

            double money = Convert.ToDouble(banlance);
            double cost = sumTime * Convert.ToDouble(price);
            textBox_balance.Text =((int)(money-cost)).ToString();
            string new_balance= ((int)(money - cost)).ToString();
            writeCard(0x10,1,s.S2U(new_balance));
            // MessageBox.Show(s.U2S(readCard(0x0A, 1)));
            /*****存储数据到leancloud**********************/
            AVObject record = new AVObject("exerciserRecords");
            record["Card_num"] = readCard(0x00, 1).Substring(0, 8);
            record["name"] = name;
            record["tel"] = tel;
            record["course"]=course;
            record["cost"]=cost.ToString();
            record["balance"] = new_balance;
            record.SaveAsync();
            /****************************************************/
            /********存储数据到本地sqlite**********************************/
            sql = new sq("data source=" + dataBasePath);
            //创建名为table1的数据表
            sql.CreateTable("exerciserRecords", new string[] { "Name", "Card_num", "tel", "balance","course","cost" },
                                       new string[] { "TEXT", "TEXT", "TEXT", "INTEGER","TEXT","INTEGER" });
            //插入数据
            sql.InsertValues("exerciser", new string[] { name, cardNum, name, tel, banlance.ToString(),course,cost.ToString() });
            sql.CloseConnection();

            /***********************************************************/

            byte[] buffer = new byte[1];
            int nRet_boomer = Reader.ControlBuzzer(20, 1, buffer);//（占空比，次数，没有用但是要的一个参数）
        }
    }

}
