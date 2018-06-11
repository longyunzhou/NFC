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
//using ;
using Excel = Microsoft.Office.Interop.Excel;
using System.IO;
using System.Data.SQLite;
using System.Reflection; // 引用这个才能使用Missing字段 

namespace CSharpDEMO
{
   public class extend_Class
    {
        public string KeyID2Card(string ID)    //处理16进制数据的字符串
        {
            string txt = ID;
            txt = txt.PadLeft(32, '0');
            string result = Regex.Replace(txt, @".{2}", "$0 ");
            result = result.TrimEnd(' ');
            return result;
        }
        public string Card2KeyID(string text)  //返回16进制数据的字符串
        {
            text = text.Replace(" ", "");
            while (text[0] == '0')
            {
                text = text.Substring(1, text.Length - 1);
            }
            return text;
        }
        public string String2Unicode(string hanzi)      //处理汉字
        {
            string txt = StringToUnicode(hanzi);
            txt = System.Text.RegularExpressions.Regex.Replace(txt, "u", "");
            txt = txt.PadLeft(32, '0');
            // string txt = "12345678";
            //string result = Regex.Replace(ss, @"(\d{2}(?!$))", "$1 ");  // 修改成最后两个数字后不加空格
            string result = Regex.Replace(txt, @".{2}", "$0 ");
            result = result.TrimEnd(' ');

            return result;
        }
        public string Unicode2String(string text)   //返回汉字
        {
            text = text.Replace(" ", "");
            while (text[0] == '0')
            {
                text = text.Substring(1, text.Length - 1);
            }
            text = System.Text.RegularExpressions.Regex.Replace(text, @"(\w{4})", "$1u").Trim('u');
            // Console.WriteLine(text);
            string str = "u";
            text = str + text;
            string hanzi = UnicodeToString(text);
            return hanzi;
        }
        //把字符串转成16进制的字符串 "abcd"---->"61 62 63 64"
        public string S2U(string text)         //处理基本的字符串
        {
            string str =text;
            byte[] bytetest = System.Text.Encoding.Default.GetBytes(str.ToString()); //转成16进制
            string strr = ToHexString(bytetest);                                     //转成16进制形式的字符串
            strr = strr.PadLeft(32, '0');                                            //补0
            string result = Regex.Replace(strr, @".{2}", "$0 ").TrimEnd(' ');        //加空格
            return result;
        }
        //把16进制的字符串转成字符串 "61 62 63 64"---->"abcd"
        public string U2S(string text)        //返回基本的字符串
        {
            text = text.Replace(" ", "");
            while (text[0] == '0')
            {
                text = text.Substring(1, text.Length - 1);
            }
            string str = text;
            string result=HexStringToString(str,System.Text.Encoding.UTF8);
            return result;
        }
        public string StringToUnicode(string value)
        {
            byte[] bytes = Encoding.Unicode.GetBytes(value);
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i += 2)
            {
                // 取两个字符，每个字符都是右对齐。
                stringBuilder.AppendFormat("u{0}{1}", bytes[i + 1].ToString("x").PadLeft(2, '0'), bytes[i].ToString("x").PadLeft(2, '0'));
            }
            return stringBuilder.ToString();
        }
       
        /// <summary>
        /// Unicode转字符串
        /// </summary>
        /// <returns>The to string.</returns>
        /// <param name="unicode">Unicode.</param>
        public string UnicodeToString(string unicode)
        {
            string resultStr = "";
            string[] strList = unicode.Split('u');
            for (int i = 1; i < strList.Length; i++)
            {
                resultStr += (char)int.Parse(strList[i], System.Globalization.NumberStyles.HexNumber);
            }
            return resultStr;
        }
        public void writeData(string writeData,byte blk_add, byte num_blk)
        {
            byte mode = 0x0000;
            //byte blk_add = 0x10;// Convert.ToByte(readStart.Text, 16);
            //byte num_blk = 0x01;//Convert.ToByte(readNum.Text, 16);
            string password_A = "ff ff ff ff ff ff";
            byte[] snr = new byte[6];
            snr = convertSNR(password_A, 16);
            if (snr == null)
            {
                MessageBox.Show("序列号无效！", "错误");
                return;
            }

            byte[] buffer = new byte[16 * num_blk];
            string bufferStr = formatStr(writeData, num_blk);
            //string bufferStr = formatStr(result, num_blk);
            if (bufferStr == null)
            {
                MessageBox.Show("序列号无效！", "错误");
                return;
            }
            convertStr(buffer, bufferStr, 16 * num_blk);
            int nRet = Reader.MF_Write(mode, blk_add, num_blk, snr, buffer);           
        }
        public string readData(byte blk_add, byte num_blk)
        {
            byte mode = 0x0000;
            string password_A = "ff ff ff ff ff ff";
            byte[] snr = new byte[6];

            snr = convertSNR(password_A, 6);
            if (snr == null)
            {
                MessageBox.Show("序列号无效！", "错误");
                return "error";
            }

            byte[] buffer = new byte[16 * num_blk];
            int nRet = Reader.MF_Read(mode, blk_add, num_blk, snr, buffer);
            string str = ToHexString(buffer);
            return str;
        }

        private byte[] convertSNR(string str, int keyN)
        {
            string regex = "[^a-fA-F0-9]";
            string tmpJudge = Regex.Replace(str, regex, "");

            //长度不对，直接退回错误
            if (tmpJudge.Length != 12) return null;

            string[] tmpResult = Regex.Split(str, regex);
            byte[] result = new byte[keyN];
            int i = 0;
            foreach (string tmp in tmpResult)
            {
                result[i] = Convert.ToByte(tmp, 16);
                i++;
            }
            return result;
        }

        public string formatStr(string str, int num_blk)
        {

            string tmp = Regex.Replace(str, "[^a-fA-F0-9]", "");
            //长度不对直接报错
            //num_blk==-1指示不用检查位数
            if (num_blk == -1) return tmp;
            //num_blk==其它负数，为-1/num_blk
            if (num_blk < -1)
            {
                if (tmp.Length != -16 / num_blk * 2) return null;
                else return tmp;
            }
            if (tmp.Length != 16 * num_blk * 2) return null;
            else return tmp;
        }
        private void convertStr(byte[] after, string before, int length)
        {
            for (int i = 0; i < length; i++)
            {
                after[i] = Convert.ToByte(before.Substring(2 * i, 2), 16);
            }
        }

        /*
         * byte[]转16进制格式string：new byte[]{ 0x30, 0x31}转成"3031":
         */
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

        //字符串变成16进制形式的字符串
        private static string StringToHexString(string s)
        {
            byte[] b = System.Text.Encoding.Default.GetBytes(s);//按照指定编码将string编程字节数组
            string result = string.Empty;
            for (int i = 0; i < b.Length; i++)//逐字节变为16进制字符
            {
                result += Convert.ToString(b[i], 16);
            }
            return result;
        }
        //16进制形式的字符串变成字符串
        private static string HexStringToString(string hs, Encoding encode)
        {
            string strTemp = "";
            byte[] b = new byte[hs.Length / 2];
            for (int i = 0; i < hs.Length / 2; i++)
            {
                strTemp = hs.Substring(i * 2, 2);
                b[i] = Convert.ToByte(strTemp, 16);
            }
            //按照指定编码将字节数组变为字符串
            return encode.GetString(b);
        }

        private int WriteE(string path)
        {
            Excel.Application excelApp = new Excel.Application();
            if (excelApp == null)
            {
                // if equal null means EXCEL is not installed.  
                MessageBox.Show("Excel is not properly installed!");
                return 0;
            }

            string filename = path;// @"D:\生产产量纪录.xlsx";
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
           


            workSheet.Cells[rowCount + 1, 1] = "1";
            workSheet.Cells[rowCount + 1, 2] = "2";
            workSheet.Cells[rowCount + 1, 3] = "3";
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
            return 1;
        }
    }

}
